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

    public async Task EnsureEnglishColumnsAsync(IEnumerable<Poi>? source = null, CancellationToken cancellationToken = default)
    {
        await _db.InitAsync();

        var pois = source?.ToList() ?? await _db.GetPoisMissingEnglishFieldsAsync();
        foreach (var poi in pois)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var historyEn = string.IsNullOrWhiteSpace(poi.HistoryEn)
                ? await TranslateAsync(poi.History, "vi", "en") ?? poi.History
                : poi.HistoryEn;

            var adrEn = string.IsNullOrWhiteSpace(poi.AdrEn)
                ? await TranslateAsync(poi.Address, "vi", "en") ?? poi.Address
                : poi.AdrEn;

            if (!string.Equals(historyEn, poi.HistoryEn, StringComparison.Ordinal) ||
                !string.Equals(adrEn, poi.AdrEn, StringComparison.Ordinal))
            {
                poi.HistoryEn = historyEn;
                poi.AdrEn = adrEn;
                await _db.UpdatePoiEnglishFieldsAsync(poi.Id, historyEn, adrEn);
            }
        }
    }

    public async Task PrefetchAsync(IEnumerable<Poi> pois, string language = "en")
    {
        if (language == "en")
        {
            await EnsureEnglishColumnsAsync(pois);
        }
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