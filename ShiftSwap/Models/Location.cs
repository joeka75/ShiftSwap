namespace ShiftSwap.Models
{
    public class Location
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string Name { get; set; } = null!;
        public string? Address { get; set; }

        public Company Company { get; set; } = null!;

        public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
    }
}
