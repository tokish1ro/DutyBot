using System.Text.Json;

public class DutyManager
{
    private string filePath = "duties.json";
    private List<string> students = new();
    private Dictionary<string, int> stats = new();
    private int currentIndex = 0;
    private DateTime lastDutyDate = DateTime.Today;
    private bool remindEnabled = false;

    public DutyManager()
    {
        LoadData();
        CheckNewDay();
    }

    private void LoadData()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<DutyData>(json);
            if (data != null)
            {
                students = data.Students;
                currentIndex = data.CurrentIndex;
                lastDutyDate = data.LastDutyDate;
                stats = data.Stats ?? new Dictionary<string, int>();
                remindEnabled = data.RemindEnabled;
            }
        }
    }

    private void SaveData()
    {
        var data = new DutyData
        {
            Students = students,
            CurrentIndex = currentIndex,
            LastDutyDate = lastDutyDate,
            Stats = stats,
            RemindEnabled = remindEnabled
        };
        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    private void CheckNewDay()
    {
        if (students.Count == 0) return;

        if (lastDutyDate.Date < DateTime.Today.Date && !IsWeekend(DateTime.Today))
        {
            DateTime nextWorkDay = GetNextWorkDay(lastDutyDate);

            if (nextWorkDay.Date <= DateTime.Today.Date)
            {
                AdvanceDuty();
                lastDutyDate = DateTime.Today;
                SaveData();
            }
        }
    }

    private bool IsWeekend(DateTime date)
    {
        return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
    }

    private DateTime GetNextWorkDay(DateTime from)
    {
        DateTime next = from.AddDays(1);
        while (IsWeekend(next))
        {
            next = next.AddDays(1);
        }
        return next;
    }

    private void AdvanceDuty()
    {
        string first = students[currentIndex];
        string second = students[(currentIndex + 1) % students.Count];

        if (!stats.ContainsKey(first)) stats[first] = 0;
        if (!stats.ContainsKey(second)) stats[second] = 0;
        stats[first]++;
        stats[second]++;

        currentIndex = (currentIndex + 2) % students.Count;
    }

    public string AddStudent(string name)
    {
        if (students.Contains(name)) return $"⚠️ {name} уже в списке.";

        students.Add(name);
        stats[name] = 0;
        SaveData();
        return $"✅ {name} добавлен в список.\nВсего: {students.Count} чел.";
    }

    public string GetList()
    {
        if (students.Count == 0) return "⚠️ Список пуст.\nИспользуй /add Фамилия";

        CheckNewDay();

        int first = currentIndex;
        int second = (currentIndex + 1) % students.Count;

        string result = "━━━━━━━━━━━━━━━━━━━━\n";
        result += "📋 <b>СПИСОК ГРУППЫ</b>\n";
        result += "━━━━━━━━━━━━━━━━━━━━\n\n";

        for (int i = 0; i < students.Count; i++)
        {
            string status = "";
            if (i == first || i == second)
            {
                status = " ⬅️ <b>СЕГОДНЯ</b>";
            }
            result += $"{i + 1}. {students[i]}{status}\n";
        }

        result += "\n━━━━━━━━━━━━━━━━━━━━";
        return result;
    }

    public string GetToday()
    {
        if (students.Count == 0) return "⚠️ Список пуст.\nДобавь одногруппников через /add";

        CheckNewDay();

        if (IsWeekend(DateTime.Today))
        {
            return "━━━━━━━━━━━━━━━━━━━━\n" +
                   "😴 <b>ВЫХОДНОЙ</b>\n" +
                   "━━━━━━━━━━━━━━━━━━━━\n\n" +
                   $"Сегодня {DateTime.Today:dddd}.\n" +
                   "Дежурства нет.\n\n" +
                   $"📅 Следующее дежурство:\n{GetNextWorkDay(DateTime.Today):dd.MM.yyyy}";
        }

        int first = currentIndex;
        int second = (currentIndex + 1) % students.Count;

        string result = "━━━━━━━━━━━━━━━━━━━━\n";
        result += $"📅 <b>{DateTime.Today:dd.MM.yyyy}</b>\n";
        result += "━━━━━━━━━━━━━━━━━━━━\n\n";
        result += "<b>СЕГОДНЯ ДЕЖУРЯТ:</b>\n\n";
        result += $"1️⃣ <b>{students[first]}</b>\n";
        result += $"2️⃣ <b>{students[second]}</b>\n\n";
        result += "━━━━━━━━━━━━━━━━━━━━";

        return result;
    }

    public string GetStats()
    {
        if (students.Count == 0) return "⚠️ Список пуст.";

        var sorted = stats.OrderByDescending(x => x.Value).ToList();
        int maxCount = sorted.Count > 0 && sorted[0].Value > 0 ? sorted[0].Value : 1;

        string result = "━━━━━━━━━━━━━━━━━━━━\n";
        result += "📊 <b>СТАТИСТИКА ДЕЖУРСТВ</b>\n";
        result += "━━━━━━━━━━━━━━━━━━━━\n\n";

        for (int i = 0; i < sorted.Count; i++)
        {
            var student = sorted[i];
            int barLength = 10;
            int filled = (int)((double)student.Value / maxCount * barLength);
            string bar = new string('█', filled) + new string('░', barLength - filled);

            string medal = i switch
            {
                0 => "🥇",
                1 => "🥈",
                2 => "🥉",
                _ => "  "
            };

            result += $"{medal} <b>{student.Key}</b>\n";
            result += $"    {bar} {student.Value}\n\n";
        }

        result += "━━━━━━━━━━━━━━━━━━━━";
        return result;
    }

    public string GetNext()
    {
        if (students.Count == 0) return "⚠️ Список пуст.";

        int daysUntilNext = 1;
        if (DateTime.Today.DayOfWeek == DayOfWeek.Friday)
        {
            daysUntilNext = 3;
        }
        else if (DateTime.Today.DayOfWeek == DayOfWeek.Saturday)
        {
            daysUntilNext = 2;
        }

        DateTime nextDutyDate = DateTime.Today.AddDays(daysUntilNext);
        int nextFirst = (currentIndex + 2) % students.Count;
        int nextSecond = (currentIndex + 3) % students.Count;

        string result = "━━━━━━━━━━━━━━━━━━━━\n";
        result += "➡️ <b>СЛЕДУЮЩЕЕ ДЕЖУРСТВО</b>\n";
        result += "━━━━━━━━━━━━━━━━━━━━\n\n";
        result += $"📅 {nextDutyDate:dd.MM.yyyy}\n\n";
        result += "<b>Дежурят:</b>\n";
        result += $"1. {students[nextFirst]}\n";
        result += $"2. {students[nextSecond]}\n\n";
        result += "━━━━━━━━━━━━━━━━━━━━";

        return result;
    }

    public string Replace(string fromName, string toName)
    {
        int fromIndex = students.FindIndex(s => s.Equals(fromName, StringComparison.OrdinalIgnoreCase));
        int toIndex = students.FindIndex(s => s.Equals(toName, StringComparison.OrdinalIgnoreCase));

        if (fromIndex == -1) return $"❌ {fromName} не найден в списке.";
        if (toIndex == -1) return $"❌ {toName} не найден в списке.";

        string temp = students[fromIndex];
        students[fromIndex] = students[toIndex];
        students[toIndex] = temp;

        SaveData();

        int first = currentIndex;
        int second = (currentIndex + 1) % students.Count;

        return $"✅ {fromName} и {toName} поменялись местами.\n\n<b>Сегодня дежурят:</b>\n1. {students[first]}\n2. {students[second]}";
    }

    public string ToggleRemind(bool enable)
    {
        remindEnabled = enable;
        SaveData();
        return enable
            ? "🔔 <b>Напоминания включены!</b>\nКаждое утро в 8:00 буду писать, кто дежурит."
            : "🔕 <b>Напоминания выключены.</b>";
    }

    public bool IsRemindEnabled() => remindEnabled;

    // ===== УДАЛЕНИЕ ПО НОМЕРУ =====
    public string RemoveStudent(int index)
    {
        if (students.Count == 0) return "⚠️ Список пуст.";
        if (index < 1 || index > students.Count)
            return $"❌ Неверный номер. В списке {students.Count} чел. Смотри /list";

        string anchor = students[currentIndex];
        string name = students[index - 1];

        students.RemoveAt(index - 1);
        stats.Remove(name);
        FixAnchor(anchor);
        SaveData();

        return $"🗑 <b>{name}</b> удалён из списка.\nОсталось: {students.Count} чел.";
    }

    // ===== ВСТАВКА НА КОНКРЕТНОЕ МЕСТО =====
    public string InsertStudent(int index, string name)
    {
        if (students.Contains(name)) return $"⚠️ {name} уже в списке.";
        if (index < 1 || index > students.Count + 1)
            return $"❌ Неверный номер. Можно вставить на позиции 1..{students.Count + 1}";

        string? anchor = students.Count > 0 ? students[currentIndex] : null;

        students.Insert(index - 1, name);
        stats[name] = 0;
        if (anchor != null) FixAnchor(anchor);
        SaveData();

        return $"✅ <b>{name}</b> вставлен на позицию {index}.\nОстальные сдвинуты вниз.";
    }

    // ===== ПЕРЕМЕЩЕНИЕ ЧЕЛОВЕКА =====
    public string MoveStudent(int from, int to)
    {
        if (students.Count == 0) return "⚠️ Список пуст.";
        if (from < 1 || from > students.Count) return $"❌ Неверный номер {from}.";
        if (to < 1 || to > students.Count) return $"❌ Неверный номер {to}.";

        string anchor = students[currentIndex];
        string name = students[from - 1];

        students.RemoveAt(from - 1);
        students.Insert(to - 1, name);
        FixAnchor(anchor);
        SaveData();

        return $"↕️ <b>{name}</b> перемещён с позиции {from} на {to}.";
    }

    // Сохраняет текущих дежурных при изменениях списка
    private void FixAnchor(string anchor)
    {
        int idx = students.IndexOf(anchor);
        if (idx >= 0) currentIndex = idx;
        else if (students.Count == 0) currentIndex = 0;
        else currentIndex = Math.Min(currentIndex, students.Count - 1);
    }
    private class DutyData
    {
        public List<string> Students { get; set; } = new();
        public Dictionary<string, int> Stats { get; set; } = new();
        public int CurrentIndex { get; set; } = 0;
        public DateTime LastDutyDate { get; set; } = DateTime.Today;
        public bool RemindEnabled { get; set; } = false;
    }
}