using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;


namespace HORIZON1.Models
{
    public class User : IdentityUser
    {
        // Додаткові дані користувача
        public string FullName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Поле для зв'язку з Telegram (ID чату)
        public long? TelegramChatId { get; set; }
        
        // Зв'язок із подіями (один користувач має багато подій)
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    }
}