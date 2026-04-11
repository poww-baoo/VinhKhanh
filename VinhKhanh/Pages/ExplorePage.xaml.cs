using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Networking;
using VinhKhanh.Models;
using VinhKhanh.Services;

namespace VinhKhanh.Pages;

public partial class ExplorePage : ContentPage
{
    private const int AllCategoryId = -1;
    private const string AllCategoryDefaultName = "Tất cả";
    private const string BaseCategoryLanguage = "vi";
    private const string TrackAsiaApiKey = "3a82d12156488a8391773657171aacb765";
    private const string TrackAsiaCssUrl = "https://maps.track-asia.com/v1.0.0/trackasia-gl.css";
    private const string TrackAsiaJsUrl = "https://maps.track-asia.com/v1.0.0/trackasia-gl.js";
    private const string MapLibreCssUrl = "https://unpkg.com/maplibre-gl@4.7.1/dist/maplibre-gl.css";
    private const string MapLibreJsUrl = "https://unpkg.com/maplibre-gl@4.7.1/dist/maplibre-gl.js";
    private const string MapHtmlCacheFileName = "explore-map-cache.html";

    private readonly AudioPlaybackService _audioService;
    private readonly LocalizationService _localizationService;
    private readonly DatabaseService _databaseService;
    private readonly FirebaseSyncService _firebaseSyncService;
    private readonly ImageSyncService _imageSyncService;

    private readonly List<Restaurant> _allRestaurants = new();
    private readonly List<Restaurant> _filteredRestaurants = new();
    private readonly List<Category> _categories = new();

    private int? _selectedCategoryId;
    private string _searchKeyword = string.Empty;
    private CancellationTokenSource? _searchDebounceCts;
    private readonly Dictionary<int, string> _sourceCategoryNames = new();

    private Location? _currentLocation;
    private string _currentLocationDisplay = string.Empty;
    private readonly string _mapHtmlCachePath;

    // Local category dictionary (không gọi API, không delay)
    private static readonly Dictionary<string, Dictionary<string, string>> CategoryTranslations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = new(StringComparer.OrdinalIgnoreCase)
        {
            [NormalizeCategoryKey("Tất cả")] = "All",
            [NormalizeCategoryKey("Món nước")] = "Soup/Noodle",
            [NormalizeCategoryKey("Món khô")] = "Dry dishes",
            [NormalizeCategoryKey("Ăn vặt")] = "Snacks",
            [NormalizeCategoryKey("Ăn Vặt - Nước")] = "Snacks & Drinks",
            [NormalizeCategoryKey("Đồ uống")] = "Drinks",
            [NormalizeCategoryKey("Tráng miệng")] = "Desserts",
        },
        ["zh"] = new(StringComparer.OrdinalIgnoreCase)
        {
            [NormalizeCategoryKey("Tất cả")] = "全部",
            [NormalizeCategoryKey("Ăn Vặt - Nước")] = "小吃与饮品",
            [NormalizeCategoryKey("Lẩu - Nướng")] = "火锅与烧烤",
            [NormalizeCategoryKey("Ốc - Hải sản")] = "螺类与海鲜",
            [NormalizeCategoryKey("Tráng miệng")] = "甜点",
        },
        ["ja"] = new(StringComparer.OrdinalIgnoreCase)
        {
            [NormalizeCategoryKey("Tất cả")] = "すべて",
            [NormalizeCategoryKey("Ăn Vặt - Nước")] = "軽食・ドリンク",
            [NormalizeCategoryKey("Lẩu - Nướng")] = "鍋・焼き物",
            [NormalizeCategoryKey("Ốc - Hải sản")] = "巻貝・シーフード",
            [NormalizeCategoryKey("Tráng miệng")] = "デザート",
        },
        ["ru"] = new(StringComparer.OrdinalIgnoreCase)
        {
            [NormalizeCategoryKey("Tất cả")] = "Все",
            [NormalizeCategoryKey("Ăn Vặt - Nước")] = "Закуски и напитки",
            [NormalizeCategoryKey("Lẩu - Nướng")] = "Хотпот и гриль",
            [NormalizeCategoryKey("Ốc - Hải sản")] = "Улитки и морепродукты",
            [NormalizeCategoryKey("Tráng miệng")] = "Десерты",
        },
        ["fr"] = new(StringComparer.OrdinalIgnoreCase)
        {
            [NormalizeCategoryKey("Tất cả")] = "Tous",
            [NormalizeCategoryKey("Ăn Vặt - Nước")] = "Snacks et boissons",
            [NormalizeCategoryKey("Lẩu - Nướng")] = "Fondue et grillades",
            [NormalizeCategoryKey("Ốc - Hải sản")] = "Escargots et fruits de mer",
            [NormalizeCategoryKey("Tráng miệng")] = "Desserts",
        }
    };

    public ExplorePage()
    {
        InitializeComponent();
        _audioService = new AudioPlaybackService();
        _localizationService = LocalizationService.Instance;
        _databaseService = ResolveService<DatabaseService>() ?? new DatabaseService();

        _imageSyncService = ResolveService<ImageSyncService>() ?? new ImageSyncService();
        _firebaseSyncService = ResolveService<FirebaseSyncService>() ?? new FirebaseSyncService(_databaseService, _imageSyncService);
        _mapHtmlCachePath = Path.Combine(FileSystem.CacheDirectory, MapHtmlCacheFileName);

        _localizationService.LanguageChanged += OnLanguageChangedEvent;
        UpdateUI();
        RenderTrackAsiaMap();
        _ = LoadCurrentLocationAsync();
    }

    private void OnLanguageChangedEvent(object? sender, EventArgs e)
    {
        ApplyRestaurantLocalization();
        ApplyCategoryLocalization();
        UpdateUI();
    }

    public void SetCategories(List<Category> categories)
    {
        _categories.Clear();

        _categories.Add(new Category
        {
            Id = AllCategoryId,
            Name = AllCategoryDefaultName,
            IconText = "🍽️",
            SortOrder = int.MinValue
        });

        if (categories is { Count: > 0 })
        {
            _categories.AddRange(categories.OrderBy(c => c.SortOrder));
        }

        _sourceCategoryNames.Clear();
        foreach (var category in _categories)
        {
            _sourceCategoryNames[category.Id] = category.Name;
        }

        _selectedCategoryId = AllCategoryId;
        UpdateCategorySelectionState();

        ApplyCategoryLocalization();
        RefreshCategoryItemsSource();
        ApplyFilters();
    }

    private static string NormalizeLanguageCode(string? language)
    {
        var normalized = (language ?? BaseCategoryLanguage).Trim().ToLowerInvariant();
        return normalized switch
        {
            "jp" => "ja",
            _ => normalized
        };
    }

    private string GetLocalizedCategoryName(string sourceName, string language)
    {
        var normalizedLang = NormalizeLanguageCode(language);
        if (normalizedLang == BaseCategoryLanguage)
        {
            return sourceName;
        }

        var normalizedKey = NormalizeCategoryKey(sourceName);

        if (CategoryTranslations.TryGetValue(normalizedLang, out var map) &&
            map.TryGetValue(normalizedKey, out var translated) &&
            !string.IsNullOrWhiteSpace(translated))
        {
            return translated;
        }

        return sourceName;
    }

    private void ApplyCategoryLocalization()
    {
        if (_categories.Count == 0 || _sourceCategoryNames.Count == 0)
        {
            return;
        }

        var language = NormalizeLanguageCode(_localizationService.CurrentLanguage);

        foreach (var category in _categories)
        {
            if (!_sourceCategoryNames.TryGetValue(category.Id, out var sourceName))
            {
                sourceName = category.Name;
            }

            category.Name = GetLocalizedCategoryName(sourceName, language);
        }

        foreach (var restaurant in _allRestaurants)
        {
            if (_sourceCategoryNames.TryGetValue(restaurant.CategoryId, out var sourceName))
            {
                restaurant.CategoryName = GetLocalizedCategoryName(sourceName, language);
            }
        }

        RefreshCategoryItemsSource();
        ApplyFilters();
    }

    private async Task LoadCurrentLocationAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status != PermissionStatus.Granted)
            {
                return;
            }

            // Ưu tiên vị trí real-time trước để tránh LastKnown cũ/sai (vd: California)
            var location = await Geolocation.GetLocationAsync(new GeolocationRequest(
                           GeolocationAccuracy.Best,
                           TimeSpan.FromSeconds(10)))
                       ?? await Geolocation.GetLastKnownLocationAsync();

            if (location is null)
            {
                return;
            }

            // Loại bỏ vị trí quá cũ để tránh hiển thị sai khu vực
            if (location.Timestamp is DateTimeOffset ts &&
                DateTimeOffset.UtcNow - ts > TimeSpan.FromMinutes(15))
            {
                System.Diagnostics.Debug.WriteLine("[Explore Location] Skip stale location.");
                return;
            }

            _currentLocation = location;
            _currentLocationDisplay = await GetAddressDisplayAsync(location);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                UpdateUI();
                RenderTrackAsiaMap();
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Explore Location] {ex.Message}");
        }
    }

    private static async Task<string> GetAddressDisplayAsync(Location location)
    {
        try
        {
            var marks = await Geocoding.Default.GetPlacemarksAsync(location.Latitude, location.Longitude);
            var place = marks?.FirstOrDefault();
            if (place is not null)
            {
                var parts = new[]
                {
                    place.Thoroughfare,
                    place.SubLocality,
                    place.Locality,
                    place.AdminArea
                }
                .Where(p => !string.IsNullOrWhiteSpace(p));

                var text = string.Join(", ", parts);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }
        }
        catch
        {
            // fallback to coordinates
        }

        return $"{location.Latitude:F5}, {location.Longitude:F5}";
    }

    private static T? ResolveService<T>() where T : class =>
        Application.Current?.Handler?.MauiContext?.Services.GetService<T>();
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
            DisplayAddress = poi.Address,
            DisplayHighlights = highlights,
            Rating = poi.Rating,
            Latitude = poi.Lat,
            Longitude = poi.Lng,
            GeofenceRadius = poi.RadiusMeters,
            Priority = poi.Priority,
            ImageFileName = poi.ImageFileName
        };
    }

    private static string BuildHighlightPreview(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var value = text.Trim();
        return value.Length > 120 ? value[..120] + "..." : value;
    }

    private void ApplyRestaurantLocalization()
    {
        var language = NormalizeLanguageCode(_localizationService.CurrentLanguage);
        var yearLabel = _localizationService.GetString("YearEstablished", language);

        foreach (var restaurant in _allRestaurants)
        {
            var localizedAddress = restaurant.GetAddressByLanguage(language);
            restaurant.DisplayAddress = string.IsNullOrWhiteSpace(localizedAddress)
                ? restaurant.Address
                : localizedAddress;

            var localizedText = restaurant.GetTextByLanguage(language);
            if (string.IsNullOrWhiteSpace(localizedText))
            {
                localizedText = restaurant.GetHistoryByLanguage(language);
            }

            restaurant.DisplayHighlights = BuildHighlightPreview(localizedText);
            restaurant.DisplayYearEstablished = $"⏱️ {yearLabel} {restaurant.YearEstablished}";
        }
    }

    private void UpdateUI()
    {
        var language = _localizationService.CurrentLanguage;

        Title = _localizationService.GetString("Explore", language);
        SearchEntry.Placeholder = _localizationService.GetString("SearchPlaceholder", language);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                // Header labels
                if (FindByName("StreetFoodLabel") is Label streetFoodLabel)
                    streetFoodLabel.Text = _localizationService.GetString("StreetFood", language);

                // Map section
                if (FindByName("MapAreaLabel") is Label mapAreaLabel)
                    mapAreaLabel.Text = _localizationService.GetString("MapArea", language);

                // Categories section
                if (FindByName("CategoriesLabel") is Label categoriesLabel)
                    categoriesLabel.Text = _localizationService.GetString("Categories", language);

                // Restaurants header
                if (FindByName("RestaurantsNearYouLabel") is Label restaurantsLabel)
                    restaurantsLabel.Text = _localizationService.GetString("RestaurantsNearYou", language);

                // Current location
                if (FindByName("LocationInfoLabel") is Label locationLabel)
                    locationLabel.Text = _localizationService.GetString("CurrentLocation", language);

                // Current location value
                if (FindByName("CurrentLocationValueLabel") is Label locationValueLabel)
                    locationValueLabel.Text = string.IsNullOrWhiteSpace(_currentLocationDisplay)
                        ? _localizationService.GetString("WaitingGPS", language)
                        : _currentLocationDisplay;

                // Cập nhật button texts trong restaurant cards
                UpdateRestaurantButtonTexts();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating UI: {ex.Message}");
            }
        });
    }

    public void SetRestaurants(List<Restaurant> restaurants)
    {
        _allRestaurants.Clear();
        if (restaurants is { Count: > 0 })
        {
            foreach (var restaurant in restaurants)
            {
                restaurant.DisplayImage = _imageSyncService.GetLocalPath(restaurant.ImageFileName);
                _allRestaurants.Add(restaurant);
            }
        }

        ApplyRestaurantLocalization();
        ApplyCategoryLocalization(); // thêm dòng này
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        IEnumerable<Restaurant> query = _allRestaurants;

        if (_selectedCategoryId.HasValue)
        {
            var selectedCategory = _categories.FirstOrDefault(c => c.Id == _selectedCategoryId.Value);
            var isAll = selectedCategory?.Id == AllCategoryId;
            if (!isAll)
            {
                query = query.Where(r => r.CategoryId == _selectedCategoryId.Value);
            }
        }

        var keyword = _searchKeyword?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = NormalizeForSearch(keyword);

            query = query.Where(r =>
            {
                var searchBlob = NormalizeForSearch($"{r.Name} {r.History} {r.Address} {r.TextVi} {r.TextEn} {r.TextZh} {r.TextJa} {r.TextRu} {r.TextFr} {r.Highlights} {r.CategoryName}");
                return searchBlob.Contains(normalizedKeyword, StringComparison.Ordinal);
            });
        }

        _filteredRestaurants.Clear();
        _filteredRestaurants.AddRange(query.OrderBy(r => r.Priority));

        RestaurantsCollection.ItemsSource = null;
        RestaurantsCollection.ItemsSource = _filteredRestaurants;

        var language = _localizationService.CurrentLanguage;
        var resultText = _localizationService.GetString("Results", language);
        ResultCountLabel.Text = $"{_filteredRestaurants.Count} {resultText}";
        RenderTrackAsiaMap();
    }

    private static string NormalizeForSearch(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var normalized = input.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace('đ', 'd');
    }

    private void RenderTrackAsiaMap()
    {
        var hasInternet = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

        string html;
        if (hasInternet)
        {
            html = BuildMapHtml(_filteredRestaurants);
            SaveMapHtmlToCache(html);
        }
        else
        {
            html = LoadCachedMapHtml() ?? BuildOfflineMapHtml(_filteredRestaurants);
        }

        SetMapHtml("TrackAsiaMapWebView", html);
        SetMapHtml("TrackAsiaMapFullScreenWebView", html);
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(RenderTrackAsiaMap);
    }

    private void SaveMapHtmlToCache(string html)
    {
        try
        {
            File.WriteAllText(_mapHtmlCachePath, html);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Explore Map Cache Save] {ex.Message}");
        }
    }

    private string? LoadCachedMapHtml()
    {
        try
        {
            if (!File.Exists(_mapHtmlCachePath))
            {
                return null;
            }

            var html = File.ReadAllText(_mapHtmlCachePath);
            return string.IsNullOrWhiteSpace(html) ? null : html;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Explore Map Cache Load] {ex.Message}");
            return null;
        }
    }

    private void SetMapHtml(string webViewName, string html)
    {
        var mapWebView = this.FindByName<WebView>(webViewName);
        if (mapWebView is null)
        {
            return;
        }

        mapWebView.Source = new HtmlWebViewSource
        {
            Html = html
        };
    }

    private static string EscapeHtml(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    private string BuildMapHtml(IReadOnlyList<Restaurant> restaurants)
    {
        var centerLat = _currentLocation?.Latitude ?? (restaurants.Count > 0 ? restaurants[0].Latitude : 10.7769);
        var centerLng = _currentLocation?.Longitude ?? (restaurants.Count > 0 ? restaurants[0].Longitude : 106.6966);

        var restaurantPayload = restaurants.Select(r => new
        {
            id = r.Id,
            name = EscapeHtml(r.Name),
            address = EscapeHtml(r.DisplayAddress),
            highlights = EscapeHtml(r.DisplayHighlights),
            rating = r.Rating,
            latitude = r.Latitude,
            longitude = r.Longitude
        }).ToList();

        var restaurantsJson = JsonSerializer.Serialize(restaurantPayload);
        var language = _localizationService.CurrentLanguage;
        var detailsText = _localizationService.GetString("ViewDetails", language);
        var mapNotLoadedText = _localizationService.GetString("MapNotLoaded", language);

        var userLat = _currentLocation?.Latitude;
        var userLng = _currentLocation?.Longitude;

        var html = @"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1.0, user-scalable=no' />
  <link rel='stylesheet' href='__TRACKASIA_CSS__' />
  <link rel='stylesheet' href='__MAPLIBRE_CSS__' />
  <style>
    html, body, #map { margin: 0; padding: 0; width: 100%; height: 100%; background: #ffffff; }
    .popup-title { font-weight: 700; margin-bottom: 4px; color: #1F1F1F; }
    .popup-sub { font-size: 12px; color: #666666; margin: 0; }
    .popup-link { margin-top: 6px; display: inline-block; color: #FF6B35; text-decoration: none; font-weight: 700; }
  </style>
</head>
<body>
  <div id='map'></div>
  <script src='__TRACKASIA_JS__'></script>
  <script src='__MAPLIBRE_JS__'></script>
  <script>
    const apiKey = '__API_KEY__';
    const restaurants = __RESTAURANTS_JSON__;
    const detailsText = '__DETAILS_TEXT__';
    const mapNotLoadedText = '__MAP_NOT_LOADED_TEXT__';
    const userLat = __USER_LAT__;
    const userLng = __USER_LNG__;

    function getMapSdk() {
      if (window.trackasia && window.trackasia.Map) return window.trackasia;
      if (window.maplibregl && window.maplibregl.Map) return window.maplibregl;
      return null;
    }

    function initMap() {
      const sdk = getMapSdk();
      if (!sdk) {
        document.body.innerHTML = '<div style=""padding:12px;color:#B00020;font-family:sans-serif;"">' + mapNotLoadedText + '</div>';
        return;
      }

      const map = new sdk.Map({
        container: 'map',
        style: 'https://maps.track-asia.com/styles/v1/streets.json?key=' + apiKey,
        center: [__CENTER_LNG__, __CENTER_LAT__],
        zoom: 14
      });

      if (sdk.NavigationControl) {
        map.addControl(new sdk.NavigationControl(), 'top-right');
      }

      if (typeof userLat === 'number' && typeof userLng === 'number') {
        new sdk.Marker({ color: '#2E86DE' })
          .setLngLat([userLng, userLat])
          .addTo(map);
      }

      restaurants.forEach(r => {
        const detailUrl = 'app://poi/' + encodeURIComponent(r.id ?? '');
        const popupHtml =
          '<div class=""popup-title"">' + (r.name ?? '') + '</div>' +
          '<p class=""popup-sub"">⭐ ' + Number(r.rating).toFixed(1) + '</p>' +
          '<p class=""popup-sub"">📍 ' + (r.address ?? '') + '</p>' +
          '<p class=""popup-sub"">' + (r.highlights ?? '') + '</p>' +
          '<a class=""popup-link"" href=""' + detailUrl + '"">' + detailsText + '</a>';

        const popup = new sdk.Popup({ offset: 20 }).setHTML(popupHtml);
        new sdk.Marker({ color: '#FF6B35' })
          .setLngLat([r.longitude, r.latitude])
          .setPopup(popup)
          .addTo(map);
      });
    }

    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', initMap);
    } else {
      initMap();
    }
  </script>
</body>
</html>";

        return html
            .Replace("__TRACKASIA_CSS__", TrackAsiaCssUrl)
            .Replace("__MAPLIBRE_CSS__", MapLibreCssUrl)
            .Replace("__TRACKASIA_JS__", TrackAsiaJsUrl)
            .Replace("__MAPLIBRE_JS__", MapLibreJsUrl)
            .Replace("__API_KEY__", TrackAsiaApiKey)
            .Replace("__RESTAURANTS_JSON__", restaurantsJson)
            .Replace("__CENTER_LAT__", centerLat.ToString(CultureInfo.InvariantCulture))
            .Replace("__CENTER_LNG__", centerLng.ToString(CultureInfo.InvariantCulture))
            .Replace("__DETAILS_TEXT__", EscapeHtml(detailsText))
            .Replace("__MAP_NOT_LOADED_TEXT__", EscapeHtml(mapNotLoadedText))
            .Replace("__USER_LAT__", userLat?.ToString(CultureInfo.InvariantCulture) ?? "null")
            .Replace("__USER_LNG__", userLng?.ToString(CultureInfo.InvariantCulture) ?? "null");
    }

    private string BuildOfflineMapHtml(IReadOnlyList<Restaurant> restaurants)
    {
        var language = _localizationService.CurrentLanguage;
        var detailsText = _localizationService.GetString("ViewDetails", language);
        var offlineModeText = GetOfflineMapModeText(language);

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8' />");
        sb.Append("<meta name='viewport' content='width=device-width, initial-scale=1.0, user-scalable=no' />");
        sb.Append("<style>");
        sb.Append("html,body{margin:0;padding:0;font-family:sans-serif;background:#fff;} ");
        sb.Append("#banner{background:#FFF4E5;color:#A15C07;font-size:12px;padding:8px 10px;border-bottom:1px solid #FFD8A8;} ");
        sb.Append("#list{padding:10px;} .item{margin-bottom:10px;padding:10px;border:1px solid #E5E5E5;border-radius:10px;} ");
        sb.Append(".title{font-weight:700;color:#1F1F1F;} .sub{margin:4px 0 0;color:#666;font-size:12px;} ");
        sb.Append(".link{display:inline-block;margin-top:8px;color:#FF6B35;text-decoration:none;font-weight:700;}");
        sb.Append("</style></head><body>");
        sb.Append($"<div id='banner'>{EscapeHtml(offlineModeText)}</div>");
        sb.Append("<div id='list'>");

        if (restaurants.Count == 0)
        {
            sb.Append("<div class='item'><div class='sub'>No data</div></div>");
        }
        else
        {
            foreach (var r in restaurants)
            {
                var detailUrl = $"app://poi/{Uri.EscapeDataString(r.Id ?? string.Empty)}";
                sb.Append("<div class='item'>");
                sb.Append($"<div class='title'>{EscapeHtml(r.Name)}</div>");
                sb.Append($"<p class='sub'>⭐ {r.Rating:F1}</p>");
                sb.Append($"<p class='sub'>📍 {EscapeHtml(r.DisplayAddress)}</p>");
                sb.Append($"<a class='link' href='{detailUrl}'>{EscapeHtml(detailsText)}</a>");
                sb.Append("</div>");
            }
        }

        sb.Append("</div></body></html>");
        return sb.ToString();
    }

    private static string GetOfflineMapModeText(string language)
    {
        return NormalizeLanguageCode(language) switch
        {
            "en" => "Offline mode: displaying cached/simple map data.",
            "zh" => "离线模式：显示缓存/简化地图数据。",
            "ja" => "オフラインモード：キャッシュ/簡易マップを表示しています。",
            "ru" => "Оффлайн-режим: отображаются кэшированные/упрощенные данные карты.",
            "fr" => "Mode hors ligne : affichage des données cartographiques en cache/simplifiées.",
            _ => "Chế độ offline: đang hiển thị dữ liệu bản đồ cache/đơn giản."
        };
    }

    private async void OnPlayAudioClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: Restaurant restaurant })
        {
            return;
        }

        var language = _localizationService.CurrentLanguage;
        var ttsText = restaurant.GetTextByLanguage(language);

        await _audioService.PlayTextAsync(
            ttsText,
            language,
            restaurant.Name,
            restaurant.Id);
    }

    private async void OnViewDetailsClicked(object sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: Restaurant restaurant })
        {
            return;
        }

        await Shell.Current.Navigation.PushAsync(new RestaurantDetailPage(restaurant, _audioService));
    }

    private async void OnMapWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Url))
        {
            return;
        }

        if (!e.Url.StartsWith("app://poi/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        e.Cancel = true;

        var encodedId = e.Url.Replace("app://poi/", string.Empty, StringComparison.OrdinalIgnoreCase);
        var poiId = Uri.UnescapeDataString(encodedId);

        var restaurant = _allRestaurants.FirstOrDefault(r => r.Id == poiId)
            ?? _filteredRestaurants.FirstOrDefault(r => r.Id == poiId);

        if (restaurant is null)
        {
            return;
        }

        await Shell.Current.Navigation.PushAsync(new RestaurantDetailPage(restaurant, _audioService));
    }

    private void UpdateCategorySelectionState()
    {
        foreach (var category in _categories)
        {
            category.IsSelected = _selectedCategoryId.HasValue && category.Id == _selectedCategoryId.Value;
        }
    }

    private void RefreshCategoryItemsSource()
    {
        FilterChipsCollection.ItemsSource = null;
        FilterChipsCollection.ItemsSource = _categories;
    }

    private void OnCategorySelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedCategory = e.CurrentSelection?.FirstOrDefault() as Category;
        _selectedCategoryId = selectedCategory?.Id;
        UpdateCategorySelectionState();
        RefreshCategoryItemsSource();
        ApplyFilters();
    }

    private void OnCategoryChipTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not Category category)
        {
            return;
        }

        _selectedCategoryId = category.Id;
        UpdateCategorySelectionState();
        RefreshCategoryItemsSource();
        ApplyFilters();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchKeyword = e.NewTextValue ?? string.Empty;

        _searchDebounceCts?.Cancel();
        var cts = new CancellationTokenSource();
        _searchDebounceCts = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(250, cts.Token);
                if (cts.IsCancellationRequested)
                {
                    return;
                }

                await MainThread.InvokeOnMainThreadAsync(ApplyFilters);
            }
            catch (TaskCanceledException)
            {
                // bỏ qua khi người dùng đang tiếp tục gõ
            }
        });
    }

    private void OnExpandMapClicked(object sender, EventArgs e)
    {
        var overlay = this.FindByName<Grid>("FullScreenMapOverlay");
        if (overlay is null)
        {
            return;
        }

        overlay.IsVisible = true;
    }

    private void OnCloseFullScreenMapClicked(object sender, EventArgs e)
    {
        var overlay = this.FindByName<Grid>("FullScreenMapOverlay");
        if (overlay is null)
        {
            return;
        }

        overlay.IsVisible = false;
    }

    private async void OnRefreshRequested(object? sender, EventArgs e)
    {
        try
        {
            await _firebaseSyncService.SyncIfNeededAsync(force: true);
            await _databaseService.InitAsync();

            var categories = await _databaseService.GetCategoriesAsync();
            var pois = await _databaseService.GetAllPoisAsync();

            SetCategories(categories);
            SetRestaurants(pois.Select(MapPoiToRestaurant).OrderBy(r => r.Priority).ToList());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Explore Refresh] {ex.Message}");
        }
        finally
        {
            if (sender is RefreshView refreshView)
            {
                refreshView.IsRefreshing = false;
            }
        }
    }

    private void UpdateRestaurantButtonTexts()
    {
        // Refresh ItemsSource để XAML Binding được cập nhật lại
        // Điều này sẽ làm cho Converter được gọi lại với language mới
        RestaurantsCollection.ItemsSource = null;
        RestaurantsCollection.ItemsSource = _filteredRestaurants;
    }

    private static string NormalizeCategoryKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value
            .Trim()
            .Replace('\t', ' ')
            .Replace("–", "-")
            .Replace("—", "-")
            .Replace("/", "-")
            .Replace(" - ", "-")
            .Replace("- ", "-")
            .Replace(" -", "-")
            .ToLowerInvariant();

        while (normalized.Contains("  ", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("  ", " ");
        }

        return normalized;
    }
}