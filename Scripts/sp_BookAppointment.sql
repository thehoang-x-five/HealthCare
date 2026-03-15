-- ========================================
-- Stored Procedure: sp_BookAppointment
-- Week 1 Infrastructure Upgrade - Task 18
-- SERIALIZABLE isolation to prevent appointment booking race conditions
-- ========================================

DELIMITER $$

DROP PROCEDURE IF EXISTS sp_BookAppointment$$

CREATE PROCEDURE sp_BookAppointment(
    IN p_MaLichHen VARCHAR(20),
    IN p_CoHieuLuc BOOLEAN,
    IN p_NgayHen DATE,
    IN p_GioHen TIME,
    IN p_ThoiLuongPhut INT,
    IN p_MaBenhNhan VARCHAR(20),
    IN p_LoaiHen VARCHAR(50),
    IN p_TenBenhNhan VARCHAR(100),
    IN p_SoDienThoai VARCHAR(15),
    IN p_MaLichTruc VARCHAR(20),
    IN p_GhiChu TEXT,
    IN p_TrangThai VARCHAR(20)
)
BEGIN
    DECLARE v_conflict_count INT DEFAULT 0;
    DECLARE v_gio_ket_thuc TIME;
    
    -- Set SERIALIZABLE isolation level for absolute consistency
    SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
    START TRANSACTION;
    
    -- Calculate end time for this appointment
    SET v_gio_ket_thuc = ADDTIME(p_GioHen, SEC_TO_TIME(p_ThoiLuongPhut * 60));
    
    -- Check for overlapping appointments using interval arithmetic
    -- Two intervals [A_start, A_end) and [B_start, B_end) overlap if:
    -- A_start < B_end AND B_start < A_end
    SELECT COUNT(*) INTO v_conflict_count
    FROM lich_hen_kham
    WHERE NgayHen = p_NgayHen
      AND MaLichTruc = p_MaLichTruc
      AND TrangThai IN ('da_xac_nhan', 'da_checkin')
      AND CoHieuLuc = TRUE
      AND (
          -- Existing appointment overlaps with new appointment
          (GioHen < v_gio_ket_thuc)
          AND
          (ADDTIME(GioHen, SEC_TO_TIME(ThoiLuongPhut * 60)) > p_GioHen)
      );
    
    -- If conflict detected, rollback and signal error
    IF v_conflict_count > 0 THEN
        ROLLBACK;
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Trùng lịch: Đã có lịch hẹn khác trong khoảng thời gian này';
    ELSE
        -- No conflict, insert the appointment
        INSERT INTO lich_hen_kham (
            MaLichHen,
            CoHieuLuc,
            NgayHen,
            GioHen,
            ThoiLuongPhut,
            MaBenhNhan,
            LoaiHen,
            TenBenhNhan,
            SoDienThoai,
            MaLichTruc,
            GhiChu,
            TrangThai
        ) VALUES (
            p_MaLichHen,
            p_CoHieuLuc,
            p_NgayHen,
            p_GioHen,
            p_ThoiLuongPhut,
            p_MaBenhNhan,
            p_LoaiHen,
            p_TenBenhNhan,
            p_SoDienThoai,
            p_MaLichTruc,
            p_GhiChu,
            p_TrangThai
        );
        
        COMMIT;
    END IF;
END$$

DELIMITER ;
