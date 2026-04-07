# Kiem Tra Thuc Te Week 1-4

> Ngay audit: 2026-04-07
>
> Pham vi ra soat:
> - `docs/upgrade_tasks/week_1..4/*.md`
> - `docs/WEEK1_4_COMPLETION_PLAN.md`
> - Source backend trong `HealthCare`
> - Source frontend o repo `my-patients`
>
> Verify da thuc hien ngay 2026-04-07:
> - Backend `dotnet build --no-restore`: thanh cong, `0 warnings`, `0 errors`
> - Frontend `npm run build`: thanh cong
>
> Luu y:
> - File nay la baseline hien tai cho Week 1-4.
> - Build pass chi chung minh compile-time integrity.
> - Khong thay the cho regression, UAT, va sign-off cuoi cung cua Week 5.

---

## Ket Luan Tong Quan

Week 1-4 hien da o trang thai co the mo ta trung thuc la:

- da hoan tat phan implementation de chuyen sang Week 5
- khong con bi block boi cac loi code-level lon ma audit dau tien tim ra
- van chua nen goi la release-ready neu chua chay verify runtime cua Week 5

Tom tat:

- `Trang thai implementation`: du de chuyen sang Week 5
- `Do tin cay release`: van phu thuoc vao manual regression theo role

---

## Trang Thai Hien Tai Theo Tuan

| Tuan | Trang thai hien tai | Y nghia |
|---|---|---|
| Week 1 | ✅ Da implement | Nen tang infrastructure, SQL/Mongo, va bootstrap DB da co trong code |
| Week 2 | ✅ Da implement | Genealogy va Mongo medical-history da di vao user flow FE |
| Week 3 | ✅ Da implement | Audit va analytics da khop voi schema Mongo that va FE adapters |
| Week 4 | ✅ Da implement | RBAC, admin runtime model, payment inline, VietQR, seed/runtime roles da san sang |

Phan con lai khong con la "lam not code Week 1-4".  
Phan con lai la viec cua Week 5: verify, regression, seed validation, role/UAT, va demo readiness.

---

## Thuc Te Da Xac Minh Theo Tuan

### Week 1: Infrastructure, SQL bootstrap, appointment state flows

Trang thai: `da implement`

Da xac minh co trong code:
- `AppointmentService` goi `sp_BookAppointment`
- genealogy services goi cac SQL procedures can thiet
- `DatabaseBootstrapper` hien dam bao stored procedures, triggers, va check constraints ton tai
- `Program.cs` chay migrations xong roi chay `DatabaseBootstrapper`

Thuc te:
- gap "bootstrap moi truong moi" cua audit cu da duoc xu ly trong code
- Week 1 khong con bi phu thuoc vao hidden manual SQL steps nhu baseline chinh nua

Luu y:
- Week 5 van nen chay mot fresh-environment runtime check

### Week 2: Mongo medical history va genealogy

Trang thai: `da implement`

Da xac minh co trong code:
- Mongo medical history repository ton tai
- backend medical-history endpoint ton tai
- genealogy backend ton tai
- genealogy frontend ton tai
- `PatientViewMode.jsx` da goi `useMedicalHistory()`
- `PatientTimeline.jsx` da nhan du lieu timeline cho patient view

Thuc te:
- Mongo medical history khong con la feature chi co backend
- user flow benh nhan hien da co duong FE thuc su cho no

Luu y:
- Week 5 van can verify chat luong du lieu va giao dien voi account seeded that

### Week 3: Analytics, audit logs, dashboard

Trang thai: `da implement`

Da xac minh co trong code:
- audit log repository va middleware ton tai
- analytics service hien query dung schema Mongo that bang `event_date` va `data.*`
- analytics FE adapter ton tai
- reports/analytics UI ton tai va build pass

Thuc te:
- hard mismatch cu giua schema Mongo ghi va schema Mongo doc da duoc xu ly
- contract drift analytics FE/BE hien da la bai toan verify, khong con la blocker implementation da biet

Luu y:
- Week 5 can test analytics tren du lieu seeded that va chart mong muon, khong chi dua vao build

### Week 4: RBAC, admin, payment inline, VietQR

Trang thai: `da implement`

Da xac minh co trong code:
- `RequireRoleAttribute` va `RequireNurseTypeAttribute` ton tai va dang duoc dung
- da co hardening role/data-scope backend tren cac module chinh
- runtime model admin da ro la `staff = user`
- `AdminController` va `AdminService` co user/staff management API that
- `Staff` la duong runtime that de admin quan ly user
- `Departments` va `Notifications` da co admin controls tai cho
- payment confirmation va VietQR endpoints ton tai
- frontend co `PaymentWizard`
- frontend co `VietQRDisplay`
- frontend co `useGenerateVietQR()` va da noi vao wizard
- da co nhanh `Thu sau`

Thuc te:
- cac blocker implementation lon cua Week 4 trong audit cu khong con la baseline nua
- Week 4 hien nen duoc mo ta la da implement, con Week 5 tap trung vao verify end-to-end

Luu y:
- mot so hanh vi runtime theo role van can duoc test tay sau restart/seed o Week 5

---

## Nhung Gi Da Thay Doi So Voi Audit Dau Tien

Audit cu da dung khi no phat hien cac gap lon vao thoi diem do.  
Nhung nhieu finding trong so do khong con la baseline dung nua.

Nay co the xem la da fix trong code:
- DB bootstrap cho procedures/triggers/check constraints
- path chuan hoa contract admin FE/BE
- enforcement nurse subtype o pharmacy
- VietQR integration trong `PaymentWizard`
- flow `Thu sau`
- muc do bao phu `ProtectedRoute` rong hon
- patient timeline wiring cho Mongo history
- nhieu lo hong RBAC va data-scope tren admin, Y ta HC, Bac si, Y ta LS, Y ta CLS, va KTV

Nay duoc xem la quyet dinh kien truc, khong phai viec dang thieu:
- `staff = user`
- khong tach `UserAccount`
- khong co mot admin runtime page rieng thay cho `Staff`

---

## Rui Ro Con Lai Cho Week 5

Day khong con la blocker de vao Week 5, nhung la dung trong tam can theo:

1. Van can manual regression theo tung role.
2. Data scope phai duoc verify bang account that, khong chi doc source.
3. Payment/VietQR phai duoc test end-to-end voi hoa don seeded that.
4. Lich truc, queue visibility, va department scoping can runtime validation.
5. Frontend build van con warning non-blocking:
   - SignalR pure comment warning
   - large chunk warning tu Vite

---

## Ket Luan Trung Thuc

Neu hoi:

- "Da du de qua Week 5 chua?" -> `Co`
- "Da co the tuyen bo production-ready 100% ma khong can verify nua chua?" -> `Chua`

Phat bieu handoff dung nhat la:

> Week 1-4 da duoc hoan tat den muc implementation, du de dong bang feature work va chuyen sang Week 5 cho regression, UAT, chuan bi demo, va chot do tin cay release.
