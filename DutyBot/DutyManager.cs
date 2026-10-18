using System.Net.Http;
using System.Text;
using System.Text.Json;

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
        _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");
    }

    public async Task LoadFromDbAsync()
    {
        try 
        {
            var requestUrl = $"{_url}/rest/v1/bot_state?id=eq.1&select=data";
            Console.WriteLine($"DEBUG URL: {requestUrl}");
            
            var response = await _httpClient.GetAsync(requestUrl);
            var rawContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"DEBUG RAW: {rawContent}");
            
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
                Console.WriteLine($"DEBUG DATA: {dataJson}");
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
            {
                Console.WriteLine($"✅ Сохранено. Студентов: {_cache.Students.Count}");
            }
            else
            {
                var err = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Ошибка сохранения: {response.StatusCode} - {err}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Исключение при сохранении: {ex.Message}");
        }
    }

    public string AddStudent(string name)
    {
        if (!_loaded)
        {
            Console.WriteLine("⚠️ Данные не загружены");
            return "⚠️ База ещё не загружена, попробуй через 30 секунд.";
        }
        
        _cache.Students.Add(name);
        SaveToDbAsync().GetAwaiter().GetResult();
        return $"✅ {name} добавлен. Всего: {_cache.Students.Count}";
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
            return $"✅ {removed} удалён. Осталось: {_cache.Students.Count}";
        }
        return "❌ Неверный номер.";
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
            return $"✅ {student} перемещён с {from} на {to}";
        }
        return "❌ Неверные номера.";
    }

    public string GetList()
    {
        if (!_loaded) return "️ База не загружена.";
        
        if (_cache.Students.Count == 0)
        {
            return "⚠️ Список пуст.";
        }
        
        var sb = new StringBuilder(" Список группы:\n\n");
        for (int i = 0; i < _cache.Students.Count; i++)
        {
            var marker = i == _cache.CurrentIndex ? "👉 " : "   ";
            sb.AppendLine($"{marker}{i + 1}. {_cache.Students[i]}");
        }
        return sb.ToString();
    }

    public string GetTodayText()
    {
        if (!_loaded) return "⚠️ База не загружена.";
        
        if (_cache.Students.Count == 0)
        {
            return "⚠️ Список пуст. Добавь студентов через /add.";
        }
        
        if (_cache.CurrentIndex >= 0 && _cache.CurrentIndex < _cache.Students.Count)
        {
            return $"📅 Сегодня дежурит:\n\n<b>{_cache.Students[_cache.CurrentIndex]}</b>\n\nПозиция: {_cache.CurrentIndex + 1} из {_cache.Students.Count}";
        }
        return "️ Дежурный не назначен.";
    }

    public string GetStats()
    {
        if (!_loaded) return "⚠️ База не загружена.";
        
        return $"📊 Статистика:\n\nВсего студентов: {_cache.Students.Count}\nТекущий дежурный: #{_cache.CurrentIndex + 1}";
    }

    public string GoBack()
    {
        if (!_loaded || _cache.Students.Count == 0) return "️ Список пуст.";
        
        _cache.CurrentIndex = (_cache.CurrentIndex - 1 + _cache.Students.Count) % _cache.Students.Count;
        SaveToDbAsync().GetAwaiter().GetResult();
        return $"⬅️ Предыдущий дежурный: <b>{_cache.Students[_cache.CurrentIndex]}</b>";
    }

    public string SkipForward()
    {
        if (!_loaded || _cache.Students.Count == 0) return "⚠️ Список пуст.";
        
        _cache.CurrentIndex = (_cache.CurrentIndex + 1) % _cache.Students.Count;
        SaveToDbAsync().GetAwaiter().GetResult();
        return $"➡️ Следующий дежурный: <b>{_cache.Students[_cache.CurrentIndex]}</b>";
    }
}

public class SupabaseRow
{
    public int id { get; set; }
    public JsonElement data { get; set; }
}

public class StudentData
{
    public List<string> Students { get; set; } = new();
    public int CurrentIndex { get; set; } = 0;
}