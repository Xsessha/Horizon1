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

        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        public bool IsTemporaryCategory { get; set; } = false;
        [StringLength(50)]
        public string? TemporaryCategoryName { get; set; }
        [StringLength(7)]
        public string? TemporaryCategoryColor { get; set; }

        public RecurrencePattern RecurrencePattern { get; set; } = RecurrencePattern.None;
        public DateTime? RecurrenceEndDate { get; set; }
        [StringLength(50)]
        public string? RecurrenceDays { get; set; }

        [ValidateNever]
        public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
    }
}