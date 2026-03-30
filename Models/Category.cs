using System.ComponentModel.DataAnnotations;

namespace HORIZON1.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Назва категорії обов'язкова")]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(7)]
        public string ColorHex { get; set; } = "#FFFFFF";

        public string? UserId { get; set; } // null for system categories, userId for user-created categories
        public User? User { get; set; }

        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}