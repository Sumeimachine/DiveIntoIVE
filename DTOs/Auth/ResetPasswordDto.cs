using System.ComponentModel.DataAnnotations;

namespace DiveIntoIVE.DTOs.Auth
{
    public class ResetPasswordDto
    {
        [Required]
        public string Token { get; set; } = default!;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = default!;
    }
}