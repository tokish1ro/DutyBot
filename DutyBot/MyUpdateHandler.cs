using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups; // ВАЖНЫЙ ИМПОРТ ДЛЯ КНОПОК

public class MyUpdateHandler : IUpdateHandler
{
    private readonly DutyManager _dutyManager = new();

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Обработка сообщений (текст)
        if (update.Message?.Text != null)
        {
            await HandleMessageAsync(botClient, update.Message, cancellationToken);
        }
        
        // Обработка нажатий на кнопки (CallbackQuery)
        else if (update.CallbackQuery != null)
        {
            await HandleCallbackAsync(botClient, update.CallbackQuery, cancellationToken);
        }
    }

    private async Task HandleMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken ct)
    {
        string text = message.Text!.ToLower().Trim();
        long chatId = message.Chat.Id;
        
        string response = "";
        ReplyMarkup? markup = null; // Переменная для кнопок

        // --- КОМАНДА START ---
        if (text.StartsWith("/start"))
        {
            response = "<b>👋 Привет!</b>\nЯ бот дежурств.\n\n" +
                       "• /add Фамилия — добавить человека\n" +
                       "• /list — посмотреть список\n" +
                       "• /today — кто дежурит сейчас (+ кнопки)\n" +
                       "• /remove № — удалить по номеру\n" +
                       "• /move №1 №2 — переместить\n" +
                       "• /stats — статистика";
        }
        
        // --- КОМАНDA TODAY (С КНОПКАМИ!) ---
        else if (text.StartsWith("/today"))
        {
            response = _dutyManager.GetTodayText();
            
            // Создаем клавиатуру
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new []
                {
                    InlineKeyboardButton.WithCallbackData("⬅️ Назад", "go_back"),
                    InlineKeyboardButton.WithCallbackData("➡️ Пропустить", "skip_forward")
                }
            });
            markup = keyboard;
        }
        
        // --- ДРУГИЕ КОМАНДЫ ---
        else if (text.StartsWith("/add "))
        {
            string name = text.Substring(5).Trim();
            response = _dutyManager.AddStudent(name);
        }
        else if (text.StartsWith("/remove "))
        {
            if (int.TryParse(text.Substring(8).Trim(), out int num))
                response = _dutyManager.RemoveStudent(num);
            else
                response = "❌ Введи число после /remove (например: /remove 3)";
        }
        else if (text.StartsWith("/move "))
        {
            var parts = text.Split(' ');
            if (parts.Length >= 3 && int.TryParse(parts[1], out int from) && int.TryParse(parts[2], out int to))
                response = _dutyManager.MoveStudent(from, to);
            else
                response = "❌ Формат: /move ОТ КУДА (числа)";
        }
        else if (text.StartsWith("/list"))
        {
            response = _dutyManager.GetList();
        }
        else if (text.StartsWith("/stats"))
        {
            response = _dutyManager.GetStats();
        }
        else
        {
            response = "❓ Не знаю такую команду. Попробуй /start";
        }

        // Отправка ответа
        await botClient.SendTextMessageAsync(
            chatId, 
            response, 
            parseMode: ParseMode.Html, 
            replyMarkup: markup, // Передаем кнопки, если они есть
            cancellationToken: ct
        );
    }

    private async Task HandleCallbackAsync(ITelegramBotClient botClient, CallbackQuery query, CancellationToken ct)
    {
        string action = query.Data;
        long chatId = query.Message!.Chat.Id;
        int messageId = query.Message.MessageId;
        
        string newText = "";

        switch (action)
        {
            case "go_back":
                newText = _dutyManager.GoBack();
                break;
                
            case "skip_forward":
                newText = _dutyManager.SkipForward();
                break;
                
            default:
                newText = "Неизвестное действие.";
                break;
        }

        // Редактируем сообщение прямо в чате (чтобы не спамить новыми сообщениями)
        await botClient.EditMessageTextAsync(
            chatId, 
            messageId, 
            newText, 
            parseMode: ParseMode.Html,
            replyMarkup: query.Message.ReplyMarkup, // Сохраняем те же кнопки
            cancellationToken: ct
        );

        // Обязательно отвечаем на callback, чтобы убрать "часики" у пользователя
        await botClient.AnswerCallbackQueryAsync(query.Id, cancellationToken: ct);
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Ошибка: {exception.Message}");
        return Task.CompletedTask;
    }
}