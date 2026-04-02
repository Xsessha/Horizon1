using HORIZON1.Data;
using HORIZON1.Factory;
using Microsoft.EntityFrameworkCore;

namespace HORIZON1.Services
{
    // BackgroundService — це спеціальний клас в ASP.NET Core для створення завдань, 
    // що працюють у фоновому режимі (Worker Service).
    public class ReminderBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<ReminderBackgroundService> _logger; // Для запису подій у консоль

        public ReminderBackgroundService(IServiceProvider services, ILogger<ReminderBackgroundService> logger)
        {
            _services = services;
            _logger = logger;
        }

        // Основний метод, який запускається автоматично при старті сервера
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Служба нагадувань запущена.");

            // Нескінченний цикл, поки сервер працює (stoppingToken відстежує зупинку сервера)
            while (!stoppingToken.IsCancellationRequested)
            {
                // Створюємо Scope, бо DbContext — це Scoped-сервіс, і ми не можемо 
                // звернутися до нього напряму з фонової служби, яка живе довго.
                using (var scope = _services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var factory = scope.ServiceProvider.GetRequiredService<HORIZON1.Factory.ReminderFactory>();

                    var now = DateTime.Now;

                    // КРОК 1: Шукаємо в базі нагадування, час яких менше або дорівнює поточному
                    var pendingReminders = await context.Reminders
                        .Include(r => r.Event) // Підтягуємо дані про саму подію
                        .ThenInclude(e => e.User) // Підтягуємо дані про користувача (щоб знати Email)
                        .Where(r => r.ReminderTime <= now)
                        .ToListAsync();

                    _logger.LogInformation($"Перевірка... Знайдено нагадувань: {pendingReminders.Count}");

                    // КРОК 2: Обробляємо кожне знайдене нагадування
                    foreach (var reminder in pendingReminders)
                    {
                        try 
                        {
                            // Додаємо перевірку: якщо події немає, пропускаємо це нагадування
                            if (reminder.Event == null) continue;

                            // Використовуємо ФАБРИКУ для створення стратегії (наприклад, Email)
                            var strategy = factory.CreateStrategy("email"); 
                            strategy.SendReminder(reminder.Event, reminder);

                            // КРОК 3: Після відправки видаляємо нагадування, щоб не слати його двічі
                            context.Reminders.Remove(reminder);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Помилка: {ex.Message}");
                        }
                    }

                    // Зберігаємо зміни в БД (видалення відпрацьованих нагадувань)
                    if (pendingReminders.Any())
                    {
                        await context.SaveChangesAsync();
                    }
                }

                // КРОК 4: Засинаємо на 1 хвилину, щоб не перевантажувати процесор постійними запитами
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}