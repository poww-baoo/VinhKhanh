<?php
require_once __DIR__ . '/config.php';
require_once __DIR__ . '/includes/auth.php';
require_once __DIR__ . '/includes/firebase.php';

if (isLoggedIn()) {
    if ($_SESSION['role'] == 1) {
        header('Location: index.php'); // Admin dashboard
    } else {
        header('Location: owner_dashboard.php'); // Owner dashboard
    }
    exit;
}

$error = '';

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $username = trim($_POST['username'] ?? '');
    $password = trim($_POST['password'] ?? '');

    if (empty($username) || empty($password)) {
        $error = "Vui lòng nhập tên đăng nhập và mật khẩu.";
    } else {
        $fb = new FirebaseRTDB();
        $users = $fb->get('vinhkhanh/users');
        
        $loggedInUser = null;
        if ($users && is_array($users)) {
            foreach ($users as $user) {
                if ($user && isset($user['Username']) && $user['Username'] === $username) {
                    $loggedInUser = $user;
                    break;
                }
            }
        }
        
        if ($loggedInUser) {
            $hashedPassword = hash('sha256', $password);
            if ($loggedInUser['Password'] === $hashedPassword) {
                if (isset($loggedInUser['IsActive']) && $loggedInUser['IsActive'] == 1) {
                    $_SESSION['user_id'] = $loggedInUser['Id'];
                    $_SESSION['username'] = $loggedInUser['Username'];
                    $_SESSION['role'] = $loggedInUser['Role'];
                    $_SESSION['full_name'] = $loggedInUser['FullName'] ?? $loggedInUser['Username'];
                    $_SESSION['last_activity'] = time();

                    if ((int)$loggedInUser['Role'] === 1) {
                        header('Location: index.php');
                    } else {
                        header('Location: owner_dashboard.php');
                    }
                    exit;
                } else {
                    $error = "Tài khoản của bạn đã bị khóa.";
                }
            } else {
                $error = "Mật khẩu không đúng.";
            }
        } else {
            $error = "Tài khoản không tồn tại.";
        }
    }
}
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Đăng nhập - Vĩnh Khánh Admin</title>
    <link rel="stylesheet" href="assets/style.css">
    <style>
        .auth-container {
            display: flex;
            align-items: center;
            justify-content: center;
            min-height: 100vh;
            background-color: var(--bg-dark);
        }
        .auth-card {
            background-color: var(--surface-dark);
            padding: 2rem;
            border-radius: var(--radius);
            box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1), 0 2px 4px -1px rgba(0,0,0,0.06);
            width: 100%;
            max-width: 400px;
        }
        .auth-card h2 { margin-bottom: 2rem; text-align: center; }
        .form-group { margin-bottom: 1.5rem; }
        .form-group label { display: block; margin-bottom: 0.5rem; }
        
        .error-msg { background: #fee2e2; color: #b91c1c; padding: 1rem; border-radius: var(--radius); margin-bottom: 1rem; }
    </style>
</head>
<body class="dark-theme">
    <div class="auth-container">
        <div class="auth-card">
            <h2>Đăng nhập</h2>
            
            <?php if ($error): ?>
                <div class="error-msg"><?php echo htmlspecialchars($error); ?></div>
            <?php endif; ?>
            
            <?php if (isset($_GET['timeout'])): ?>
                <div class="error-msg" style="background:#fef3c7; color:#b45309;">Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.</div>
            <?php endif; ?>

            <form method="POST" action="">
                <div class="form-group">
                    <label for="username">Tên đăng nhập</label>
                    <input type="text" id="username" name="username" required autocomplete="username">
                </div>
                
                <div class="form-group">
                    <label for="password">Mật khẩu</label>
                    <input type="password" id="password" name="password" required autocomplete="current-password">
                </div>
                
                <div style="margin-top: 2rem;">
                    <button type="submit" class="btn btn-primary" style="width: 100%; justify-content: center; padding: 0.75rem;">Đăng nhập</button>
                </div>
                
                <div style="margin-top: 1.5rem; text-align: center;">
                    <a href="register.php" style="color: var(--primary-color);">Đăng ký tài khoản Owner</a>
                </div>
            </form>
        </div>
    </div>
</body>
</html>