using Microsoft.Data.SqlClient;

namespace BaseBusiness.util
{
    /// <summary>
    /// "Include Roomsharer" của Reservation Alerts (giống WinForm): alert gốc được chép sang các booking ở chung phòng
    /// (cùng Reservation.ShareRoom). Bản chép có OriginAlertID = ID alert gốc; alert gốc có OriginAlertID = chính nó.
    /// Bỏ qua booking ở chung đã Checked Out (2), Cancel (3), No Show (7).
    /// </summary>
    public static class ReservationAlertUtil
    {
        private const string SharerFilter = "ISNULL(r2.Status, 0) NOT IN (2, 3, 7)";

        /// <summary>Chép alert gốc sang các roomsharer chưa có bản chép. Trả về số dòng được thêm.</summary>
        public static int CopyToRoomSharers(int originAlertId)
        {
            return Exec($@"
                INSERT INTO ReservationAlerts (ReservationID, Code, Area, Description, UserInsertID, CreateDate,
                                               UserUpdateID, UpdateDate, OriginAlertID, WarningDay, IsActive)
                SELECT r2.ID, a.Code, a.Area, a.Description, a.UserInsertID, GETDATE(),
                       a.UserUpdateID, GETDATE(), a.ID, a.WarningDay, a.IsActive
                  FROM ReservationAlerts a
                  JOIN Reservation r1 ON r1.ID = a.ReservationID
                  JOIN Reservation r2 ON r2.ShareRoom = r1.ShareRoom AND r2.ID <> r1.ID
                 WHERE a.ID = @ID AND ISNULL(r1.ShareRoom, 0) > 0 AND {SharerFilter}
                   AND NOT EXISTS (SELECT 1 FROM ReservationAlerts c
                                    WHERE c.OriginAlertID = a.ID AND c.ReservationID = r2.ID)", originAlertId);
        }

        /// <summary>Cập nhật nội dung các bản chép theo alert gốc.</summary>
        public static int UpdateCopies(int originAlertId)
        {
            return Exec(@"
                UPDATE c SET c.Code = a.Code, c.Area = a.Area, c.Description = a.Description,
                             c.WarningDay = a.WarningDay, c.IsActive = a.IsActive,
                             c.UserUpdateID = a.UserUpdateID, c.UpdateDate = GETDATE()
                  FROM ReservationAlerts c
                  JOIN ReservationAlerts a ON a.ID = c.OriginAlertID
                 WHERE a.ID = @ID AND c.ID <> a.ID", originAlertId);
        }

        /// <summary>Xóa các bản chép của alert gốc.</summary>
        public static int DeleteCopies(int originAlertId)
        {
            return Exec("DELETE FROM ReservationAlerts WHERE OriginAlertID = @ID AND ID <> @ID", originAlertId);
        }

        /// <summary>Alert gốc đã được chép sang roomsharer chưa (để tick lại checkbox khi Edit).</summary>
        public static bool HasCopies(int originAlertId)
        {
            using var cn = new SqlConnection(DBUtils.GetDBConnectionString());
            using var cmd = new SqlCommand(
                "SELECT COUNT(1) FROM ReservationAlerts WHERE OriginAlertID = @ID AND ID <> @ID", cn);
            cmd.Parameters.AddWithValue("@ID", originAlertId);
            cn.Open();
            return System.Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private static int Exec(string sql, int id)
        {
            using var cn = new SqlConnection(DBUtils.GetDBConnectionString());
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@ID", id);
            cn.Open();
            return cmd.ExecuteNonQuery();
        }
    }
}
