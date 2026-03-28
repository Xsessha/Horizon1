using HORIZON1.Models;

namespace HORIZON1.Strategy
{
    /// <summary>
    /// Інтерфейс Стратегії для відправки нагадувань.
    /// Кожен конкретний клас (Email, SMS) має реалізувати метод SendReminder.
    /// </summary>
    public interface IReminderStrategy
    {
        void SendReminder(Event eventItem, Reminder reminder);
    }
}