using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public class DutyManager
{
    private readonly HttpClient _httpClient;
    private readonly string _url;
    private readonly string _apiKey;
    private StudentData _cache = new();
    private bool _loaded = false;

    public DutyManager()
    {
        _httpClient = new HttpClient();
        _url = Environment.GetEnvironmentVariable("SUPABASE_URL");
        _apiKey = Environment.GetEnvironmentVariable("SUPABASE_KEY");
        _httpClient.DefaultRequestHeaders.Add("apikey", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    private string Esc(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    public async Task LoadFromDbAsync()
    {
        try 
        {
            var requestUrl = $"{_url}/rest/v1/bot_state?id=eq.1&select=data";
            var response = await _httpClient.GetAsync(requestUrl);
            var rawContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode) 
            {
                Console.WriteLine($"❌ Ошибка загрузки: {response.StatusCode}");
                return;
            }

            if (string.IsNullOrWhiteSpace(rawContent) || rawContent == "[]")
            {
                Console.WriteLine("⚠️ Пустой ответ");
                return;
            }

            var rows = JsonSerializer.Deserialize<List<SupabaseRow>>(rawContent);
            
            if (rows != null && rows.Count > 0)
            {
                var dataJson = rows[0].data.GetRawText();
                _cache = JsonSerializer.Deserialize<StudentData>(dataJson) ?? new();
                _loaded = true;
                Console.WriteLine($"✅ Загружено студентов: {_cache.Students.Count}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Исключение при загрузке: {ex.Message}");
        }
    }

    private async Task SaveToDbAsync()
    {
        try
        {
            var payload = new { data = _cache };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PatchAsync($"{_url}/rest/v1/bot_state?id=eq.1", content);
            
            if (response.IsSuccessStatusCode)
                Console.WriteLine($"✅ Сохранено. Студентов: {_cache.Students.Count}");
            else
                Console.WriteLine($"❌ Ошибка сохранения: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Исключение при сохранении: {ex.Message}");
        }
    }

    public string AddStudent(string name)
    {
        if (!_loaded) return "⚠️ База ещё не загружена, попробуй через минуту.";
        
        _cache.Students.Add(name);
        SaveToDbAsync().GetAwaiter().GetResult();
        return $"✅ <b>{Esc(name)}</b> добавлен!\n\n👥 Теперь в списке: <b>{_cache.Students.Count}</b> чел.";
    }

    public string RemoveStudent(int number)
    {
        if (!_loaded) return "⚠️ База не загружена.";
        
        int index = number - 1;
        if (index >= 0 && index < _cache.Students.Count)
        {
            var removed = _cache.Students[index];
            _cache.Students.RemoveAt(index);
            if (_cache.CurrentIndex >= _cache.Students.Count && _cache.Students.Count > 0)
                _cache.CurrentIndex = 0;
            SaveToDbAsync().GetAwaiter().GetResult();
            return $"🗑 <b>{Esc(removed)}</b> удалён из списка.\n\n👥 Осталось: <b>{_cache.Students.Count}</b> чел.";
        }
        return $"❌ Неверный номер. В списке {_cache.Students.Count} чел.";
    }

    public string MoveStudent(int from, int to)
    {
        if (!_loaded) return "⚠️ База не загружена.";
        
        int fromIdx = from - 1;
        int toIdx = to - 1;
        
        if (fromIdx >= 0 && fromIdx < _cache.Students.Count && toIdx >= 0 && toIdx < _cache.Students.Count)
        {
            var student = _cache.Students[fromIdx];
            _cache.Students.RemoveAt(fromIdx);
            _cache.Students.Insert(toIdx, student);
            SaveToDbAsync().GetAwaiter().GetResult();
            return $"↕️ <b>{Esc(student)}</b> перемещён: {from} → {to}";
        }
        return "❌ Неверные номера.";
    }

    public string GetList()
    {
        if (!_loaded) return "⚠️ База не загружена.";
        
        if (_cache.Students.Count == 0)
            return "⚠️ <b>Список пуст.</b>\nДобавь студентов: /add Фамилия";
        
        var sb = new StringBuilder();
        sb.AppendLine("📋 <b>СПИСОК ГРУППЫ</b>");
        sb.AppendLine("━━━━━━━━━━━━━━━━━━");
        sb.AppendLine();
        
        for (int i = 0; i < _cache.Students.Count; i++)
        {
            if (i == _cache.CurrentIndex)
                sb.AppendLine($"👉 <b>{i + 1}. {Esc(_cache.Students[i])}</b> 🟢");
            else
                sb.AppendLine($"     {i + 1}. {Esc(_cache.Students[i])}");
        }
        
        sb.AppendLine();
        sb.AppendLine("━━━━━━━━━━━━━━━━━━");
        sb.AppendLine($"👥 Всего: <b>{_cache.Students.Count}</b>  |  🟢 Дежурит: <b>#{_cache.CurrentIndex + 1}</b>");
        
        return sb.ToString();
    }

    public string GetTodayText()
    {
        if (!_loaded) return "⚠️ База не загружена.";
        
        if (_cache.Students.Count == 0)
            return "⚠️ <b>Список пуст.</b>\nДобавь студентов: /add Фамилия";
        
        var name = _cache.Students[_cache.CurrentIndex];
        return $"📅 <b>ДЕЖУРСТВО</b>\n━━━━━━━━━━━━━━━━━━\n\n🟢 Сегодня дежурит:\n👉 <b>{Esc(name)}</b>\n\n📊 Позиция: {_cache.CurrentIndex + 1} из {_cache.Students.Count}\n\n<i>Листай кнопками ниже 👇</i>";
    }

    public string GetStats()
    {
        if (!_loaded) return "⚠️ База не загружена.";
        
        var name = _cache.Students.Count > 0 ? _cache.Students[_cache.CurrentIndex] : "—";
        return $"📊 <b>СТАТИСТИКА</b>\n━━━━━━━━━━━━━━━━━━\n\n👥 Всего студентов: <b>{_cache.Students.Count}</b>\n🟢 Дежурит сейчас: <b>{Esc(name)}</b>\n💾 Хранилище: Supabase (облако)\n🤖 Хостинг: Render (24/7)";
    }

    public string GoBack()
    {
        if (!_loaded || _cache.Students.Count == 0) return "⚠️ Список пуст.";
        
        _cache.CurrentIndex = (_cache.CurrentIndex - 1 + _cache.Students.Count) % _cache.Students.Count;
        SaveToDbAsync().GetAwaiter().GetResult();
        return $"⬅️ <b>Предыдущий</b>\n━━━━━━━━━━━━━━━━━━\n\n🟢 Теперь дежурит:\n👉 <b>{Esc(_cache.Students[_cache.CurrentIndex])}</b>\n\n📊 Позиция: {_cache.CurrentIndex + 1} из {_cache.Students.Count}";
    }

    public string SkipForward()
    {
        if (!_loaded || _cache.Students.Count == 0) return "⚠️ Список пуст.";
        
        _cache.CurrentIndex = (_cache.CurrentIndex + 1) % _cache.Students.Count;
        SaveToDbAsync().GetAwaiter().GetResult();
        return $"➡️ <b>Следующий</b>\n━━━━━━━━━━━━━━━━━━\n\n🟢 Теперь дежурит:\n👉 <b>{Esc(_cache.Students[_cache.CurrentIndex])}</b>\n\n📊 Позиция: {_cache.CurrentIndex + 1} из {_cache.Students.Count}";
    }
}

public class SupabaseRow
{
    public int id { get; set; }
    public JsonElement data { get; set; }
}

public class StudentData
{
    [JsonPropertyName("students")]
    public List<string> Students { get; set; } = new();
    
    [JsonPropertyName("currentIndex")]
    public int CurrentIndex { get; set; } = 0;
}