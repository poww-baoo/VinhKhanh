<?php
require_once __DIR__ . '/includes/auth.php';

requireLogin();
checkTimeout();

if ($_SESSION['role'] == 1) {
    header('Location: dashboard.php');
} else {
    header('Location: owner_dashboard.php');
}
exit;
?><?php
require_once __DIR__ . '/config.php';
require_once __DIR__ . '/includes/firebase.php';

$fb = new FirebaseRTDB();

$fbData = $fb->get('vinhkhanh');

$totalCategories = isset($fbData['categories']) ? count(array_filter($fbData['categories'])) : 0;
$pois = isset($fbData['pois']) ? $fbData['pois'] : [];
$totalPois = count(array_filter($pois));
$activePois = count(array_filter($pois, function($p) { return $p && isset($p['IsActive']) && $p['IsActive'] == 1; }));
$totalMenuItems = isset($fbData['menu_items']) ? count(array_filter($fbData['menu_items'])) : 0;

$lastUpdated = isset($fbData['meta']['last_updated']) ? $fbData['meta']['last_updated'] : 'Unknown';

if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['update_meta'])) {
    $fb->update('vinhkhanh/meta', [
        'last_updated' => date('Y-m-d\TH:i:s\Z'),
        'version' => isset($fbData['meta']['version']) ? $fbData['meta']['version'] + 1 : 1
    ]);
    header('Location: ' . BASE_URL . 'index.php');
    exit;
}
?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Vĩnh Khánh Admin - Dashboard</title>
    <link rel="stylesheet" href="assets/style.css">
</head>
<body>
    <div class="layout">
        <?php include 'includes/sidebar.php'; ?>

        <main class="main-content">
            <header class="header">
                <h1>Dashboard</h1>
            </header>

            <div class="stats-grid">
                <div class="stat-card">
                    <h3>Total POIs</h3>
                    <p class="stat-value"><?php echo $totalPois; ?></p>
                </div>
                <div class="stat-card">
                    <h3>Active POIs</h3>
                    <p class="stat-value"><?php echo $activePois; ?></p>
                </div>
                <div class="stat-card">
                    <h3>Categories</h3>
                    <p class="stat-value"><?php echo $totalCategories; ?></p>
                </div>
                <div class="stat-card">
                    <h3>Menu Items</h3>
                    <p class="stat-value"><?php echo $totalMenuItems; ?></p>
                </div>
            </div>

            <div class="action-panel">
                <h2>System Status</h2>
                <p><strong>Last Updated (Firebase):</strong> <?php echo htmlentities($lastUpdated); ?></p>
                <form method="POST">
                    <!-- Nút này giúp cập nhật meta.version thủ công để app kéo dữ liệu nếu admin mới update -->
                    <button type="submit" name="update_meta" class="btn btn-primary">Bump Version & Lệnh App Sync</button>
                </form>
            </div>
        </main>
    </div>
</body>
</html>

