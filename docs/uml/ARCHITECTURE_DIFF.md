# SO SANH KIEN TRUC: DU AN HIEN TAI vs DU AN THONG THUONG

Tai lieu nay giup bao ve do an tot nghiep bang cach chi ro nhung diem **NANG CAO (Advanced)** ma ban da them vao so voi mot do an CRUD co ban.

---

## 1. Tong quan Kien truc

| Tieu chi | Do an CRUD Co ban (Standard) | HealthCare+ (Advanced Monolith) |
|---|---|---|
| **Kieu kien truc** | Monolith (Controller goi truc tiep Repository) | **Layered Architecture (Clean)**: Tach biet Controller -> Service -> Repository. De dang Unit Test va mo rong. |
| **Giao tiep** | Dong bo (REST API) | **Hybrid**: REST API + **Real-time (SignalR)** + **Background Jobs**. |
| **User Experience** | Phai F5 de thay du lieu moi | **Live Updates**: Tu dong cap nhat Hang doi, Thong bao ma khong can tai lai trang. |

## 2. Co so Du lieu (Database)

| Tieu chi | Do an CRUD Co ban | HealthCare+ (Polyglot Persistence) |
|---|---|---|
| **Luu tru** | 100% MySQL (Quan he) | **MySQL + MongoDB**: Ket hop suc manh cua ca hai. |
| **Luu lich su kham** | Kho khan do cau truc dong (moi BS kham 1 kieu) -> phai tao nhieu cot NULL hoac TEXT dai. | **MongoDB (Schema-less)**: Luu linh hoat moi loai phieu kham ma khong can sua bang. |
| **Luu Audit Log** | It khi lam, hoac luu file text | **MongoDB**: Luu vet tac dong (Ai lam gi, luc nao) cuc nhanh, khong anh huong hieu nang chinh. |
| **Ton kho** | Tru thang vao bang Thuoc (de bi Lock) | **Transactional**: Dung Transaction SERIALIZABLE trong MySQL de dam bao khong sai lech 1 vien thuoc nao. |

## 3. Tinh nang Real-time (Thoi gian thuc)

| Tieu chi | Do an CRUD Co ban | HealthCare+ (SignalR) |
|---|---|---|
| **Hang doi kham** | Y ta phai bam "Lam moi" lien tuc de xem ai vua Check-in. | **Tu dong day (Push)**: Benh nhan vua check-in -> Man hinh Bac si tu nhay so ngay lap tuc. |
| **Thong bao** | Khong co hoac gui Email cham | **Instant Notification**: Thong bao "Bac si da ke don xong" hien ngay len App benh nhan. |

## 4. Bao mat & Hieu nang

| Tieu chi | Do an CRUD Co ban | HealthCare+ (Secure & Scalable) |
|---|---|---|
| **Auth** | Session hoac Simple Token | **JWT (Access + Refresh Token)**: Chuan bao mat hien dai, ho tro dang nhap lau dai an toan. |
| **Cache (Neu co)** | Khong co | **Memory Cache**: Luu cac danh muc (Khoa, Phong, Thuoc) giup API nhanh hon gap doi. |
| **Xu ly tac vu nang** | Gui Email/Bao cao lam treo request (Loading...) | **Background Job**: Xu ly ngam, tra ve ket qua ngay cho nguoi dung (Fire-and-Forget). |

---

## 5. DevOps & Trien khai (Deployment)

| Tieu chi | Do an CRUD Co ban | HealthCare+ (Modern DevOps) |
|---|---|---|
| **Moi truong** | Cai truc tiep len Windows (XAMPP/IIS) -> "May to chay, may thay khong chay". | **Docker Container**: Dong goi moi truong (App + Db) trong Container -> Chay on dinh tren moi may. |
| **Database** | Localhost (vong doi ngan) | **Cloud Database (MongoDB Atlas)**: Ket noi Database tren may chu that, sat voi thuc te trien khai. |
| **Log** | File text hoac Console | **Centralized Logging (Seq)**: Quan ly log tap trung, de dang tra cuu loi (Bug tracing). |

---

## 6. KET LUAN: CO BI "QUA MUC" (OVER-ENGINEERING) KHONG?

**Cau tra loi: KHONG.**

Du an HEALTHCARE+ van giu kien truc **MONOLITH** (mot khoi duy nhat) de de trien khai, de cham diem. 
Tuy nhien, no tich hop cac cong nghe hien dai (MongoDB, SignalR) de giai quyet cac bai toan thuc te:
1.  **Du lieu y te rat phuc tap**: SQL khong du linh hoat -> Can MongoDB.
2.  **Benh vien can toc do**: Khong the F5 lien tuc -> Can SignalR.

-> Day la diem cong rat lon ve **CONG NGHE** va **TUDUY GIAI QUYET VAN DE**.
