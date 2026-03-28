using HORIZON1.Models;

namespace HORIZON1.Strategy
{
    public class EmailStrategy : IReminderStrategy
    {
        public void SendReminder(Event eventItem, Reminder reminder)
        {
            System.Console.WriteLine($"[EMAIL] Надіслано для: {eventItem.Title}");
        }
    }
}