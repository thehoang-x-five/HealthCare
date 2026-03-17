-- =================================================================================
-- Script: sp_Genealogy.sql
-- Mô tả: Các Stored Procedures sử dụng Đệ quy (Recursive CTE) cho tính năng Pha hệ
-- =================================================================================

DELIMITER //

-- 1. Lấy cây tổ tiên (cha mẹ, ông bà...) tối đa 5 đời
DROP PROCEDURE IF EXISTS sp_GetAncestors //
CREATE PROCEDURE sp_GetAncestors(IN p_MaBenhNhan VARCHAR(50))
BEGIN
    WITH RECURSIVE Ancestors AS (
        SELECT MaBenhNhan, HoTen, NgaySinh, GioiTinh, NhomMau, BenhManTinh,
               MaCha, MaMe, 0 AS DoiThu
        FROM benh_nhan
        WHERE MaBenhNhan = p_MaBenhNhan

        UNION ALL

        SELECT bn.MaBenhNhan, bn.HoTen, bn.NgaySinh, bn.GioiTinh, bn.NhomMau, bn.BenhManTinh,
               bn.MaCha, bn.MaMe, a.DoiThu + 1
        FROM benh_nhan bn
        INNER JOIN Ancestors a ON bn.MaBenhNhan = a.MaCha OR bn.MaBenhNhan = a.MaMe
        WHERE a.DoiThu < 5
    )
    SELECT DISTINCT MaBenhNhan, HoTen, NgaySinh, GioiTinh, NhomMau, BenhManTinh,
           MaCha, MaMe, DoiThu
    FROM Ancestors;
END //


-- 2. Lấy cây con cháu (con, cháu...) tối đa 5 đời
DROP PROCEDURE IF EXISTS sp_GetDescendants //
CREATE PROCEDURE sp_GetDescendants(IN p_MaBenhNhan VARCHAR(50))
BEGIN
    WITH RECURSIVE Descendants AS (
        SELECT MaBenhNhan, HoTen, NgaySinh, GioiTinh, NhomMau, BenhManTinh,
               MaCha, MaMe, 0 AS DoiThu
        FROM benh_nhan
        WHERE MaBenhNhan = p_MaBenhNhan

        UNION ALL

        SELECT bn.MaBenhNhan, bn.HoTen, bn.NgaySinh, bn.GioiTinh, bn.NhomMau, bn.BenhManTinh,
               bn.MaCha, bn.MaMe, d.DoiThu + 1
        FROM benh_nhan bn
        INNER JOIN Descendants d ON bn.MaCha = d.MaBenhNhan OR bn.MaMe = d.MaBenhNhan
        WHERE d.DoiThu < 5
    )
    SELECT DISTINCT MaBenhNhan, HoTen, NgaySinh, GioiTinh, NhomMau, BenhManTinh,
           MaCha, MaMe, DoiThu
    FROM Descendants;
END //


-- 3. Kiểm tra xem 1 ứng viên có phải là dòng dõi (con/cháu) của 1 tổ tiên không (chống vòng lặp)
DROP PROCEDURE IF EXISTS sp_IsDescendantOf //
CREATE PROCEDURE sp_IsDescendantOf(IN p_AncestorId VARCHAR(50), IN p_CandidateId VARCHAR(50))
BEGIN
    WITH RECURSIVE Desc_Check AS (
        SELECT MaBenhNhan, HoTen, NgaySinh, GioiTinh, NhomMau, BenhManTinh, MaCha, MaMe
        FROM benh_nhan
        WHERE MaBenhNhan = p_AncestorId

        UNION ALL

        SELECT bn.MaBenhNhan, bn.HoTen, bn.NgaySinh, bn.GioiTinh, bn.NhomMau, bn.BenhManTinh, bn.MaCha, bn.MaMe
        FROM benh_nhan bn
        INNER JOIN Desc_Check dc ON bn.MaCha = dc.MaBenhNhan OR bn.MaMe = dc.MaBenhNhan
    )
    SELECT * FROM Desc_Check WHERE MaBenhNhan = p_CandidateId;
END //

DELIMITER ;
