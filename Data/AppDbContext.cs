using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using HORIZON1.Models;

namespace HORIZON1.Data
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Event> Events { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Reminder> Reminders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Event>()
                .HasQueryFilter(e => !e.IsDeleted);

            modelBuilder.Entity<Event>()
                .HasOne(e => e.Category)
                .WithMany(c => c.Events)
                .HasForeignKey(e => e.CategoryId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

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