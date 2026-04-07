# Hướng dẫn hoàn thành Tuần 5 — Dev 2: Chốt Frontend, UAT, Permission Matrix Test, E2E UI & Tài Liệu Demo

> **File:** `UPGRADE_IMPLEMENTATION_PLAN.md` (Mục 5.1, 5.2)
> **Bối cảnh:** Tuần 1-3 hoàn thành Sidebar filter, luồng hủy UI, AdminUsers, Timeline/Pha hệ, Analytics dashboard. Tuần 4 hoàn thành RBAC UI 7 tầng, Payment Wizard, ~~QL Nhân Viên~~, FE adapter chuẩn hóa. Tuần 5 là tuần **chốt** — không thêm feature mới, chỉ test, fix, verify, demo.

> [!IMPORTANT]
> **THAY ĐỔI QUAN TRỌNG (2026-04-05)**:
> - **Trang `/user-management`**: ❌ ĐÃ HỦY — không tạo trang riêng.
> - **Hướng thay thế**: Trang `/staff` dùng chung. Admin thấy toggle Card↔Table + auth columns + action buttons (Khóa/Mở/Reset). Non-admin chỉ thấy Cards HR read-only.
> - Mọi test liên quan `/user-management` bên dưới → thay bằng test Staff page toggle + data scoping.
> - `chucVu` vẫn dùng được (không tách bảng) → không cần grep/đổi hết.

---

## Nguyên Tắc Tuần 5

> **KHÔNG CODE FEATURE MỚI.** Tuần này chỉ:
> 1. Fix bug phát sinh từ Tuần 4 (auth store, RBAC UI, wizard)
> 2. Test phân quyền UI cho mọi vai trò
> 3. Test E2E toàn bộ luồng trên giao diện
> 4. Chuẩn bị demo + quay video
> 5. Phối hợp Dev 1 fix lỗi contract/mapping

---

## Nhiệm vụ 1: Permission Matrix UI Test

### 1.1 Mục tiêu
Đăng nhập từng vai trò, verify mọi trang hiển thị/ẩn/disabled/read-only đúng ma trận.

### 1.2 Test Script — Chạy THỦ CÔNG cho mỗi vai trò

#### Vai trò: Admin (`admin / Admin@123`)

| # | Kiểm tra | Kỳ vọng | ✅/❌ |
|---|----------|---------|------|
| 1 | Sidebar | Thấy TẤT CẢ menu kể cả "QL Nhân viên" | |
| 2 | Tổng quan | Dashboard global (không có badge scope) | |
| 3 | Lịch hẹn | Thấy bảng lịch hẹn, KHÔNG có nút Tạo/Sửa/Hủy | |
| 4 | Bệnh nhân | Thấy + Tạo + Sửa + Xóa đầy đủ | |
| 5 | Khám bệnh | Thấy hàng đợi toàn bộ (global), có thể xem | |
| 6 | Khoa phòng | Thấy + Tạo + Sửa + Xóa khoa/phòng | |
| 7 | Nhân sự | Thấy danh sách + có action buttons | |
| 8 | QL Nhân viên | Thấy bảng user accounts, CRUD, lock/unlock, reset pw | |
| 9 | Đơn thuốc | Thấy + quyền quản lý | |
| 10 | Lịch sử | Thấy global (không scope) | |
| 11 | Báo cáo | Thấy TẤT CẢ loại báo cáo (doanh thu, lượt khám, kho, nhân viên) | |
| 12 | Thông báo | Thấy + quản lý | |

#### Vai trò: Y tá HC (`yta_hc_01 / YTa@123`)

| # | Kiểm tra | Kỳ vọng | ✅/❌ |
|---|----------|---------|------|
| 1 | Sidebar | Thấy tất cả TRỪ "QL Nhân viên" | |
| 2 | Tổng quan | Dashboard global | |
| 3 | Lịch hẹn | Full CRUD: Tạo, Sửa, Hủy, Xác nhận, Check-in | |
| 4 | Bệnh nhân | Full CRUD | |
| 5 | Khám bệnh | Full: gọi BN, hủy lượt | |
| 6 | Khoa phòng | Xem only, button sửa/xóa DISABLED | |
| 7 | Nhân sự | Xem danh sách, KHÔNG có action | |
| 8 | QL Nhân viên | KHÔNG thấy trong Sidebar | |
| 9 | URL `/user-management` | Redirect về `/` | |
| 10 | Đơn thuốc | Phát thuốc (không kê) | |
| 11 | Báo cáo | Chỉ thấy: Doanh thu + Kho thuốc | |

#### Vai trò: Bác sĩ (`bs_noi_01 / BacSi@123`)

| # | Kiểm tra | Kỳ vọng | ✅/❌ |
|---|----------|---------|------|
| 1 | Sidebar | KHÔNG thấy: Lịch hẹn, QL Nhân viên | |
| 2 | Tổng quan | Scope badge: "Khoa Nội" | |
| 3 | Bệnh nhân | Xem only, nút Tạo/Sửa ẨN | |
| 4 | Khám bệnh | Full LS: chẩn đoán, kê đơn | |
| 5 | Khoa phòng | Xem only | |
| 6 | Đơn thuốc | Kê đơn (không phát) | |
| 7 | Báo cáo | Chỉ thấy: Lượt khám | |
| 8 | Lịch sử | Scope theo khoa mình | |

#### Vai trò: Y tá LS (`yta_ls_01 / YTa@123`)

| # | Kiểm tra | Kỳ vọng | ✅/❌ |
|---|----------|---------|------|
| 1 | Sidebar | KHÔNG thấy: Lịch hẹn, QL Nhân viên | |
| 2 | Khám bệnh | Chỉ thấy tab LS, KHÔNG tab CLS | |
| 3 | Bệnh nhân | Xem only | |
| 4 | Đơn thuốc | Xem only | |

#### Vai trò: KTV (`ktv_xn_01 / KTV@123`)

| # | Kiểm tra | Kỳ vọng | ✅/❌ |
|---|----------|---------|------|
| 1 | Sidebar | KHÔNG thấy: Lịch hẹn, QL Nhân viên | |
| 2 | Khám bệnh | Chỉ thấy tab CLS, nhập kết quả | |
| 3 | Bệnh nhân | Xem scope CLS only | |
| 4 | Đơn thuốc | Xem only | |
| 5 | Báo cáo | Giới hạn | |

### 1.3 Route Guard Test

| # | URL trực tiếp | Vai trò test | Kỳ vọng | ✅/❌ |
|---|---------------|-------------|---------|------|
| 1 | `/user-management` | Bác sĩ | Redirect `/` | |
| 2 | `/user-management` | Y tá HC | Redirect `/` | |
| 3 | `/appointments` | KTV | Redirect `/` | |
| 4 | `/appointments` | Admin | Hiện (xem only) | |
| 5 | Refresh trang `/examination` | Bác sĩ (đã login) | Giữ nguyên trang | |
| 6 | Refresh trang bất kỳ | Chưa login | Redirect `/login` | |

---

## Nhiệm vụ 2: E2E UI Flow Test

### 2.1 Luồng khám ngoại trú (Happy Path) — Ghi lại video

Chạy trên browser, quay màn hình:

```
Bước 1: Login yta_hc_01
Bước 2: Mở Lịch hẹn → Tạo lịch hẹn cho BN001 → ✅/❌
Bước 3: Xác nhận lịch hẹn → ✅/❌
Bước 4: Check-in BN001 → BN vào hàng đợi → ✅/❌
Bước 5: Mở Khám bệnh → Thấy BN001 trong hàng đợi → ✅/❌

Bước 6: Logout → Login bs_noi_01
Bước 7: Mở Khám bệnh → Gọi BN001 → ✅/❌
Bước 8: Tạo phiếu khám → Wizard thanh toán hiện → ✅/❌
Bước 9: Chọn "Tiền mặt" → Xác nhận thu → Hoàn tất wizard → ✅/❌
Bước 10: Khám → Triệu chứng, Chẩn đoán sơ bộ → ✅/❌
Bước 11: Chỉ định CLS (xét nghiệm máu) → Wizard thanh toán CLS → Thu tiền → ✅/❌

Bước 12: Logout → Login ktv_xn_01
Bước 13: Mở Khám bệnh (tab CLS) → Thấy phiếu CLS → ✅/❌
Bước 14: Nhập kết quả XN → Lưu → ✅/❌

Bước 15: Logout → Login bs_noi_01
Bước 16: Xem kết quả CLS → Lập chẩn đoán cuối → ✅/❌
Bước 17: Kê đơn thuốc → Wizard thanh toán thuốc → Thu tiền → ✅/❌

Bước 18: Logout → Login yta_hc_01
Bước 19: Mở Đơn thuốc → Phát thuốc cho BN001 → ✅/❌
Bước 20: Hoàn tất lượt khám → ✅/❌

Bước 21: Kiểm tra Dashboard → KPI cập nhật → ✅/❌
Bước 22: Kiểm tra Lịch sử → BN001 có bản ghi → ✅/❌
```

### 2.2 Luồng hủy

```
Bước H1: YTa HC tạo lịch hẹn → Hủy → UI cập nhật badge "Đã hủy" → ✅/❌
Bước H2: Hover dòng hàng đợi → Hiện ✕ → Click → Confirm → Hủy lượt khám → ✅/❌
Bước H3: BS tạo phiếu CLS → Hover ✕ → Hủy phiếu CLS → ✅/❌
Bước H4: BS kê đơn → Hủy đơn → Confirm "Hoàn thuốc về kho" → ✅/❌
Bước H5: Hóa đơn chưa thu → Hủy hóa đơn → ✅/❌
```

### 2.3 Admin flow

```
Bước A1: Admin → QL Nhân viên → Tạo user mới (username: test_user) → ✅/❌
Bước A2: Logout → Login test_user → Thành công → ✅/❌
Bước A3: Login admin → Khóa test_user → ✅/❌
Bước A4: Logout → Login test_user → Fail 403 / thông báo "Tài khoản bị khóa" → ✅/❌
Bước A5: Login admin → Mở khóa test_user → Login test_user → OK → ✅/❌
Bước A6: Admin → Reset password test_user → Login pw cũ fail → Login pw mới OK → ✅/❌
```

### 2.4 Payment Wizard UI Test

```
Bước P1: Tạo phiếu → Wizard stepper hiện 3 bước → Bước 1 active → ✅/❌
Bước P2: Tạo xong → Stepper chuyển bước 2 (Thanh toán) → Số tiền hiện đúng → ✅/❌
Bước P3: Chọn "Tiền mặt" → Card highlight → ✅/❌
Bước P4: Chọn "VietQR" → QR code hiện → ✅/❌
Bước P5: Xác nhận thu tiền → Stepper chuyển bước 3 (Hoàn tất) → ✅/❌
Bước P6: "Thu sau" → Toast "Hóa đơn chưa thu" → Wizard đóng → ✅/❌
Bước P7: Phát thuốc khi hóa đơn chưa thu → Warning hiện → ✅/❌
```

---

## Nhiệm vụ 3: Fix Bug & Stabilization

### 3.1 Quy trình

1. Test theo Nhiệm vụ 1-2 → ghi bug vào danh sách
2. Phân loại: FE bug (tự fix) vs BE bug (báo Dev 1)
3. Fix → re-test bước liên quan
4. Repeat cho đến khi mọi checklist ✅

### 3.2 Bug thường gặp FE

| # | Triệu chứng | Nguyên nhân có thể | Fix |
|---|-------------|-------------------|-----|
| 1 | Login OK nhưng Sidebar vẫn hiện cũ | `chucVu` chưa đổi hết | `grep -r "chucVu" src/` → fix |
| 2 | Page hiện nhưng data rỗng | API trả 200 nhưng scope filter lọc hết | Hiện "Không có dữ liệu" thay vì blank |
| 3 | Wizard bước 2 crash | Response tạo phiếu không có `MaHoaDon` | Check adapter mapping |
| 4 | Nút disabled nhưng vẫn click được | CSS `disabled` nhưng thiếu `pointer-events: none` | Add CSS |
| 5 | VietQR → QR không hiện | API generate-qr trả 500 | Báo Dev 1 |
| 6 | ProtectedRoute loop redirect | Auth state check sai khi refresh | Check `useAuthStore` persist |
| 7 | Enum hiện raw (`chua_thu`) thay vì label (`Chưa thu`) | Thiếu label mapping | Thêm vào `enums.js` |
| 8 | Admin tạo user → form submit nhưng 400 | Request body thiếu field bắt buộc | Verify DTO contract |

### 3.3 Field mapping regression check

```bash
# Trong src/, kiểm tra không còn hardcoded enum
grep -rn '"da_thu"\|"chua_thu"\|"da_huy"' src/ --include="*.jsx" --include="*.js" | grep -v "constants/enums"
# Kỳ vọng: 0 kết quả (mọi chỗ dùng constants)

# Kiểm tra không còn chucVu
grep -rn "chucVu" src/ --include="*.jsx" --include="*.js"
# Kỳ vọng: 0 kết quả

# Kiểm tra permissions.js được import đúng
grep -rn "permissions" src/routes/ --include="*.jsx"
# Kỳ vọng: mọi route file import permissions
```

---

## Nhiệm vụ 4: Chuẩn Bị Demo & Tài Liệu

### 4.1 Video Demo — Quay 4 chức năng "biến số"

| Video | Nội dung | Thời lượng | Dev 2 quay |
|-------|----------|-----------|-----------|
| 1. Race Condition | 2 tab đặt lịch cùng lúc → 1 pass, 1 fail | ~2 phút | ✅ |
| 2. Schema Evolution | Thêm event `tiem_vac_xin` vào MongoDB → FE hiện đúng trong timeline | ~2 phút | ✅ |
| 3. Pha Hệ | Xem cây pha hệ BN → click tổ tiên → tiền sử bệnh gia đình | ~2 phút | ✅ |
| 4. Analytics | Dashboard admin → biểu đồ xu hướng bệnh + thuốc hay dùng | ~2 phút | ✅ |

**Bonus videos:**

| Video | Nội dung |
|-------|----------|
| 5. RBAC Demo | Login 3 vai trò khác nhau → Sidebar/page/action khác nhau |
| 6. Payment Wizard | Wizard tạo phiếu + thu tiền VietQR |
| 7. Admin Management | Tạo user, khóa, mở khóa, reset pw |

### 4.2 Kịch bản demo (nếu demo live)

```
Phần 1 (5 phút): Luồng khám ngoại trú
  → Đặt lịch → Check-in → Khám → CLS → Đơn thuốc → Thanh toán → Hoàn tất

Phần 2 (3 phút): 4 chức năng kỹ thuật
  → Race condition (2 tab)
  → MongoDB timeline + Schema Evolution
  → Pha hệ (cây gia đình)
  → Analytics dashboard

Phần 3 (2 phút): Phân quyền + Admin
  → Login 3 vai trò → thấy khác nhau
  → Admin tạo user, khóa, mở khóa
```

### 4.3 Tài liệu FE Dev 2 viết

| Phần | Nội dung |
|------|----------|
| Phân quyền FE | Ma trận Tab × Role, 7 tầng permission, code examples |
| Payment Wizard | Flow diagram, component structure, step transitions |
| QL Nhân viên | Screen requirements, User vs Staff phân biệt |
| Frontend Architecture | Component tree, state management, API adapter pattern |

### 4.4 Slide báo cáo (phần FE)

- Screenshot mỗi trang chính (Dashboard, Lịch hẹn, Khám bệnh, QL Nhân viên, Payment Wizard)
- Sơ đồ component hierarchy
- Bảng permission matrix (from Nhiệm vụ 1)

---

## Nhiệm vụ 5: Joint Integration Test với Dev 1

### 5.1 Session chung (Ngày 4-5)

- Dev 1 chạy backend, monitor console/log
- Dev 2 thao tác FE, ghi nhận lỗi
- Fix liên tục: Dev 2 báo → Dev 1 fix BE → Dev 2 verify FE

### 5.2 Checklist integration cuối cùng

| # | Kiểm tra | ✅/❌ |
|---|----------|------|
| 1 | Login 5 vai trò → tất cả OK | |
| 2 | Sidebar hiện đúng cho mỗi vai trò | |
| 3 | Dashboard data scope đúng | |
| 4 | Tạo phiếu LS → Wizard → Thu tiền → OK | |
| 5 | Tạo phiếu CLS → Wizard → VietQR → OK | |
| 6 | Kê đơn → Wizard → Thu tiền → Phát thuốc → OK | |
| 7 | Hủy lượt khám (hover ✕) → OK | |
| 8 | Hủy phiếu CLS (hover ✕) → OK | |
| 9 | Admin CRUD user → OK | |
| 10 | Admin lock/unlock → OK | |
| 11 | Reports scope đúng | |
| 12 | Lịch sử MongoDB timeline → OK | |
| 13 | Pha hệ cây gia đình → OK | |
| 14 | Analytics biểu đồ → OK | |
| 15 | Thông báo realtime → OK | |
| 16 | Race condition demo → OK | |
| 17 | Schema evolution demo → OK | |

---

## Checklist Nghiệm Thu Cuối Cùng (Dev 2)

### Phân quyền
- [ ] Ma trận permission test 5 vai trò × 12 tabs → 100% đúng
- [ ] Route guard chặn URL trực tiếp → 100% đúng
- [ ] Disabled/read-only UI đúng cho mỗi vai trò
- [ ] Scope hint hiện đúng khi non-global user

### Payment Wizard
- [ ] Wizard 3 bước hoạt động đúng
- [ ] Tiền mặt + VietQR đều OK
- [ ] "Thu sau" giữ `chua_thu`
- [ ] Phát thuốc khi `chua_thu` → warning

### QL Nhân viên
- [ ] CRUD user hoạt động
- [ ] Lock/Unlock/Reset password OK
- [ ] Phân biệt rõ trang Staff vs QL Nhân viên

### Chuẩn hóa
- [ ] Không còn `chucVu` trong FE
- [ ] Mọi enum dùng constants
- [ ] Mọi API adapter đúng contract mới

### Demo
- [ ] 4 video chức năng kỹ thuật đã quay
- [ ] Kịch bản demo live đã chuẩn bị
- [ ] Slide/tài liệu FE đã viết

---

## Rủi Ro Tuần 5

| # | Rủi ro | Phòng tránh |
|---|--------|-------------|
| 1 | Test phát hiện nhiều bug → fix không kịp | Ưu tiên: crash > logic > cosmetic. Bỏ cosmetic nếu hết thời gian |
| 2 | Demo live bị lỗi | Quay video TRƯỚC demo live làm backup |
| 3 | Backend xuống giữa lúc test | Dev 1 không push breaking change trong tuần 5 |
| 4 | QR code không scan được | Test với app ngân hàng thật TRƯỚC ngày demo |
| 5 | Video quay bị mờ/không rõ flow | Viết kịch bản chi tiết TRƯỚC khi quay, dùng zoom browser nếu chữ nhỏ |
