using System.Collections.Generic;
using System.Threading.Tasks;
using HealthCare.DTOs;
using HealthCare.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace HealthCare.Realtime
{
    /// <summary>
    /// Facade dùng từ các service nghiệp vụ để bắn realtime (SignalR).
    /// 
    /// ✅ FILTERED BROADCAST STRATEGY (2025-01-03):
    /// Thay vì broadcast rộng rãi cho tất cả nhân sự, service này chỉ gửi realtime
    /// cho đúng người liên quan dựa trên context và phân quyền:
    /// 
    /// ROLE GROUPS (cho Dashboard/KPI/Patient CRUD):
    ///  - "bac_si" : tất cả bác sĩ – FE join bằng JoinRoleAsync("bac_si")
    ///  - "y_ta"   : tất cả y tá hành chính (quản lý lịch, thu ngân, phát thuốc, hỗ trợ phòng khám...)
    ///               – FE join bằng JoinRoleAsync("y_ta")
    ///
    /// USER GROUPS (cho thông báo cá nhân, phiếu khám của bác sĩ cụ thể):
    ///  - RealtimeHub.GetUserGroupName(loaiNguoiDung, maNguoiDung)
    ///    Ví dụ: "user:bac_si:BS001", "user:y_ta:YT002"
    ///
    /// ROOM GROUPS (cho hàng đợi, phiếu khám, CLS theo phòng):
    ///  - RealtimeHub.GetRoomGroupName(maPhong)
    ///    Ví dụ: "room:PK01", "room:XN01"
    ///
    /// FILTERING RULES:
    ///  - Clinical Exams: Chỉ gửi cho bác sĩ được chỉ định + y tá trong phòng
    ///  - CLS Orders: Chỉ gửi cho bác sĩ yêu cầu + y tá phòng CLS + y tá phòng LS nhận kết quả
    ///  - Visits: Chỉ gửi cho bác sĩ khám + nhân sự trong phòng
    ///  - Appointments: Chỉ gửi cho bác sĩ được chỉ định + tất cả y tá (quản lý lịch)
    ///  - Invoices: Chỉ gửi cho y tá (xử lý thanh toán)
    ///  - Prescriptions: Chỉ gửi cho bác sĩ kê đơn + y tá (xử lý phát thuốc)
    ///  - Dashboard/KPI/Patient CRUD: Gửi cho tất cả nhân sự (bac_si + y_ta)
    ///  - Queue: Gửi theo phòng (room group)
    ///  - Notifications: Gửi theo người nhận cụ thể hoặc role
    /// </summary>
    public class RealtimeService(IHubContext<RealtimeHub, IRealtimeClient> hub) : IRealtimeService
    {
        private readonly IHubContext<RealtimeHub, IRealtimeClient> _hub = hub;

        // Tất cả bác sĩ
        private static readonly string DoctorRoleGroupName =
            RealtimeHub.GetRoleGroupName("bac_si");

        // Tất cả y tá (chung)
        private static readonly string NurseRoleGroupName =
            RealtimeHub.GetRoleGroupName("y_ta");

        // Y tá hành chính (quản lý lịch, thu ngân, phát thuốc)
        private static readonly string AdminNurseGroupName =
            RealtimeHub.GetNurseTypeGroupName("hanhchinh");

        // Y tá lâm sàng (hỗ trợ bác sĩ trong phòng khám)
        private static readonly string ClinicalNurseGroupName =
            RealtimeHub.GetNurseTypeGroupName("phong_kham");

        // Y tá cận lâm sàng (xét nghiệm, siêu âm, X-quang...)
        private static readonly string ClsNurseGroupName =
            RealtimeHub.GetNurseTypeGroupName("can_lam_sang");

        // ==============================
        // ===== DASHBOARD / KPI ========
        // ==============================

        // Tất cả nhân sự (bác sĩ + y tá) xem dashboard
        public Task BroadcastDashboardTodayAsync(DashboardTodayDto dashboard)
        {
            var tasks = new List<Task>
            {
                
                _hub.Clients.Group(DoctorRoleGroupName).DashboardTodayUpdated(dashboard),
                _hub.Clients.Group(NurseRoleGroupName).DashboardTodayUpdated(dashboard)
            };
            return Task.WhenAll(tasks);

        }

        public Task BroadcastTodayPatientsKpiAsync(TodayPatientsKpiDto dto)
        {
            var tasks = new List<Task>
            {
               
                _hub.Clients.Group(DoctorRoleGroupName).TodayPatientsKpiUpdated(dto),
                _hub.Clients.Group(NurseRoleGroupName).TodayPatientsKpiUpdated(dto)
            };
            return Task.WhenAll(tasks);
        }

        public Task BroadcastTodayAppointmentsKpiAsync(TodayAppointmentsKpiDto dto)
        {
            var tasks = new List<Task>
            {
               
                _hub.Clients.Group(DoctorRoleGroupName).TodayAppointmentsKpiUpdated(dto),
                _hub.Clients.Group(NurseRoleGroupName).TodayAppointmentsKpiUpdated(dto)
            };
            return Task.WhenAll(tasks);
        }

        public Task BroadcastTodayRevenueKpiAsync(TodayRevenueKpiDto dto)
        {
            var tasks = new List<Task>
            {
               
                _hub.Clients.Group(DoctorRoleGroupName).TodayRevenueKpiUpdated(dto),
                _hub.Clients.Group(NurseRoleGroupName).TodayRevenueKpiUpdated(dto)
            };
            return Task.WhenAll(tasks);
        }

        public Task BroadcastTodayExamOverviewAsync(TodayExamOverviewDto dto)
        {
            var tasks = new List<Task>
            {
               
                _hub.Clients.Group(DoctorRoleGroupName).TodayExamOverviewUpdated(dto),
                _hub.Clients.Group(NurseRoleGroupName).TodayExamOverviewUpdated(dto)
            };
            return Task.WhenAll(tasks);
        }

        public Task BroadcastUpcomingAppointmentsAsync(
            IReadOnlyList<UpcomingAppointmentDashboardItemDto> items)
        {
            var tasks = new List<Task>
            {
                
                _hub.Clients.Group(DoctorRoleGroupName).UpcomingAppointmentsUpdated(items),
                _hub.Clients.Group(NurseRoleGroupName).UpcomingAppointmentsUpdated(items)
            };
            return Task.WhenAll(tasks);
        }

        public Task BroadcastRecentActivitiesAsync(
            IReadOnlyList<DashboardActivityDto> items)
        {
            var tasks = new List<Task>
            {
              
                _hub.Clients.Group(DoctorRoleGroupName).RecentActivitiesUpdated(items),
                _hub.Clients.Group(NurseRoleGroupName).RecentActivitiesUpdated(items)
            };
            return Task.WhenAll(tasks);
        }


        // ==========================
        // ===== BỆNH NHÂN =========
        // ==========================

        public Task BroadcastPatientCreatedAsync(PatientDto benhNhan)
        {
            var tasks = new List<Task>
            {
               
                _hub.Clients.Group(DoctorRoleGroupName).PatientCreated(benhNhan),
                _hub.Clients.Group(NurseRoleGroupName).PatientCreated(benhNhan)
            };
            return Task.WhenAll(tasks);
        }

        public Task BroadcastPatientUpdatedAsync(PatientDto benhNhan)
        {
            var tasks = new List<Task>
            {
               
                _hub.Clients.Group(DoctorRoleGroupName).PatientUpdated(benhNhan),
                _hub.Clients.Group(NurseRoleGroupName).PatientUpdated(benhNhan)
            };
            return Task.WhenAll(tasks);
        }

        public Task BroadcastPatientStatusUpdatedAsync(PatientDto benhNhan)
        {
            var tasks = new List<Task>
            {
                
                _hub.Clients.Group(DoctorRoleGroupName).PatientStatusUpdated(benhNhan),
                _hub.Clients.Group(NurseRoleGroupName).PatientStatusUpdated(benhNhan)
            };
            return Task.WhenAll(tasks);
        }


        // =====================================
        // ===== KHÁM BỆNH (LÂM SÀNG) =========
        // =====================================

        public Task BroadcastClinicalExamCreatedAsync(ClinicalExamDto phieuKham)
        {
            var tasks = new List<Task>();

            // ✅ Chỉ gửi cho bác sĩ được chỉ định (nếu có)
            if (!string.IsNullOrWhiteSpace(phieuKham.MaBacSiKham))
            {
                var doctorGroup = RealtimeHub.GetUserGroupName("bac_si", phieuKham.MaBacSiKham);
                tasks.Add(_hub.Clients.Group(doctorGroup).ClinicalExamCreated(phieuKham));
            }

            // ✅ Gửi cho y tá trong phòng khám (nếu có)
            if (!string.IsNullOrWhiteSpace(phieuKham.MaPhong))
            {
                var roomGroup = RealtimeHub.GetRoomGroupName(phieuKham.MaPhong);
                tasks.Add(_hub.Clients.Group(roomGroup).ClinicalExamCreated(phieuKham));
            }

            return tasks.Count > 0 ? Task.WhenAll(tasks) : Task.CompletedTask;
        }

        public Task BroadcastClinicalExamUpdatedAsync(ClinicalExamDto phieuKham)
        {
            var tasks = new List<Task>();

            // ✅ Chỉ gửi cho bác sĩ được chỉ định (nếu có)
            if (!string.IsNullOrWhiteSpace(phieuKham.MaBacSiKham))
            {
                var doctorGroup = RealtimeHub.GetUserGroupName("bac_si", phieuKham.MaBacSiKham);
                tasks.Add(_hub.Clients.Group(doctorGroup).ClinicalExamUpdated(phieuKham));
            }

            // ✅ Gửi cho y tá trong phòng khám (nếu có)
            if (!string.IsNullOrWhiteSpace(phieuKham.MaPhong))
            {
                var roomGroup = RealtimeHub.GetRoomGroupName(phieuKham.MaPhong);
                tasks.Add(_hub.Clients.Group(roomGroup).ClinicalExamUpdated(phieuKham));
            }

            return tasks.Count > 0 ? Task.WhenAll(tasks) : Task.CompletedTask;
        }

        public Task BroadcastFinalDiagnosisChangedAsync(FinalDiagnosisDto chanDoan)
        {
            // ⚠️ FinalDiagnosisDto hiện tại không có MaBacSi và MaPhong
            // Tạm thời không gửi realtime cho đến khi DTO được bổ sung thông tin
            // TODO: Bổ sung MaBacSi và MaPhong vào FinalDiagnosisDto để filter chính xác
            return Task.CompletedTask;
        }


        // ======================================
        // ===== CẬN LÂM SÀNG (CLS SERVICE) =====
        // ======================================

        public Task BroadcastClsOrderCreatedAsync(ClsOrderDto phieuCls)
        {
            var tasks = new List<Task>();

            // ✅ Gửi cho người lập phiếu CLS (thường là bác sĩ yêu cầu)
            if (!string.IsNullOrWhiteSpace(phieuCls.MaNguoiLap))
            {
                var doctorGroup = RealtimeHub.GetUserGroupName("bac_si", phieuCls.MaNguoiLap);
                tasks.Add(_hub.Clients.Group(doctorGroup).ClsOrderCreated(phieuCls));
            }

            // ✅ Gửi cho y tá trong các phòng CLS thực hiện
            if (phieuCls.ListItemDV != null)
            {
                var clsRooms = phieuCls.ListItemDV
                    .Where(item => !string.IsNullOrWhiteSpace(item.MaPhong))
                    .Select(item => item.MaPhong)
                    .Distinct();

                foreach (var room in clsRooms)
                {
                    var roomGroup = RealtimeHub.GetRoomGroupName(room);
                    tasks.Add(_hub.Clients.Group(roomGroup).ClsOrderCreated(phieuCls));
                }
            }

            return tasks.Count > 0 ? Task.WhenAll(tasks) : Task.CompletedTask;
        }

        public Task BroadcastClsOrderUpdatedAsync(ClsOrderDto phieuCls)
        {
            var tasks = new List<Task>();

            // ✅ Gửi cho người lập phiếu CLS (thường là bác sĩ yêu cầu)
            if (!string.IsNullOrWhiteSpace(phieuCls.MaNguoiLap))
            {
                var doctorGroup = RealtimeHub.GetUserGroupName("bac_si", phieuCls.MaNguoiLap);
                tasks.Add(_hub.Clients.Group(doctorGroup).ClsOrderUpdated(phieuCls));
            }

            // ✅ Gửi cho y tá trong các phòng CLS thực hiện
            if (phieuCls.ListItemDV != null)
            {
                var clsRooms = phieuCls.ListItemDV
                    .Where(item => !string.IsNullOrWhiteSpace(item.MaPhong))
                    .Select(item => item.MaPhong)
                    .Distinct();

                foreach (var room in clsRooms)
                {
                    var roomGroup = RealtimeHub.GetRoomGroupName(room);
                    tasks.Add(_hub.Clients.Group(roomGroup).ClsOrderUpdated(phieuCls));
                }
            }

            return tasks.Count > 0 ? Task.WhenAll(tasks) : Task.CompletedTask;
        }

        public Task BroadcastClsOrderStatusUpdatedAsync(ClsOrderDto phieuCls)
        {
            var tasks = new List<Task>();

            // ✅ Gửi cho người lập phiếu CLS (thường là bác sĩ yêu cầu)
            if (!string.IsNullOrWhiteSpace(phieuCls.MaNguoiLap))
            {
                var doctorGroup = RealtimeHub.GetUserGroupName("bac_si", phieuCls.MaNguoiLap);
                tasks.Add(_hub.Clients.Group(doctorGroup).ClsOrderStatusUpdated(phieuCls));
            }

            // ✅ Gửi cho y tá trong các phòng CLS thực hiện
            if (phieuCls.ListItemDV != null)
            {
                var clsRooms = phieuCls.ListItemDV
                    .Where(item => !string.IsNullOrWhiteSpace(item.MaPhong))
                    .Select(item => item.MaPhong)
                    .Distinct();

                foreach (var room in clsRooms)
                {
                    var roomGroup = RealtimeHub.GetRoomGroupName(room);
                    tasks.Add(_hub.Clients.Group(roomGroup).ClsOrderStatusUpdated(phieuCls));
                }
            }

            return tasks.Count > 0 ? Task.WhenAll(tasks) : Task.CompletedTask;
        }

        public Task BroadcastClsResultCreatedAsync(ClsResultDto ketQua)
        {
            // ⚠️ ClsResultDto không có MaPhong
            // Tạm thời không gửi realtime cho đến khi DTO được bổ sung thông tin
            // TODO: Bổ sung MaPhong vào ClsResultDto để filter chính xác
            return Task.CompletedTask;
        }

        public Task BroadcastClsSummaryCreatedAsync(ClsSummaryDto tongHop)
        {
            // ⚠️ ClsSummaryDto không có MaPhong
            // Tạm thời không gửi realtime cho đến khi DTO được bổ sung thông tin
            // TODO: Bổ sung MaPhong vào ClsSummaryDto để filter chính xác
            return Task.CompletedTask;
        }

        public Task BroadcastClsSummaryUpdatedAsync(ClsSummaryDto tongHop)
        {
            // ⚠️ ClsSummaryDto không có MaPhong
            // Tạm thời không gửi realtime cho đến khi DTO được bổ sung thông tin
            // TODO: Bổ sung MaPhong vào ClsSummaryDto để filter chính xác
            return Task.CompletedTask;
        }

        public Task BroadcastClsItemUpdatedAsync(ClsItemDto item)
        {
            var tasks = new List<Task>();

            // ✅ Gửi cho phòng CLS thực hiện (nếu có)
            if (!string.IsNullOrWhiteSpace(item.MaPhong))
            {
                var roomGroup = RealtimeHub.GetRoomGroupName(item.MaPhong);
                tasks.Add(_hub.Clients.Group(roomGroup).ClsItemUpdated(item));
            }

            return tasks.Count > 0 ? Task.WhenAll(tasks) : Task.CompletedTask;
        }


        // =====================================
        // ===== LƯỢT KHÁM (VISIT / HISTORY) ===
        // =====================================

        public Task BroadcastVisitCreatedAsync(HistoryVisitRecordDto luotKham)
        {
            var tasks = new List<Task>();

            // ✅ Gửi cho bác sĩ khám (nếu có)
            if (!string.IsNullOrWhiteSpace(luotKham.MaBacSi))
            {
                var doctorGroup = RealtimeHub.GetUserGroupName("bac_si", luotKham.MaBacSi);
                tasks.Add(_hub.Clients.Group(doctorGroup).VisitCreated(luotKham));
            }

            // ⚠️ HistoryVisitRecordDto không có MaPhong
            // Chỉ gửi cho bác sĩ khám, không gửi cho phòng

            return tasks.Count > 0 ? Task.WhenAll(tasks) : Task.CompletedTask;
        }

        public Task BroadcastVisitStatusUpdatedAsync(HistoryVisitRecordDto luotKham)
        {
            var tasks = new List<Task>();

            // ✅ Gửi cho bác sĩ khám (nếu có)
            if (!string.IsNullOrWhiteSpace(luotKham.MaBacSi))
            {
                var doctorGroup = RealtimeHub.GetUserGroupName("bac_si", luotKham.MaBacSi);
                tasks.Add(_hub.Clients.Group(doctorGroup).VisitStatusUpdated(luotKham));
            }

            // ⚠️ HistoryVisitRecordDto không có MaPhong
            // Chỉ gửi cho bác sĩ khám, không gửi cho phòng

            return tasks.Count > 0 ? Task.WhenAll(tasks) : Task.CompletedTask;
        }


        // ==========================
        // ===== HÀNG ĐỢI (QUEUE) ===
        // ==========================

        // Hàng đợi gửi theo phòng, FE join group theo phòng.
        public Task BroadcastQueueByRoomAsync(
            string maPhong,
            IReadOnlyList<QueueItemDto> items)
        {
            var groupName = RealtimeHub.GetRoomGroupName(maPhong);
            return _hub.Clients.Group(groupName).QueueByRoomUpdated(items);
        }

        public Task BroadcastQueueItemChangedAsync(QueueItemDto item)
        {
            if (string.IsNullOrWhiteSpace(item.MaPhong))
                return Task.CompletedTask;

            var groupName = RealtimeHub.GetRoomGroupName(item.MaPhong);
            return _hub.Clients.Group(groupName).QueueItemChanged(item);
        }


        // ==========================
        // ===== LỊCH HẸN ==========
        // ==========================

        public Task BroadcastAppointmentChangedAsync(
            AppointmentReadRequestDto lichHen)
        {
            var tasks = new List<Task>();

            // ✅ Gửi cho bác sĩ được chỉ định (nếu có)
            if (!string.IsNullOrWhiteSpace(lichHen.MaBacSiKham))
            {
                var doctorUserGroup = RealtimeHub.GetUserGroupName("bac_si", lichHen.MaBacSiKham);
                tasks.Add(_hub.Clients.Group(doctorUserGroup).AppointmentChanged(lichHen));
            }

            // ✅ Gửi cho y tá hành chính (quản lý lịch hẹn) - KHÔNG gửi cho y tá LS/CLS
            tasks.Add(_hub.Clients.Group(AdminNurseGroupName).AppointmentChanged(lichHen));

            return Task.WhenAll(tasks);
        }


        // ===================================
        // ===== HOÁ ĐƠN / THANH TOÁN =======
        // ===================================

        public Task BroadcastInvoiceChangedAsync(InvoiceDto hoaDon)
        {
            // ✅ Chỉ gửi cho y tá hành chính (xử lý thanh toán) - KHÔNG gửi cho bác sĩ, y tá LS, y tá CLS
            return _hub.Clients.Group(AdminNurseGroupName).InvoiceChanged(hoaDon);
        }

        // ==========================
        // ===== PHARMACY ===========
        // ==========================

        public Task BroadcastDrugChangedAsync(DrugDto thuoc)
        {
            // ✅ Kho thuốc do y tá hành chính quản lý - KHÔNG gửi cho y tá LS/CLS
            return _hub.Clients
                .Group(AdminNurseGroupName)
                .DrugChanged(thuoc);
        }

        public Task BroadcastPrescriptionCreatedAsync(PrescriptionDto donThuoc)
        {
            var tasks = new List<Task>();

            // ✅ Gửi cho bác sĩ kê đơn (nếu có)
            if (!string.IsNullOrWhiteSpace(donThuoc.MaBacSiKeDon))
            {
                var doctorGroup = RealtimeHub.GetUserGroupName("bac_si", donThuoc.MaBacSiKeDon);
                tasks.Add(_hub.Clients.Group(doctorGroup).PrescriptionCreated(donThuoc));
            }

            // ✅ Gửi cho y tá hành chính (xử lý phát thuốc) - KHÔNG gửi cho y tá LS/CLS
            tasks.Add(_hub.Clients.Group(AdminNurseGroupName).PrescriptionCreated(donThuoc));

            return Task.WhenAll(tasks);
        }

        public Task BroadcastPrescriptionStatusUpdatedAsync(PrescriptionDto donThuoc)
        {
            var tasks = new List<Task>();

            // ✅ Gửi cho bác sĩ kê đơn (nếu có)
            if (!string.IsNullOrWhiteSpace(donThuoc.MaBacSiKeDon))
            {
                var doctorGroup = RealtimeHub.GetUserGroupName("bac_si", donThuoc.MaBacSiKeDon);
                tasks.Add(_hub.Clients.Group(doctorGroup).PrescriptionStatusUpdated(donThuoc));
            }

            // ✅ Gửi cho y tá hành chính (xử lý phát thuốc) - KHÔNG gửi cho y tá LS/CLS
            tasks.Add(_hub.Clients.Group(AdminNurseGroupName).PrescriptionStatusUpdated(donThuoc));

            return Task.WhenAll(tasks);
        }
        // ==========================
        // ===== THÔNG BÁO =========
        // ==========================

        public Task BroadcastNotificationCreatedAsync(NotificationDto thongBao)
        {
            var loai = (thongBao.LoaiNguoiNhan ?? string.Empty).Trim().ToLowerInvariant();
            var ma = (thongBao.MaNguoiNhan ?? string.Empty).Trim();

            // Không chỉ đích danh → broadcast theo role
            if (string.IsNullOrWhiteSpace(ma))
            {
                // Không chỉ rõ loại → gửi cho toàn bộ nhân sự (bác sĩ + y tá)
                if (string.IsNullOrWhiteSpace(loai) ||
                    loai is "nhan_su" or "staff" or "nhan_vien_y_te")
                {
                    return Task.WhenAll(
                        _hub.Clients.Group(DoctorRoleGroupName).NotificationCreated(thongBao),
                        _hub.Clients.Group(NurseRoleGroupName).NotificationCreated(thongBao)
                    );
                }

                if (loai == "bac_si")
                {
                    return _hub.Clients.Group(DoctorRoleGroupName)
                        .NotificationCreated(thongBao);
                }

                // Y tá hành chính quy về y_ta
                if (loai is "y_ta" or "thu_ngan" or "phat_thuoc")
                {
                    return _hub.Clients.Group(NurseRoleGroupName).NotificationCreated(thongBao);
                }

                // Loại khác không rõ → fallback toàn bộ nhân sự
                return Task.WhenAll(
                    _hub.Clients.Group(DoctorRoleGroupName).NotificationCreated(thongBao),
                    _hub.Clients.Group(NurseRoleGroupName).NotificationCreated(thongBao)
                );
            }

            // Có mã người nhận → gửi theo user group (bac_si / y_ta)
            var userGroup = RealtimeHub.GetUserGroupName(thongBao.LoaiNguoiNhan ?? "", thongBao.MaNguoiNhan??"");
            return _hub.Clients.Group(userGroup).NotificationCreated(thongBao);
        }


        public Task BroadcastNotificationUpdatedAsync(NotificationDto thongBao)
        {
            var loai = (thongBao.LoaiNguoiNhan ?? string.Empty).Trim().ToLowerInvariant();
            var ma = (thongBao.MaNguoiNhan ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(ma))
            {
                if (string.IsNullOrWhiteSpace(loai) ||
                    loai is "nhan_su" or "staff" or "nhan_vien_y_te")
                {
                    return Task.WhenAll(
                        _hub.Clients.Group(DoctorRoleGroupName).NotificationUpdated(thongBao),
                        _hub.Clients.Group(NurseRoleGroupName).NotificationUpdated(thongBao)
                    );
                }

                if (loai == "bac_si")
                {
                    return _hub.Clients.Group(DoctorRoleGroupName).NotificationUpdated(thongBao);
                }

                if (loai is "y_ta" or "thu_ngan" or "phat_thuoc")
                {
                    return _hub.Clients.Group(NurseRoleGroupName).NotificationUpdated(thongBao);
                }

                // fallback gửi toàn bộ nhân sự
                return Task.WhenAll(
                    _hub.Clients.Group(DoctorRoleGroupName).NotificationUpdated(thongBao),
                    _hub.Clients.Group(NurseRoleGroupName).NotificationUpdated(thongBao)
                );
            }

            var userGroup = RealtimeHub.GetUserGroupName(thongBao.LoaiNguoiNhan ?? "", thongBao.MaNguoiNhan ?? "");
            return _hub.Clients.Group(userGroup).NotificationUpdated(thongBao);
        }

    }
}
