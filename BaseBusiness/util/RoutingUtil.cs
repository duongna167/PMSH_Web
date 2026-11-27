using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace BaseBusiness.util
{
    /// <summary>
    /// Hỗ trợ Routing Instructions (port từ WinForm frmRoutingInstructions):
    /// cờ ReservationOptions.Routing (nút Routing sáng trên Reservation) và tìm booking theo Conf.No.
    /// </summary>
    public static class RoutingUtil
    {
        /// <summary>Bật/tắt cờ Routing trong ReservationOptions (WinForm: ClassReservation.ClearButton(id, "Routing") khi tắt).</summary>
        public static void SetRoutingFlag(int reservationId, bool on)
        {
            if (reservationId <= 0) return;
            using var cn = new SqlConnection(DBUtils.GetDBConnectionString());
            using var cmd = new SqlCommand(@"
                IF EXISTS (SELECT 1 FROM ReservationOptions WHERE ReservationID = @ID)
                    UPDATE ReservationOptions SET Routing = @On WHERE ReservationID = @ID
                ELSE IF @On = 1
                    INSERT INTO ReservationOptions (ReservationID, Routing) VALUES (@ID, 1)", cn);
            cmd.Parameters.AddWithValue("@ID", reservationId);
            cmd.Parameters.AddWithValue("@On", on ? 1 : 0);
            cn.Open();
            cmd.ExecuteNonQuery();
        }

        /// <summary>Số routing còn lại của booking (gồm routing Master Folio theo Conf.No như spReservationRouting).</summary>
        public static int CountRouting(int reservationId, string confirmationNo)
        {
            using var cn = new SqlConnection(DBUtils.GetDBConnectionString());
            using var cmd = new SqlCommand(@"
                SELECT COUNT(1) FROM Routing WITH (NOLOCK)
                 WHERE FromReservationID = @ID
                    OR (ToFolioNo = -1 AND @Conf <> '' AND ConfirmationNo = @Conf)", cn);
            cmd.Parameters.AddWithValue("@ID", reservationId);
            cmd.Parameters.AddWithValue("@Conf", confirmationNo ?? "");
            cn.Open();
            return System.Convert.ToInt32(cmd.ExecuteScalar());
        }

        public class RoutingMaster
        {
            public int ReservationID { get; set; }
            public int ProfileID { get; set; }
            public string Name { get; set; } = "";
            public string Address { get; set; } = "";
        }

        /// <summary>
        /// Booking Master (ReservationNo = 0) của Conf.No. Tên = profile Agent / Company / Source / Group của master
        /// (vd "ARENA WEBSITE"), không có thì lấy tên booking master bỏ "* ..., Master *".
        /// </summary>
        public static List<RoutingMaster> GetMasters(string confirmationNo)
        {
            var list = new List<RoutingMaster>();
            if (string.IsNullOrWhiteSpace(confirmationNo)) return list;
            using var cn = new SqlConnection(DBUtils.GetDBConnectionString());
            using var cmd = new SqlCommand(@"
                SELECT r.ID,
                       ISNULL(p.ID, 0) AS ProfileID,
                       ISNULL(NULLIF(p.Account, ''), r.LastName) AS Name,
                       ISNULL(p.Address, '') AS Address
                  FROM Reservation r WITH (NOLOCK)
                  OUTER APPLY (SELECT TOP 1 x.PID FROM (VALUES (1, r.ProfileAgentID), (2, r.ProfileCompanyID),
                                       (3, r.ProfileSourceID), (4, r.ProfileGroupID), (5, r.ProfileIndividualID)) x(Ord, PID)
                                WHERE ISNULL(x.PID, 0) > 0 ORDER BY x.Ord) pid
                  LEFT JOIN Profile p WITH (NOLOCK) ON p.ID = pid.PID
                 WHERE r.ConfirmationNo = @Conf AND r.ReservationNo = 0
                 ORDER BY r.ID", cn);
            cmd.Parameters.AddWithValue("@Conf", confirmationNo);
            cn.Open();
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                string name = rd.IsDBNull(2) ? "" : rd.GetString(2).Trim();
                if (name.StartsWith("*")) name = name.Trim('*', ' ').Replace(", Master", "").Trim();
                list.Add(new RoutingMaster
                {
                    ReservationID = rd.GetInt32(0),
                    ProfileID = rd.GetInt32(1),
                    Name = name,
                    Address = rd.IsDBNull(3) ? "" : rd.GetString(3)
                });
            }
            return list;
        }

        /// <summary>ID các booking cùng Conf.No có ReservationNo > 0 (giống WinForm khi xóa routing Master Folio).</summary>
        public static List<int> GetReservationIdsByConfirmation(string confirmationNo)
        {
            var ids = new List<int>();
            if (string.IsNullOrWhiteSpace(confirmationNo)) return ids;
            using var cn = new SqlConnection(DBUtils.GetDBConnectionString());
            using var cmd = new SqlCommand(
                "SELECT ID FROM Reservation WITH (NOLOCK) WHERE ConfirmationNo = @Conf AND ReservationNo > 0", cn);
            cmd.Parameters.AddWithValue("@Conf", confirmationNo);
            cn.Open();
            using var rd = cmd.ExecuteReader();
            while (rd.Read()) ids.Add(rd.GetInt32(0));
            return ids;
        }
    }
}
