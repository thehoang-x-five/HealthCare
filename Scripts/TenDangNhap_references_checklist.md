# TenDangNhap References Checklist

This document lists all files that reference `TenDangNhap` and need to be updated after the UserAccount/NhanVienYTe split.

## Files Requiring Updates

### 1. Services/UserInteraction/AuthService.cs
**Status**: NEEDS UPDATE
**References**:
- Line 37: `FirstOrDefaultAsync(s => s.TenDangNhap == request.TenDangNhap)` - Query NhanVienYTe
- Line 127-128: `request.TenDangNhap` validation in ForgotPasswordAsync
- Line 138: `FirstOrDefaultAsync(s => s.TenDangNhap == request.TenDangNhap)` - Query NhanVienYTe
- Line 280: `new Claim(JwtRegisteredClaimNames.UniqueName, staff.TenDangNhap)` - JWT claim
- Line 345: `TenDangNhap = staff.TenDangNhap` - LoginResponse DTO
- Line 384: `TenDangNhap = staff.TenDangNhap` - Another response DTO

**Required Changes**:
- Change queries from `NhanVienYTes` to `UserAccounts`
- Include `.Include(u => u.NhanVienYTe)` for navigation
- Update JWT claims to use `UserAccount.TenDangNhap`
- Update response DTOs to use `UserAccount.TenDangNhap`

### 2. Services/Admin/AdminService.cs
**Status**: NEEDS UPDATE
**References**:
- Line 30: `n.TenDangNhap.ToLower().Contains(q)` - Search filter
- Line 75: `AnyAsync(n => n.TenDangNhap == request.TenDangNhap)` - Duplicate check
- Line 84: `TenDangNhap = request.TenDangNhap` - Create user
- Line 170: `TenDangNhap = n.TenDangNhap` - Response DTO

**Required Changes**:
- Update search filter to query UserAccounts
- Update duplicate check to query UserAccounts
- Update create user to create UserAccount entity
- Update response DTOs to use UserAccount.TenDangNhap

### 3. Entities/UserAccount.cs
**Status**: ✅ ALREADY CORRECT
**References**:
- Line 14: Property definition `public string TenDangNhap { get; set; }`

**No changes needed** - This is the new entity with TenDangNhap

### 4. DTOs/AuthDtos.cs
**Status**: ✅ ALREADY CORRECT
**References**:
- Line 8: `AuthLoginRequest.TenDangNhap` - Request DTO
- Line 27: `AuthLoginResponse.TenDangNhap` - Response DTO
- Line 62: `AuthMeResponse.TenDangNhap` - Response DTO
- Line 82: `AuthForgotPasswordRequest.TenDangNhap` - Request DTO

**No changes needed** - DTOs are correct, services need to map from UserAccount

### 5. DTOs/AdminDtos.cs
**Status**: ✅ ALREADY CORRECT
**References**:
- Line 9: `AdminUserResponse.TenDangNhap` - Response DTO
- Line 28: `AdminUserCreateRequest.TenDangNhap` - Request DTO

**No changes needed** - DTOs are correct, services need to map from UserAccount

### 6. Datas/DataSeed.cs
**Status**: NEEDS UPDATE
**References**:
- Lines 661-673: Multiple `TenDangNhap` assignments in NhanVienYTe seed data

**Required Changes**:
- Remove TenDangNhap from NhanVienYTe seed data
- Create corresponding UserAccount seed data with TenDangNhap
- Ensure 1:1 relationship between UserAccount and NhanVienYTe

### 7. Migrations (OLD - Will be replaced)
**Status**: ⚠️ WILL BE REPLACED BY NEW MIGRATION
**Files**:
- Migrations/DataContextModelSnapshot.cs
- Migrations/20260317075029_InitialCreate.Designer.cs
- Migrations/20260317075029_InitialCreate.cs

**No manual changes needed** - These will be updated by new EF migration

## Summary

**Total files**: 7
**Need updates**: 3 (AuthService.cs, AdminService.cs, DataSeed.cs)
**Already correct**: 3 (UserAccount.cs, AuthDtos.cs, AdminDtos.cs)
**Will be replaced**: 1 (Old migrations)

## Next Steps

1. ✅ Complete Task 1.5 - Document all references (THIS FILE)
2. ⏭️ Task 1.6 - Create manual SQL migration script
3. ⏭️ Task 1.7 - Generate EF migration
4. ⏭️ Task 1.8 - Execute manual SQL migration
5. ⏭️ Task 1.9 - Execute EF migration
6. ⏭️ Task 3 - Update AuthService
7. ⏭️ Task 4 - Update AdminService
8. ⏭️ Task 20 - Update DataSeed
