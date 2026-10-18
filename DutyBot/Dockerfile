# Используем официальный образ SDK для сборки
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем только csproj сначала (для кэширования зависимостей)
COPY ["DutyBot.csproj", "./"]
RUN dotnet restore "DutyBot.csproj"

# Копируем весь остальной код
COPY . .

# Публикуем релизную версию
RUN dotnet publish "DutyBot.csproj" -c Release -o /app/publish

# Финальный образ (легче и быстрее)
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Команда запуска
ENTRYPOINT ["dotnet", "DutyBot.dll"]