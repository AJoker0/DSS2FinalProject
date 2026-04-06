namespace FinalProjectDSS.DTOs
{
    public class UpdateTodoRequest : CreateTodoRequest
    {
        public bool IsCompleted { get; set; }
    }
}