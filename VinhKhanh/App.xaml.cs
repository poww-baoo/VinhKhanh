namespace VinhKhanh
{
    public partial class App : Application
    {
        public App(Services.FirebaseSyncService? syncService = null)
        {
            InitializeComponent();

            if (syncService is not null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await syncService.SyncIfNeededAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[App Startup Sync] {ex.Message}");
                    }
                });
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}