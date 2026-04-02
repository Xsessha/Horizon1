using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HORIZON1.Models
{
    // МОДЕЛЬ КАТЕГОРІЇ: Описує структуру таблиці в БД
    public class Category
    {
        // [Key] — вказує Entity Framework, що це поле є первинним ключем (Primary Key)
        [Key]
        public int Id { get; set; }

        // [Required] — робить поле обов'язковим для заповнення (NOT NULL у базі)
        // ErrorMessage — текст, який побачить користувач, якщо залишить поле пустим
        [Required(ErrorMessage = "Назва категорії обов'язкова")]
        [StringLength(50)] // Обмеження довжини назви до 50 символів
        public string Name { get; set; } = string.Empty;

        [StringLength(7)]
        public string ColorHex { get; set; } = "#FFFFFF";

        // [JsonIgnore] — ДУЖЕ ВАЖЛИВО: каже серверу НЕ включати список подій у відповідь JSON.
        // Це запобігає "циклічним посиланням" (коли категорія тягне події, а події знову категорію)
        [JsonIgnore]
        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}