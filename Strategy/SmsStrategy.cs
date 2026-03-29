using HORIZON1.Models;
using System;

namespace HORIZON1.Strategy
{
    public class SmsStrategy : IReminderStrategy
    {
        public void SendReminder(Event eventItem, Reminder reminder)
        {
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine($"[SMS NOTIFICATION] Відправка на телефон...");
            Console.WriteLine($"Подія: {eventItem.Title}");
            Console.WriteLine($"Час: {eventItem.StartTime:HH:mm}");
            Console.WriteLine($"Повідомлення: {reminder.Message}");
            Console.WriteLine("--------------------------------------------------");
        }
    }
}