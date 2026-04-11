<?php
$currentPage = basename($_SERVER['PHP_SELF']);
$role = $_SESSION['role'] ?? 1;
?>
<aside class="sidebar" style="border-right: 2px solid <?php echo $role == 1 ? '#f59e0b' : '#3b82f6'; ?>;">
    <h2 class="brand" style="color: <?php echo $role == 1 ? '#f59e0b' : '#3b82f6'; ?>;"><?php echo $role == 1 ? 'VK Admin' : 'VK Owner'; ?></h2>
    <nav class="nav" style="margin-top: 1rem;">
        <?php if ($role == 1): ?>
        <a href="dashboard.php" class="<?php echo in_array($currentPage, ['index.php', 'dashboard.php']) ? 'active' : ''; ?>">📊 Dashboard</a>
        <a href="categories.php" class="<?php echo $currentPage === 'categories.php' ? 'active' : ''; ?>">🏷️ Categories</a>
        <a href="pois.php" class="<?php echo in_array($currentPage, ['pois.php', 'poi_form.php']) ? 'active' : ''; ?>">📍 POIs</a>
        <a href="menu_items.php" class="<?php echo $currentPage === 'menu_items.php' ? 'active' : ''; ?>">🍜 Menu Items</a>
        <a href="map.php" class="<?php echo $currentPage === 'map.php' ? 'active' : ''; ?>">🗺️ Map View</a>
        <a href="logout.php" style="color: #ef4444; margin-top: auto; padding-top: 1rem; border-top: 1px solid var(--border-dark);">🚪 Log out</a>
        <?php else: ?>
        <a href="owner_dashboard.php" class="<?php echo $currentPage === 'owner_dashboard.php' ? 'active' : ''; ?>">📊 Dashboard</a>
        <a href="owner_pois.php" class="<?php echo in_array($currentPage, ['owner_pois.php', 'owner_poi_detail.php', 'owner_poi_form.php']) ? 'active' : ''; ?>">📍 My POIs</a>
        <a href="owner_menu_items.php" class="<?php echo $currentPage === 'owner_menu_items.php' ? 'active' : ''; ?>">🍜 Menu Items</a>
        <a href="logout.php" style="color: #ef4444; margin-top: auto; padding-top: 1rem; border-top: 1px solid var(--border-dark);">🚪 Log out</a>
        <?php endif; ?>
    </nav>
</aside>
<style>
    /* Theo yêu cầu: Phân biệt admin vs owner bằng accent color */
    :root {
        --primary-color: <?php echo $role == 1 ? '#f59e0b' : '#3b82f6'; ?>;
        --accent: <?php echo $role == 1 ? '#f59e0b' : '#3b82f6'; ?>;
        --accent-dim: <?php echo $role == 1 ? 'rgba(245, 158, 11, 0.12)' : 'RGBA(59, 130, 246, 0.15)'; ?>;
    }
    .btn-primary {
        background-color: var(--primary-color) !important;
    }
    .nav a.active {
        background-color: var(--accent-dim) !important;
    }
    tbody tr:hover {
  background: var(--accent-dim);
}
</style>
