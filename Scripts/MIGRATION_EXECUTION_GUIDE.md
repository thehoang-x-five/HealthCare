# Migration Execution Guide: UserAccount/NhanVienYTe Split

## ⚠️ CRITICAL: Execution Order

This migration MUST be executed in the following order:

1. **FIRST**: Execute manual SQL migration script
2. **SECOND**: Execute Entity Framework migration
3. **THIRD**: Update application services

**DO NOT reverse this order!** The manual SQL script creates the user_accounts table and migrates data. The EF migration then drops the old columns from nhan_vien_y_te.

---

## Prerequisites

Before starting:
- [ ] Backup the database: `mysqldump -u root -p HealthCareDB > backup_before_user_split.sql`
- [ ] Ensure no active user sessions
- [ ] Stop the application server
- [ ] Review the manual SQL script: `backend/HealthCare/Scripts/manual_user_split_migration.sql`
- [ ] Review the EF migration: `backend/HealthCare/Migrations/20260406000000_SplitUserFromStaff.cs`

---

## Step 1: Execute Manual SQL Migration (FIRST)

### 1.1 Connect to MySQL

```bash
mysql -u root -p HealthCareDB
```

### 1.2 Execute the manual migration script

```bash
source backend/HealthCare/Scripts/manual_user_split_migration.sql
```

Or from command line:

```bash
mysql -u root -p HealthCareDB < backend/HealthCare/Scripts/manual_user_split_migration.sql
```

### 1.3 Verify the migration results

The script includes verification queries. Check the output for:

- ✅ Total user accounts created matches staff with credentials
- ✅ All user accounts have staff links (MaNhanVien)
- ✅ All refresh tokens have user links (MaUser)
- ✅ Zero orphaned refresh tokens

### 1.4 Review before committing

The script runs in a transaction. Review the verification output:

- If everything looks correct: The script auto-commits
- If there are issues: Manually ROLLBACK and investigate

---

## Step 2: Execute Entity Framework Migration (SECOND)

### 2.1 Navigate to project directory

```bash
cd backend/HealthCare
```

### 2.2 Apply the EF migration

```bash
dotnet ef database update
```

This will:
- Drop `TenDangNhap` column from `nhan_vien_y_te`
- Drop `MatKhauHash` column from `nhan_vien_y_te`
- Drop `VaiTro` column from `nhan_vien_y_te`
- Drop `LoaiYTa` column from `nhan_vien_y_te`
- Drop `ChucVu` column from `nhan_vien_y_te`

### 2.3 Verify the migration

```sql
-- Check that authentication columns are removed from nhan_vien_y_te
DESCRIBE nhan_vien_y_te;

-- Should NOT see: TenDangNhap, MatKhauHash, VaiTro, LoaiYTa, ChucVu
-- Should still see: MaNhanVien, HoTen, MaKhoa, ChuyenMon, HocVi, etc.
```

---

## Step 3: Verify Complete Migration

### 3.1 Check table structures

```sql
-- user_accounts should exist with all columns
DESCRIBE user_accounts;

-- nhan_vien_y_te should have personnel columns only
DESCRIBE nhan_vien_y_te;

-- refresh_token should use MaUser FK
DESCRIBE refresh_token;
```

### 3.2 Check data integrity

```sql
-- All user accounts should have staff links
SELECT COUNT(*) FROM user_accounts WHERE MaNhanVien IS NULL;
-- Expected: 0

-- All staff should have user accounts
SELECT COUNT(*) 
FROM nhan_vien_y_te nv
LEFT JOIN user_accounts ua ON nv.MaNhanVien = ua.MaNhanVien
WHERE ua.MaUser IS NULL;
-- Expected: 0

-- All refresh tokens should have user links
SELECT COUNT(*) FROM refresh_token WHERE MaUser IS NULL;
-- Expected: 0
```

### 3.3 Check foreign key constraints

```sql
-- Show all foreign keys
SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    CONSTRAINT_NAME,
    REFERENCED_TABLE_NAME,
    REFERENCED_COLUMN_NAME
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = 'HealthCareDB'
AND TABLE_NAME IN ('user_accounts', 'refresh_token', 'nhan_vien_y_te')
AND REFERENCED_TABLE_NAME IS NOT NULL;
```

Expected constraints:
- `user_accounts.MaNhanVien` → `nhan_vien_y_te.MaNhanVien`
- `refresh_token.MaUser` → `user_accounts.MaUser`

---

## Step 4: Update Application Code (THIRD)

After migrations complete successfully, update the following services:

### 4.1 Update AuthService (Task 3)
- Change Login to query UserAccounts
- Update JWT token generation
- Update RefreshToken methods

### 4.2 Update AdminService (Task 4)
- Update user management methods
- Query UserAccounts instead of NhanVienYTe

### 4.3 Update DataSeed (Task 20)
- Remove authentication fields from NhanVienYTe seed
- Add UserAccount seed data

---

## Rollback Procedure (If Needed)

If you need to rollback:

### If manual SQL migration hasn't been committed:
```sql
ROLLBACK;
```

### If manual SQL migration was committed but EF migration not run:
```sql
-- Drop user_accounts table
DROP TABLE IF EXISTS user_accounts;

-- Restore refresh_token.MaNhanVien
ALTER TABLE refresh_token ADD COLUMN MaNhanVien VARCHAR(255);
UPDATE refresh_token rt
INNER JOIN nhan_vien_y_te nv ON rt.MaUser = CONCAT('USR_', nv.MaNhanVien)
SET rt.MaNhanVien = nv.MaNhanVien;
ALTER TABLE refresh_token DROP COLUMN MaUser;
```

### If both migrations completed:
```bash
# Restore from backup
mysql -u root -p HealthCareDB < backup_before_user_split.sql
```

---

## Post-Migration Testing

### Test 1: Login with existing credentials
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"TenDangNhap":"bs_tq01","MatKhau":"password123"}'
```

Expected: 200 OK with JWT token

### Test 2: Verify JWT claims
Decode the JWT token and verify it contains:
- `ma_user` (new)
- `ma_nhan_vien`
- `vai_tro`
- `loai_y_ta` (if applicable)
- `ho_ten`
- `ma_khoa`

### Test 3: Refresh token flow
```bash
curl -X POST http://localhost:5000/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"RefreshToken":"<token>"}'
```

Expected: 200 OK with new access token

---

## Troubleshooting

### Issue: "Orphaned refresh tokens found"
**Cause**: Some refresh tokens reference staff that don't exist
**Solution**: Delete orphaned tokens before proceeding
```sql
DELETE FROM refresh_token WHERE MaNhanVien NOT IN (SELECT MaNhanVien FROM nhan_vien_y_te);
```

### Issue: "Duplicate TenDangNhap"
**Cause**: Multiple staff records with same username
**Solution**: Fix duplicates before migration
```sql
SELECT TenDangNhap, COUNT(*) 
FROM nhan_vien_y_te 
GROUP BY TenDangNhap 
HAVING COUNT(*) > 1;
```

### Issue: "Foreign key constraint fails"
**Cause**: Data integrity issue
**Solution**: Check that all MaNhanVien values in user_accounts exist in nhan_vien_y_te

---

## Success Criteria

Migration is successful when:
- ✅ user_accounts table exists with all data
- ✅ refresh_token uses MaUser FK
- ✅ nhan_vien_y_te has no authentication columns
- ✅ All foreign key constraints valid
- ✅ No orphaned records
- ✅ Existing users can login
- ✅ JWT tokens contain all required claims
- ✅ Refresh token flow works

---

## Next Steps

After successful migration:
1. Mark Task 1.8 and 1.9 as complete
2. Proceed to Checkpoint 2 - Verify UserAccount Migration
3. Continue with Task 3 - Update AuthService
4. Continue with Task 4 - Implement AdminService

---

## Notes

- The manual SQL script is idempotent (can be run multiple times safely)
- The EF migration can be rolled back using `dotnet ef database update <previous-migration>`
- Keep the backup file until all testing is complete
- Monitor application logs after deployment for any authentication issues
