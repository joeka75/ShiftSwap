using System.ComponentModel.DataAnnotations;

namespace ShiftSwap.Dtos
{
    public class CreateSwapRequestDto
    {
        [Required]
        public int ShiftId { get; set; }

        // FromUserId-t már úgyis JWT-ből vesszük, ha még itt van, kivehetjük
    }
}
