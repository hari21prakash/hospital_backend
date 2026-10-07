using HospitalManagement.Api.Data;
using HospitalManagement.Api.DTOs;
using HospitalManagement.Api.Exceptions;
using HospitalManagement.Api.Interfaces;
using HospitalManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HospitalManagement.Api.Services;

public class NotificationService(AppDbContext dbContext) : INotificationService
{
    public async Task<NotificationPageDto> GetNotificationsAsync(Guid userId, int page, int pageSize, bool unreadOnly, CancellationToken cancellationToken)
    {
        var query = dbContext.Notifications
            .AsNoTracking()
            .Where(item => item.UserId == userId);

        if (unreadOnly)
            query = query.Where(item => !item.IsRead);

        var totalCount = await query.CountAsync(cancellationToken);
        var unreadCount = await dbContext.Notifications.CountAsync(item => item.UserId == userId && !item.IsRead, cancellationToken);

        var notifications = await query
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new NotificationDto(
                item.Id,
                item.UserId,
                item.Type,
                item.Title,
                item.Message,
                item.IsRead,
                item.ReadAt,
                item.CreatedAt,
                item.RelatedEntityName,
                item.RelatedEntityId))
            .ToListAsync(cancellationToken);

        return new NotificationPageDto(notifications, totalCount, page, pageSize, unreadCount);
    }

    public async Task<NotificationDto> MarkNotificationAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken)
    {
        var notification = await dbContext.Notifications
            .SingleOrDefaultAsync(item => item.Id == notificationId && item.UserId == userId, cancellationToken);

        if (notification is null)
            throw new NotFoundException(nameof(Notification), notificationId);

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new NotificationDto(
            notification.Id,
            notification.UserId,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.IsRead,
            notification.ReadAt,
            notification.CreatedAt,
            notification.RelatedEntityName,
            notification.RelatedEntityId);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Notifications.CountAsync(item => item.UserId == userId && !item.IsRead, cancellationToken);
    }

    public async Task<AuditLogPageDto> GetAuditLogsAsync(string? search, string? entityName, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.AuditLogs
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(item =>
                item.Action.Contains(term) ||
                item.Description.Contains(term) ||
                item.EntityName.Contains(term) ||
                item.UserEmail != null && item.UserEmail.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(item => item.EntityName == entityName);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var logs = await query
            .OrderByDescending(item => item.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new AuditLogDto(
                item.Id,
                item.Timestamp,
                item.Action,
                item.EntityName,
                item.EntityId,
                item.Description,
                item.UserEmail,
                item.UserRole,
                item.IpAddress))
            .ToListAsync(cancellationToken);

        return new AuditLogPageDto(logs, totalCount, page, pageSize);
    }
}
