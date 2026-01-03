using System;
using System.Threading.Tasks;
using HealthCare.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HealthCare.Hubs
{
    /// <summary>
    /// Hub trung tâm cho tất cả realtime (Dashboard, Queue, Notification...).
    /// Chạy kiểu strong-typed với IRealtimeClient.
    /// </summary>
    [Authorize]
    public class RealtimeHub : Hub<IRealtimeClient>
    {
        // ===== ROOMS (Queue) =====

        /// <summary>
        /// Client gọi khi mở màn Hàng đợi của 1 phòng.
        /// </summary>
        public Task JoinRoomAsync(string maPhong)
        {
            if (string.IsNullOrWhiteSpace(maPhong))
                throw new ArgumentException("maPhong is required", nameof(maPhong));

            var groupName = GetRoomGroupName(maPhong);
            return Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public Task LeaveRoomAsync(string maPhong)
        {
            if (string.IsNullOrWhiteSpace(maPhong))
                throw new ArgumentException("maPhong is required", nameof(maPhong));

            var groupName = GetRoomGroupName(maPhong);
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        // ===== USER (notification, patient/personal data) =====

        public Task JoinUserAsync(string loaiNguoiNhan, string maNguoiNhan)
        {
            if (string.IsNullOrWhiteSpace(loaiNguoiNhan))
                throw new ArgumentException("loaiNguoiNhan is required", nameof(loaiNguoiNhan));
            if (string.IsNullOrWhiteSpace(maNguoiNhan))
                throw new ArgumentException("maNguoiNhan is required", nameof(maNguoiNhan));

            var groupName = GetUserGroupName(loaiNguoiNhan, maNguoiNhan);
            return Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public Task LeaveUserAsync(string loaiNguoiNhan, string maNguoiNhan)
        {
            if (string.IsNullOrWhiteSpace(loaiNguoiNhan))
                throw new ArgumentException("loaiNguoiNhan is required", nameof(loaiNguoiNhan));
            if (string.IsNullOrWhiteSpace(maNguoiNhan))
                throw new ArgumentException("maNguoiNhan is required", nameof(maNguoiNhan));

            var groupName = GetUserGroupName(loaiNguoiNhan, maNguoiNhan);
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        /// <summary>
        /// FE gọi ngay sau khi connect để join group role tương ứng.
        /// - Bác sĩ    → JoinRoleAsync("bac_si")
        /// - Y tá      → JoinRoleAsync("y_ta") (chung cho tất cả y tá)
        /// 
        /// Ngoài ra, y tá còn join thêm group chi tiết:
        /// - Y tá hành chính → JoinNurseTypeAsync("hanhchinh")
        /// - Y tá lâm sàng   → JoinNurseTypeAsync("phong_kham")
        /// - Y tá CLS        → JoinNurseTypeAsync("can_lam_sang")
        /// </summary>
        public Task JoinRoleAsync(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentException("role is required", nameof(role));

            var groupName = GetRoleGroupName(role);
            return Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        /// <summary>
        /// Join group theo loại y tá cụ thể (hanhchinh, phong_kham, can_lam_sang)
        /// </summary>
        public Task JoinNurseTypeAsync(string nurseType)
        {
            if (string.IsNullOrWhiteSpace(nurseType))
                throw new ArgumentException("nurseType is required", nameof(nurseType));

            var groupName = GetNurseTypeGroupName(nurseType);
            return Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public Task LeaveNurseTypeAsync(string nurseType)
        {
            if (string.IsNullOrWhiteSpace(nurseType))
                throw new ArgumentException("nurseType is required", nameof(nurseType));

            var groupName = GetNurseTypeGroupName(nurseType);
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        public Task LeaveRoleAsync(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentException("role is required", nameof(role));

            var groupName = GetRoleGroupName(role);
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        public override Task OnConnectedAsync()
        {
            // có thể log, hoặc auto join role theo Claims nếu muốn
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            return base.OnDisconnectedAsync(exception);
        }

        // ===== Helper build group name =====

        public static string GetRoomGroupName(string maPhong)
            => $"room:{maPhong}";

        public static string GetNurseTypeGroupName(string nurseType)
            => $"nurse_type:{nurseType}";

        public static string GetUserGroupName(string loaiNguoiNhan, string maNguoiNhan)
            => $"user:{loaiNguoiNhan}:{maNguoiNhan}";

        public static string GetRoleGroupName(string role)
            => $"role:{role}";
    }
}
