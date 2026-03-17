using VinhKhanh.Models;
using VinhKhanh.Services;

namespace VinhKhanh.Pages
{
    public partial class QRCodePage : ContentPage
    {
        private string? _lastScannedResult;
        private bool _isFlashlightOn = false;

        public QRCodePage()
        {
            InitializeComponent();
            InitializeQRScanner();
        }

        private void InitializeQRScanner()
        {
            var html = BuildQRScannerHtml();
            QRScannerWebView.Source = new HtmlWebViewSource { Html = html };
            QRScannerWebView.Navigating += OnWebViewNavigating;
        }

        private void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
        {
            if (e.Url.StartsWith("qr-scan-result://"))
            {
                e.Cancel = true;
                var result = Uri.UnescapeDataString(e.Url.Replace("qr-scan-result://", ""));
                OnQRScanned(result);
            }
        }

        private string BuildQRScannerHtml()
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0, user-scalable=no' />
    <script src='https://cdn.jsdelivr.net/npm/jsqr@1.4.0/dist/jsQR.js'></script>
    <style>
        html, body { margin: 0; padding: 0; width: 100%; height: 100%; background: #1F1F1F; }
        #scanner-container { width: 100%; height: 100%; display: flex; justify-content: center; align-items: center; }
        video { width: 100%; height: 100%; object-fit: cover; }
        canvas { display: none; }
        .scanner-overlay { position: absolute; border: 2px solid #FF6B35; width: 250px; height: 250px; border-radius: 8px; }
    </style>
</head>
<body>
    <div id='scanner-container'>
        <video id='video' playsinline></video>
        <canvas id='canvas'></canvas>
        <div class='scanner-overlay'></div>
    </div>
    <script>
        const video = document.getElementById('video');
        const canvas = document.getElementById('canvas');
        const context = canvas.getContext('2d');
        let lastResult = '';

        async function startScanning() {
            try {
                const stream = await navigator.mediaDevices.getUserMedia({ 
                    video: { facingMode: 'environment', width: { ideal: 1280 }, height: { ideal: 720 } } 
                });
                video.srcObject = stream;
                video.play();
                requestAnimationFrame(scanQRCode);
            } catch (err) {
                console.error('Camera error:', err);
                document.body.innerHTML = '<div style=""color:white;padding:20px;font-family:sans-serif;"">Không thể truy cập camera.</div>';
            }
        }

        function scanQRCode() {
            if (video.readyState === video.HAVE_ENOUGH_DATA) {
                canvas.width = video.videoWidth;
                canvas.height = video.videoHeight;
                context.drawImage(video, 0, 0, canvas.width, canvas.height);
                
                const imageData = context.getImageData(0, 0, canvas.width, canvas.height);
                const code = jsQR(imageData.data, imageData.width, imageData.height);
                
                if (code && code.data !== lastResult) {
                    lastResult = code.data;
                    window.location.href = 'qr-scan-result://' + encodeURIComponent(code.data);
                }
            }
            requestAnimationFrame(scanQRCode);
        }

        startScanning();
    </script>
</body>
</html>";
        }

        private void OnFlashlightToggleClicked(object sender, EventArgs e)
        {
            _isFlashlightOn = !_isFlashlightOn;
            FlashlightToggleButton.BackgroundColor = _isFlashlightOn 
                ? Color.FromArgb("#FFC107") 
                : Color.FromArgb("#FF6B35");
            
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Thông báo", 
                    _isFlashlightOn ? "Bật đèn pin" : "Tắt đèn pin", "OK");
            });
        }

        private void OnRescanClicked(object sender, EventArgs e)
        {
            _lastScannedResult = null;
            ScanResultLabel.Text = "Chưa quét mã nào";
            InitializeQRScanner();
        }

        private void OnCopyClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_lastScannedResult))
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert("Thông báo", "Chưa có kết quả quét để sao chép", "OK");
                });
                return;
            }

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Clipboard.SetTextAsync(_lastScannedResult);
                await DisplayAlert("Thành công", "Đã sao chép vào clipboard", "OK");
            });
        }

        public void OnQRScanned(string result)
        {
            _lastScannedResult = result;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ScanResultLabel.Text = $"✓ {result}";
            });
        }
    }
}