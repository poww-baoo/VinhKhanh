<?php
require_once __DIR__ . '/includes/auth.php';
requireLogin();
checkTimeout();
requireRole(1);

require_once __DIR__ . '/config.php';
require_once __DIR__ . '/includes/firebase.php';
require_once __DIR__ . '/includes/cloudinary.php';

$fb = new FirebaseRTDB();

$categories = $fb->get('vinhkhanh/categories') ?: [];
$allUsers = $fb->get('vinhkhanh/users') ?: [];
$owners = array_filter($allUsers, function($u) {
    return $u && isset($u['Role']) && $u['Role'] == 2;
});

$id = isset($_GET['id']) ? intval($_GET['id']) : null;
$poi = null;
if ($id) {
    $poi = $fb->get('vinhkhanh/pois/' . $id);
}

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    if (!$id) {
        $lastPoi = $fb->get('vinhkhanh/pois', ['orderBy' => '"$key"', 'limitToLast' => 1]);
        $saveId = empty($lastPoi) ? 1 : max(array_keys($lastPoi)) + 1;
    } else {
        $saveId = $id;
    }
    
    $imageUrl = $poi['ImageUrl'] ?? null;
    $cloudinaryId = $poi['CloudinaryId'] ?? null;
    $imageFileName = $_POST['ImageFileName'] ?? ($poi['ImageFileName'] ?? '');
    
    // Handle Image Upload
    if (isset($_FILES['image']) && $_FILES['image']['error'] === UPLOAD_ERR_OK) {
        $fileTmpPath = $_FILES['image']['tmp_name'];
        $originalName = $_FILES['image']['name'];
        
        $newPublicId = "vinhkhanh/poi_{$saveId}_" . time();
        $uploadResult = uploadToCloudinary($fileTmpPath, $newPublicId);
        
        if ($uploadResult && isset($uploadResult['secure_url'])) {
            // Delete old image if exists
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
        'Lat' => floatval($_POST['Lat']),
        'Lng' => floatval($_POST['Lng']),
        'RadiusMeters' => floatval($_POST['RadiusMeters']),
        'Priority' => intval($_POST['Priority']),
        'YearEstablished' => intval($_POST['YearEstablished']),
        'Rating' => floatval($_POST['Rating']),
        'ImageFileName' => $imageFileName,
        'ImageUrl' => $imageUrl,
        'CloudinaryId' => $cloudinaryId,
        'IsActive' => intval($_POST['IsActive']),
        'OwnerId' => (isset($_POST['OwnerId']) && $_POST['OwnerId'] !== '') ? intval($_POST['OwnerId']) : null,
        'Address' => $_POST['Address'] ?? '',
        'AdrEn' => $_POST['AdrEn'] ?? '',
        'AdrFr' => $_POST['AdrFr'] ?? '',
        'AdrJp' => $_POST['AdrJp'] ?? '',
        'AdrRu' => $_POST['AdrRu'] ?? '',
        'AdrZh' => $_POST['AdrZh'] ?? ''
    ];

    $fb->set('vinhkhanh/pois/' . $saveId, $newPoi);
    header("Location: pois.php");
    exit;
}
?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title><?php echo $poi ? 'Edit POI' : 'New POI'; ?> - VK Admin</title>
    <link rel="stylesheet" href="assets/style.css">
    
    <!-- TrackAsia GL JS & CSS cho Map Preview -->
    <link rel="stylesheet" href="https://unpkg.com/trackasia-gl@1.0.0/dist/trackasia-gl.css" />
    <script src="https://unpkg.com/trackasia-gl@1.0.0/dist/trackasia-gl.js"></script>
    <style>
        #map { height: 300px; width: 100%; border-radius: 8px; margin-top: 8px; border: 1px solid var(--border); }
    </style>
</head>
<body>
    <div class="layout">
        <?php include 'includes/sidebar.php'; ?>

        <main class="main-content">
            <header class="header">
                <a href="pois.php" class="btn btn-back">← Quay lại</a>
                <h1><?php echo $poi ? 'Edit POI' : 'Create POI'; ?></h1>
            </header>

            <form method="POST" class="form-container" style="max-width: 800px; margin: 0 auto;" enctype="multipart/form-data">
                <div class="form-grid">
                    <div class="form-group">
                        <label>POI Name</label>
                        <input type="text" name="Name" value="<?php echo htmlspecialchars($poi['Name'] ?? ''); ?>" required>
                    </div>
                    <div class="form-group">
                        <label>Category</label>
                        <select name="CategoryId" class="form-control" required>
                            <option value="">-- Select --</option>
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
                        <label>Chủ POI (Owner)</label>
                        <select name="OwnerId" class="form-control">
                            <option value="">-- Không có (Admin quản lý) --</option>
                            <?php foreach ($owners as $owner): ?>
                                <option value="<?php echo $owner['Id']; ?>" <?php echo isset($poi['OwnerId']) && $poi['OwnerId'] == $owner['Id'] ? 'selected' : ''; ?>>
                                    <?php echo htmlspecialchars(($owner['FullName'] ?? $owner['Username']) . ' (' . $owner['Username'] . ')'); ?>
                                </option>
                            <?php endforeach; ?>
                        </select>
                    </div>
                    
                    <div class="form-group" style="grid-column: span 2;">
                        <label>Address</label>
                        <div style="display: flex; gap: 10px;">
                            <input type="text" id="Address" name="Address" value="<?php echo htmlspecialchars($poi['Address'] ?? ''); ?>" style="flex: 1;">
                            <button type="button" id="btnTranslateAddress" class="btn btn-secondary" onclick="translateAddress()">Auto Translate</button>
                        </div>
                    </div>

                    <div class="form-group" style="grid-column: span 2;">
                        <details>
                            <summary style="cursor: pointer; color: var(--accent);">Edit Address in Other Languages (En, Jp, Zh, Ru, Fr)</summary>
                            <div class="form-grid" style="margin-top: 10px;">
                                <div class="form-group">
                                    <label>Address (En)</label>
                                    <input type="text" id="AdrEn" name="AdrEn" value="<?php echo htmlspecialchars($poi['AdrEn'] ?? ''); ?>">
                                </div>
                                <div class="form-group">
                                    <label>Address (Jp)</label>
                                    <input type="text" id="AdrJp" name="AdrJp" value="<?php echo htmlspecialchars($poi['AdrJp'] ?? ''); ?>">
                                </div>
                                <div class="form-group">
                                    <label>Address (Zh)</label>
                                    <input type="text" id="AdrZh" name="AdrZh" value="<?php echo htmlspecialchars($poi['AdrZh'] ?? ''); ?>">
                                </div>
                                <div class="form-group">
                                    <label>Address (Ru)</label>
                                    <input type="text" id="AdrRu" name="AdrRu" value="<?php echo htmlspecialchars($poi['AdrRu'] ?? ''); ?>">
                                </div>
                                <div class="form-group">
                                    <label>Address (Fr)</label>
                                    <input type="text" id="AdrFr" name="AdrFr" value="<?php echo htmlspecialchars($poi['AdrFr'] ?? ''); ?>">
                                </div>
                            </div>
                        </details>
                    </div>

                    <div class="form-group">
                        <label>Latitude</label>
                        <input type="number" step="0.000001" id="Lat" name="Lat" value="<?php echo $poi['Lat'] ?? 10.765; ?>" required onchange="updateMarker()">
                    </div>
                    <div class="form-group">
                        <label>Longitude</label>
                        <input type="number" step="0.000001" id="Lng" name="Lng" value="<?php echo $poi['Lng'] ?? 106.682; ?>" required onchange="updateMarker()">
                    </div>

                    <!-- Map Preview -->
                    <div class="form-group" style="grid-column: span 2;">
                        <label>Location Preview</label>
                        <div id="map"></div>
                        <small>Drag the marker to update Lat/Lng fields.</small>
                    </div>

                    <div class="form-group">
                        <label>Radius (Meters) for Geofence</label>
                        <input type="number" name="RadiusMeters" value="<?php echo $poi['RadiusMeters'] ?? 50; ?>" required>
                    </div>
                    <div class="form-group">
                        <label>Display Priority</label>
                        <input type="number" name="Priority" value="<?php echo $poi['Priority'] ?? 1; ?>">
                    </div>
                    
                    <div class="form-group">
                        <label>Year Established</label>
                        <input type="number" name="YearEstablished" value="<?php echo $poi['YearEstablished'] ?? ''; ?>">
                    </div>
                    <div class="form-group">
                        <label>Rating (0-5)</label>
                        <input type="number" step="0.1" name="Rating" value="<?php echo $poi['Rating'] ?? 0; ?>" min="0" max="5">
                    </div>

                    <div class="form-group" style="grid-column: span 2;">
                        <label>POI Image</label>
                        <?php if (!empty($poi['ImageUrl'])): ?>
                            <div style="margin-bottom: 10px;">
                                <img src="<?php echo htmlspecialchars($poi['ImageUrl']); ?>" alt="Current Image" style="max-width: 200px; max-height: 200px; border-radius: 8px; border: 1px solid var(--border);">
                            </div>
                        <?php endif; ?>
                        <input type="file" name="image" accept="image/*" class="form-control">
                        <small>Upload a new image to replace the current one. Leave empty to keep the existing image.</small>
                    </div>

                    <div class="form-group">
                        <label>Image File Name (Optional)</label>
                        <input type="text" name="ImageFileName" value="<?php echo htmlspecialchars($poi['ImageFileName'] ?? ''); ?>">
                    </div>
                    <div class="form-group">
                        <label>Status (IsActive)</label>
                        <select name="IsActive" class="form-control">
                            <option value="1" <?php echo !isset($poi) || ($poi['IsActive'] ?? 0) == 1 ? 'selected' : ''; ?>>Active (Show on app)</option>
                            <option value="0" <?php echo isset($poi) && ($poi['IsActive'] ?? 0) == 0 ? 'selected' : ''; ?>>Hidden</option>
                            <option value="2" <?php echo isset($poi) && ($poi['IsActive'] ?? 0) == 2 ? 'selected' : ''; ?>>Pending (Chờ duyệt)</option>
                        </select>
                    </div>
                </div>

                <hr style="margin: 20px 0; border-color: var(--border);">
                <h3>Multilanguage Content</h3>
                
                <div class="form-group">
                    <label>History / Description (vi)</label>
                    <div style="display: flex; gap: 10px;">
                        <textarea id="History" name="History" rows="3" style="flex: 1;"><?php echo htmlspecialchars($poi['History'] ?? ''); ?></textarea>
                        <button type="button" id="btnTranslateHistory" class="btn btn-secondary" onclick="translateHistory()">Auto Translate</button>
                    </div>
                </div>
                
                <div class="form-group">
                    <label>Text (Vietnamese)</label>
                    <div style="display: flex; gap: 10px;">
                        <textarea id="TextVi" name="TextVi" rows="3" style="flex: 1;"><?php echo htmlspecialchars($poi['TextVi'] ?? ''); ?></textarea>
                        <button type="button" id="btnTranslateText" class="btn btn-secondary" onclick="translateText()">Auto Translate</button>
                    </div>
                </div>
                <!-- Expandable for other languages -->
                <details>
                    <summary style="cursor: pointer; padding: 10px 0; color: var(--accent);">Edit Other Languages (En, Jp, Zh, Ru, Fr)</summary>
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

                <div class="form-actions" style="margin-top: 30px;">
                    <button type="submit" class="btn btn-primary" style="width: 100%; font-size: 1.1em; padding: 12px;"><?php echo $poi ? 'Update POI' : 'Create POI'; ?></button>
                </div>
            </form>
        </main>
    </div>

    <!-- Map Script -->
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
            btn.innerText = 'Translating...';
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
            btn.innerText = 'Translating...';
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
            btn.innerText = 'Translating...';
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

