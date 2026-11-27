using System;
using System.Collections.Generic;
using System.Data;
using BaseBusiness.util;
using Microsoft.Data.SqlClient;

namespace HouseKeeping.Services.Implements
{
    /// <summary>Màu và ghi chú theo ngày của Room Availability (bảng RoomAvailibilityColor).</summary>
    public partial class HouseKeepingService
    {
        /// <summary>Đặt phòng, OOO/OOS, Business Block của một loại phòng trong một ngày. null: không phải loại phòng.</summary>
        public DataTable? RoomAvailabilityDetail(string roomTypeCode, DateTime date)
        {
            using var connection = new SqlConnection(DBUtils.GetDBConnectionString());
            connection.Open();

            using (var check = new SqlCommand("SELECT COUNT(1) FROM dbo.RoomType WHERE Code = @Code", connection))
            {
                check.Parameters.Add("@Code", SqlDbType.NVarChar, 50).Value = roomTypeCode;
                if (Convert.ToInt32(check.ExecuteScalar()) == 0) return null;
            }

            using var command = new SqlCommand("dbo.spAvailableRoomsResvDetail", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.Add("@RoomTypeCode", SqlDbType.NVarChar, 50).Value = roomTypeCode;
            // yyyyMMdd để SQL Server không hiểu nhầm ngày và tháng
            command.Parameters.Add("@Date", SqlDbType.NVarChar, 60).Value = date.ToString("yyyyMMdd");
            var table = new DataTable();
            using var adapter = new SqlDataAdapter(command);
            adapter.Fill(table);
            return table;
        }

        public DataTable RoomAvailabilityNotes(DateTime fromDate, DateTime toDate)
        {
            using var connection = new SqlConnection(DBUtils.GetDBConnectionString());
            using var command = new SqlCommand(@"
                SELECT ID, [Date], DayOfWeek, ColorName, Description
                FROM dbo.RoomAvailibilityColor WITH (NOLOCK)
                WHERE [Date] >= @From AND [Date] < @End
                ORDER BY [Date], ID", connection);
            command.Parameters.Add("@From", SqlDbType.DateTime).Value = fromDate.Date;
            command.Parameters.Add("@End", SqlDbType.DateTime).Value = toDate.Date.AddDays(1);
            var table = new DataTable();
            using var adapter = new SqlDataAdapter(command);
            adapter.Fill(table);
            return table;
        }

        /// <summary>
        /// Lưu cho nhiều ngày. colorName != null: chỉ đổi màu; description != null: chỉ đổi ghi chú.
        /// Chuỗi rỗng nghĩa là xóa giá trị đó.
        /// </summary>
        public void SaveRoomAvailabilityNotes(IReadOnlyList<DateTime> dates, string? colorName, string? description, int userId)
        {
            using var connection = new SqlConnection(DBUtils.GetDBConnectionString());
            connection.Open();
            using var transaction = connection.BeginTransaction();

            foreach (var date in dates)
            {
                // Mỗi ngày một dòng; nếu dữ liệu cũ có dòng trùng thì cập nhật tất cả
                using var command = new SqlCommand(@"
                    UPDATE dbo.RoomAvailibilityColor WITH (UPDLOCK, HOLDLOCK)
                    SET ColorName = ISNULL(@Color, ColorName), Description = ISNULL(@Description, Description),
                        UpdateDate = GETDATE(), UserUpdateID = @UserID
                    WHERE [Date] >= @Date AND [Date] < DATEADD(DAY, 1, @Date);

                    IF @@ROWCOUNT = 0 AND (ISNULL(@Color, '') <> '' OR ISNULL(@Description, '') <> '')
                        INSERT INTO dbo.RoomAvailibilityColor
                            ([Date], RateCodeID, Description, DayOfWeek, ColorName, UserInsertID, CreateDate, UpdateDate, UserUpdateID)
                        VALUES (@Date, 0, ISNULL(@Description, ''), @DayOfWeek, ISNULL(@Color, ''), @UserID, GETDATE(), GETDATE(), @UserID);",
                    connection, transaction);
                command.Parameters.Add("@Date", SqlDbType.DateTime).Value = date.Date;
                command.Parameters.Add("@DayOfWeek", SqlDbType.NVarChar, 50).Value = date.DayOfWeek.ToString();
                command.Parameters.Add("@Color", SqlDbType.NVarChar, 50).Value = (object?)colorName ?? DBNull.Value;
                command.Parameters.Add("@Description", SqlDbType.NVarChar, 200).Value = (object?)description ?? DBNull.Value;
                command.Parameters.Add("@UserID", SqlDbType.Int).Value = userId;
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }
    }
}
