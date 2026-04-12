DELIMITER $$

DROP TRIGGER IF EXISTS tr_LichHen_ValidateTransition$$

CREATE TRIGGER tr_LichHen_ValidateTransition
BEFORE UPDATE ON lich_hen_kham
FOR EACH ROW
BEGIN
    IF OLD.TrangThai = 'da_huy' AND NEW.TrangThai != 'da_huy' THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Khong the chuyen trang thai tu da_huy sang trang thai khac';
    END IF;

    IF OLD.TrangThai = 'da_checkin' AND NEW.TrangThai IN ('dang_cho', 'da_xac_nhan') THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Khong the chuyen trang thai tu da_checkin ve dang_cho hoac da_xac_nhan';
    END IF;
END$$

DROP TRIGGER IF EXISTS tr_KhoThuoc_PreventNegative$$

CREATE TRIGGER tr_KhoThuoc_PreventNegative
BEFORE UPDATE ON kho_thuoc
FOR EACH ROW
BEGIN
    IF NEW.SoLuongTon < 0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'So luong ton kho khong duoc am';
    END IF;
END$$

DROP TRIGGER IF EXISTS tr_DonThuoc_RollbackKho$$

CREATE TRIGGER tr_DonThuoc_RollbackKho
AFTER UPDATE ON don_thuoc
FOR EACH ROW
BEGIN
    DECLARE v_ma_thuoc VARCHAR(20);
    DECLARE v_so_luong INT;
    DECLARE v_so_luong_con_lai INT;
    DECLARE done INT DEFAULT FALSE;

    DECLARE cur_chi_tiet CURSOR FOR
        SELECT MaThuoc, SoLuong
        FROM chi_tiet_don_thuoc
        WHERE MaDonThuoc = NEW.MaDonThuoc;

    DECLARE CONTINUE HANDLER FOR NOT FOUND SET done = TRUE;

    IF OLD.TrangThai != 'da_huy' AND NEW.TrangThai = 'da_huy' THEN
        OPEN cur_chi_tiet;

        read_loop: LOOP
            FETCH cur_chi_tiet INTO v_ma_thuoc, v_so_luong;
            IF done THEN
                LEAVE read_loop;
            END IF;

            UPDATE kho_thuoc
            SET SoLuongTon = SoLuongTon + v_so_luong
            WHERE MaThuoc = v_ma_thuoc;

            SELECT SoLuongTon INTO v_so_luong_con_lai
            FROM kho_thuoc
            WHERE MaThuoc = v_ma_thuoc;

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
                CONCAT('Hoan tra tu dong do huy don thuoc ', NEW.MaDonThuoc)
            );
        END LOOP;

        CLOSE cur_chi_tiet;
    END IF;
END$$

DROP TRIGGER IF EXISTS tr_BenhNhan_ValidateParents_Insert$$

CREATE TRIGGER tr_BenhNhan_ValidateParents_Insert
BEFORE INSERT ON benh_nhan
FOR EACH ROW
BEGIN
    DECLARE v_cha_gioi_tinh VARCHAR(50) DEFAULT NULL;
    DECLARE v_me_gioi_tinh VARCHAR(50) DEFAULT NULL;
    DECLARE v_cha_ngay_sinh DATETIME DEFAULT NULL;
    DECLARE v_me_ngay_sinh DATETIME DEFAULT NULL;

    IF NEW.MaCha IS NOT NULL AND NEW.MaCha = NEW.MaBenhNhan THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Benh nhan khong the la cha cua chinh minh';
    END IF;

    IF NEW.MaMe IS NOT NULL AND NEW.MaMe = NEW.MaBenhNhan THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Benh nhan khong the la me cua chinh minh';
    END IF;

    IF NEW.MaCha IS NOT NULL AND NEW.MaMe IS NOT NULL AND NEW.MaCha = NEW.MaMe THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Khong the gan cung mot benh nhan lam ca cha va me';
    END IF;

    IF NEW.MaCha IS NOT NULL THEN
        SELECT GioiTinh, NgaySinh
        INTO v_cha_gioi_tinh, v_cha_ngay_sinh
        FROM benh_nhan
        WHERE MaBenhNhan = NEW.MaCha
        LIMIT 1;

        IF v_cha_gioi_tinh IS NULL THEN
            SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Khong tim thay benh nhan duoc gan lam cha';
        END IF;

        IF LOWER(TRIM(IFNULL(v_cha_gioi_tinh, ''))) NOT IN ('nam', 'male', 'm') THEN
            SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Benh nhan duoc gan lam cha phai co gioi tinh nam';
        END IF;

        IF v_cha_ngay_sinh >= NEW.NgaySinh THEN
            SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Ngay sinh cua cha phai som hon ngay sinh cua con';
        END IF;

        IF DATE_ADD(v_cha_ngay_sinh, INTERVAL 12 YEAR) > NEW.NgaySinh THEN
            SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Khoang cach tuoi cha/con khong hop le';
        END IF;
    END IF;

    IF NEW.MaMe IS NOT NULL THEN
        SELECT GioiTinh, NgaySinh
        INTO v_me_gioi_tinh, v_me_ngay_sinh
        FROM benh_nhan
        WHERE MaBenhNhan = NEW.MaMe
        LIMIT 1;

        IF v_me_gioi_tinh IS NULL THEN
            SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Khong tim thay benh nhan duoc gan lam me';
        END IF;

        IF LOWER(TRIM(IFNULL(v_me_gioi_tinh, ''))) NOT IN ('nu', 'nữ', 'female', 'f') THEN
            SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Benh nhan duoc gan lam me phai co gioi tinh nu';
        END IF;

        IF v_me_ngay_sinh >= NEW.NgaySinh THEN
            SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Ngay sinh cua me phai som hon ngay sinh cua con';
        END IF;

        IF DATE_ADD(v_me_ngay_sinh, INTERVAL 12 YEAR) > NEW.NgaySinh THEN
            SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Khoang cach tuoi me/con khong hop le';
        END IF;
    END IF;
END$$

DROP TRIGGER IF EXISTS tr_BenhNhan_ValidateParents_Update$$

CREATE TRIGGER tr_BenhNhan_ValidateParents_Update
BEFORE UPDATE ON benh_nhan
FOR EACH ROW
BEGIN
    DECLARE v_cha_gioi_tinh VARCHAR(50) DEFAULT NULL;
    DECLARE v_me_gioi_tinh VARCHAR(50) DEFAULT NULL;
    DECLARE v_cha_ngay_sinh DATETIME DEFAULT NULL;
    DECLARE v_me_ngay_sinh DATETIME DEFAULT NULL;

    IF NEW.MaCha IS NOT NULL AND NEW.MaCha = NEW.MaBenhNhan THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Benh nhan khong the la cha cua chinh minh';
    END IF;

    IF NEW.MaMe IS NOT NULL AND NEW.MaMe = NEW.MaBenhNhan THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Benh nhan khong the la me cua chinh minh';
    END IF;

    IF NEW.MaCha IS NOT NULL AND NEW.MaMe IS NOT NULL AND NEW.MaCha = NEW.MaMe THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Khong the gan cung mot benh nhan lam ca cha va me';
    END IF;

    IF NEW.MaCha IS NOT NULL THEN
        SELECT GioiTinh, NgaySinh
        INTO v_cha_gioi_tinh, v_cha_ngay_sinh
        FROM benh_nhan
        WHERE MaBenhNhan = NEW.MaCha
        LIMIT 1;

        IF v_cha_gioi_tinh IS NULL THEN
            SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Khong tim thay benh nhan duoc gan lam cha';
        END IF;

        IF LOWER(TRIM(IFNULL(v_cha_gioi_tinh, ''))) NOT IN ('nam', 'male', 'm') THEN
            SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Benh nhan duoc gan lam cha phai co gioi tinh nam';
        END IF;

        IF v_cha_ngay_sinh >= NEW.NgaySinh THEN
            SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Ngay sinh cua cha phai som hon ngay sinh cua con';
        END IF;

        IF DATE_ADD(v_cha_ngay_sinh, INTERVAL 12 YEAR) > NEW.NgaySinh THEN
            SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Khoang cach tuoi cha/con khong hop le';
        END IF;
    END IF;

    IF NEW.MaMe IS NOT NULL THEN
        SELECT GioiTinh, NgaySinh
        INTO v_me_gioi_tinh, v_me_ngay_sinh
        FROM benh_nhan
        WHERE MaBenhNhan = NEW.MaMe
        LIMIT 1;

        IF v_me_gioi_tinh IS NULL THEN
            SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Khong tim thay benh nhan duoc gan lam me';
        END IF;

        IF LOWER(TRIM(IFNULL(v_me_gioi_tinh, ''))) NOT IN ('nu', 'nữ', 'female', 'f') THEN
            SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Benh nhan duoc gan lam me phai co gioi tinh nu';
        END IF;

        IF v_me_ngay_sinh >= NEW.NgaySinh THEN
            SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Ngay sinh cua me phai som hon ngay sinh cua con';
        END IF;

        IF DATE_ADD(v_me_ngay_sinh, INTERVAL 12 YEAR) > NEW.NgaySinh THEN
            SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Khoang cach tuoi me/con khong hop le';
        END IF;
    END IF;
END$$

DELIMITER ;
