
DELIMITER $$

-- State Transition Validation Trigger
-- Prevents invalid state transitions for appointments

DROP TRIGGER IF EXISTS tr_LichHen_ValidateTransition$$

CREATE TRIGGER tr_LichHen_ValidateTransition
BEFORE UPDATE ON lich_hen_kham
FOR EACH ROW
BEGIN
    -- Prevent transitions FROM da_huy (terminal state)
    IF OLD.TrangThai = 'da_huy' AND NEW.TrangThai != 'da_huy' THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Không thể chuyển trạng thái từ da_huy sang trạng thái khác';
    END IF;
    
    -- Prevent backward transitions FROM da_checkin
    IF OLD.TrangThai = 'da_checkin' AND NEW.TrangThai IN ('dang_cho', 'da_xac_nhan') THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Không thể chuyển trạng thái từ da_checkin về dang_cho hoặc da_xac_nhan';
    END IF;
END$$

-- Inventory Non-Negativity Trigger
-- Prevents negative inventory in kho_thuoc

DROP TRIGGER IF EXISTS tr_KhoThuoc_PreventNegative$$

CREATE TRIGGER tr_KhoThuoc_PreventNegative
BEFORE UPDATE ON kho_thuoc
FOR EACH ROW
BEGIN
    IF NEW.SoLuongTon < 0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Số lượng tồn kho không được âm';
    END IF;
END$$

-- Prescription Cancellation Rollback Trigger
-- Automatically restores inventory when prescription is cancelled

DROP TRIGGER IF EXISTS tr_DonThuoc_RollbackKho$$

CREATE TRIGGER tr_DonThuoc_RollbackKho
AFTER UPDATE ON don_thuoc
FOR EACH ROW
BEGIN
    DECLARE v_ma_thuoc VARCHAR(20);
    DECLARE v_so_luong INT;
    DECLARE v_so_luong_con_lai INT;
    DECLARE done INT DEFAULT FALSE;
    
    -- Cursor to iterate through prescription items
    DECLARE cur_chi_tiet CURSOR FOR
        SELECT MaThuoc, SoLuong
        FROM chi_tiet_don_thuoc
        WHERE MaDonThuoc = NEW.MaDonThuoc;
    
    DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = TRUE;
    
    -- Only process when status changes TO da_huy
    IF OLD.TrangThai != 'da_huy' AND NEW.TrangThai = 'da_huy' THEN
        OPEN cur_chi_tiet;
        
        read_loop: LOOP
            FETCH cur_chi_tiet INTO v_ma_thuoc, v_so_luong;
            IF done THEN
                LEAVE read_loop;
            END IF;
            
            -- Restore inventory
            UPDATE kho_thuoc
            SET SoLuongTon = SoLuongTon + v_so_luong
            WHERE MaThuoc = v_ma_thuoc;
            
            -- Get updated inventory level
            SELECT SoLuongTon INTO v_so_luong_con_lai
            FROM kho_thuoc
            WHERE MaThuoc = v_ma_thuoc;
            
            -- Log rollback transaction in lich_su_xuat_kho
            INSERT INTO lich_su_xuat_kho (
                MaGiaoDich,
                MaThuoc,
                MaDonThuoc,
                MaNhanSuXuat,
                LoaiGiaoDich,
                SoLuong,
                SoLuongConLai,
                ThoiGianXuat,
                GhiChu
            ) VALUES (
                CONCAT('ROLLBACK_', NEW.MaDonThuoc, '_', v_ma_thuoc, '_', UNIX_TIMESTAMP()),
                v_ma_thuoc,
                NEW.MaDonThuoc,
                COALESCE(NEW.MaNhanSuPhat, NEW.MaBacSiKeDon),
                'hoan_tra',
                v_so_luong,
                v_so_luong_con_lai,
                NOW(),
                CONCAT('Hoàn trả tự động do hủy đơn thuốc ', NEW.MaDonThuoc)
            );
        END LOOP;
        
        CLOSE cur_chi_tiet;
    END IF;
END$$

DELIMITER ;
