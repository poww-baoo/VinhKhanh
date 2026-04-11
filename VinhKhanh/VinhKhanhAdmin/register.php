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
$success = '';

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $username = trim($_POST['username'] ?? '');
    $password = trim($_POST['password'] ?? '');
    $confirm_password = trim($_POST['confirm_password'] ?? '');
    $fullname = trim($_POST['fullname'] ?? '');
    $email = trim($_POST['email'] ?? '');
    $sdt = trim($_POST['sdt'] ?? '');

    if (empty($username) || empty($password) || empty($confirm_password) || empty($fullname)) {
        $error = "Vui lòng nhập đầy đủ các trường bắt buộc.";
    } elseif (strlen($password) < 6) {
        $error = "Mật khẩu phải từ 6 ký tự trở lên.";
    } elseif ($password !== $confirm_password) {
        $error = "Mật khẩu nhập lại không khớp.";
    } else {
        $fb = new FirebaseRTDB();
        $users = $fb->get('vinhkhanh/users');
        
        $userExists = false;
        $maxId = 0;
        
        if ($users && is_array($users)) {
            foreach ($users as $uId => $u) {
                if ($u && isset($u['Username']) && strtolower($u['Username']) === strtolower($username)) {
                    $userExists = true;
                    break;
                }
                if ($u && isset($u['Id']) && (int)$u['Id'] > $maxId) {
                    $maxId = (int)$u['Id'];
                }
            }
        }
        
        if ($userExists) {
            $error = "Tên đăng nhập đã tồn tại, vui lòng chọn tên khác.";
        } else {
            $newId = $maxId + 1;
            
            $newUser = [
                'Id' => $newId,
                'Username' => $username,
                'Password' => hash('sha256', $password),
                'FullName' => $fullname,
                'Email' => $email,
                'Sdt' => $sdt,
                'Role' => 2, // Fixed role for owner
                'IsActive' => 1,
                'CreatedAt' => gmdate('Y-m-d\TH:i:s\Z')
            ];
            
            $result = $fb->update('vinhkhanh/users/' . $newId, $newUser);
            
            if ($result) {
                $_SESSION['user_id'] = $newId;
                $_SESSION['username'] = $username;
                $_SESSION['role'] = 2;
                $_SESSION['full_name'] = $fullname;
                $_SESSION['last_activity'] = time();

                header('Location: owner_dashboard.php');
                exit;
            } else {
                $error = "Có lỗi xảy ra khi tạo tài khoản. Vui lòng thử lại.";
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
    <title>Đăng ký Owner - Vĩnh Khánh Admin</title>
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
            max-width: 500px;
        }
        .auth-card h2 { margin-bottom: 2rem; text-align: center; }
        .form-group { margin-bottom: 1.5rem; }
        .form-group label { display: block; margin-bottom: 0.5rem; }
        
        .error-msg { background: #fee2e2; color: #b91c1c; padding: 1rem; border-radius: var(--radius); margin-bottom: 1rem; }
        .success-msg { background: #dcfce7; color: #166534; padding: 1rem; border-radius: var(--radius); margin-bottom: 1rem; }
    </style>
</head>
<body class="dark-theme">
    <div class="auth-container">
        <div class="auth-card">
            <h2>Đăng ký Owner</h2>
            
            <?php if ($error): ?>
                <div class="error-msg"><?php echo htmlspecialchars($error); ?></div>
            <?php endif; ?>
            
            <form method="POST" action="">
                <div class="form-group">
                    <label for="username">Tên đăng nhập *</label>
                    <input type="text" id="username" name="username" value="<?php echo htmlspecialchars($_POST['username'] ?? ''); ?>" required autocomplete="username">
                </div>
                
                <div class="form-group">
                    <label for="password">Mật khẩu * (ít nhất 6 ký tự)</label>
                    <input type="password" id="password" name="password" required autocomplete="new-password">
                </div>
                
                <div class="form-group">
                    <label for="confirm_password">Nhập lại Mật khẩu *</label>
                    <input type="password" id="confirm_password" name="confirm_password" required autocomplete="new-password">
                </div>
                
                <div class="form-group">
                    <label for="fullname">Họ tên *</label>
                    <input type="text" id="fullname" name="fullname" value="<?php echo htmlspecialchars($_POST['fullname'] ?? ''); ?>" required>
                </div>
                
                <div class="form-group">
                    <label for="email">Email</label>
                    <input type="email" id="email" name="email" value="<?php echo htmlspecialchars($_POST['email'] ?? ''); ?>">
                </div>
                
                <div class="form-group">
                    <label for="sdt">Số điện thoại</label>
                    <input type="text" id="sdt" name="sdt" value="<?php echo htmlspecialchars($_POST['sdt'] ?? ''); ?>">
                </div>
                
                <div style="margin-top: 2rem;">
                    <button type="submit" class="btn btn-primary" style="width: 100%; justify-content: center; padding: 0.75rem;">Đăng ký</button>
                </div>
                
                <div style="margin-top: 1.5rem; text-align: center;">
                    Đã có tài khoản? <a href="login.php" style="color: var(--primary-color);">Đăng nhập</a>
                </div>
            </form>
        </div>
    </div>
</body>
</html>