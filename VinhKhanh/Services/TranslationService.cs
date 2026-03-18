using System.Net.Http.Json;
using VinhKhanh.Models;

namespace VinhKhanh.Services;

public class TranslationService
{
    private readonly DatabaseService _db;
    private readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private const string ApiUrl =
        "https://api.mymemory.translated.net/get";

    public TranslationService(DatabaseService db) => _db = db;

    public async Task<string?> TranslateAsync(string text,
        string from = "vi", string to = "en")
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        try
        {
            var url = $"{ApiUrl}?q={Uri.EscapeDataString(text)}" +
                      $"&langpair={from}|{to}";
            var res = await _http
                .GetFromJsonAsync<MyMemoryResponse>(url);
            if (res?.ResponseStatus == 200 &&
                !string.IsNullOrWhiteSpace(
                    res.ResponseData?.TranslatedText))
                return res.ResponseData.TranslatedText;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[Translation] {ex.Message}");
        }
        return null;
    }

    public async Task<string> GetTranslationAsync(
        Poi poi, string language = "en")
    {
        if (language == "vi") return poi.TextVi;

        var cached = await _db.GetTranslationCacheAsync(
            poi.Id, language);
        if (cached is not null && cached.IsValid)
            return cached.TranslatedText;

        var translated = await TranslateAsync(
            poi.TextVi, "vi", language);
        if (!string.IsNullOrWhiteSpace(translated))
        {
            await _db.SaveTranslationCacheAsync(new TranslationCache
            {
                PoiId = poi.Id,
                Language = language,
                TranslatedText = translated,
                CachedAt = DateTime.Now
            });
            return translated;
        }

        return poi.TextVi;
    }

    public async Task PrefetchAsync(IEnumerable<Poi> pois,
        string language = "en")
    {
        if (language == "vi") return;

        foreach (var poi in pois)
        {
            var cached = await _db.GetTranslationCacheAsync(
                poi.Id, language);
            if (cached is not null && cached.IsValid) continue;

            var translated = await TranslateAsync(
                poi.TextVi, "vi", language);
            if (!string.IsNullOrWhiteSpace(translated))
            {
                await _db.SaveTranslationCacheAsync(
                    new TranslationCache
                    {
                        PoiId = poi.Id,
                        Language = language,
                        TranslatedText = translated,
                        CachedAt = DateTime.Now
                    });
            }
            await Task.Delay(300);
        }
    }

    private class MyMemoryResponse
    {
        public ResponseData? ResponseData { get; set; }
        public int ResponseStatus { get; set; }
    }

    private class ResponseData
    {
        public string? TranslatedText { get; set; }
    }
}