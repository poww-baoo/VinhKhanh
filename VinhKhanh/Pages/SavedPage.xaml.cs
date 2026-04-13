using Microsoft.Extensions.DependencyInjection;
using VinhKhanh.Models;
using VinhKhanh.Services;

namespace VinhKhanh.Pages
{
    public partial class SavedPage : ContentPage
    {
        private readonly SQLiteDbContext _dbContext;
        private readonly DatabaseService _databaseService;
        private readonly LocalizationService _localizationService;
        private readonly ImageSyncService _imageSyncService;
        private readonly AudioPlaybackService _audioService;
        private readonly List<Restaurant> _savedRestaurants = new();

        private const string BaseCategoryLanguage = "vi";
        private readonly Dictionary<int, string> _sourceCategoryNames = new();

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
                [NormalizeCategoryKey("Lẩu - Nướng")] = "Hotpot & Grill",
                [NormalizeCategoryKey("Ốc - Hải sản")] = "Snails & Seafood",
            },
            ["zh"] = new(StringComparer.OrdinalIgnoreCase)
            {
                [NormalizeCategoryKey("Tất cả")] = "全部",
                [NormalizeCategoryKey("Món nước")] = "汤/粉面",
                [NormalizeCategoryKey("Món khô")] = "干拌类",
                [NormalizeCategoryKey("Ăn vặt")] = "小吃",
                [NormalizeCategoryKey("Ăn Vặt - Nước")] = "小吃与饮品",
                [NormalizeCategoryKey("Đồ uống")] = "饮品",
                [NormalizeCategoryKey("Tráng miệng")] = "甜点",
                [NormalizeCategoryKey("Lẩu - Nướng")] = "火锅与烧烤",
                [NormalizeCategoryKey("Ốc - Hải sản")] = "螺类与海鲜",
            },
            ["ja"] = new(StringComparer.OrdinalIgnoreCase)
            {
                [NormalizeCategoryKey("Tất cả")] = "すべて",
                [NormalizeCategoryKey("Món nước")] = "汁物・麺",
                [NormalizeCategoryKey("Món khô")] = "ドライ料理",
                [NormalizeCategoryKey("Ăn vặt")] = "軽食",
                [NormalizeCategoryKey("Ăn Vặt - Nước")] = "軽食・ドリンク",
                [NormalizeCategoryKey("Đồ uống")] = "ドリンク",
                [NormalizeCategoryKey("Tráng miệng")] = "デザート",
                [NormalizeCategoryKey("Lẩu - Nướng")] = "鍋・焼き物",
                [NormalizeCategoryKey("Ốc - Hải sản")] = "巻貝・シーフード",
            },
            ["ru"] = new(StringComparer.OrdinalIgnoreCase)
            {
                [NormalizeCategoryKey("Tất cả")] = "Все",
                [NormalizeCategoryKey("Món nước")] = "Супы и лапша",
                [NormalizeCategoryKey("Món khô")] = "Сухие блюда",
                [NormalizeCategoryKey("Ăn vặt")] = "Закуски",
                [NormalizeCategoryKey("Ăn Vặt - Nước")] = "Закуски и напитки",
                [NormalizeCategoryKey("Đồ uống")] = "Напитки",
                [NormalizeCategoryKey("Tráng miệng")] = "Десерты",
                [NormalizeCategoryKey("Lẩu - Нướng")] = "Хотпот и гриль",
                [NormalizeCategoryKey("Ốc - Hải sản")] = "Улитки и морепродукты",
            },
            ["fr"] = new(StringComparer.OrdinalIgnoreCase)
            {
                [NormalizeCategoryKey("Tất cả")] = "Tous",
                [NormalizeCategoryKey("Món nước")] = "Soupes et nouilles",
                [NormalizeCategoryKey("Món khô")] = "Plats secs",
                [NormalizeCategoryKey("Ăn vặt")] = "Snacks",
                [NormalizeCategoryKey("Ăn Vặt - Nước")] = "Snacks et boissons",
                [NormalizeCategoryKey("Đồ uống")] = "Boissons",
                [NormalizeCategoryKey("Tráng miệng")] = "Desserts",
                [NormalizeCategoryKey("Lẩu - Nướng")] = "Fondue et grillades",
                [NormalizeCategoryKey("Ốc - Hải sản")] = "Escargots et fruits de mer",
            }
        };

        public SavedPage()
        {
            InitializeComponent();
            _dbContext = new SQLiteDbContext();
            _databaseService = ResolveService<DatabaseService>() ?? new DatabaseService();
            _localizationService = LocalizationService.Instance;
            _imageSyncService = ResolveService<ImageSyncService>() ?? new ImageSyncService();
            _audioService = new AudioPlaybackService();

            _localizationService.LanguageChanged += OnLanguageChangedEvent;
            RestaurantDetailPage.SavedStateChanged += OnSavedStateChanged;

            UpdateUI();
        }

        ~SavedPage()
        {
            RestaurantDetailPage.SavedStateChanged -= OnSavedStateChanged;
        }

        private void OnSavedStateChanged()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await RefreshSavedRestaurantsAsync();
            });
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = LoadSavedRestaurantsAsync();
        }

        private static T? ResolveService<T>() where T : class =>
            Application.Current?.Handler?.MauiContext?.Services.GetService<T>();

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

            foreach (var restaurant in _savedRestaurants)
            {
                if (_sourceCategoryNames.TryGetValue(restaurant.CategoryId, out var sourceCategoryName))
                {
                    restaurant.CategoryName = GetLocalizedCategoryName(sourceCategoryName, language);
                }

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
            }
        }

        private void OnLanguageChangedEvent(object? sender, EventArgs e)
        {
            UpdateUI();
            ApplyRestaurantLocalization();
            MainThread.BeginInvokeOnMainThread(RefreshSavedListUI);
        }

        private void UpdateUI()
        {
            try
            {
                var language = _localizationService.CurrentLanguage;
                Title = _localizationService.GetString("Saved", language);

                if (SavedPageLabel is not null)
                {
                    SavedPageLabel.Text = _localizationService.GetString("SavedRestaurantsLabel", language);
                }

                if (NoSavedTextLabel is not null)
                {
                    NoSavedTextLabel.Text = _localizationService.GetString("NoSavedRestaurants", language);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SavedPage UpdateUI Error] {ex.Message}");
            }
        }

        private async Task LoadSavedRestaurantsAsync()
        {
            try
            {
                await _dbContext.InitializeAsync();
                await _databaseService.InitAsync();

                var savedList = await _dbContext.GetAllSavedRestaurantsAsync();

                _savedRestaurants.Clear();

                foreach (var saved in savedList)
                {
                    if (int.TryParse(saved.RestaurantId, out var poiId))
                    {
                        var poi = await _databaseService.GetPoiByIdAsync(poiId);
                        if (poi != null)
                        {
                            var restaurant = new Restaurant
                            {
                                Id = poi.Id.ToString(),
                                CategoryId = poi.CategoryId,
                                CategoryName = _sourceCategoryNames.TryGetValue(poi.CategoryId, out var sourceCategoryName)
    ? sourceCategoryName
    : poi.CategoryName,
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
                                Rating = poi.Rating,
                                Latitude = poi.Lat,
                                Longitude = poi.Lng,
                                GeofenceRadius = poi.RadiusMeters,
                                Priority = poi.Priority,
                                ImageFileName = poi.ImageFileName,
                                DisplayImage = _imageSyncService.GetLocalPath(poi.ImageFileName)
                            };

                            _savedRestaurants.Add(restaurant);
                        }
                    }
                }

                _savedRestaurants.Sort((a, b) => a.Priority.CompareTo(b.Priority));
                ApplyRestaurantLocalization();

                await MainThread.InvokeOnMainThreadAsync(RefreshSavedListUI);
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    var language = _localizationService.CurrentLanguage;
                    await DisplayAlert(
                        _localizationService.GetString("Error", language),
                        $"{_localizationService.GetString("CannotLoadData", language)}: {ex.Message}",
                        _localizationService.GetString("OK", language));
                });
            }

            var categories = await _databaseService.GetCategoriesAsync();
            _sourceCategoryNames.Clear();
            foreach (var category in categories)
            {
                _sourceCategoryNames[category.Id] = category.Name;
            }
        }

        public Task RefreshSavedRestaurantsAsync() => LoadSavedRestaurantsAsync();

        private void RefreshSavedListUI()
        {
            if (_savedRestaurants.Count > 0)
            {
                SavedRestaurantsCollection.IsVisible = true;
                NoSavedLabel.IsVisible = false;
                SavedRestaurantsCollection.ItemsSource = null;
                SavedRestaurantsCollection.ItemsSource = _savedRestaurants;
            }
            else
            {
                SavedRestaurantsCollection.IsVisible = false;
                NoSavedLabel.IsVisible = true;
            }

            var language = _localizationService.CurrentLanguage;
            var resultText = _localizationService.GetString("Results", language);
            ResultCountLabel.Text = $"{_savedRestaurants.Count} {resultText}";
        }

        private async void OnViewDetailsClicked(object sender, EventArgs e)
        {
            if (sender is not Button { CommandParameter: Restaurant restaurant })
            {
                return;
            }

            await Shell.Current.Navigation.PushAsync(new RestaurantDetailPage(restaurant, _audioService));
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

        private async void OnToggleSavedClicked(object sender, EventArgs e)
        {
            if (sender is not Button { CommandParameter: Restaurant restaurant })
            {
                return;
            }

            try
            {
                await _dbContext.InitializeAsync();

                var removed = await _dbContext.RemoveSavedRestaurantAsync(restaurant.Id);
                if (removed > 0)
                {
                    _savedRestaurants.RemoveAll(r => r.Id == restaurant.Id);
                    await MainThread.InvokeOnMainThreadAsync(RefreshSavedListUI);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SavedPage Remove] {ex.Message}");
            }
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
}