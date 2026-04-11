<?php
session_start();

function isLoggedIn() {
    return isset($_SESSION['user_id']) && isset($_SESSION['username']);
}

function requireLogin() {
    if (!isLoggedIn()) {
        header('Location: login.php');
        exit;
    }
}

function requireRole($role) {
    if (!isset($_SESSION['role']) || $_SESSION['role'] != $role) {
        http_response_code(403);
        die('Forbidden: You do not have permission to access this resource.');
    }
}

function checkTimeout() {
    $timeout_duration = 30 * 60; // 30 minutes

    if (isset($_SESSION['last_activity']) && (time() - $_SESSION['last_activity']) > $timeout_duration) {
        session_unset();
        session_destroy();
        header('Location: login.php?timeout=1');
        exit;
    }

    $_SESSION['last_activity'] = time();
}

function loginUser($user) {
    $_SESSION['user_id'] = $user['Id'];
    $_SESSION['username'] = $user['Username'];
    $_SESSION['role'] = $user['Role'];
    $_SESSION['full_name'] = $user['FullName'];
    $_SESSION['last_activity'] = time();
}

function logoutUser() {
    session_unset();
    session_destroy();
}
?>