<?php
require_once __DIR__ . '/includes/auth.php';
requireLogin();
checkTimeout();
requireRole(2);

require_once __DIR__ . '/config.php';
require_once __DIR__ . '/includes/firebase.php';

$fb = new FirebaseRTDB();
$userId = $_SESSION['user_id'];
$successMsg = '';
$errorMsg = '';

// Handle Delete (soft delete)
if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['action']) && $_POST['action'] === 'delete') {
    $idToDel = intval($_POST['id']);
    if ($idToDel) {
        $poiCheck = $fb->get('vinhkhanh/pois/' . $idToDel);
        if ($poiCheck && isset($poiCheck['OwnerId']) && $poiCheck['OwnerId'] == $userId) {
            $fb->update('vinhkhanh/pois/' . $idToDel, ['IsActive' => 0]);
            $successMsg = "Đã ẩn POI thành công!";
        } else {
            $errorMsg = "Lỗi: Không tìm thấy POI hoặc bạn không có quyền xóa.";
        }
    }
}

$fbData = $fb->get('vinhkhanh/pois');
$myPois = [];

if (is_array($fbData)) {
    foreach ($fbData as $id => $poi) {
        if ($poi && isset($poi['OwnerId']) && $poi['OwnerId'] == $userId) {
            $poi['id'] = $id;
            $myPois[] = $poi;
        }
    }
}

// Sort myPois newest first (just by ID desc)
usort($myPois, function($a, $b) {
    return ($b['Id'] ?? 0) - ($a['Id'] ?? 0);
});

?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>POI của tôi - Vĩnh Khánh Admin</title>
    <link rel="stylesheet" href="assets/style.css">
    <style>
        .actions { display: flex; gap: 0.5rem; flex-wrap: wrap; }
        .actions form { margin: 0; }
        .btn-view { border: 1px solid var(--primary-color); color: var(--primary-color); background: transparent; }
        .btn-view:hover { background: rgba(59, 130, 246, 0.1); }
        .btn-edit { border: 1px solid #f59e0b; color: #f59e0b; background: transparent; }
        .btn-edit:hover { background: rgba(245, 158, 11, 0.1); }
        .btn-delete { border: 1px solid #ef4444; color: #ef4444; background: transparent; }
        .btn-delete:hover { background: rgba(239, 68, 68, 0.1); }
        .thumb-img { width: 60px; height: 60px; object-fit: cover; border-radius: 6px; }
        .thumb-placeholder { width: 60px; height: 60px; display: flex; align-items: center; justify-content: center; background: #333; border-radius: 6px; font-size: 24px; }
    </style>
    <script>
        function confirmHide(name) {
            return confirm('Bạn có chắc muốn ẩn POI "' + name + '" không?');
        }
    </script>
</head>
<body class="dark-theme owner-theme">
    <div class="layout">
        <?php include 'includes/sidebar.php'; ?>

        <main class="main-content">
            <header class="header" style="display: flex; justify-content: space-between; align-items: center;">
                <h1 style="margin: 0;">My POI list</h1>
                <div style="display: flex; gap: 15px; align-items: center;">
                    <a href="owner_poi_form.php" class="btn btn-primary" style="padding: 0.5rem 1rem;">+ Tạo POI mới</a>
                    <div class="user-greeting" style="font-weight: bold; color: var(--text-light);">
                        Welcome, <?php echo htmlspecialchars($_SESSION['full_name'] ?? $_SESSION['username']); ?>
                        
                    </div>
                </div>
            </header>

            <?php if ($successMsg): ?>
                <div class="alert alert-success" style="background: rgba(16, 185, 129, 0.2); color: #10b981; padding: 1rem; border-radius: vả(--radius); margin-bottom: 1rem;">
                    <?php echo htmlspecialchars($successMsg); ?>
                </div>
            <?php endif; ?>

            <?php if ($errorMsg): ?>
                <div class="alert alert-danger" style="background: rgba(239, 68, 68, 0.2); color: #ef4444; padding: 1rem; border-radius: var(--radius); margin-bottom: 1rem;">
                    <?php echo htmlspecialchars($errorMsg); ?>
                </div>
            <?php endif; ?>

            <div class="card">
                <div class="table-responsive">
                    <table class="table">
                        <thead>
                            <tr>
                                <th>STT</th>
                                <th>Ảnh</th>
                                <th>Tên POI</th>
                                <th>Địa chỉ</th>
                                <th>Trạng thái</th>
                                <th>Hành động</th>
                            </tr>
                        </thead>
                        <tbody>
                            <?php if (empty($myPois)): ?>
                            <tr>
                                <td colspan="6" style="text-align: center; padding: 2rem;">Bạn chưa có POI nào.</td>
                            </tr>
                            <?php else: ?>
                                <?php $stt = 1; foreach ($myPois as $poi): ?>
                                <tr>
                                    <td><?php echo $stt++; ?></td>
                                    <td>
                                        <?php 
                                        $imgUrl = $poi['ImageUrl'] ?? (isset($poi['ImageUrls'][0]) ? $poi['ImageUrls'][0] : '');
                                        if ($imgUrl): 
                                        ?>
                                            <img src="<?php echo htmlspecialchars($imgUrl); ?>" alt="POI Image" class="thumb-img">
                                        <?php else: ?>
                                            <div class="thumb-placeholder">🍜</div>
                                        <?php endif; ?>
                                    </td>
                                    <td><strong><?php echo htmlspecialchars($poi['Name'] ?? ''); ?></strong></td>
                                    <td><?php echo htmlspecialchars($poi['Address'] ?? ''); ?></td>
                                    <td>
                                        <span class="badge <?php 
                                            if (($poi['IsActive'] ?? 0) == 1) echo 'badge-active'; 
                                            elseif (($poi['IsActive'] ?? 0) == 2) echo 'badge-pending';
                                            else echo 'badge-inactive'; 
                                        ?>" style="font-weight: normal; padding: 4px 8px; border-radius: 4px; <?php if(isset($poi['IsActive']) && $poi['IsActive'] == 2) echo 'background-color: #f59e0b; color: white;'; ?>">
                                            <?php 
                                            if (($poi['IsActive'] ?? 0) == 1) echo 'Active'; 
                                            elseif (($poi['IsActive'] ?? 0) == 2) echo 'Chờ duyệt';
                                            else echo 'Hidden'; 
                                            ?>
                                        </span>
                                    </td>
                                    <td>
                                        <div class="actions">
                                            <a href="owner_poi_detail.php?id=<?php echo $poi['id']; ?>" class="btn btn-sm btn-view">Xem</a>
                                            <a href="owner_poi_form.php?id=<?php echo $poi['id']; ?>" class="btn btn-sm btn-edit">Sửa</a>
                                            
                                            <?php if (isset($poi['IsActive']) && $poi['IsActive'] == 1 || $poi['IsActive'] == 2): ?>
                                            <form method="POST" action="" onsubmit="return confirmHide('<?php echo htmlspecialchars(addslashes($poi['Name'] ?? '')); ?>');" style="display:inline;">
                                                <input type="hidden" name="action" value="delete">
                                                <input type="hidden" name="id" value="<?php echo $poi['id']; ?>">
                                                <button type="submit" class="btn btn-sm btn-delete">Xóa (Ẩn)</button>
                                            </form>
                                            <?php endif; ?>
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