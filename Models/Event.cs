using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace HORIZON1.Models
{
    // МОДЕЛЬ ПОДІЇ: Описує структуру таблиці подій у базі даних
    public class Event
    {
        [Key] // Первинний ключ
        public int Id { get; set; }

        // Валідація: Назва обов'язкова і має обмеження по довжині
        [Required(ErrorMessage = "Назва події обов'язкова")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Назва має бути від 3 до 100 символів")]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; } // Опис події (необов'язковий)

        [Required(ErrorMessage = "Час початку обов'язковий")]
        public DateTime StartTime { get; set; } // Дата і час початку

        [Required(ErrorMessage = "Час закінчення обов'язковий")]
        public DateTime EndTime { get; set; } // Дата і час завершення

        // ПРАПОРЦІ СТАНУ
        public bool IsRecurring { get; set; } // Чи є подія повторюваною
        public bool IsDeleted { get; set; } = false; // "М'яке видалення" (Soft Delete)

        // ЗВ'ЯЗОК З КОРИСТУВАЧЕМ
        [ValidateNever] // Атрибут каже валідатору ігнорувати це поле при перевірці форми
        public string UserId { get; set; } = string.Empty; // ID власника події
        public User? User { get; set; } // Навігаційна властивість (об'єкт користувача)
        
        // ЗВ'ЯЗОК З КАТЕГОРІЄЮ
        public int? CategoryId { get; set; } // Зовнішній ключ (null, якщо категорія тимчасова)
        public Category? Category { get; set; } // Об'єкт постійної категорії

        // ЛОГІКА ТИМЧАСОВИХ КАТЕГОРІЙ (Custom Categories)
        // Потрібна, якщо користувач хоче задати колір/назву лише для цієї події
        public bool IsTemporaryCategory { get; set; } = false;
        [StringLength(50)]
        public string? TemporaryCategoryName { get; set; }
        [StringLength(7)]
        public string? TemporaryCategoryColor { get; set; }

        // ЛОГІКА ПОВТОРЕНЬ (Recurrence)
        public RecurrencePattern RecurrencePattern { get; set; } = RecurrencePattern.None; // Тип (щодня, щотижня і т.д.)
        public DateTime? RecurrenceEndDate { get; set; } // Дата, коли повторення мають припинитися
        [StringLength(50)]
        public string? RecurrenceDays { get; set; } // Дні тижня (наприклад: "Monday,Friday")

        // ЗВ'ЯЗОК З НАГАДУВАННЯМИ
        [ValidateNever]
        public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
    }
}