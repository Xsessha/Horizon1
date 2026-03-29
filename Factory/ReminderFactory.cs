using HORIZON1.Strategy;

namespace HORIZON1.Factory
{
    public class ReminderFactory
    {
        public IReminderStrategy CreateStrategy(string reminderType)
        {
            return reminderType.ToLower() switch
            {
                "email" => new EmailStrategy(),
                "sms" => new SmsStrategy(),
                "push" => new PushStrategy(),
                _ => throw new ArgumentException($"Тип нагадування '{reminderType}' не підтримується.")
            };
        }
    }
}