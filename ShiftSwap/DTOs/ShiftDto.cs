namespace ShiftSwap.DTOs
{
    public class ShiftDto
    {
        public int Id { get; set; }
        public DateTime ShiftDate { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public string Status { get; set; } = null!;
        public string LocationName { get; set; } = null!;
    }
}
