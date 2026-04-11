<?php
require_once __DIR__ . '/../config.php';

class FirebaseRTDB {
    private $url;
    private $secret;

    public function __construct() {
        $this->url = rtrim(FIREBASE_DB_URL, '/');
        $this->secret = FIREBASE_SECRET;
    }

    private function getHeaders() {
        return [
            'Content-Type: application/json',
        ];
    }

    private function buildUrl($path, $queryParams = []) {
        $url = $this->url . '/' . ltrim($path, '/') . '.json';
        $params = [];
        
        if (!empty($this->secret)) {
            $params['auth'] = $this->secret;
        }
        $params = array_merge($params, $queryParams);
        
        if (!empty($params)) {
            $url .= '?' . http_build_query($params);
        }
        return $url;
    }

    private function call($path, $method = 'GET', $data = null, $queryParams = []) {
        $ch = curl_init();
        $url = $this->buildUrl($path, $queryParams);
        
        $options = [
            CURLOPT_URL => $url,
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_CUSTOMREQUEST => $method,
            CURLOPT_HTTPHEADER => $this->getHeaders(),
            CURLOPT_SSL_VERIFYPEER => false,
        ];

        if ($data !== null) {
            $options[CURLOPT_POSTFIELDS] = json_encode($data);
        }

        curl_setopt_array($ch, $options);
        $response = curl_exec($ch);
        $httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        curl_close($ch);

        if ($httpCode >= 400) {
            // Error handling could be improved
            return null;
        }

        return json_decode($response, true);
    }

    // CRUD Methods
    public function get($path, $queryParams = []) {
        // Fix for encoded orderByKey etc. since http_build_query encodes strings
        // like "%24key" instead of "$key". We apply it in call if needed.
        return $this->call($path, 'GET', null, $queryParams);
    }

    public function set($path, $data) {
        return $this->call($path, 'PUT', $data);
    }

    public function push($path, $data) {
        return $this->call($path, 'POST', $data);
    }

    public function update($path, $data) {
        return $this->call($path, 'PATCH', $data);
    }

    public function delete($path) {
        return $this->call($path, 'DELETE');
    }
}
