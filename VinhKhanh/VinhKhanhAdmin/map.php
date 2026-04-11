<?php
require_once __DIR__ . '/config.php';
require_once __DIR__ . '/includes/firebase.php';

$fb = new FirebaseRTDB();
$pois = $fb->get('vinhkhanh/pois') ?: [];
$activePois = [];

foreach ($pois as $key => $poi) {
    if ($poi && isset($poi['Lat']) && isset($poi['Lng'])) {
        $activePois[] = $poi;
    }
}
?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>POI Map - VK Admin</title>
    <link rel="stylesheet" href="assets/style.css">
    
    <!-- TrackAsia GL JS & CSS -->
    <link rel="stylesheet" href="https://unpkg.com/trackasia-gl@1.0.0/dist/trackasia-gl.css" />
    <script src="https://unpkg.com/trackasia-gl@1.0.0/dist/trackasia-gl.js"></script>
    <style>
        .map-wrapper {
            position: relative;
            height: calc(100vh - 120px);
            width: 100%;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 4px 20px rgba(0,0,0,0.4);
            border: 1px solid var(--border);
        }
        #map {
            position: absolute;
            top: 0;
            bottom: 0;
            width: 100%;
        }
        .trackasiagl-popup-content {
            padding: 0;
            border-radius: 8px;
            box-shadow: 0 4px 15px rgba(0,0,0,0.15);
            max-width: 250px;
            border: none;
        }
        .trackasiagl-popup-close-button { display: none; }
        .poi-popup-img {
            width: 100%; height: 120px; object-fit: cover; border-radius: 8px 8px 0 0;
        }
        .poi-popup-body { padding: 12px; }
        .poi-popup-title { font-size: 16px; font-weight: bold; margin: 0 0 8px 0; color: #333; }
        .poi-popup-info { font-size: 13px; color: #666; margin: 4px 0; display: flex; align-items: center; }
        .poi-popup-info svg { width: 14px; height: 14px; margin-right: 6px; fill: var(--accent); }
        .poi-popup-info strong { margin-left: 5px; }
        .legend {
            position: absolute; top: 20px; right: 20px; background: var(--surface); padding: 15px;
            border-radius: 8px; box-shadow: 0 4px 20px rgba(0,0,0,0.5); z-index: 1; min-width: 220px;
            border: 1px solid var(--border);
            color: var(--text);
        }
        .legend h4 { margin: 0 0 15px 0; font-size: 15px; font-weight: 600; color: var(--text); }
        .legend-item { display: flex; align-items: center; margin-bottom: 10px; font-size: 13px; color: var(--muted); }
        .legend-color { width: 16px; height: 16px; border-radius: 50%; margin-right: 10px; }
        .legend-color.active-poi { background: var(--accent); }
        .legend-color.inactive-poi { background: var(--muted); }
        .legend-color.radius { border: 2px solid rgba(245, 158, 11, 0.4); background: rgba(245, 158, 11, 0.1); }
        .legend-toggle { display: flex; justify-content: space-between; align-items: center; margin-top: 15px; padding-top: 15px; border-top: 1px solid var(--border); font-size: 13px; color: var(--text); }
        .switch { position: relative; display: inline-block; width: 40px; height: 20px; }
        .switch input { opacity: 0; width: 0; height: 0; }
        .slider { position: absolute; cursor: pointer; top: 0; left: 0; right: 0; bottom: 0; background-color: var(--border); transition: .4s; border-radius: 20px; }
        .slider:before { position: absolute; content: ""; height: 16px; width: 16px; left: 2px; bottom: 2px; background-color: var(--muted); transition: .4s; border-radius: 50%; }
        input:checked + .slider { background-color: var(--accent); }
        input:checked + .slider:before { transform: translateX(20px); background-color: #000; }
        
        .custom-marker {
            width: 30px; height: 30px;
            background-image: url('data:image/svg+xml;utf8,<svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5c-1.38 0-2.5-1.12-2.5-2.5s1.12-2.5 2.5-2.5 2.5 1.12 2.5 2.5-1.12 2.5-2.5 2.5z" fill="%23f59e0b"/></svg>');
            background-size: cover; cursor: pointer; filter: drop-shadow(0 2px 3px rgba(0,0,0,0.5));
        }
        .custom-marker.inactive { background-image: url('data:image/svg+xml;utf8,<svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5c-1.38 0-2.5-1.12-2.5-2.5s1.12-2.5 2.5-2.5 2.5 1.12 2.5 2.5-1.12 2.5-2.5 2.5z" fill="%23555555"/></svg>'); }

        /* Modal Styles - Dark Theme */
        .modal-overlay {
            display: none; position: fixed; top: 0; left: 0; right: 0; bottom: 0;
            background: rgba(0,0,0,0.7); z-index: 1000; align-items: center; justify-content: center;
        }
        .modal-overlay.active { display: flex; }
        .poi-modal {
            background: var(--surface2); color: var(--text); border-radius: 16px; width: 90%; max-width: 900px;
            max-height: 90vh; overflow-y: auto; position: relative;
            box-shadow: 0 10px 40px rgba(0,0,0,0.5); display: flex; flex-direction: column;
            border: 1px solid var(--border);
        }
        .modal-header-banner { width: 100%; height: 200px; object-fit: cover; border-radius: 16px 16px 0 0; background: var(--surface); }
        .modal-close {
            position: absolute; top: 15px; right: 15px; width: 30px; height: 30px; border-radius: 50%;
            background: rgba(0,0,0,0.5); border: 1px solid var(--border); color: var(--text); cursor: pointer; display: flex; align-items: center; justify-content: center; font-size: 18px; z-index: 10;
        }
        .modal-close:hover { background: var(--surface); }
        .modal-body { padding: 30px; position: relative; }
        .modal-avatar-container {
            position: absolute; top: -60px; left: 30px; width: 120px; height: 120px;
            background: var(--surface2); border-radius: 12px; padding: 4px; box-shadow: 0 4px 10px rgba(0,0,0,0.3); border: 1px solid var(--border);
        }
        .modal-avatar { width: 100%; height: 100%; object-fit: cover; border-radius: 8px; }
        .modal-top-section { display: flex; justify-content: space-between; margin-top: 60px; margin-bottom: 30px; flex-wrap: wrap; gap: 20px;}
        .modal-info { flex: 1; }
        .modal-info h2 { font-size: 28px; margin: 0 0 10px 0; color: var(--text); font-family: 'Syne', sans-serif; }
        .modal-address { color: var(--muted); display: flex; align-items: center; margin-bottom: 15px; font-weight: 500; font-size: 14px;}
        .modal-address svg { width: 16px; height: 16px; margin-right: 5px; fill: var(--accent); }
        .modal-badges { display: flex; gap: 10px; flex-wrap: wrap; }
        .modal-badge {
            display: inline-flex; align-items: center; padding: 6px 12px; border: 1px solid var(--border);
            border-radius: 20px; font-size: 12px; font-weight: 600; text-transform: uppercase; color: var(--text); background: var(--surface); text-decoration: none;
        }
        .modal-badge svg { width: 14px; height: 14px; margin-right: 6px; }

        .modal-stats {
            background: #000; border: 1px solid var(--border); border-radius: 12px; padding: 20px; min-width: 250px;
        }
        .modal-stats h4 { margin: 0 0 15px 0; display: flex; align-items: center; font-size: 14px; color: var(--text); }
        .modal-stats h4 svg { width: 16px; height: 16px; margin-right: 8px; fill: var(--muted); }
        .stat-row { display: flex; justify-content: space-between; margin-bottom: 10px; font-size: 13px; color: var(--muted); }
        .stat-val { font-weight: bold; background: var(--surface2); padding: 2px 8px; border-radius: 4px; border: 1px solid var(--border); color: var(--text); }

        .modal-tabs { border-bottom: 1px solid var(--border); margin-bottom: 20px; display: flex; overflow-x: auto; }
        .modal-tab { padding: 10px 20px; cursor: pointer; border-bottom: 2px solid transparent; color: var(--muted); white-space: nowrap; font-weight: 600; font-size: 14px; }
        .modal-tab.active { color: var(--text); border-bottom-color: var(--accent); }
        .modal-tab-content-area { background: #000; border: 1px solid var(--border); border-radius: 8px; padding: 20px; min-height: 100px; }
        .modal-tab-content { display: none; color: var(--text); line-height: 1.6; font-size: 14px; white-space: pre-line; }
        .modal-tab-content.active { display: block; }
    </style>
</head>
<body>
    <div class="layout">
        <?php include 'includes/sidebar.php'; ?>

        <main class="main-content">
            <div class="header">
                <h1>POI Map</h1>
            </div>
            <div class="map-wrapper">
                <div id="map"></div>
                <div class="legend">
                    <h4>Chú giải Geofence</h4>
                    <div class="legend-item">
                        <div class="legend-color active-poi"></div>
                        <span>Điểm đang hoạt động</span>
                    </div>
                    <div class="legend-item">
                        <div class="legend-color inactive-poi"></div>
                        <span>Điểm tạm ngưng</span>
                    </div>
                    <div class="legend-item">
                        <div class="legend-color radius" style="border-radius: 50%;"></div>
                        <span>Vùng kích hoạt (Radius)</span>
                    </div>
                    <div class="legend-toggle">
                        <span>Hiện POI tạm ngưng</span>
                        <label class="switch">
                            <input type="checkbox" id="toggleInactive">
                            <span class="slider"></span>
                        </label>
                    </div>
                </div>
            </div>
            
            <!-- POPUP MODAL -->
            <div class="modal-overlay" id="poiModal">
                <div class="poi-modal">
                    <button class="modal-close" onclick="closeModal()">×</button>
                    <img src="" id="modalBanner" class="modal-header-banner" alt="Banner">
                    <div class="modal-body">
                        <div class="modal-avatar-container">
                            <img src="" id="modalAvatar" class="modal-avatar" alt="Avatar">
                        </div>
                        <div class="modal-top-section">
                            <div class="modal-info">
                                <h2 id="modalTitle">Tên quán</h2>
                                <div class="modal-address">
                                    <svg viewBox="0 0 24 24"><path d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5c-1.38 0-2.5-1.12-2.5-2.5s1.12-2.5 2.5-2.5 2.5 1.12 2.5 2.5-1.12 2.5-2.5 2.5z"/></svg> 
                                    <span id="modalAddress">Địa chỉ</span>
                                </div>
                                <div class="modal-badges">
                                    <div class="modal-badge" style="color: #f59e0b; border-color: #5c3a11; background: #2d1e08;">
                                        <svg viewBox="0 0 24 24" fill="none" stroke="#f59e0b" stroke-width="2"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/></svg>
                                        Bán kính: <span id="modalRadius">0</span>m
                                    </div>
                                    <a href="#" id="modalGoogleMaps" target="_blank" class="modal-badge" style="color: #60a5fa; border-color: #1e3a8a; background: #172554;">
                                        <svg viewBox="0 0 24 24" fill="#60a5fa"><path d="M12 2L4.5 20.29l.71.71L12 18l6.79 3 .71-.71z"/></svg>
                                        Mở Google Maps
                                    </a>
                                </div>
                            </div>
                            <div class="modal-stats">
                                <h4>
                                    <svg viewBox="0 0 24 24"><path d="M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zM9 17H7v-7h2v7zm4 0h-2V7h2v10zm4 0h-2v-4h2v4z"/></svg>
                                    Thống kê tương tác
                                </h4>
                                <div class="stat-row"><span>Lượt vào vùng (Visit):</span><span class="stat-val">0</span></div>
                                <div class="stat-row"><span>Lượt nghe Audio:</span><span class="stat-val">0</span></div>
                                <div class="stat-row"><span>Nghe trung bình:</span><span class="stat-val">0.0s</span></div>
                            </div>
                        </div>

                        <div class="modal-tabs">
                            <div class="modal-tab active" onclick="switchTab(this, 'vi')"><span style="color:#f59e0b;font-size:10px;margin-right:4px;">VN</span> Tiếng Việt</div>
                            <div class="modal-tab" onclick="switchTab(this, 'en')"><span style="color:#6b7280;font-size:10px;margin-right:4px;">GB</span> Tiếng Anh</div>
                            <div class="modal-tab" onclick="switchTab(this, 'zh')"><span style="color:#6b7280;font-size:10px;margin-right:4px;">CN</span> Tiếng Trung</div>
                            <div class="modal-tab" onclick="switchTab(this, 'jp')"><span style="color:#6b7280;font-size:10px;margin-right:4px;">JP</span> Tiếng Nhật</div>
                            <div class="modal-tab" onclick="switchTab(this, 'fr')"><span style="color:#6b7280;font-size:10px;margin-right:4px;">FR</span> Tiếng Pháp</div>
                            <div class="modal-tab" onclick="switchTab(this, 'ru')"><span style="color:#6b7280;font-size:10px;margin-right:4px;">RU</span> Tiếng Nga</div>
                        </div>

                        <div class="modal-tab-content-area">
                            <div style="font-weight: bold; margin-bottom: 15px; color: #9ca3af;font-size: 13px;display:flex;align-items:center;">
                                <svg style="width:16px;height:16px;margin-right:8px;fill:#9ca3af;" viewBox="0 0 24 24"><path d="M4 18h16c.55 0 1-.45 1-1s-.45-1-1-1H4c-.55 0-1 .45-1 1s.45 1 1 1zm0-5h16c.55 0 1-.45 1-1s-.45-1-1-1H4c-.55 0-1 .45-1 1s.45 1 1 1zM3 7c0 .55.45 1 1 1h16c.55 0 1-.45 1-1s-.45-1-1-1H4c-.55 0-1 .45-1 1z"/></svg>
                                MÔ TẢ TÓM TẮT
                            </div>
                            <div id="tab-vi" class="modal-tab-content active"></div>
                            <div id="tab-en" class="modal-tab-content"></div>
                            <div id="tab-zh" class="modal-tab-content"></div>
                            <div id="tab-jp" class="modal-tab-content"></div>
                            <div id="tab-fr" class="modal-tab-content"></div>
                            <div id="tab-ru" class="modal-tab-content"></div>
                        </div>
                    </div>
                </div>
            </div>

            <script>
                const rawPois = <?php echo json_encode($activePois); ?>;

                trackasiagl.accessToken = '3a82d12156488a8391773657171aacb765';
                
                const map = new trackasiagl.Map({
                    container: 'map',
                    style: 'https://maps.track-asia.com/styles/v2/streets.json?key=3a82d12156488a8391773657171aacb765',
                    center: [106.70236, 10.76168],
                    zoom: 15
                });

                map.addControl(new trackasiagl.NavigationControl(), 'top-left');

                function createGeoJSONCircle(center, radiusInMeters, points = 64) {
                    const coords = { latitude: center[1], longitude: center[0] };
                    const km = radiusInMeters / 1000;
                    const ret = [];
                    const distanceX = km / (111.320 * Math.cos(coords.latitude * Math.PI / 180));
                    const distanceY = km / 110.574;

                    for(let i = 0; i < points; i++) {
                        const theta = (i / points) * (2 * Math.PI);
                        const x = distanceX * Math.cos(theta);
                        const y = distanceY * Math.sin(theta);
                        ret.push([coords.longitude + x, coords.latitude + y]);
                    }
                    ret.push(ret[0]);
                    return ret;
                }

                let mapMarkers = [];
                let activePopup = null;
                let popup = new trackasiagl.Popup({
                    closeButton: false, closeOnClick: false, offset: [0, -15], maxWidth: "250px"
                });

                map.on('load', () => {
                    initLayers();
                    renderMarkers(false);

                    document.getElementById('toggleInactive').addEventListener('change', (e) => {
                        const showInactive = e.target.checked;
                        if (showInactive) {
                            map.setFilter('poi-radius-fill', null);
                            map.setFilter('poi-radius-line', null);
                        } else {
                            map.setFilter('poi-radius-fill', ['==', 'isActive', true]);
                            map.setFilter('poi-radius-line', ['==', 'isActive', true]);
                        }
                        renderMarkers(showInactive);
                    });
                });
                
                function initLayers() {
                    const features = rawPois.map((poi, index) => {
                        const r = poi.RadiusMeters ? parseFloat(poi.RadiusMeters) : 50;
                        const polygon = createGeoJSONCircle([poi.Lng, poi.Lat], r);
                        
                        return {
                            'type': 'Feature',
                            'properties': {
                                'id': index,
                                'isActive': poi.IsActive == 1 || poi.IsActive === true || poi.IsActive === "1"
                            },
                            'geometry': { 'type': 'Polygon', 'coordinates': [polygon] }
                        };
                    });

                    map.addSource('poi-radius', {
                        'type': 'geojson',
                        'data': { 'type': 'FeatureCollection', 'features': features }
                    });

                    map.addLayer({
                        'id': 'poi-radius-fill',
                        'type': 'fill',
                        'source': 'poi-radius',
                        'paint': {
                            'fill-color': ['case', ['==', ['get', 'isActive'], true], '#ff9800', '#9e9e9e'],
                            'fill-opacity': 0.15
                        },
                        'filter': ['==', 'isActive', true]
                    });

                    map.addLayer({
                        'id': 'poi-radius-line',
                        'type': 'line',
                        'source': 'poi-radius',
                        'paint': {
                            'line-color': ['case', ['==', ['get', 'isActive'], true], '#ff7043', '#9e9e9e'],
                            'line-width': 1.5
                        },
                        'filter': ['==', 'isActive', true]
                    });
                }

                function renderMarkers(showInactive) {
                    mapMarkers.forEach(m => m.remove());
                    mapMarkers = [];
                    if(activePopup) activePopup.remove();

                    rawPois.forEach((poi, index) => {
                        const isActive = poi.IsActive == 1 || poi.IsActive === true || poi.IsActive === "1";
                        if (!isActive && !showInactive) return;

                        const el = document.createElement('div');
                        el.className = 'custom-marker';
                        if (!isActive) el.classList.add('inactive');

                        const marker = new trackasiagl.Marker({ element: el })
                            .setLngLat([poi.Lng, poi.Lat])
                            .addTo(map);

                        const imgUrl = poi.ImageUrl || "https://via.placeholder.com/250x120?text=No+Image";
                        
                        const popupContent = `
                            <div style="cursor:pointer;" onclick="openModal(${index})">
                                <img src="${imgUrl}" class="poi-popup-img" alt="POI">
                                <div class="poi-popup-body">
                                    <h3 class="poi-popup-title">${poi.Name || "Unnamed POI"}</h3>
                                    <p class="poi-popup-info">
                                        <svg viewBox="0 0 24 24"><path d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5c-1.38 0-2.5-1.12-2.5-2.5s1.12-2.5 2.5-2.5 2.5 1.12 2.5 2.5-1.12 2.5-2.5 2.5z"/></svg> 
                                        Vị trí: <strong>${poi.Lat}, ${poi.Lng}</strong>
                                    </p>
                                    <p class="poi-popup-info">
                                        <svg viewBox="0 0 24 24" fill="none" stroke="#f44336" stroke-width="2"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/></svg>
                                        Bán kính: <strong>${poi.RadiusMeters || 50}m</strong>
                                    </p>
                                    <p style="color: #999; font-style: italic; margin-top: 10px; font-size: 11px; text-decoration: underline;">Click để xem chi tiết cửa sổ</p>
                                </div>
                            </div>
                        `;

                        el.addEventListener('mouseenter', () => {
                            activePopup = popup.setLngLat([poi.Lng, poi.Lat])
                                .setHTML(popupContent)
                                .addTo(map);
                        });

                        el.addEventListener('click', (e) => {
                            e.stopPropagation();
                            openModal(index);
                            if(activePopup) activePopup.remove();
                        });

                        mapMarkers.push(marker);
                    });
                }

                // Modal Logic
                function openModal(index) {
                    const poi = rawPois[index];
                    if (!poi) return;
                    
                    document.getElementById('poiModal').classList.add('active');
                    
                    const imgUrl = poi.ImageUrl || "https://via.placeholder.com/800x200?text=No+Image";
                    document.getElementById('modalBanner').src = imgUrl;
                    document.getElementById('modalAvatar').src = imgUrl;
                    
                    document.getElementById('modalTitle').textContent = poi.Name || "Tên quán";
                    document.getElementById('modalAddress').textContent = poi.Address || "Không có địa chỉ";
                    document.getElementById('modalRadius').textContent = poi.RadiusMeters || "50";
                    
                    document.getElementById('modalGoogleMaps').href = "https://www.google.com/maps/search/?api=1&query=" + poi.Lat + "," + poi.Lng;
                    
                    // Set Tab Contents
                    document.getElementById('tab-vi').textContent = poi.TextVi || poi.History || "Không có dữ liệu tiếng Việt.";
                    document.getElementById('tab-en').textContent = poi.TextEn || "No data available in English.";
                    document.getElementById('tab-zh').textContent = poi.TextZh || "无数据。";
                    document.getElementById('tab-jp').textContent = poi.TextJp || "データなし。";
                    document.getElementById('tab-fr').textContent = poi.TextFr || "Aucune donnée disponible en français.";
                    document.getElementById('tab-ru').textContent = poi.TextRu || "Нет доступных данных на русском.";

                    // Reset and select default tab "vi"
                    switchTab(document.querySelector('.modal-tab'), 'vi');
                }

                function closeModal() {
                    document.getElementById('poiModal').classList.remove('active');
                }

                function switchTab(el, lang) {
                    // Update active state in tab navigation
                    document.querySelectorAll('.modal-tab').forEach(tab => {
                        tab.classList.remove('active');
                        let span = tab.querySelector('span');
                        if(span) span.style.color = "#6b7280"; // Reset to inactive color
                    });

                    // Update active state in content areas
                    document.querySelectorAll('.modal-tab-content').forEach(content => {
                        content.classList.remove('active');
                    });
                    
                    // Set active to the clicked item
                    el.classList.add('active');
                    let span = el.querySelector('span');
                    if(span) span.style.color = "#f59e0b"; // Accent color amber
                    
                    // Show target content
                    const targetContent = document.getElementById('tab-' + lang);
                    if(targetContent) targetContent.classList.add('active');
                }

                // Close modal when clicking outside of it
                document.getElementById('poiModal').addEventListener('click', function(e) {
                    if (e.target === this) {
                        closeModal();
                    }
                });
            </script>
        </main>
    </div>
</body>
</html>
