namespace ShiftSwap.Models
{
    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        public ICollection<Location> Locations { get; set; } = new List<Location>();
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
