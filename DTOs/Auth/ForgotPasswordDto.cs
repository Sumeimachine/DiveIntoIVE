using System.ComponentModel.DataAnnotations;

namespace DiveIntoIVE.DTOs.Auth
{
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;
    }
}