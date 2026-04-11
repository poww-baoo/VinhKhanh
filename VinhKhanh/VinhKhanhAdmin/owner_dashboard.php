<?php
require_once __DIR__ . '/includes/auth.php';
requireLogin();
checkTimeout();
requireRole(2);

require_once __DIR__ . '/config.php';
require_once __DIR__ . '/includes/firebase.php';

$fb = new FirebaseRTDB();
$fbData = $fb->get('vinhkhanh/pois');

$myPois = [];
$totalPois = 0;
$activePois = 0;
$pendingPois = 0;

$userId = $_SESSION['user_id'];

if (is_array($fbData)) {
    foreach ($fbData as $id => $poi) {
        if ($poi && isset($poi['OwnerId']) && $poi['OwnerId'] == $userId) {
            $poi['id'] = $id;
            $myPois[] = $poi;
            $totalPois++;
            if (isset($poi['IsActive']) && $poi['IsActive'] == 1) {
                $activePois++;
            } elseif (isset($poi['IsActive']) && $poi['IsActive'] == 2) {
                $pendingPois++;
            }
        }
    }
}
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Dashboard Owner - Vĩnh Khánh Admin</title>
    <link rel="stylesheet" href="assets/style.css">
    <style>
        .actions { display: flex; gap: 0.5rem; }
    </style>
</head>
<body class="dark-theme">
    <div class="layout">
        <?php include 'includes/sidebar.php'; ?>

        <main class="main-content">
            <header class="header" style="display: flex; justify-content: space-between; align-items: center;">
                <h1 style="margin: 0;">Owner Dashboard</h1>
                <div class="user-greeting" style="font-weight: bold; color: var(--text-light);">
                    Welcome, <?php echo htmlspecialchars($_SESSION['full_name'] ?? $_SESSION['username']); ?>
                    
                </div>
            </header>

            <div class="stats-grid" style="margin-bottom: 2rem;">
                <div class="stat-card" style="border-top: 4px solid var(--primary-color);">
                    <h3>Tổng POI của bạn</h3>
                    <p class="stat-value"><?php echo $totalPois; ?></p>
                </div>
                <div class="stat-card" style="border-top: 4px solid #10b981;">
                    <h3>POI Đang hoạt động</h3>
                    <p class="stat-value"><?php echo $activePois; ?></p>
                </div>
                <div class="stat-card" style="border-top: 4px solid #f59e0b;">
                    <h3>POI Chờ duyệt</h3>
                    <p class="stat-value"><?php echo $pendingPois; ?></p>
                </div>
                <div class="stat-card" style="border-top: 4px solid #ef4444;">
                    <h3>POI Ngừng hoạt động</h3>
                    <p class="stat-value"><?php echo $totalPois - $activePois - $pendingPois; ?></p>
                </div>
            </div>

            <div class="card">
                <h2>Danh sách POI của tôi</h2>
                <div class="table-responsive">
                    <table class="table">
                        <thead>
                            <tr>
                                <th>ID</th>
                                <th>Image</th>
                                <th>Name</th>
                                <th>Address</th>
                                <th>Status</th>
                                <th>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            <?php if (empty($myPois)): ?>
                            <tr>
                                <td colspan="6" style="text-align: center; padding: 2rem;">Bạn chưa có POI nào.</td>
                            </tr>
                            <?php else: ?>
                                <?php foreach ($myPois as $poi): ?>
                                <tr>
                                    <td><?php echo htmlspecialchars($poi['Id'] ?? $poi['id']); ?></td>
                                    <td>
                                        <?php 
                                        $imgUrl = $poi['ImageUrl'] ?? (isset($poi['ImageUrls'][0]) ? $poi['ImageUrls'][0] : '');
                                        if ($imgUrl): 
                                        ?>
                                            <img src="<?php echo htmlspecialchars($imgUrl); ?>" alt="POI Image" style="width: 50px; height: 50px; object-fit: cover; border-radius: 4px;">
                                        <?php else: ?>
                                            <div style="width: 50px; height: 50px; background: #333; border-radius: 4px; display: flex; align-items: center; justify-content: center;">No Img</div>
                                        <?php endif; ?>
                                    </td>
                                    <td><?php echo htmlspecialchars($poi['Name'] ?? ''); ?></td>
                                    <td><?php echo htmlspecialchars($poi['Address'] ?? ''); ?></td>
                                    <td>
                                        <span class="badge <?php echo isset($poi['IsActive']) && $poi['IsActive'] ? 'badge-active' : 'badge-inactive'; ?>">
                                            <?php echo isset($poi['IsActive']) && $poi['IsActive'] ? 'Active' : 'Inactive'; ?>
                                        </span>
                                    </td>
                                    <td>
                                        <div class="actions">
                                            <a href="owner_poi_form.php?id=<?php echo $poi['id']; ?>" class="btn btn-primary btn-sm">Chỉnh sửa</a>
                                        </div>
                                    </td>
                                </tr>
                                <?php endforeach; ?>
                            <?php endif; ?>
                        </tbody>
                    </table>
                </div>
            </div>
        </main>
    </div>
</body>
</html>