using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

public class StudentData
{
    public List<string> Students { get; set; } = new();
    public int CurrentIndex { get; set; } = 0;
}

// SupaBase
public class SupabaseRow
{
    public JsonElement data { get; set; }
}

public class DutyManager
{
    private static readonly HttpClient _httpClient = new();
    private readonly string? _url;
    private readonly string? _apiKey;
    
    private StudentData _cache = new();
    private bool _loaded = false;

    public DutyManager()
    {
        _url = Environment.GetEnvironmentVariable("SUPABASE_URL");
        _apiKey = Environment.GetEnvironmentVariable("SUPABASE_KEY");

        if (string.IsNullOrEmpty(_url) || string.IsNullOrEmpty(_apiKey))
        {
            Console.WriteLine("❌ SUPABASE_URL или SUPABASE_KEY не заданы!");
            return;
        }

        _httpClient.DefaultRequestHeaders.Add("apikey", _apiKey);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        Task.Run(LoadFromDbAsync).Wait();
    }

    private async Task LoadFromDbAsync()
    {
        try 
        {
            var response = await _httpClient.GetAsync($"{_url}/rest/v1/bot_state?id=eq.1&select=data");
            
            if (!response.IsSuccessStatusCode) 
            {
                Console.WriteLine($"Ошибка загрузки: {response.StatusCode}");
                return;
            }

            // Десериализуем ответ как список строк Supabase (в табличном виде там)
            var rows = await response.Content.ReadFromJsonAsync<List<SupabaseRow>>();
            
            if (rows != null && rows.Count > 0)
            {
                // Извлекаем поле data из первой строки
                var dataJson = rows[0].data.GetRawText();
                _cache = JsonSerializer.Deserialize<StudentData>(dataJson) ?? new();
                _loaded = true;
                Console.WriteLine($"✅ Данные загружены из Supabase. Студентов: {_cache.Students.Count}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Исключение при загрузке: {ex.Message}");
        }
    }

    private async Task SaveToDbAsync()
    {
        if (!_loaded) return;

        try 
        {
            using var httpContent = JsonContent.Create(new { data = _cache });

            var response = await _httpClient.PatchAsync($"{_url}/rest/v1/bot_state?id=eq.1", httpContent);
            
            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Ошибка Supabase ({response.StatusCode}): {errorBody}");
            }
            else
            {
                Console.WriteLine("✅ Данные успешно сохранены в Supabase.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 Исключение при сохранении: {ex.Message}");
        }
    }

    public string AddStudent(string name)
    {
        name = name.Trim();
        if (string.IsNullOrEmpty(name)) return "❌ Имя пустое.";
        if (_cache.Students.Contains(name)) return $"️ {name} уже есть.";

        _cache.Students.Add(name);
        SaveToDbAsync().Wait();
        return $"✅ {name} добавлен.\nВсего: {_cache.Students.Count} чел.";
    }

    public string RemoveStudent(int index)
    {
        if (index < 1 || index > _cache.Students.Count) return $"❌ Номер вне диапазона (1-{_cache.Students.Count}).";
        
        string removed = _cache.Students[index - 1];
        _cache.Students.RemoveAt(index - 1);
        
        if (_cache.CurrentIndex >= _cache.Students.Count && _cache.Students.Count > 0) 
            _cache.CurrentIndex = 0;
            
        SaveToDbAsync().Wait();
        return $"🗑 Удален: {removed}. Осталось: {_cache.Students.Count} чел.";
    }

    public string MoveStudent(int from, int to)
    {
        if (from < 1 || from > _cache.Students.Count || to < 1 || to > _cache.Students.Count)
            return "❌ Неверные позиции.";

        string temp = _cache.Students[from - 1];
        _cache.Students.RemoveAt(from - 1);
        _cache.Students.Insert(to - 1, temp);
        
        FixAnchor(temp);
        SaveToDbAsync().Wait();
        return $"↕️ Перемещено: {temp} на позицию {to}.";
    }

    private void FixAnchor(string anchorName)
    {
        int idx = _cache.Students.IndexOf(anchorName);
        if (idx >= 0) _cache.CurrentIndex = idx;
    }

    public string GetList()
    {
        if (_cache.Students.Count == 0) return "⚠️ Список пуст.";
        
        var sb = new System.Text.StringBuilder("📋 СПИСОК ГРУППЫ:\n━━━━━━━━━━━━━━\n");
        for (int i = 0; i < _cache.Students.Count; i++)
        {
            string marker = (i == _cache.CurrentIndex) ? "🔴" : "⚪";
            sb.AppendLine($"{marker} {i + 1}. {_cache.Students[i]}");
        }
        sb.AppendLine("━━━━━━━━━━━━━━");
        sb.AppendLine($"Всего: {_cache.Students.Count} чел.");
        return sb.ToString();
    }

    public string GetTodayText()
    {
        if (_cache.Students.Count == 0) return "️ Список пуст. Добавь людей командой /add";
        
        string first = _cache.Students[_cache.CurrentIndex];
        int nextIdx = (_cache.CurrentIndex + 1) % _cache.Students.Count;
        string second = _cache.Students[nextIdx];
        
        return $"<b> Дежурные сегодня:</b>\n\n1. <b>{first}</b>\n2. <b>{second}</b>\n\n<i>Нажми кнопки ниже.</i>";
    }

    public string GoBack()
    {
        if (_cache.Students.Count <= 1) return "⚠️ Мало людей.";
        
        _cache.CurrentIndex--;
        if (_cache.CurrentIndex < 0) _cache.CurrentIndex = _cache.Students.Count - 1;
        
        SaveToDbAsync().Wait();
        return GetTodayText();
    }

    public string SkipForward()
    {
        if (_cache.Students.Count <= 1) return "⚠️ Мало людей.";
        
        _cache.CurrentIndex++;
        if (_cache.CurrentIndex >= _cache.Students.Count) _cache.CurrentIndex = 0;
        
        SaveToDbAsync().Wait();
        return GetTodayText();
    }
    
    public string GetStats() => "Статистика доступна только локально (для демо).";
}