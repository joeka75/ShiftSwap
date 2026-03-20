namespace ShiftSwap.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public int? UserId { get; set; }

        public string Action { get; set; } = null!;      // pl. "SwapCreated"
        public string EntityName { get; set; } = null!;  // pl. "Shift"
        public int? EntityId { get; set; }

        public string? Details { get; set; }

        public User? User { get; set; }
    }
}
