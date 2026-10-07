using System.ComponentModel.DataAnnotations;
using System.Text;
using HospitalManagement.Api.Data;
using HospitalManagement.Api.DTOs;
using HospitalManagement.Api.Exceptions;
using HospitalManagement.Api.Interfaces;
using HospitalManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Api.Services;

public class StaffAccountService(AppDbContext dbContext) : IStaffAccountService
{
    private static readonly string[] StandardStaffRoles =
    [
        "Doctor", "Nurse", "Receptionist", "Pharmacist", "Lab Technician", "Accountant"
    ];

    public async Task<IReadOnlyList<StaffRoleOptionDto>> GetAssignableRolesAsync(
        string actorRole,
        CancellationToken cancellationToken)
    {
        var roleNames = actorRole == "Super Admin"
            ? StandardStaffRoles.Concat(["Hospital Admin", "Super Admin"]).ToArray()
            : StandardStaffRoles;

        return await dbContext.Roles
            .AsNoTracking()
            .Where(role => roleNames.Contains(role.Name))
            .OrderBy(role => role.Name)
            .Select(role => new StaffRoleOptionDto(role.Name, role.Description))
            .ToListAsync(cancellationToken);
    }

    public async Task<StaffAccountDto> CreateAsync(
        CreateStaffAccountRequest request,
        string actorRole,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var roleName = request.Role.Trim();
        if (Encoding.UTF8.GetByteCount(request.Password) > 72)
            throw new RequestValidationException("Password must be no more than 72 UTF-8 bytes.");
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && !new PhoneAttribute().IsValid(request.PhoneNumber))
            throw new RequestValidationException("Phone number is invalid.");
        if (await dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken))
            throw new ConflictException("An account with this email already exists.");

        var allowedRoleNames = actorRole == "Super Admin"
            ? StandardStaffRoles.Concat(["Hospital Admin", "Super Admin"]).ToArray()
            : StandardStaffRoles;
        if (!allowedRoleNames.Contains(roleName, StringComparer.OrdinalIgnoreCase))
            throw new ForbiddenException("You cannot create an account with the selected role.");

        var role = await dbContext.Roles
            .SingleOrDefaultAsync(candidate => candidate.Name.ToLower() == roleName.ToLower(), cancellationToken);
        if (role is null)
            throw new RequestValidationException("The selected role is not available.");

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

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new StaffAccountDto(user.Id, user.Email, user.FirstName, user.LastName, role.Name, user.IsActive);
    }
}