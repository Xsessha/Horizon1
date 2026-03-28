using HORIZON1.Models;
using System;

namespace HORIZON1.Strategy
{
    public class PushStrategy : IReminderStrategy
    {
        public void SendReminder(Event eventItem, Reminder reminder)
        {
            // Імітація Push-повідомлення
            Console.WriteLine("==================================================");
            Console.WriteLine($"[PUSH MESSAGE] Нове сповіщення на екрані!");
            Console.WriteLine($"Заголовок: {eventItem.Title}");
            Console.WriteLine($"Текст: {reminder.Message}");
            Console.WriteLine("==================================================");
        }
    }
}