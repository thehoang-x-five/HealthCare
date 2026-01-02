# Ma trận phân quyền hệ thống (Cập nhật chi tiết)

## Vai trò và Chức vụ

### Vai trò (VaiTro):
- `admin` - Quản trị viên (toàn quyền)
- `bac_si` - Bác sĩ
- `y_ta` - Y tá (có phân loại)
- `ky_thuat_vien` - Kỹ thuật viên

### Loại Y tá (LoaiYTa):
- `hanhchinh` - Y tá hành chính (tiếp nhận)
- `phong_kham` - Y tá lâm sàng (phòng khám) - **quyền = Bác sĩ**
- `can_lam_sang` - Y tá CLS - **quyền = Kỹ thuật viên**

---

## 📋 PHÂN QUYỀN CHI TIẾT

### 1. TIẾP NHẬN (Lịch hẹn + Bệnh nhân)

#### Backend API:
| Chức năng | Endpoint | Quyền |
|-----------|----------|-------|
| Tạo lịch hẹn | `POST /api/appointments` | Y tá HC, Admin |
| Cập nhật lịch hẹn | `PUT /api/appointments/{id}` | Y tá HC, Admin |
| Check-in | `PUT /api/appointments/{id}/status` | Y tá HC, Admin |
| Xem lịch hẹn | `GET /api/appointments/*` | **Tất cả** |
| Tạo bệnh nhân | `POST /api/patient` | Y tá HC, Admin |
| Cập nhật thông tin BN | `POST /api/patient` (update) | Y tá HC, Admin |
| Xem bệnh nhân | `GET /api/patient/*` | **Tất cả** |

#### Frontend UI:
| Trang | Thành phần | Quyền |
|-------|------------|-------|
| Lịch hẹn | Nút "+ Tạo lịch hẹn" | Y tá HC, Admin |
| Lịch hẹn | Nút "Check-in" | Y tá HC, Admin |
| Lịch hẹn | Nút "Sửa/Xóa" trong modal | Y tá HC, Admin |
| Bệnh nhân | Nút "+ Thêm" | Y tá HC, Admin |
| Bệnh nhân | Nút "✎ Sửa" | Y tá HC, Admin |
| Bệnh nhân | Nút "Tạo lịch hẹn" (trong modal) | Y tá HC, Admin |
| Bệnh nhân | Tab "Thông tin" - form edit | Y tá HC, Admin |

**Ẩn với:** Bác sĩ, Y tá LS, Kỹ thuật viên, Y tá CLS

---

### 2. KHÁM BỆNH (Lâm sàng)

#### Backend API:
| Chức năng | Endpoint | Quyền |
|-----------|----------|-------|
| Tạo phiếu khám LS | `POST /api/clinical` | Y tá HC, Y tá LS, Admin |
| Cập nhật trạng thái | `PUT /api/clinical/{id}/status` | Bác sĩ, Y tá LS, Admin |
| Tạo chẩn đoán | `POST /api/clinical/final-diagnosis` | Bác sĩ, Y tá LS, Admin |
| Hoàn tất khám | `POST /api/clinical/{id}/complete` | Bác sĩ, Y tá LS, Admin |
| Tạo phiếu CLS (chỉ định) | `POST /api/cls/orders` | Bác sĩ, Y tá LS, Admin |
| Xem phiếu khám | `GET /api/clinical/*` | **Tất cả** |

#### Frontend UI:
| Trang | Thành phần | Quyền |
|-------|------------|-------|
| Khám bệnh | Toàn bộ trang | Bác sĩ, Y tá LS, Admin |
| Khám bệnh | Nút "Gọi vào" | Bác sĩ, Y tá LS, Admin |
| Khám bệnh | Tab "Lập phiếu khám" | Y tá HC, Y tá LS, Admin |
| Khám bệnh | Tab "Xử lý & Chẩn đoán" | Bác sĩ, Y tá LS, Admin |
| Bệnh nhân | Nút "Lập phiếu khám" | Y tá HC, Y tá LS, Admin |
| Bệnh nhân | Nút "Xử lý & chẩn đoán" | Bác sĩ, Y tá LS, Admin |

**Ẩn "Gọi vào" với:** Y tá HC (chỉ xem danh sách hàng chờ)

---

### 3. CẬN LÂM SÀNG (CLS)

#### Backend API:
| Chức năng | Endpoint | Quyền |
|-----------|----------|-------|
| Tạo phiếu CLS | `POST /api/cls/orders` | Bác sĩ, Y tá LS, Admin |
| Cập nhật trạng thái | `PUT /api/cls/orders/{id}/status` | Kỹ thuật viên, Y tá CLS, Admin |
| Tạo kết quả | `POST /api/cls/results` | Kỹ thuật viên, Y tá CLS, Admin |
| Tạo tổng hợp | `POST /api/cls/summary/{id}` | Kỹ thuật viên, Y tá CLS, Admin |
| Xem phiếu CLS | `GET /api/cls/*` | **Tất cả** |

#### Frontend UI:
| Trang | Thành phần | Quyền |
|-------|------------|-------|
| Khám bệnh (CLS) | Toàn bộ trang | Kỹ thuật viên, Y tá CLS, Admin |
| Khám bệnh (CLS) | Nút "Gọi vào" | Kỹ thuật viên, Y tá CLS, Admin |
| Khám bệnh (CLS) | Cập nhật kết quả | Kỹ thuật viên, Y tá CLS, Admin |

**Lưu ý:** Data đã phân riêng LS/CLS nên mỗi nhóm chỉ thấy data của mình

---

### 4. CÁC TRANG KHÁC (Chưa có thao tác)

| Trang | Quyền |
|-------|-------|
| Lịch sử | **Tất cả** (chỉ xem) |
| Đơn thuốc | **Tất cả** (chỉ xem) |
| Khoa/Phòng | **Tất cả** (chỉ xem) |
| Nhân sự | **Tất cả** (chỉ xem) |
| Dashboard | **Tất cả** (chỉ xem) |

---

## 🎯 TÓM TẮT THEO VAI TRÒ

### Y tá Hành chính:
- ✅ Toàn quyền: Tiếp nhận (Lịch hẹn, Bệnh nhân, Lập phiếu khám)
- ✅ Xem: Khám bệnh (danh sách hàng chờ)
- ❌ Không: Gọi vào khám, Chẩn đoán, CLS

### Bác sĩ / Y tá Lâm sàng:
- ✅ Toàn quyền: Khám bệnh LS (Gọi vào, Chẩn đoán, Chỉ định CLS)
- ✅ Xem: Tất cả trang khác
- ❌ Không: Tiếp nhận (Tạo lịch hẹn, Tạo BN, Sửa thông tin BN)

### Kỹ thuật viên / Y tá CLS:
- ✅ Toàn quyền: Khám bệnh CLS (Gọi vào, Cập nhật kết quả)
- ✅ Xem: Tất cả trang khác
- ❌ Không: Tiếp nhận (Tạo lịch hẹn, Tạo BN, Sửa thông tin BN)

### Admin:
- ✅ Toàn quyền: **TẤT CẢ**
