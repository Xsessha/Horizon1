using HORIZON1.Strategy;
using HORIZON1.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HORIZON1.Factory
{
    public class ReminderFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ReminderFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IReminderStrategy CreateStrategy(string reminderType)
        {
            return reminderType.ToLower() switch
            {
                "email" => new EmailStrategy(),
                // Додаємо підтримку Telegram
                "telegram" => new TelegramStrategy(_serviceProvider.GetRequiredService<TelegramBotService>()),
                
                "sms" => new SmsStrategy(),
                "push" => new PushStrategy(),
                _ => throw new ArgumentException($"Тип нагадування '{reminderType}' не підтримується.")
            };
        }
    }
}