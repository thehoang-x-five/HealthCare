# Kịch bản thuyết trình: Kiến trúc Database HealthCare+

Tài liệu này dùng để thuyết trình phần thiết kế cơ sở dữ liệu theo kiến trúc **Polyglot Persistence**: MySQL giữ dữ liệu giao dịch có ràng buộc mạnh, MongoDB giữ lịch sử y khoa linh hoạt và phục vụ đọc/analytics.

---

## Phần 1: Mở đầu và vấn đề thiết kế

**Người nói:**
"Kính thưa Hội đồng, khi thiết kế HealthCare+, nhóm gặp một bài toán rất thực tế: không phải dữ liệu nào trong hệ thống y tế cũng có cùng đặc tính.

Thứ nhất, dữ liệu vận hành như lịch hẹn, hàng đợi, hóa đơn, tồn kho và phân quyền cần tính chính xác cao, có khóa ngoại, ràng buộc và transaction ACID.

Thứ hai, dữ liệu bệnh án như kết quả khám, xét nghiệm, chẩn đoán hình ảnh, đơn thuốc và thanh toán lại phát triển liên tục theo nghiệp vụ. Nếu ép toàn bộ vào bảng quan hệ, hệ thống sẽ phình to, nhiều cột JSON/NULL và khó mở rộng.

Vì vậy, nhóm chọn kiến trúc Polyglot Persistence: dùng đúng loại database cho đúng loại dữ liệu."

---

## Phần 2: Vai trò của MySQL và MongoDB

**Người nói:**
"Ở tầng lưu trữ, hệ thống được chia thành hai phần rõ ràng.

**MySQL là nguồn dữ liệu vận hành chính.** MySQL lưu nhân sự, khoa phòng, lịch hẹn, hàng đợi, phiếu khám, phiếu CLS, hóa đơn, đơn thuốc và kho thuốc. Các bảng này cần khóa ngoại, constraint, trigger và transaction để đảm bảo dữ liệu không sai lệch.

**MongoDB là kho lịch sử y khoa và audit.** Collection `medical_histories` lưu các sự kiện y khoa theo dạng document linh hoạt. Một bệnh nhân có thể phát sinh nhiều loại event như `kham_lam_sang`, `xet_nghiem`, `chan_doan_hinh_anh`, `cls_order_created`, `cls_service_completed`, `tong_hop_cls`, `don_thuoc`, `thanh_toan`. Nhờ schema linh hoạt, khi bổ sung loại phiếu hoặc chỉ số mới, hệ thống không cần ALTER TABLE."

---

## Phần 3: CQRS và luồng ghi/đọc lai

**Người nói:**
"Kiến trúc này được triển khai theo hướng CQRS nhẹ.

Luồng ghi vẫn đi qua MySQL trước để đảm bảo ACID. Ví dụ với CLS, hệ thống tạo phiếu CLS, tạo chi tiết dịch vụ, chuyển hàng đợi qua từng phòng CLS, ghi kết quả và cuối cùng tạo phiếu tổng hợp trong MySQL.

Sau mỗi bước y khoa quan trọng, backend dual-write một event sang MongoDB. Nếu MongoDB tạm thời lỗi, luồng vận hành MySQL vẫn hoàn tất; MongoDB đóng vai trò read/history store và có thể tái tạo từ MySQL.

Luồng đọc bệnh án và phân tích y khoa ưu tiên đọc từ MongoDB. Khi bác sĩ xem phiếu tổng hợp CLS, backend giữ trạng thái pháp lý từ MySQL, đồng thời đưa các event y khoa từ MongoDB vào snapshot để tạo một góc nhìn đầy đủ cho bác sĩ."

---

## Phần 4: Ví dụ flow CLS

**Người nói:**
"Flow cận lâm sàng thể hiện rõ nhất kiến trúc Polyglot Persistence.

Khi bác sĩ chỉ định CLS, MySQL tạo `phieu_kham_can_lam_sang` và các dòng `chi_tiet_dich_vu`, còn MongoDB ghi event `cls_order_created`.

Khi bệnh nhân bắt đầu thực hiện CLS, MySQL tạo hàng đợi cho phòng thực hiện. KTV chỉ được xử lý dịch vụ thuộc phòng mình phụ trách. Khi KTV chốt kết quả, MySQL cập nhật trạng thái dịch vụ và hàng đợi, còn MongoDB ghi event chuyên môn như `xet_nghiem` hoặc `chan_doan_hinh_anh`, kèm event vòng đời `cls_service_completed`.

Nếu phiếu còn dịch vụ CLS khác, hệ thống tự chuyển sang phòng kế tiếp và ghi event `cls_transfer_to_next_service`. Nếu tất cả dịch vụ đã có kết quả, hệ thống tự tạo `phieu_tong_hop_ket_qua`, trả bệnh nhân về hàng đợi khám lâm sàng bằng nguồn `service_return`, đồng thời ghi event `tong_hop_cls` vào MongoDB."

---

## Phần 5: Defense in Depth

**Người nói:**
"Bên cạnh phân tách database, hệ thống có hai tầng bảo vệ.

Tầng backend kiểm tra RBAC, data scope, vai trò nhân sự và trạng thái nghiệp vụ trước khi ghi dữ liệu.

Tầng database dùng khóa ngoại, constraint, trigger và stored procedure để đảm bảo dữ liệu vẫn an toàn nếu có lỗi từ tầng ứng dụng. Ví dụ: kho thuốc không được âm, thông tin cha mẹ bệnh nhân phải hợp lệ, và các thao tác đặt lịch cần tránh race condition."

---

## Phần 6: Kết luận

**Người nói:**
"Tóm lại, HealthCare+ không chọn một database duy nhất cho mọi bài toán. MySQL đảm bảo tính đúng đắn cho luồng vận hành, còn MongoDB giúp lưu lịch sử y khoa linh hoạt, dễ mở rộng và phù hợp cho phân tích dữ liệu.

Đây là cách triển khai thực tế của Polyglot Persistence: MySQL là operational gold source, MongoDB là clinical history/read model. Hai bên đồng bộ một chiều từ MySQL sang MongoDB, giúp hệ thống vừa chính xác, vừa mở rộng tốt."

---

## Q&A nhanh

**Hỏi: Tại sao không dùng cột JSON trong MySQL cho tất cả dữ liệu y khoa?**

**Trả lời:** "MySQL JSON vẫn nằm trong database vận hành nên sẽ làm nặng hệ thống giao dịch. MongoDB phù hợp hơn cho lịch sử y khoa vì schema linh hoạt, dễ thêm event mới, và hỗ trợ aggregation tốt hơn cho phân tích."

**Hỏi: Dữ liệu MySQL và MongoDB có thể lệch nhau không?**

**Trả lời:** "Có thể có eventual consistency vì đây là one-way sync. Tuy nhiên MySQL là nguồn dữ liệu pháp lý chính. MongoDB là read/history model, nếu mất hoặc thiếu event có thể tái tạo từ MySQL."

**Hỏi: Nếu MongoDB lỗi thì hệ thống có dừng không?**

**Trả lời:** "Không. Backend bắt lỗi dual-write MongoDB và vẫn hoàn tất transaction MySQL. Điều này đảm bảo hệ thống khám chữa bệnh không bị dừng vì lỗi read/history store."
