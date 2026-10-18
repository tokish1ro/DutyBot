using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

public class MyUpdateHandler : IUpdateHandler
{
    private DutyManager dutyManager = new DutyManager();

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // ===== ОБРАБОТКА ОБЫЧНЫХ СООБЩЕНИЙ =====
        if (update.Type == UpdateType.Message && update.Message != null)
        {
            Message message = update.Message;
            string text = message.Text ?? "";

            if (string.IsNullOrWhiteSpace(text)) return;

            string response = "";
            ReplyMarkup? keyboard = null;

            if (text == "/start")
            {
                response = "━━━━━━━━━━━━━━━━━━━━\n" +
                           "🤖 <b>БОТ ДЕЖУРСТВ</b>\n" +
                           "━━━━━━━━━━━━━━━━━━━━\n\n" +
                           "<b>Команды:</b>\n" +
                           "/add Фамилия — добавить в конец\n" +
                           "/add 7 Фамилия — вставить на место 7\n" +
                           "/remove 7 — удалить с места 7\n" +
                           "/move 7 6 — переставить человека\n" +
                           "/replace Иванов Петров — замена\n\n" +
                           "Или выбери кнопкой:";

                keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("📋 Список", "list"),
                        InlineKeyboardButton.WithCallbackData("📅 Сегодня", "today")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("📊 Статистика", "stats"),
                        InlineKeyboardButton.WithCallbackData("➡️ Следующий", "next")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🔔 Напоминания", "remind")
                    }
                });
            }
                       else if (text.StartsWith("/add "))
            {
                string args = text.Substring(5).Trim();
                string[] parts = args.Split(' ', 2);

                if (parts.Length == 2 && int.TryParse(parts[0], out int pos))
                {
                    // /add 7 Иванов — вставка на позицию
                    response = dutyManager.InsertStudent(pos, parts[1].Trim());
                }
                else if (parts.Length == 1 && !int.TryParse(parts[0], out _))
                {
                    // /add Иванов — в конец списка
                    response = dutyManager.AddStudent(parts[0]);
                }
                else
                {
                    response = "️ Формат:\n/add Фамилия — в конец\n/add 7 Фамилия — на позицию 7";
                }
            }
            else if (text.StartsWith("/remove "))
            {
                if (int.TryParse(text.Substring(8).Trim(), out int idx))
                {
                    response = dutyManager.RemoveStudent(idx);
                }
                else
                {
                    response = "⚠️ Формат: /remove 7 (номер из /list)";
                }
            }
            else if (text.StartsWith("/move "))
            {
                string[] parts = text.Substring(6).Trim().Split(' ');
                if (parts.Length == 2 && int.TryParse(parts[0], out int from) && int.TryParse(parts[1], out int to))
                {
                    response = dutyManager.MoveStudent(from, to);
                }
                else
                {
                    response = "⚠️ Формат: /move 7 6 (с позиции на позицию)";
                }
            }
            else if (text == "/list")
            {
                response = dutyManager.GetList();
            }
            else if (text == "/today")
            {
                response = dutyManager.GetToday();
            }
            else if (text == "/stats")
            {
                response = dutyManager.GetStats();
            }
            else if (text == "/next")
            {
                response = dutyManager.GetNext();
            }
            else if (text.StartsWith("/replace "))
            {
                string[] parts = text.Substring(9).Trim().Split(' ', 2);
                if (parts.Length == 2)
                {
                    response = dutyManager.Replace(parts[0], parts[1]);
                }
                else
                {
                    response = "⚠️ Формат: /replace Иванов Петров";
                }
            }
            else if (text == "/remind on")
            {
                response = dutyManager.ToggleRemind(true);
            }
            else if (text == "/remind off")
            {
                response = dutyManager.ToggleRemind(false);
            }
            else if (text == "/remind")
            {
                bool enabled = dutyManager.IsRemindEnabled();
                response = enabled
                    ? "🔔 Напоминания <b>включены</b>.\nИспользуй /remind off для выключения."
                    : "🔕 Напоминания <b>выключены</b>.\nИспользуй /remind on для включения.";
            }
            else
            {
                response = "❓ Неизвестная команда.\nНажми /start для меню.";
            }

            await botClient.SendMessage(
                message.Chat.Id,
                response,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard
            );
        }
        // ===== ОБРАБОТКА НАЖАТИЙ НА КНОПКИ =====
        else if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
        {
            CallbackQuery callbackQuery = update.CallbackQuery;
            string data = callbackQuery.Data ?? "";

            string response = "";

            switch (data)
            {
                case "list":
                    response = dutyManager.GetList();
                    break;
                case "today":
                    response = dutyManager.GetToday();
                    break;
                case "stats":
                    response = dutyManager.GetStats();
                    break;
                case "next":
                    response = dutyManager.GetNext();
                    break;
                case "remind":
                    bool enabled = dutyManager.IsRemindEnabled();
                    response = enabled
                        ? "🔔 Напоминания <b>включены</b>.\nИспользуй /remind off для выключения."
                        : "🔕 Напоминания <b>выключены</b>.\nИспользуй /remind on для включения.";
                    break;
                default:
                    response = "❓ Неизвестная команда";
                    break;
            }

            await botClient.AnswerCallbackQuery(callbackQuery.Id);

            if (callbackQuery.Message != null)
            {
                await botClient.SendMessage(
                    callbackQuery.Message.Chat.Id,
                    response,
                    parseMode: ParseMode.Html
                );
            }
        }
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource errorSource, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Ошибка: {exception.Message}");
        return Task.CompletedTask;
    }
}