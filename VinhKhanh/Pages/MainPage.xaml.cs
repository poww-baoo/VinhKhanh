using System.Globalization;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using VinhKhanh.Models;
using VinhKhanh.Services;

namespace VinhKhanh.Pages
{
    public partial class MainPage : ContentPage
    {
        private const string TrackAsiaApiKey = "3a82d12156488a8391773657171aacb765";

        // Nếu SDK URL của TrackAsia docs khác, chỉ cần đổi 2 URL này
        private const string TrackAsiaCssUrl = "https://maps.track-asia.com/v1.0.0/trackasia-gl.css";
        private const string TrackAsiaJsUrl = "https://maps.track-asia.com/v1.0.0/trackasia-gl.js";

        // Fallback chắc chắn có sẵn
        private const string MapLibreCssUrl = "https://unpkg.com/maplibre-gl@4.7.1/dist/maplibre-gl.css";
        private const string MapLibreJsUrl = "https://unpkg.com/maplibre-gl@4.7.1/dist/maplibre-gl.js";

        private readonly DatabaseService _databaseService;
        private readonly LocationService _locationService;
        private readonly AudioPlaybackService _audioService;
        private readonly LocalizationService _localizationService;
        private readonly FirebaseSyncService? _firebaseSyncService;
        private readonly TranslationService _translationService;

        private List<Restaurant> _restaurants = new();
        private string _currentTab = "explore";
        private readonly ExplorePage _explorePage;
        private readonly SavedPage _savedPage;
        private readonly TrackingPage _trackingPage;
        private readonly SettingsPage _settingsPage;
        private readonly QRCodePage _qrcodePage;

        private View? _exploreContent;
        private View? _savedContent;
        private View? _trackingContent;
        private View? _settingsContent;
        private View? _qrcodeContent;

        private bool _isDataLoaded;

        public MainPage()
        {
            InitializeComponent();

            _databaseService = ResolveService<DatabaseService>() ?? new DatabaseService();
            _locationService = ResolveService<LocationService>() ?? new LocationService();
            _audioService = ResolveService<AudioPlaybackService>() ?? new AudioPlaybackService();
            _localizationService = LocalizationService.Instance;
            _firebaseSyncService = ResolveService<FirebaseSyncService>();
            _translationService = ResolveService<TranslationService>() ?? new TranslationService(_databaseService);

            _locationService.LocationUpdated += OnLocationUpdated;
            _locationService.EnteredGeofence += OnEnteredGeofence;
            _audioService.PlaybackCompleted += OnPlaybackCompleted;
            _localizationService.LanguageChanged += OnLocalizationLanguageChanged;
            if (_firebaseSyncService is not null)
            {
                _firebaseSyncService.SyncCompleted += OnFirebaseSyncCompleted;
            }

            _explorePage = new ExplorePage();
            _savedPage = new SavedPage();
            _trackingPage = new TrackingPage();
            _trackingPage.SetLocationService(_locationService);
            _qrcodePage = new QRCodePage();
            _settingsPage = new SettingsPage();

            CachePageContents();
            UpdateTabLabels();
            ShowTabContent("explore");

            Loaded += OnLoaded;
        }

        private static T? ResolveService<T>() where T : class =>
            Application.Current?.Handler?.MauiContext?.Services.GetService<T>();

        private async void OnLoaded(object? sender, EventArgs e)
        {
            if (_isDataLoaded) return;
            _isDataLoaded = true;
            await LoadDataFromDatabaseAsync();
        }

        private async Task LoadDataFromDatabaseAsync()
        {
            try
            {
                await _databaseService.InitAsync();

                var categories = await _databaseService.GetCategoriesAsync();
                _explorePage.SetCategories(categories);

                var pois = await _databaseService.GetAllPoisAsync();

                if (_localizationService.CurrentLanguage == "en")
                {
                    await _translationService.EnsureEnglishColumnsAsync(pois);
                    pois = await _databaseService.GetAllPoisAsync();
                }

                _restaurants = pois
                    .Select(MapPoiToRestaurant)
                    .OrderBy(r => r.Priority)
                    .ToList();

                _explorePage.SetRestaurants(_restaurants);
                _trackingPage.SetRestaurants(_restaurants);
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var language = _localizationService.CurrentLanguage;
                    await DisplayAlert(
                        _localizationService.GetString("DataError", language),
                        $"{_localizationService.GetString("CannotLoadData", language)}: {ex.Message}",
                        _localizationService.GetString("OK", language));
                });
            }
        }

        private static Restaurant MapPoiToRestaurant(Poi poi)
        {
            var highlights = string.IsNullOrWhiteSpace(poi.TextVi)
                ? poi.History
                : poi.TextVi;

            if (highlights.Length > 120)
                highlights = highlights[..120] + "...";

            return new Restaurant
            {
                Id = poi.Id.ToString(),
                CategoryId = poi.CategoryId,
                CategoryName = poi.CategoryName,
                Name = poi.Name,
                YearEstablished = poi.YearEstablished,
                History = poi.History,
                HistoryEn = poi.HistoryEn,
                Address = poi.Address,
                AdrEn = poi.AdrEn,
                TextVi = poi.TextVi,
                TextEn = poi.TextEn,
                TextZh = poi.TextZh,
                TextJa = poi.TextJa,
                TextRu = poi.TextRu,
                TextFr = poi.TextFr,
                Highlights = highlights,
                Rating = poi.Rating,
                Latitude = poi.Lat,
                Longitude = poi.Lng,
                GeofenceRadius = poi.RadiusMeters,
                Priority = poi.Priority,
                ImageFileName = poi.ImageFileName
            };
        }

        private void CachePageContents()
        {
            _exploreContent = _explorePage.Content;
            _savedContent = _savedPage.Content;
            _trackingContent = _trackingPage.Content;
            _qrcodeContent = _qrcodePage.Content;
            _settingsContent = _settingsPage.Content;

            _explorePage.Content = null;
            _savedPage.Content = null;
            _trackingPage.Content = null;
            _qrcodePage.Content = null;
            _settingsPage.Content = null;
        }

        private static string EscapeJs(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }

        private void OnTabTapped(object sender, TappedEventArgs e)
        {
            var tab = e.Parameter as string;

            if (tab == null) return;

            _currentTab = tab;
            UpdateTabUI();
            ShowTabContent(tab);
        }

        private void UpdateTabUI()
        {
            ExploreTab.Opacity = _currentTab == "explore" ? 1.0 : 0.5;
            SavedTab.Opacity = _currentTab == "saved" ? 1.0 : 0.5;
            TrackingTab.Opacity = _currentTab == "tracking" ? 1.0 : 0.5;
            QRCodeTab.Opacity = _currentTab == "qrcode" ? 1.0 : 0.5;
            SettingsTab.Opacity = _currentTab == "settings" ? 1.0 : 0.5;

            var exploreLabelTextColor = _currentTab == "explore" ? Color.FromArgb("#FF6B35") : Color.FromArgb("#1F1F1F");
            var savedLabelTextColor = _currentTab == "saved" ? Color.FromArgb("#FF6B35") : Color.FromArgb("#666666");
            var trackingLabelTextColor = _currentTab == "tracking" ? Color.FromArgb("#FF6B35") : Color.FromArgb("#666666");
            var qrcodeLabelTextColor = _currentTab == "qrcode" ? Color.FromArgb("#FF6B35") : Color.FromArgb("#666666");
            var settingsLabelTextColor = _currentTab == "settings" ? Color.FromArgb("#FF6B35") : Color.FromArgb("#666666");

            ((Label)ExploreTab.Children[1]).TextColor = exploreLabelTextColor;
            ((Label)SavedTab.Children[1]).TextColor = savedLabelTextColor;
            ((Label)TrackingTab.Children[1]).TextColor = trackingLabelTextColor;
            ((Label)QRCodeTab.Children[1]).TextColor = qrcodeLabelTextColor;
            ((Label)SettingsTab.Children[1]).TextColor = settingsLabelTextColor;
        }

        private void ShowTabContent(string tab)
        {
            ContentFrame.Children.Clear();

            try
            {
                var contentToShow = tab switch
                {
                    "explore" => _exploreContent,
                    "saved" => _savedContent,
                    "tracking" => _trackingContent,
                    "qrcode" => _qrcodeContent,
                    "settings" => _settingsContent,
                    _ => _exploreContent
                };

                if (contentToShow != null)
                {
                    ContentFrame.Children.Add(contentToShow);
                }

                if (tab == "saved")
                {
                    _ = _savedPage.RefreshSavedRestaurantsAsync();
                }
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    var language = _localizationService.CurrentLanguage;
                    await DisplayAlert(
                        _localizationService.GetString("Error", language),
                        $"{_localizationService.GetString("CannotLoadPage", language)}: {ex.Message}",
                        _localizationService.GetString("OK", language));
                });
            }
        }

        private void OnLocationUpdated(object? sender, Location location)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _trackingPage.UpdateLocation(location);
            });
        }

        private async void OnEnteredGeofence(object? sender, Restaurant restaurant)
        {
            var language = _localizationService.CurrentLanguage;
            var ttsText = restaurant.GetTextByLanguage(language);
            var playingText = _localizationService.GetString("Tracking_Status_Playing", language);

            await _audioService.PlayTextAsync(
                ttsText,
                language,
                restaurant.Name,
                restaurant.Id);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _trackingPage.UpdateStatus($"{playingText}: {restaurant.Name}");
            });
        }

        private void OnPlaybackCompleted(object? sender, AudioContent e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var language = _localizationService.CurrentLanguage;
                var completedText = _localizationService.GetString("Tracking_Status_Completed", language);
                _trackingPage.UpdateStatus(completedText);
            });
        }

        private void OnFirebaseSyncCompleted(object? sender, int version)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await LoadDataFromDatabaseAsync();
            });
        }

        private async void OnLocalizationLanguageChanged(object? sender, EventArgs e)
        {
            UpdateTabLabels();
            await LoadDataFromDatabaseAsync();
        }

        private void UpdateTabLabels()
        {
            var language = _localizationService.CurrentLanguage;
            
            ((Label)ExploreTab.Children[1]).Text = _localizationService.GetString("Explore", language);
            ((Label)SavedTab.Children[1]).Text = _localizationService.GetString("Saved", language);
            ((Label)TrackingTab.Children[1]).Text = _localizationService.GetString("Tracking", language);
            ((Label)QRCodeTab.Children[1]).Text = _localizationService.GetString("QRCode", language);
            ((Label)SettingsTab.Children[1]).Text = _localizationService.GetString("Settings", language);
        }
    }
}