using HospitalManagement.Api.DTOs;

namespace HospitalManagement.Api.Interfaces;

public interface IAuthService
{
    Task<AuthSession> RegisterAsync(RegisterRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<AuthSession> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<AuthSession> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken);
    Task RevokeAsync(string? refreshToken, CancellationToken cancellationToken);
    Task<AuthUserDto> GetUserAsync(Guid userId, CancellationToken cancellationToken);
}