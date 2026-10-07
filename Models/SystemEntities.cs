using HospitalManagement.Api.Models.Enums;

namespace HospitalManagement.Api.Models;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public NotificationType Type { get; set; } = NotificationType.General;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    /// <summary>Optional pointer to the related record (e.g. "Appointment" + its id) for deep links.</summary>
    public string? RelatedEntityName { get; set; }
    public Guid? RelatedEntityId { get; set; }
}

/// <summary>Append-only audit trail. Not a BaseEntity: rows are never updated.</summary>
public class AuditLog
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }

    /// <summary>Null for anonymous or system actions. Kept (set null) if the user is ever deleted.</summary>
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    // Snapshots so the trail stays readable even if the user changes later.
    public string? UserEmail { get; set; }
    public string? UserRole { get; set; }

    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
}
