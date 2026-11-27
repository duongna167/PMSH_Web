using BaseBusiness.Model;
using Microsoft.Data.SqlClient;
using System;

namespace BaseBusiness.util
{
    /// <summary>
    /// Đồng bộ thông tin Profile sang các bảng lưu bản sao tên khách (Reservation lưu LastName/FirstName riêng lúc tạo booking).
    /// </summary>
    public static class ProfileSyncUtil
    {
        /// <summary>
        /// Sau khi sửa Profile cá nhân (Type = 0): cập nhật tên khách trên các booking đang dùng profile này.
        /// Reservation.LastName = Profile.Account (tên hiển thị dạng "Họ, Tên"), Reservation.FirstName = Profile.Firstname.
        /// Bỏ qua booking đã check-out (Status = 2) để giữ nguyên lịch sử / hóa đơn đã in.
        /// Trả về số booking được cập nhật.
        /// </summary>
        public static int SyncIndividualNameToReservations(ProfileModel profile)
        {
            if (profile == null || profile.ID <= 0 || profile.Type != 0) return 0;
            string account = (profile.Account ?? "").Trim();
            if (account.Length == 0) return 0;

            using var cn = new SqlConnection(DBUtils.GetDBConnectionString());
            using var cmd = new SqlCommand(
                @"UPDATE Reservation
                     SET LastName = @LastName, FirstName = @FirstName
                   WHERE ProfileIndividualID = @ProfileID
                     AND ISNULL(Status, 0) <> 2
                     AND (ISNULL(LastName, '') <> @LastName OR ISNULL(FirstName, '') <> @FirstName)", cn);
            cmd.Parameters.AddWithValue("@LastName", account);
            cmd.Parameters.AddWithValue("@FirstName", (profile.Firstname ?? "").Trim());
            cmd.Parameters.AddWithValue("@ProfileID", profile.ID);
            cn.Open();
            return cmd.ExecuteNonQuery();
        }
    }
}
