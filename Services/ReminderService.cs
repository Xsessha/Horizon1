using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using HORIZON1.Data;
using HORIZON1.Models;
using HORIZON1.Factory;
using HORIZON1.Strategy;

namespace HORIZON1.Services
{
    public class ReminderService : IHostedService, IDisposable
    {
        private Timer? _timer;
        private readonly IServiceScopeFactory _scopeFactory;

        public ReminderService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(CheckReminders, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            return Task.CompletedTask;
        }

        private void CheckReminders(object? state)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var factory = scope.ServiceProvider.GetRequiredService<ReminderFactory>();

            var now = DateTime.Now;
            var reminders = context.Reminders
                .Where(r => r.ReminderTime <= now && !r.IsSent)
                .Include(r => r.Event)
                .ToList();

            foreach (var reminder in reminders)
            {
                if (reminder.Event != null)
                {
                    var strategy = factory.CreateStrategy(reminder.Type.ToString());
                    strategy.SendReminder(reminder.Event, reminder);
                    reminder.IsSent = true;
                }
            }

            context.SaveChanges();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}