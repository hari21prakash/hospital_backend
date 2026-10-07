using HospitalManagement.Api.DTOs;

namespace HospitalManagement.Api.Interfaces;

public interface ILaboratoryService
{
    Task<LaboratoryPageDto> GetPageAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);
    Task<LaboratoryLookupDto> GetLookupAsync(CancellationToken cancellationToken);
    Task<LabOrderDetailDto> CreateAsync(CreateLabOrderRequest request, CancellationToken cancellationToken);
}
