using HORIZON1.Strategy;

namespace HORIZON1.Factory
{
    // КЛАС-ФАБРИКА: Відповідає за створення об'єктів різних стратегій сповіщення
    public class ReminderFactory
    {
        // МЕТОД-ФАБРИКА: Повертає потрібну стратегію залежно від вхідного рядка (email, sms тощо)
        // Тип повернення — інтерфейс IReminderStrategy, що робить код універсальним
        public IReminderStrategy CreateStrategy(string reminderType)
        {
            // Використовуємо switch-вираз для вибору конкретної реалізації
            return reminderType.ToLower() switch
            {
                "email" => new EmailStrategy(),
                "sms" => new SmsStrategy(),
                "push" => new PushStrategy(),
                // Обробка помилки: якщо передано невідомий тип, програма видасть виключення
                _ => throw new ArgumentException($"Тип нагадування '{reminderType}' не підтримується.")
            };
        }
    }
}