using HORIZON1.Data;
using HORIZON1.Models;
using HORIZON1.Strategy;
using Microsoft.EntityFrameworkCore;

namespace HORIZON1.Services
{
    public class ReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReminderService> _logger;

        public ReminderService(IServiceProvider serviceProvider, ILogger<ReminderService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndSendReminders();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while checking reminders");
                }

                // Check every minute
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task CheckAndSendReminders()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.UtcNow;
            var upcomingReminders = await context.Reminders
                .Include(r => r.Event)
                .ThenInclude(e => e.Category)
                .Where(r => !r.IsSent && r.ReminderTime <= now.AddMinutes(1) && r.ReminderTime >= now)
                .ToListAsync();

            foreach (var reminder in upcomingReminders)
            {
                try
                {
                    IReminderStrategy strategy = reminder.Type switch
                    {
                        ReminderType.Email => new EmailStrategy(),
                        ReminderType.SMS => new SmsStrategy(),
                        ReminderType.Push => new PushStrategy(),
                        _ => new EmailStrategy()
                    };

                    var eventItem = reminder.Event;
                    if (eventItem != null)
                    {
                        strategy.SendReminder(eventItem, reminder);
                        reminder.IsSent = true;
                        await context.SaveChangesAsync();

                        _logger.LogInformation($"Reminder sent for event: {eventItem.Title}");
                    }
                    else
                    {
                        _logger.LogWarning($"Reminder {reminder.Id} has no associated event");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send reminder for event {reminder.EventId}");
                }
            }
        }
    }
}