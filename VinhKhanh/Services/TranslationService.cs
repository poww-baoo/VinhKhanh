using System.Text.Json;
using VinhKhanh.Models;

namespace VinhKhanh.Services;

public class TranslationService
{
    private readonly DatabaseService _db;
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(12) };

    public TranslationService(DatabaseService db) => _db = db;

    public async Task<string?> TranslateAsync(string text, string from = "vi", string to = "en")
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase)) return text;

        try
        {
            var url =
                $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(text)}&langpair={from}|{to}";
            var json = await Http.GetStringAsync(url);
            var payload = JsonSerializer.Deserialize<MyMemoryResponse>(json);

            var translated = payload?.responseData?.translatedText?.Trim();
            return string.IsNullOrWhiteSpace(translated) ? text : translated;
        }
        catch
        {
            return text;
        }
    }

    public Task<string> GetTranslationAsync(Poi poi, string language = "en")
    {
        var text = poi.GetTextByLanguage(language);
        return Task.FromResult(text);
    }

    public async Task PrefetchAsync(IEnumerable<Poi> pois, string language = "en")
    {
        // Prefetch is now a no-op since automatic translation is disabled
        await Task.CompletedTask;
    }

    private sealed class MyMemoryResponse
    {
        public MyMemoryData? responseData { get; set; }
    }

    private sealed class MyMemoryData
    {
        public string? translatedText { get; set; }
    }
}