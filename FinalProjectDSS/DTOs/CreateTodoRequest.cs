using System.ComponentModel.DataAnnotations;

namespace FinalProjectDSS.DTOs
{
    // DTO for creating a new todo item (request body)
    public class CreateTodoRequest
    {
        // Title of the todo (required, 3-100 characters)
        [Required, StringLength(100, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        // Optional details/description (max 1000 characters)
        [MaxLength(1000)]
        public string? Details { get; set; }

        // Priority: must be "low", "medium", or "high" (required)
        [Required, RegularExpression("^(low|medium|high)$")]
        public string Priority { get; set; } = "medium";

        // Optional due date in YYYY-MM-DD format
        [RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "Format must be YYYY-MM-DD")]
        public string? DueDate { get; set; }

        // Whether the todo is public (visible to everyone)
        public bool IsPublic { get; set; }
    }
}