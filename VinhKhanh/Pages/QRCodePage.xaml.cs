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
            try
            {
                // Khởi động scanner
                QRScannerView.IsDetecting = true;
                RequestCameraPermission();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnAppearing error: {ex}");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            try
            {
                QRScannerView.IsDetecting = false;
                QRScannerView?.Handler?.DisconnectHandler();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnDisappearing error: {ex}");
            }
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
                    // Bắt đầu quét
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        QRScannerView.IsDetecting = true;
                    });
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
            try
            {
                if (_isProcessing) return;
                if (e.Results == null || e.Results.Count() == 0) return;

                var barcode = e.Results.FirstOrDefault();
                if (barcode == null) return;

                var result = barcode.Value;
                
                if (string.IsNullOrEmpty(result)) return;
                if (result == _lastScannedResult) return;

                _isProcessing = true;
                _lastScannedResult = result;
                
                Debug.WriteLine($"QR Code detected: {result}");

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        QRScannerView.IsDetecting = false;
                        await DisplayAlert("Quét Mã QR Thành Công", $"Kết quả:\n{result}", "OK");
                        
                        // Reset để quét lại
                        _isProcessing = false;
                        _lastScannedResult = null;
                        QRScannerView.IsDetecting = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"DisplayAlert error: {ex}");
                        _isProcessing = false;
                        QRScannerView.IsDetecting = true;
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnBarcodesDetected error: {ex}");
                _isProcessing = false;
            }
        }

        private async void OnFlashlightToggleClicked(object sender, EventArgs e)
        {
            try
            {
                _isFlashlightOn = !_isFlashlightOn;
                FlashlightToggleButton.BackgroundColor = _isFlashlightOn 
                    ? Color.FromArgb("#FFC107") 
                    : Color.FromArgb("#FF6B35");
                
#if ANDROID
                try
                {
                    if (_isFlashlightOn)
                        await Flashlight.Default.TurnOnAsync();
                    else
                        await Flashlight.Default.TurnOffAsync();
                }
                catch (FeatureNotSupportedException)
                {
                    await DisplayAlert("Lỗi", "Thiết bị không hỗ trợ đèn pin", "OK");
                }
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Flashlight error: {ex.Message}");
            }
        }
    }
}