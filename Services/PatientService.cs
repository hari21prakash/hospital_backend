using HospitalManagement.Api.Data;
using HospitalManagement.Api.DTOs;
using HospitalManagement.Api.Interfaces;
using HospitalManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Api.Services;

public class PatientService(AppDbContext dbContext) : IPatientService
{
    public async Task<PatientPageDto> GetPageAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Patients.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(patient =>
                patient.FirstName.Contains(term) ||
                patient.LastName.Contains(term) ||
                patient.PatientNumber.Contains(term) ||
                patient.Phone.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(patient => patient.RegisteredAt)
            .ThenBy(patient => patient.LastName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(patient => new PatientListItemDto(
                patient.Id,
                patient.PatientNumber,
                patient.FirstName,
                patient.LastName,
                patient.DateOfBirth,
                patient.Gender,
                patient.Phone,
                patient.Email,
                patient.City,
                patient.State,
                patient.Status,
                patient.RegisteredAt))
            .ToListAsync(cancellationToken);

        return new PatientPageDto(items, totalCount, page, pageSize);
    }

    public async Task<PatientListItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Patients
            .AsNoTracking()
            .Where(patient => patient.Id == id)
            .Select(patient => new PatientListItemDto(
                patient.Id,
                patient.PatientNumber,
                patient.FirstName,
                patient.LastName,
                patient.DateOfBirth,
                patient.Gender,
                patient.Phone,
                patient.Email,
                patient.City,
                patient.State,
                patient.Status,
                patient.RegisteredAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PatientListItemDto> CreateAsync(
        CreatePatientRequest request,
        CancellationToken cancellationToken)
    {
        var sequenceNumber = await dbContext.Database
            .SqlQueryRaw<long>($"SELECT nextval('{DbSequences.PatientNumber}') AS \"Value\"")
            .SingleAsync(cancellationToken);

        var patient = new Patient
        {
            PatientNumber = $"PAT-{DateTime.UtcNow.Year}-{sequenceNumber:D6}",
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            DateOfBirth = request.DateOfBirth!.Value,
            Gender = request.Gender,
            Phone = request.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Address = request.Address.Trim(),
            City = request.City.Trim(),
            State = request.State.Trim(),
            EmergencyContactName = request.EmergencyContactName.Trim(),
            EmergencyContactPhone = request.EmergencyContactPhone.Trim(),
            RegisteredAt = DateTime.UtcNow
        };

        dbContext.Patients.Add(patient);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new PatientListItemDto(
            patient.Id,
            patient.PatientNumber,
            patient.FirstName,
            patient.LastName,
            patient.DateOfBirth,
            patient.Gender,
            patient.Phone,
            patient.Email,
            patient.City,
            patient.State,
            patient.Status,
            patient.RegisteredAt);
    }
}