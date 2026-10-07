using System.IdentityModel.Tokens.Jwt;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HospitalManagement.Api.Authentication;
using HospitalManagement.Api.Authorization;
using HospitalManagement.Api.Data;
using HospitalManagement.Api.DTOs;
using HospitalManagement.Api.Exceptions;
using HospitalManagement.Api.Interfaces;
using HospitalManagement.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HospitalManagement.Api.Services;

public class AuthService(AppDbContext dbContext, IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<AuthSession> RegisterAsync(
        RegisterRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (Encoding.UTF8.GetByteCount(request.Password) > 72)
            throw new RequestValidationException("Password must be no more than 72 UTF-8 bytes.");
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && !new PhoneAttribute().IsValid(request.PhoneNumber))
            throw new RequestValidationException("Phone number is invalid.");

        if (await dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken))
            throw new ConflictException("An account with this email already exists.");

        var role = await dbContext.Roles
            .Include(candidate => candidate.RolePermissions)
            .ThenInclude(link => link.Permission)
            .SingleOrDefaultAsync(candidate => candidate.Name == "Patient", cancellationToken);
        if (role is null)
            throw new InvalidOperationException("The patient registration role has not been initialized.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            RoleId = role.Id,
            Role = role,
            IsActive = true
        };

        var session = CreateSession(user, ipAddress);
        dbContext.Users.Add(user);
        dbContext.RefreshTokens.Add(session.RefreshTokenEntity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<AuthSession> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users
            .Include(candidate => candidate.Role)
            .ThenInclude(role => role.RolePermissions)
            .ThenInclude(link => link.Permission)
            .SingleOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);

        if (user is null || !user.IsActive || Encoding.UTF8.GetByteCount(request.Password) > 72 ||
            !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAppException("Email or password is incorrect.");

        user.LastLoginAt = DateTime.UtcNow;
        var session = CreateSession(user, ipAddress);
        dbContext.RefreshTokens.Add(session.RefreshTokenEntity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<AuthSession> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var oldHash = HashRefreshToken(refreshToken);
        var now = DateTime.UtcNow;
        var oldToken = await dbContext.RefreshTokens
            .AsNoTracking()
            .Include(token => token.User)
            .ThenInclude(user => user.Role)
            .ThenInclude(role => role.RolePermissions)
            .ThenInclude(link => link.Permission)
            .SingleOrDefaultAsync(token => token.TokenHash == oldHash, cancellationToken);

        if (oldToken is null || oldToken.RevokedAt is not null || oldToken.ExpiresAt <= now || !oldToken.User.IsActive)
            throw new UnauthorizedAppException("Refresh token is invalid or expired.");

        var session = CreateSession(oldToken.User, ipAddress);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var revoked = await dbContext.RefreshTokens
            .Where(token => token.TokenHash == oldHash && token.RevokedAt == null && token.ExpiresAt > now)
            .ExecuteUpdateAsync(update => update
                .SetProperty(token => token.RevokedAt, now)
                .SetProperty(token => token.ReplacedByTokenHash, session.RefreshTokenEntity.TokenHash), cancellationToken);

        if (revoked != 1)
            throw new UnauthorizedAppException("Refresh token is invalid or has already been used.");

        dbContext.RefreshTokens.Add(session.RefreshTokenEntity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return session;
    }

    public async Task RevokeAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var hash = HashRefreshToken(refreshToken);
        var now = DateTime.UtcNow;
        await dbContext.RefreshTokens
            .Where(token => token.TokenHash == hash && token.RevokedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(token => token.RevokedAt, now), cancellationToken);
    }

    public async Task<AuthUserDto> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Include(candidate => candidate.Role)
            .ThenInclude(role => role.RolePermissions)
            .ThenInclude(link => link.Permission)
            .SingleOrDefaultAsync(candidate => candidate.Id == userId && candidate.IsActive, cancellationToken);

        if (user is null)
            throw new UnauthorizedAppException();

        return ToDto(user);
    }

    private AuthSession CreateSession(User user, string? ipAddress)
    {
        var now = DateTime.UtcNow;
        var accessExpiresAt = now.AddMinutes(_jwt.AccessTokenMinutes);
        var refreshExpiresAt = now.AddDays(_jwt.RefreshTokenDays);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new(ClaimTypes.Role, user.Role.Name)
        };
        claims.AddRange(user.Role.RolePermissions.Select(link =>
            new Claim(PermissionAuthorizationHandler.ClaimType, link.Permission.Code)));
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(_jwt.Issuer, _jwt.Audience, claims, now, accessExpiresAt, signingCredentials);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);
        var rawRefreshToken = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashRefreshToken(rawRefreshToken),
            ExpiresAt = refreshExpiresAt,
            CreatedByIp = ipAddress
        };

        return new AuthSession(
            new AuthResponseDto(accessToken, accessExpiresAt, ToDto(user)),
            rawRefreshToken,
            refreshExpiresAt,
            refreshToken);
    }

    private static AuthUserDto ToDto(User user) =>
        new(user.Id, user.Email, user.FirstName, user.LastName, user.Role.Name,
            user.Role.RolePermissions.Select(link => link.Permission.Code).Distinct().OrderBy(code => code).ToArray());

    private static string HashRefreshToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}