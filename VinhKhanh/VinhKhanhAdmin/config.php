<?php
$envPath = __DIR__ . '/env/.env';
$env = [];
if (file_exists($envPath)) {
    $env = parse_ini_file($envPath);
} else {
    die("File .env không tồn tại. Vui lòng tạo file /env/.env để cấu hình.");
}

// Firebase Configuration
define('FIREBASE_DB_URL', $env['FIREBASE_DB_URL']);
define('FIREBASE_SECRET', $env['FIREBASE_SECRET']);

define('BASE_URL', $env['BASE_URL']); // Thư mục gốc chứa codebase Admin Web
define('CLOUDINARY_CLOUD',  $env['CLOUDINARY_CLOUD']);
define('CLOUDINARY_KEY',    $env['CLOUDINARY_KEY']);
define('CLOUDINARY_SECRET', $env['CLOUDINARY_SECRET']);
define('CLOUDINARY_UPLOAD_URL', $env['CLOUDINARY_UPLOAD_URL']);