<div align="center">

# 🤖 DutyBot

**Telegram-бот для автоматизации дежурств в учебных группах**

Управляйте списком участников и распределяйте дежурства прямо в Telegram.

<br>

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Telegram Bot](https://img.shields.io/badge/Telegram-Bot-26A5E4?style=flat-square&logo=telegram&logoColor=white)
![Supabase](https://img.shields.io/badge/Database-Supabase-3ECF8E?style=flat-square&logo=supabase&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-supported-2496ED?style=flat-square&logo=docker&logoColor=white)
![License: MIT](https://img.shields.io/badge/License-MIT-yellow?style=flat-square)

</div>

---

## ✨ Возможности

- **Список участников** — добавление, просмотр и удаление студентов.
- **Ротация дежурных** — переключение дежурства вперёд или назад.
- **Просмотр дежурных на сегодня** — быстрый доступ к актуальной информации.
- **Инлайн-кнопки** — управление через интерфейс Telegram.
- **Облачное хранение** — состояние бота сохраняется в Supabase (PostgreSQL).

## 🚀 Команды

| Команда | Назначение |
|---|---|
| `/start` | Запустить бота |
| `/add <имя>` | Добавить участника |
| `/list` | Показать список группы |
| `/today` | Посмотреть, кто дежурит сегодня |
| `/remove <номер>` | Удалить участника по номеру |
| `/move <от> <до>` | Поменять участников местами |
| `/back` | Переключить дежурство назад |
| `/skip` | Переключить дежурство вперёд |

## 🧰 Технологии

- **C# / .NET 10** — логика приложения
- **Telegram.Bot** — взаимодействие с Telegram Bot API
- **Supabase / PostgreSQL** — хранение данных
- **Docker** — контейнеризация
- **Render** — облачный хостинг
- **System.Text.Json** — сериализация данных

## ⚙️ Запуск собственного экземпляра

### 1. Создайте Telegram-бота

Откройте [@BotFather](https://t.me/BotFather), отправьте `/newbot` и сохраните выданный токен.

### 2. Настройте Supabase

Создайте проект на [supabase.com](https://supabase.com), откройте **SQL Editor** и выполните:

```sql
CREATE TABLE bot_state (
    id BIGINT PRIMARY KEY,
    created_at TIMESTAMPTZ DEFAULT now(),
    data JSONB NOT NULL
);

INSERT INTO bot_state (id, data)
VALUES (1, '{"students":[],"currentIndex":0}');
```

> **Важно:** не отключайте Row Level Security без необходимости и не используйте публичный ключ для обхода правил доступа. Настройте безопасный доступ к таблице согласно способу подключения, реализованному в приложении. Никогда не публикуйте секретные ключи в репозитории.

### 3. Настройте переменные окружения

Подготовьте следующие переменные:

| Переменная | Значение |
|---|---|
| `BOT_TOKEN` | Токен от BotFather |
| `SUPABASE_URL` | URL проекта Supabase |
| `SUPABASE_KEY` | Ключ Supabase, используемый приложением |

### 4. Запустите приложение

Для локального запуска установите [.NET 10 SDK](https://dotnet.microsoft.com/download) и [Git](https://git-scm.com/), клонируйте репозиторий и запустите проект из каталога с `.csproj`:

```bash
git clone https://github.com/tokish1ro/DutyBot.git
cd DutyBot
dotnet restore
dotnet run
```

Перед запуском убедитесь, что переменные окружения настроены, а команды соответствуют фактической структуре проекта.

### 🐳 Docker

Если в репозитории настроен `Dockerfile`, образ можно собрать командой:

```bash
docker build -t dutybot .
```

Запуск контейнера зависит от конфигурации приложения и необходимых переменных окружения.

### ☁️ Развёртывание

Для облачного размещения можно использовать [Render](https://render.com/) или другую платформу, поддерживающую выбранный способ запуска. Перед развёртыванием проверьте требования к веб-сервису, портам и обработке обновлений Telegram в текущей реализации.

## ⚠️ Текущие ограничения

- В текущей версии один экземпляр бота рассчитан на одну группу.
- Веб-панель управления не предусмотрена.
- Нет статистики дежурств и встроенного экспорта.
- Возможности управления и разграничения прав зависят от текущей реализации.

## 🗺️ Планы развития

- Поддержка нескольких групп.
- Уведомления о предстоящих дежурствах.
- Экспорт расписания в Excel или PDF.
- Роли администратора и участника.
- Статистика распределения дежурств.

## 📄 Лицензия

Проект распространяется по лицензии [MIT](LICENSE). Подробности приведены в файле `LICENSE`.

## 👨‍💻 Автор

**tokish1ro**

- Telegram: [@konechno_aga_da](https://t.me/konechno_aga_da)
- GitHub: [@tokish1ro](https://github.com/tokish1ro)

---

<div align="center">

**Если проект оказался полезным, поставьте ⭐ репозиторию!**

Сделано с ❤️

</div>
