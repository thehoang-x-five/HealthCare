# Huong dan hoan thanh Tuan 4 - Dev 1: Auth, RBAC Backend, Thanh toan Inline, VietQR API, Seed Data, Admin API

> Cap nhat theo code hien tai ngay 2026-04-07.
>
> Muc tieu cua file nay la chot lai pham vi Week 4 da lam xong o backend, bo cac ke hoach cu da huy, va tach ro cac muc con lai sang Week 5 de verify/UAT.

---

## Trang thai tong quan

| NV | Noi dung | Trang thai | Ghi chu |
|---|---|:---:|---|
| NV0 | Drop `NoiDungKetQua` | ✅ DONE | Da bo khoi entity SQL, giu chi tiet o MongoDB, luong CLS/History da doc du lieu moi |
| NV1 | Tach `UserAccount` | ❌ HUY | Quyet dinh giu auth fields trong `NhanVienYTe`, khong tach bang |
| NV2 | RBAC Backend | ✅ DONE | Da co `[RequireRole]`, `[RequireNurseType]`, scope backend, bo dần logic bypass thu cong |
| NV3 | Thanh toan inline backend | ✅ DONE | Da co `PaymentConfirmRequest`, endpoint confirm, luong `chua_thu -> da_thu` |
| NV4 | VietQR API | ✅ DONE | Da co generate QR endpoint + service + config |
| NV5 | Chuan hoa contract FE-BE | ✅ DONE | Da chuan hoa DTO/payment/admin mapping can thiet cho FE |
| NV6 | DataSeed viet lai | ✅ DONE | Seed hien tai khop mo hinh that su dung `NhanVienYTe`, co role/phong/lich truc/tai khoan test |
| NV7 | Admin Management API | ✅ DONE | Da co list/create/update/status/lock/reset password cho admin |

---

## Quyet dinh chot cua Week 4

### 1. Khong tach bang `UserAccount`

- [x] Chap nhan mo hinh thuc te: `NhanVienYTe` van la bang chua thong tin auth
- [x] JWT/claims van lay tu `NhanVienYTe`
- [x] Admin API va Staff page duoc thiet ke xoay quanh mo hinh nay
- [x] Bo toan bo noi dung Week 4 cu lien quan den migration `user_accounts`

### 2. Testing, demo, UAT chi tiet chuyen sang Week 5

- [x] Week 4 chot o muc code hoan thanh
- [x] Week 5 se tap trung verify end-to-end, seed lai moi truong sach, test role matrix va demo

### 3. Ghi chu 8 cot y te `BenhNhan -> MongoDB`

- [x] Khong nam trong pham vi chot Week 4
- [x] Tiep tuc de sau Week 5 neu can

---

## NV0 - Drop `NoiDungKetQua`

- [x] SQL chi giu metadata can thiet cua ket qua CLS
- [x] Chi tiet noi dung ket qua duoc doc tu MongoDB
- [x] `ClsService`, `HistoryService`, DTO, seed da duoc cap nhat theo huong nay
- [x] Khong con xem day la blocker cho auth/RBAC/payment cua Week 4

**Ket luan:** NV0 da xong va khong con viec treo cua Week 4.

---

## NV1 - Auth model

> **Trang thai cu da HUY.** Week 4 khong con tach bang `UserAccount`.

### Pham vi chot lai

- [x] Auth fields van nam tren `NhanVienYTe`
- [x] Login/JWT tiep tuc dung model hien tai
- [x] Khong tao migration `user_accounts`
- [x] Khong doi FK `RefreshToken` sang bang moi
- [x] FE/BE dong bo theo quyet dinh "staff = user"

### He qua thiet ke duoc chap nhan

- [x] Admin quan ly user ngay tren luong nhan su
- [x] Staff page la diem vao chung cho HR/Admin
- [x] Khong con nhiem vu ky thuat nao cua Week 4 lien quan tach bang

**Ket luan:** NV1 khong phai "chua lam", ma la **da dong theo quyet dinh huy**.

---

## NV2 - RBAC Backend

### Da hoan thanh trong Week 4

- [x] Controller-level role guard da duoc bo sung cho cac module chinh
- [x] Tach ro `admin`, `bac_si`, `y_ta`, `ky_thuat_vien`
- [x] Tach ro subtype y ta qua `RequireNurseType`
- [x] Admin API da duoc khoa dung role
- [x] Appointments/Patients/Billing/Clinical/CLS/Pharmacy da duoc siat lai action-level
- [x] Dashboard/Queue va cac man read quan trong da duoc bo sung data scope backend
- [x] Thong bao da suy nguoi dung tu JWT thay vi tin query scope tu client

### Week 4 chot o muc nao

- [x] Nen backend RBAC da co the dung de FE tiep tuc verify role
- [x] Khong de lai noi dung "chi them `[RequireRole]`" theo pham vi cu
- [x] Xem Week 5 la giai doan regression/UAT, khong phai con thieu implementation cot loi cua Week 4

**Ket luan:** NV2 duoc danh dau DONE o muc code/backend implementation.

---

## NV3 - Thanh toan inline backend

- [x] Da co `PaymentConfirmRequest`
- [x] Da co endpoint confirm thanh toan hoa don
- [x] Da co flow giu `TrangThai = chua_thu` cho nhung truong hop chua thu ngay
- [x] Da duoc FE Payment Wizard su dung
- [x] Billing controller hien duoc khoa dung cho `y_ta hanh chinh`

**Ket luan:** Phan backend cua thanh toan inline da xong.

---

## NV4 - VietQR API

- [x] Da co endpoint generate QR cho hoa don
- [x] Da co service va config VietQR
- [x] FE da co hook su dung API nay trong wizard thanh toan

**Ket luan:** API VietQR backend da xong, khong con blocker Week 4.

---

## NV5 - Chuan hoa contract FE-BE

- [x] Da co DTO xac nhan thanh toan dung contract hien tai
- [x] Da co mapping cho admin users va `TrangThaiTaiKhoan`
- [x] Da chuan hoa cac truong can thiet de FE goi payment/admin/report/permissions
- [x] Cac mismatch lon cua admin PascalCase/camelCase da duoc xu ly

**Ket luan:** Muc tieu Week 4 la "contract dung de FE chay", phan nay da dat.

---

## NV6 - DataSeed viet lai

> Day la noi dung da duoc update lai theo mo hinh that su dang dung, khong con `UserAccount 1:1`.

### Da hoan thanh

- [x] Seed khoa, phong, dich vu, benh nhan, lich hen, luot kham, hoa don, don thuoc
- [x] Seed `NhanVienYTe` voi auth fields truc tiep tren bang nay
- [x] Seed du role cot loi: admin, y ta hanh chinh, y ta lam sang, y ta can lam sang, bac si, ky thuat vien
- [x] Seed lich truc du cho phong kham va phong dich vu
- [x] Seed account test can thiet cho FE/role testing
- [x] Da bo noi dung seed cu dua tren bang `UserAccount`

### Account/test data can dung cho Week 5

- [x] Admin
- [x] Y ta hanh chinh
- [x] Y ta lam sang
- [x] Y ta can lam sang
- [x] Bac si
- [x] Ky thuat vien

### Ghi chu

- [x] Seed hien tai la "seed thuc te cua du an", khong phai matrix minh hoa cu
- [x] Co the tiep tuc bo sung account/phong neu can cho UAT Week 5, nhung khong xem do la thieu Week 4

**Ket luan:** NV6 da xong theo code thuc te.

---

## NV7 - Admin Management API

### Da hoan thanh

- [x] `GET /api/admin/users`
- [x] `GET /api/admin/users/{id}`
- [x] `POST /api/admin/users`
- [x] `PUT /api/admin/users/{id}`
- [x] `PUT /api/admin/users/{id}/status`
- [x] `PUT /api/admin/users/{id}/lock-status`
- [x] `POST /api/admin/users/{id}/reset-password`

### Pham vi can lam ro

- [x] Week 4 chi chot **Admin user/staff management API**
- [x] Khong tiep tuc ghi `/api/admin/schedules` la mot phan cua NV7
- [x] Quan ly lich truc hien tai di qua module `master-data` va cac modal Staff/Departments, khong phai `AdminController`

**Ket luan:** NV7 da xong o phan API quan ly user/staff. Noi dung schedule duoi `/api/admin` duoc loai bo khoi file nay.

---

## Ban giao cho Week 5

### Da san sang

- [x] Nen auth/rbac backend da co
- [x] Payment/VietQR backend da co
- [x] Admin API da co
- [x] Seed data de login va test role da co

### Viec con lai cua Week 5 la verify, khong phai viet lai Week 4

- [ ] Smoke test tren moi truong sach
- [ ] UAT theo role matrix
- [ ] Rà lai docs tong hop va `rbac_matrix.md`
- [ ] Demo script cho cac luong chinh
- [ ] Regression sau khi restart API/seed moi truong

---

## Ket luan cuoi cung

Week 4 - Dev 1 duoc chot la **hoan thanh ve code**.

Nhung diem truoc day gay nham lan da duoc dong lai:

- [x] Khong tach `UserAccount`
- [x] Khong con ke hoach migration auth model cu
- [x] DataSeed duoc tinh theo `NhanVienYTe` la user
- [x] Admin management chi tinh phan user/staff API
- [x] Manual verify/UAT duoc day sang Week 5

File nay tu nay duoc dung lam baseline de vao Week 5.
