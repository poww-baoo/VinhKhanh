using System.Diagnostics;

namespace VinhKhanh.Services
{
    public class AudioContent
    {
        public string RestaurantId { get; set; }
        public string Language { get; set; }
        public string ContentType { get; set; }
        public string AudioUrl { get; set; }
        public string Title { get; set; }
    }

    public class AudioPlaybackService
    {
        private AudioContent _currentlyPlaying;
        private bool _isPlaying;

        public event EventHandler<AudioContent> PlaybackStarted;
        public event EventHandler<AudioContent> PlaybackCompleted;

        public AudioPlaybackService()
        {
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
                
                // Simulate playback for 3 seconds
                await Task.Delay(3000);
                
                _isPlaying = false;
                PlaybackCompleted?.Invoke(this, _currentlyPlaying);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Playback error: {ex.Message}");
            }
        }

        public void Pause()
        {
            try
            {
                if (_isPlaying)
                {
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
            try
            {
                if (_currentlyPlaying != null && !_isPlaying)
                {
                    _isPlaying = true;
                    Debug.WriteLine("Audio resumed");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Resume error: {ex.Message}");
            }
        }

        public async Task StopAsync()
        {
            try
            {
                _isPlaying = false;
                _currentlyPlaying = null;
                Debug.WriteLine("Audio stopped");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Stop error: {ex.Message}");
            }
        }

        public TimeSpan CurrentPosition => TimeSpan.Zero;
        public TimeSpan Duration => TimeSpan.Zero;
        public bool IsPlaying => _isPlaying;
    }
}