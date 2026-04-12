<?php
require_once __DIR__ . '/includes/auth.php';
requireLogin();
checkTimeout();
requireRole(1);

require_once __DIR__ . '/config.php';
require_once __DIR__ . '/includes/firebase.php';

$fb = new FirebaseRTDB();

$poisApproved = $fb->get('vinhkhanh/pois') ?: [];
$poisPending = $fb->get('vinhkhanh/poi_submissions') ?: [];
$categories = $fb->get('vinhkhanh/categories') ?: [];

$pois = [];
foreach ($poisApproved as $id => $poi) {
    if (!$poi) continue;
    $poi['IsSubmission'] = false;
    $pois[$id] = $poi;
}
foreach ($poisPending as $id => $poi) {
    if (!$poi) continue;
    $poi['IsSubmission'] = true;
    $poi['IsActive'] = 2; // Always pending
    $pois[$id] = $poi;
}

$filterCategory = isset($_GET['category']) && $_GET['category'] !== '' ? intval($_GET['category']) : '';
$filterActive = isset($_GET['active']) && $_GET['active'] !== '' ? intval($_GET['active']) : '';

// Handle Actions
if ($_SERVER['REQUEST_METHOD'] === 'POST' && isset($_POST['action'])) {
    $id = $_POST['id'];
    if (isset($pois[$id])) {
        if ($_POST['action'] === 'toggle') {
            if (!$pois[$id]['IsSubmission']) {
                $pois[$id]['IsActive'] = $pois[$id]['IsActive'] == 1 ? 0 : 1;
                $fb->update('vinhkhanh/pois/' . $id, ['IsActive' => $pois[$id]['IsActive']]);
            }
        } elseif ($_POST['action'] === 'approve') {
            if ($pois[$id]['IsSubmission']) {
                $approvedPoi = $poisPending[$id];
                $approvedPoi['IsActive'] = 1;
                unset($approvedPoi['IsSubmission']); // Ensure this key is not saved
                
                $fb->set('vinhkhanh/pois/' . $id, $approvedPoi);
                $fb->delete('vinhkhanh/poi_submissions/' . $id);
            } else {
                $pois[$id]['IsActive'] = 1;
                $fb->update('vinhkhanh/pois/' . $id, ['IsActive' => 1]);
            }
        }
    }
    header("Location: pois.php");
    exit;
}

?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>POIs - Vĩnh Khánh Admin</title>
    <link rel="stylesheet" href="assets/style.css">
</head>
<body>
    <div class="layout">
        <?php include 'includes/sidebar.php'; ?>

        <main class="main-content">
            <header class="header flex-between">
                <h1>POIs Management</h1>
                <a href="poi_form.php" class="btn btn-create">+ Create POI</a>
            </header>

            <div class="filter-section">
                <form method="GET" class="flex-row">
                    <select name="category" class="form-control">
                        <option value="">All Categories</option>
                        <?php foreach($categories as $cat):
                            if (!$cat) continue;
                        ?>
                            <option value="<?php echo $cat['Id']; ?>" <?php echo $filterCategory === $cat['Id'] ? 'selected' : ''; ?>>
                                <?php echo htmlspecialchars($cat['Name']); ?>
                            </option>
                        <?php endforeach; ?>
                    </select>
                    <select name="active" class="form-control">
                        <option value="">All Status</option>
                        <option value="1" <?php echo $filterActive === 1 ? 'selected' : ''; ?>>Active</option>
                        <option value="0" <?php echo $filterActive === 0 ? 'selected' : ''; ?>>Hidden</option>
                        <option value="2" <?php echo $filterActive === 2 ? 'selected' : ''; ?>>Pending</option>
                    </select>
                    <button type="submit" class="btn btn-filter">Filter</button>
                    <a href="pois.php" class="btn btn-clear">Clear</a>
                </form>
            </div>

            <div class="table-wrapper">
                <table class="table">
                    <thead>
                        <tr>
                            <th>ID</th>
                            <th>Name</th>
                        <th>Category</th>
                        <th>Address</th>
                        <th>Status</th>
                        <th>Actions</th>
                    </tr>
                </thead>
                <tbody>
                    <?php foreach ($pois as $poi): 
                        if (!$poi) continue;
                        if ($filterCategory !== '' && (!isset($poi['CategoryId']) || $poi['CategoryId'] != $filterCategory)) continue;
                        if ($filterActive !== '' && (!isset($poi['IsActive']) || $poi['IsActive'] != $filterActive)) continue;
                    ?>
                        <tr>
                            <td><?php echo $poi['Id']; ?></td>
                            <td><?php echo htmlspecialchars($poi['Name'] ?? ''); ?></td>
                            <td>
                                <?php 
                                $catId = $poi['CategoryId'] ?? null;
                                echo htmlspecialchars(isset($categories[$catId]) ? $categories[$catId]['Name'] : 'Unknown'); 
                                ?>
                            </td>
                            <td><?php echo htmlspecialchars($poi['Address'] ?? ''); ?></td>
                            <td>
                                <span class="badge <?php 
                                    if (($poi['IsActive'] ?? 0) == 1) echo 'badge-active'; 
                                    elseif (($poi['IsActive'] ?? 0) == 2) echo 'badge-pending';
                                    else echo 'badge-hidden'; 
                                ?>" <?php if (($poi['IsActive'] ?? 0) == 2) echo 'style="background-color: #f59e0b;"'; ?>>
                                    <?php 
                                    if (($poi['IsActive'] ?? 0) == 1) echo 'Active'; 
                                    elseif (($poi['IsActive'] ?? 0) == 2) echo 'Pending';
                                    else echo 'Hidden'; 
                                    ?>
                                </span>
                            </td>
                            <td class="actions-cell" style="display:flex; gap: 5px;">
                                <?php if (($poi['IsActive'] ?? 0) == 2): ?>
                                <form method="POST" style="margin:0;">
                                    <input type="hidden" name="action" value="approve">
                                    <input type="hidden" name="id" value="<?php echo $poi['Id']; ?>">
                                    <button type="submit" class="btn btn-ghost" style="color: #10b981; border-color: #10b981;">Approve</button>
                                </form>
                                <?php endif; ?>
                                <a href="poi_detail.php?id=<?php echo $poi['Id']; ?>" class="btn" style="background-color: #3b82f6; color: white; border: none;">View</a>
                                <a href="poi_form.php?id=<?php echo $poi['Id']; ?>" class="btn btn-edit">Edit</a>
                                <form method="POST" style="margin:0;">
                                    <input type="hidden" name="action" value="toggle">
                                    <input type="hidden" name="id" value="<?php echo $poi['Id']; ?>">
                                    <button type="submit" class="btn btn-ghost">Toggle</button>
                                </form>
                            </td>
                        </tr>
                    <?php endforeach; ?>
                </tbody>
            </table>
            </div>
        </main>
    </div>
</body>
</html>

