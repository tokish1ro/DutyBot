using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

public class MyUpdateHandler : IUpdateHandler
{
    private readonly DutyManager _dutyManager = new();

    public async Task InitializeAsync()
    {
        await _dutyManager.LoadFromDbAsync();
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            var client = (TelegramBotClient)botClient;

            if (update.Message?.Text != null)
            {
                await HandleMessageAsync(client, update.Message, cancellationToken);
            }
            else if (update.CallbackQuery != null)
            {
                await HandleCallbackAsync(client, update.CallbackQuery, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обработки апдейта: {ex}");
        }
    }

    private async Task HandleMessageAsync(TelegramBotClient client, Message message, CancellationToken ct)
    {
        string text = message.Text!.ToLower().Trim();
        ChatId chatId = message.Chat.Id;
        
        string response = "";
        InlineKeyboardMarkup? markup = null;

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
        else if (text.StartsWith("/today"))
        {
            response = _dutyManager.GetTodayText();
            
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
                response = "❌ Введи число после /remove";
        }
        else if (text.StartsWith("/move "))
        {
            var parts = text.Split(' ');
            if (parts.Length >= 3 && int.TryParse(parts[1], out int from) && int.TryParse(parts[2], out int to))
                response = _dutyManager.MoveStudent(from, to);
            else
                response = "❌ Формат: /move ОТ КУДА";
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
            response = "❓ Не знаю такую команду.";
        }

        await client.SendMessage(
            chatId: chatId, 
            text: response, 
            parseMode: ParseMode.Html, 
            replyMarkup: markup, 
            cancellationToken: ct
        );
    }

    private async Task HandleCallbackAsync(TelegramBotClient client, CallbackQuery query, CancellationToken ct)
    {
        string action = query.Data ?? "";
        ChatId chatId = query.Message!.Chat.Id;
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

        await client.EditMessageText(
            chatId: chatId,
            messageId: messageId,
            text: newText,
            parseMode: ParseMode.Html,
            replyMarkup: query.Message.ReplyMarkup as InlineKeyboardMarkup,
            cancellationToken: ct
        );

        await client.AnswerCallbackQuery(
            callbackQueryId: query.Id, 
            cancellationToken: ct
        );
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Ошибка ({source}): {exception.Message}");
        return Task.CompletedTask;
    }
}