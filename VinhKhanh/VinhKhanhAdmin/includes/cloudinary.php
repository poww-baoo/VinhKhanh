<?php
require_once __DIR__ . '/../config.php';

function uploadToCloudinary(string $filePath, string $publicId): ?array {
    $timestamp = time();
    
    // Tạo signature đúng format Cloudinary
    $paramsToSign = [
        'public_id' => $publicId,
        'timestamp'  => $timestamp,
    ];
    ksort($paramsToSign);
    $signString = "public_id={$publicId}&timestamp={$timestamp}" . CLOUDINARY_SECRET;
    $signature  = sha1($signString);

    $ch = curl_init(CLOUDINARY_UPLOAD_URL);
    curl_setopt_array($ch, [
        CURLOPT_RETURNTRANSFER => true,
        CURLOPT_POST           => true,
        CURLOPT_POSTFIELDS     => [
            'file'       => new CURLFile($filePath),
            'public_id'  => $publicId,
            'timestamp'  => $timestamp,
            'api_key'    => CLOUDINARY_KEY,
            'signature'  => $signature,
        ],
    ]);
    $res  = curl_exec($ch);
    $code = curl_getinfo($ch, CURLINFO_HTTP_CODE);
    $err  = curl_error($ch);
    curl_close($ch);

    if ($err) {
        error_log("Cloudinary curl error: " . $err);
        return null;
    }

    if ($code !== 200) {
        error_log("Cloudinary upload failed. HTTP {$code}: " . $res);
        return null;
    }

    $data = json_decode($res, true);
    return [
        'secure_url' => $data['secure_url'] ?? null,
        'public_id'  => $data['public_id']  ?? null,
    ];
}

function deleteFromCloudinary(string $publicId): bool {
    $timestamp = time();

    $paramsToSign = [
        'public_id' => $publicId,
        'timestamp'  => $timestamp,
    ];
    ksort($paramsToSign);
    $signString = http_build_query($paramsToSign) . CLOUDINARY_SECRET;
    $signature  = sha1($signString);

    $url = 'https://api.cloudinary.com/v1_1/' . CLOUDINARY_CLOUD . '/image/destroy';

    $ch = curl_init($url);
    curl_setopt_array($ch, [
        CURLOPT_RETURNTRANSFER => true,
        CURLOPT_POST           => true,
        CURLOPT_POSTFIELDS     => [
            'public_id' => $publicId,
            'timestamp' => $timestamp,
            'api_key'   => CLOUDINARY_KEY,
            'signature' => $signature,
        ],
    ]);
    $res  = curl_exec($ch);
    $code = curl_getinfo($ch, CURLINFO_HTTP_CODE);
    curl_close($ch);

    $data = json_decode($res, true);
    return isset($data['result']) && $data['result'] === 'ok';
}