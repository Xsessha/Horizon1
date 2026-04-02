using HORIZON1.Models;
using HORIZON1.Services;

namespace HORIZON1.Strategy // Перевір, щоб тут було саме HORIZON1.Strategy
{
    public class TelegramStrategy : IReminderStrategy
    {
        private readonly TelegramBotService _botService;

        public TelegramStrategy(TelegramBotService botService)
        {
            _botService = botService;
        }

        public async void SendReminder(Event ev, Reminder reminder)
        {
            if (ev.User?.TelegramChatId != null)
            {
                await _botService.SendReminderAsync(ev.User.TelegramChatId.Value, reminder.Message);
            }
        }
    }
}