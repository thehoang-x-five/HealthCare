# Hướng dẫn hoàn thành Tuần 5 — Dev 1: Chốt Backend, Integration Test, Migration Verify, E2E Flow & Tài Liệu

> **File:** `UPGRADE_IMPLEMENTATION_PLAN.md` (Mục 5.1, 5.2)
> **Bối cảnh:** Tuần 1-3 hoàn thành MongoDB, SP, Genealogy, Analytics, Audit, luồng hủy. Tuần 4 hoàn thành tách User/Staff, RBAC backend, thanh toán inline, VietQR, DataSeed, chuẩn hóa contract. Tuần 5 là tuần **chốt** — không thêm feature mới, chỉ test, fix, verify, document.

---

## Nguyên Tắc Tuần 5

> **KHÔNG CODE FEATURE MỚI.** Tuần này chỉ:
> 1. Fix bug phát sinh từ Tuần 4 (tách User, RBAC, thanh toán)
> 2. Integration test toàn bộ stack
> 3. Verify migration & seed data
> 4. Chuẩn bị tài liệu nộp
> 5. Hỗ trợ Dev 2 fix lỗi mapping/contract

---

## Nhiệm vụ 1: Verify Migration & Data Integrity

### 1.1 Mục tiêu
Đảm bảo migration từ mô hình cũ (NhanVienYTe = User) sang mới (UserAccount + NhanVienYTe) hoàn chỉnh, không mất data.

### 1.2 Checklist kiểm tra

- [ ] **Bảng `user_accounts`**: Có đủ số bản ghi = số NhanVienYTe cũ
- [ ] **FK integrity**: Mọi `MaNhanVien` trong `user_accounts` tồn tại trong `nhan_vien_y_te`
- [ ] **RefreshToken FK**: Mọi `MaUser` trong `refresh_tokens` tồn tại trong `user_accounts`
- [ ] **Không còn cột cũ**: `nhan_vien_y_te` KHÔNG còn `TenDangNhap`, `MatKhauHash`, `VaiTro`, `LoaiYTa`, `ChucVu`
- [ ] **Unique constraint**: `TenDangNhap` UNIQUE trên `user_accounts`
- [ ] **Seed data**: Chạy `DataSeed.Initialize()` trên DB mới → không lỗi
- [ ] **Login smoke test**: Login mọi 5 vai trò seed → JWT claim đúng

### 1.3 Script verify SQL

```sql
-- Verify 1: Số lượng
SELECT COUNT(*) AS total_users FROM user_accounts;
SELECT COUNT(*) AS total_staff FROM nhan_vien_y_te;

-- Verify 2: FK integrity
SELECT ua.MaUser FROM user_accounts ua
LEFT JOIN nhan_vien_y_te nv ON ua.MaNhanVien = nv.MaNhanVien
WHERE nv.MaNhanVien IS NULL AND ua.MaNhanVien IS NOT NULL;
-- Kỳ vọng: 0 rows

-- Verify 3: RefreshToken FK
SELECT rt.Id FROM refresh_tokens rt
LEFT JOIN user_accounts ua ON rt.MaUser = ua.MaUser
WHERE ua.MaUser IS NULL;
-- Kỳ vọng: 0 rows

-- Verify 4: Cột cũ đã xóa
DESCRIBE nhan_vien_y_te;
-- Kỳ vọng: KHÔNG còn TenDangNhap, MatKhauHash, VaiTro, LoaiYTa, ChucVu
```

### 1.4 Xử lý lỗi phát sinh
- Nếu migration lỗi → rollback, fix script, chạy lại
- Nếu FK orphan → fix data thủ công + thêm constraint

---

## Nhiệm vụ 2: Test Backend Đầy Đủ

### 2.1 Permission Matrix Test

Sử dụng Postman/script test mọi tổ hợp Role × Endpoint:

| Endpoint | Admin | Y tá HC | Y tá LS | Y tá CLS | Bác sĩ | KTV | Kỳ vọng |
|----------|:-----:|:-------:|:-------:|:--------:|:------:|:---:|---------|
| `GET /api/dashboard` | ✅ global | ✅ global | ✅ scope | ✅ scope | ✅ scope | ✅ scope | Data scope đúng |
| `POST /api/master-data/departments` | ✅ | ❌ 403 | ❌ 403 | ❌ 403 | ❌ 403 | ❌ 403 | Chỉ admin CUD |
| `GET /api/patients` | ✅ all | ✅ all | ✅ scope | ✅ scope | ✅ scope | ✅ scope | Data scope |
| `POST /api/patients` | ✅ | ✅ | ❌ 403 | ❌ 403 | ❌ 403 | ❌ 403 | Chỉ y_ta_hc+admin |
| `GET /api/appointments` | ✅ view | ✅ full | ❌ 403 | ❌ 403 | ❌ 403 | ❌ 403 | Admin view only |
| `POST /api/appointments` | ❌ 403 | ✅ | ❌ 403 | ❌ 403 | ❌ 403 | ❌ 403 | Chỉ y_ta_hc |
| `GET /api/reports/revenue` | ✅ | ✅ | ❌ 403 | ❌ 403 | ❌ 403 | ❌ 403 | Admin+YTHC |
| `GET /api/reports/visits` | ✅ | ❌ 403 | ❌ 403 | ❌ 403 | ✅ | ❌ 403 | Admin+BS |
| `GET /api/admin/users` | ✅ | ❌ 403 | ❌ 403 | ❌ 403 | ❌ 403 | ❌ 403 | Admin only |
| `PUT /api/billing/{id}/confirm` | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ 403 | Billing roles |

**Cách test**: Tạo collection Postman với 6 environment (1/role), mỗi env có token riêng. Chạy Runner → export kết quả.

### 2.2 Data Scope Test

Login 2 tài khoản khác khoa → gọi cùng endpoint → verify data khác nhau:

```
1. Login bs_noi_01 (Khoa Nội) → GET /api/dashboard → đếm visitCount
2. Login ktv_xn_01 (Khoa XN) → GET /api/dashboard → đếm visitCount
3. visitCount #1 ≠ visitCount #2 (trừ khi data trùng khoa)
```

### 2.3 Payment Flow Test

| # | Kịch bản | Bước | Kỳ vọng |
|---|----------|------|---------|
| 1 | Happy path tiền mặt | Tạo phiếu → hóa đơn `chua_thu` → confirm(tien_mat) | `da_thu` |
| 2 | Happy path VietQR | Tạo phiếu → hóa đơn `chua_thu` → generate-qr → confirm(vietqr) | `da_thu` + `MaGiaoDich` |
| 3 | Thu sau | Tạo phiếu → hóa đơn `chua_thu` → không confirm | Phiếu vẫn tồn tại, hóa đơn `chua_thu` |
| 4 | Hủy hóa đơn | Tạo phiếu → hóa đơn `chua_thu` → cancel | `da_huy` |
| 5 | Double confirm | Confirm → confirm lại | 400 Bad Request (đã thu = terminal) |
| 6 | Confirm hóa đơn đã hủy | Cancel → confirm | 400 Bad Request |
| 7 | Phát thuốc khi chưa thu | Đơn thuốc + hóa đơn `chua_thu` → phát | Reject "Cần thu tiền trước" |

### 2.4 Race Condition Test (từ Tuần 1)

Chạy lại để verify SP vẫn hoạt động sau migration:
```bash
# 10 request đặt lịch cùng khung giờ → chỉ 1 thành công
for i in {1..10}; do curl -X POST /api/appointments ... & done; wait
```

### 2.5 Schema Evolution Test (từ Tuần 2)

```javascript
// Thêm event type mới vào MongoDB
db.medical_histories.insertOne({
  patient_id: "BN001", event_type: "tiem_vac_xin",
  data: { vaccine: "COVID-19 Pfizer", dose: 3 }
});
// Query lại → bản ghi cũ + mới đều trả OK
```

### 2.6 API Contract Test

Verify mọi endpoint trả đúng format:
- [ ] PascalCase field names (Swagger UI)
- [ ] Enum values snake_case
- [ ] Error response: `{ Code, Message, Details }`
- [ ] Pagination: `{ Items, TotalItems, Page, PageSize }`
- [ ] Date ISO 8601

---

## Nhiệm vụ 3: End-to-End Flow Verification

### 3.1 Luồng khám ngoại trú hoàn chỉnh

Chạy từ DataSeed → end-to-end, ghi log mỗi bước:

```
Bước 1: Login yta_hc_01 → Tạo lịch hẹn cho BN001 → ✅/❌
Bước 2: Check-in BN001 → BN vào hàng đợi → ✅/❌
Bước 3: Login bs_noi_01 → Gọi BN001 từ hàng đợi → ✅/❌
Bước 4: Tạo phiếu khám LS → Hóa đơn chua_thu → Thu tiền → ✅/❌
Bước 5: Khám → Lập chẩn đoán → ✅/❌
Bước 6: Chỉ định CLS (xét nghiệm máu) → Hóa đơn CLS chua_thu → Thu tiền → ✅/❌
Bước 7: Login ktv_xn_01 → Nhập kết quả XN → ✅/❌
Bước 8: Login bs_noi_01 → Xem kết quả → Lập chẩn đoán cuối → ✅/❌
Bước 9: Kê đơn thuốc → Hóa đơn thuốc chua_thu → ✅/❌
Bước 10: Login yta_hc_01 → Thu tiền thuốc → Phát thuốc → ✅/❌
Bước 11: Hoàn tất lượt khám → ✅/❌
Bước 12: Verify MongoDB: có document lịch sử cho BN001 → ✅/❌
Bước 13: Verify Dashboard: KPI đúng → ✅/❌
Bước 14: Verify Reports: dữ liệu đúng scope → ✅/❌
```

### 3.2 Luồng hủy

```
Bước H1: Tạo lịch hẹn → Hủy lịch hẹn → ✅/❌
Bước H2: Tạo phiếu khám → Hủy lượt khám → BN status=da_huy → ✅/❌
Bước H3: Tạo phiếu CLS → Hủy phiếu CLS → ✅/❌
Bước H4: Kê đơn → Hủy đơn → Kho hoàn lại → ✅/❌
Bước H5: Hóa đơn chua_thu → Hủy hóa đơn → ✅/❌
```

### 3.3 Admin flow

```
Bước A1: Login admin → Tạo user mới → ✅/❌
Bước A2: Login user mới → OK → ✅/❌
Bước A3: Admin khóa user → Login fail 403 → ✅/❌
Bước A4: Admin mở khóa → Login OK → ✅/❌
Bước A5: Admin reset password → Login pw cũ fail, pw mới OK → ✅/❌
Bước A6: Admin CRUD khoa/phòng/dịch vụ → ✅/❌
```

---

## Nhiệm vụ 4: Fix Bug & Stabilization

### 4.1 Quy trình
1. Dev 2 báo bug (FE gọi BE lỗi) → Dev 1 fix BE → push → Dev 2 verify
2. Ưu tiên: crash/500 > logic sai > cosmetic
3. Mọi fix phải re-test flow liên quan

### 4.2 Checklist fix thường gặp
- [ ] Endpoint trả 500 do null reference (thiếu Include navigation property)
- [ ] Data scope filter sai (thiếu join phòng → khoa)
- [ ] Payment confirm reject do trạng thái không khớp
- [ ] Seed data thiếu mối quan hệ (lịch trực thiếu phòng)
- [ ] JWT claim thiếu field → frontend crash

---

## Nhiệm vụ 5: Chuẩn Bị Tài Liệu Nộp

### 5.1 Báo cáo PDF

| Phần | Nội dung | Dev 1 viết |
|------|----------|-----------|
| Kiến trúc Polyglot | MySQL + MongoDB, CQRS, One-way sync | ✅ |
| ERD mới (unified) | ERD_Full_Unified.puml + giải thích | ✅ |
| MongoDB Schema | JSON Schema cho medical_histories + audit_logs | ✅ |
| Stored Procedure | sp_BookAppointment + giải thích SERIALIZABLE | ✅ |
| Recursive CTE | Query pha hệ + giải thích | ✅ |
| Aggregation Pipeline | 3 pipeline (abnormal, disease, drugs) | ✅ |
| Auth & RBAC | Mô hình UserAccount tách Staff + ma trận quyền | ✅ |
| Payment Flow | Luồng tạo phiếu → thanh toán → workflow kế tiếp | ✅ |

### 5.2 AI Audit Log

Ghi nhận sử dụng AI theo rubric:
- Prompt gốc
- Code AI tạo ra
- Phân tích/chỉnh sửa thủ công
- Lỗi AI đã mắc và cách fix

### 5.3 Swagger / API Documentation

- Export Swagger JSON/YAML từ `/swagger/v1/swagger.json`
- Verify mọi endpoint có description + request/response example
- Verify enum values liệt kê đúng

---

## Nhiệm vụ 6: Hỗ Trợ Dev 2 Integration

### 6.1 Standby cho Dev 2
- Giữ backend chạy ổn định (không push breaking change)
- Fix nhanh khi Dev 2 báo lỗi mapping/response
- Verify API response khi Dev 2 nghi ngờ field sai

### 6.2 Joint testing session (Ngày 4-5)
- Dev 1 + Dev 2 cùng chạy E2E flow trên cùng backend
- Dev 1 monitor log backend, Dev 2 thao tác FE
- Ghi nhận mọi lỗi → fix → re-test

---

## Checklist Nghiệm Thu Cuối Cùng (Dev 1)

### Database & Migration
- [ ] Bảng `user_accounts` tồn tại, có FK 1:1
- [ ] Migration script không drop data
- [ ] DataSeed chạy clean trên DB mới

### Auth & RBAC
- [ ] Login 5 vai trò → JWT đủ claims
- [ ] Permission matrix test: 100% endpoint × role đúng
- [ ] Data scope: 2 user khác khoa → data khác nhau

### Payment
- [ ] Default hóa đơn = `chua_thu`
- [ ] Confirm → `da_thu` (terminal)
- [ ] Phát thuốc chỉ khi `da_thu`
- [ ] VietQR generate → QR hợp lệ

### The Big 4
- [ ] Race condition: 10 requests → chỉ 1 pass
- [ ] Schema evolution: event mới không phá cũ
- [ ] Recursive CTE: pha hệ 3 đời
- [ ] Aggregation: top 5 stats đúng

### E2E Flow
- [ ] Luồng khám ngoại trú 14 bước → OK
- [ ] Luồng hủy 5 kịch bản → OK
- [ ] Admin flow 6 bước → OK

### Tài liệu
- [ ] Báo cáo PDF sections Dev 1 → done
- [ ] AI Audit Log → done
- [ ] Swagger export → done

---

## Rủi Ro Tuần 5

| # | Rủi ro | Phòng tránh |
|---|--------|-------------|
| 1 | Migration lỗi trên DB production/demo | Test trên DB clone TRƯỚC |
| 2 | Bug fix gây regression | Re-test flow liên quan sau mỗi fix |
| 3 | Dev 2 chờ Dev 1 fix → block | Fix nhanh < 30 phút, nếu phức tạp → workaround tạm |
| 4 | Tài liệu chưa đủ khi demo | Viết song song với test, không đợi cuối |
| 5 | E2E fail ở bước giữa → phải fix + re-test từ đầu | Mỗi bước log kết quả, fix bước lỗi rồi chạy lại từ bước đó |
