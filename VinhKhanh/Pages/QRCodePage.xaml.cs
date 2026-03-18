using System.Diagnostics;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using VinhKhanh.Models;
using VinhKhanh.Services;

namespace VinhKhanh.Pages
{
    public partial class QRCodePage : ContentPage
    {
        private string? _lastScannedResult;
        private bool _isFlashlightOn = false;
        private bool _isProcessing = false;

        public QRCodePage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            RequestCameraPermission();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            QRScannerView?.Handler?.DisconnectHandler();
        }

        private async void RequestCameraPermission()
        {
            try
            {
                var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();

                if (cameraStatus != PermissionStatus.Granted)
                {
                    cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
                }

                if (cameraStatus == PermissionStatus.Granted)
                {
                    Debug.WriteLine("Camera permission granted");
                }
                else
                {
                    await DisplayAlert("Lỗi", "Ứng dụng cần quyền truy cập camera để quét mã QR", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Lỗi", $"Lỗi yêu cầu quyền: {ex.Message}", "OK");
                Debug.WriteLine($"Permission error: {ex}");
            }
        }

        private void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
        {
            if (_isProcessing) return;

            var barcode = e.Results.FirstOrDefault();
            if (barcode == null) return;

            var result = barcode.Value;

            if (result == _lastScannedResult) return;

            _isProcessing = true;
            _lastScannedResult = result;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                QRScannerView.IsEnabled = false;
                await DisplayAlert("Quét Mã QR", $"Kết quả: {result}", "OK");
                QRScannerView.IsEnabled = true;
                _isProcessing = false;
            });
        }

        private void OnFlashlightToggleClicked(object sender, EventArgs e)
        {
            try
            {
                _isFlashlightOn = !_isFlashlightOn;
                FlashlightToggleButton.BackgroundColor = _isFlashlightOn 
                    ? Color.FromArgb("#FFC107") 
                    : Color.FromArgb("#FF6B35");
                
                #if ANDROID
                if (_isFlashlightOn)
                    Flashlight.Default.TurnOnAsync();
                else
                    Flashlight.Default.TurnOffAsync();
                #endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Flashlight error: {ex.Message}");
            }
        }
    }
}