using HospitalManagement.Api.DTOs;

namespace HospitalManagement.Api.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentPageDto> GetPageAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);
    Task<AppointmentLookupDto> GetLookupAsync(CancellationToken cancellationToken);
    Task<AppointmentListItemDto> CreateAsync(CreateAppointmentRequest request, Guid createdByUserId, CancellationToken cancellationToken);
}
