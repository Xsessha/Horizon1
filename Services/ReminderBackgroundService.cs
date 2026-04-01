using HORIZON1.Data;
using HORIZON1.Factory;
using Microsoft.EntityFrameworkCore;

namespace HORIZON1.Services
{
    public class ReminderBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<ReminderBackgroundService> _logger;

        public ReminderBackgroundService(IServiceProvider services, ILogger<ReminderBackgroundService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Служба нагадувань запущена.");

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var factory = scope.ServiceProvider.GetRequiredService<HORIZON1.Factory.ReminderFactory>();

                    var now = DateTime.Now;

                    // Шукаємо нагадування, час яких настав
                    var pendingReminders = await context.Reminders
                        .Include(r => r.Event)
                        .ThenInclude(e => e.User)
                        .Where(r => r.ReminderTime <= now)
                        .ToListAsync();

                    _logger.LogInformation($"Перевірка... Знайдено нагадувань: {pendingReminders.Count}");

                    foreach (var reminder in pendingReminders)
                    {
                        try 
                        {
                            // Додаємо перевірку: якщо події немає, пропускаємо це нагадування
                            if (reminder.Event == null) continue;

                            var strategy = factory.CreateStrategy("email"); 
                            strategy.SendReminder(reminder.Event, reminder);

                            context.Reminders.Remove(reminder);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Помилка: {ex.Message}");
                        }
                    }

                    if (pendingReminders.Any())
                    {
                        await context.SaveChangesAsync();
                    }
                }

                // Перевірка кожну хвилину
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}