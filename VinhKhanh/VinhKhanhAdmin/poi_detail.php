<?php
require_once __DIR__ . '/includes/auth.php';
requireLogin();
checkTimeout();
requireRole(1);

require_once __DIR__ . '/config.php';
require_once __DIR__ . '/includes/firebase.php';

$fb = new FirebaseRTDB();

$id = isset($_GET['id']) ? $_GET['id'] : null;
if (!$id) {
    header('Location: pois.php');
    exit;
}

$poi = $fb->get('vinhkhanh/poi_submissions/' . $id);
if (!$poi) {
    $poi = $fb->get('vinhkhanh/pois/' . $id);
}

if (!$poi) {
    http_response_code(404);
    die('Not Found: POI không tồn tại.');
}

// Lấy danh sách menu items
$allMenuItems = $fb->get('vinhkhanh/menu_items') ?: [];
$poiMenus = [];
if (is_array($allMenuItems)) {
    foreach ($allMenuItems as $mid => $item) {
        if ($item && isset($item['PoiId']) && $item['PoiId'] == $id) {
            $item['id'] = $mid;
            $poiMenus[] = $item;
        }
    }
}
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Chi tiết POI (Admin) - Vĩnh Khánh</title>
    <link rel="stylesheet" href="assets/style.css">
    
    <!-- TrackAsia GL JS cho Map Preview -->
    <link rel="stylesheet" href="https://unpkg.com/trackasia-gl@1.0.0/dist/trackasia-gl.css" />
    <script src="https://unpkg.com/trackasia-gl@1.0.0/dist/trackasia-gl.js"></script>
    <style>
        #map { height: 300px; width: 100%; border-radius: 8px; border: 1px solid var(--border-dark); }
        .detail-row { display: flex; margin-bottom: 0.5rem; border-bottom: 1px solid var(--border-dark); padding-bottom: 0.5rem; }
        .detail-label { width: 150px; font-weight: bold; color: var(--text-muted); }
        .detail-value { flex: 1; }
        .tabs { display: flex; border-bottom: 1px solid var(--border-dark); margin-bottom: 1rem; }
        .tab-btn { background: none; border: none; padding: 10px 15px; color: var(--text-light); cursor: pointer; border-bottom: 2px solid transparent; }
        .tab-btn.active { border-bottom-color: var(--primary-color); color: var(--primary-color); font-weight: bold; }
        .tab-content { display: none; background: rgba(0,0,0,0.2); padding: 15px; border-radius: var(--radius); }
        .tab-content.active { display: block; }
        .img-preview { max-width: 100%; height: auto; max-height: 300px; border-radius: var(--radius); object-fit: cover; }
        .readonly-field { background-color: var(--bg-dark); padding: 0.5rem; border-radius: 4px; border: 1px solid var(--border-dark); opacity: 0.8; }
        .btn-edit { background: #f59e0b; color: #fff; padding: 0.5rem 1rem; border-radius: var(--radius); text-decoration: none; }
        .btn-edit:hover { background: #d97706; }
    </style>
</head>
<body class="dark-theme">
    <div class="layout">
        <?php include 'includes/sidebar.php'; ?>

        <main class="main-content">
            <header class="header">
                <div style="display: flex; gap: 10px; align-items: center;">
                    <a href="pois.php" class="btn btn-back">← Quay lại</a>
                    <h1 style="margin:0;">Chi tiết POI: <?php echo htmlspecialchars($poi['Name'] ?? 'Không rõ'); ?></h1>
                </div>
                <div class="user-greeting" style="font-weight: bold; color: var(--text-light);">
                   Welcome, <?php echo htmlspecialchars($_SESSION['full_name'] ?? $_SESSION['username']); ?>
                    
                </div>
            </header>

            <div class="card" style="margin-bottom: 1.5rem;">
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1.5rem;">
                    <h2>Thông tin cơ bản</h2>
                    <a href="poi_form.php?id=<?php echo $id; ?>" class="btn-edit" style="color: #fff; border: 1px solid #f59e0b; background: transparent;">Chỉnh sửa POI</a>
                </div>
                
                <div style="display: grid; grid-template-columns: 1fr 2fr; gap: 2rem;">
                    <div>
                        <?php 
                        $imgUrl = $poi['ImageUrl'] ?? (isset($poi['ImageUrls'][0]) ? $poi['ImageUrls'][0] : '');
                        if ($imgUrl): 
                        ?>
                            <img src="<?php echo htmlspecialchars($imgUrl); ?>" alt="POI Image" class="img-preview" style="margin-bottom: 1rem;">
                        <?php else: ?>
                            <div style="width: 100%; height: 200px; background: #333; border-radius: 8px; display: flex; align-items: center; justify-content: center; color: #777; margin-bottom: 1rem;">Không có ảnh</div>
                        <?php endif; ?>
                        
                        <div id="map" style="margin-bottom: 1rem;"></div>
                    </div>
                    
                    <div>
                        <div style="display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem;">
                            <div style="flex: 1;">
                                <div class="detail-row">
                            <div class="detail-label">Tên POI</div>
                            <div class="detail-value"><?php echo htmlspecialchars($poi['Name'] ?? ''); ?></div>
                        </div>
                        <div class="detail-row">
                            <div class="detail-label">Địa chỉ</div>
                            <div class="detail-value"><?php echo htmlspecialchars($poi['Address'] ?? ''); ?></div>
                        </div>
                        <div class="detail-row">
                            <div class="detail-label">Trạng thái</div>
                            <div class="detail-value">
                                <span class="badge <?php echo isset($poi['IsActive']) && $poi['IsActive'] == 1 ? 'badge-active' : (isset($poi['IsActive']) && $poi['IsActive'] == 2 ? 'badge-pending' : 'badge-inactive'); ?>" <?php if (isset($poi['IsActive']) && $poi['IsActive'] == 2) echo 'style="background-color: #f59e0b;"'; ?>>
                                    <?php echo isset($poi['IsActive']) && $poi['IsActive'] == 1 ? 'Active' : (isset($poi['IsActive']) && $poi['IsActive'] == 2 ? 'Pending' : 'Hidden'); ?>
                                </span>
                            </div>
                        </div>
                        <div class="detail-row">
                            <div class="detail-label">Năm thành lập</div>
                            <div class="detail-value"><?php echo htmlspecialchars($poi['YearEstablished'] ?? 'N/A'); ?></div>
                        </div>
                        <div class="detail-row">
                            <div class="detail-label">Đánh giá (Rating)</div>
                            <div class="detail-value"><?php echo htmlspecialchars($poi['Rating'] ?? 'N/A'); ?> / 5</div>
                        </div>
                        <div class="detail-row">
                            <div class="detail-label">Mức ưu tiên (Priority)</div>
                            <div class="detail-value"><?php echo htmlspecialchars($poi['Priority'] ?? 'N/A'); ?></div>
                        </div>
                        <div class="detail-row">
                            <div class="detail-label">Bán kính (RadiusMeters)</div>
                            <div class="detail-value"><?php echo htmlspecialchars($poi['RadiusMeters'] ?? 'N/A'); ?> m</div>
                        </div>
                            </div>
                            
                            <div style="background: rgba(0,0,0,0.2); padding: 15px; border-radius: 8px; border: 1px solid var(--border-dark); text-align: center; width: 140px; flex-shrink: 0;">
                                <div style="font-size: 0.85rem; color: #bbb; margin-bottom: 10px; font-weight: bold; text-transform: uppercase;">QR Check-in</div>
                                <img src="https://api.qrserver.com/v1/create-qr-code/?size=110x110&data=poi:<?php echo $id; ?>&margin=2" alt="QR Code" style="background: white; border-radius: 6px; display: block; margin: 0 auto; width: 110px; height: 110px;">
                                <div style="margin-top: 8px; font-family: monospace; font-size: 0.95rem; color: var(--primary-color); font-weight: bold;">poi:<?php echo $id; ?></div>
                            </div>
                        </div>
                        
                        <h3 style="margin-top: 1.5rem; margin-bottom: 1rem; border-bottom: 1px solid var(--border-dark); padding-bottom: 0.5rem;">Nội dung hiển thị</h3>
                        
                        <div class="tabs">
                            <button class="tab-btn active" onclick="openTab('vi')">Tiếng Việt</button>
                            <button class="tab-btn" onclick="openTab('en')">English</button>
                            <button class="tab-btn" onclick="openTab('jp')">日本語 (Jp)</button>
                            <button class="tab-btn" onclick="openTab('zh')">中文 (Zh)</button>
                            <button class="tab-btn" onclick="openTab('ru')">Русский (Ru)</button>
                            <button class="tab-btn" onclick="openTab('fr')">Français (Fr)</button>
                        </div>
                        
                        <div id="tab-vi" class="tab-content active">
                            <strong>Mô tả:</strong> <div class="readonly-field" style="margin-bottom: 10px;"><?php echo nl2br(htmlspecialchars($poi['TextVi'] ?? 'Chưa cập nhật')); ?></div>
                            <strong>Lịch sử:</strong> <div class="readonly-field"><?php echo nl2br(htmlspecialchars($poi['History'] ?? 'Chưa cập nhật')); ?></div>
                        </div>
                        <div id="tab-en" class="tab-content">
                            <strong>Description:</strong> <div class="readonly-field" style="margin-bottom: 10px;"><?php echo nl2br(htmlspecialchars($poi['TextEn'] ?? 'N/A')); ?></div>
                            <strong>History:</strong> <div class="readonly-field"><?php echo nl2br(htmlspecialchars($poi['HistoryEn'] ?? 'N/A')); ?></div>
                            <strong>Address:</strong> <div class="readonly-field"><?php echo nl2br(htmlspecialchars($poi['AdrEn'] ?? 'N/A')); ?></div>
                        </div>
                        <div id="tab-jp" class="tab-content">
                            <strong>概要:</strong> <div class="readonly-field" style="margin-bottom: 10px;"><?php echo nl2br(htmlspecialchars($poi['TextJp'] ?? 'N/A')); ?></div>
                            <strong>歴史:</strong> <div class="readonly-field"><?php echo nl2br(htmlspecialchars($poi['HistoryJp'] ?? 'N/A')); ?></div>
                            <strong>住所:</strong> <div class="readonly-field"><?php echo nl2br(htmlspecialchars($poi['AdrJp'] ?? 'N/A')); ?></div>
                        </div>
                        <div id="tab-zh" class="tab-content">
                            <strong>描述:</strong> <div class="readonly-field" style="margin-bottom: 10px;"><?php echo nl2br(htmlspecialchars($poi['TextZh'] ?? 'N/A')); ?></div>
                            <strong>历史:</strong> <div class="readonly-field"><?php echo nl2br(htmlspecialchars($poi['HistoryZh'] ?? 'N/A')); ?></div>
                            <strong>地址:</strong> <div class="readonly-field"><?php echo nl2br(htmlspecialchars($poi['AdrZh'] ?? 'N/A')); ?></div>
                        </div>
                        <div id="tab-ru" class="tab-content">
                            <strong>Описание:</strong> <div class="readonly-field" style="margin-bottom: 10px;"><?php echo nl2br(htmlspecialchars($poi['TextRu'] ?? 'N/A')); ?></div>
                            <strong>История:</strong> <div class="readonly-field"><?php echo nl2br(htmlspecialchars($poi['HistoryRu'] ?? 'N/A')); ?></div>
                            <strong>Адрес:</strong> <div class="readonly-field"><?php echo nl2br(htmlspecialchars($poi['AdrRu'] ?? 'N/A')); ?></div>
                        </div>
                        <div id="tab-fr" class="tab-content">
                            <strong>Description:</strong> <div class="readonly-field" style="margin-bottom: 10px;"><?php echo nl2br(htmlspecialchars($poi['TextFr'] ?? 'N/A')); ?></div>
                            <strong>Histoire:</strong> <div class="readonly-field"><?php echo nl2br(htmlspecialchars($poi['HistoryFr'] ?? 'N/A')); ?></div>
                            <strong>Adresse:</strong> <div class="readonly-field"><?php echo nl2br(htmlspecialchars($poi['AdrFr'] ?? 'N/A')); ?></div>
                        </div>
                        
                    </div>
                </div>
            </div>

            <!-- Menu Items Card -->
            <div class="card">
                <h2>Danh sách Menu của POI này (<?php echo count($poiMenus); ?> món)</h2>
                <?php if (empty($poiMenus)): ?>
                    <p style="color: var(--text-muted); padding: 1rem;">Chưa có món ăn nào.</p>
                <?php else: ?>
                    <div class="table-responsive">
                        <table class="table">
                            <thead>
                                <tr>
                                    <th>Ảnh</th>
                                    <th>Tên món</th>
                                    <th>Giá</th>
                                    <th>Món đặc trưng</th>
                                </tr>
                            </thead>
                            <tbody>
                                <?php foreach ($poiMenus as $menu): ?>
                                <tr>
                                    <td>
                                        <?php if (!empty($menu['ImageUrl'])): ?>
                                            <img src="<?php echo htmlspecialchars($menu['ImageUrl']); ?>" width="50" height="50" style="object-fit:cover; border-radius:4px;">
                                        <?php else: ?>
                                            <div style="width:50px; height:50px; background:#333; border-radius:4px; display:flex; align-items:center; justify-content:center;">🍲</div>
                                        <?php endif; ?>
                                    </td>
                                    <td><?php echo htmlspecialchars($menu['Name'] ?? ''); ?></td>
                                    <td><?php echo htmlspecialchars($menu['Price'] ?? ''); ?></td>
                                    <td>
                                        <?php if (isset($menu['IsSignature']) && $menu['IsSignature']): ?>
                                            <span class="badge" style="background-color: #f59e0b; color: white;">★ Signature</span>
                                        <?php else: ?>
                                            <span style="color: var(--text-muted);">-</span>
                                        <?php endif; ?>
                                    </td>
                                </tr>
                                <?php endforeach; ?>
                            </tbody>
                        </table>
                    </div>
                <?php endif; ?>
            </div>
            
        </main>
    </div>

    <!-- Scripts for Tab & Map -->
    <script>
        function openTab(lang) {
            document.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('active'));
            document.querySelectorAll('.tab-content').forEach(content => content.classList.remove('active'));
            event.target.classList.add('active');
            document.getElementById('tab-' + lang).classList.add('active');
        }

        // Initialize Map
        const lat = <?php echo floatval($poi['Lat'] ?? 10.765); ?>;
        const lng = <?php echo floatval($poi['Lng'] ?? 106.682); ?>;

        trackasiagl.accessToken = '3a82d12156488a8391773657171aacb765';

        const map = new trackasiagl.Map({
            container: 'map',
            style: 'https://maps.track-asia.com/styles/v2/streets.json?key=3a82d12156488a8391773657171aacb765',
            center: [lng, lat],
            zoom: 16,
            interactive: false // Không cho drag, scroll
        });

        // Add Marker
        new trackasiagl.Marker({ color: 'red' })
            .setLngLat([lng, lat])
            .addTo(map);
    </script>
</body>
</html>