using System.ComponentModel.DataAnnotations;

namespace FinalProjectDSS.DTOs
{
    public class RegisterRequest
    {
        [Required, EmailAddress, MaxLength(254)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(128, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        public string? DisplayName { get; set; }
    }
}