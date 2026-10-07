using System.ComponentModel.DataAnnotations;
using HospitalManagement.Api.Models.Enums;

namespace HospitalManagement.Api.DTOs;

public class CreatePatientRequest
{
    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public DateOnly? DateOfBirth { get; set; }

    public Gender Gender { get; set; }

    [Required, StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress, StringLength(256)]
    public string? Email { get; set; }

    [Required, StringLength(300)]
    public string Address { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string State { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string EmergencyContactName { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string EmergencyContactPhone { get; set; } = string.Empty;
}

public record PatientListItemDto(
    Guid Id,
    string PatientNumber,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    Gender Gender,
    string Phone,
    string? Email,
    string City,
    string State,
    PatientStatus Status,
    DateTime RegisteredAt);

public record PatientPageDto(
    IReadOnlyList<PatientListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);