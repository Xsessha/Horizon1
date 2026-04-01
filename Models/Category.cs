using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

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

        [JsonIgnore]
        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}