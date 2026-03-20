using System.ComponentModel.DataAnnotations;

namespace ShiftSwap.Dtos
{
    public class CreateShiftDto
    {
        [Required]
        public int LocationId { get; set; }

        public int? UserId { get; set; }

        [Required]
        public DateTime StartDateTime { get; set; }

        [Required]
        public DateTime EndDateTime { get; set; }
    }
}
