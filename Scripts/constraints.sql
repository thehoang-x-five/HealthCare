

-- Prevent negative inventory in kho_thuoc
ALTER TABLE kho_thuoc
ADD CONSTRAINT chk_kho_thuoc_so_luong_non_negative
CHECK (SoLuongTon >= 0);

-- Enforce valid status values for lich_hen_kham
ALTER TABLE lich_hen_kham
ADD CONSTRAINT chk_lich_hen_trang_thai
CHECK (TrangThai IN ('dang_cho', 'da_xac_nhan', 'da_checkin', 'da_huy'));

-- Enforce valid status values for luot_kham_benh
ALTER TABLE luot_kham_benh
ADD CONSTRAINT chk_luot_kham_trang_thai
CHECK (TrangThai IN ('dang_thuc_hien', 'hoan_tat', 'da_huy'));

-- Enforce valid status values for don_thuoc
ALTER TABLE don_thuoc
ADD CONSTRAINT chk_don_thuoc_trang_thai
CHECK (TrangThai IN ('da_ke', 'cho_phat', 'da_phat', 'da_huy'));

-- Enforce valid status values for hoa_don_thanh_toan
ALTER TABLE hoa_don_thanh_toan
ADD CONSTRAINT chk_hoa_don_trang_thai
CHECK (TrangThai IN ('chua_thu', 'da_thu', 'da_huy'));
