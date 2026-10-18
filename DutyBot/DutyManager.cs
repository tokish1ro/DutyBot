using System.Net.Http.Json;
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
    }

    public async Task LoadFromDbAsync()
    {
        try 
        {
            var requestUrl = $"{_url}/rest/v1/bot_state?id=eq.1&select=data";
            Console.WriteLine($"DEBUG URL: {requestUrl}");
            
            var response = await _httpClient.GetAsync(requestUrl);
            var rawContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"DEBUG RAW RESPONSE: {rawContent}");
            
            if (!response.IsSuccessStatusCode) 
            {
                Console.WriteLine($"❌ Ошибка: {response.StatusCode}");
                return;
            }

            if (string.IsNullOrWhiteSpace(rawContent) || rawContent == "[]")
            {
                Console.WriteLine("️ Пустой ответ от базы");
                return;
            }

            var rows = JsonSerializer.Deserialize<List<SupabaseRow>>(rawContent);
            
            if (rows != null && rows.Count > 0)
            {
                var dataJson = rows[0].data.GetRawText();
                Console.WriteLine($"DEBUG DATA JSON: {dataJson}");
                _cache = JsonSerializer.Deserialize<StudentData>(dataJson) ?? new();
                _loaded = true;
                Console.WriteLine($"✅ Загружено студентов: {_cache.Students.Count}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Исключение: {ex.Message}");
        }
    }

    public async Task SaveToDbAsync()
    {
        try
        {
            var data = new { data = _cache };
            var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PatchAsync($"{_url}/rest/v1/bot_state?id=eq.1", content);
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"✅ Данные сохранены. Студентов: {_cache.Students.Count}");
            }
            else
            {
                Console.WriteLine($"❌ Ошибка сохранения: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($" Исключение при сохранении: {ex.Message}");
        }
    }

    public void AddStudent(string name)
    {
        if (!_loaded)
        {
            Console.WriteLine("⚠️ Данные ещё не загружены");
            return;
        }
        
        _cache.Students.Add(name);
        Console.WriteLine($"✅ Добавлен: {name}. Всего: {_cache.Students.Count}");
    }

    public void RemoveStudent(int index)
    {
        if (!_loaded) return;
        
        if (index >= 0 && index < _cache.Students.Count)
        {
            var removed = _cache.Students[index];
            _cache.Students.RemoveAt(index);
            Console.WriteLine($"✅ Удалён: {removed}");
        }
        else
        {
            Console.WriteLine("❌ Неверный номер");
        }
    }

    public void MoveStudent(int from, int to)
    {
        if (!_loaded) return;
        
        if (from >= 0 && from < _cache.Students.Count && to >= 0 && to < _cache.Students.Count)
        {
            var student = _cache.Students[from];
            _cache.Students.RemoveAt(from);
            _cache.Students.Insert(to, student);
            Console.WriteLine($"✅ {student} перемещён с {from + 1} на {to + 1}");
        }
        else
        {
            Console.WriteLine("❌ Неверные номера");
        }
    }

    public string GetList()
    {
        if (!_loaded || _cache.Students.Count == 0)
        {
            return "⚠️ Список пуст.";
        }
        
        var sb = new StringBuilder("📋 Список группы:\n\n");
        for (int i = 0; i < _cache.Students.Count; i++)
        {
            var marker = i == _cache.CurrentIndex ? "👉 " : "   ";
            sb.AppendLine($"{marker}{i + 1}. {_cache.Students[i]}");
        }
        return sb.ToString();
    }

    public string GetTodayDuty()
    {
        if (!_loaded || _cache.Students.Count == 0)
        {
            return "⚠️ Список пуст.";
        }
        
        if (_cache.CurrentIndex >= 0 && _cache.CurrentIndex < _cache.Students.Count)
        {
            return $"📅 Сегодня дежурит: {_cache.Students[_cache.CurrentIndex]}";
        }
        return "⚠️ Дежурный не назначен.";
    }

    public void NextDuty()
    {
        if (!_loaded || _cache.Students.Count == 0) return;
        
        _cache.CurrentIndex = (_cache.CurrentIndex + 1) % _cache.Students.Count;
        Console.WriteLine($"✅ Следующий дежурный: {_cache.Students[_cache.CurrentIndex]}");
    }

    public void PrevDuty()
    {
        if (!_loaded || _cache.Students.Count == 0) return;
        
        _cache.CurrentIndex = (_cache.CurrentIndex - 1 + _cache.Students.Count) % _cache.Students.Count;
        Console.WriteLine($"✅ Предыдущий дежурный: {_cache.Students[_cache.CurrentIndex]}");
    }

    public int StudentCount => _cache.Students.Count;
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