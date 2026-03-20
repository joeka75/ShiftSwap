using System.ComponentModel.DataAnnotations;
using ShiftSwap.Models;

namespace ShiftSwap.Dtos
{
    public class UpdateShiftDto
    {
        public int? UserId { get; set; }

        public DateTime? StartDateTime { get; set; }

        public DateTime? EndDateTime { get; set; }

        public ShiftStatus? Status { get; set; }
    }
}
