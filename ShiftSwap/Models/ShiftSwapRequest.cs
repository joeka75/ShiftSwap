namespace ShiftSwap.Models
{
    public enum SwapRequestStatus
    {
        Open = 0,
        Accepted = 1,
        Rejected = 2,
        Cancelled = 3,
        ApprovedByManager = 4
    }

    public class ShiftSwapRequest
    {
        public int Id { get; set; }

        public int ShiftId { get; set; }
        public int FromUserId { get; set; }
        public int? ToUserId { get; set; }

        public SwapRequestStatus Status { get; set; } = SwapRequestStatus.Open;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public Shift Shift { get; set; } = null!;
        public User FromUser { get; set; } = null!;
        public User? ToUser { get; set; }
    }
}
