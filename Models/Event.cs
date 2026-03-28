using System.ComponentModel.DataAnnotations;

namespace HORIZON1.Models
{
    public class Event
    {
        public int Id { get; set; }
        [Required, StringLength(100, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsRecurring { get; set; }
        public bool IsDeleted { get; set; } // Для Soft Delete

        public string UserId { get; set; } = string.Empty;
        public User? User { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }
    }
}