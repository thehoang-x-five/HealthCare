# Huong dan hoan thanh Tuan 4 - Dev 2: UI Permissions 7 Tang, Payment Wizard, Staff Admin View, VietQR UI, FE Contract

> Cap nhat theo code hien tai ngay 2026-04-07.
>
> File nay chot lai nhung gi frontend cua Week 4 da lam xong theo code thuc te. Cac muc test/UAT/demo duoc tach ro sang Week 5.

---

## Trang thai tong quan

| NV | Noi dung | Trang thai | Ghi chu |
|---|---|:---:|---|
| NV1 | Auth store update | ✅ DONE | Auth/permissions da cap nhat theo mo hinh hien tai `NhanVienYTe = user` |
| NV2 | Phan quyen UI 7 tang | ✅ DONE | Menu, route, page, component, action-level da co; data scope backend da duoc noi vao FE |
| NV3 | Payment Wizard + VietQR UI | ✅ DONE | Da co wizard inline, `Thu sau`, `VietQRDisplay`, billing hooks |
| NV4 | Trang QL Nhan vien rieng | ❌ HUY | Khong tao page rieng; gop vao `Staff` page cho admin |
| NV5 | Chuan hoa FE client/enum/adapter | ✅ DONE | Da co `enums.js`, adapter/hook/mapping cho cac luong Week 4 |

> Luu y: File Dev 2 chi co 5 nhiem vu. Testing UAT, demo, va checklist tay da duoc day sang Week 5.

---

## Quyet dinh chot cua Week 4

### 1. Staff page la giao dien quan ly user/staff cho admin

- [x] Khong tao `/user-management` rieng
- [x] Admin vao `Staff` va co table view + auth columns + action buttons
- [x] Non-admin van xem `Staff` o dang HR/read-only
- [x] Route `/admin/users` hien render vao `Staff`, khong phai page doc lap

### 2. Week 4 chot o muc implementation FE

- [x] Da co permission matrix va route guard
- [x] Da co payment wizard, VietQR UI, contract adapters
- [x] Da co admin actions tren Staff
- [x] Cac bai test tay/UAT chi tiet de sang Week 5

### 3. Mo hinh auth FE theo code that su

- [x] Khong con gia dinh tach `UserAccount`
- [x] `chucVu`/`vaiTro`/`loaiYTa` duoc dung theo contract hien tai
- [x] Permissions doc tu user hien tai, khong doi sang auth model cu

---

## NV1 - Auth store va permission system

### Da hoan thanh

- [x] `permissions.js` duoc mo rong theo role matrix hien tai
- [x] Helpers cho admin auth view: `canViewStaffAuth`, `canToggleStaffView`, `canLockUnlock`, `canResetPw`
- [x] Login/auth store phu hop voi mo hinh hien tai cua backend
- [x] Khong con phu thuoc vao ke hoach tach `UserAccount`
- [x] Role/subtype duoc doc va su dung trong UI guards

### Chot pham vi

- [x] Day la update auth store/phat quyen theo code that
- [x] Bo toan bo mo ta cu kieu "staff != user" hoac "chuyen sang UserAccount model"

**Ket luan:** NV1 da xong theo contract hien tai.

---

## NV2 - Phan quyen UI 7 tang

### Da hoan thanh

- [x] Menu visibility theo `TAB_VISIBILITY`
- [x] Route guard qua `ProtectedRoute`
- [x] Page-level guard tren cac route chinh
- [x] Component/action-level guard tren `Appointments`, `Patients`, `Departments`, `Staff`, `Examination`, `Prescriptions`, `Reports`, `Notifications`
- [x] Scope hint/UI phu hop voi backend data scope moi
- [x] Admin chi xem o cac man nghiep vu nhay cam, khong con thao tac vuot quyen
- [x] Y ta hanh chinh/Bac si/Y ta LS/Y ta CLS/KTV da duoc siat lai theo permission matrix hien tai

### Chot pham vi

- [x] Week 4 chi can FE guards va wiring dung voi backend
- [x] Manual verify tung role la viec cua Week 5

**Ket luan:** NV2 da xong o muc implementation FE.

---

## NV3 - Payment Wizard + VietQR UI

### Da hoan thanh

- [x] Co `PaymentWizard`
- [x] Co `PaymentStep`
- [x] Co `VietQRDisplay`
- [x] Co hook `confirmPayment`
- [x] Co hook `generateVietQR`
- [x] Co nhanh `Thu sau`
- [x] Flow tao phieu -> thanh toan -> tiep tuc workflow da duoc noi vao cac man lien quan
- [x] Luong Patients/Examination da mo wizard thay vi nhay thang sang in phieu

### Chot pham vi

- [x] Week 4 da xong phan UI/flow
- [x] Week 5 chi con verify tay cac case thanh toan thuc te

**Ket luan:** NV3 da xong.

---

## NV4 - QL Nhan vien rieng

> **Trang thai cu da HUY.** Khong con tao page `/user-management` rieng.

### Huong thay the da duoc ap dung

- [x] `Staff` dung chung cho tat ca role
- [x] Admin co `Card/Table toggle`
- [x] Admin table co username, vai tro, trang thai tai khoan, action buttons
- [x] Admin co them/sua/khoa/mo/reset password ngay trong luong `Staff`
- [x] API dung `admin.js` khi admin o table mode

### Dieu can loai bo khoi doc cu

- [x] Khong con xem `/user-management` la deliverable cua Week 4
- [x] Khong con test redirect `/user-management` la tieu chi dong Week 4
- [x] `AdminUsers.jsx` neu con ton tai thi xem nhu legacy/tham khao, khong phai source of truth

**Ket luan:** NV4 duoc dong theo quyet dinh huy va gop vao `Staff`.

---

## NV5 - Chuan hoa FE client, enum, adapter

### Da hoan thanh

- [x] Da co `src/constants/enums.js`
- [x] Billing client da dung enum/constants
- [x] Admin adapter da map dung contract backend
- [x] Payment/VietQR hooks da su dung contract hien tai
- [x] Pagination/admin/user mappings da duoc sua lai theo shape backend
- [x] Route/permission/auth utilities da duoc dong bo hoa

### Chot pham vi

- [x] Muc tieu cua Week 4 la FE goi dung API va render dung data
- [x] Viec ra soat them hardcode nho le co the tiep tuc trong Week 5 neu can, nhung khong xem la blocker Week 4

**Ket luan:** NV5 da xong theo muc tieu implementation.

---

## Nhung gi da duoc tich hop trong frontend Week 4

### Staff/Admin

- [x] Staff page dung chung cho moi role
- [x] Admin co table view
- [x] Admin goi `/api/admin/users`
- [x] Admin co create/edit/lock/unlock/reset password

### RBAC/Permissions

- [x] Sidebar loc menu theo role
- [x] Router boc `ProtectedRoute`
- [x] Page actions bi an/disable theo role
- [x] FE khop voi backend action-level permission hien tai

### Payment/VietQR

- [x] Wizard thanh toan hien trong UI
- [x] Ho tro `Tien mat`, `VietQR`, `Thu sau`
- [x] VietQR duoc hien inline thay vi popup rieng

### FE contract

- [x] Admin mapping dung contract hien tai
- [x] Billing hooks dung contract hien tai
- [x] Enum constants da duoc tap trung hoa

---

## Ban giao cho Week 5

### Da san sang

- [x] FE da co du UI de test role matrix
- [x] FE da co du UI de test payment/VietQR
- [x] FE da co Staff-admin flow de test CRUD user/staff

### Viec con lai cua Week 5 la verify/UAT

- [ ] Login tung role va chay smoke test theo route matrix
- [ ] Test payment cash/VietQR/Thu sau voi du lieu that
- [ ] Test Staff admin flow end-to-end
- [ ] Rà lai dead code/legacy page khong con dung
- [ ] Chup demo va viet tai lieu su dung cuoi cung

---

## Ket luan cuoi cung

Week 4 - Dev 2 duoc chot la **hoan thanh ve implementation frontend**.

Nhung diem da duoc dong lai de tranh nham lan:

- [x] Khong co nhiem vu 6, 7 cho file Dev 2
- [x] Khong tao trang `/user-management` rieng
- [x] Staff page la noi admin quan ly user/staff
- [x] Payment Wizard va VietQR UI da co that
- [x] Manual test/UAT/demo duoc day sang Week 5

File nay tu nay duoc dung lam baseline chuan bi cho Week 5.
