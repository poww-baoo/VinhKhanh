using Microsoft.Extensions.DependencyInjection;
using VinhKhanh.Models;
using VinhKhanh.Services;
using ZXing.Net.Maui;

namespace VinhKhanh.Pages
{
    public partial class RestaurantDetailPage : ContentPage
    {
        public static event Action? SavedStateChanged;

        private readonly Restaurant _restaurant;
        private readonly AudioPlaybackService _audioService;
        private readonly DatabaseService _databaseService;
        private readonly LocalizationService _localizationService;
        private readonly ImageSyncService _imageSyncService;
        private readonly SQLiteDbContext _dbContext;
        private readonly QRCodeService _qrCodeService;
        private readonly string _poiQrPayload;

        private string _selectedAudioLanguage = "vi";
        private bool _isSaved = false;

        public string DisplayImage { get; private set; } = "placeholder.png";

        public RestaurantDetailPage(Restaurant restaurant, AudioPlaybackService? audioService = null)
        {
            InitializeComponent();
            BindingContext = this;

            _restaurant = restaurant;
            _audioService = audioService ?? new AudioPlaybackService();
            _databaseService = ResolveService<DatabaseService>() ?? new DatabaseService();
            _localizationService = LocalizationService.Instance;
            _imageSyncService = ResolveService<ImageSyncService>() ?? new ImageSyncService();
            _dbContext = new SQLiteDbContext();
            _qrCodeService = ResolveService<QRCodeService>() ?? new QRCodeService(_databaseService);

            _poiQrPayload = int.TryParse(_restaurant.Id, out var poiId)
                ? _qrCodeService.BuildPoiQrPayload(poiId)
                : _restaurant.Id;

            LoadDisplayImage();
            InitializeAudioLanguage();
            LoadRestaurantData();
            _ = LoadMenuItemsAsync();
            _ = CheckIfSavedAsync();

            _localizationService.LanguageChanged += OnLanguageChangedEvent;
        }

        private static T? ResolveService<T>() where T : class =>
            Application.Current?.Handler?.MauiContext?.Services.GetService<T>();

        private void LoadDisplayImage()
        {
            DisplayImage = _imageSyncService.GetLocalPath(_restaurant.ImageFileName);
            OnPropertyChanged(nameof(DisplayImage));
        }

        private void InitializeAudioLanguage()
        {
            var currentLanguage = _localizationService.CurrentLanguage;
            _selectedAudioLanguage = currentLanguage is "en" or "zh" or "ja" or "ru" or "fr"
                ? currentLanguage
                : "vi";

            if (!HasTextForLanguage(_selectedAudioLanguage))
            {
                _selectedAudioLanguage = "vi";
            }

            ApplyLanguageAvailability();
            UpdateAudioOptionUI();
        }

        private bool HasTextForLanguage(string language)
        {
            return language switch
            {
                "en" => !string.IsNullOrWhiteSpace(_restaurant.TextEn),
                "zh" => !string.IsNullOrWhiteSpace(_restaurant.TextZh),
                "ja" => !string.IsNullOrWhiteSpace(_restaurant.TextJa),
                "ru" => !string.IsNullOrWhiteSpace(_restaurant.TextRu),
                "fr" => !string.IsNullOrWhiteSpace(_restaurant.TextFr),
                _ => !string.IsNullOrWhiteSpace(_restaurant.TextVi) || !string.IsNullOrWhiteSpace(_restaurant.History)
            };
        }

        private void ApplyLanguageAvailability()
        {
            ViOptionButton.IsVisible = HasTextForLanguage("vi");
            EnOptionButton.IsVisible = HasTextForLanguage("en");
            ZhOptionButton.IsVisible = HasTextForLanguage("zh");
            JaOptionButton.IsVisible = HasTextForLanguage("ja");
            RuOptionButton.IsVisible = HasTextForLanguage("ru");
            FrOptionButton.IsVisible = HasTextForLanguage("fr");

            if (!HasTextForLanguage(_selectedAudioLanguage))
            {
                _selectedAudioLanguage = "vi";
            }
        }

        private void LoadRestaurantData()
        {
            var language = _localizationService.CurrentLanguage;

            Title = _localizationService.GetString("RestaurantDetailTitle", language);
            RestaurantNameLabel.Text = _restaurant.Name;
            YearLabel.Text = $"{_localizationService.GetString("YearEstablished", language)}: {_restaurant.YearEstablished}";
            RatingLabel.Text = $"⭐ {_restaurant.Rating:F1}";
            HistoryLabel.Text = _restaurant.GetHistoryByLanguage(language);
            IntroLabel.Text = _restaurant.GetTextByLanguage(language);

            HistoryTitleLabel.Text = _localizationService.GetString("History", language);
            IntroTitleLabel.Text = _localizationService.GetString("Introduction", language);
            AddressTitleLabel.Text = _localizationService.GetString("Address", language);
            SignatureDishTitleLabel.Text = _localizationService.GetString("SignatureDish", language);
            NarrationLanguageLabel.Text = _localizationService.GetString("NarrationLanguage", language);
            MenuTitleLabel.Text = _localizationService.GetString("FeaturedMenu", language);
            PlaybackControlTitleLabel.Text = _localizationService.GetString("PlaybackControls", language);
            PlaySignatureButton.Text = _localizationService.GetString("ListenStoryButton", language);
            MenuCollection.EmptyView = _localizationService.GetString("NoMenuData", language);

            QrTitleLabel.Text = _localizationService.GetString("QRCode", language);
            QrContentLabel.Text = _poiQrPayload;
            PoiQrCodeView.Format = BarcodeFormat.QrCode;
            PoiQrCodeView.Value = _poiQrPayload;

            ViOptionButton.Text = $"🇻🇳 {_localizationService.GetString("Vi", language)}";
            EnOptionButton.Text = $"🇬🇧 {_localizationService.GetString("En", language)}";
            ZhOptionButton.Text = $"🇨🇳 {_localizationService.GetString("Zh", language)}";
            JaOptionButton.Text = $"🇯🇵 {_localizationService.GetString("Ja", language)}";
            RuOptionButton.Text = $"🇷🇺 {_localizationService.GetString("Ru", language)}";
            FrOptionButton.Text = $"🇫🇷 {_localizationService.GetString("Fr", language)}";

            var localizedAddress = _restaurant.GetAddressByLanguage(language);
            AddressLabel.Text = string.IsNullOrWhiteSpace(localizedAddress) ? "-" : localizedAddress;

            SignatureDishLabel.Text = _restaurant.Name;
            SignatureDishStoryLabel.Text = _restaurant.GetHistoryByLanguage(language);
            UpdateSaveButtonUI();
        }

        private async Task LoadMenuItemsAsync()
        {
            if (!int.TryParse(_restaurant.Id, out var poiId))
            {
                MenuCollection.ItemsSource = Array.Empty<PoiMenuItem>();
                return;
            }

            try
            {
                await _databaseService.InitAsync();
                var menuItems = await _databaseService.GetMenuItemsAsync(poiId);
                MenuCollection.ItemsSource = menuItems;

                var signature = menuItems.FirstOrDefault(i => i.IsSignature) ?? menuItems.FirstOrDefault();
                if (signature is not null)
                {
                    SignatureDishLabel.Text = signature.Name;
                    if (!string.IsNullOrWhiteSpace(signature.Description))
                    {
                        SignatureDishStoryLabel.Text = signature.Description;
                    }
                }
            }
            catch
            {
                MenuCollection.ItemsSource = Array.Empty<PoiMenuItem>();
            }
        }

        private async Task CheckIfSavedAsync()
        {
            try
            {
                await _dbContext.InitializeAsync();
                _isSaved = await _dbContext.IsSavedAsync(_restaurant.Id);
            }
            catch
            {
                _isSaved = false;
            }
            finally
            {
                UpdateSaveButtonUI();
            }
        }

        private void UpdateSaveButtonUI()
        {
            if (SaveButton is null)
            {
                return;
            }

            if (_isSaved)
            {
                SaveButton.Text = "★";
                SaveButton.BackgroundColor = Color.FromArgb("#FF6B35");
                SaveButton.TextColor = Colors.White;
            }
            else
            {
                SaveButton.Text = "☆";
                SaveButton.BackgroundColor = Color.FromArgb("#E0E0E0");
                SaveButton.TextColor = Color.FromArgb("#2C3E50");
            }
        }

        private async void OnSaveButtonClicked(object sender, EventArgs e)
        {
            if (sender is not Button saveButton)
            {
                return;
            }

            try
            {
                saveButton.IsEnabled = false;
                await _dbContext.InitializeAsync();

                var isCurrentlySaved = await _dbContext.IsSavedAsync(_restaurant.Id);
                var changed = false;

                if (isCurrentlySaved)
                {
                    var removeResult = await _dbContext.RemoveSavedRestaurantAsync(_restaurant.Id);
                    if (removeResult > 0)
                    {
                        _isSaved = false;
                        changed = true;
                    }
                }
                else
                {
                    var saveResult = await _dbContext.SaveRestaurantAsync(_restaurant.Id);
                    if (saveResult > 0)
                    {
                        _isSaved = true;
                        changed = true;
                    }
                }

                _isSaved = await _dbContext.IsSavedAsync(_restaurant.Id);
                UpdateSaveButtonUI();

                if (changed)
                {
                    SavedStateChanged?.Invoke();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Save Toggle] {ex.Message}");
            }
            finally
            {
                saveButton.IsEnabled = true;
            }
        }

        private void OnLanguageChangedEvent(object? sender, EventArgs e)
        {
            LoadRestaurantData();
            ApplyLanguageAvailability();
            UpdateAudioOptionUI();
        }

        private void OnAudioOptionClicked(object sender, EventArgs e)
        {
            if (sender is not Button { CommandParameter: string language })
            {
                return;
            }

            _selectedAudioLanguage = language;
            UpdateAudioOptionUI();
        }

        private void SetLanguageButtonState(Button button, bool isSelected)
        {
            var activeBg = Color.FromArgb("#FF6B35");
            var inactiveBg = Color.FromArgb("#E0E0E0");
            var activeText = Colors.White;
            var inactiveText = Color.FromArgb("#1F1F1F");

            button.BackgroundColor = isSelected ? activeBg : inactiveBg;
            button.TextColor = isSelected ? activeText : inactiveText;
        }

        private void UpdateAudioOptionUI()
        {
            SetLanguageButtonState(ViOptionButton, _selectedAudioLanguage == "vi");
            SetLanguageButtonState(EnOptionButton, _selectedAudioLanguage == "en");
            SetLanguageButtonState(ZhOptionButton, _selectedAudioLanguage == "zh");
            SetLanguageButtonState(JaOptionButton, _selectedAudioLanguage == "ja");
            SetLanguageButtonState(RuOptionButton, _selectedAudioLanguage == "ru");
            SetLanguageButtonState(FrOptionButton, _selectedAudioLanguage == "fr");
        }

        private async void OnPlaySignatureDish(object sender, EventArgs e)
        {
            var ttsText = _restaurant.GetTextByLanguage(_selectedAudioLanguage);

            await _audioService.PlayTextAsync(
                ttsText,
                _selectedAudioLanguage,
                _restaurant.Name,
                _restaurant.Id);
        }

        private void OnPlayClicked(object sender, EventArgs e)
        {
            _audioService.Resume();
        }

        private void OnPauseClicked(object sender, EventArgs e)
        {
            _audioService.Pause();
        }

        private async void OnStopClicked(object sender, EventArgs e)
        {
            await _audioService.StopAsync();
        }
    }
}