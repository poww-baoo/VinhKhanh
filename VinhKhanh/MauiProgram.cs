using Microsoft.Extensions.Logging;
using VinhKhanh.Pages;
using VinhKhanh.Services;

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
                .Services
                .AddSingleton<LocationService>()
                .AddSingleton<AudioPlaybackService>()
                .AddSingleton<LocalizationService>()
                .AddSingleton<MainPage>()
                .AddSingleton<ExplorePage>()
                .AddSingleton<SavedPage>()
                .AddSingleton<TrackingPage>()
                .AddSingleton<SettingsPage>()
                .AddSingleton<RestaurantDetailPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
