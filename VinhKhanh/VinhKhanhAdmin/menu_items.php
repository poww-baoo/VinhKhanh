<?php
require_once __DIR__ . '/includes/auth.php';
requireLogin();
checkTimeout();
requireRole(1);

require_once __DIR__ . '/config.php';
require_once __DIR__ . '/includes/firebase.php';

$fb = new FirebaseRTDB();
$path = 'vinhkhanh/menu_items';

$menuItems = $fb->get($path) ?: [];
$pois = $fb->get('vinhkhanh/pois') ?: [];

$itemId = isset($_GET['edit']) ? intval($_GET['edit']) : null;
$editItem = $itemId && isset($menuItems[$itemId]) ? $menuItems[$itemId] : null;

// POST Handlers
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    if (isset($_POST['action'])) {
        $action = $_POST['action'];

        if ($action === 'create' || $action === 'edit') {
            $id = isset($_POST['id']) && !empty($_POST['id']) ? intval($_POST['id']) : (empty($menuItems) ? 1 : max(array_keys($menuItems)) + 1);
            $newItem = [
                'Id' => $id,
                'PoiId' => intval($_POST['PoiId']),
                'Name' => $_POST['Name'],
                'Description' => $_POST['Description'],
                'Price' => floatval($_POST['Price']),
                'IsSignature' => isset($_POST['IsSignature']) ? 1 : 0
            ];
            $fb->set($path . '/' . $id, $newItem);
            header("Location: menu_items.php");
            exit;
        }

        if ($action === 'delete') {
            $delId = intval($_POST['id']);
            $fb->delete($path . '/' . $delId);
            header("Location: menu_items.php");
            exit;
        }
    }
}
?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Menu Items - Vĩnh Khánh Admin</title>
    <link rel="stylesheet" href="assets/style.css">
</head>
<body>
    <div class="layout">
        <?php include 'includes/sidebar.php'; ?>

        <main class="main-content">
            <header class="header">
                <h1>Menu Items Management</h1>
            </header>

            <div class="split-view">
                <div class="form-container view-left">
                    <h2 class="section-heading"><?php echo $editItem ? 'Edit Item' : 'Create Item'; ?></h2>
                    <form method="POST">
                        <input type="hidden" name="action" value="<?php echo $editItem ? 'edit' : 'create'; ?>">
                        <?php if ($editItem): ?>
                            <input type="hidden" name="id" value="<?php echo $editItem['Id']; ?>">
                        <?php endif; ?>

                        <div class="form-group">
                            <label>Item Name</label>
                            <input type="text" name="Name" value="<?php echo htmlspecialchars($editItem['Name'] ?? ''); ?>" required>
                        </div>
                        
                        <div class="form-group">
                            <label>Belongs to POI</label>
                            <select name="PoiId" class="form-control" required>
                                <option value="">-- Select POI --</option>
                                <?php foreach ($pois as $poi): 
                                    if (!$poi) continue;
                                ?>
                                    <option value="<?php echo $poi['Id']; ?>" <?php echo ($editItem['PoiId'] ?? '') == $poi['Id'] ? 'selected' : ''; ?>>
                                        <?php echo htmlspecialchars($poi['Name']); ?>
                                    </option>
                                <?php endforeach; ?>
                            </select>
                        </div>

                        <div class="form-group">
                            <label>Price (VNĐ)</label>
                            <input type="number" step="1000" name="Price" value="<?php echo $editItem['Price'] ?? 0; ?>" required>
                        </div>

                        <div class="form-group">
                            <label>Description</label>
                            <textarea name="Description" rows="2"><?php echo htmlspecialchars($editItem['Description'] ?? ''); ?></textarea>
                        </div>

                        <div class="form-group checkbox-group" style="display:flex; align-items:center; gap:8px;">
                            <input type="checkbox" name="IsSignature" id="IsSignature" value="1" <?php echo ($editItem['IsSignature'] ?? 0) == 1 ? 'checked' : ''; ?>>
                            <label for="IsSignature" style="margin:0;">Signature Item (Món đặc trưng)</label>
                        </div>

                        <div class="form-actions" style="margin-top: 20px;">
                            <button type="submit" class="btn btn-primary"><?php echo $editItem ? 'Update' : 'Create'; ?></button>
                            <?php if ($editItem): ?>
                                <a href="menu_items.php" class="btn btn-secondary">Cancel</a>
                            <?php endif; ?>
                        </div>
                    </form>
                </div>

                <div class="data-container view-right">
                    <h2>Item List</h2>
                    <table class="table">
                        <thead>
                            <tr>
                                <th>Item</th>
                                <th>POI</th>
                                <th>Price</th>
                                <th>Signature</th>
                                <th>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            <?php foreach ($menuItems as $item): 
                                if (!$item) continue;
                            ?>
                                <tr>
                                    <td><?php echo htmlspecialchars($item['Name'] ?? ''); ?></td>
                                    <td>
                                        <small>
                                            <?php 
                                            $pId = $item['PoiId'] ?? null;
                                            echo htmlspecialchars(isset($pois[$pId]) ? $pois[$pId]['Name'] : 'Unknown POI'); 
                                            ?>
                                        </small>
                                    </td>
                                    <td><?php echo number_format($item['Price'] ?? 0); ?>đ</td>
                                    <td>
                                        <?php if (($item['IsSignature'] ?? 0) == 1): ?>
                                            <span class="badge badge-signature">★ Signature</span>
                                        <?php endif; ?>
                                    </td>
                                    <td class="actions-cell">
                                        <a href="?edit=<?php echo $item['Id']; ?>" class="btn btn-edit">Edit</a>
                                        <form method="POST" onsubmit="return confirm('Xóa item này?');">
                                            <input type="hidden" name="action" value="delete">
                                            <input type="hidden" name="id" value="<?php echo $item['Id']; ?>">
                                            <button type="submit" class="btn btn-delete">Delete</button>
                                        </form>
                                    </td>
                                </tr>
                            <?php endforeach; ?>
                        </tbody>
                    </table>
                </div>
            </div>
        </main>
    </div>
</body>
</html>

