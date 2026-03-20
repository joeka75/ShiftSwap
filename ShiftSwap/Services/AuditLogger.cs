using ShiftSwap.Data;
using ShiftSwap.Models;

namespace ShiftSwap.Services
{
    public class AuditLogger : IAuditLogger
    {
        private readonly AppDbContext _db;

        public AuditLogger(AppDbContext db)
        {
            _db = db;
        }

        public async Task LogAsync(int? userId, string action, string entityName, int? entityId, string? details = null)
        {
            var log = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Details = details,
                Timestamp = DateTime.UtcNow
            };

            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync();
        }
    }
}
