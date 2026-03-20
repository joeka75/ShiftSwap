namespace ShiftSwap.Models
{
    public enum UserRole
    {
        Admin = 0,
        Manager = 1,
        Worker = 2
    }

    public class User
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int? LocationId { get; set; }

        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;

        // Egyelőre plain string, később lehet rendes hash.
        public string PasswordHash { get; set; } = null!;

        public UserRole Role { get; set; } = UserRole.Worker;
        public bool IsActive { get; set; } = true;

        public Company Company { get; set; } = null!;
        public Location? Location { get; set; }

        public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
    }
}
