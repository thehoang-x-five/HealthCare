-- Keep database check constraints aligned with the codebase.
-- This script is idempotent and safe to run on every application startup.

-- Prevent negative inventory in kho_thuoc
SET @constraint_sql = IF (
    EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA = DATABASE()
          AND CONSTRAINT_NAME = 'chk_kho_thuoc_so_luong_non_negative'
    ),
    'SELECT 1',
    'ALTER TABLE kho_thuoc ADD CONSTRAINT chk_kho_thuoc_so_luong_non_negative CHECK (SoLuongTon >= 0)'
);
PREPARE bootstrap_stmt FROM @constraint_sql;
EXECUTE bootstrap_stmt;
DEALLOCATE PREPARE bootstrap_stmt;

-- Enforce valid status values for lich_hen_kham
SET @constraint_sql = IF (
    EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA = DATABASE()
          AND CONSTRAINT_NAME = 'chk_lich_hen_trang_thai'
    ),
    'SELECT 1',
    'ALTER TABLE lich_hen_kham ADD CONSTRAINT chk_lich_hen_trang_thai CHECK (TrangThai IN (''dang_cho'', ''da_xac_nhan'', ''da_checkin'', ''da_huy''))'
);
PREPARE bootstrap_stmt FROM @constraint_sql;
EXECUTE bootstrap_stmt;
DEALLOCATE PREPARE bootstrap_stmt;

-- Enforce valid status values for luot_kham_benh
SET @constraint_sql = IF (
    EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA = DATABASE()
          AND CONSTRAINT_NAME = 'chk_luot_kham_trang_thai'
    ),
    'SELECT 1',
    'ALTER TABLE luot_kham_benh ADD CONSTRAINT chk_luot_kham_trang_thai CHECK (TrangThai IN (''dang_thuc_hien'', ''hoan_tat'', ''da_huy''))'
);
PREPARE bootstrap_stmt FROM @constraint_sql;
EXECUTE bootstrap_stmt;
DEALLOCATE PREPARE bootstrap_stmt;

-- Enforce valid status values for don_thuoc
SET @constraint_sql = IF (
    EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA = DATABASE()
          AND CONSTRAINT_NAME = 'chk_don_thuoc_trang_thai'
    ),
    'SELECT 1',
    'ALTER TABLE don_thuoc ADD CONSTRAINT chk_don_thuoc_trang_thai CHECK (TrangThai IN (''da_ke'', ''cho_phat'', ''da_phat'', ''da_huy''))'
);
PREPARE bootstrap_stmt FROM @constraint_sql;
EXECUTE bootstrap_stmt;
DEALLOCATE PREPARE bootstrap_stmt;

-- Enforce valid status values for hoa_don_thanh_toan
-- Recreate this constraint on every bootstrap so new enum values stay in sync.
SET @constraint_sql = IF (
    EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA = DATABASE()
          AND CONSTRAINT_NAME = 'chk_hoa_don_trang_thai'
    ),
    'ALTER TABLE hoa_don_thanh_toan DROP CHECK chk_hoa_don_trang_thai',
    'SELECT 1'
);
PREPARE bootstrap_stmt FROM @constraint_sql;
EXECUTE bootstrap_stmt;
DEALLOCATE PREPARE bootstrap_stmt;

SET @constraint_sql =
    'ALTER TABLE hoa_don_thanh_toan ADD CONSTRAINT chk_hoa_don_trang_thai CHECK (TrangThai IN (''chua_thu'', ''da_thu'', ''da_huy'', ''bao_luu''))';
PREPARE bootstrap_stmt FROM @constraint_sql;
EXECUTE bootstrap_stmt;
DEALLOCATE PREPARE bootstrap_stmt;
