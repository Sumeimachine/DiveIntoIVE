using System.ComponentModel.DataAnnotations;

namespace DiveIntoIVE.DTOs.Auth
{
    public class LoginDto
    {
        [Required]
        public string Username { get; set; } = default!;

        [Required]
        public string Password { get; set; } = default!;
    }
}