using System.ComponentModel.DataAnnotations;

namespace FinalProjectDSS.DTOs
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [MaxLength(254)]
        
        
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [MaxLength(128)]

        public string Password { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
    }
}
