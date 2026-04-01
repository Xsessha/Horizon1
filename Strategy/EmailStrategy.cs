using System.Net;
using System.Net.Mail;
using HORIZON1.Models;

namespace HORIZON1.Strategy
{
    public class EmailStrategy : IReminderStrategy
    {
        public void SendReminder(Event eventItem, Reminder reminder)
        {
            try
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    // ВСТАВЛЯЙ СЮДИ: твоя пошта та 16-значний код без пробілів
                    Credentials = new NetworkCredential("horizon666222@gmail.com", "bjvj zbvc xkpj wvhf"), 
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("horizon666222@gmail.com"),
                    Subject = $"HORIZON: {eventItem.Title}",
                    Body = reminder.Message,
                    IsBodyHtml = false,
                };

                // Відправляємо на Email користувача, який створив подію
                if (eventItem.User != null && !string.IsNullOrEmpty(eventItem.User.Email))
                {
                    mailMessage.To.Add(eventItem.User.Email);
                    smtpClient.Send(mailMessage);
                    System.Console.WriteLine($"[SMTP] Лист успішно надіслано на {eventItem.User.Email}");
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[SMTP ERROR] Помилка: {ex.Message}");
            }
        }
    }
}