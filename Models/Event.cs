using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace HORIZON1.Models
{
    public class Event
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Назва події обов'язкова")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Назва має бути від 3 до 100 символів")]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "Час початку обов'язковий")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Час закінчення обов'язковий")]
        public DateTime EndTime { get; set; }

        public bool IsRecurring { get; set; }
        
        public bool IsDeleted { get; set; } = false;

        [ValidateNever]
        public string UserId { get; set; } = string.Empty;
        public User? User { get; set; }

        [Required]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
    }
}