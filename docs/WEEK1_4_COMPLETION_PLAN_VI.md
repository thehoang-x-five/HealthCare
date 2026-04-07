# Ke Hoach Hoan Thanh Week 1-4

> Cap nhat: 2026-04-07
>
> File nay khong con mo ta mot ke hoach "cuu hoa" lon cho Week 1-4 nua.
> No ghi lai vi tri hien tai va checklist handoff sang Week 5.

---

## Vi Tri Hien Tai

Week 1-4 gio nen duoc xem la:

- da hoan tat baseline implementation
- da on dinh kien truc cot loi
- da san sang cho phase verify cua Week 5

Verify ngay 2026-04-07:
- Backend `dotnet build --no-restore`: pass, `0 warnings`, `0 errors`
- Frontend `npm run build`: pass

---

## Dinh Nghia Hoan Thanh De Chuyen Sang Week 5

Week 1-4 duoc xem la da complete du de handoff sang Week 5 khi tat ca dieu sau dung:

1. Backend build sach.
2. Frontend build thanh cong.
3. Logic bootstrap DB ton tai trong code.
4. Core FE/BE contracts da khop du de runtime test.
5. Week 4 payment flow co cash, VietQR, va deferred payment.
6. Admin runtime management ton tai theo mo hinh gop `staff = user`.
7. Role/data-scope enforcement ton tai o ca FE va BE cho cac module chinh.
8. Docs cua cac tuan da phan anh dung kien truc hien tai.

Ket luan hien tai: `dat`

---

## Nhung Gi Da Hoan Thanh

### Week 1

- [x] Nen tang SQL/Mongo ton tai
- [x] Appointment SP path ton tai
- [x] Genealogy SP path ton tai
- [x] Bootstrapper DB hien da apply SQL artifacts can thiet luc startup

### Week 2

- [x] Mongo medical-history backend ton tai
- [x] Genealogy backend ton tai
- [x] Genealogy frontend ton tai
- [x] Wiring patient-facing medical-history timeline ton tai

### Week 3

- [x] Audit log infrastructure ton tai
- [x] Analytics backend doc dung schema Mongo that
- [x] Analytics FE adapter/UI wiring ton tai
- [x] Reports module build pass theo contract hien tai

### Week 4

- [x] Nen tang RBAC backend ton tai
- [x] Enforcement subtype cua nurse ton tai
- [x] Runtime model admin da canh chinh theo `staff = user`
- [x] `Staff` page la duong runtime that cho admin management
- [x] Admin controls cung ton tai trong `Notifications` va `Departments`
- [x] Payment confirm flow ton tai
- [x] VietQR API ton tai
- [x] `PaymentWizard` va `VietQRDisplay` ton tai
- [x] Deferred payment (`Thu sau`) ton tai
- [x] Seed data hien da ho tro cac role/runtime path can test

---

## Checklist Vao Week 5

Phase tiep theo nen tap trung vao bang chung, khong phai viet them nhieu implementation lon.

### 1. Verify Moi Truong

- [ ] Start tu DB sach hoac snapshot da biet
- [ ] Verify migrations + bootstrap scripts chay dung
- [ ] Verify cac account seeded dang nhap duoc

### 2. Regression Theo Tung Role

- [ ] Admin
- [ ] Y ta hanh chinh
- [ ] Bac si
- [ ] Y ta lam sang
- [ ] Y ta can lam sang
- [ ] Ky thuat vien

Voi moi role, verify:
- [ ] menu visibility
- [ ] route access
- [ ] page-level access
- [ ] action-level access
- [ ] backend data scope

### 3. Regression Workflow Chinh

- [ ] dat lich hen
- [ ] check-in
- [ ] dang ky/cap nhat benh nhan
- [ ] flow kham lam sang
- [ ] flow chi dinh va tra ket qua CLS
- [ ] tao don thuoc va phat thuoc
- [ ] xac nhan thanh toan
- [ ] generate va su dung VietQR
- [ ] nhanh `Thu sau`
- [ ] thong bao
- [ ] report visibility theo role

### 4. Verify Data Scope

- [ ] dashboard scope
- [ ] queue scope
- [ ] patients scope
- [ ] history scope
- [ ] clinical scope
- [ ] CLS scope
- [ ] prescription/pharmacy scope

### 5. Seed Va Runtime Confidence

- [ ] verify account admin
- [ ] verify account YTHC
- [ ] verify account Bac si
- [ ] verify account Y ta LS
- [ ] verify account Y ta CLS
- [ ] verify account KTV
- [ ] verify lich truc/phong thuc su chi phoi queue visibility dung

### 6. Khoa Tai Lieu

- [ ] sync `rbac_matrix.md` voi runtime behavior that
- [ ] sync docs Week 5 role/UAT voi code hien tai
- [ ] danh dau cac historical docs khong con la source of truth
- [ ] remove hoac label cac legacy pages/components khong con la runtime-primary

### 7. Chuan Bi Demo/Release

- [ ] chuan bi smoke-test script
- [ ] chuan bi demo accounts
- [ ] chuan bi cac buoc reset demo data
- [ ] chuan bi known-issues list neu van con warning non-blocking

---

## Rui Ro Con Lai Nhung Khong Block Week 5

Nhung muc sau can theo trong Week 5, nhung khong con co nghia Week 1-4 dang thieu implementation:

- warning chunk-size tu Vite
- warning pure-comment cua SignalR trong build output
- co the con mot so file legacy ton tai nhung khong con la runtime-primary
- runtime behavior van can verify bang account that sau restart va reseed

---

## Dieu Kien Dong Week 5

Week 5 co the chot cuoi khi tat ca dieu sau dung:

1. Moi role muc tieu deu pass manual regression tren cac trang du kien.
2. Data scope khop voi RBAC matrix trong runtime, khong chi tren source.
3. Payment va VietQR duoc verify end-to-end.
4. Account seeded va lich truc phuc vu dung cho test/demo.
5. Docs, UAT checklists, va role matrix deu khop voi behavior thuc te cua code.

---

## Phat Bieu Handoff Cuoi

Week 1-4 nen duoc xem la phan implementation da hoan tat.  
Week 5 nen duoc xem la phase:

- regression
- UAT
- verify role
- chuan bi demo
- khoa tai lieu va chot do tin cay release
