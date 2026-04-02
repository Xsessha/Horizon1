using System.Net;
using System.Net.Mail;
using HORIZON1.Models;

namespace HORIZON1.Strategy
{
    // СТРАТЕГІЯ EMAIL: Конкретна реалізація відправки сповіщень через пошту
    public class EmailStrategy : IReminderStrategy
    {
        // Основний метод, який викликається фоновою службою
        public void SendReminder(Event eventItem, Reminder reminder)
        {
            try
            {
                // Налаштування SMTP-клієнта для Gmail
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587, // Порт для безпечної передачі пошти
                    // АВТЕНТИФІКАЦІЯ: Твоя пошта та спеціальний "пароль додатка" (App Password)
                    Credentials = new NetworkCredential("horizon666222@gmail.com", "bjvj zbvc xkpj wvhf"), 
                    EnableSsl = true, // Вмикаємо шифрування (SSL/TLS)
                };

                // ФОРМУВАННЯ ЛИСТА
                var mailMessage = new MailMessage
                {
                    From = new MailAddress("horizon666222@gmail.com"), // Від кого (має збігатися з логіном)
                    Subject = $"HORIZON: {eventItem.Title}", // Тема листа: назва події
                    Body = reminder.Message, // Текст нагадування
                    IsBodyHtml = false, // Відправляємо як звичайний текст, а не HTML-код
                };

                // ВІДПРАВКА: Тільки якщо у події є прив'язаний користувач з Email
                if (eventItem.User != null && !string.IsNullOrEmpty(eventItem.User.Email))
                {
                    mailMessage.To.Add(eventItem.User.Email); // Кому відправляємо
                    smtpClient.Send(mailMessage); // Сам процес відправки

                    // Логування в консоль сервера для контролю роботи
                    System.Console.WriteLine($"[SMTP] Лист успішно надіслано на {eventItem.User.Email}");
                }
            }
            catch (System.Exception ex)
            {
                // Якщо сервер Google відхилив запит або немає інтернету — записуємо помилку
                System.Console.WriteLine($"[SMTP ERROR] Помилка: {ex.Message}");
            }
        }
    }
}