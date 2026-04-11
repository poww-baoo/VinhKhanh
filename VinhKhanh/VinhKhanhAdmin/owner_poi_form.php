<?php
require_once __DIR__ . '/includes/auth.php';
requireLogin();
checkTimeout();
requireRole(2);

require_once __DIR__ . '/config.php';
require_once __DIR__ . '/includes/firebase.php';
require_once __DIR__ . '/includes/cloudinary.php';

$fb = new FirebaseRTDB();

$categories = $fb->get('vinhkhanh/categories') ?: [];

$id = isset($_GET['id']) ? intval($_GET['id']) : null;
$poi = null;
if ($id) {
    $poi = $fb->get('vinhkhanh/pois/' . $id);
    if (!$poi || !isset($poi['OwnerId']) || $poi['OwnerId'] != $_SESSION['user_id']) {
        http_response_code(403);
        die('Forbidden: Bạn không có quyền chỉnh sửa POI này.');
    }
}

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    if (!$id) {
        $lastPoi = $fb->get('vinhkhanh/pois', ['orderBy' => '"$key"', 'limitToLast' => 1]);
        $saveId = empty($lastPoi) ? 1 : max(array_keys($lastPoi)) + 1;
        
        $priority = 3;
        $radiusMeters = 20;
        $isActive = 2; // Pending default for owner
    } else {
        $saveId = $id;
        
        // Keep existing values or defaults for edits since owners can't edit these fields
        $priority = $poi['Priority'] ?? 3;
        $radiusMeters = $poi['RadiusMeters'] ?? 20;
        $isActive = $poi['IsActive'] ?? 2;
    }
    
    $imageUrl = $poi['ImageUrl'] ?? null;
    $cloudinaryId = $poi['CloudinaryId'] ?? null;
    $imageFileName = $_POST['ImageFileName'] ?? ($poi['ImageFileName'] ?? '');
    
    // Handle Image Upload
    if (isset($_FILES['image']) && $_FILES['image']['error'] === UPLOAD_ERR_OK) {
        $fileTmpPath = $_FILES['image']['tmp_name'];
        $originalName = $_FILES['image']['name'];
        
        $newPublicId = "vinhkhanh/poi_" . $saveId . "_" . time();
        $uploadResult = uploadToCloudinary($fileTmpPath, $newPublicId);
        
        if ($uploadResult && isset($uploadResult['secure_url'])) {
            if ($cloudinaryId) {
                deleteFromCloudinary($cloudinaryId);
            }
            
            $imageUrl = $uploadResult['secure_url'];
            $cloudinaryId = $uploadResult['public_id'];
            $imageFileName = $originalName; 
        }
    }

    $newPoi = [
        'Id' => $saveId,
        'OwnerId' => $_SESSION['user_id'],
        'CategoryId' => intval($_POST['CategoryId']),
        'Name' => $_POST['Name'],
        'History' => $_POST['History'] ?? '',
        'HistoryEn' => $_POST['HistoryEn'] ?? '',
        'HistoryFr' => $_POST['HistoryFr'] ?? '',
        'HistoryJp' => $_POST['HistoryJp'] ?? '',
        'HistoryRu' => $_POST['HistoryRu'] ?? '',
        'HistoryZh' => $_POST['HistoryZh'] ?? '',
        'TextVi' => $_POST['TextVi'] ?? '',
        'TextEn' => $_POST['TextEn'] ?? '',
        'TextJp' => $_POST['TextJp'] ?? '',
        'TextZh' => $_POST['TextZh'] ?? '',
        'TextRu' => $_POST['TextRu'] ?? '',
        'TextFr' => $_POST['TextFr'] ?? '',
        'Lat' => floatval($_POST['Lat'] ?? 0),
        'Lng' => floatval($_POST['Lng'] ?? 0),
        'RadiusMeters' => intval($radiusMeters),
        'Priority' => intval($priority),
        'YearEstablished' => intval($_POST['YearEstablished'] ?? 0),
        'Rating' => floatval($poi['Rating'] ?? 0),
        'ImageFileName' => $imageFileName,
        'ImageUrl' => $imageUrl,
        'CloudinaryId' => $cloudinaryId,
        'IsActive' => intval($isActive),
        'Address' => $_POST['Address'] ?? '',
        'AdrEn' => $_POST['AdrEn'] ?? '',
        'AdrFr' => $_POST['AdrFr'] ?? '',
        'AdrJp' => $_POST['AdrJp'] ?? '',
        'AdrRu' => $_POST['AdrRu'] ?? '',
        'AdrZh' => $_POST['AdrZh'] ?? ''
    ];

    $fb->set('vinhkhanh/pois/' . $saveId, $newPoi);
    header("Location: owner_poi_detail.php?id=" . $saveId);
    exit;
}
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title><?php echo $poi ? 'Chỉnh sửa POI' : 'Tạo POI mới'; ?> - VK Owner</title>
    <link rel="stylesheet" href="assets/style.css">
    
    <link rel="stylesheet" href="https://unpkg.com/trackasia-gl@1.0.0/dist/trackasia-gl.css" />
    <script src="https://unpkg.com/trackasia-gl@1.0.0/dist/trackasia-gl.js"></script>
    <style>
        #map { height: 300px; width: 100%; border-radius: 8px; margin-top: 8px; border: 1px solid var(--border); }
        .readonly-field { background-color: var(--bg-dark); opacity: 0.7; cursor: not-allowed; }
    </style>
</head>
<body class="dark-theme role-owner">
    <div class="layout">
        <?php include 'includes/sidebar.php'; ?>

        <main class="main-content">
            <header class="header">
                <a href="<?php echo $poi ? 'owner_poi_detail.php?id='.$id : 'owner_pois.php'; ?>" class="btn btn-back">← Quay lại</a>
                <h1><?php echo $poi ? 'Chỉnh sửa: '.htmlspecialchars($poi['Name']) : 'Tạo POI mới'; ?></h1>
            </header>

            <form method="POST" class="form-container" style="max-width: 800px; margin: 0 auto;" enctype="multipart/form-data">
                <div class="form-grid">
                    <div class="form-group" style="grid-column: span 2;">
                        <label>Tên POI</label>
                        <input type="text" name="Name" value="<?php echo htmlspecialchars($poi['Name'] ?? ''); ?>" required>
                    </div>

                    <div class="form-group" style="grid-column: span 2;">
                        <label>Danh mục</label>
                        <select name="CategoryId" class="form-control" required>
                            <option value="">-- Chọn danh mục --</option>
                            <?php foreach ($categories as $cat): 
                                if (!$cat) continue;
                            ?>
                                <option value="<?php echo $cat['Id']; ?>" <?php echo ($poi['CategoryId'] ?? 0) == $cat['Id'] ? 'selected' : ''; ?>>
                                    <?php echo htmlspecialchars($cat['Name']); ?>
                                </option>
                            <?php endforeach; ?>
                        </select>
                    </div>
                    
                    <div class="form-group" style="grid-column: span 2;">
                        <label>Địa chỉ (Tiếng Việt)</label>
                        <div style="display: flex; gap: 10px;">
                            <input type="text" id="Address" name="Address" value="<?php echo htmlspecialchars($poi['Address'] ?? ''); ?>" style="flex: 1;">
                            <button type="button" id="btnTranslateAddress" class="btn btn-secondary" onclick="translateAddress()">Dịch tự động</button>
                        </div>
                    </div>

                    <div class="form-group" style="grid-column: span 2;">
                        <details>
                            <summary style="cursor: pointer; color: var(--owner-accent);">Chỉnh sửa địa chỉ đa ngôn ngữ (En, Jp, Zh, Ru, Fr)</summary>
                            <div class="form-grid" style="margin-top: 10px;">
                                <div class="form-group">
                                    <label>Địa chỉ (En)</label>
                                    <input type="text" id="AdrEn" name="AdrEn" value="<?php echo htmlspecialchars($poi['AdrEn'] ?? ''); ?>">
                                </div>
                                <div class="form-group">
                                    <label>Địa chỉ (Jp)</label>
                                    <input type="text" id="AdrJp" name="AdrJp" value="<?php echo htmlspecialchars($poi['AdrJp'] ?? ''); ?>">
                                </div>
                                <div class="form-group">
                                    <label>Địa chỉ (Zh)</label>
                                    <input type="text" id="AdrZh" name="AdrZh" value="<?php echo htmlspecialchars($poi['AdrZh'] ?? ''); ?>">
                                </div>
                                <div class="form-group">
                                    <label>Địa chỉ (Ru)</label>
                                    <input type="text" id="AdrRu" name="AdrRu" value="<?php echo htmlspecialchars($poi['AdrRu'] ?? ''); ?>">
                                </div>
                                <div class="form-group">
                                    <label>Địa chỉ (Fr)</label>
                                    <input type="text" id="AdrFr" name="AdrFr" value="<?php echo htmlspecialchars($poi['AdrFr'] ?? ''); ?>">
                                </div>
                            </div>
                        </details>
                    </div>

                    <div class="form-group">
                        <label>Vĩ độ (Lat)</label>
                        <input type="number" step="0.000001" id="Lat" name="Lat" value="<?php echo $poi['Lat'] ?? 10.765; ?>" required onchange="updateMarker()">
                    </div>
                    <div class="form-group">
                        <label>Kinh độ (Lng)</label>
                        <input type="number" step="0.000001" id="Lng" name="Lng" value="<?php echo $poi['Lng'] ?? 106.682; ?>" required onchange="updateMarker()">
                    </div>

                    <!-- Map Preview -->
                    <div class="form-group" style="grid-column: span 2;">
                        <label>Vị trí bản đồ</label>
                        <div id="map"></div>
                        <small>Kéo thả marker để chọn vị trí lấy Lat/Lng.</small>
                    </div>
                    
                    <div class="form-group" style="grid-column: span 2;">
                        <label>Năm thành lập</label>
                        <input type="number" name="YearEstablished" value="<?php echo $poi['YearEstablished'] ?? ''; ?>">
                    </div>

                    <div class="form-group" style="grid-column: span 2;">
                        <label>Ảnh POI</label>
                        <?php if (!empty($poi['ImageUrl'])): ?>
                            <div style="margin-bottom: 10px;">
                                <img src="<?php echo htmlspecialchars($poi['ImageUrl']); ?>" alt="Current Image" style="max-width: 200px; max-height: 200px; border-radius: 8px; border: 1px solid var(--border);">
                            </div>
                        <?php endif; ?>
                        <input type="file" name="image" accept="image/*" class="form-control">
                        <small>Tải lên ảnh mới để thay thế. Nếu không chọn sẽ giữ ảnh cũ.</small>
                    </div>

                </div>

                <hr style="margin: 20px 0; border-color: var(--border);">
                <h3>Nội dung Đa ngôn ngữ</h3>
                
                <div class="form-group" style="grid-column: span 2;">
                    <label>Mô tả Ngắn (Tiếng Việt)</label>
                    <div style="display: flex; gap: 10px;">
                        <textarea id="TextVi" name="TextVi" rows="3" style="flex: 1;"><?php echo htmlspecialchars($poi['TextVi'] ?? ''); ?></textarea>
                        <button type="button" id="btnTranslateText" class="btn btn-secondary" onclick="translateText()">Dịch tự động</button>
                    </div>
                </div>

                <div class="form-group" style="grid-column: span 2; margin-top: 15px;">
                    <label>Lịch sử / Mô tả chi tiết (Tiếng Việt)</label>
                    <div style="display: flex; gap: 10px;">
                        <textarea id="History" name="History" rows="5" style="flex: 1;"><?php echo htmlspecialchars($poi['History'] ?? ''); ?></textarea>
                        <button type="button" id="btnTranslateHistory" class="btn btn-secondary" onclick="translateHistory()">Dịch tự động</button>
                    </div>
                </div>
                
                <div class="form-group" style="grid-column: span 2; margin-top: 15px;">
                    <details>
                        <summary style="cursor: pointer; padding: 10px 0; color: var(--owner-accent);">Chỉnh sửa nội dung các ngôn ngữ khác (En, Jp, Zh, Ru, Fr)</summary>
                        <div class="form-grid">
                            <div class="form-group">
                                <label>Text (English)</label>
                                <textarea id="TextEn" name="TextEn" rows="2"><?php echo htmlspecialchars($poi['TextEn'] ?? ''); ?></textarea>
                            </div>
                            <div class="form-group">
                                <label>History (English)</label>
                                <textarea id="HistoryEn" name="HistoryEn" rows="2"><?php echo htmlspecialchars($poi['HistoryEn'] ?? ''); ?></textarea>
                            </div>
                            
                            <div class="form-group">
                                <label>Text (Japanese)</label>
                                <textarea id="TextJp" name="TextJp" rows="2"><?php echo htmlspecialchars($poi['TextJp'] ?? ''); ?></textarea>
                            </div>
                            <div class="form-group">
                                <label>History (Japanese)</label>
                                <textarea id="HistoryJp" name="HistoryJp" rows="2"><?php echo htmlspecialchars($poi['HistoryJp'] ?? ''); ?></textarea>
                            </div>

                            <div class="form-group">
                                <label>Text (Chinese)</label>
                                <textarea id="TextZh" name="TextZh" rows="2"><?php echo htmlspecialchars($poi['TextZh'] ?? ''); ?></textarea>
                            </div>
                            <div class="form-group">
                                <label>History (Chinese)</label>
                                <textarea id="HistoryZh" name="HistoryZh" rows="2"><?php echo htmlspecialchars($poi['HistoryZh'] ?? ''); ?></textarea>
                            </div>

                            <div class="form-group">
                                <label>Text (Russian)</label>
                                <textarea id="TextRu" name="TextRu" rows="2"><?php echo htmlspecialchars($poi['TextRu'] ?? ''); ?></textarea>
                            </div>
                            <div class="form-group">
                                <label>History (Russian)</label>
                                <textarea id="HistoryRu" name="HistoryRu" rows="2"><?php echo htmlspecialchars($poi['HistoryRu'] ?? ''); ?></textarea>
                            </div>

                            <div class="form-group">
                                <label>Text (French)</label>
                                <textarea id="TextFr" name="TextFr" rows="2"><?php echo htmlspecialchars($poi['TextFr'] ?? ''); ?></textarea>
                            </div>
                            <div class="form-group">
                                <label>History (French)</label>
                                <textarea id="HistoryFr" name="HistoryFr" rows="2"><?php echo htmlspecialchars($poi['HistoryFr'] ?? ''); ?></textarea>
                            </div>
                        </div>
                    </details>
                </div>

                <div class="form-actions" style="margin-top: 30px;">
                    <button type="submit" class="btn btn-primary" style="width: 100%; font-size: 1.1em; padding: 12px;"><?php echo $poi ? 'Cập nhật POI' : 'Tạo POI chờ duyệt'; ?></button>
                </div>
            </form>
        </main>
    </div>

    <!-- Map & Translate Script -->
    <script>
        const inputLat = document.getElementById('Lat');
        const inputLng = document.getElementById('Lng');
        
        const initialLat = parseFloat(inputLat.value) || 10.765;
        const initialLng = parseFloat(inputLng.value) || 106.682;

        trackasiagl.accessToken = '3a82d12156488a8391773657171aacb765';
        
        const map = new trackasiagl.Map({
            container: 'map',
            style: 'https://maps.track-asia.com/styles/v2/streets.json?key=3a82d12156488a8391773657171aacb765',
            center: [initialLng, initialLat],
            zoom: 15
        });

        const marker = new trackasiagl.Marker({ draggable: true })
            .setLngLat([initialLng, initialLat])
            .addTo(map);

        marker.on('dragend', function() {
            const lngLat = marker.getLngLat();
            inputLat.value = lngLat.lat.toFixed(6);
            inputLng.value = lngLat.lng.toFixed(6);
        });

        function updateMarker() {
            const lat = parseFloat(inputLat.value) || 10.765;
            const lng = parseFloat(inputLng.value) || 106.682;
            marker.setLngLat([lng, lat]);
            map.flyTo({ center: [lng, lat] });
        }

        // Auto Translate
        async function googleTranslate(text, targetLang) {
            if (!text) return '';
            const url = `https://translate.googleapis.com/translate_a/single?client=gtx&sl=vi&tl=${targetLang}&dt=t&q=${encodeURIComponent(text)}`;
            try {
                const response = await fetch(url);
                const data = await response.json();
                return data[0].map(item => item[0]).join('');
            } catch (e) {
                console.error('Translation error', e);
                return '';
            }
        }

        async function translateAddress() {
            const viText = document.getElementById('Address').value;
            if (!viText) return;
            const btn = document.getElementById('btnTranslateAddress');
            const originalText = btn.innerText;
            btn.innerText = 'Đang dịch...';
            btn.disabled = true;
            
            document.getElementById('AdrEn').value = await googleTranslate(viText, 'en');
            document.getElementById('AdrJp').value = await googleTranslate(viText, 'ja');
            document.getElementById('AdrZh').value = await googleTranslate(viText, 'zh-CN');
            document.getElementById('AdrRu').value = await googleTranslate(viText, 'ru');
            document.getElementById('AdrFr').value = await googleTranslate(viText, 'fr');
            
            btn.innerText = originalText;
            btn.disabled = false;
        }
        
        async function translateHistory() {
            const viText = document.getElementById('History').value;
            if (!viText) return;
            const btn = document.getElementById('btnTranslateHistory');
            const originalText = btn.innerText;
            btn.innerText = 'Đang dịch...';
            btn.disabled = true;
            
            document.getElementById('HistoryEn').value = await googleTranslate(viText, 'en');
            document.getElementById('HistoryJp').value = await googleTranslate(viText, 'ja');
            document.getElementById('HistoryZh').value = await googleTranslate(viText, 'zh-CN');
            document.getElementById('HistoryRu').value = await googleTranslate(viText, 'ru');
            document.getElementById('HistoryFr').value = await googleTranslate(viText, 'fr');
            
            btn.innerText = originalText;
            btn.disabled = false;
        }

        async function translateText() {
            const viText = document.getElementById('TextVi').value;
            if (!viText) return;
            const btn = document.getElementById('btnTranslateText');
            const originalText = btn.innerText;
            btn.innerText = 'Đang dịch...';
            btn.disabled = true;
            
            document.getElementById('TextEn').value = await googleTranslate(viText, 'en');
            document.getElementById('TextJp').value = await googleTranslate(viText, 'ja');
            document.getElementById('TextZh').value = await googleTranslate(viText, 'zh-CN');
            document.getElementById('TextRu').value = await googleTranslate(viText, 'ru');
            document.getElementById('TextFr').value = await googleTranslate(viText, 'fr');
            
            btn.innerText = originalText;
            btn.disabled = false;
        }
    </script>
</body>
</html>