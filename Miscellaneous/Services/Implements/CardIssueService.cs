using System.Data;
using System.Globalization;
using BaseBusiness.util;
using Microsoft.Data.SqlClient;
using Miscellaneous.Services.Interfaces;

namespace Miscellaneous.Services.Implements
{
    /// <summary>Add card to guest (frmCardByReservation): Process_PMS và Process_Cancel_PMS.</summary>
    public partial class MiscellaneousService
    {
        private sealed record CardGuest(int ReservationId, string CardId, int Status, string RoomType, string RoomNo,
            DateTime Arrival, DateTime Departure, int ShareRoom, int ProfileId, string GuestName);

        public CardIssueResult IssueCards(IReadOnlyList<CardAssignment> items, int userId, string userName) =>
            RunCardTransaction((connection, transaction, sendInterface, result) =>
            {
                foreach (var item in items)
                {
                    var cardId = item.CardId?.Trim() ?? "";
                    var guest = ReadCardGuest(connection, transaction, item.ReservationId);
                    var reason = guest == null ? "reservation not found"
                        : !IsActive(guest.Status) ? "reservation status does not allow cards"
                        : guest.CardId != "" ? "guest already has a card"
                        : !CardExistsForIssue(connection, transaction, cardId) ? "card does not exist on the system"
                        : null;
                    if (reason != null)
                    {
                        result.Skipped.Add($"{guest?.GuestName ?? item.ReservationId.ToString()} ({cardId}): {reason}");
                        continue;
                    }

                    UpdateGuestCard(connection, transaction, guest!, cardId, "", cardId, "Add card", userId, userName);
                    if (sendInterface) InsertCardInterface(connection, transaction, "SCN", guest!, cardId);
                    result.Processed++;
                }
            });

        public CardIssueResult CancelCards(IReadOnlyList<int> reservationIds, int userId, string userName) =>
            RunCardTransaction((connection, transaction, sendInterface, result) =>
            {
                foreach (var reservationId in reservationIds.Distinct())
                {
                    var guest = ReadCardGuest(connection, transaction, reservationId);
                    // Chỉ hủy thẻ đã lưu của đặt phòng còn hiệu lực; mã thẻ lấy từ database
                    if (guest == null || guest.CardId == "" || !IsActive(guest.Status))
                    {
                        result.Skipped.Add($"{guest?.GuestName ?? reservationId.ToString()}: no card to cancel");
                        continue;
                    }

                    UpdateGuestCard(connection, transaction, guest, "", guest.CardId, guest.CardId, "Cancel card", userId, userName);
                    if (sendInterface) InsertCardInterface(connection, transaction, "SCC", guest, guest.CardId);
                    result.Processed++;
                }
            });

        private static bool IsActive(int status) => status is 0 or 5 or 1 or 6;

        private static CardIssueResult RunCardTransaction(Action<SqlConnection, SqlTransaction, bool, CardIssueResult> action)
        {
            using var connection = new SqlConnection(DBUtils.GetDBConnectionString());
            connection.Open();
            using var transaction = connection.BeginTransaction();

            // IF_IN = 0: không ghi sang bảng Interface
            using var config = new SqlCommand("SELECT TOP 1 KeyValue FROM dbo.ConfigSystem WHERE KeyName = 'IF_IN'", connection, transaction);
            var sendInterface = Convert.ToString(config.ExecuteScalar()) != "0";

            var result = new CardIssueResult();
            action(connection, transaction, sendInterface, result);
            transaction.Commit();
            return result;
        }

        private static CardGuest? ReadCardGuest(SqlConnection connection, SqlTransaction transaction, int reservationId)
        {
            using var command = new SqlCommand(@"
                SELECT a.CardID, a.[Status], a.RoomType, a.RoomNo, a.ArrivalDate, a.DepartureDate, a.ShareRoom, b.ID, b.Account
                FROM dbo.Reservation a WITH (UPDLOCK)
                JOIN dbo.[Profile] b ON a.ProfileIndividualID = b.ID
                WHERE a.ID = @ID", connection, transaction);
            command.Parameters.Add("@ID", SqlDbType.Int).Value = reservationId;
            using var reader = command.ExecuteReader();
            if (!reader.Read()) return null;

            string Text(int i) => Convert.ToString(reader.GetValue(i))?.Trim() ?? "";
            int Number(int i) => reader.IsDBNull(i) ? 0 : Convert.ToInt32(reader.GetValue(i));
            DateTime Date(int i) => reader.IsDBNull(i) ? DateTime.MinValue : reader.GetDateTime(i);

            return new CardGuest(reservationId, Text(0), Number(1), Text(2), Text(3), Date(4), Date(5), Number(6), Number(7), Text(8));
        }

        private static bool CardExistsForIssue(SqlConnection connection, SqlTransaction transaction, string cardId)
        {
            using var command = new SqlCommand("SELECT COUNT(1) FROM dbo.Card WHERE ID = @ID", connection, transaction);
            AddCardId(command, cardId);
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        private static void UpdateGuestCard(SqlConnection connection, SqlTransaction transaction, CardGuest guest,
            string newCardId, string oldLogValue, string cardId, string description, int userId, string userName)
        {
            using var command = new SqlCommand(@"
                UPDATE dbo.Reservation SET CardID = @NewCardID WHERE ID = @ReservationID;
                UPDATE dbo.Card SET CanSell = 0 WHERE ID = @ID;
                INSERT INTO dbo.ActivityLog (TableName, ObjectID, UserID, UserName, ChangeDate, Change, OldValue, NewValue, Description)
                VALUES ('Reservation', @ReservationID, @UserID, @UserName, GETDATE(), 'CardID', @OldValue, @NewCardID, @Description);",
                connection, transaction);
            AddCardId(command, cardId);
            command.Parameters.Add("@ReservationID", SqlDbType.Int).Value = guest.ReservationId;
            command.Parameters.Add("@NewCardID", SqlDbType.NVarChar, 21).Value = newCardId;
            command.Parameters.Add("@OldValue", SqlDbType.NVarChar, 21).Value = oldLogValue;
            command.Parameters.Add("@UserID", SqlDbType.Int).Value = userId;
            command.Parameters.Add("@UserName", SqlDbType.NVarChar, 50).Value = userName;
            command.Parameters.Add("@Description", SqlDbType.NVarChar, 50).Value = description;
            command.ExecuteNonQuery();
        }

        // SCN (cấp thẻ) / SCC (hủy thẻ): |R#|G#|GA|GD|GN|RN|CA|SR|
        private static void InsertCardInterface(SqlConnection connection, SqlTransaction transaction, string key, CardGuest guest, string cardId)
        {
            // Phòng ảo không đẩy dữ liệu
            if (guest.RoomType == "XXX") return;

            string Day(DateTime value) => value.ToString("yyMMdd", CultureInfo.InvariantCulture);
            var description = $"{key}|R#{guest.ReservationId}|G#{guest.ProfileId}|GA{Day(guest.Arrival)}|GD{Day(guest.Departure)}"
                + $"|GN{guest.GuestName}|RN{guest.RoomNo}|CA{cardId}|SR{guest.ShareRoom}|";

            using var command = new SqlCommand(
                "INSERT INTO dbo.Interface (KeyValue, Description, CreateDate) VALUES (@Key, @Description, GETDATE())",
                connection, transaction);
            command.Parameters.Add("@Key", SqlDbType.VarChar, 50).Value = key;
            command.Parameters.Add("@Description", SqlDbType.NVarChar, -1).Value = description;
            command.ExecuteNonQuery();
        }
    }
}
