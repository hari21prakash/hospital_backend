using System.ComponentModel.DataAnnotations;
using HospitalManagement.Api.Models.Enums;

namespace HospitalManagement.Api.DTOs;

public record NotificationDto(
    Guid Id,
    Guid UserId,
    NotificationType Type,
    string Title,
    string Message,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt,
    string? RelatedEntityName,
    Guid? RelatedEntityId);

public record NotificationPageDto(
    IReadOnlyList<NotificationDto> Notifications,
    int TotalCount,
    int Page,
    int PageSize,
    int UnreadCount);

public record AuditLogDto(
    Guid Id,
    DateTime Timestamp,
    string Action,
    string EntityName,
    string? EntityId,
    string Description,
    string? UserEmail,
    string? UserRole,
    string? IpAddress);

public record AuditLogPageDto(
    IReadOnlyList<AuditLogDto> Logs,
    int TotalCount,
    int Page,
    int PageSize);
