using VinhKhanh.Models;

namespace VinhKhanh.Services;

public class PlaybackService
{
    private readonly DatabaseService _db;
    private readonly TtsService _tts;
    private readonly TranslationService _translation;

    private readonly TimeSpan _cooldown =
        TimeSpan.FromMinutes(5);
    private const int DebounceSeconds = 3;
    private CancellationTokenSource? _debounce;

    private readonly string _deviceId =
        $"{DeviceInfo.Current.Name}_{DeviceInfo.Current.Model}";

    public bool IsPlaying => _tts.IsSpeaking;

    public PlaybackService(DatabaseService db,
        TtsService tts, TranslationService translation)
    {
        _db = db;
        _tts = tts;
        _translation = translation;
    }

    public async Task TriggerAsync(Poi poi,
        string language = "vi")
    {
        var last = await _db.GetLastLogAsync(
            poi.Id, _deviceId);
        if (last != null &&
            DateTime.Now - last.PlayedAt < _cooldown)
            return;

        _debounce?.Cancel();
        _debounce = new CancellationTokenSource();

        try
        {
            await Task.Delay(
                DebounceSeconds * 1000, _debounce.Token);

            var text = await _translation
                .GetTranslationAsync(poi, language);
            if (string.IsNullOrWhiteSpace(text)) return;

            await _tts.SpeakAsync(text, language);

            await _db.AddLogAsync(new PlaybackLog
            {
                PoiId = poi.Id,
                Language = language,
                PlayedAt = DateTime.Now,
                DeviceId = _deviceId
            });
        }
        catch (TaskCanceledException) { }
    }

    public void Stop() => _tts.StopCurrent();
}