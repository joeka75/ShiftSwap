namespace ShiftSwap.Dtos
{
    public class ShiftFilterDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public int? LocationId { get; set; }  // ha null, akkor a saját location
    }
}
