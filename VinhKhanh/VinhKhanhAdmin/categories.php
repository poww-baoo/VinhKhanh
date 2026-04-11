<?php
require_once __DIR__ . '/includes/auth.php';
requireLogin();
checkTimeout();
requireRole(1);

require_once __DIR__ . '/config.php';
require_once __DIR__ . '/includes/firebase.php';

$fb = new FirebaseRTDB();
$path = 'vinhkhanh/categories';

$categories = $fb->get($path) ?: [];
$catId = isset($_GET['edit']) ? intval($_GET['edit']) : null;
$editCat = $catId ? (isset($categories[$catId]) ? $categories[$catId] : null) : null;

// POST Handlers
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    if (isset($_POST['action'])) {
        $action = $_POST['action'];

        if ($action === 'create' || $action === 'edit') {
            $id = isset($_POST['id']) && !empty($_POST['id']) ? intval($_POST['id']) : (empty($categories) ? 1 : max(array_keys($categories)) + 1);
            $newCat = [
                'Id' => $id,
                'Name' => $_POST['name'],
                'IconText' => $_POST['icon_text'],
                'SortOrder' => intval($_POST['sort_order'])
            ];
            $fb->set($path . '/' . $id, $newCat);
            header("Location: categories.php");
            exit;
        }

        if ($action === 'delete') {
            $delId = intval($_POST['id']);
            $fb->delete($path . '/' . $delId);
            header("Location: categories.php");
            exit;
        }
    }
}

// Sort by SortOrder
if (!empty($categories)) {
    uasort($categories, function($a, $b) {
        return ($a['SortOrder'] ?? 0) <=> ($b['SortOrder'] ?? 0);
    });
}
?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Categories - Vĩnh Khánh Admin</title>
    <link rel="stylesheet" href="assets/style.css">
</head>
<body>
    <div class="layout">
        <?php include 'includes/sidebar.php'; ?>

        <main class="main-content">
            <header class="header">
                <h1>Categories Management</h1>
            </header>

            <div class="split-view">
                <div class="form-container view-left">
                    <h2 class="section-heading"><?php echo $editCat ? 'Edit Category' : 'Create Category'; ?></h2>
                    <form method="POST">
                        <input type="hidden" name="action" value="<?php echo $editCat ? 'edit' : 'create'; ?>">
                        <?php if ($editCat): ?>
                            <input type="hidden" name="id" value="<?php echo $editCat['Id']; ?>">
                        <?php endif; ?>

                        <div class="form-group">
                            <label>Name</label>
                            <input type="text" name="name" value="<?php echo $editCat ? htmlspecialchars($editCat['Name']) : ''; ?>" required>
                        </div>
                        <div class="form-group">
                            <label>Icon Text (Emoji)</label>
                            <input type="text" name="icon_text" value="<?php echo $editCat ? htmlspecialchars($editCat['IconText'] ?? '') : ''; ?>">
                        </div>
                        <div class="form-group">
                            <label>Sort Order</label>
                            <input type="number" name="sort_order" value="<?php echo $editCat ? intval($editCat['SortOrder'] ?? 1) : 1; ?>" required>
                        </div>
                        <div class="form-actions">
                            <button type="submit" class="btn btn-primary"><?php echo $editCat ? 'Update' : 'Create'; ?></button>
                            <?php if ($editCat): ?>
                                <a href="categories.php" class="btn btn-secondary">Cancel</a>
                            <?php endif; ?>
                        </div>
                    </form>
                </div>

                <div class="data-container view-right">
                    <h2>Category List</h2>
                    <table class="table">
                        <thead>
                            <tr>
                                <th>ID</th>
                                <th>Icon</th>
                                <th>Name</th>
                                <th>Sort</th>
                                <th>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            <?php foreach ($categories as $cat): 
                                if (!$cat) continue;
                            ?>
                                <tr>
                                    <td><?php echo $cat['Id']; ?></td>
                                    <td><?php echo isset($cat['IconText']) ? htmlspecialchars($cat['IconText']) : ''; ?></td>
                                    <td><?php echo htmlspecialchars($cat['Name']); ?></td>
                                    <td><?php echo isset($cat['SortOrder']) ? $cat['SortOrder'] : 0; ?></td>
                                    <td class="actions-cell">
                                        <a href="?edit=<?php echo $cat['Id']; ?>" class="btn btn-edit">Edit</a>
                                        <form method="POST" onsubmit="return confirm('Xóa category này?');">
                                            <input type="hidden" name="action" value="delete">
                                            <input type="hidden" name="id" value="<?php echo $cat['Id']; ?>">
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

