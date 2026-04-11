<?php
require_once __DIR__ . '/includes/auth.php';
requireLogin();
checkTimeout();
requireRole(2); // Dành cho Owner

require_once __DIR__ . '/config.php';
require_once __DIR__ . '/includes/firebase.php';

$fb = new FirebaseRTDB();
$path = 'vinhkhanh/menu_items';

$allMenuItems = $fb->get($path) ?: [];
$allPois = $fb->get('vinhkhanh/pois') ?: [];

$userId = $_SESSION['user_id'];
$myPois = [];

// Chỉ lấy những POI thuộc về Owner này
if (is_array($allPois)) {
    foreach ($allPois as $id => $poi) {
        if ($poi && isset($poi['OwnerId']) && $poi['OwnerId'] == $userId) {
            $myPois[$id] = $poi;
        }
    }
}

// Lọc các Menu Items chỉ thuộc về các POI của Owner này
$myMenuItems = [];
if (is_array($allMenuItems)) {
    foreach ($allMenuItems as $id => $item) {
        if ($item && isset($item['PoiId']) && array_key_exists($item['PoiId'], $myPois)) {
            $myMenuItems[$id] = $item;
        }
    }
}

$itemId = isset($_GET['edit']) ? intval($_GET['edit']) : null;
$editItem = $itemId && isset($myMenuItems[$itemId]) ? $myMenuItems[$itemId] : null;

// POST Handlers
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    if (isset($_POST['action'])) {
        $action = $_POST['action'];

        if ($action === 'create' || $action === 'edit') {
            $poiId = intval($_POST['PoiId']);
            // Kiểm tra bảo mật: Không cho phép thêm/sửa món ăn vào POI của người khác
            if (!array_key_exists($poiId, $myPois)) {
                http_response_code(403);
                die('Forbidden: Bạn không có quyền thêm món cho POI này.');
            }

            $id = isset($_POST['id']) && !empty($_POST['id']) ? intval($_POST['id']) : (empty($allMenuItems) ? 1 : max(array_keys($allMenuItems)) + 1);
            
            // Kiểm tra bảo mật khi Edit: Không cho phép sửa món của người khác
            if ($action === 'edit' && (!isset($myMenuItems[$id]))) {
                http_response_code(403);
                die('Forbidden: Bạn không có quyền sửa món ăn này.');
            }

            $newItem = [
                'Id' => $id,
                'PoiId' => $poiId,
                'Name' => $_POST['Name'],
                'Description' => $_POST['Description'],
                'Price' => floatval($_POST['Price']),
                'IsSignature' => isset($_POST['IsSignature']) ? 1 : 0
            ];
            
            $fb->set($path . '/' . $id, $newItem);
            header("Location: owner_menu_items.php");
            exit;
        }

        if ($action === 'delete') {
            $delId = intval($_POST['id']);
            // Kiểm tra bảo mật khi Delete: Chỉ cho xóa món của mình
            if (isset($myMenuItems[$delId])) {
                $fb->delete($path . '/' . $delId);
            }
            header("Location: owner_menu_items.php");
            exit;
        }
    }
}
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Quản lý Menu - Owner Dashboard</title>
    <link rel="stylesheet" href="assets/style.css">
    <style>
        .form-control { width: 100%; box-sizing: border-box; }
        textarea.form-control { resize: vertical; }
    </style>
</head>
<body class="dark-theme">
    <div class="layout">
        <?php include 'includes/sidebar.php'; ?>

        <main class="main-content">
            <header class="header" style="display: flex; justify-content: space-between; align-items: center;">
                <h1 style="margin: 0;">Quản lý Menu Items</h1>
                <div class="user-greeting" style="font-weight: bold; color: var(--text-light);">
                     Welcome, <?php echo htmlspecialchars($_SESSION['full_name'] ?? $_SESSION['username']); ?>
                   
                </div>
            </header>

            <div class="split-view">
                <div class="form-container view-left">
                    <h2 class="section-heading"><?php echo $editItem ? 'Chỉnh sửa Món' : 'Thêm Món Mới'; ?></h2>
                    <form method="POST">
                        <input type="hidden" name="action" value="<?php echo $editItem ? 'edit' : 'create'; ?>">
                        <?php if ($editItem): ?>
                            <input type="hidden" name="id" value="<?php echo $editItem['Id']; ?>">
                        <?php endif; ?>

                        <div class="form-group">
                            <label>Tên món</label>
                            <input type="text" name="Name" class="form-control" value="<?php echo htmlspecialchars($editItem['Name'] ?? ''); ?>" required>
                        </div>
                        
                        <div class="form-group">
                            <label>Thuộc Quán / POI</label>
                            <select name="PoiId" class="form-control" required>
                                <option value="">-- Chọn POI của bạn --</option>
                                <?php foreach ($myPois as $poiId => $poi): 
                                    if (!$poi) continue;
                                ?>
                                    <option value="<?php echo $poiId; ?>" <?php echo ($editItem['PoiId'] ?? '') == $poiId ? 'selected' : ''; ?>>
                                        <?php echo htmlspecialchars($poi['Name']); ?>
                                    </option>
                                <?php endforeach; ?>
                            </select>
                        </div>

                        <div class="form-group">
                            <label>Giá (VNĐ)</label>
                            <input type="number" step="1000" name="Price" class="form-control" value="<?php echo $editItem['Price'] ?? 0; ?>" required>
                        </div>

                        <div class="form-group">
                            <label>Mô tả</label>
                            <textarea name="Description" class="form-control" rows="3"><?php echo htmlspecialchars($editItem['Description'] ?? ''); ?></textarea>
                        </div>

                        <div class="form-group" style="display:flex; align-items:center; gap:8px;">
                            <input type="checkbox" name="IsSignature" id="IsSignature" value="1" <?php echo ($editItem['IsSignature'] ?? 0) == 1 ? 'checked' : ''; ?>>
                            <label for="IsSignature" style="margin:0; font-weight: bold; color: #f59e0b;">★ Món đặc trưng (Signature Item)</label>
                        </div>

                        <div class="form-actions" style="margin-top: 20px;">
                            <button type="submit" class="btn btn-primary" style="background-color: var(--primary-color); border:none;"><?php echo $editItem ? 'Cập nhật' : 'Thêm mới'; ?></button>
                            <?php if ($editItem): ?>
                                <a href="owner_menu_items.php" class="btn btn-secondary">Hủy</a>
                            <?php endif; ?>
                        </div>
                    </form>
                </div>

                <div class="data-container view-right">
                    <h2>Danh sách Món Của Bạn (<?php echo count($myMenuItems); ?>)</h2>
                    <div class="table-responsive">
                        <table class="table">
                            <thead>
                                <tr>
                                    <th>Tên món</th>
                                    <th>Thuộc POI</th>
                                    <th>Giá</th>
                                    <th>Trạng thái</th>
                                    <th>Hành động</th>
                                </tr>
                            </thead>
                            <tbody>
                                <?php if (empty($myMenuItems)): ?>
                                <tr>
                                    <td colspan="5" style="text-align: center; padding: 2rem;">Chưa có món ăn nào.</td>
                                </tr>
                                <?php else: ?>
                                    <?php foreach ($myMenuItems as $item): 
                                        if (!$item) continue;
                                    ?>
                                        <tr>
                                            <td><?php echo htmlspecialchars($item['Name'] ?? ''); ?></td>
                                            <td>
                                                <small style="color: #9ca3af;">
                                                    <?php 
                                                    $pId = $item['PoiId'] ?? null;
                                                    echo htmlspecialchars(isset($myPois[$pId]) ? $myPois[$pId]['Name'] : 'Unknown POI'); 
                                                    ?>
                                                </small>
                                            </td>
                                            <td><?php echo number_format($item['Price'] ?? 0); ?>đ</td>
                                            <td>
                                                <?php if (($item['IsSignature'] ?? 0) == 1): ?>
                                                    <span class="badge" style="background-color: #f59e0b; color: white;">★ Signature</span>
                                                <?php else: ?>
                                                    <span style="color: var(--text-muted);">-</span>
                                                <?php endif; ?>
                                            </td>
                                            <td class="actions-cell" style="display:flex; gap: 5px;">
                                                <a href="?edit=<?php echo $item['Id']; ?>" class="btn btn-edit" style="padding: 0.3rem 0.6rem; font-size: 0.9em;">Sửa</a>
                                                <form method="POST" onsubmit="return confirm('Bạn có chắc muốn xóa món này không?');" style="margin:0;">
                                                    <input type="hidden" name="action" value="delete">
                                                    <input type="hidden" name="id" value="<?php echo $item['Id']; ?>">
                                                    <button type="submit" class="btn btn-delete" style="padding: 0.3rem 0.6rem; font-size: 0.9em; background: transparent; border: 1px solid #ef4444; color: #ef4444;">Xóa</button>
                                                </form>
                                            </td>
                                        </tr>
                                    <?php endforeach; ?>
                                <?php endif; ?>
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </main>
    </div>
</body>
</html>