# PRD — Vĩnh Khánh Admin Web + Firebase Sync

**Version:** 1.0  
**Platform:** PHP Web Admin (InfinityFree hosting) + Firebase Realtime Database + .NET MAUI (Android/iOS/Windows)  
**Document owner:** Product / Engineering  
**Status:** Active

---

## 1. Overview

Hệ thống gồm 3 thành phần liên kết:

| Thành phần | Công nghệ | Vai trò |
|---|---|---|
| Mobile App | .NET MAUI + SQLite (`vinhkhanh.db`) | End-user đọc dữ liệu POI offline |
| Admin Web | PHP + HTML/CSS/JS (InfinityFree) | Admin CRUD dữ liệu POI |
| Cloud Sync | Firebase Realtime Database | Nguồn truth online, sync về app |

**Luồng dữ liệu:**
```
Admin Web → Firebase RTDB → MAUI App (sync khi online)
                         ↓
                   SQLite local (cache offline)
```

---

## 2. Database Schema (SQLite `vinhkhanh.db`)

### Categories
| Column | Type | Note |
|---|---|---|
| Id | INTEGER PK | |
| Name | TEXT | Tên danh mục |
| IconText | TEXT | Emoji icon |
| SortOrder | INTEGER | Thứ tự hiển thị |

### Pois
| Column | Type | Note |
|---|---|---|
| Id | INTEGER PK | |
| CategoryId | INTEGER FK | → Categories.Id |
| Name | TEXT | Tên POI |
| History | TEXT | Lịch sử (vi) |
| TextVi | TEXT | Thuyết minh tiếng Việt |
| TextEn | TEXT | Thuyết minh tiếng Anh |
| TextJp | TEXT | Thuyết minh tiếng Nhật |
| TextZh | TEXT | Thuyết minh tiếng Trung |
| TextRu | TEXT | Thuyết minh tiếng Nga |
| TextFr | TEXT | Thuyết minh tiếng Pháp |
| Lat | REAL | Vĩ độ |
| Lng | REAL | Kinh độ |
| RadiusMeters | REAL | Bán kính geofence (m) |
| Priority | INTEGER | Thứ tự hiển thị |
| YearEstablished | INTEGER | Năm thành lập |
| Rating | REAL | Điểm đánh giá (0–5) |
| ImageFileName | TEXT | Tên file ảnh |
| IsActive | INTEGER | 0/1 — ẩn/hiện |
| Address | TEXT | Địa chỉ |

### MenuItems
| Column | Type | Note |
|---|---|---|
| Id | INTEGER PK | |
| PoiId | INTEGER FK | → Pois.Id |
| Name | TEXT | Tên món |
| Description | TEXT | Mô tả |
| Price | REAL | Giá (VNĐ) |
| IsSignature | INTEGER | 0/1 — món đặc trưng |

### PlaybackLogs *(read-only từ app)*
| Column | Type | Note |
|---|---|---|
| Id | INTEGER PK | |
| PoiId | INTEGER FK | |
| Language | TEXT | vi/en/zh/ja/ru/fr |
| PlayedAt | TEXT | ISO datetime |
| DeviceId | TEXT | |

### TranslationCache *(read-only từ app)*
| Column | Type | Note |
|---|---|---|
| Id | INTEGER PK | |
| PoiId | INTEGER FK | |
| Language | TEXT | |
| TranslatedText | TEXT | |
| CachedAt | TEXT | ISO datetime |

---

## 3. Firebase Realtime Database Structure

```json
{
  "vinhkhanh": {
    "categories": {
      "1": { "Id": 1, "Name": "Quán ăn", "IconText": "🍜", "SortOrder": 1 }
    },
    "pois": {
      "1": {
        "Id": 1, "CategoryId": 1,
        "Name": "Quán Bà Năm",
        "TextVi": "...", "TextEn": "...",
        "Lat": 10.765, "Lng": 106.682,
        "IsActive": 1, "Priority": 1
      }
    },
    "menu_items": {
      "1": { "Id": 1, "PoiId": 1, "Name": "Bún bò", "Price": 45000, "IsSignature": 1 }
    },
    "meta": {
      "last_updated": "2025-01-01T00:00:00Z",
      "version": 1
    }
  }
}
```

---

## 4. Admin Web — Functional Requirements

### 4.1 Authentication
- **FR-AUTH-01:** Login bằng username + password (hardcoded hoặc PHP session).  
- **FR-AUTH-02:** Session hết hạn sau 30 phút không hoạt động.  
- **FR-AUTH-03:** Tất cả route admin redirect về login nếu chưa auth.

### 4.2 Dashboard
- **FR-DASH-01:** Hiển thị số lượng: POI tổng / đang active / categories / menu items.
- **FR-DASH-02:** Hiển thị `last_updated` từ Firebase.
- **FR-DASH-03:** Nút "Sync to Firebase" — đẩy toàn bộ data lên RTDB.

### 4.3 POI Management
- **FR-POI-01:** List POI với filter theo Category + IsActive.
- **FR-POI-02:** Tạo POI mới (form đầy đủ tất cả field).
- **FR-POI-03:** Sửa POI (inline hoặc trang riêng).
- **FR-POI-04:** Xóa POI (soft delete: IsActive = 0).
- **FR-POI-05:** Multilang text editor cho TextVi/En/Jp/Zh/Ru/Fr.
- **FR-POI-06:** Preview vị trí trên mini-map (Leaflet.js).

### 4.4 Category Management
- **FR-CAT-01:** List / Tạo / Sửa / Xóa category.
- **FR-CAT-02:** Drag-drop reorder SortOrder.

### 4.5 Menu Items Management
- **FR-MENU-01:** List menu items theo POI.
- **FR-MENU-02:** Tạo / Sửa / Xóa menu item.
- **FR-MENU-03:** Toggle IsSignature.

### 4.6 Firebase Sync
- **FR-SYNC-01:** Nút sync đẩy Categories + Pois + MenuItems lên Firebase.
- **FR-SYNC-02:** Hiển thị trạng thái sync (loading → success/error).
- **FR-SYNC-03:** Ghi `meta.last_updated` = timestamp hiện tại sau sync.

---

## 5. MAUI App — Sync Requirements (bổ sung vào app đã có)

- **FR-APP-SYNC-01:** Khi có mạng, check `meta.version` trên Firebase so với version local.
- **FR-APP-SYNC-02:** Nếu Firebase version > local → pull data → ghi vào SQLite.
- **FR-APP-SYNC-03:** Khi offline → đọc SQLite như bình thường (không báo lỗi).
- **FR-APP-SYNC-04:** Sync chạy background khi app khởi động, không block UI.

---

## 6. UI/UX Spec — Admin Web

**Tone:** Minimal, mobile-first, utilitarian dark theme  
**Framework:** Plain HTML + CSS + Vanilla JS (không dùng React/Vue — tương thích PHP hosting)  
**Font:** System font stack (không cần Google Fonts — tránh latency)  
**Colors:**
```css
--bg: #0f0f0f;
--surface: #1a1a1a;
--border: #2a2a2a;
--accent: #f59e0b;   /* amber */
--text: #e5e5e5;
--muted: #6b7280;
```

**Layout:** Single sidebar nav + main content area  
**Mobile:** Sidebar collapse thành bottom nav trên mobile  
**Breakpoint:** 768px

---

## 7. Tech Stack

| Layer | Technology | Lý do chọn |
|---|---|---|
| Hosting | InfinityFree (free PHP hosting) | Free, supports PHP 8.x, MySQL |
| Backend | PHP 8.x (pure, không framework) | Đơn giản, deploy dễ |
| Frontend | HTML/CSS/Vanilla JS | Không cần build step |
| Map preview | Leaflet.js (CDN) | Free, lightweight |
| Firebase SDK | Firebase JS SDK v9 (CDN) | Giao tiếp RTDB từ browser |
| MAUI Sync | Firebase .NET SDK hoặc REST API | Pull data khi online |

---

## 8. File Structure — Admin Web

```
/admin/
├── index.php           ← redirect → login hoặc dashboard
├── login.php           ← form login
├── logout.php          ← destroy session
├── dashboard.php       ← stats overview
├── pois.php            ← list POI
├── poi_form.php        ← create/edit POI
├── categories.php      ← list + manage categories
├── menu_items.php      ← list + manage menu items per POI
├── sync.php            ← endpoint: push data → Firebase (AJAX)
├── config.php          ← Firebase config, admin credentials
├── includes/
│   ├── auth.php        ← session check helper
│   ├── db.php          ← SQLite PDO connection
│   └── firebase.php    ← Firebase REST helper (PHP → RTDB)
└── assets/
    ├── style.css
    └── app.js
```

---

## 9. Out-of-scope (v1.0)

- User management nhiều admin.
- Image upload lên server.
- Real-time preview thay đổi trên app.
- Export/import CSV.
- PlaybackLogs analytics dashboard.

---

## 10. Acceptance Criteria

| ID | Scenario | Pass |
|---|---|---|
| AC-ADM-01 | Admin login sai → không vào được dashboard | ✓ |
| AC-ADM-02 | Tạo POI mới → xuất hiện trong list | ✓ |
| AC-ADM-03 | Sửa TextEn POI → save → Firebase có value mới | ✓ |
| AC-ADM-04 | Bấm Sync → Firebase RTDB cập nhật trong 5s | ✓ |
| AC-ADM-05 | MAUI app online → tự pull data mới từ Firebase | ✓ |
| AC-ADM-06 | MAUI app offline → vẫn đọc SQLite bình thường | ✓ |
| AC-ADM-07 | Xóa POI (soft) → IsActive=0 → không hiện trên app | ✓ |
