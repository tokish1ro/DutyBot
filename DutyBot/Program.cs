using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Microsoft.Extensions.Configuration;

// Загружаем токен из файла настроек
IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

string token = configuration["BotToken"]!;

// Создаём клиента бота
TelegramBotClient bot = new TelegramBotClient(token);

// ===== РЕГИСТРИРУЕМ МЕНЮ КОМАНД (подсказки при вводе "/") =====
await bot.SetMyCommands(new[]{
    new BotCommand { Command = "start",   Description = " Главное меню с кнопками" },
    new BotCommand { Command = "add",     Description = "➕ Добавить одногруппника (/add Фамилия)" },
    new BotCommand { Command = "list",    Description = "📋 Список группы" },
    new BotCommand { Command = "today",   Description = "📅 Кто сегодня дежурит" },
    new BotCommand { Command = "next",    Description = "➡️ Следующее дежурство" },
    new BotCommand { Command = "remove",  Description = "🗑 Удалить по номеру (/remove 7)" },
    new BotCommand { Command = "move",    Description = "↕️ Переставить (/move 7 6)" },
    new BotCommand { Command = "stats",   Description = "📊 Статистика дежурств" },
    new BotCommand { Command = "replace", Description = "🔄 Замена (/replace Иванов Петров)" },
    new BotCommand { Command = "remind",  Description = "🔔 Напоминания (/remind on или off)" }
});

// Создаём обработчик
MyUpdateHandler handler = new MyUpdateHandler();

// Запускаем бота
CancellationTokenSource cts = new CancellationTokenSource();
bot.StartReceiving(handler, cancellationToken: cts.Token);

Console.WriteLine("Бот запущен. Нажми Enter для остановки...");
Console.ReadLine();

cts.Cancel();