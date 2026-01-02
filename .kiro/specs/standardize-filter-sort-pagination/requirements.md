# Requirements Document

## Introduction

Đây là yêu cầu kiểm tra và chuẩn hóa các thao tác lọc (filter), sắp xếp (sort), tìm kiếm (search) và phân trang (pagination) trong toàn bộ hệ thống HIS (Hospital Information System). Mục tiêu là đảm bảo tất cả các trang ở Frontend (FE) đều được xử lý đúng cách ở Backend (BE) để đáp ứng phân trang chuẩn và cải thiện hiệu suất.

## Glossary

- **FE (Frontend)**: Phần giao diện người dùng, được xây dựng bằng React
- **BE (Backend)**: Phần xử lý logic nghiệp vụ và dữ liệu, được xây dựng bằng ASP.NET Core
- **Filter**: Thao tác lọc dữ liệu theo các tiêu chí cụ thể
- **Sort**: Thao tác sắp xếp dữ liệu theo thứ tự tăng dần hoặc giảm dần
- **Search**: Thao tác tìm kiếm dữ liệu theo từ khóa
- **Pagination**: Thao tác phân trang dữ liệu để hiển thị theo từng trang
- **PagedResult**: Cấu trúc dữ liệu chuẩn cho kết quả phân trang, bao gồm Items, TotalItems, Page, PageSize
- **DTO (Data Transfer Object)**: Đối tượng truyền dữ liệu giữa FE và BE
- **API Endpoint**: Điểm cuối API để FE gọi đến BE

## Requirements

### Requirement 1

**User Story:** Là một developer, tôi muốn kiểm tra toàn bộ hệ thống để xác định các trang FE có sử dụng filter/sort/search/pagination, để đảm bảo tất cả đều được xử lý đúng cách ở BE.

#### Acceptance Criteria

1. WHEN kiểm tra các trang FE THEN hệ thống SHALL liệt kê tất cả các trang có sử dụng filter, sort, search hoặc pagination
2. WHEN kiểm tra các API endpoint BE THEN hệ thống SHALL xác định các endpoint đã hỗ trợ filter, sort, search và pagination
3. WHEN so sánh FE và BE THEN hệ thống SHALL xác định các trang FE chưa được xử lý đúng cách ở BE
4. WHEN tạo báo cáo THEN hệ thống SHALL tạo báo cáo chi tiết về trạng thái hiện tại của từng trang
5. WHEN xác định các trang cần cải thiện THEN hệ thống SHALL ưu tiên các trang có ảnh hưởng lớn đến hiệu suất

### Requirement 2

**User Story:** Là một developer, tôi muốn chuẩn hóa các DTO filter ở BE, để đảm bảo tất cả đều có đầy đủ các field cần thiết cho filter/sort/search/pagination.

#### Acceptance Criteria

1. WHEN kiểm tra các DTO filter THEN hệ thống SHALL xác định các DTO thiếu field Keyword, SortBy, SortDirection, Page, PageSize
2. WHEN chuẩn hóa DTO THEN hệ thống SHALL thêm các field thiếu vào DTO với giá trị mặc định phù hợp
3. WHEN chuẩn hóa PageSize THEN hệ thống SHALL đặt giá trị mặc định là 50 items cho tất cả các DTO
4. WHEN chuẩn hóa SortDirection THEN hệ thống SHALL đặt giá trị mặc định là "asc" cho tất cả các DTO
5. WHEN cập nhật DTO THEN hệ thống SHALL đảm bảo tương thích ngược với code hiện tại

### Requirement 3

**User Story:** Là một developer, tôi muốn chuẩn hóa các Service method ở BE, để đảm bảo tất cả đều xử lý đúng cách các tham số filter/sort/search/pagination.

#### Acceptance Criteria

1. WHEN kiểm tra Service method THEN hệ thống SHALL xác định các method chưa xử lý Keyword, SortBy, SortDirection
2. WHEN chuẩn hóa Service method THEN hệ thống SHALL thêm logic xử lý Keyword để tìm kiếm trong các field quan trọng
3. WHEN chuẩn hóa Service method THEN hệ thống SHALL thêm logic xử lý SortBy và SortDirection để sắp xếp dữ liệu
4. WHEN xử lý Keyword THEN hệ thống SHALL tìm kiếm không phân biệt hoa thường trong các field text
5. WHEN xử lý SortBy THEN hệ thống SHALL hỗ trợ sắp xếp theo các field phổ biến như tên, ngày, mã

### Requirement 4

**User Story:** Là một developer, tôi muốn cập nhật FE để sử dụng đúng các tham số filter/sort/search/pagination khi gọi API, để đảm bảo hiệu suất tốt nhất.

#### Acceptance Criteria

1. WHEN FE gọi API THEN hệ thống SHALL truyền đầy đủ các tham số Page, PageSize, Keyword, SortBy, SortDirection
2. WHEN FE nhận kết quả THEN hệ thống SHALL xử lý đúng cấu trúc PagedResult với Items, TotalItems, Page, PageSize
3. WHEN FE hiển thị pagination THEN hệ thống SHALL tính toán đúng số trang dựa trên TotalItems và PageSize
4. WHEN người dùng thay đổi filter THEN hệ thống SHALL reset về trang 1
5. WHEN người dùng thay đổi sort THEN hệ thống SHALL giữ nguyên trang hiện tại và áp dụng sort mới

### Requirement 5

**User Story:** Là một developer, tôi muốn kiểm tra đặc biệt tab kê thuốc trong trang khám bệnh, để đảm bảo filter/search được xử lý đúng cách.

#### Acceptance Criteria

1. WHEN kiểm tra RxPickerModal THEN hệ thống SHALL xác nhận modal đã sử dụng API search với pagination
2. WHEN kiểm tra PharmacyService THEN hệ thống SHALL xác nhận service đã xử lý Keyword để tìm kiếm thuốc
3. WHEN tìm kiếm thuốc THEN hệ thống SHALL tìm trong các field TenThuoc, MaThuoc, CongDung
4. WHEN hiển thị kết quả THEN hệ thống SHALL hiển thị pagination nếu có nhiều hơn 20 kết quả
5. WHEN người dùng chọn thuốc THEN hệ thống SHALL không gọi lại API search

### Requirement 6

**User Story:** Là một developer, tôi muốn chuẩn hóa trang Notifications, để filter/sort/search được xử lý đúng cách ở BE thay vì FE.

#### Acceptance Criteria

1. WHEN kiểm tra NotificationController THEN hệ thống SHALL xác nhận API đang dùng NotificationFilterRequest thay vì NotificationSearchFilter
2. WHEN cập nhật API THEN hệ thống SHALL chuyển sang dùng NotificationSearchFilter có đầy đủ Keyword, LoaiThongBao, MucDoUuTien, SortBy, SortDirection
3. WHEN cập nhật FE THEN hệ thống SHALL gọi API search với các tham số filter đầy đủ
4. WHEN xử lý Keyword THEN hệ thống SHALL tìm kiếm trong TieuDe (title) và NoiDung (message/description)
5. WHEN xử lý Sort THEN hệ thống SHALL hỗ trợ sắp xếp theo MucDoUuTien (priority) và ThoiGianTao (createdAt)

### Requirement 7

**User Story:** Là một developer, tôi muốn chuẩn hóa trang Departments, để filter/sort được xử lý ở BE thay vì FE.

#### Acceptance Criteria

1. WHEN kiểm tra RoomSearchFilter THEN hệ thống SHALL xác nhận DTO đã có Keyword, LoaiPhong, TrangThai, SortBy, SortDirection, Page, PageSize
2. WHEN cập nhật FE THEN hệ thống SHALL chuyển từ filter client-side sang gọi API search với pagination
3. WHEN xử lý Keyword THEN hệ thống SHALL tìm kiếm trong TenPhong, TenKhoa, TenBacSi, TenDieuDuong
4. WHEN xử lý Sort THEN hệ thống SHALL hỗ trợ sắp xếp theo SucChua (capacity), TenPhong, TenKhoa
5. WHEN hiển thị kết quả THEN hệ thống SHALL hiển thị pagination controls nếu có nhiều hơn 50 kết quả

### Requirement 8

**User Story:** Là một developer, tôi muốn chuẩn hóa tab Stock trong trang Prescriptions, để filter DonViTinh được xử lý ở BE thay vì FE.

#### Acceptance Criteria

1. WHEN kiểm tra DrugSearchFilter THEN hệ thống SHALL xác nhận DTO đã có field DonViTinh
2. WHEN kiểm tra PharmacyService THEN hệ thống SHALL xác nhận service đã xử lý filter DonViTinh
3. WHEN cập nhật FE THEN hệ thống SHALL xóa logic filter unit ở client-side (dòng 236-256)
4. WHEN gọi API THEN hệ thống SHALL truyền DonViTinh vào params
5. WHEN hiển thị kết quả THEN hệ thống SHALL hiển thị dữ liệu đã được filter từ BE

### Requirement 9

**User Story:** Là một developer, tôi muốn tạo tài liệu hướng dẫn chuẩn hóa, để các developer khác có thể áp dụng cho các trang mới trong tương lai.

#### Acceptance Criteria

1. WHEN tạo tài liệu THEN hệ thống SHALL mô tả cấu trúc DTO filter chuẩn
2. WHEN tạo tài liệu THEN hệ thống SHALL mô tả cách xử lý filter/sort/search trong Service
3. WHEN tạo tài liệu THEN hệ thống SHALL mô tả cách gọi API từ FE với pagination
4. WHEN tạo tài liệu THEN hệ thống SHALL cung cấp ví dụ code mẫu cho từng bước
5. WHEN tạo tài liệu THEN hệ thống SHALL liệt kê các best practices và lưu ý quan trọng
