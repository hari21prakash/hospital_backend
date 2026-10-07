using HospitalManagement.Api.DTOs;

namespace HospitalManagement.Api.Interfaces;

public interface IPatientService
{
    Task<PatientPageDto> GetPageAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);
    Task<PatientListItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PatientListItemDto> CreateAsync(CreatePatientRequest request, CancellationToken cancellationToken);
}