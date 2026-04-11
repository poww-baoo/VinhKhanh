# Prompt — Thêm Authentication vào PHP Admin Web

## Context

Thêm hệ thống Authentication vào PHP Admin Web hiện tại.
Toàn bộ data đọc/ghi từ Firebase RTDB. KHÔNG dùng SQLite.

**Firebase DB URL:** `https://vinhkhanh-f275f-default-rtdb.asia-southeast1.firebasedatabase.app`  
**Firebase Secret:** `[N0j84cIKwuph7caTTSzbErxaUCYF7TEIc3CJGvVn]`  
**Firebase path users:** `/vinhkhanh/users/{id}`

**Cấu trúc User trên Firebase:**
```json
{
  "Id": 1,
  "Username": "admin",
  "Password": "sha256_hash",
  "FullName": "Administrator",
  "Email": "...",
  "Sdt": "...",
  "Role": 1,
  "IsActive": 1,
  "CreatedAt": "..."
}
```

> Role: `1` = admin, `2` = owner_poi, `3` = user

---

## File Structure cần tạo/sửa

```
/admin/
├── login.php               ← MỚI
├── register.php            ← MỚI
├── logout.php              ← MỚI
├── owner_dashboard.php     ← MỚI
├── owner_poi_form.php      ← MỚI
├── includes/
│   └── auth.php            ← MỚI
├── dashboard.php           ← SỬA (thêm auth guard)
├── pois.php                ← SỬA (thêm auth guard)
├── poi_form.php            ← SỬA (thêm auth guard)
├── categories.php          ← SỬA (thêm auth guard)
├── menu_items.php          ← SỬA (thêm auth guard)
└── index.php               ← SỬA (redirect theo role)
```

---

## Yêu cầu chi tiết

### 1. `includes/auth.php` — Session helper

- `session_start()` mỗi request
- Lưu vào session: `user_id`, `username`, `role`, `last_activity`
- `requireLogin()`: nếu chưa login → redirect `login.php`
- `requireRole($role)`: nếu role không đủ → redirect 403
- `checkTimeout()`: nếu `last_activity` > 30 phút → auto logout + redirect `login.php`
- Gọi `checkTimeout()` ở đầu mọi trang protected

---

### 2. `login.php`

- Form: Username + Password
- Lấy toàn bộ `/vinhkhanh/users.json` từ Firebase
- Tìm user có Username khớp
- So sánh `hash('sha256', $inputPassword) == $user['Password']`
- Nếu đúng và `IsActive=1` → set session → redirect theo role:
  - Role `1` (admin) → `dashboard.php`
  - Role `2` (owner) → `owner_dashboard.php`
- Nếu sai → hiển thị lỗi
- Có link **"Đăng ký tài khoản Owner"** → `register.php`

---

### 3. `register.php` — Chỉ tạo được Role=2 (owner_poi)

- Form: Username, Password, ConfirmPassword, FullName, Email, Sdt
- Validate:
  - Username unique (check Firebase)
  - Password >= 6 ký tự
  - ConfirmPassword khớp Password
- Hash password: `hash('sha256', $password)`
- Tạo Id mới = `max(existing ids) + 1`
- POST lên Firebase: `/vinhkhanh/users/{newId}`
- Role cố định = `2`, IsActive = `1`
- Sau khi tạo → auto login → redirect `owner_dashboard.php`
- **KHÔNG cho tạo Role=1 (admin)**

---

### 4. `logout.php`

- Destroy session hoàn toàn
- Redirect `login.php`

---

### 5. Bảo vệ các trang hiện có

Thêm vào đầu mỗi file:
```php
require_once 'includes/auth.php';
requireLogin();
checkTimeout();
```

| File | Guard thêm |
|---|---|
| `dashboard.php` | `requireRole(1)` — chỉ admin |
| `pois.php` | `requireRole(1)` — chỉ admin |
| `poi_form.php` | `requireRole(1)` — chỉ admin |
| `categories.php` | `requireRole(1)` — chỉ admin |
| `menu_items.php` | `requireRole(1)` — chỉ admin |
| `index.php` | redirect theo role sau khi check login |

---

### 6. `owner_dashboard.php` — Trang riêng cho owner (Role=2)

- Hiển thị: tên owner, danh sách POI thuộc `OwnerId` của mình
- Lấy POIs từ Firebase: filter theo `OwnerId == session user_id`
- Hiển thị stats: số POI active / inactive của mình
- Nút **"Chỉnh sửa"** từng POI → `owner_poi_form.php?id={poiId}`

---

### 7. `owner_poi_form.php` — Owner chỉnh sửa POI của mình

- Load POI từ Firebase theo `id`
- Kiểm tra `OwnerId == session user_id` → nếu không khớp → redirect 403
- **Cho phép sửa:** Name, Address, History, TextVi/En/Jp/Zh/Ru/Fr, Rating, Image (upload Cloudinary)
- **KHÔNG cho sửa:** CategoryId, Lat, Lng, RadiusMeters, IsActive, Priority
- Save → PATCH lên Firebase `/vinhkhanh/pois/{id}`

---

### 8. UI

- `login.php` + `register.php`: centered card, dark theme, dùng `style.css` hiện có
- Header trên mọi trang protected: hiển thị **"Xin chào {FullName}"** + nút Logout
- `owner_dashboard.php`: sidebar chỉ có Dashboard + "POI của tôi"
- Phân biệt admin vs owner bằng accent color:
  - **Admin:** `#f59e0b` (amber)
  - **Owner:** `#3b82f6` (blue)

> Dùng lại `style.css` hiện có, chỉ thêm class mới nếu cần.

---

## Thứ tự tạo file

```
[1] includes/auth.php
[2] login.php
[3] register.php
[4] logout.php
[5] Sửa dashboard.php + pois.php + categories.php + menu_items.php + poi_form.php + index.php
[6] owner_dashboard.php
[7] owner_poi_form.php
```

Tạo từng file một, bắt đầu từ `includes/auth.php`.
