using System.Net;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Microsoft.Extensions.Configuration;

string? token = Environment.GetEnvironmentVariable("BOT_TOKEN");

if (string.IsNullOrEmpty(token))
{
    IConfiguration configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: true)
        .Build();
    
    token = configuration["BotToken"];
}

if (string.IsNullOrEmpty(token))
{
    Console.WriteLine("❌ Ошибка: BOT_TOKEN не найден!");
    return;
}

TelegramBotClient bot = new TelegramBotClient(token);

await bot.SetMyCommands(new[]{
    new BotCommand { Command = "start",   Description = "🏠 Главное меню" },
    new BotCommand { Command = "add",     Description = "➕ Добавить (/add Фамилия)" },
    new BotCommand { Command = "list",    Description = "📋 Список группы" },
    new BotCommand { Command = "today",   Description = "📅 Кто дежурит (+кнопки)" },
    new BotCommand { Command = "next",    Description = "➡️ Следующее дежурство" },
    new BotCommand { Command = "remove",  Description = "🗑 Удалить (/remove №)" },
    new BotCommand { Command = "move",    Description = "↕️ Переставить (/move №1 №2)" },
    new BotCommand { Command = "stats",   Description = "📊 Статистика" },
    new BotCommand { Command = "replace", Description = "🔄 Замена (/replace Иванов Петров)" },
    new BotCommand { Command = "remind",  Description = "🔔 Напоминания (/remind on/off)" }
});

MyUpdateHandler handler = new MyUpdateHandler();

Console.WriteLine("⏳ Загрузка данных из базы...");
await handler.InitializeAsync();

CancellationTokenSource cts = new CancellationTokenSource();
bot.StartReceiving(handler, cancellationToken: cts.Token);

Console.WriteLine("🚀 Бот запущен!");

var listener = new HttpListener();
int port = int.Parse(Environment.GetEnvironmentVariable("PORT") ?? "10000");
listener.Prefixes.Add($"http://*:{port}/");
listener.Start();

Console.WriteLine($"✅ Health check сервер запущен на порту {port}");

_ = Task.Run(async () =>
{
    while (!cts.IsCancellationRequested)
    {
        try
        {
            var context = await listener.GetContextAsync();
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes("OK");
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer);
            context.Response.Close();
        }
        catch { }
    }
});

await Task.Delay(Timeout.Infinite, cts.Token);