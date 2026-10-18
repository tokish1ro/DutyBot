using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Microsoft.Extensions.Configuration;

// === 1. Пробуем взять токен из переменной окружения (Render) ===
string? token = Environment.GetEnvironmentVariable("BOT_TOKEN");

// === 2. Если переменной нет (локальный запуск), читаем из файла ===
if (string.IsNullOrEmpty(token))
{
    IConfiguration configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: true) // optional: true важно!
        .Build();
    
    token = configuration["BotToken"];
}

// === 3. Если токена всё ещё нет — ошибка ===
if (string.IsNullOrEmpty(token))
{
    Console.WriteLine("❌ Ошибка: BOT_TOKEN не найден ни в ENV, ни в файле!");
    return;
}

// Создаём клиента бота
TelegramBotClient bot = new TelegramBotClient(token);

// ===== РЕГИСТРИРУЕМ МЕНЮ КОМАНД =====
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