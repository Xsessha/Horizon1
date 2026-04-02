using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HORIZON1.Models;

namespace HORIZON1.Data
{
    // IdentityDbContext<User> означає, що база даних вже включає готові таблиці для 
    // користувачів, ролей та логіну (завдяки бібліотеці ASP.NET Core Identity)
    public class AppDbContext : IdentityDbContext<User>
    {
        // Конструктор: передає налаштування (наприклад, рядок підключення до SQLite) у базовий клас
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSet — це представлення таблиць у коді. Кожен DbSet відповідає таблиці в базі даних.
        public DbSet<Event> Events { get; set; } // Таблиця подій
        public DbSet<Category> Categories { get; set; } // Таблиця категорій
        public DbSet<Reminder> Reminders { get; set; } // Таблиця нагадувань

        // Метод OnModelCreating: тут ми прописуємо додаткові правила для бази даних
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Виклик базового методу обов'язковий для коректної роботи Identity (користувачів)
            base.OnModelCreating(modelBuilder);

            // ГЛОБАЛЬНИЙ ФІЛЬТР: "М'яке видалення"
            // Всі запити до таблиці Events автоматично будуть ігнорувати записи, де IsDeleted = true.
            // Це дозволяє не видаляти дані назавжди, а просто приховувати їх.
            modelBuilder.Entity<Event>()
                .HasQueryFilter(e => !e.IsDeleted);

            // НАЛАШТУВАННЯ ЗВ'ЯЗКІВ (Relationship)
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Category)           // У кожної події є одна категорія
                .WithMany(c => c.Events)           // У однієї категорії може бути багато подій
                .HasForeignKey(e => e.CategoryId)  // Зовнішній ключ для зв'язку
                .IsRequired(false)                 // Категорія не обов'язкова (може бути null)
                .OnDelete(DeleteBehavior.SetNull); // Якщо категорію видалити, у події CategoryId стане null

            // SEED DATA (Початкові дані): 
            // Ці категорії будуть автоматично додані в базу при першій міграції.
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Робота", ColorHex = "#FF5733" },
                new Category { Id = 2, Name = "Навчання", ColorHex = "#33FF57" },
                new Category { Id = 3, Name = "Спорт", ColorHex = "#3357FF" },
                new Category { Id = 4, Name = "Особисте", ColorHex = "#F1C40F" },
                new Category { Id = 5, Name = "Здоров'я", ColorHex = "#9B59B6" }
            );
        }
    }
}