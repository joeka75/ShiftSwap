namespace ShiftSwap.DTOs
{
    public class SwapRequestDto
    {
        public int Id { get; set; }
        public int ShiftId { get; set; }

        public string ShiftInfo { get; set; } = null!;
        public string FromUserName { get; set; } = null!;
        public string? ToUserName { get; set; }
        public string Status { get; set; } = null!;
    }
}
