# HealthCare+ Polyglot Persistence - Phân Tích Toàn Diện
## (Dựa trên tài liệu Topic 9 đã đọc)

---

## I. TỔNG QUAN DỰ ÁN

| Thông Tin | Chi Tiết |
|-----------|----------|
| **Đề Tài** | Topic 9 - Polyglot Persistence (SQL + NoSQL) |
| **Môn Học** | CS-402: Advanced Database Systems |
| **Dự Án Gốc** | HealthCare (BE: ASP.NET Core) + my-patients (FE: React/Vite) |
| **Dự Án Mới** | HealthCare+ - Hệ thống Quản lý Bệnh nhân Hợp nhất |

### Cấu trúc Điểm theo Rubric
| Hạng Mục | Trọng Số | Tiêu Chí Xuất Sắc (9-10) |
|----------|----------|---------------------------|
| Thiết kế & Kiến trúc CSDL | **35%** | Biện luận sắc bén về trade-offs (ACID vs BASE), ERD chuẩn hóa, thiết kế MongoDB tối ưu |
| Triển khai Kỹ thuật | **35%** | PL/SQL, Recursive CTE, Aggregation Pipeline hoạt động chính xác |
| Báo cáo & Tài liệu | **15%** | Trình bày chuyên nghiệp, đầy đủ sơ đồ |
| Trình bày & Vấn đáp | **15%** | Demo mượt mà, trả lời phản biện thuyết phục |

---

## 🔗 KẾT NỐI DATABASE

### MySQL (Hiện Tại - Giữ nguyên)
```
Server=127.0.0.1;Port=3306;Database=HealthCareDB;User=root;Password=Abc@1234
```

### MongoDB Atlas (Mới - Thêm vào)
```
mongodb+srv://thehoangacc:TThedeptrai1@@cluster0.tuuj92z.mongodb.net/?appName=Cluster0
```
> **Database Name**: `HealthCarePlusDB`  
> **Collection**: `MedicalHistories`

---

## II. TỔNG QUAN CHỨC NĂNG MỚI (HEALTHCARE+)

Dựa trên tài liệu đặc tả tích hợp, hệ thống HealthCare+ sẽ bao gồm **12 nhóm chức năng cốt lõi** sau khi nâng cấp:

| Nhóm Chức Năng | Chi Tiết Nghiệp Vụ | CSDL Phụ Trách |
|----------------|--------------------|----------------|
| **1. Quản lý Xác thực & Phân quyền** | Đăng nhập/xuất, đổi mật khẩu. Phân quyền theo vai trò (Bác sĩ, Y tá, Kỹ thuật viên, Admin). | MySQL |
| **2. Dashboard Tổng quan** | Hiển thị KPI thời gian thực: số lượt khám, doanh thu, lịch hẹn sắp tới. | MySQL |
| **3. Quản lý Lịch hẹn** | Tạo, sửa, hủy lịch hẹn. Theo dõi trạng thái (Đang chờ, Đã xác nhận, Đã check-in). **[New]** Ngăn chặn trùng lịch tuyệt đối. | MySQL |
| **4. Quản lý Bệnh nhân & Tiếp nhận** | Quản lý hồ sơ hành chính. Điều phối trạng thái bệnh nhân trong ngày. | MySQL |
| **5. Quy trình Khám bệnh** | Bác sĩ truy cập lịch sử bệnh án, chẩn đoán, chỉ định CLS. **[New]** Dữ liệu bệnh án lưu trữ dạng Document linh hoạt. | MongoDB |
| **6. Quản lý Khoa & Phòng** | Theo dõi trạng thái phòng khám, sức chứa, hàng đợi bệnh nhân tại từng phòng. | MySQL |
| **7. Quản lý Nhân sự** | Quản lý hồ sơ nhân viên, lịch trực, trạng thái làm việc. | MySQL |
| **8. Quản lý Đơn thuốc & Kho** | Kê đơn điện tử (e-prescription). Quản lý tồn kho thuốc. | MongoDB (Đơn thuốc) + MySQL (Kho) |
| **9. Quản lý Viện phí & Giao dịch** | Tự động tính toán hóa đơn. **[New]** Thanh toán đa phương thức (Tiền mặt/QR Code). Quản lý lịch sử giao dịch chi tiết. | MySQL |
| **10. Hệ thống Thông báo Real-time** | Thông báo đẩy (Push Notification) khi có sự kiện: Bệnh nhân check-in, Có kết quả CLS, Đơn thuốc sẵn sàng. | SignalR |
| **11. Báo cáo & Thống kê** | Báo cáo doanh thu, hiệu suất khám. **[New]** Phân tích dữ liệu bệnh lý (Analytics) trên tập lớn. | MySQL + MongoDB |
| **12. Nghiên cứu Di truyền** | **[New]** Truy vấn và dựng cây phả hệ bệnh nhân để nghiên cứu bệnh di truyền. | MySQL (Recursive) |

---

## III. PHÂN CHIA DATABASE POLYGLOT

### MySQL (ACID - Giữ lại cho dữ liệu giao dịch)

| Entity Hiện Tại | Giữ/Sửa | Thay Đổi Cần Thiết |
|-----------------|---------|-------------------|
| `nhan_vien_y_te` | ✅ Giữ | Không đổi |
| `benh_nhan` | ⚠️ **SỬA** | **Thêm cột `MaBenhNhanCha` (ParentID)** cho truy vấn phả hệ |
| `lich_hen_kham` | ✅ Giữ | Không đổi, nhưng thêm **Stored Procedure** |
| `lich_truc` | ✅ Giữ | Không đổi |
| `hoa_don_thanh_toan` | ✅ Giữ | Không đổi |
| `phong`, `khoa_chuyen_mon` | ✅ Giữ | Không đổi |
| `kho_thuoc`, `dich_vu_y_te` | ✅ Giữ | Không đổi |

### MongoDB (BASE - Mới, cho dữ liệu bệnh án linh hoạt)

| Dữ Liệu Cần Di Chuyển | Từ Entity MySQL | Sang MongoDB |
|-----------------------|-----------------|--------------|
| Tiền sử bệnh | `benh_nhan.TieuSuBenh` | `events[type=medical_history]` |
| Dị ứng | `benh_nhan.DiUng` | `events[type=allergy]` |
| Chống chỉ định | `benh_nhan.ChongChiDinh` | `events[type=contraindication]` |
| Phiếu khám lâm sàng | `phieu_kham_lam_sang` | `events[type=PhieuKham]` |
| Phiếu khám CLS | `phieu_kham_can_lam_sang` | `events[type=PhieuKhamCls]` |
| Kết quả dịch vụ | `ket_qua_dich_vu` | Embedded trong `PhieuKhamCls.results[]` |
| Phiếu chẩn đoán cuối | `phieu_chan_doan_cuoi` | `events[type=PhieuChanDoan]` |
| Đơn thuốc (lịch sử) | `don_thuoc` | `events[type=DonThuoc]` |
| Chi tiết đơn thuốc | `chi_tiet_don_thuoc` | Embedded trong `DonThuoc.medicines[]` |

### Cấu Trúc MongoDB Document (theo tài liệu đề bài)

```json
{
  "_id": "ObjectId(...)",
  "patient_id": "BN001",
  "patient_name": "Nguyễn Văn An",
  "events": [
    {
      "event_id": "PK001",
      "event_type": "PhieuKham",
      "date": "2025-10-20T09:00:00Z",
      "doctor_id": "BS01",
      "TrieuChung": "Ho, sốt nhẹ",
      "status": "DaHoanTat"
    },
    {
      "event_id": "PKCLS001",
      "event_type": "PhieuKhamCls",
      "date": "2025-10-20T09:15:00Z",
      "status": "DaHoanTat",
      "results": [
        {
          "service_id": "XN_MAU",
          "result_content": "Bạch cầu tăng nhẹ",
          "attachment": "/files/xn_mau_bn001.pdf"
        }
      ]
    },
    {
      "event_id": "PCD001",
      "event_type": "PhieuChanDoan",
      "date": "2025-10-20T10:30:00Z",
      "ChanDoanCuoi": "Viêm họng cấp",
      "HuongXuTri": "Dùng thuốc kháng sinh, nghỉ ngơi."
    },
    {
      "event_id": "DT001",
      "event_type": "DonThuoc",
      "date": "2025-10-20T10:35:00Z",
      "status": "DaPhat",
      "medicines": [
        { "medicine_id": "TH001", "name": "Amoxicillin 500mg", "quantity": 10, "usage": "Sáng 1 viên, tối 1 viên sau ăn" }
      ]
    },
    {
      "event_id": "VAC001",
      "event_type": "vaccination",
      "date": "2024-01-15T10:00:00Z",
      "vaccine_name": "COVID-19 Pfizer",
      "dose": 2
    }
  ]
}
```

---

## IV. 4 CHỨC NĂNG KỸ THUẬT BẮT BUỘC (Theo Topic 9)

### 1️⃣ Ngăn Chặn Trùng Lịch - Stored Procedure + SERIALIZABLE

| Yêu Cầu | Chi Tiết |
|---------|----------|
| **Tên SP** | `sp_CreateAppointment` |
| **Kỹ thuật** | `SET TRANSACTION ISOLATION LEVEL SERIALIZABLE` |
| **Mục tiêu** | Ngăn chặn **100%** race condition khi 2 y tá đặt lịch cùng lúc |
| **Bài toán** | Không cho phép 2 lịch hẹn trùng khung giờ cho cùng 1 bác sĩ |

**Logic xử lý (theo tài liệu):**
1. BEGIN TRANSACTION
2. SET TRANSACTION ISOLATION LEVEL SERIALIZABLE
3. SELECT kiểm tra xung đột: `WHERE StaffID = p_StaffID AND StartTime < p_EndTime AND EndTime > p_StartTime`
4. Nếu có xung đột → ROLLBACK + trả lỗi "Lịch bị trùng"
5. Nếu không → INSERT + COMMIT

---

### 2️⃣ Truy Vấn Phả Hệ - Recursive CTE

| Yêu Cầu | Chi Tiết |
|---------|----------|
| **Cột mới** | `benh_nhan.MaBenhNhanCha` (self-referencing FK) |
| **API** | `GET /api/patients/{id}/ancestors` |
| **Kỹ thuật** | WITH RECURSIVE CTE |
| **Mục đích** | Phục vụ nghiên cứu bệnh di truyền - tìm tổ tiên bệnh nhân |

**Cấu trúc CTE:**
- **Anchor Member**: SELECT bệnh nhân gốc với PatientID
- **Recursive Member**: JOIN CTE với Patients qua ParentID
- **Điều kiện dừng**: Khi ParentID là NULL (tổ tiên xa nhất)

---

### 3️⃣ Schema Evolution - MongoDB $push

| Yêu Cầu | Chi Tiết |
|---------|----------|
| **API** | `PATCH /api/histories/{patientId}/events` |
| **Kỹ thuật** | MongoDB `updateOne` với toán tử `$push` |
| **Demo** | Thêm event `vaccination` mới vào document cũ |
| **Ưu điểm** | Không cần ALTER TABLE, không downtime |

**Lệnh MongoDB mẫu:**
```javascript
db.MedicalHistories.updateOne(
  { "patient_id": "BN001" },
  { 
    $push: { 
      "events": {
        "event_type": "vaccination",
        "vaccine_name": "COVID-19 Pfizer",
        "dose": 2,
        "date": ISODate("2024-01-15T10:00:00Z")
      }
    }
  }
)
```

---

### 4️⃣ Aggregation Pipeline - Phân Tích Dữ Liệu

| Yêu Cầu | Chi Tiết |
|---------|----------|
| **API** | `GET /api/analytics/diagnoses?disease={name}` |
| **Mục tiêu** | Tìm và đếm bệnh nhân theo chẩn đoán |
| **Operators** | `$unwind`, `$match`, `$group` |

**Pipeline mẫu:**
```javascript
[
  { $unwind: "$events" },
  { $match: { 
    "events.event_type": "PhieuChanDoan", 
    "events.ChanDoanCuoi": /Viêm họng/i 
  }},
  { $group: { 
    "_id": "$patient_id", 
    "patient_name": { "$first": "$patient_name" } 
  }}
]
```

---

## 📁 FILES CẦN THAY ĐỔI/THÊM MỚI

### Infrastructure & Config

| File | Hành Động | Mô Tả |
|------|-----------|-------|
| `HealthCare.csproj` | **MODIFY** | Thêm `<PackageReference Include="MongoDB.Driver" Version="2.25.0" />` |
| `appsettings.json` | **MODIFY** | Thêm section `"MongoDB"` với connection string |
| `Program.cs` | **MODIFY** | Đăng ký `MongoDbContext` vào DI container |
| `Settings/MongoDbSettings.cs` | **NEW** | Class config cho MongoDB |
| `Datas/MongoDbContext.cs` | **NEW** | MongoDB context service |

### Entities & Models

| File | Hành Động | Mô Tả |
|------|-----------|-------|
| `Entities/BenhNhan.cs` | **MODIFY** | Thêm `public string? MaBenhNhanCha { get; set; }` |
| `Entities/MongoDB/PatientHistory.cs` | **NEW** | MongoDB document model |
| `Entities/MongoDB/MedicalEvent.cs` | **NEW** | Base class cho events |

### Services (Logic nghiệp vụ)

| File | Hành Động | Mô Tả |
|------|-----------|-------|
| `Services/PatientManagement/GenealogyService.cs` | **NEW** | Recursive CTE cho phả hệ |
| `Services/PatientManagement/MedicalHistoryService.cs` | **NEW** | MongoDB CRUD cho histories |
| `Services/Report/AnalyticsService.cs` | **NEW** | Aggregation Pipeline analytics |
| `Services/OutpatientCare/AppointmentService.cs` | **MODIFY** | Gọi `sp_CreateAppointment` thay vì EF |

### Controllers

| File | Hành Động | Mô Tả |
|------|-----------|-------|
| `Controllers/PatientsController.cs` | **MODIFY** | Thêm `GET /api/patients/{id}/ancestors` |
| `Controllers/HistoryController.cs` | **MODIFY** | Thêm `PATCH /api/histories/{id}/events` |
| `Controllers/AnalyticsController.cs` | **NEW** | Analytics endpoints |

### Database Scripts

| File | Hành Động | Mô Tả |
|------|-----------|-------|
| `Migrations/AddParentIdToBenhNhan.cs` | **NEW** | EF Migration thêm cột ParentID |
| `Scripts/sp_CreateAppointment.sql` | **NEW** | Stored procedure với SERIALIZABLE |
| `Scripts/migrate_to_mongodb.js` | **NEW** | Script di chuyển dữ liệu |

### DTOs

| File | Hành Động | Mô Tả |
|------|-----------|-------|
| `DTOs/AncestorDto.cs` | **NEW** | Response cho phả hệ |
| `DTOs/AddEventRequest.cs` | **NEW** | Request thêm event |
| `DTOs/DiagnosisStatsDto.cs` | **NEW** | Response cho analytics |

---

## VI. KIỂM THỬ BẮT BUỘC (Theo tài liệu)

### Test 1: Race Condition cho Stored Procedure
- Viết script Python/Node.js tạo 2 threads
- Cả 2 threads gọi `sp_CreateAppointment` cùng StaffID, cùng khung giờ
- **Kỳ vọng**: 1 thành công, 1 thất bại với lỗi "Lịch bị trùng"

### Test 2: Schema Evolution
- Lấy document cũ (chưa có vaccination)
- Gọi `PATCH /histories/{id}/events` với type=vaccination
- **Kỳ vọng**: Event mới được thêm, dữ liệu cũ không mất

### Test 3: Aggregation Pipeline
- Gọi `GET /analytics/diagnoses?disease=Viêm`
- **Kỳ vọng**: Trả về danh sách bệnh nhân có chẩn đoán chứa "Viêm"

---

## 📊 TỔNG KẾT PHẠM VI

| Loại Thay Đổi | Số Lượng |
|---------------|----------|
| Files cần MODIFY | 7 |
| Files NEW | 12 |
| **TỔNG CỘNG** | **~19 files** |

### Ước Tính Thời Gian

| Phase | Công Việc | Thời Gian |
|-------|-----------|-----------|
| 1 | MongoDB setup + config | 1h |
| 2 | Entities + Migration | 1h |
| 3 | `sp_CreateAppointment` | 1.5h |
| 4 | Recursive CTE + API | 1h |
| 5 | MongoDB Services | 2h |
| 6 | Analytics Service | 1h |
| 7 | Controllers + DTOs | 1.5h |
| 8 | Testing | 2h |
| **TỔNG** | | **~11h** |

---

## 📝 GHI NHỚ QUAN TRỌNG

> **AI Audit Log**: Theo rubric, cần ghi nhận việc sử dụng AI trong file `AI_AUDIT_LOG.md`, bao gồm prompt gốc, code AI tạo ra, và phân tích/chỉnh sửa thủ công.

> **Demo Tips**: Chuẩn bị kịch bản demo 2 người cùng đặt lịch để minh họa race condition prevention.

> **Vấn đáp**: Mỗi thành viên cần nắm vững module mình phụ trách + hiểu tổng quan toàn hệ thống.
