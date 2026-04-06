using System.ComponentModel.DataAnnotations;


namespace FinalProjectDSS.DTOs
{
    public class CreateTodoRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Details { get; set; }

        [Required]
        [RegularExpression("low|medium|high", ErrorMessage = "Priority must be low, medium, or high")]
        public string Priority { get; set; } = "medium";

        // simple validation for date format (yyyy-MM-dd)
        [RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "DueDate must be YYYY-MM-DD")]
        public string? DueDate { get; set; }

        public bool IsPublic { get; set; }
    }
}
