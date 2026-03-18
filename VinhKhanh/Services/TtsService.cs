namespace VinhKhanh.Services;

public class TtsService
{
    private CancellationTokenSource? _cts;
    public bool IsSpeaking { get; private set; }

    public async Task SpeakAsync(string text, string language = "vi")
    {
        StopCurrent();
        _cts = new CancellationTokenSource();
        IsSpeaking = true;

        try
        {
            var locales = await TextToSpeech.GetLocalesAsync();
            var locale = locales.FirstOrDefault(l =>
                l.Language.StartsWith(language,
                    StringComparison.OrdinalIgnoreCase));

            await TextToSpeech.SpeakAsync(text, new SpeechOptions
            {
                Locale = locale,
                Volume = 1.0f,
                Pitch = 1.0f
            }, _cts.Token);
        }
        catch (OperationCanceledException) { }
        catch
        {
            try { await TextToSpeech.SpeakAsync(text); }
            catch { }
        }
        finally { IsSpeaking = false; }
    }

    public void StopCurrent()
    {
        _cts?.Cancel();
        _cts = null;
        IsSpeaking = false;
    }
}