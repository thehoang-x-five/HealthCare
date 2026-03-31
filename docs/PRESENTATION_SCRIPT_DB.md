# Kịch bản Thuyết trình: Kiến trúc Database HealthCare+

Tài liệu này cung cấp kịch bản thuyết trình từng bước, khớp với sơ đồ kiến trúc **Polyglot Persistence** của dự án.

---

## 🎙️ PHẦN 1: MỞ ĐẦU & ĐẶT VẤN ĐỀ (30 giây)

**Người nói:** 
"Kính thưa Hội đồng, khi thiết kế hệ thống HealthCare+, thách thức lớn nhất mà nhóm gặp phải là sự xung đột giữa hai loại dữ liệu: 
1. Dữ liệu **Tài chính & Quy trình** cần sự chính xác tuyệt đối (ACID).
2. Dữ liệu **Bệnh án Lâm sàng** lại cực kỳ đa dạng và biến đổi liên tục.

Nếu gộp tất cả vào một Database duy nhất, chúng ta sẽ đối mặt với tình trạng 'phình to' bảng với hàng trăm cột NULL hoặc sự chậm trễ khi truy vấn lịch sử bệnh án dài hạn. Vì vậy, chúng em đã lựa chọn kiến trúc **Polyglot Persistence** — kết hợp thế mạnh của cả MySQL và MongoDB."

---

## 🎙️ PHẦN 2: CHIẾN LƯỢC LƯU TRỮ LAI (1 phút)

**Người nói:** (Chỉ vào phần MySQL và MongoDB trên sơ đồ)

"Ở tầng lưu trữ, chúng em chia hệ thống thành hai 'bán cầu' riêng biệt:

*   **Bên phải (MySQL) — 'Sổ sách kế toán' của hệ thống:** Đảm nhận 24 bảng thuộc 6 Domain chính như Nhân sự, Hàng đợi, và Tài chính. Tại đây, tính nhất quán (Consistency) và quan hệ chặt chẽ (Foreign Key) được đặt lên hàng đầu để đảm bảo không sai sót một đồng phí hay một lịch hẹn nào.
*   **Bên trái (MongoDB) — 'Hồ sơ lưu trữ' thông minh:** Thay vì tạo hàng chục bảng lâm sàng, chúng em chỉ dùng 1 Collection duy nhất là `medical_histories`. Nhờ cơ chế **Schema Evolution**, chúng em có thể lưu trữ mọi loại sự kiện từ Xét nghiệm máu, Siêu âm đến Đơn thuốc dưới dạng JSON linh hoạt. Việc thêm một loại chuyên khoa mới chỉ mất 0 giây cấu hình DB và 0 phút downtime."

---

## 🎙️ PHẦN 3: ĐIỀU PHỐI CQRS & HYBRID FLOW (1 phút)

**Người nói:** (Chỉ vào tầng CQRS và luồng mũi tên)

"Để tối ưu hiệu năng, chúng em áp dụng mô hình **CQRS (tách biệt luồng Đọc và Ghi)**:
*   Mọi lệnh **Ghi (Write)** về giao dịch được đẩy xuống MySQL theo cơ chế Transaction chặt chẽ.
*   Việc **Đọc (Read)** lịch sử bệnh án được thực hiện trực tiếp trên MongoDB thông qua **Aggregation Pipeline**.

Một điểm sáng kỹ thuật là **Hybrid Data Flow**: Khi bác sĩ xem 'Phiếu tổng hợp', Backend sẽ lấy trạng thái từ MySQL và chi tiết lâm sàng từ MongoDB để ghép thành một DTO hoàn chỉnh. Điều này giúp hệ thống vừa giữ được tính pháp lý của dữ liệu gốc, vừa mang lại trải nghiệm truy vấn siêu tốc."

---

## 🎙️ PHẦN 4: CHIẾN LƯỢC PHÒNG THỦ CHIỀU SÂU (45 giây)

**Người nói:** (Chỉ vào lớp Defense Tier 1 và Tier 2)

"Bảo mật y tế là ưu tiên hàng đầu, vì vậy chúng em triển khai **Defense-in-Depth** với hai tầng bảo vệ:
1.  **Tier 1 (Backend):** Kiểm tra Code-first để phản hồi nhanh và giảm tải cho Database.
2.  **Tier 2 (Database Level):** Đây là chốt chặn cuối cùng. Chúng em sử dụng **Stored Procedures** với mức cô lập `SERIALIZABLE` để chống Race Condition khi đặt lịch hẹn. Các lệnh **CHECK Constraint** và **TRIGGER** đảm bảo số lượng tồn kho hay đơn giá không bao giờ bị âm, ngay cả khi có sự cố từ phía Backend."

---

## 🎙️ PHẦN 5: KẾT LUẬN & GIÁ TRỊ (15 giây)

**Người nói:**
"Tóm lại, với sự kết hợp giữa **MySQL (Chính xác)** và **MongoDB (Linh hoạt)**, HealthCare+ không chỉ là một ứng dụng quản lý, mà là một hệ thống có khả năng mở rộng không giới hạn, an toàn tuyệt đối và sẵn sàng cho các bài toán phân tích Big Data trong tương lai. Em xin cảm ơn Hội đồng đã lắng nghe!"

---

## 💡 CÁC CÂU HỎI "HÓA GIẢI" NHANH (Q&A)

> [!TIP]
> **Hỏi: "Tại sao không dùng cột JSON trong MySQL cho xong?"**
> **Trả lời:** "Dạ, cột JSON của MySQL không hỗ trợ Index trên các trường lồng nhau hiệu quả bằng MongoDB. Hơn nữa, việc đẩy gánh nặng đọc lịch sử sang MongoDB giúp MySQL rảnh tay để xử lý các giao dịch tài chính quan trọng."
>
> **Hỏi: "Dữ liệu hai bên có bị lệch nhau (Out of sync) không?"**
> **Trả lời:** "Hệ thống dùng cơ chế One-way Sync. MySQL giữ dữ liệu gốc (Gold source), MongoDB lưu bản Snapshot. Nếu MongoDB gặp sự cố, chúng em hoàn toàn có thể tái tạo lại dữ liệu từ các bảng ghi trong MySQL."
