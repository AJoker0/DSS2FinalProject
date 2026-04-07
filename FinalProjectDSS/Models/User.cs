namespace FinalProjectDSS.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public DateTime CreatedAt { get; set; }

        // navigation property for communication with TodoItem
        public ICollection<TodoItem> Todos { get; set; } = new List<TodoItem>();
    }
}