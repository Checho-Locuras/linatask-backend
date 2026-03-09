using LinaTask.Domain.DTOs;
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.DataBaseContext;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace LinaTask.Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly LinaTaskDbContext _context;

        public NotificationRepository(LinaTaskDbContext context)
        {
            _context = context;
        }

        // ── GET BY ID ────────────────────────────────────────────────
        public async Task<Notification?> GetByIdAsync(Guid id)
        {
            return await _context.Notifications
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        // ── GET PAGINADO POR USUARIO ─────────────────────────────────
        public async Task<PagedNotificationsDto> GetByUserAsync(Guid userId, NotificationQueryDto query)
        {
            var queryable = _context.Notifications
                .Where(n => n.UserId == userId)
                .AsNoTracking();

            // Aplicar filtros adicionales
            if (query.IsRead.HasValue)
                queryable = queryable.Where(n => n.IsRead == query.IsRead.Value);

            if (!string.IsNullOrEmpty(query.Category))
                queryable = queryable.Where(n => n.Category == query.Category);

            // Obtener total de registros
            var total = await queryable.CountAsync();

            // Obtener datos paginados
            var items = await queryable
                .OrderByDescending(n => n.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedNotificationsDto
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = total,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        // ── SUMMARY (badge campana) ──────────────────────────────────
        public async Task<NotificationSummaryDto> GetSummaryAsync(Guid userId)
        {
            // Contar no leídas
            var unread = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            // Obtener 8 notificaciones más recientes
            var recent = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(8)
                .AsNoTracking()
                .ToListAsync();

            return new NotificationSummaryDto
            {
                UnreadCount = unread,
                Recent = recent.Select(MapToDto).ToList()
            };
        }

        // ── CREATE ───────────────────────────────────────────────────
        public async Task<Notification> CreateAsync(Notification n)
        {
            n.Id = Guid.NewGuid();
            n.CreatedAt = DateTime.UtcNow;
            n.IsRead = false;

            await _context.Notifications.AddAsync(n);
            await _context.SaveChangesAsync();

            return n;
        }

        // ── MARK AS READ ─────────────────────────────────────────────
        public async Task<bool> MarkAsReadAsync(Guid id, Guid userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId && !n.IsRead);

            if (notification == null)
                return false;

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        // ── MARK ALL AS READ ─────────────────────────────────────────
        public async Task<int> MarkAllAsReadAsync(Guid userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (!notifications.Any())
                return 0;

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            return await _context.SaveChangesAsync();
        }

        // ── DELETE ───────────────────────────────────────────────────
        public async Task<bool> DeleteAsync(Guid id, Guid userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null)
                return false;

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
            return true;
        }

        // ── DELETE ALL READ ──────────────────────────────────────────
        public async Task<int> DeleteAllReadAsync(Guid userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.IsRead)
                .ToListAsync();

            if (!notifications.Any())
                return 0;

            _context.Notifications.RemoveRange(notifications);
            return await _context.SaveChangesAsync();
        }

        // ── MÉTODOS ADICIONALES ÚTILES ───────────────────────────────

        /// <summary>
        /// Obtiene notificaciones no leídas de un usuario
        /// </summary>
        public async Task<IEnumerable<Notification>> GetUnreadAsync(Guid userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene el conteo de notificaciones no leídas
        /// </summary>
        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        /// <summary>
        /// Marca múltiples notificaciones como leídas
        /// </summary>
        public async Task<int> MarkMultipleAsReadAsync(IEnumerable<Guid> notificationIds, Guid userId)
        {
            var notifications = await _context.Notifications
                .Where(n => notificationIds.Contains(n.Id) && n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (!notifications.Any())
                return 0;

            var now = DateTime.UtcNow;
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = now;
            }

            return await _context.SaveChangesAsync();
        }

        // ── MAPPER ───────────────────────────────────────────────────
        private static NotificationDto MapToDto(Notification n) => new()
        {
            Id = n.Id,
            UserId = n.UserId,
            Title = n.Title,
            Message = n.Message,
            Type = n.Type,
            Category = n.Category,
            IsRead = n.IsRead,
            ReadAt = n.ReadAt,
            ReferenceId = n.ReferenceId,
            ReferenceType = n.ReferenceType,
            ActionUrl = n.ActionUrl,
            CreatedAt = n.CreatedAt,
            // ← nuevo
            Actions = string.IsNullOrEmpty(n.ActionsJson)
                ? null
                : JsonSerializer.Deserialize<List<NotificationActionDto>>(
                    n.ActionsJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        };
    }
}