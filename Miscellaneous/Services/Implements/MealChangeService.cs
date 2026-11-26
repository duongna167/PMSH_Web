using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using BaseBusiness.util;
using Microsoft.Data.SqlClient;
using Miscellaneous.Services.Interfaces;

namespace Miscellaneous.Services.Implements
{
    /// <summary>
    /// Meal Change Manager - chuyển từ frmMealChangeManager (WinForms).
    /// </summary>
    public partial class MiscellaneousService
    {
        // Chỉ số 0/1/2 giống combo "meal" bên WinForms. Tên cột lấy từ mảng cố định này, không lấy từ client.
        private static readonly string[] MealColumns = { "Breakfast", "Lunch", "Dinner" };

        public DataTable MealChangeHistory(int reservationId) =>
            DataTableHelper.getTableData("spMeal_SearchChangeHistory",
                new[] { new SqlParameter("@ReservationID", reservationId) });

        // ShowMealInfo: "đã ăn / được ăn" của từng bữa
        public string MealSummary(int reservationId)
        {
            using var connection = new SqlConnection(DBUtils.GetDBConnectionString());
            connection.Open();

            var total = new int[3];
            var used = new int[3];

            // Được ăn = số đêm có bữa (ReservationRate) + bữa của ngày đi (Reservation)
            using (var command = new SqlCommand(@"
                SELECT ISNULL(SUM(CONVERT(INT, Breakfast)), 0), ISNULL(SUM(CONVERT(INT, Lunch)), 0), ISNULL(SUM(CONVERT(INT, Dinner)), 0)
                FROM (
                    SELECT Breakfast, Lunch, Dinner FROM dbo.ReservationRate WITH (NOLOCK) WHERE ReservationID = @ID
                    UNION ALL
                    SELECT Breakfast, Lunch, Dinner FROM dbo.Reservation WITH (NOLOCK) WHERE ID = @ID
                ) meal", connection))
            {
                command.Parameters.Add("@ID", SqlDbType.Int).Value = reservationId;
                using var reader = command.ExecuteReader();
                if (reader.Read())
                    for (var i = 0; i < 3; i++) total[i] = Convert.ToInt32(reader[i]);
            }

            using (var command = new SqlCommand(@"
                SELECT b.[Type], COUNT(a.ID)
                FROM dbo.MealDailyUsing a WITH (NOLOCK)
                JOIN dbo.MealShift b WITH (NOLOCK) ON a.MealShiftID = b.ID
                WHERE a.Inactive = 0 AND a.ReservationID = @ID
                GROUP BY b.[Type]", connection))
            {
                command.Parameters.Add("@ID", SqlDbType.Int).Value = reservationId;
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var type = Convert.ToInt32(reader[0]);
                    if (type >= 0 && type < 3) used[type] = Convert.ToInt32(reader[1]);
                }
            }

            return $"BF: {used[0]}/{total[0]} -- LN: {used[1]}/{total[1]} -- DN: {used[2]}/{total[2]}";
        }

        public MealChangeResult ProcessMealChange(MealChangeRequest request, string userName)
        {
            var from = request.FromDate.Date;
            var to = request.ToDate.Date;
            var description = request.Description ?? "";
            var result = new MealChangeResult();
            var systemDate = TextUtils.GetSystemDate();

            using var connection = new SqlConnection(DBUtils.GetDBConnectionString());
            connection.Open();
            using var transaction = connection.BeginTransaction();

            SqlCommand Command(string sql, int reservationId)
            {
                var command = new SqlCommand(sql, connection, transaction);
                command.Parameters.Add("@ID", SqlDbType.Int).Value = reservationId;
                command.Parameters.Add("@From", SqlDbType.DateTime).Value = from;
                command.Parameters.Add("@To", SqlDbType.DateTime).Value = to;
                return command;
            }

            bool Exists(string sql, int reservationId)
            {
                using var command = Command(sql, reservationId);
                return command.ExecuteScalar() != null;
            }

            void Execute(string sql, int reservationId)
            {
                using var command = Command(sql, reservationId);
                command.ExecuteNonQuery();
            }

            // Khách có bữa này trong ngày không? Ngày đi lưu ở Reservation, các ngày khác ở ReservationRate.
            bool HasMeal(Guest guest, DateTime date, string dateParameter, int meal) =>
                date == guest.Departure
                    ? guest.Meals[meal]
                    : Exists($@"SELECT TOP 1 ID FROM dbo.ReservationRate
                                WHERE ReservationID = @ID AND DATEDIFF(DAY, RateDate, {dateParameter}) = 0 AND {MealColumns[meal]} = 1",
                        guest.Id);

            // Check_Change
            bool CanChange(Guest guest)
            {
                if (!HasMeal(guest, from, "@From", request.FromMeal)) return false;

                // Đã ăn rồi thì không cho đổi / khóa nữa
                if (Exists($@"SELECT TOP 1 a.ID FROM dbo.MealDailyUsing a
                              JOIN dbo.MealShift b ON a.MealShiftID = b.ID
                              WHERE a.ReservationID = @ID AND DATEDIFF(DAY, a.UsingDate, @From) = 0
                                AND a.Inactive = 0 AND b.[Type] = {request.FromMeal}", guest.Id))
                    return false;

                // Khóa bữa không cần kiểm tra bữa đổi đến; đổi bữa thì bữa đến phải đang trống
                return request.Type == 3 || !HasMeal(guest, to, "@To", request.ToMeal);
            }

            // Change_UpdateResvRate
            void SetMeal(Guest guest, DateTime date, string dateParameter, int meal, int value)
            {
                var column = MealColumns[meal];
                if (date == guest.Departure)
                    Execute($"UPDATE dbo.Reservation SET FixedMeal = 1, {column} = {value} WHERE ID = @ID", guest.Id);
                else
                    Execute($@"UPDATE dbo.ReservationRate SET FixedMeal = 1, {column} = {value}
                               WHERE ReservationID = @ID AND DATEDIFF(DAY, RateDate, {dateParameter}) = 0", guest.Id);
            }

            // Add_UpdateResvRate / Cancel_UpdateResvRate
            void SetMealRange(Guest guest, int meal, int value)
            {
                var column = MealColumns[meal];
                Execute($@"UPDATE dbo.ReservationRate SET FixedMeal = 1, {column} = {value}
                           WHERE ReservationID = @ID AND DATEDIFF(DAY, RateDate, @From) <= 0 AND DATEDIFF(DAY, RateDate, @To) >= 0",
                    guest.Id);
                if (to == guest.Departure)
                    Execute($"UPDATE dbo.Reservation SET FixedMeal = 1, {column} = {value} WHERE ID = @ID", guest.Id);
            }

            foreach (var id in request.ReservationIds.Distinct())
            {
                var guest = LoadGuest(connection, transaction, id);
                if (guest == null)
                {
                    result.Skipped.Add($"Reservation ID {id}");
                    continue;
                }

                // CheckRate_by_Date: ngày xử lý phải nằm trong thời gian lưu trú
                var inStay = from >= guest.Arrival && from <= guest.Departure
                    && (request.Type == 3 || (to >= guest.Arrival && to <= guest.Departure));
                if (!inStay || ((request.Type == 0 || request.Type == 3) && !CanChange(guest)))
                {
                    result.Skipped.Add(guest.Name);
                    continue;
                }

                switch (request.Type)
                {
                    case 0: // Change meal
                        SetMeal(guest, from, "@From", request.FromMeal, 0);
                        SetMeal(guest, to, "@To", request.ToMeal, 1);
                        break;
                    case 1: // Add meal
                        SetMealRange(guest, request.ToMeal, 1);
                        break;
                    case 2: // Cancel meal
                        SetMealRange(guest, request.ToMeal, 0);
                        break;
                    case 3: // Lock meal. MealShiftID 1/2/3 = sáng/trưa/tối như bản WinForms
                        using (var insert = Command(@"
                            INSERT INTO dbo.MealDailyUsing (ReservationID, CardID, MealShiftID, UsingDate, Inactive, MealConfigID, [Type], Description, AmountMeal)
                            VALUES (@ID, @CardID, @Shift, @UsingDate, 0, 0, 1, @Description, 0)", guest.Id))
                        {
                            insert.Parameters.AddWithValue("@CardID", guest.CardId);
                            insert.Parameters.AddWithValue("@Shift", request.FromMeal + 1);
                            insert.Parameters.AddWithValue("@UsingDate", from.AddHours(systemDate.Hour).AddMinutes(systemDate.Minute));
                            insert.Parameters.AddWithValue("@Description", description);
                            insert.ExecuteNonQuery();
                        }
                        break;
                }

                // Lịch sử thay đổi. Cờ bữa: Add/Cancel lấy theo "Meal", Lock lấy theo "From meal", Change không đặt
                var flagMeal = request.Type == 0 ? -1 : request.Type == 3 ? request.FromMeal : request.ToMeal;
                using (var history = Command(@"
                    INSERT INTO dbo.MealChangeHistory
                        (ReservationID, FromDate, FromMeal, ToDate, ToMeal, Breakfast, Lunch, Dinner, [Type], Description,
                         CreatedBy, CreatedDate, UpdatedBy, UpdatedDate)
                    VALUES (@ID, @From, @FromMeal, @To, @ToMeal, @Breakfast, @Lunch, @Dinner, @Type, @Description,
                            @User, @Now, @User, @Now)", guest.Id))
                {
                    history.Parameters.AddWithValue("@FromMeal", request.FromMeal);
                    history.Parameters.AddWithValue("@ToMeal", request.ToMeal);
                    history.Parameters.AddWithValue("@Breakfast", flagMeal == 0);
                    history.Parameters.AddWithValue("@Lunch", flagMeal == 1);
                    history.Parameters.AddWithValue("@Dinner", flagMeal == 2);
                    history.Parameters.AddWithValue("@Type", request.Type);
                    history.Parameters.AddWithValue("@Description", description);
                    history.Parameters.AddWithValue("@User", userName);
                    history.Parameters.AddWithValue("@Now", systemDate);
                    history.ExecuteNonQuery();
                }
                result.Processed++;
            }

            // Có lỗi ở bất kỳ khách nào thì transaction tự rollback khi dispose
            transaction.Commit();
            return result;
        }

        private sealed record Guest(int Id, DateTime Arrival, DateTime Departure, bool[] Meals, string CardId, string Name);

        private static Guest LoadGuest(SqlConnection connection, SqlTransaction transaction, int id)
        {
            using var command = new SqlCommand(@"
                SELECT a.ArrivalDate, a.DepartureDate, a.Breakfast, a.Lunch, a.Dinner, a.CardID, b.Account
                FROM dbo.Reservation a
                LEFT JOIN dbo.[Profile] b ON a.ProfileIndividualID = b.ID
                WHERE a.ID = @ID", connection, transaction);
            command.Parameters.Add("@ID", SqlDbType.Int).Value = id;
            using var reader = command.ExecuteReader();
            if (!reader.Read()) return null;

            bool Flag(int i) => reader[i] != DBNull.Value && Convert.ToInt32(reader[i]) != 0;
            return new Guest(id,
                Convert.ToDateTime(reader[0]).Date,
                Convert.ToDateTime(reader[1]).Date,
                new[] { Flag(2), Flag(3), Flag(4) },
                Convert.ToString(reader[5]) ?? "",
                Convert.ToString(reader[6]) ?? "");
        }
    }
}
