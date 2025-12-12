using System.Collections.Generic;
using System.Threading.Tasks;
using HealthCare.DTOs;
using HealthCare.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace HealthCare.Realtime
{
    /// <summary>
    /// Facade dùng từ các service nghiệp vụ để bắn realtime (SignalR).
    /// Quy ước group (theo role – đơn giản, tránh double event):
    ///  - "bac_si" : tất cả tài khoản bác sĩ – FE nên JoinRoleAsync("bac_si") nếu user là bác sĩ.
    ///  - "y_ta"   : tất cả tài khoản y tá (bao gồm y tá hành chính, thu ngân, phát thuốc,
    ///              điều dưỡng hỗ trợ phòng khám...) – FE nên JoinRoleAsync("y_ta") nếu user thuộc nhóm này.
    ///
    /// Không còn sử dụng group "staff". Mọi realtime gửi cho toàn bộ nhân sự sẽ broadcast
    /// đồng thời cho cả 2 group "bac_si" và "y_ta".
    ///
    /// Ngoài ra vẫn có group theo user / phòng:
    ///  - RealtimeHub.GetUserGroupName(loaiNguoiDung, maNguoiDung)
    ///  - RealtimeHub.GetRoomGroupName(maPhong)
    /// </summary>
    public class RealtimeService(IHubContext<RealtimeHub, IRealtimeClient> hub) : IRealtimeService
    {
        private readonly IHubContext<RealtimeHub, IRealtimeClient> _hub = hub;

        // Tất cả bác sĩ
        private static readonly string DoctorRoleGroupName =
            RealtimeHub.GetRoleGroupName("bac_si");

        // Tất cả y tá (hành chính + hỗ trợ phòng khám + thu ngân + phát thuốc...)
        private static readonly string NurseRoleGroupName =
            RealtimeHub.GetRoleGroupName("y_ta");

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
            var tasks = new List<Task>
            {
               
                _hub.Clients.Group(DoctorRoleGroupName).ClinicalExamCreated(phieuKham),
                _hub.Clients.Group(NurseRoleGroupName).ClinicalExamCreated(phieuKham)
            };

            if (!string.IsNullOrWhiteSpace(phieuKham.MaPhong))
            {
                var roomGroup = RealtimeHub.GetRoomGroupName(phieuKham.MaPhong);
                tasks.Add(_hub.Clients.Group(roomGroup).ClinicalExamCreated(phieuKham));
            }

            return Task.WhenAll(tasks);
        }

        public Task BroadcastClinicalExamUpdatedAsync(ClinicalExamDto phieuKham)
        {
            var tasks = new List<Task>
            {
               
                _hub.Clients.Group(DoctorRoleGroupName).ClinicalExamUpdated(phieuKham),
                _hub.Clients.Group(NurseRoleGroupName).ClinicalExamUpdated(phieuKham)
            };

            if (!string.IsNullOrWhiteSpace(phieuKham.MaPhong))
            {
                var roomGroup = RealtimeHub.GetRoomGroupName(phieuKham.MaPhong);
                tasks.Add(_hub.Clients.Group(roomGroup).ClinicalExamUpdated(phieuKham));
            }

            return Task.WhenAll(tasks);
        }

        public Task BroadcastFinalDiagnosisChangedAsync(FinalDiagnosisDto chanDoan)
        {
            var tasks = new List<Task>
            {
                
                _hub.Clients.Group(DoctorRoleGroupName).FinalDiagnosisChanged(chanDoan),
                _hub.Clients.Group(NurseRoleGroupName).FinalDiagnosisChanged(chanDoan)
            };
            return Task.WhenAll(tasks);
        }


        // ======================================
        // ===== CẬN LÂM SÀNG (CLS SERVICE) =====
        // ======================================

        public Task BroadcastClsOrderCreatedAsync(ClsOrderDto phieuCls)
        {
            var tasks = new List<Task>
            {
               
                _hub.Clients.Group(DoctorRoleGroupName).ClsOrderCreated(phieuCls),
                _hub.Clients.Group(NurseRoleGroupName).ClsOrderCreated(phieuCls)
            };

            var roomForCls = phieuCls.ListItemDV?.FirstOrDefault()?.MaPhong;
            if (!string.IsNullOrWhiteSpace(roomForCls))
            {
                var roomGroup = RealtimeHub.GetRoomGroupName(roomForCls);
                tasks.Add(_hub.Clients.Group(roomGroup).ClsOrderCreated(phieuCls));
            }

            return Task.WhenAll(tasks);
        }

        public Task BroadcastClsOrderUpdatedAsync(ClsOrderDto phieuCls)
        {
            var tasks = new List<Task>
            {
                
                _hub.Clients.Group(DoctorRoleGroupName).ClsOrderUpdated(phieuCls),
                _hub.Clients.Group(NurseRoleGroupName).ClsOrderUpdated(phieuCls)
            };

            var roomForClsUpd = phieuCls.ListItemDV?.FirstOrDefault()?.MaPhong;
            if (!string.IsNullOrWhiteSpace(roomForClsUpd))
            {
                var roomGroup = RealtimeHub.GetRoomGroupName(roomForClsUpd);
                tasks.Add(_hub.Clients.Group(roomGroup).ClsOrderUpdated(phieuCls));
            }

            return Task.WhenAll(tasks);
        }

        public Task BroadcastClsOrderStatusUpdatedAsync(ClsOrderDto phieuCls)
        {
            var tasks = new List<Task>
            {
                
                _hub.Clients.Group(DoctorRoleGroupName).ClsOrderStatusUpdated(phieuCls),
                _hub.Clients.Group(NurseRoleGroupName).ClsOrderStatusUpdated(phieuCls)
            };

            var roomForClsStatus = phieuCls.ListItemDV?.FirstOrDefault()?.MaPhong;
            if (!string.IsNullOrWhiteSpace(roomForClsStatus))
            {
                var roomGroup = RealtimeHub.GetRoomGroupName(roomForClsStatus);
                tasks.Add(_hub.Clients.Group(roomGroup).ClsOrderStatusUpdated(phieuCls));
            }

            return Task.WhenAll(tasks);
        }

        public Task BroadcastClsResultCreatedAsync(ClsResultDto ketQua)
        {
            var tasks = new List<Task>
            {
               
                _hub.Clients.Group(DoctorRoleGroupName).ClsResultCreated(ketQua),
                _hub.Clients.Group(NurseRoleGroupName).ClsResultCreated(ketQua)
            };
            return Task.WhenAll(tasks);
        }

        public Task BroadcastClsSummaryCreatedAsync(ClsSummaryDto tongHop)
        {
            var tasks = new List<Task>
            {
               
                _hub.Clients.Group(DoctorRoleGroupName).ClsSummaryCreated(tongHop),
                _hub.Clients.Group(NurseRoleGroupName).ClsSummaryCreated(tongHop)
            };
            return Task.WhenAll(tasks);
        }

        public Task BroadcastClsSummaryUpdatedAsync(ClsSummaryDto tongHop)
        {
            var tasks = new List<Task>
            {
               
                _hub.Clients.Group(DoctorRoleGroupName).ClsSummaryUpdated(tongHop),
                _hub.Clients.Group(NurseRoleGroupName).ClsSummaryUpdated(tongHop)
            };
            return Task.WhenAll(tasks);
        }

        public Task BroadcastClsItemUpdatedAsync(ClsItemDto item)
        {
            var tasks = new List<Task>
            {
                _hub.Clients.Group(DoctorRoleGroupName).ClsItemUpdated(item),
                _hub.Clients.Group(NurseRoleGroupName).ClsItemUpdated(item)
            };
            return Task.WhenAll(tasks);
        }


        // =====================================
        // ===== LƯỢT KHÁM (VISIT / HISTORY) ===
        // =====================================

        public Task BroadcastVisitCreatedAsync(HistoryVisitRecordDto luotKham)
        {
            var tasks = new List<Task>
            {
                
                _hub.Clients.Group(DoctorRoleGroupName).VisitCreated(luotKham),
                _hub.Clients.Group(NurseRoleGroupName).VisitCreated(luotKham)
            };
            return Task.WhenAll(tasks);
        }

        public Task BroadcastVisitStatusUpdatedAsync(HistoryVisitRecordDto luotKham)
        {
            var tasks = new List<Task>
            {
                
                _hub.Clients.Group(DoctorRoleGroupName).VisitStatusUpdated(luotKham),
                _hub.Clients.Group(NurseRoleGroupName).VisitStatusUpdated(luotKham)
            };
            return Task.WhenAll(tasks);
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
            var tasks = new List<Task>
            {
                // Dashboard + màn Lịch hẹn cho toàn bộ nhân sự
               
                _hub.Clients.Group(DoctorRoleGroupName).AppointmentChanged(lichHen),
                _hub.Clients.Group(NurseRoleGroupName).AppointmentChanged(lichHen)
            };

            // Gửi thêm cho đúng bác sĩ nếu DTO có MaBacSiKham
            if (!string.IsNullOrWhiteSpace(lichHen.MaBacSiKham))
            {
                var doctorUserGroup =
                    RealtimeHub.GetUserGroupName("bac_si", lichHen.MaBacSiKham);
                tasks.Add(_hub.Clients.Group(doctorUserGroup).AppointmentChanged(lichHen));
            }

            return Task.WhenAll(tasks);
        }


        // ===================================
        // ===== HOÁ ĐƠN / THANH TOÁN =======
        // ===================================

        public Task BroadcastInvoiceChangedAsync(InvoiceDto hoaDon)
        {
            var tasks = new List<Task>
            {
               
                _hub.Clients.Group(DoctorRoleGroupName).InvoiceChanged(hoaDon),
                _hub.Clients.Group(NurseRoleGroupName).InvoiceChanged(hoaDon)
            };
            return Task.WhenAll(tasks);
        }

        // ==========================
        // ===== PHARMACY ===========
        // ==========================

        public Task BroadcastDrugChangedAsync(DrugDto thuoc)
        {
            // Kho thuốc chủ yếu do y tá / hành chính sử dụng
            return _hub.Clients
                .Group(NurseRoleGroupName)
                .DrugChanged(thuoc);
        }

        public Task BroadcastPrescriptionCreatedAsync(PrescriptionDto donThuoc)
        {
            // Đơn thuốc mới: cả bác sĩ (người kê) và y tá (phát thuốc) đều quan tâm
            var tasks = new List<Task>
            {
                _hub.Clients.Group(DoctorRoleGroupName).PrescriptionCreated(donThuoc),
                _hub.Clients.Group(NurseRoleGroupName).PrescriptionCreated(donThuoc)
            };
            return Task.WhenAll(tasks);
        }

        public Task BroadcastPrescriptionStatusUpdatedAsync(PrescriptionDto donThuoc)
        {
            // Trạng thái đơn thay đổi (ví dụ: da_phat) → thông báo cho cả hai bên
            var tasks = new List<Task>
            {
                _hub.Clients.Group(DoctorRoleGroupName).PrescriptionStatusUpdated(donThuoc),
                _hub.Clients.Group(NurseRoleGroupName).PrescriptionStatusUpdated(donThuoc)
            };
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

                // Y tá + nhóm hành chính quy về y_ta
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

            // Có mã người nhận → gửi theo user group (bac_si / y_ta / thu_ngan / phat_thuoc...)
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
