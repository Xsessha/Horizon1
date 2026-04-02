using System.ComponentModel.DataAnnotations;

namespace HORIZON1.Models
{
    // МОДЕЛЬ НАГАДУВАННЯ: Описує структуру таблиці для запланованих сповіщень
    public class Reminder
    {
        // [Key] — Вказує Entity Framework, що це унікальний ідентифікатор запису (Primary Key)
        [Key]
        public int Id { get; set; }

        // [Required] — Гарантує, що нагадування обов'язково матиме текст (напр. "Подія скоро почнеться")
        [Required(ErrorMessage = "Текст нагадування не може бути порожнім")]
        public string Message { get; set; } = string.Empty;

        // [Required] — Точний час, коли фонова служба повинна відправити це сповіщення
        [Required]
        public DateTime ReminderTime { get; set; }

        // ЗВ'ЯЗОК З ПОДІЄЮ:
        // EventId — це зовнішній ключ (Foreign Key), який каже, до якої саме події відноситься це нагадування
        public int EventId { get; set; }

        // Навігаційна властивість: дозволяє легко отримати всі дані про подію (напр. її назву), 
        // знаючи лише об'єкт нагадування
        public Event? Event { get; set; }
    }
}