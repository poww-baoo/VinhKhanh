using VinhKhanh.Models;

namespace VinhKhanh.Services;

public class TranslationService
{
    private readonly DatabaseService _db;

    public TranslationService(DatabaseService db) => _db = db;

    public Task<string?> TranslateAsync(string text,
        string from = "vi", string to = "en")
    {
        if (string.IsNullOrWhiteSpace(text)) return Task.FromResult<string?>(null);
        return Task.FromResult<string?>(text);
    }

    public Task<string> GetTranslationAsync(
        Poi poi, string language = "en")
    {
        var text = poi.GetTextByLanguage(language);
        return Task.FromResult(text);
    }

    public Task PrefetchAsync(IEnumerable<Poi> pois,
        string language = "en")
    {
        return Task.CompletedTask;
    }
}