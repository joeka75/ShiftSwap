using ShiftSwap.Models;
using System.ComponentModel.DataAnnotations;

namespace ShiftSwap.DTOs
{
    public class RegisterRequestDto
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = null!;

        public int? LocationId { get; set; }

        [Required]
        public UserRole Role { get; set; } = UserRole.Worker;
    }
}