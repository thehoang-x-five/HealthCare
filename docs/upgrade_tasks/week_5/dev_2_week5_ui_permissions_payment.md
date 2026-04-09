# Week 5 - Dev 2: Frontend UAT, Permission Matrix, Payment Flow

> Week 5 khong phai tuan mo them feature lon cho frontend.
> Muc tieu la chot UI theo source hien tai, verify role matrix, regression thanh toan va ghi lai evidence demo/UAT.

## 1. Baseline UI hien tai

- [x] Kien truc hien tai la `staff = user`.
- [x] Khong co trang `/user-management` rieng; admin quan ly nguoi dung ngay trong `/staff`.
- [x] RBAC frontend da duoc chot lai theo `permissions.js` + `ProtectedRoute`.
- [x] Payment Wizard da hoan thien cho luong:
  - [x] tao phieu
  - [x] thu tien mat
  - [x] VietQR
  - [x] thu sau
- [x] Admin UI da duoc hop nhat vao:
  - [x] `Staff`
  - [x] `Departments`
  - [x] `Notifications`
  - [x] `Prescriptions`
- [x] Pagination, toast, filter popover da duoc chuan hoa lai tren cac trang chinh.

## 2. Cong viec Dev 2 da hoan tat

### 2.1 Permission va route

- [x] Route/menu da duoc chot lai cho `admin`.
- [x] Route/menu da duoc chot lai cho `y_ta_hanh_chinh`.
- [x] Route/menu da duoc chot lai cho `bac_si`.
- [x] Route/menu da duoc chot lai cho `y_ta_lam_sang`.
- [x] Route/menu da duoc chot lai cho `y_ta_can_lam_sang`.
- [x] Route/menu da duoc chot lai cho `ky_thuat_vien`.
- [x] Admin khong con thao tac nghiep vu trai role o `Appointments`, `Patients`, `Examination`.
- [x] `y_ta_hanh_chinh` chi xem bao cao y khoa, khong xem doanh thu khi khong duoc phep.

### 2.2 Payment va nghiep vu UI

- [x] Payment Wizard khong con bo qua UI thanh toan o luong lap phieu.
- [x] VietQR UI da duoc noi vao wizard.
- [x] `Thu sau` da duoc noi vao wizard.
- [x] Toast/payment feedback da duoc chuan hoa ve mot he thong thong bao.
- [x] Popup thong bao phu gay chong toast da duoc bo.

### 2.3 Admin UI

- [x] `Staff` la man quan tri user/staff chinh.
- [x] Admin co create/edit/lock/unlock/reset password trong `Staff`.
- [x] Admin co quan tri khoa/phong/dich vu trong `Departments`.
- [x] Admin co mode quan tri thong bao trong `Notifications`.
- [x] Admin co quan ly thuoc/kho trong `Prescriptions`.

### 2.4 Chuan hoa UI

- [x] Pagination da duoc dong bo lai tren cac trang chinh.
- [x] Filter popover da co footer action va giao dien dong bo hon.
- [x] Toast reset/header khong con lech pattern giua cac trang.
- [x] KPI dashboard da duoc chot lai theo role va wording nghiep vu.

## 3. Nhiem vu verify cua Dev 2 trong Week 5

### 3.1 Permission matrix UI

Can verify thu cong tren 6 tai khoan seed sau:

| Vai tro | Username | Password |
|---|---|---|
| Admin | `admin` | `Admin@123` |
| Y ta hanh chinh | `yt_hc01` | `P@ssw0rd` |
| Y ta lam sang | `yt_ls01` | `P@ssw0rd` |
| Y ta can lam sang | `yt_cls01` | `P@ssw0rd` |
| Bac si | `bs_noi01` | `P@ssw0rd` |
| KTV | `ktv_xn_01` | `KTV@123` |

Checklist:

- [ ] Sidebar dung theo role.
- [ ] Route direct URL dung theo role.
- [ ] Button/action dung theo role.
- [ ] Data hien thi dung theo scope role.
- [ ] Trang nao khong duoc phep thi phai ro rang `khong co quyen` hoac redirect hop ly.

### 3.2 E2E UI flow can quay / verify

- [ ] Luong hen kham -> check-in -> hang doi -> kham -> CLS -> don thuoc -> thu tien -> phat thuoc -> hoan tat.
- [ ] Luong `VietQR`.
- [ ] Luong `Thu sau`.
- [ ] Luong huy lich hen / huy luot kham / huy phieu CLS / huy don / huy hoa don.
- [ ] Luong admin:
  - [ ] tao nhan su
  - [ ] khoa / mo khoa
  - [ ] reset password
  - [ ] CRUD khoa / phong / dich vu

### 3.3 Regression UI can chot

- [ ] Khong con toast chong len nhau.
- [ ] Khong con popup thong bao phu de ngoai he toast chung.
- [ ] Payment Wizard mo dung o moi diem vao nghiep vu.
- [ ] Popover loc, pagination, badge scope, KPI cards hien thi dong bo.
- [ ] Staff table/card view va menu action hoat dong on dinh.

## 4. Ky vong theo tung cum trang

### 4.1 Dashboard

- [ ] Scope badge/text dung theo role.
- [ ] KPI cuoi cung khong bi lap nghia.
- [ ] Role khong co doanh thu thi phai thay bang KPI nghiep vu phu hop.

### 4.2 Appointments

- [ ] `y_ta_hanh_chinh` co full nghiep vu.
- [ ] `admin` chi xem.
- [ ] Role con lai khong thay action sai.
- [ ] Form tao hen kham khong tu reload khi dang nhap lieu.

### 4.3 Patients

- [ ] Toast da dong bo.
- [ ] Action `Bo ve` / status flow khong chong feedback.
- [ ] Admin chi xem, khong thao tac nghiep vu.
- [ ] Role hop le moi thay action hop le.

### 4.4 Examination / CLS

- [ ] Bac si / y ta LS / y ta CLS / KTV thay dung tab va action.
- [ ] Khong con nut action gia ma bam khong duoc.
- [ ] Queue va ket qua CLS khong hien sai scope.

### 4.5 Prescriptions

- [ ] Don thuoc / kho thuoc dung role.
- [ ] Pagination hien dong bo.
- [ ] Y ta hanh chinh / admin thao tac dung vai tro.

### 4.6 Staff / Departments / Notifications

- [ ] Admin UI hoat dong on.
- [ ] Khong con dead route theo kien truc cu.
- [ ] Form filter / popover / modal dong bo va on dinh.

### 4.7 Reports / History

- [ ] `y_ta_hanh_chinh` xem bao cao y khoa, khong xem doanh thu neu khong duoc phep.
- [ ] Bac si xem dung phan bao cao duoc cap.
- [ ] Role khong duoc xem thi khong lo UI/cache cu.
- [ ] History khong hien sai scope.

## 5. Khong lam trong Week 5

- [x] Khong mo lai trang `/user-management`.
- [x] Khong mo them flow FE trai voi contract backend da chot.
- [x] Khong doi role matrix mot cach tuy y tren frontend de "ne" backend.
- [x] Khong mo them bo component auth moi neu khong co bug that.

## 6. Deliverable Dev 2 cua Week 5

- [ ] Bang kiem thu permission matrix UI da dien.
- [ ] Danh sach bug UAT neu con.
- [ ] Video / screenshot evidence cho luong nghiep vu chinh.
- [ ] Xac nhan frontend build pass tren baseline Week 5.
- [ ] Xac nhan khong con dead flow lon cua Week 4.

## 7. Source of truth cho Dev 2

- `my-patients/src/utils/permissions.js`
- `my-patients/src/main.jsx`
- `my-patients/src/routes/Staff.jsx`
- `my-patients/src/routes/Departments.jsx`
- `my-patients/src/routes/Notifications.jsx`
- `my-patients/src/components/billing/PaymentWizard.jsx`
- `docs/rbac_matrix.md`
- `docs/WEEK1_4_REALITY_CHECK.md`
- `docs/WEEK1_4_COMPLETION_PLAN.md`
