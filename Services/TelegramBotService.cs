using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using HORIZON1.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HORIZON1.Services
{
    public class TelegramBotService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IServiceProvider _serviceProvider;
        // Рекомендується виносити токен у appsettings.json, але поки залишаємо тут
        private readonly string _token = "8653289138:AAGlEjUxRgcbMd3qxJKnUJReBwc3fS5Lb5s";

        public TelegramBotService(IServiceProvider serviceProvider)
        {
            _botClient = new TelegramBotClient(_token);
            _serviceProvider = serviceProvider;
        }

        public void Start()
        {
            using var cts = new CancellationTokenSource();
            
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>() // Отримувати всі типи оновлень
            };

            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                errorHandler: HandleErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            Console.WriteLine("--- [БОТ] Horizon1 успішно запущений ---");
        }

        private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
        {
            // Перевіряємо, чи є повідомлення та текст у ньому
            if (update.Message is not { Text: { } msgText } message) 
                return;

            var chatId = message.Chat.Id;
            var userText = msgText.Trim();

            Console.WriteLine($"[БОТ] Отримано повідомлення: '{userText}' від ChatId: {chatId}");

            // Команда /start
            if (userText.StartsWith("/start", StringComparison.OrdinalIgnoreCase)) 
            {
                await bot.SendMessage(
                    chatId: chatId, 
                    text: "Привіт! Я бот Horizon1. 👋\nБудь ласка, напиши свій Email, щоб підключити сповіщення.", 
                    cancellationToken: ct
                );
                return;
            }

            // Логіка підключення Email
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // Шукаємо користувача за Email
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == userText.ToLower(), ct);

            if (user != null) 
            {
                user.TelegramChatId = chatId;
                await context.SaveChangesAsync(ct);
                
                await bot.SendMessage(
                    chatId: chatId, 
                    text: $"✅ Успіх! Акаунт {user.Email} підключено. Тепер ви отримуватимете нагадування за 15 хвилин до подій.", 
                    cancellationToken: ct
                );
            } 
            else 
            {
                await bot.SendMessage(
                    chatId: chatId, 
                    text: "❌ Користувача з таким Email не знайдено. Перевірте правильність написання та спробуйте ще раз.", 
                    cancellationToken: ct
                );
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
        {
            var errorMessage = ex switch
            {
                Telegram.Bot.Exceptions.ApiRequestException apiEx => $"Telegram API Error:\n[{apiEx.ErrorCode}]\n{apiEx.Message}",
                _ => ex.ToString()
            };

            Console.WriteLine($"[БОТ ПОМИЛКА]: {errorMessage}");
            return Task.CompletedTask;
        }

        public async Task SendReminderAsync(long chatId, string text)
        {
            try
            {
                await _botClient.SendMessage(chatId, text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[БОТ] Не вдалося надіслати нагадування в чат {chatId}: {ex.Message}");
            }
        }
    }
}