using System.Collections.ObjectModel;
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
        private const string TrackAsiaCssUrl = "https://maps.track-asia.com/v1.0.0/trackasia-gl.css";
        private const string TrackAsiaJsUrl = "https://maps.track-asia.com/v1.0.0/trackasia-gl.js";
        private const string MapLibreCssUrl = "https://unpkg.com/maplibre-gl@4.7.1/dist/maplibre-gl.css";
        private const string MapLibreJsUrl = "https://unpkg.com/maplibre-gl@4.7.1/dist/maplibre-gl.js";
        private const double AutoPlayNearestDistanceMeters = 180;
        private const int AutoPlayCooldownMinutes = 0;

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
        private bool _isTrackingStarted = false;
        private string? _lastAutoPlayedRestaurantId;
        private DateTime _lastAutoPlayedAt = DateTime.MinValue;

        // Tracking TTS queue + popup
        private readonly Queue<Restaurant> _trackingNarrationQueue = new();
        private readonly HashSet<string> _queuedOrPlayingRestaurantIds = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<Restaurant> _pendingPopupRestaurants = new();
        private readonly SemaphoreSlim _trackingNarrationLock = new(1, 1);
        private CancellationTokenSource? _queuePopupDebounceCts;
        private int _trackingQueueWorkerState;
        private readonly ObservableCollection<Restaurant> _popupNarrationItems = new();
        private readonly Dictionary<string, Restaurant> _insideGeofenceRestaurants = new(StringComparer.OrdinalIgnoreCase);
        private Location? _latestTrackingLocation;
        private Restaurant? _currentNarrationRestaurant;
        private bool _isNarrationQueueExpanded;
        private bool _isNarrationPopupMinimized;

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
            _locationService.ExitedGeofence += OnExitedGeofence;
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

            TrackingNarrationQueueCollection.ItemsSource = _popupNarrationItems;
            SetNarrationQueueExpanded(false);
            SetNarrationPopupMinimized(false);
            UpdateTrackingNarrationPopupTexts();

            Loaded += OnLoaded;
        }

        private static T? ResolveService<T>() where T : class =>
            Application.Current?.Handler?.MauiContext?.Services.GetService<T>();

        private async void OnLoaded(object? sender, EventArgs e)
        {
            if (_isDataLoaded) return;
            _isDataLoaded = true;
            await LoadDataFromDatabaseAsync();

            InitializeTracking();
        }

        private async Task LoadDataFromDatabaseAsync()
        {
            try
            {
                await _databaseService.InitAsync();

                var categories = await _databaseService.GetCategoriesAsync();
                _explorePage.SetCategories(categories);

                var pois = await _databaseService.GetAllPoisAsync();

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

        private void InitializeTracking()
        {
            if (_isTrackingStarted || _restaurants.Count == 0)
                return;

            _isTrackingStarted = true;

            try
            {
                _ = _locationService.StartTrackingAsync(_restaurants);
                _trackingPage.EnableTrackingUI();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] Error starting tracking: {ex.Message}");
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
                HistoryJp = poi.HistoryJp,
                HistoryZh = poi.HistoryZh,
                HistoryRu = poi.HistoryRu,
                HistoryFr = poi.HistoryFr,

                Address = poi.Address,
                AdrEn = poi.AdrEn,
                AdrJp = poi.AdrJp,
                AdrZh = poi.AdrZh,
                AdrRu = poi.AdrRu,
                AdrFr = poi.AdrFr,

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

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (_currentTab == "qrcode")
            {
                _ = _qrcodePage.ActivateFromHostAsync();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Khi rời MainPage (vd: push detail), dừng scanner để tránh state treo camera
            _qrcodePage.DeactivateFromHost();
        }

        private void OnTabTapped(object sender, TappedEventArgs e)
        {
            var tab = e.Parameter as string;

            if (tab == null) return;

            // Bấm lại chính tab hiện tại: không re-render để tránh lỗi UI,
            // riêng tab QR thì restart scanner nhẹ để user quét lại ngay.
            if (tab == _currentTab)
            {
                if (tab == "qrcode")
                {
                    _ = _qrcodePage.ActivateFromHostAsync();
                }
                return;
            }

            if (_currentTab == "qrcode")
            {
                _qrcodePage.DeactivateFromHost();
            }

            _currentTab = tab;
            UpdateTabUI();
            ShowTabContent(tab);

            if (tab == "qrcode")
            {
                _ = _qrcodePage.ActivateFromHostAsync();
            }
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
            _latestTrackingLocation = location;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _trackingPage.UpdateLocation(location);
                RefreshPopupFromInsideGeofences();
            });

            // Không phát trực tiếp ở đây, dùng luồng geofence + queue để có popup danh sách.
        }

        private async void OnEnteredGeofence(object? sender, Restaurant restaurant)
        {
            await _trackingNarrationLock.WaitAsync();
            try
            {
                if (!string.IsNullOrWhiteSpace(restaurant?.Id))
                {
                    _insideGeofenceRestaurants[restaurant.Id] = restaurant;
                }
            }
            finally
            {
                _trackingNarrationLock.Release();
            }

            await MainThread.InvokeOnMainThreadAsync(RefreshPopupFromInsideGeofences);
            await EnqueueTrackingNarrationAsync(restaurant);
        }

        private async void OnExitedGeofence(object? sender, Restaurant restaurant)
        {
            if (restaurant is null || string.IsNullOrWhiteSpace(restaurant.Id))
            {
                return;
            }

            await _trackingNarrationLock.WaitAsync();
            try
            {
                _insideGeofenceRestaurants.Remove(restaurant.Id);
                _pendingPopupRestaurants.RemoveAll(x => string.Equals(x.Id, restaurant.Id, StringComparison.OrdinalIgnoreCase));
                _queuedOrPlayingRestaurantIds.Remove(restaurant.Id);

                if (_trackingNarrationQueue.Count > 0)
                {
                    var kept = _trackingNarrationQueue
                        .Where(x => !string.Equals(x.Id, restaurant.Id, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    _trackingNarrationQueue.Clear();
                    foreach (var item in kept)
                    {
                        _trackingNarrationQueue.Enqueue(item);
                    }
                }
            }
            finally
            {
                _trackingNarrationLock.Release();
            }

            await MainThread.InvokeOnMainThreadAsync(RefreshPopupFromInsideGeofences);
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
            UpdateTrackingNarrationPopupTexts();
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

        private async Task EnqueueTrackingNarrationAsync(Restaurant restaurant)
        {
            if (restaurant is null || string.IsNullOrWhiteSpace(restaurant.Id))
            {
                return;
            }

            var added = false;

            await _trackingNarrationLock.WaitAsync();
            try
            {
                if (!_queuedOrPlayingRestaurantIds.Contains(restaurant.Id))
                {
                    _queuedOrPlayingRestaurantIds.Add(restaurant.Id);
                    _pendingPopupRestaurants.Add(restaurant);
                    added = true;
                }
            }
            finally
            {
                _trackingNarrationLock.Release();
            }

            if (!added)
            {
                return;
            }

            _ = ShowTrackingQueuePopupDebouncedAsync();
        }

        private void UpdateTrackingNarrationPopupTexts()
        {
            var language = _localizationService.CurrentLanguage;

            if (TrackingNarrationSubtitleLabel is not null)
            {
                TrackingNarrationSubtitleLabel.Text = language == "en"
                    ? "Default order: Priority, then nearest distance"
                    : "Mặc định: Priority trước, rồi POI gần hơn";
            }

            UpdateQueueCountLabel();
            UpdateCurrentNarrationMiniInfo(_currentNarrationRestaurant);
        }

        private void SetNarrationQueueExpanded(bool expanded)
        {
            _isNarrationQueueExpanded = expanded;

            if (TrackingNarrationQueuePanel is not null)
            {
                TrackingNarrationQueuePanel.IsVisible = expanded && !_isNarrationPopupMinimized;
            }

            if (TrackingNarrationToggleButton is not null)
            {
                TrackingNarrationToggleButton.Text = expanded ? "▾" : "▸";
            }
        }

        private void SetNarrationPopupMinimized(bool minimized)
        {
            _isNarrationPopupMinimized = minimized;

            if (TrackingNarrationMainCard is not null)
            {
                TrackingNarrationMainCard.IsVisible = !minimized;
            }

            if (TrackingNarrationMiniBubble is not null)
            {
                TrackingNarrationMiniBubble.IsVisible = minimized;
            }

            if (minimized)
            {
                SetNarrationQueueExpanded(false);
            }

            TrackingNarrationOverlay.IsVisible = true;
            UpdateMiniBubbleBadge();
        }

        private void UpdateMiniBubbleBadge()
        {
            if (TrackingNarrationBubbleCountLabel is null)
            {
                return;
            }

            TrackingNarrationBubbleCountLabel.Text = _popupNarrationItems.Count.ToString();
        }

        private void OnMinimizeTrackingNarrationClicked(object sender, EventArgs e)
        {
            SetNarrationPopupMinimized(true);
        }

        private void OnRestoreTrackingNarrationClicked(object? sender, TappedEventArgs e)
        {
            SetNarrationPopupMinimized(false);
            SetNarrationQueueExpanded(true);
        }

        private void OnToggleTrackingNarrationQueueClicked(object sender, EventArgs e)
        {
            if (_isNarrationPopupMinimized)
            {
                SetNarrationPopupMinimized(false);
                SetNarrationQueueExpanded(true);
                return;
            }

            SetNarrationQueueExpanded(!_isNarrationQueueExpanded);
        }

        private void UpdateQueueCountLabel()
        {
            if (TrackingNarrationHintLabel is null)
            {
                return;
            }

            var language = _localizationService.CurrentLanguage;
            TrackingNarrationHintLabel.Text = language == "en"
                ? $"{_popupNarrationItems.Count} item(s)"
                : $"{_popupNarrationItems.Count} mục";

            UpdateMiniBubbleBadge();
        }

        private void UpdateCurrentNarrationMiniInfo(Restaurant? current)
        {
            if (TrackingNarrationCurrentTitleLabel is null || TrackingNarrationCurrentSubtitleLabel is null)
            {
                return;
            }

            var language = _localizationService.CurrentLanguage;

            if (current is null)
            {
                TrackingNarrationCurrentTitleLabel.Text = language == "en" ? "No narration playing" : "Chưa phát thuyết minh";
                TrackingNarrationCurrentSubtitleLabel.Text = language == "en"
                    ? "Press Play to start queue"
                    : "Bấm Phát để bắt đầu danh sách";
                return;
            }

            TrackingNarrationCurrentTitleLabel.Text = current.Name;
            TrackingNarrationCurrentSubtitleLabel.Text = language == "en"
                ? $"Narration language: {language.ToUpperInvariant()}"
                : $"Ngôn ngữ thuyết minh: {language.ToUpperInvariant()}";
        }

        private List<Restaurant> SortRestaurantsByPriorityThenDistance(IEnumerable<Restaurant> restaurants)
        {
            return restaurants
                .OrderBy(r => r.Priority)
                .ThenBy(r => _latestTrackingLocation is null
                    ? double.MaxValue
                    : GetDistanceInMeters(
                        _latestTrackingLocation.Latitude,
                        _latestTrackingLocation.Longitude,
                        r.Latitude,
                        r.Longitude))
                .ThenBy(r => r.Name)
                .ToList();
        }

        private void RefreshPopupNarrationItems(IEnumerable<Restaurant> items)
        {
            var sorted = SortRestaurantsByPriorityThenDistance(items);

            _popupNarrationItems.Clear();
            foreach (var item in sorted)
            {
                _popupNarrationItems.Add(item);
            }

            UpdateQueueCountLabel();
        }

        private void RefreshPopupFromInsideGeofences()
        {
            RefreshPopupNarrationItems(_insideGeofenceRestaurants.Values);
        }

        private async Task ShowTrackingQueuePopupDebouncedAsync()
        {
            _queuePopupDebounceCts?.Cancel();
            _queuePopupDebounceCts = new CancellationTokenSource();
            var token = _queuePopupDebounceCts.Token;

            try
            {
                await Task.Delay(650, token);

                List<Restaurant> popupItems;
                await _trackingNarrationLock.WaitAsync(token);
                try
                {
                    popupItems = _pendingPopupRestaurants
                        .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                        .Select(g => g.First())
                        .ToList();

                    _pendingPopupRestaurants.Clear();
                }
                finally
                {
                    _trackingNarrationLock.Release();
                }

                if (popupItems.Count == 0)
                {
                    return;
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var merged = _popupNarrationItems
                        .Concat(popupItems)
                        .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                        .Select(g => g.First())
                        .ToList();

                    // luôn giữ item đang inside geofence + item vừa thêm
                    var insideMerged = _insideGeofenceRestaurants.Values
                        .Concat(merged)
                        .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                        .Select(g => g.First())
                        .ToList();

                    RefreshPopupNarrationItems(insideMerged);
                    TrackingNarrationOverlay.IsVisible = true;
                    if (!_isNarrationPopupMinimized)
                    {
                        SetNarrationQueueExpanded(true);
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // có item mới vào queue, chờ lần debounce mới
            }
        }

        private async void OnPlayTrackingNarrationClicked(object sender, EventArgs e)
        {
            var selectedOrder = _popupNarrationItems.ToList();
            if (selectedOrder.Count == 0)
            {
                TrackingNarrationOverlay.IsVisible = true;
                SetNarrationQueueExpanded(false);
                return;
            }

            SetNarrationQueueExpanded(false);

            await _trackingNarrationLock.WaitAsync();
            try
            {
                foreach (var item in selectedOrder)
                {
                    if (string.IsNullOrWhiteSpace(item.Id))
                    {
                        continue;
                    }

                    if (_trackingNarrationQueue.All(x => !string.Equals(x.Id, item.Id, StringComparison.OrdinalIgnoreCase)))
                    {
                        _trackingNarrationQueue.Enqueue(item);
                    }

                    _queuedOrPlayingRestaurantIds.Add(item.Id);
                }
            }
            finally
            {
                _trackingNarrationLock.Release();
            }

            TrackingNarrationOverlay.IsVisible = true;
            _ = ProcessTrackingNarrationQueueAsync();
        }

        private async void OnSkipTrackingNarrationClicked(object sender, EventArgs e)
        {
            await _trackingNarrationLock.WaitAsync();
            try
            {
                _pendingPopupRestaurants.Clear();
                _trackingNarrationQueue.Clear();
            }
            finally
            {
                _trackingNarrationLock.Release();
            }

            // Giữ mini-player + danh sách POI đang còn trong vùng, chỉ bỏ qua queue phát hiện tại.
            TrackingNarrationOverlay.IsVisible = true;
            RefreshPopupFromInsideGeofences();
            SetNarrationQueueExpanded(false);
        }

        private async void OnNextTrackingNarrationClicked(object sender, EventArgs e)
        {
            if (!_audioService.IsPlaying)
            {
                // nếu chưa phát mà đang có queue thì cho chạy luôn mục kế tiếp
                _ = ProcessTrackingNarrationQueueAsync();
                return;
            }

            await _audioService.StopAsync();
        }

        private void OnPrioritizeNarrationPoiTapped(object sender, EventArgs e)
        {
            if (sender is not Button { CommandParameter: Restaurant restaurant })
            {
                return;
            }

            MovePopupRestaurantToTop(restaurant);
            UpdateQueueCountLabel();
        }

        private void OnMoveNarrationUpClicked(object sender, EventArgs e)
        {
            if (sender is not Button { CommandParameter: Restaurant restaurant })
            {
                return;
            }

            var index = _popupNarrationItems.IndexOf(restaurant);
            if (index <= 0)
            {
                return;
            }

            _popupNarrationItems.Move(index, index - 1);
        }

        private void OnMoveNarrationDownClicked(object sender, EventArgs e)
        {
            if (sender is not Button { CommandParameter: Restaurant restaurant })
            {
                return;
            }

            var index = _popupNarrationItems.IndexOf(restaurant);
            if (index < 0 || index >= _popupNarrationItems.Count - 1)
            {
                return;
            }

            _popupNarrationItems.Move(index, index + 1);
        }

        private void MovePopupRestaurantToTop(Restaurant restaurant)
        {
            var index = _popupNarrationItems.IndexOf(restaurant);
            if (index <= 0)
            {
                return;
            }

            _popupNarrationItems.Move(index, 0);
        }

        private async Task ProcessTrackingNarrationQueueAsync()
        {
            if (Interlocked.Exchange(ref _trackingQueueWorkerState, 1) == 1)
            {
                return;
            }

            try
            {
                while (true)
                {
                    Restaurant? next = null;

                    await _trackingNarrationLock.WaitAsync();
                    try
                    {
                        if (_trackingNarrationQueue.Count > 0)
                        {
                            next = _trackingNarrationQueue.Dequeue();
                        }
                    }
                    finally
                    {
                        _trackingNarrationLock.Release();
                    }

                    if (next is null)
                    {
                        break;
                    }

                    _currentNarrationRestaurant = next;
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        TrackingNarrationOverlay.IsVisible = true;
                        UpdateCurrentNarrationMiniInfo(next);
                    });

                    var language = _localizationService.CurrentLanguage;
                    var playingText = _localizationService.GetString("Tracking_Status_Playing", language);

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        _trackingPage.UpdateStatus($"{playingText}: {next.Name}");
                    });

                    var ttsText = next.GetTextByLanguage(language);
                    if (string.IsNullOrWhiteSpace(ttsText))
                    {
                        ttsText = next.GetHistoryByLanguage(language);
                    }

                    if (string.IsNullOrWhiteSpace(ttsText))
                    {
                        ttsText = language switch
                        {
                            "en" => $"You have arrived at {next.Name}",
                            "zh" => $"您已到达 {next.Name}",
                            "ja" => $"{next.Name} に到着しました",
                            "ru" => $"Вы прибыли в {next.Name}",
                            "fr" => $"Vous êtes arrivé à {next.Name}",
                            _ => $"Bạn đã đến {next.Name} rồi"
                        };
                    }

                    await _audioService.PlayTextAsync(
                        ttsText,
                        language,
                        next.Name,
                        next.Id);

                    await _trackingNarrationLock.WaitAsync();
                    try
                    {
                        _queuedOrPlayingRestaurantIds.Remove(next.Id);
                    }
                    finally
                    {
                        _trackingNarrationLock.Release();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TrackingNarrationQueue] {ex.Message}");
            }
            finally
            {
                _currentNarrationRestaurant = null;
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    UpdateCurrentNarrationMiniInfo(null);
                    TrackingNarrationOverlay.IsVisible = true;
                    if (_popupNarrationItems.Count == 0)
                    {
                        SetNarrationQueueExpanded(false);
                    }
                });

                Interlocked.Exchange(ref _trackingQueueWorkerState, 0);

                var hasPending = false;
                await _trackingNarrationLock.WaitAsync();
                try
                {
                    hasPending = _trackingNarrationQueue.Count > 0;
                }
                finally
                {
                    _trackingNarrationLock.Release();
                }

                if (hasPending)
                {
                    _ = ProcessTrackingNarrationQueueAsync();
                }
            }
        }

        private static double GetDistanceInMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadius = 6371000;
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadius * c;
        }
    }
}