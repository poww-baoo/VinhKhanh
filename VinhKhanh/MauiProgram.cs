using Microsoft.Extensions.Logging;
using VinhKhanh.Pages;
using VinhKhanh.Services;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace VinhKhanh
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .UseBarcodeReader();

            builder.Services
                .AddSingleton<SQLiteDbContext>()
                .AddSingleton<DatabaseService>()
                .AddSingleton<ImageSyncService>()
                .AddSingleton<FirebaseSyncService>()
                .AddSingleton<QRCodeService>()
                .AddSingleton<LocationService>()
                .AddSingleton<GeofenceService>()
                .AddSingleton<AudioPlaybackService>()
                .AddSingleton<TtsService>()
                .AddSingleton<TranslationService>()
                .AddSingleton<PlaybackService>()
                .AddSingleton(_ => LocalizationService.Instance)
                .AddSingleton<MainPage>()
                .AddSingleton<ExplorePage>()
                .AddSingleton<SavedPage>()
                .AddSingleton<TrackingPage>()
                .AddSingleton<SettingsPage>()
                .AddSingleton<QRCodePage>()
                .AddSingleton<RestaurantDetailPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
