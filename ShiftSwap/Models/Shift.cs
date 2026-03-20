namespace ShiftSwap.Models
{
    public enum ShiftStatus
    {
        Assigned = 0,
        PendingSwap = 1,
        Swapped = 2,
        Cancelled = 3
    }

    public class Shift
    {
        public int Id { get; set; }

        public int LocationId { get; set; }
        public int? UserId { get; set; }  // lehet null, ha még nincs kiosztva

        public DateTime ShiftDate { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }

        public ShiftStatus Status { get; set; } = ShiftStatus.Assigned;
        public bool IsDeleted { get; set; } = false;
        public Location Location { get; set; } = null!;
        public User? User { get; set; }

        public ICollection<ShiftSwapRequest> SwapRequests { get; set; } = new List<ShiftSwapRequest>();
    }
}
