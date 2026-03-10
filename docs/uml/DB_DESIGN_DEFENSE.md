# Giai thich Thiet ke Database - HealthCare+
# (Tai lieu chuan bi bao ve Do an)

---

## 1. TONG QUAN KIEN TRUC: POLYGLOT PERSISTENCE

### 1.1 Polyglot Persistence la gi?

La kien truc su dung **nhieu loai Database** trong cung 1 he thong, moi loai DB duoc chon
dua tren **the manh rieng** phu hop voi tung loai du lieu.

```
 HealthCare+ System
 ┌─────────────────────────────────────────────────┐
 │                                                 │
 │  ┌──────────────────┐  ┌──────────────────────┐ │
 │  │     MySQL         │  │      MongoDB         │ │
 │  │  (Relational DB)  │  │   (Document DB)      │ │
 │  │                   │  │                      │ │
 │  │  - Benh nhan      │  │  - Lich su kham      │ │
 │  │  - Lich hen       │  │    (medical_histories│ │
 │  │  - Hoa don        │  │  - Audit logs        │ │
 │  │  - Kho thuoc      │  │                      │ │
 │  │  - Hang doi       │  │                      │ │
 │  │  - Thong bao      │  │                      │ │
 │  └──────────────────┘  └──────────────────────┘ │
 │         ACID                Schema Evolution    │
 │     Foreign Key              Flexible JSON      │
 │     Transaction              High Write Speed   │
 └─────────────────────────────────────────────────┘
```

### 1.2 Tai sao khong dung 1 DB duy nhat?

**Neu chi dung MySQL:**
- Lich su kham benh co hang chuc loai su kien khac nhau
  (kham noi, kham mat, xet nghiem mau, sieu am, X-quang...)
- Moi loai co cac truong du lieu HOAN TOAN KHAC NHAU
- Phai tao hang chuc bang hoac 1 bang co hang tram cot NULL
- Khi them loai kham moi -> phai ALTER TABLE (downtime)

**Neu chi dung MongoDB:**
- Khong co Foreign Key -> khong dam bao tinh nhat quan (Consistency)
- Khong co ACID Transaction tot -> nguy hiem cho Hoa don, Thanh toan
- Khong co JOIN -> query lien ket giua Benh nhan - Lich hen - Hoa don rat cham

**Giai phap: Dung CA HAI**
- MySQL: Cho du lieu can **ACID, Consistency, Relationship**
- MongoDB: Cho du lieu can **Flexibility, Schema Evolution**

---

## 2. TAI SAO LICH SU KHAM LUU TRONG MONGODB?

### 2.1 Van de voi MySQL

Moi lan kham benh, du lieu rat khac nhau:

| Kham Noi khoa | Xet nghiem mau | Sieu am |
|---|---|---|
| huyet_ap | RBC | hinh_anh_url |
| nhip_tim | WBC | mo_ta_hinh_anh |
| nhiet_do | HGB | ket_luan |
| chan_doan | Glucose | |
| | Cholesterol | |

Neu dung MySQL:
- **Cach 1:** Tao nhieu bang (kham_noi, xet_nghiem, sieu_am...) -> 10+ bang chi cho lich su
- **Cach 2:** 1 bang chung voi 100+ cot, hau het la NULL -> lang phi bo nho
- **Cach 3:** Dung JSON column trong MySQL -> mat loi the cua Relational

### 2.2 Giai phap MongoDB

Chi can **1 Collection duy nhat**: `medical_histories`

```json
// Lan kham 1: Kham noi khoa
{
  "patient_id": "BN001",
  "event_type": "kham_lam_sang",
  "event_date": "2024-01-15",
  "data": {
    "huyet_ap": "120/80",
    "nhip_tim": 72,
    "chan_doan": "Viem hong cap"
  }
}

// Lan kham 2: Xet nghiem mau (cau truc HOAN TOAN KHAC)
{
  "patient_id": "BN001",
  "event_type": "xet_nghiem",
  "event_date": "2024-02-20",
  "data": {
    "chi_so": [
      {"ten": "Glucose", "gia_tri": 95, "don_vi": "mg/dL"},
      {"ten": "WBC", "gia_tri": 8.5, "don_vi": "K/uL"}
    ],
    "ket_luan": "Binh thuong"
  }
}
```

**Loi ich:**
1. **Schema Evolution**: Them loai kham moi (VD: "tiem_vac_xin") ma KHONG CAN thay doi
   cau truc DB, khong can migration, khong can downtime
2. **Gon nhe**: Khong co cot NULL thua
3. **Toc do ghi nhanh**: MongoDB duoc toi uu cho viec Insert nhanh
4. **Aggregation Pipeline**: Query thong ke manh me (dem so lan bat thuong, thong ke benh...)

### 2.3 Cau hoi phong ve

**Hoi: "Tai sao khong dung JSON column cua MySQL 8.0?"**

Tra loi:
> MySQL JSON column van phai luu trong engine InnoDB, khong co index tren nested field
> hieu qua. MongoDB co native index tren bat ky nested field nao.
> Ngoai ra, MongoDB co Aggregation Pipeline ($unwind, $group, $match) de thong ke
> tren du lieu JSON, MySQL khong co tuong duong.

---

## 3. BANG HANG DOI vs LUOT KHAM (Cau hoi kho nhat)

### 3.1 Cau hoi thuong gap

> "Tai sao can 2 bang? 1 lan vao = 1 luot kham, gop lai 1 bang duoc khong?"
> "Hang doi nen dung RabbitMQ/Kafka, sao lai luu vao Database?"

### 3.2 Tra loi: Tai sao KHONG dung RabbitMQ/Kafka

| Dac tinh | Database (MySQL) | RabbitMQ | Kafka |
|---|---|---|---|
| Tim kiem BN | SQL WHERE | KHONG THE | KHONG THE |
| Sap xep lai thu tu | UPDATE Priority | Khong the | Khong the |
| Hien thi len TV/Dashboard | SELECT truc tiep | Phai consume ra truoc | Phai consume ra truoc |
| Luu vet lich su | Co san | Mat khi consume | Kho query |
| Thay doi uu tien dong | UPDATE 1 dong | Khong the | Khong the |

**Ket luan:** RabbitMQ/Kafka dung de giao tiep GIUA CAC HE THONG (System-to-System).
Hang doi benh vien la bai toan QUAN LY TRANG THAI (Human Workflow) can:
- Hien thi danh sach cho Y ta/Bac si xem
- Tim kiem benh nhan theo ten/ma
- Sap xep lai uu tien (cap cuu len dau)
- Luu lich su de bao cao

### 3.3 Tra loi: Tai sao tach 2 bang (HangDoi vs LuotKham)

**Nguyen tac: Single Responsibility (Tach biet moi quan tam)**

```
 BENH NHAN DEN        BENH NHAN VAO PHONG      BENH NHAN VE
      |                      |                      |
      v                      v                      v
 ┌──────────┐          ┌──────────┐
 │ HANG DOI │ -------> │ LUOT KHAM│
 │          │  1 : 1   │          │
 │ Ai den?  │          │ Kham gi? │
 │ Thu tu?  │          │ Ket qua? │
 │ Uu tien? │          │ Bac si?  │
 │ Vang mat?│          │ Sinh hieu│
 └──────────┘          └──────────┘
  LOGISTICS             CLINICAL
  (Van hanh)            (Lam sang)
```

**3 ly do chinh:**

**Ly do 1: Vong doi khac nhau**
- HangDoi: Ket thuc khi benh nhan BUOC VAO phong
- LuotKham: Bat dau khi benh nhan BUOC VAO phong
- 1 benh nhan co the vao Hang doi nhung KHONG CO Luot kham (bo ve, vang mat)

**Ly do 2: Du lieu khac nhau**
- HangDoi: SoThuTu, DoUuTien, ThoiGianCho, SoLanGoi (KHONG LIEN QUAN y te)
- LuotKham: SinhHieu, BacSiKham, ThoiGianKham (DU LIEU Y TE)
- Neu gop: 50% cot se la NULL tai moi thoi diem

**Ly do 3: Hieu nang (Performance)**
- HangDoi: Bi READ/WRITE lien tuc (cap nhat man hinh cho moi 5 giay)
- LuotKham: Bi WRITE khi bac si dien benh an (transaction dai)
- Tach ra -> viec Y ta goi benh nhan KHONG LOCK bang ma Bac si dang ghi benh an

---

## 4. CAC CAU HOI KHO KHAC VA CACH TRA LOI

### 4.1 "Tai sao bang BenhNhan co truong MaCha, MaMe? Khong nen tao bang rieng?"

**Tra loi:**
> Em dung truong MaCha/MaMe (Self-referencing FK) de ho tro truy van
> **Recursive CTE** trong MySQL. Day la yeu cau cua de tai (Genealogy Tree).
> Neu tao bang rieng (VD: QuanHePhaHe), se phai JOIN them 1 bang nua
> khi truy van cay pha he, lam tang do phuc tap.
> Cach cua em cho phep dung 1 cau SQL Recursive CTE de lay toan bo
> to tien/con chau cua 1 benh nhan.

```sql
WITH RECURSIVE PhaHe AS (
  SELECT * FROM BenhNhan WHERE MaBenhNhan = ?
  UNION ALL
  SELECT bn.* FROM BenhNhan bn
  JOIN PhaHe ph ON bn.MaBenhNhan = ph.MaCha
     OR bn.MaBenhNhan = ph.MaMe
)
SELECT * FROM PhaHe;
```

---

### 4.2 "Tai sao PhieuKham va PhieuChanDoan tach rieng? Gop lai duoc khong?"

**Tra loi:**
> 2 phieu nay co **VONG DOI KHAC NHAU**:
> - PhieuKham: Duoc tao boi Y ta HC khi **tiep nhan** benh nhan (truoc khi kham)
> - PhieuChanDoan: Duoc tao boi Bac si **SAU KHI kham xong**
>
> Neu gop: Y ta se phai tao 1 ban ghi co cac truong ChanDoan, HuongXuTri... la NULL,
> roi Bac si moi cap nhat sau -> Vi pham nguyen tac "Don't store NULL unnecessarily"
>
> Ngoai ra, 1 PhieuKham co the KHONG CO PhieuChanDoan (neu benh nhan bo ve giua chung)

---

### 4.3 "Tai sao HoaDon co 3 FK (MaPhieuKham, MaPhieuKhamCls, MaDonThuoc)?"

**Tra loi:**
> Vi he thong co 3 THOI DIEM THU TIEN khac nhau trong 1 luot kham:
> 1. **Thu phi kham** (khi tiep nhan) -> lien ket voi PhieuKham
> 2. **Thu phi CLS** (khi chi dinh xet nghiem/sieu am) -> lien ket voi PhieuKhamCls
> 3. **Thu tien thuoc** (khi ke don) -> lien ket voi DonThuoc
>
> Moi hoa don chi lien ket voi 1 trong 3 (2 FK con lai la NULL).
> Day la mo hinh **Polymorphic Association** de tranh phai tao 3 bang hoa don rieng.

---

### 4.4 "Tai sao dung VietQR ma khong dung Stripe/PayPal?"

**Tra loi:**
> He thong nay la phong kham Viet Nam, benh nhan thanh toan bang
> chuyen khoan ngan hang noi dia. VietQR la tieu chuan QR cua
> Ngan hang Nha nuoc Viet Nam, ho tro 40+ ngan hang.
> Stripe/PayPal khong pho bien va mat phi cao hon o Viet Nam.

---

### 4.5 "Du lieu MongoDB va MySQL co bi mat dong bo khong?"

**Tra loi:**
> MongoDB chi luu **BAN SAO (Snapshot)** cua du lieu lich su kham.
> Du lieu GOC van nam trong MySQL (PhieuKham, KetQuaDichVu, DonThuoc...).
>
> Khi Bac si hoan tat 1 luot kham, he thong tu dong:
> 1. Luu vao MySQL (du lieu goc, co Foreign Key)
> 2. Tao 1 document vao MongoDB (ban sao de tra cuu nhanh)
>
> Neu MongoDB mat du lieu -> Co the tai tao tu MySQL.
> Neu MySQL mat du lieu -> Day la su co nghiem trong (can backup/restore).
>
> 2 database khong can dong bo 2 chieu (One-way sync: MySQL -> MongoDB).

---

### 4.6 "Bang ThongBaoMau de lam gi? Sao khong hardcode template?"

**Tra loi:**
> ThongBaoMau cho phep Admin **tuy chinh noi dung thong bao** qua giao dien
> ma khong can thay doi code. Vi du:
> - Mau "Nhac lich hen": "Kinh gui {{TenBN}}, ban co lich hen ngay {{NgayHen}}..."
> - Admin co the sua loi chao, noi dung, ma khong can Developer can thiep.
>
> Neu hardcode: Moi lan doi 1 chu trong thong bao phai re-deploy ung dung.

---

### 4.7 "Tai sao RefreshToken luu trong DB ma khong dung Redis?"

**Tra loi:**
> Redis la lua chon tot hon ve hieu nang, nhung:
> 1. Tang do phuc tap he thong (them 1 service nua de quan ly)
> 2. Doi voi quy mo phong kham (~100-500 nguoi dung), MySQL du nhanh
> 3. Luu trong MySQL cho phep truy van lich su dang nhap, revoke token
>    theo IP address (bao mat)
>
> Neu mo rong len benh vien lon (1000+ nguoi dung dong thoi),
> co the chuyen sang Redis de toi uu.

---

### 4.8 "LichSuXuatKho co can thiet khong? Admin them thuoc bang tay ma"

**Tra loi:**
> Bang nay KHONG phuc vu cho Admin them thuoc.
> No tu dong ghi lai moi lan **XUAT THUOC** khi phat don cho benh nhan.
> Muc dich:
> 1. **Truy vet**: Biet chinh xac thuoc nao xuat cho benh nhan nao, khi nao
> 2. **Hoan tra**: Khi huy hoa don, he thong biet can cong lai bao nhieu vien
> 3. **Kiem ke**: So sanh SoLuongTon voi tong xuat de phat hien sai lech
>
> Ve UI: Viec co hay khong co giao dien la lua chon thiet ke, khong phai mac dinh.
> - Neu co UI: Admin xem duoc lich su xuat/nhap chi tiet de doi chieu, kiem ke
> - Neu khong co UI: No van chay ngam o Backend, phuc vu hoan tra va nhat quan kho
> Ca 2 phuong an deu hop ly, tuy theo do phuc tap mong muon cua he thong.

---

### 4.9 "PhieuTongHop co bi TRUNG voi MongoDB khong? Du lieu MongoDB da phong phu hon roi"

**Tra loi:**
> Ban dau, PhieuTongHop co cot `SnapshotJson` luu lai tat ca ket qua CLS.
> Nhung khi da co MongoDB luu chi tiet, SnapshotJson TRO NEN DU THUA.
>
> **Giai phap:** Em da **XOA SnapshotJson** khoi PhieuTongHop.
> Gio PhieuTongHop chi con la **WORKFLOW TRIGGER** (cai "den bao" trong quy trinh):

**PhieuTongHop (MySQL) - SAU KHI TINH GON:**
- Chi luu: `TrangThai` (DangThucHien -> ChoXuLy -> DaHoanTat)
- Chi luu: `MaPhieuKhamCls` (FK), `MaNhanSuXuLy` (FK), `ThoiGian*`
- **KHONG luu chi tiet ket qua** (da chuyen sang MongoDB)
- Muc dich: Bao hieu "TAT CA ket qua CLS da xong" -> day BN quay lai Bac si

**medical_histories (MongoDB) = NOI LUU DU LIEU THUC SU:**
- Luu toan bo chi so chi tiet (Glucose, WBC, hinh anh sieu am...)
- Schema Evolution: Them loai kham moi khong can migration
- Aggregation Pipeline: Thong ke, bao cao manh me

**HYBRID DATA FLOW (Cach lay du lieu khi hien thi):**

```
 Bac si mo "Phieu Tong Hop CLS":

  Frontend: GET /api/visits/{id}/cls-summary
       |
       v
  Backend (Controller):
       |
       |-- [1] Query MySQL (PhieuTongHop)
       |       -> Lay: TrangThai, ThoiGian, NhanSuXuLy
       |
       |-- [2] Query MongoDB (medical_histories) 
       |       -> Lay: Chi tiet ket qua XN, Sieu am, X-quang...
       |
       |-- [3] Ghep (Map) 2 nguon du lieu thanh 1 DTO
       |
       v
  Frontend: Hien thi day du (nguoi dung khong thay khac biet)
```

> **Tom lai:**
> - PhieuTongHop la "DEN BAO" trong quy trinh - khi xong thi tat di
> - MongoDB la "HO SO BENH AN" vinh vien - ghi nhan moi thu de tra cuu sau
> - Backend lam nhiem vu AGGREGATE (gop) du lieu tu ca 2 nguon
> - Nguoi dung KHONG CAN BIET du lieu den tu dau

---

### 4.10 "Bang KetQuaDichVu (MySQL) co bi thua, bi Null nhieu khong?"

**Tra loi:**
> Hoan toan KHONG.
> Tuong tu PhieuTongHop, em da **XOA cot `NoiDungKetQua`** (JSON) khoi bang nay.
> Gio bang `KetQuaDichVu` chi con la **METADATA** (muc luc), cuc ky gon nhe:

| Truong | Muc dich | Giu/Xoa |
|---|---|---|
| `LoaiKetQua` | Phan loai: XetNghiem hay HinhAnh | **Giu** |
| `KetLuanChuyen` | Ket luan tom tat: "Binh thuong" | **Giu** |
| `TepDinhKem` | Link file PDF/Anh | **Giu** |
| `TrangThaiChot` | Nhap -> DaChot -> HoanTat | **Giu** |
| ~~`NoiDungKetQua`~~ | ~~Chi so chi tiet (RBC, WBC...)~~ | **DA XOA** -> MongoDB |

> **Phan chia trach nhiem:**
> - **MySQL** = "Muc Luc" & "Trang Thai": Biet la DA CO ket qua chua, ket luan gi, file o dau
> - **MongoDB** = "Noi Dung Chi Tiet": Luu toan bo chi so (RBC=5.0, Glucose=95...)
>
> **Loi ich:**
> 1. **Khong NULL thua**: MySQL khong can hang tram cot cho moi loai xet nghiem
> 2. **Toc do**: MySQL nhe -> query nhanh cho Workflow. MongoDB toi uu cho Read lich su
> 3. **Schema Evolution**: Them loai XN moi chi can them event_type moi ben MongoDB
-> **Ket luan:** MySQL dam nhan quan ly **Quy trinh (Workflow)**, MongoDB dam nhan luu tru **Du lieu Chuyen mon (Clinical Data)**. Khong co su trung lap ve du lieu chi tiet.

---

### 4.11 "Tai sao MongoDB da luu event 'DonThuoc', 'HoaDon' roi ma van giu bang SQL? Co bi du thua (duplicate) va phinh to DB khong?"

**Tra loi:**
> Day la thiet ke **CQRS (Command Query Responsibility Segregation)** - Tach biet Ghi va Doc.
> Viec giu ca 2 la **BAT BUOC** de dam bao Tinh toan ven du lieu (Integrity) va Hieu nang (Performance).

#### 1. Tai sao KHÔNG THỂ bo bang SQL (DonThuoc, HoaDon)?
MongoDB luu du lieu dang **Lich su (Historical View)**, con SQL quan ly **Van hanh (Operational View)**.
Neu bo SQL, he thong se chet o cac nghiep vu sau:
- **Kho thuoc & Ton kho**: Khi ke don, phai tru ton kho ngay lap tuc (Transaction ACID). MongoDB khong dam bao duoc viec "giu ghe" (lock row) so luong ton chuan xac khi co 100 bac si ke don cung luc.
- **Tinh tien & Doanh thu**: Can `SUM(SoTien)` chinh xac tuyet doi. SQL la trum ve aggregation so hoc chat che.
- **Rang buoc gia**: Gia thuoc thay doi hang ngay. SQL luu gia tai thoi diem ke (Snapshot gia).

#### 2. Van de Du thua & Phinh to (Bloat)?
> **Thuc te:** Kien truc nay chong phinh to hieu qua hon la chi dung 1 DB.

- **MySQL rat GON (Lean)**: Vi da day het noi dung chi tiet (JSON kham, Kq xet nghiem...) sang MongoDB, nen bang SQL chi con lai cac cot "xuong song" (ID, Ngay, TrangThai, TongTien).
  -> Mot bang `HoaDon` co the chua hang chuc trieu dong ma van nhanh.
- **MongoDB ganh tai READ**: Lich su kham benh 10 nam truoc -> Doc tu MongoDB. MySQL khong bi nang ganh boi du lieu cu.

#### 3. Co cach nao toi uu hon khong?
Dung, co phuong an **ARCHIVING (Luu tru)**:
- **Buoc 1**: Giu du lieu trong MySQL khoang 6 thang - 1 nam (Hot Data) de phuc vu sua chua, thanh toan, doi tra.
- **Buoc 2**: Sau khi ho so ket thuc (Don thuoc da phat, Hoa don da chot so), **MOVE** (chuyen han) du lieu sang MongoDB hoac Data Warehouse.
- **Buoc 3**: **XOA** du lieu trong MySQL.


> **Tuy nhien:** Voi quy mo hien tai, viec giu ca 2 (MySQL nhe + MongoDB full) la phuong an an toan va de trien khai nhat. No giup bao cao doanh thu (tu MySQL) va bao cao chuyen mon (tu MongoDB) chay doc lap, khong lam cham he thong.

#### 4. "Tai sao cac thuoc tinh giong het nhau? SQL co phai de Read khong?"
**Tra loi:**
> Dung la tai thoi diem ghi, du lieu la **GIONG HET NHAU**.
> Nhung muc dich su dung la **KHAC NHAU**:
>
> 1. **SQL la de GHI (Write-Intensive) & KIEM TRA (Validate)**:
>    - Khi ke don thuoc, SQL phai kiem tra ton kho, gia tien, va khoa (lock) hang de tru kho.
>    - Neu dung MongoDB, viec nay cuc kho va de sai sot (Race condition).
>
> 2. **MongoDB la de DOC (Read-Intensive)**:
>    - Khi xem lai lich su kham: Chi can query 1 document MongoDB la co het (Khong can Join 5-6 bang SQL).
>    - Neu dung SQL de doc lich su: Query cham, anh huong den nguoi dang ke don moi.
>
> -> **Tom lai:**
> - SQL giong het MongoDB ve **NOI DUNG**, nhung khac ve **NGU CANH SU DUNG**.
> - SQL = "So sach ke toan" (Ghi chep tung dong, chinh xac tung xu).
> - MongoDB = "Ho so luu tru" (Dong goi tung tap, de tra cuu).

---

### 4.12 "Tai sao kiem tra o ca BE lan DB? Code-first co du roi khong?"

**Tra loi:**
> He thong su dung chien luoc **"Defense in Depth"** (Phong thu nhieu tang):
> - **Tang 1 — Backend (C# Service):** Kiem tra **TRUOC** de toi uu hieu nang va UX.
>   Nguoi dung nhan phan hoi nhanh (400 Bad Request) MA KHONG CAN truy van DB.
> - **Tang 2 — Database (Stored Procedure / CHECK / TRIGGER):** Kiem tra **LAN CUOI**
>   de dam bao tinh dung dan **TUYET DOI** cua du lieu (Data Integrity),
>   ke ca khi co loi phia Backend hoac khi co ai do goi thang DB.

#### Vi sao can CA HAI tang?

```
 Nguoi dung bam nut "Dat lich"
       |
       v
 ┌─────────────────────────────────────────────┐
 │  TANG 1: Backend (C# AppointmentService)    │
 │                                             │
 │  ✓ Kiem tra: Ngay hen >= hom nay?            │
 │  ✓ Kiem tra: Gio nam trong gio lam viec?     │
 │  ✓ Kiem tra: Benh nhan da co lich chua?      │
 │  ✓ Kiem tra: Bac si da kin slot chua?         │
 │                                             │
 │  → Neu SAI: Tra 400 NGAY LAP TUC            │
 │    (Nhanh, UX tot, khong ton DB connection)  │
 │                                             │
 │  → Neu DUNG: Goi tiep xuong DB ↓            │
 └─────────────────────────────────────────────┘
       |
       v
 ┌─────────────────────────────────────────────┐
 │  TANG 2: Database (sp_BookAppointment)      │
 │  SERIALIZABLE Transaction                   │
 │                                             │
 │  ✓ LOCK row benh nhan + lich hen            │
 │  ✓ Kiem tra trung lich LAN CUOI trong DB     │
 │  ✓ INSERT lich hen                           │
 │  ✓ COMMIT hoac ROLLBACK                     │
 │                                             │
 │  → Dam bao: Du 100 request cung luc,         │
 │    KHONG BAO GIO co 2 lich hen trung nhau   │
 └─────────────────────────────────────────────┘
```

#### Tai sao KHONG THE bo tang nao?

| Tinh huong | Chi co BE | Chi co DB | Co CA HAI ✅ |
|---|---|---|---|
| 2 request dat lich **cung luc** (Race Condition) | ❌ BE check OK ca 2 → **TRUNG LICH** | ✅ DB lock row → chi 1 thanh cong | ✅ |
| Nguoi dung **nhap sai** (date qua khu) | ✅ BE tra 400 ngay | ❌ DB phai ton connection + parse date | ✅ BE tra 400 ngay |
| Developer/DBA **chay SQL truc tiep** (bypass BE) | ❌ Khong ai kiem tra | ✅ DB tu check | ✅ DB tu check |
| **99% request binh thuong** (khong conflict) | ✅ Nhanh, khong ton DB | ❌ Ton DB connection moi request | ✅ BE loc truoc, DB chi xu ly cac case hiem |

#### Ap dung cho TUNG module:

| Module | Tang 1: BE (Code-first) | Tang 2: DB |
|---|---|---|
| **Dat lich hen** | `AppointmentService.FindConflicts()` — kiem tra trung lich | `sp_BookAppointment` — SERIALIZABLE, lock + check + insert |
| **Xuat kho thuoc** | `PharmacyService.XuatThuocAsync()` — kiem tra ton kho >= so luong | `CHECK (SoLuong >= 0)` + `TRIGGER tr_CheckStock` — khong cho SoLuong am |
| **Chuyen trang thai** | `ClinicalService/AppointmentService` — validate transition (VD: chi cho dang_cho→da_xac_nhan) | `TRIGGER tr_ValidateTransition` — kiem tra `OLD.TrangThai` → `NEW.TrangThai` hop le |
| **Xoa benh nhan** | `PatientService` — kiem tra co lich hen active khong | `FK ON DELETE RESTRICT` — MySQL tu chan xoa neu con lich hen tham chieu |

#### Ket luan:

> **Tang 1 (BE)** mang lai **Hieu nang + UX tot**: Phan hoi nhanh, giam tai cho DB.
> **Tang 2 (DB)** mang lai **Tinh dung dan tuyet doi**: Ke ca khi BE bi loi hoac bi bypass.
>
> Day la nguyen tac bao mat noi tieng trong Software Engineering:
> "**Never trust the client (or the layer above you)**."
> DB la lop cuoi cung — no KHONG BAO GIO tin BE da kiem tra dung.
>
> Doi voi **do an nay**, yeu cau mon hoc bat buoc dung **Stored Procedure, CHECK Constraint, TRIGGER**
> nen viec bo sung validation o tang DB la BAT BUOC. Nhung em VAN GIU code-first o BE
> de khong mat di cac loi the ve Performance va Developer Experience.

---

## 5. TOM TAT: BANG PHAN CHIA DU LIEU

| Du lieu | Database | Ly do |
|---|---|---|
| Benh nhan, Nhan su | MySQL | Can UNIQUE constraint, Foreign Key |
| Lich hen, Lich truc | MySQL | Can SERIALIZABLE Transaction (tranh trung lich) |
| Hang doi, Luot kham | MySQL | Can Query phuc tap, sap xep uu tien |
| Hoa don, Don thuoc | MySQL | Can ACID (tien bac khong duoc sai) |
| Thong bao | MySQL | Can Foreign Key lien ket voi Benh nhan/Nhan su |
| Lich su kham benh | **MongoDB** | Schema Evolution, da dang loai su kien |
| Audit Logs | **MongoDB** | Ghi nhanh, du lieu lon, TTL tu dong xoa |

---

## 6. CAU CHOT KHI BAO VE

> "He thong cua em su dung kien truc Polyglot Persistence,
> ket hop MySQL va MongoDB. MySQL dam nhan toan bo du lieu
> giao dich (Transactional) can tinh nhat quan cao nhu
> Hoa don, Lich hen, Kho thuoc. MongoDB dam nhan du lieu
> Lich su kham benh vi no cho phep Schema Evolution -
> moi loai kham (noi khoa, xet nghiem, sieu am) co cau truc
> du lieu hoan toan khac nhau ma khong can ALTER TABLE.
>
> Ngoai ra, em ap dung nguyen tac **Defense in Depth**:
> Moi nghiep vu quan trong duoc kiem tra o **CA 2 TANG** —
> Backend kiem tra truoc de toi uu hieu nang va UX,
> Database kiem tra lan cuoi bang Stored Procedure,
> CHECK Constraint va TRIGGER de dam bao Data Integrity tuyet doi.
> Dieu nay giup he thong vua nhanh, vua an toan,
> va de dang mo rong khi them chuyen khoa moi
> ma khong anh huong den cac module dang hoat dong."
