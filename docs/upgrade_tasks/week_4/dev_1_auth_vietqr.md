# Hướng dẫn hoàn thành Tuần 4 - Phân Quyền Backend & API Thanh Toán

> **File:** `UPGRADE_IMPLEMENTATION_PLAN.md` (Mục 3.3, 3.4)

## Nhiệm vụ 1: Phân Quyền Middleware (`[RequireRole]`)
1. **Rà soát Controller**:
   - Phân tích 5 Controller: `MasterDataController`, `PatientsController`, `HistoryController`, `ReportsController`, `AdminController(Mới)`.
   - Thêm Attribute `[RequireRole("admin", ...)]` (hoặc Policy authorize của ASP.NET Core) thay thế cho Authorization thuần túy ở các API update nhạy cảm.
   - Chặn Admin ở những API khám/tiếp nhận (để tách bạch y tế và quản trị).
   - Đảm bảo KTV không load được Lịch hẹn hoặc Kho. Test các call trái phép qua Postman (chắc chắn trả 403 Forbidden).

## Nhiệm vụ 2: VietQR API Generation
1. **Thêm phương thức VietQR (Enum Cập Nhật)**:
   - Sửa Enum (nếu có enum ở BE) Phương thức thanh toán trên `HoaDonThanhToan`.
2. **Tích hợp tạo QR Động Bank**:
   - Viết API `/api/billing/{id}/generate-qr`.
   - Logic: Đọc Hóa đơn `id`, lấy `SoTienPhaiTra`, gọi thư viện hoặc String Formatter của VietQR/Napas (BankID, AccountNo, Amount, Memo). Generate ra QRCode Base64 trả về cho Frontend hiển thị.
