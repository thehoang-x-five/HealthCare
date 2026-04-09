# Week 5 - Dev 1: Backend RBAC, Seed, Smoke Verify

> Muc tieu cua Week 5 la chot backend va verify, khong mo them mot huong kien truc moi.
> Kien truc hien tai la `staff = user`, auth fields nam trong `NhanVienYTe`, khong co bang `user_accounts`.

## 1. Baseline hien tai

- [x] Week 1-4 da duoc chot lai theo source hien tai.
- [x] `Program.cs` da tu dong `MigrateAsync()`, `DatabaseBootstrapper.BootstrapAsync()` va `DataSeed.EnsureSeedAsync(db)`.
- [x] Backend RBAC da chuyen ve mo hinh claim `VaiTro` + `LoaiYTa` + data scope theo JWT.
- [x] Data scope backend da duoc sieu lai cho cac man nghiep vu chinh: dashboard, queue, patient, clinical, CLS, pharmacy, reports, notifications.
- [x] Backend build baseline da pass.
- [x] Da co script smoke test de verify nhanh role matrix sau moi lan build.

## 2. Tai khoan seed dung cho Week 5

| Vai tro | Username | Password | Ghi chu |
|---|---|---|---|
| Admin | `admin` | `Admin@123` | Tai khoan admin seed moi de test clean DB |
| Y ta hanh chinh | `yt_hc01` | `P@ssw0rd` | Global operational scope |
| Y ta lam sang | `yt_ls01` | `P@ssw0rd` | Scope theo khoa lam sang |
| Y ta can lam sang | `yt_cls01` | `P@ssw0rd` | Scope theo khoa CLS |
| Bac si | `bs_noi01` | `P@ssw0rd` | Scope theo khoa Noi |
| KTV | `ktv_xn_01` | `KTV@123` | Scope theo khoa XN / phong CLS |

Nguon seed:

- `Datas/DataSeed.cs`

## 3. Cong viec Dev 1 da hoan tat

### 3.1 Auth va seed

- [x] Giu mo hinh `NhanVienYTe` chua thong tin auth, khong tach `UserAccount`.
- [x] Seed bo tai khoan role dung cho verify Week 5.
- [x] Bo sung tai khoan `admin / Admin@123` trong source seed.
- [x] Dam bao role matrix seed phu hop voi huong kiem thu Week 5.

### 3.2 Backend RBAC va data scope

- [x] `Appointments`: chi admin xem, chi `y_ta_hanh_chinh` thao tac nghiep vu.
- [x] `Patients`: role action-level va read scope da duoc chot theo JWT.
- [x] `Clinical / CLS`: queue, tim kiem, chi tiet, cancel, result da duoc sieu scope.
- [x] `Pharmacy`: role action-level va search scope da duoc chot.
- [x] `Reports`: `y_ta_hanh_chinh` chi xem bao cao y khoa, khong xem doanh thu neu khong duoc cap phep.
- [x] `Notifications`: inbox/status suy tu JWT, khong tin role-scope tu query string.
- [x] `Dashboard` va `Queue`: khong con cho FE override data scope.

### 3.3 Verify tooling

- [x] Them script `Scripts/verify_week5_smoke.ps1`.
- [x] Script build backend.
- [x] Script build frontend.
- [x] Script login va smoke theo cac tai khoan seed.
- [x] Script verify cac endpoint toi thieu:
  - [x] `POST /api/auth/login`
  - [x] `GET /api/dashboard/today`
  - [x] `POST /api/appointments/search`
  - [x] `GET /api/patient?page=1&pageSize=1`
  - [x] `POST /api/reports/overview`
  - [x] `GET /api/admin/users?page=1&pageSize=1`
- [x] Script xuat `smoke-report.json` va `smoke-report.md` vao `artifacts/week5-smoke`.

## 4. Cach verify Week 5 cho Dev 1

### 4.1 Lenh chay nhanh

```powershell
powershell -ExecutionPolicy Bypass -File .\Scripts\verify_week5_smoke.ps1
```

### 4.2 Output can kiem tra

- `artifacts/week5-smoke/smoke-report.json`
- `artifacts/week5-smoke/smoke-report.md`
- `artifacts/week5-smoke/backend.stdout.log`
- `artifacts/week5-smoke/backend.stderr.log`

### 4.3 Tieu chi pass

- [ ] Tat ca role login thanh cong tren DB da seed moi.
- [ ] Status code cua role matrix khop smoke script.
- [ ] Khong co endpoint nao tra `500`.
- [ ] Backend startup sach, khong crash sau migrate + bootstrap + seed.

## 5. Nhiem vu con lai cua Dev 1 trong Week 5

Nhung muc nay la verify / handoff, khong phai mo feature moi.

- [ ] Chay smoke test tren mot DB clean snapshot.
- [ ] Chay manual regression backend voi 6 tai khoan seed.
- [ ] Export / luu lai evidence cho role matrix da chot.
- [ ] Chot Swagger/OpenAPI dung theo source hien tai.
- [ ] Dong bo `rbac_matrix.md` neu co thay doi cuoi cung sau UAT.
- [ ] Ho tro Dev 2 trong cac bug UI-to-BE contract neu phat sinh luc UAT.

## 6. Khong lam trong Week 5

- [x] Khong mo lai huong tach `UserAccount`.
- [x] Khong tao bang `user_accounts`.
- [x] Khong mo them trang auth/doc architecture trai voi source hien tai.
- [x] Khong doi contract backend neu khong phai bug / verify issue.

## 7. Ket luan handoff sang Week 5

Dev 1 da hoan tat phan implementation can thiet de vao Week 5. Cong viec con lai cua tuan nay la:

- verify build
- verify smoke
- verify role matrix
- regression/UAT
- dong bo tai lieu evidence

Source of truth cua Week 5:

- `Program.cs`
- `Datas/DataSeed.cs`
- `Scripts/verify_week5_smoke.ps1`
- `docs/rbac_matrix.md`
- `docs/WEEK1_4_REALITY_CHECK.md`
- `docs/WEEK1_4_COMPLETION_PLAN.md`
