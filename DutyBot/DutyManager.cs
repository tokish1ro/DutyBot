using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public class DutyData
{
    public List<string> Students { get; set; } = new();
    public int CurrentIndex { get; set; } = 0;
    public Dictionary<string, int> Stats { get; set; } = new();
}

public class DutyManager
{
    private readonly string _filePath = "duties.json";
    private List<string> students = new();
    private int currentIndex = 0;
    private Dictionary<string, int> stats = new();

    public DutyManager()
    {
        LoadData();
        CheckNewDay(); // Проверяем дату при старте
    }

    private void LoadData()
    {
        if (File.Exists(_filePath))
        {
            try
            {
                var json = File.ReadAllText(_filePath);
                var data = JsonSerializer.Deserialize<DutyData>(json);
                
                if (data != null)
                {
                    students = data.Students ?? new();
                    currentIndex = data.CurrentIndex;
                    stats = data.Stats ?? new();
                    
                    // Инициализируем статистику для новых людей
                    foreach (var s in students)
                        if (!stats.ContainsKey(s)) stats[s] = 0;
                }
            }
            catch { /* Если файл битый, начинаем с чистого листа */ }
        }
    }

    private void SaveData()
    {
        var data = new DutyData 
        { 
            Students = students, 
            CurrentIndex = currentIndex, 
            Stats = stats 
        };
        
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(_filePath, JsonSerializer.Serialize(data, options));
    }

    private void CheckNewDay()
    {
        // Логика пропуска выходных/праздников может быть здесь сложной.
        // Для простоты пока оставим как есть: индекс меняется только вручную или через команды.
        // Если нужно авто-пропускать выходные — это отдельная большая задача.
        // Сейчас мы делаем акцент на РУЧНОМ управлении кнопками.
    }

    // === ОСНОВНЫЕ КОМАНДЫ ===

    public string AddStudent(string name)
    {
        name = name.Trim();
        if (string.IsNullOrEmpty(name)) return "❌ Имя не может быть пустым.";
        if (students.Contains(name)) return $"⚠️ {name} уже в списке.";

        students.Add(name);
        stats[name] = 0;
        SaveData();
        return $"✅ {name} добавлен.\nВсего: {students.Count} чел.";
    }

    public string RemoveStudent(int index)
    {
        if (index < 1 || index > students.Count) return $"❌ Неверный номер (от 1 до {students.Count}).";
        
        int realIdx = index - 1;
        string removed = students[realIdx];
        students.RemoveAt(realIdx);
        stats.Remove(removed);
        
        if (currentIndex >= students.Count && students.Count > 0) currentIndex = 0;
        else if (students.Count == 0) currentIndex = 0;
        
        SaveData();
        return $"🗑 Удален: {removed}. Осталось: {students.Count} чел.";
    }

    public string MoveStudent(int from, int to)
    {
        if (from < 1 || from > students.Count || to < 1 || to > students.Count)
            return "❌ Неверные номера позиций.";
            
        int fIdx = from - 1;
        int tIdx = to - 1;
        
        string temp = students[fIdx];
        students.RemoveAt(fIdx);
        students.Insert(tIdx, temp);
        
        // Корректировка индекса текущего дежурного, если он сместился
        FixAnchor(temp); 
        
        SaveData();
        return $"↕️ Перемещено: {temp} на позицию {to}.";
    }

    private void FixAnchor(string anchorName)
    {
        // Простая логика: если анкор (первый дежурный) был перемещен, обновляем индекс
        int idx = students.IndexOf(anchorName);
        if (idx >= 0) currentIndex = idx;
    }

    public string GetList()
    {
        if (students.Count == 0) return "⚠️ Список пуст.\nИспользуй /add Фамилия";
        
        var sb = new System.Text.StringBuilder("📋 СПИСОК ГРУППЫ:\n━━━━━━━━━━━━━━\n");
        for (int i = 0; i < students.Count; i++)
        {
            string marker = (i == currentIndex) ? "🔴" : "⚪";
            sb.AppendLine($"{marker} {i + 1}. {students[i]}");
        }
        sb.AppendLine("━━━━━━━━━━━━━━");
        sb.AppendLine($"Всего: {students.Count} чел.");
        return sb.ToString();
    }

    // === МЕТОДЫ ДЛЯ ТЕКУЩИХ ДЕЖУРНЫХ ===

    // Этот метод возвращает текст о том, кто дежурит СЕЙЧАС (по индексу)
    public string GetTodayText()
    {
        if (students.Count == 0) return "⚠️ Список пуст. Добавь людей командой /add";
        
        // Берем первого
        string first = students[currentIndex];
        
        // Берем второго (циклически)
        int nextIdx = (currentIndex + 1) % students.Count;
        string second = students[nextIdx];
        
        return $"<b>📅 Дежурные сегодня:</b>\n\n1. <b>{first}</b>\n2. <b>{second}</b>\n\n<i>Нажми кнопки ниже, чтобы изменить очередь.</i>";
    }

    // Кнопка "Назад": откатывает индекс
    public string GoBack()
    {
        if (students.Count <= 1) return "⚠️ Слишком мало людей для смены.";
        
        currentIndex--;
        if (currentIndex < 0) currentIndex = students.Count - 1;
        
        SaveData();
        return GetTodayText();
    }

    // Кнопка "Пропустить": двигает вперед
    public string SkipForward()
    {
        if (students.Count <= 1) return "⚠️ Слишком мало людей для смены.";
        
        currentIndex++;
        if (currentIndex >= students.Count) currentIndex = 0;
        
        SaveData();
        return GetTodayText();
    }

    public string GetStats()
    {
        if (stats.Count == 0) return "Статистики нет.";
        
        var sorted = stats.OrderByDescending(x => x.Value).ToList();
        var sb = new System.Text.StringBuilder("📊 СТАТИСТИКА:\n━━━━━━━━━━━━━━\n");
        
        foreach (var pair in sorted.Take(10)) // Топ-10
        {
            sb.AppendLine($"{pair.Key}: {pair.Value} раз(а)");
        }
        
        return sb.ToString();
    }
    
    // Метод для увеличения счетчика (вызывается где-то в обработчике при подтверждении дежурства)
    public void IncrementStat(string studentName)
    {
        if (stats.ContainsKey(studentName))
            stats[studentName]++;
        else
            stats[studentName] = 1;
            
        SaveData();
    }
}