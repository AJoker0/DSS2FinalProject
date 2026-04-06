using System;
using System.Collections.Generic;

namespace FinalProjectDSS.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // one user can have several tasks
        public ICollection<TodoItem> Todos { get; set; } = new List<TodoItem>();
    }
}

