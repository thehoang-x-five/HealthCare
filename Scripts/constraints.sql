-- ========================================
-- Database CHECK Constraints
-- Week 1 Infrastructure Upgrade - Task 14
-- Defense-in-Depth Layer 2: Database-level validation
-- ========================================

-- 14.1: Prevent negative inventory in kho_thuoc
ALTER TABLE kho_thuoc
ADD CONSTRAINT chk_kho_thuoc_so_luong_non_negative
CHECK (SoLuongTon >= 0);

-- 14.2: Enforce valid status values for lich_hen_kham
ALTER TABLE lich_hen_kham
ADD CONSTRAINT chk_lich_hen_trang_thai
CHECK (TrangThai IN ('dang_cho', 'da_xac_nhan', 'da_checkin', 'da_huy'));

-- 14.3: Enforce valid status values for luot_kham_benh
ALTER TABLE luot_kham_benh
ADD CONSTRAINT chk_luot_kham_trang_thai
CHECK (TrangThai IN ('dang_thuc_hien', 'hoan_tat', 'da_huy'));

-- 14.4: Enforce valid status values for don_thuoc
ALTER TABLE don_thuoc
ADD CONSTRAINT chk_don_thuoc_trang_thai
CHECK (TrangThai IN ('da_ke', 'cho_phat', 'da_phat', 'da_huy'));

-- 14.5: Enforce valid status values for hoa_don_thanh_toan
ALTER TABLE hoa_don_thanh_toan
ADD CONSTRAINT chk_hoa_don_trang_thai
CHECK (TrangThai IN ('chua_thu', 'da_thu', 'da_huy'));
