using System.Diagnostics;
using System.Net.Http.Json;

namespace VinhKhanh.Services
{
    public class AudioContent
    {
        public string RestaurantId { get; set; } = string.Empty;
        public string Language { get; set; } = "vi";
        public string ContentType { get; set; } = string.Empty;
        public string AudioUrl { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    public class AudioPlaybackService
    {
        private readonly TtsService _ttsService;
        private readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        private const string TranslationApiUrl = "https://api.mymemory.translated.net/get";
        private AudioContent? _currentlyPlaying;
        private bool _isPlaying;

        public event EventHandler<AudioContent>? PlaybackStarted;
        public event EventHandler<AudioContent>? PlaybackCompleted;

        public AudioPlaybackService() : this(new TtsService())
        {
        }

        public AudioPlaybackService(TtsService ttsService)
        {
            _ttsService = ttsService;
            _isPlaying = false;
        }

        public async Task PlayAudioAsync(AudioContent audioContent)
        {
            try
            {
                if (_currentlyPlaying != null)
                {
                    await StopAsync();
                }

                _currentlyPlaying = audioContent;
                _isPlaying = true;
                PlaybackStarted?.Invoke(this, audioContent);

                Debug.WriteLine($"Playing audio: {audioContent.Title} from {audioContent.AudioUrl}");

                // Nếu không có URL audio thật, fallback sang TTS đọc Title
                if (string.IsNullOrWhiteSpace(audioContent.AudioUrl))
                {
                    await _ttsService.SpeakAsync(audioContent.Title, audioContent.Language);
                }
                else
                {
                    // Simulate playback for 3 seconds
                    await Task.Delay(3000);
                }

                _isPlaying = false;
                if (_currentlyPlaying != null)
                {
                    PlaybackCompleted?.Invoke(this, _currentlyPlaying);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Playback error: {ex.Message}");
            }
        }

        public async Task PlayTextAsync(string text, string language, string title, string restaurantId = "")
        {
            var content = new AudioContent
            {
                RestaurantId = restaurantId,
                Language = language,
                ContentType = "history_tts",
                Title = title,
                AudioUrl = string.Empty
            };

            try
            {
                if (_currentlyPlaying != null)
                {
                    await StopAsync();
                }

                _currentlyPlaying = content;
                _isPlaying = true;
                PlaybackStarted?.Invoke(this, content);

                var textToSpeak = text;
                if (language.Equals("en", StringComparison.OrdinalIgnoreCase))
                {
                    textToSpeak = await TranslateToEnglishAsync(text) ?? text;
                }

                await _ttsService.SpeakAsync(textToSpeak, language);

                _isPlaying = false;
                PlaybackCompleted?.Invoke(this, content);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"PlayTextAsync error: {ex.Message}");
                _isPlaying = false;
            }
        }

        private async Task<string?> TranslateToEnglishAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            try
            {
                var url = $"{TranslationApiUrl}?q={Uri.EscapeDataString(text)}&langpair=vi|en";
                var response = await _httpClient.GetFromJsonAsync<MyMemoryResponse>(url);

                if (response?.ResponseStatus == 200 &&
                    !string.IsNullOrWhiteSpace(response.ResponseData?.TranslatedText))
                {
                    return response.ResponseData.TranslatedText;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TranslateToEnglishAsync error: {ex.Message}");
            }

            return null;
        }

        private sealed class MyMemoryResponse
        {
            public ResponseData? ResponseData { get; set; }
            public int ResponseStatus { get; set; }
        }

        private sealed class ResponseData
        {
            public string? TranslatedText { get; set; }
        }

        public void Pause()
        {
            try
            {
                if (_isPlaying)
                {
                    _ttsService.StopCurrent();
                    _isPlaying = false;
                    Debug.WriteLine("Audio paused");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Pause error: {ex.Message}");
            }
        }

        public void Resume()
        {
            // TTS không có resume đúng nghĩa, giữ nguyên API để không vỡ luồng cũ.
            Debug.WriteLine("Resume is not supported for current TTS playback.");
        }

        public Task StopAsync()
        {
            try
            {
                _ttsService.StopCurrent();
                _isPlaying = false;
                _currentlyPlaying = null;
                Debug.WriteLine("Audio stopped");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Stop error: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        public TimeSpan CurrentPosition => TimeSpan.Zero;
        public TimeSpan Duration => TimeSpan.Zero;
        public bool IsPlaying => _isPlaying;
    }
}