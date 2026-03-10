# HealthCare+ UML Diagrams

Thu muc nay chua cac so do UML mo ta kien truc va quy trinh cua he thong HealthCare+.

## Cach xem So do

### Online (Khuyen dung)
1. Copy noi dung file `.puml`
2. Truy cap [PlantUML Web Server](https://www.plantuml.com/plantuml/uml/)
3. Paste va xem ket qua

### VS Code Extension
1. Cai extension **PlantUML** (jebbs.plantuml)
2. Mo file `.puml`
3. `Alt + D` de preview

### Export to PNG/SVG
```bash
# Cai Java va PlantUML
java -jar plantuml.jar *.puml -tpng
```

---

## Danh sach So do

### 1. Architecture (Thu muc: `architecture/`)
To chuc theo **CHU DE**, chi tiet va day du.

| File | Mo ta |
|------|-------|
| `ARCH_00_Overview.puml` | Tong quan kien truc he thong (Polyglot Persistence) |
| `ARCH_01_Component.puml` | So do thanh phan 4 layers (Presentation/API/Business/DAL) |
| `ARCH_02_Deployment.puml` | So do trien khai (Docker, LB, Redis, MySQL, MongoDB Atlas) |
| `ARCH_03_TechStack.puml` | Cong nghe su dung (Frontend/Backend/DB/DevOps) |
| `ARCH_04_DataFlow.puml` | Luong du lieu chinh (Tu dang ky den phat thuoc) |
| `ARCH_05_Security.puml` | Kien truc bao mat (Auth/Encryption/Audit/Compliance) |

### 2. Use Case (Thu muc: `usecase/`)
To chuc theo **FLOW/MODULE**, moi file liet ke tat ca actors tham gia.

| File | Mo ta | So UC |
|------|-------|-------|
| `UC_00_Overview.puml` | Tong quan he thong (11 module) | 11 |
| `UC_01_Auth.puml` | Xac thuc & Quan ly phien | 23 |
| `UC_02_Dashboard.puml` | Dashboard & Thong bao | 20 |
| `UC_03_Master.puml` | Quan ly Danh muc (User/Khoa/Phong/DV/Thuoc/Lich truc) | 52 |
| `UC_04_Patient.puml` | Quan ly Benh nhan (Tim kiem/Dang ky/Pha he/Lich su/Giao dich) | 42 |
| `UC_05_Appointment.puml` | Quan ly Lich hen (Tao/Sua/Huy/Check-in) | 28 |
| `UC_06_Queue.puml` | Dieu phoi Hang doi (Them/Goi/Cap nhat) | 21 |
| `UC_07_Clinical.puml` | Kham Lam sang (Chuan bi/Tao phieu/Kham/Chan doan) | 40 |
| `UC_08_CLS.puml` | Can lam sang (Tiep nhan/XN/CDHA/Ket qua) | 44 |
| `UC_09_Prescription.puml` | Ke don & Phat thuoc (Ke don/Xem/Phat/Ton kho) | 35 |
| `UC_10_Billing.puml` | Thanh toan Vien phi (Chi phi/Hoa don/Thu tien/Giao dich) | 37 |
| `UC_11_Report.puml` | Bao cao & Thong ke (Doanh thu/Luot kham/Nhan vien/Thuoc) | 30 |

**Tong cong: ~376 Use Cases**

### 3. ERD - Entity Relationship Diagram (Thu muc: `erd/`)
To chuc theo **DOMAIN**, chi tiet tung truong, kieu du lieu, rang buoc, index.

| File | Mo ta | So Entity |
|------|-------|-----------|
| `ERD_00_Overview.puml` | Tong quan tat ca packages | 5 packages |
| `ERD_01_DuLieuNen.puml` | Master Data (Khoa/Phong/NhanSu/DichVu/LichTruc/Token) | 6 |
| `ERD_02_BenhNhanTiepNhan.puml` | Benh nhan, Lich hen, Phieu kham, CLS, Ket qua | 6 |
| `ERD_03_HangDoiVaKham.puml` | Hang doi, Luot kham, Phieu tong hop, Phieu chan doan | 4 |
| `ERD_04_KeDonThuVienPhi.puml` | Kho thuoc, Don thuoc, Hoa don, Lich su xuat | 5 |
| `ERD_05_ThongBao.puml` | Thong bao, Nguoi nhan, Mau thong bao | 3 |
| `ERD_06_MongoDB.puml` | Document schemas (medical_histories, audit_logs) | 6 event types |
| `ERD_Diff_NewAttributes.puml` | Cac thuoc tinh/Entity moi them vao so voi ERD cu | **Diff** |
| `ERD_Full_Unified.puml` | **TONG HOP tat ca ERD** trong 1 file (22 tables + 2 collections) | **ALL** |

**Tong: 22 Tables (MySQL) + 2 Collections (MongoDB)**

### 4. Workflow — Activity Diagram (Thu muc: `workflow/`)
To chuc theo **GIAI DOAN**, moi file bao gom **Happy Path + Cancellation/Error**.

| File | Mo ta | Noi dung chinh |
|------|-------|---------------|
| `WF_00_Appointment.puml` | **[MOI]** Dat lich hen kham | Defense in Depth (BE+DB), Xac nhan, Check-in, Huy |
| `WF_01_Reception.puml` | Tiep don & Dieu phoi | Check-in, Priority queue, HoaDon chua_thu, BN bo ve rollback |
| `WF_02_Examination.puml` | Kham benh (Lam sang) | Sinh hieu, Kham, Chi dinh CLS, Ke don, Huy phieu, Het thuoc |
| `WF_03_CLS.puml` | Can lam sang (XN/CDHA) | Lay mau, Ket qua, Bat thuong, PhieuTongHop, Quay lai BS |
| `WF_04_Pharmacy.puml` | Duoc & Cap phat thuoc | SERIALIZABLE transaction, Rollback kho, Huy don, LichSuXuatKho |
| `WF_05_Billing.puml` | Thanh toan & VietQR | chua_thu→da_thu, QR Webhook, Huy hoa don, Ket thuc luot kham |
| `WF_FULL_STANDARD_UPGRADED.puml` | **Toan trinh** (0→6) | Ket hop tat ca WF thanh 1 flow lien tuc |

### 5. Sequence Diagram (Thu muc: `sequence/`)
To chuc theo **MODULE**, chi tiet API calls, SQL queries, SignalR events.

| File | Mo ta | So luong groups |
|------|-------|----------------|
| `SEQ_01_Appointment.puml` | **[MOI]** Dat lich (Defense in Depth) | 4 (Happy + BE error + Race cond + Huy) |
| `SEQ_02_Reception.puml` | **[MOI]** Tiep don & Tao hang doi | 4 (Tim BN + Check-in + Queue + BN bo ve) |
| `SEQ_03_Examination.puml` | **[MOI]** Kham benh + CLS + Ke don | 5 (Vitals + Exam + CLS + Diag + Complete) |
| `SEQ_04_Pharmacy.puml` | **[MOI]** Phat thuoc & Huy don | 2 (Phat thuoc + Huy don rollback) |
| `SEQ_05_Billing.puml` | **[MOI]** Thanh toan & VietQR | 5 (Xem HD + Cash + QR + Huy + Ket thuc) |
| `SEQ_FULL_VISIT_UPGRADED.puml` | Toan trinh 1 luot kham | 5 phases (Reception→Billing) |

---

## Vai tro (6 Actors)
| Actor | Mau sac | Mo ta |
|-------|---------|-------|
| Admin | #LightCoral | Quan tri he thong, CRUD danh muc |
| Y ta Hanh chinh | #LightBlue | Tiep nhan, Lich hen, Thanh toan, Phat thuoc |
| Y ta Lam sang | #LightGreen | Ho tro Bac si, Do sinh hieu, Tao phieu kham |
| Y ta Can lam sang | #Gold | Tiep nhan chi dinh, Lay mau, Ho tro KTV |
| Bac si | #Lavender | Kham benh, Chan doan, Chi dinh CLS, Ke don |
| Ky thuat vien | #PeachPuff | Thuc hien XN/CDHA, Nhap ket qua, Upload file |

---

## Ghi chu
- Tat ca so do su dung tieng Viet khong dau.
- Cac so do workflow the hien swimlane theo vai tro nguoi dung.
- ERD MongoDB the hien cau truc document linh hoat (Schema Evolution).
- Use case duoc danh so nhat quan: UC01.x, UC02.x, ...
