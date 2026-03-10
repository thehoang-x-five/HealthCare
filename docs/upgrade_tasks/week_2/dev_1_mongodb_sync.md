# Hướng dẫn hoàn thành Tuần 2 - MongoDB (Lịch sử Sinh thái) & APIs

> **File:** `UPGRADE_IMPLEMENTATION_PLAN.md` (Mục 2.3)

## Nhiệm vụ 1: Thiết kế & Xây dựng MongoDB Repository
1. **Interface & Implementation**:
   - Tạo `IMongoHistoryRepository.cs` và `MongoHistoryRepository.cs` trong `Infrastructure/Repositories`.
   - Setup các phương thức ghi đè sự kiện y tế theo chuẩn "Một sự kiện - Một record (Flat document)".

## Nhiệm vụ 2: Tích hợp Ghi Lịch Sử Khí Hoàn Thành
1. **Khám Lâm Sàng (`ClinicalService.cs`)**:
   - Khi `TaoChanDoanCuoiAsync()` thành công, build payload có event_type: `"kham_lam_sang"`, lưu `sinh_hieu`, `chan_doan`, `loi_khuyen` rồi push vào Mongo.
   - Cập nhật hàm `TaoPhieuKhamAsync` lưu `medicalProfile` ban đầu vào Mongo, không dùng bảng SQL cũ.
2. **Cận Lâm Sàng (`ClsService.cs`)**:
   - Khi lưu kết quả CLS -> push document `event_type: "xet_nghiem"` (hoặc hình ảnh), kèm `chi_so`, `ket_luan`, `files`.
3. **Phát Thuốc & Thanh Toán**:
   - Phát thuốc (`PharmacyService.cs`): Push `event_type: "don_thuoc"`.
   - Thanh Toán (`BillingService.cs`): Push `event_type: "thanh_toan"`.
4. **API lấy Lịch sử**:
   - Viết API `GET /api/patients/{id}/medical-history` query mongodb trả về danh sách có filter theo type/time.
