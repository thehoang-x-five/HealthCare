-- ============================================================================
-- Manual SQL Migration: Split UserAccount from NhanVienYTe
-- ============================================================================
-- CRITICAL: This script MUST be executed BEFORE the Entity Framework migration
-- Purpose: Migrate authentication data from nhan_vien_y_te to user_accounts
-- Date: 2026-04-06
-- ============================================================================

USE HealthCareDB;

-- Start transaction for data safety
START TRANSACTION;

-- ============================================================================
-- STEP 1: Create user_accounts table
-- ============================================================================
CREATE TABLE IF NOT EXISTS user_accounts (
    MaUser VARCHAR(255) NOT NULL PRIMARY KEY,
    TenDangNhap VARCHAR(50) NOT NULL,
    MatKhauHash LONGTEXT NOT NULL,
    VaiTro VARCHAR(50) NOT NULL,
    LoaiYTa VARCHAR(50) NULL,
    TrangThaiTaiKhoan VARCHAR(50) NOT NULL DEFAULT 'hoat_dong',
    LanDangNhapCuoi DATETIME(6) NULL,
    NgayTao DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    NgayCapNhat DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    MaNhanVien VARCHAR(255) NULL,
    
    CONSTRAINT UK_UserAccount_TenDangNhap UNIQUE (TenDangNhap),
    CONSTRAINT CK_UserAccount_VaiTro CHECK (VaiTro IN ('admin', 'bac_si', 'y_ta', 'ky_thuat_vien')),
    CONSTRAINT CK_UserAccount_TrangThaiTaiKhoan CHECK (TrangThaiTaiKhoan IN ('hoat_dong', 'khoa', 'tam_ngung')),
    CONSTRAINT CK_UserAccount_LoaiYTa CHECK (LoaiYTa IS NULL OR LoaiYTa IN ('hanhchinh', 'ls', 'cls'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- STEP 2: Migrate authentication data from nhan_vien_y_te to user_accounts
-- ============================================================================
-- Generate MaUser with pattern: USR_{MaNhanVien}
-- Map ChucVu to VaiTro using business rules
INSERT INTO user_accounts (
    MaUser,
    TenDangNhap,
    MatKhauHash,
    VaiTro,
    LoaiYTa,
    TrangThaiTaiKhoan,
    LanDangNhapCuoi,
    NgayTao,
    NgayCapNhat,
    MaNhanVien
)
SELECT 
    CONCAT('USR_', MaNhanVien) AS MaUser,
    TenDangNhap,
    MatKhauHash,
    -- Map VaiTro from existing VaiTro column (already correct in current schema)
    VaiTro,
    -- Map LoaiYTa from existing LoaiYTa column (already correct in current schema)
    LoaiYTa,
    -- Set all accounts to active status
    'hoat_dong' AS TrangThaiTaiKhoan,
    -- No previous login data
    NULL AS LanDangNhapCuoi,
    -- Set creation timestamp to now
    NOW(6) AS NgayTao,
    NOW(6) AS NgayCapNhat,
    -- Link to staff record
    MaNhanVien
FROM nhan_vien_y_te
WHERE TenDangNhap IS NOT NULL AND TenDangNhap != '';

-- ============================================================================
-- STEP 3: Verify user_accounts data integrity
-- ============================================================================
-- Check that all staff with credentials have corresponding user accounts
SELECT 
    'Staff with credentials' AS check_type,
    COUNT(*) AS count
FROM nhan_vien_y_te 
WHERE TenDangNhap IS NOT NULL AND TenDangNhap != '';

SELECT 
    'User accounts created' AS check_type,
    COUNT(*) AS count
FROM user_accounts;

-- These counts should match!

-- ============================================================================
-- STEP 4: Add MaUser column to refresh_token table
-- ============================================================================
ALTER TABLE refresh_token 
ADD COLUMN MaUser VARCHAR(255) NULL AFTER Id;

-- ============================================================================
-- STEP 5: Populate refresh_token.MaUser from NhanVienYTe join
-- ============================================================================
UPDATE refresh_token rt
INNER JOIN user_accounts ua ON rt.MaNhanVien = ua.MaNhanVien
SET rt.MaUser = ua.MaUser;

-- ============================================================================
-- STEP 6: Verify no orphaned RefreshToken records
-- ============================================================================
SELECT 
    'Orphaned refresh tokens (should be 0)' AS check_type,
    COUNT(*) AS count
FROM refresh_token 
WHERE MaUser IS NULL;

-- If count > 0, investigate before proceeding!
-- Uncomment the following to see orphaned records:
-- SELECT * FROM refresh_token WHERE MaUser IS NULL;

-- ============================================================================
-- STEP 7: Make MaUser NOT NULL and add foreign key constraint
-- ============================================================================
-- Make MaUser required
ALTER TABLE refresh_token 
MODIFY COLUMN MaUser VARCHAR(255) NOT NULL;

-- Add foreign key constraint to user_accounts
ALTER TABLE refresh_token
ADD CONSTRAINT FK_RefreshToken_UserAccount_MaUser 
FOREIGN KEY (MaUser) REFERENCES user_accounts(MaUser) ON DELETE CASCADE;

-- Add index for performance
CREATE INDEX IX_RefreshToken_MaUser_IsTrangThai 
ON refresh_token(MaUser, IsTrangThai);

-- ============================================================================
-- STEP 8: Drop old MaNhanVien column from refresh_token
-- ============================================================================
-- First drop the foreign key constraint
ALTER TABLE refresh_token 
DROP FOREIGN KEY FK_RefreshToken_NhanVienYTe_MaNhanVien;

-- Drop the index
DROP INDEX IX_RefreshToken_MaNhanVien_IsTrangThai ON refresh_token;

-- Drop the column
ALTER TABLE refresh_token 
DROP COLUMN MaNhanVien;

-- ============================================================================
-- STEP 9: Add foreign key from user_accounts to nhan_vien_y_te
-- ============================================================================
ALTER TABLE user_accounts
ADD CONSTRAINT FK_UserAccount_NhanVienYTe_MaNhanVien
FOREIGN KEY (MaNhanVien) REFERENCES nhan_vien_y_te(MaNhanVien) ON DELETE RESTRICT;

-- ============================================================================
-- STEP 10: Final verification
-- ============================================================================
SELECT 'Migration verification' AS status;

SELECT 
    'Total user accounts' AS metric,
    COUNT(*) AS value
FROM user_accounts;

SELECT 
    'User accounts with staff link' AS metric,
    COUNT(*) AS value
FROM user_accounts 
WHERE MaNhanVien IS NOT NULL;

SELECT 
    'Refresh tokens migrated' AS metric,
    COUNT(*) AS value
FROM refresh_token;

SELECT 
    'Refresh tokens with user link' AS metric,
    COUNT(*) AS value
FROM refresh_token 
WHERE MaUser IS NOT NULL;

-- ============================================================================
-- COMMIT TRANSACTION
-- ============================================================================
-- Review the verification results above before committing!
-- If everything looks correct, commit the transaction:
COMMIT;

-- If there are issues, rollback instead:
-- ROLLBACK;

-- ============================================================================
-- POST-MIGRATION NOTES
-- ============================================================================
-- After this script completes successfully:
-- 1. Run Entity Framework migration to drop authentication columns from nhan_vien_y_te
-- 2. Update AuthService to query user_accounts instead of nhan_vien_y_te
-- 3. Update AdminService to manage user_accounts
-- 4. Update all services to use UserAccount for authentication
-- ============================================================================
