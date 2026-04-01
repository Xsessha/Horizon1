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
                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var factory = scope.ServiceProvider.GetRequiredService<ReminderFactory>();

                        var now = DateTime.Now;

                        // Шукаємо нагадування, час яких настав
                        var pendingReminders = await context.Reminders
                            .Include(r => r.Event)
                                .ThenInclude(e => e!.User) // Захист від null для події
                            .Where(r => r.ReminderTime <= now)
                            .ToListAsync(stoppingToken);

                        if (pendingReminders.Any())
                        {
                            _logger.LogInformation($"Знайдено нагадувань до відправки: {pendingReminders.Count}");
                        }

                        foreach (var reminder in pendingReminders)
                        {
                            try 
                            {
                                // Перевірка чи подія та користувач існують
                                if (reminder.Event?.User == null)
                                {
                                    _logger.LogWarning($"Нагадування {reminder.Id} не має зв'язку з подією або користувачем.");
                                    context.Reminders.Remove(reminder);
                                    continue;
                                }

                                // ВИБІР СТРАТЕГІЇ: Telegram або Email
                                // Якщо у користувача є TelegramChatId, відправляємо в ТГ
                                string strategyType = reminder.Event.User.TelegramChatId.HasValue ? "telegram" : "email";
                                
                                var strategy = factory.CreateStrategy(strategyType); 
                                strategy.SendReminder(reminder.Event, reminder);

                                // Видаляємо нагадування, щоб не відправляти його знову
                                context.Reminders.Remove(reminder);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($"Помилка відправки конкретного нагадування: {ex.Message}");
                            }
                        }

                        if (pendingReminders.Any())
                        {
                            await context.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Критична помилка в циклі служби нагадувань: {ex.Message}");
                }

                // Перевірка кожну хвилину
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}