using System.ComponentModel.DataAnnotations;

namespace HORIZON1.Models
{
    public class Reminder
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Текст нагадування не може бути порожнім")]
        public string Message { get; set; } = string.Empty;

        [Required]
        public DateTime ReminderTime { get; set; }

        public int EventId { get; set; }
        public Event? Event { get; set; }
    }
}