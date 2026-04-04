using System.Diagnostics;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
using VinhKhanh.Models;
using VinhKhanh.Services;

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

        /// <summary>Trạng thái đèn pin (bật/tắt)</summary>
        private bool _isFlashlightOn = false;

        /// <summary>
        /// Cờ kiểm soát xử lý để tránh xử lý nhiều mã QR cùng lúc
        /// Khi đang xử lý mã này, không xử lý mã tiếp theo
        /// </summary>
        private bool _isProcessing = false;

        /// <summary>Dịch vụ đa ngôn ngữ (Tiếng Việt, Tiếng Anh)</summary>
        private readonly LocalizationService _localizationService;

        /// <summary>Dịch vụ quét mã QR và tìm kiếm nhà hàng</summary>
        private readonly QRCodeService _qrCodeService;

        /// <summary>Dịch vụ phát nhạc nền</summary>
        private readonly AudioPlaybackService _audioPlaybackService;

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
        /// QUAN TRỌNG: Delay 500ms để đảm bảo camera ready
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                Debug.WriteLine("QRCodePage.OnAppearing: Trang đang xuất hiện...");

                _isProcessing = false;
                _lastScannedResult = null;

                var hasPermission = await RequestCameraPermissionAsync();
                if (!hasPermission)
                {
                    QRScannerView.IsDetecting = false;
                    return;
                }

                // máy yếu cần thêm thời gian init camera
                await Task.Delay(700);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ConfigureScanner();
                    QRScannerView.IsTorchOn = _isFlashlightOn;
                    QRScannerView.IsDetecting = true;
                    Debug.WriteLine("QRCodePage: Camera được bật + scanner configured");
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"QRCodePage.OnAppearing: Lỗi - {ex.Message}");
            }
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

                _isFlashlightOn = false;
                QRScannerView.IsTorchOn = false;
                QRScannerView.IsDetecting = false;

                Debug.WriteLine("QRCodePage: Camera chuyển sang trạng thái nghỉ");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"QRCodePage.OnDisappearing: Lỗi - {ex.Message}");
            }
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
        /// </summary>
        private void UpdateUI()
        {
            var language = _localizationService.CurrentLanguage;
            Title = _localizationService.GetString("ScanQR", language);
            InstructionLabel.Text = _localizationService.GetString("QRInstruction", language);
        }

        // ============ QUẢN LÝ QUYỀN VÀ CAMERA ============
        /// <summary>
        /// Yêu cầu quyền truy cập camera từ hệ điều hành (async version)
        /// 
        /// MAUI PermissionStatus chỉ có 3 trạng thái:
        /// - Granted: Quyền được cấp
        /// - Denied: Quyền bị từ chối
        /// - Unknown: Trạng thái không xác định (chưa hỏi, hoặc lỗi)
        /// 
        /// Các bước:
        /// 1. Kiểm tra xem quyền camera đã được cấp chưa
        /// 2. Nếu chưa, yêu cầu người dùng cấp quyền
        /// 3. Nếu được cấp quyền, sẵn sàng quét
        /// 4. Nếu bị từ chối hoặc Unknown, hiển thị thông báo lỗi
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
        /// QUAN TRỌNG: Hàm này được gọi nhiều lần trên luồng nền
        /// Phải kiểm soát để tránh xử lý lặp lại
        /// 
        /// Luồng xử lý:
        /// 1. Kiểm tra xem có đang xử lý mã khác không
        /// 2. Lấy mã QR từ kết quả phát hiện
        /// 3. Kiểm tra ID có hợp lệ không
        /// 4. Gửi sang xử lý chính
        /// </summary>
        private void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
        {
            try
            {
                // Bước 1: Kiểm tra xem có đang xử lý mã khác không
                if (_isProcessing)
                {
                    return;
                }

                // Bước 2: Kiểm tra xem có kết quả phát hiện không
                if (e.Results == null || !e.Results.Any())
                {
                    return;
                }

                // Bước 3: Lấy mã QR đầu tiên
                var barcode = e.Results.FirstOrDefault();
                if (barcode == null)
                {
                    return;
                }

                var result = barcode.Value?.Trim();
                
                // Bước 4: Kiểm tra xem mã QR có hợp lệ không
                if (string.IsNullOrEmpty(result))
                {
                    Debug.WriteLine("QRCodePage: Mã QR trống");
                    return;
                }

                // Bước 5: Kiểm tra xem mã QR có trùng lặp với mã trước không
                if (result == _lastScannedResult)
                {
                    Debug.WriteLine($"QRCodePage: Mã QR trùng lặp - {result}");
                    return;
                }

                // Đánh dấu đang xử lý
                _isProcessing = true;
                _lastScannedResult = result;

                Debug.WriteLine($"✅ QRCodePage: Phát hiện mã QR mới - {result}");

                // Xử lý mã QR trên Main Thread (vì nó liên quan đến UI)
                MainThread.BeginInvokeOnMainThread(async () =>
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

                // Bước 1: Tắt máy quét để tránh phát hiện lặp lại
                QRScannerView.IsDetecting = false;

                // Bước 2: Tìm kiếm nhà hàng từ ID
                var restaurant = await _qrCodeService.GetRestaurantFromIdAsync(qrCodeId);

                if (restaurant != null)
                {
                    Debug.WriteLine($"✅ QRCodePage: Tìm thấy nhà hàng - {restaurant.Name}");

                    // Bước 3: Chuyển trang đến RestaurantDetailPage
                    await Shell.Current.GoToAsync(
                        $"restaurantdetail?id={restaurant.Id}",
                        new Dictionary<string, object>
                        {
                            { "restaurant", restaurant },
                            { "audioService", _audioPlaybackService }
                        });
                }
                else
                {
                    // Bước 4: Không tìm thấy nhà hàng
                    Debug.WriteLine($"❌ QRCodePage: Không tìm thấy nhà hàng với ID: {qrCodeId}");
                    
                    await DisplayAlert(
                        _localizationService.GetString("Error", language),
                        $"ID: {qrCodeId}\n\n{_localizationService.GetString("RestaurantNotFound", language)}\n\n{_localizationService.GetString("ScanOtherQR", language)}",
                        _localizationService.GetString("OK", language));

                    // Bước 5: Reset trạng thái để quét lại
                    _isProcessing = false;
                    _lastScannedResult = null;
                    QRScannerView.IsDetecting = true;
                }
            }
            catch (Exception ex)
            {
                var language = _localizationService.CurrentLanguage;
                Debug.WriteLine($"❌ QRCodePage.ProcessQRCodeAsync: Lỗi - {ex.Message}");

                await DisplayAlert(
                    _localizationService.GetString("Error", language),
                    $"{_localizationService.GetString("LoadRestaurantError", language)}\n{ex.Message}",
                    _localizationService.GetString("OK", language));

                // Reset trạng thái để quét lại
                _isProcessing = false;
                _lastScannedResult = null;
                QRScannerView.IsDetecting = true;
            }
        }

        // ============ QUẢN LÝ ĐÈN PIN ============
        /// <summary>
        /// Bật/tắt đèn pin của điện thoại
        /// 
        /// Các bước:
        /// 1. Chuyển đổi trạng thái đèn pin
        /// 2. Cập nhật màu nút để hiển thị trạng thái
        /// 3. Gọi API Flashlight để bật/tắt đèn pin thực tế
        /// 4. Hiển thị thông báo lỗi nếu thiết bị không hỗ trợ đèn pin
        /// </summary>
        private async void OnFlashlightToggleClicked(object sender, EventArgs e)
        {
            try
            {
                _isFlashlightOn = !_isFlashlightOn;
                QRScannerView.IsTorchOn = _isFlashlightOn;

                FlashlightToggleButton.BackgroundColor = _isFlashlightOn
                    ? Color.FromArgb("#FFC107")
                    : Color.FromArgb("#FF6B35");

                Debug.WriteLine($"QRCodePage: Torch = {(_isFlashlightOn ? "ON" : "OFF")}");
            }
            catch (Exception ex)
            {
                _isFlashlightOn = false;
                QRScannerView.IsTorchOn = false;
                FlashlightToggleButton.BackgroundColor = Color.FromArgb("#FF6B35");

                var language = _localizationService.CurrentLanguage;
                Debug.WriteLine($"❌ QRCodePage.OnFlashlightToggleClicked: Lỗi - {ex.Message}");

                await DisplayAlert(
                    _localizationService.GetString("Error", language),
                    _localizationService.GetString("FlashlightNotSupported", language),
                    _localizationService.GetString("OK", language));
            }
        }

        private void ConfigureScanner()
        {
            QRScannerView.Options = new BarcodeReaderOptions
            {
                // chỉ scan nhóm 2D (bao gồm QR), tương thích nhiều version ZXing.Net.Maui
                Formats = BarcodeFormats.TwoDimensional,
                AutoRotate = true,
                Multiple = false
            };

            QRScannerView.CameraLocation = CameraLocation.Rear;
        }
    }
}