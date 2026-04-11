using System.Diagnostics;
using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using ZXing.SkiaSharp;
using VinhKhanh.Models;
using VinhKhanh.Services;
using ZxingBarcodeFormat = ZXing.BarcodeFormat;

namespace VinhKhanh.Pages
{
    /// <summary>
    /// Trang quét mã QR để tìm kiếm nhà hàng
    /// 
    /// Luồng hoạt động:
    /// 1. Khởi động camera và yêu cầu quyền truy cập
    /// 2. Quét mã QR chứa ID của nhà hàng (ví dụ: "1", "2", "3")
    /// 3. Tìm kiếm nhà hàng tương ứng từ cơ sở dữ liệu
    /// 4. Chuyển trang đến RestaurantDetailPage với toàn bộ dữ liệu nhà hàng
    /// </summary>
    public partial class QRCodePage : ContentPage
    {
        // ============ BIẾN PRIVATE ============
        /// <summary>Lưu trữ kết quả quét mã QR cuối cùng để tránh quét lặp lại</summary>
        private string? _lastScannedResult;

        /// <summary>
        /// Cờ kiểm soát xử lý để tránh xử lý nhiều mã QR cùng lúc
        /// Khi đang xử lý mã này, không xử lý mã tiếp theo
        /// </summary>
        private bool _isProcessing = false;

        /// <summary>
        /// Cờ đánh dấu việc đã chuyển trang hay chưa
        /// </summary>
        private bool _hasNavigated = false;

        /// <summary>Dịch vụ đa ngôn ngữ (Tiếng Việt, Tiếng Anh)</summary>
        private readonly LocalizationService _localizationService;

        /// <summary>Dịch vụ quét mã QR và tìm kiếm nhà hàng</summary>
        private readonly QRCodeService _qrCodeService;

        /// <summary>Dịch vụ phát nhạc nền</summary>
        private readonly AudioPlaybackService _audioPlaybackService;

        private static readonly BarcodeReader ImageBarcodeReader = new()
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = new List<ZxingBarcodeFormat>
                {
                    ZxingBarcodeFormat.QR_CODE
                }
            }
        };

        // ============ CONSTRUCTOR ============
        /// <summary>
        /// Khởi tạo trang quét mã QR
        /// - Thiết lập các dịch vụ cần thiết
        /// - Đăng ký sự kiện thay đổi ngôn ngữ
        /// - Cập nhật giao diện
        /// </summary>
        public QRCodePage()
        {
            InitializeComponent();

            // Khởi tạo các dịch vụ
            _localizationService = LocalizationService.Instance;
            _qrCodeService = ResolveService<QRCodeService>() ?? new QRCodeService(new DatabaseService());
            _audioPlaybackService = ResolveService<AudioPlaybackService>() ?? new AudioPlaybackService();

            // Đăng ký sự kiện thay đổi ngôn ngữ
            _localizationService.LanguageChanged += OnLanguageChangedEvent;

            // Cập nhật giao diện lần đầu
            UpdateUI();
            ConfigureScanner();

            Debug.WriteLine("QRCodePage: Trang QR Code đã được khởi tạo");
        }

        /// <summary>
        /// Hỗ trợ giải quyết dịch vụ từ DI Container
        /// </summary>
        private static T? ResolveService<T>() where T : class =>
            Application.Current?.Handler?.MauiContext?.Services.GetService<T>();

        // ============ SỰ KIỆN VÒNG ĐỜI (LIFECYCLE) ============
        /// <summary>
        /// Được gọi khi trang xuất hiện trên màn hình
        /// - Bật camera quét mã QR
        /// - Yêu cầu quyền truy cập camera
        /// 
        /// QUAN TRỌNG: Delay 700ms để đảm bảo camera ready
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                Debug.WriteLine("QRCodePage.OnAppearing: Trang đang xuất hiện...");

                await ActivateScannerAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"QRCodePage.OnAppearing: Lỗi - {ex.Message}");
            }
        }

        protected override void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);
            // Đảm bảo trạng thái luôn sạch khi quay lại từ trang chi tiết
            ResetScanState();
        }

        /// <summary>
        /// Được gọi khi trang biến mất khỏi màn hình
        /// - Tắt máy quét mã QR
        /// - Giải phóng tài nguyên camera
        /// </summary>
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            try
            {
                Debug.WriteLine("QRCodePage.OnDisappearing: Trang đang ẩn...");

                StopScanner();

                Debug.WriteLine("QRCodePage: Camera chuyển sang trạng thái nghỉ");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"QRCodePage.OnDisappearing: Lỗi - {ex.Message}");
            }
        }

        /// <summary>
        /// Dùng khi QR page được nhúng trong host page/tab (không nhận lifecycle đầy đủ của ContentPage).
        /// </summary>
        public async Task ActivateFromHostAsync()
        {
            try
            {
                await ActivateScannerAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"QRCodePage.ActivateFromHostAsync: Lỗi - {ex.Message}");
            }
        }

        /// <summary>
        /// Dùng khi tab QR bị rời đi để dừng camera gọn.
        /// </summary>
        public void DeactivateFromHost()
        {
            StopScanner();
        }

        // ============ QUẢN LÝ NGÔN NGỮ ============
        /// <summary>
        /// Sự kiện được gọi khi người dùng thay đổi ngôn ngữ
        /// - Cập nhật toàn bộ giao diện theo ngôn ngữ mới
        /// </summary>
        private void OnLanguageChangedEvent(object? sender, EventArgs e)
        {
            UpdateUI();
            Debug.WriteLine($"QRCodePage: Ngôn ngữ đã thay đổi");
        }

        /// <summary>
        /// Cập nhật giao diện theo ngôn ngữ hiện tại
        /// - Cập nhật tiêu đề trang
        /// - Cập nhật label hướng dẫn
        /// </summary>
        private void UpdateUI()
        {
            var language = _localizationService.CurrentLanguage;
            Title = _localizationService.GetString("ScanQR", language);
            InstructionLabel.Text = _localizationService.GetString("QRInstruction", language);
        }

        // ============ QUẢN LÝ QUYỀN VÀ CAMERA ============
        /// <summary>
        /// Yêu cầu quyền truy cập camera từ hệ điều hành
        /// 
        /// MAUI PermissionStatus chỉ có 3 trạng thái:
        /// - Granted: Quyền được cấp
        /// - Denied: Quyền bị từ chối
        /// - Unknown: Trạng thái không xác định
        /// </summary>
        private async Task<bool> RequestCameraPermissionAsync()
        {
            try
            {
                var language = _localizationService.CurrentLanguage;

                var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
                Debug.WriteLine($"QRCodePage: Trạng thái quyền camera - {cameraStatus}");

                if (cameraStatus != PermissionStatus.Granted)
                {
                    Debug.WriteLine("QRCodePage: Yêu cầu quyền camera từ người dùng");
                    cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
                    Debug.WriteLine($"QRCodePage: Người dùng phản hồi - {cameraStatus}");
                }

                if (cameraStatus == PermissionStatus.Granted)
                {
                    Debug.WriteLine("✅ QRCodePage: Quyền camera được cấp");
                    return true;
                }

                if (cameraStatus == PermissionStatus.Denied)
                {
                    Debug.WriteLine("❌ QRCodePage: Quyền camera bị từ chối");
                    await DisplayAlert(
                        _localizationService.GetString("Error", language),
                        _localizationService.GetString("CameraPermissionError", language),
                        _localizationService.GetString("OK", language));
                    return false;
                }

                Debug.WriteLine("❓ QRCodePage: Trạng thái quyền camera không xác định");
                await DisplayAlert(
                    _localizationService.GetString("Error", language),
                    _localizationService.GetString("PermissionUnknownError", language),
                    _localizationService.GetString("OK", language));
                return false;
            }
            catch (Exception ex)
            {
                var language = _localizationService.CurrentLanguage;
                Debug.WriteLine($"❌ QRCodePage.RequestCameraPermissionAsync: Lỗi - {ex.Message}");

                await DisplayAlert(
                    _localizationService.GetString("PermissionError", language),
                    $"{_localizationService.GetString("PermissionError", language)}: {ex.Message}",
                    _localizationService.GetString("OK", language));
                return false;
            }
        }

        // ============ XỬ LÝ MÃ QR ============
        /// <summary>
        /// Sự kiện được gọi khi máy quét phát hiện mã vạch/QR
        /// 
        /// Kiểm soát để tránh xử lý lặp lại
        /// </summary>
        private void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
        {
            try
            {
                if (_isProcessing || _hasNavigated)
                {
                    return;
                }

                if (e.Results == null || !e.Results.Any())
                {
                    return;
                }

                var result = e.Results
                    .Select(x => x?.Value?.Trim())
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

                if (string.IsNullOrWhiteSpace(result))
                {
                    Debug.WriteLine("QRCodePage: Không có kết quả QR hợp lệ trong frame hiện tại");
                    return;
                }

                if (result == _lastScannedResult)
                {
                    Debug.WriteLine($"QRCodePage: Mã QR trùng lặp - {result}");
                    return;
                }

                _isProcessing = true;
                _lastScannedResult = result;

                // tắt detect ngay để không bị quét lặp
                MainThread.BeginInvokeOnMainThread(() => QRScannerView.IsDetecting = false);

                Debug.WriteLine($"✅ QRCodePage: Phát hiện mã QR mới - {result}");

                _ = MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await ProcessQRCodeAsync(result);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ QRCodePage.OnBarcodesDetected: Lỗi - {ex.Message}");
                _isProcessing = false;
            }
        }

        /// <summary>
        /// Xử lý mã QR được phát hiện
        /// 
        /// Các bước:
        /// 1. Tắt máy quét để tránh phát hiện lặp lại
        /// 2. Tìm kiếm nhà hàng từ ID trong mã QR
        /// 3. Nếu tìm thấy, chuyển trang đến RestaurantDetailPage
        /// 4. Nếu không tìm thấy, hiển thị thông báo lỗi
        /// 5. Reset trạng thái để quét lại
        /// </summary>
        private async Task ProcessQRCodeAsync(string qrCodeId)
        {
            try
            {
                var language = _localizationService.CurrentLanguage;

                Debug.WriteLine($"🔍 QRCodePage: Đang tìm kiếm nhà hàng với ID: {qrCodeId}");

                var restaurant = await _qrCodeService.GetRestaurantFromIdAsync(qrCodeId);

                if (restaurant != null)
                {
                    _hasNavigated = true; // đánh dấu đã chuyển trang
                    StopScanner();
                    _isProcessing = false; // tránh bị kẹt state nếu lifecycle không chạy như kỳ vọng

                    Debug.WriteLine($"✅ QRCodePage: Tìm thấy nhà hàng - {restaurant.Name}");

                    await Shell.Current.Navigation.PushAsync(new RestaurantDetailPage(restaurant, _audioPlaybackService));
                    return;
                }

                Debug.WriteLine($"❌ QRCodePage: Không tìm thấy nhà hàng với ID: {qrCodeId}");

                await DisplayAlert(
                    _localizationService.GetString("Error", language),
                    $"ID: {qrCodeId}\n\n{_localizationService.GetString("RestaurantNotFound", language)}\n\n{_localizationService.GetString("ScanOtherQR", language)}",
                    _localizationService.GetString("OK", language));

                _isProcessing = false;
                _lastScannedResult = null;
                await StartScannerAsync();
            }
            catch (Exception ex)
            {
                var language = _localizationService.CurrentLanguage;
                Debug.WriteLine($"❌ QRCodePage.ProcessQRCodeAsync: Lỗi - {ex.Message}");

                await DisplayAlert(
                    _localizationService.GetString("Error", language),
                    $"{_localizationService.GetString("LoadRestaurantError", language)}\n{ex.Message}",
                    _localizationService.GetString("OK", language));

                _isProcessing = false;
                _lastScannedResult = null;
                await StartScannerAsync();
            }
        }

        private void ConfigureScanner()
        {
            QRScannerView.Options = new BarcodeReaderOptions
            {
                // Thư viện hiện tại hỗ trợ ổn định với nhóm định dạng 2D
                Formats = BarcodeFormats.TwoDimensional,
                AutoRotate = true,
                Multiple = false
            };

            QRScannerView.CameraLocation = CameraLocation.Rear;
        }

        private async Task ActivateScannerAsync()
        {
            ResetScanState();

            var hasPermission = await RequestCameraPermissionAsync();
            if (!hasPermission)
            {
                StopScanner();
                return;
            }

            // máy yếu cần thêm thời gian init camera
            await Task.Delay(350);
            await StartScannerAsync();
            Debug.WriteLine("QRCodePage: Scanner đã sẵn sàng từ ActivateScannerAsync");
        }

        private void ResetScanState()
        {
            _isProcessing = false;
            _hasNavigated = false;
            _lastScannedResult = null;
        }

        private void StopScanner()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                QRScannerView.IsDetecting = false;
                QRScannerView.IsEnabled = false;
            });
        }

        private async Task StartScannerAsync()
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                QRScannerView.IsDetecting = false;
                QRScannerView.IsEnabled = false;
            });

            await Task.Delay(180);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ConfigureScanner();
                QRScannerView.IsEnabled = true;
                QRScannerView.IsDetecting = true;
            });
        }

        // ============ TÍNH NĂNG CHỌN ẢNH ============
        private async void OnPickImageClicked(object sender, EventArgs e)
        {
            if (_isProcessing)
            {
                Debug.WriteLine("QRCodePage: Bỏ qua chọn ảnh vì đang xử lý mã QR...");
                return;
            }

            // luôn làm sạch trạng thái cũ trước khi chọn ảnh
            _hasNavigated = false;
            _lastScannedResult = null;

            var language = _localizationService.CurrentLanguage;

            try
            {
                StopScanner();

                var photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = language == "en" ? "Select a QR image" : "Chọn ảnh chứa mã QR"
                });

                if (photo is null)
                {
                    await StartScannerAsync();
                    return;
                }

                var decodedValue = await DecodeQrFromPhotoAsync(photo);
                if (string.IsNullOrWhiteSpace(decodedValue))
                {
                    await DisplayAlert(
                        _localizationService.GetString("Error", language),
                        language == "en" ? "No QR code detected in selected image." : "Không phát hiện mã QR trong ảnh đã chọn.",
                        _localizationService.GetString("OK", language));

                    _isProcessing = false;
                    _lastScannedResult = null;
                    await StartScannerAsync();
                    return;
                }

                _isProcessing = true;
                _lastScannedResult = decodedValue;

                Debug.WriteLine($"✅ QRCodePage: Đọc QR từ ảnh thành công - {decodedValue}");
                await ProcessQRCodeAsync(decodedValue);
            }
            catch (FeatureNotSupportedException)
            {
                await DisplayAlert(
                    _localizationService.GetString("Error", language),
                    language == "en" ? "Photo picking is not supported on this device." : "Thiết bị không hỗ trợ chọn ảnh.",
                    _localizationService.GetString("OK", language));

                _isProcessing = false;
                _lastScannedResult = null;
                await StartScannerAsync();
            }
            catch (PermissionException)
            {
                await DisplayAlert(
                    _localizationService.GetString("PermissionError", language),
                    language == "en" ? "Photo access permission was denied." : "Quyền truy cập ảnh đã bị từ chối.",
                    _localizationService.GetString("OK", language));

                _isProcessing = false;
                _lastScannedResult = null;
                await StartScannerAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ QRCodePage.OnPickImageClicked: Lỗi - {ex.Message}");

                await DisplayAlert(
                    _localizationService.GetString("Error", language),
                    ex.Message,
                    _localizationService.GetString("OK", language));

                _isProcessing = false;
                _lastScannedResult = null;
                await StartScannerAsync();
            }
        }

        private static async Task<string?> DecodeQrFromPhotoAsync(FileResult photo)
        {
            await using var stream = await photo.OpenReadAsync();
            using var managedStream = new SKManagedStream(stream);
            using var bitmap = SKBitmap.Decode(managedStream);

            if (bitmap is null)
            {
                return null;
            }

            var result = ImageBarcodeReader.Decode(bitmap);
            return result?.Text?.Trim();
        }
    }
}