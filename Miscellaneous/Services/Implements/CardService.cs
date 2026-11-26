using System.Data;
using BaseBusiness.util;
using Microsoft.Data.SqlClient;
using Miscellaneous.Services.Interfaces;

namespace Miscellaneous.Services.Implements
{
    /// <summary>Card management (frmCard).</summary>
    public partial class MiscellaneousService
    {
        private const string CardSelect = @"
            SELECT ID, CardTypeID, N'Card Hotel' AS CardType, Status,
                   CASE Status WHEN 0 THEN N'Inactive' WHEN 1 THEN N'Active' ELSE N'Other' END AS StatusText,
                   CreatedBy, CreatedDate, UpdatedBy, UpdatedDate
            FROM dbo.Card WITH (NOLOCK)";

        public DataTable Cards(string? id = null)
        {
            using var connection = new SqlConnection(DBUtils.GetDBConnectionString());
            using var command = new SqlCommand(CardSelect
                + (id == null ? " ORDER BY CreatedDate DESC, ID" : " WHERE ID = @ID"), connection);
            if (id != null) AddCardId(command, id);
            var table = new DataTable();
            using var adapter = new SqlDataAdapter(command);
            adapter.Fill(table);
            return table;
        }

        public bool InsertCard(string id, int cardTypeId, int status, string userName) =>
            // Kiểm tra trùng và thêm trong một câu lệnh để quét liên tiếp không tạo thẻ đôi.
            // CanSell để mặc định của bảng như bản WinForms.
            ExecuteCard(@"
                INSERT INTO dbo.Card (ID, CardTypeID, Status, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate)
                SELECT @ID, @CardTypeID, @Status, @User, GETDATE(), @User, GETDATE()
                WHERE NOT EXISTS (SELECT 1 FROM dbo.Card WITH (UPDLOCK, HOLDLOCK) WHERE ID = @ID);",
                id, cardTypeId, status, userName);

        public bool UpdateCard(string id, int cardTypeId, int status, string userName) =>
            ExecuteCard(@"
                UPDATE dbo.Card SET CardTypeID = @CardTypeID, Status = @Status, UpdatedBy = @User, UpdatedDate = GETDATE()
                WHERE ID = @ID;",
                id, cardTypeId, status, userName);

        public CardDeleteResult DeleteCard(string id)
        {
            using var connection = new SqlConnection(DBUtils.GetDBConnectionString());
            // Thẻ đã gắn vào đặt phòng thì không được xóa
            using var command = new SqlCommand(@"
                IF EXISTS (SELECT 1 FROM dbo.Reservation WHERE CardID = @ID)
                    SELECT 1;
                ELSE
                BEGIN
                    DELETE FROM dbo.Card WHERE ID = @ID;
                    SELECT CASE WHEN @@ROWCOUNT > 0 THEN 0 ELSE 2 END;
                END", connection);
            AddCardId(command, id);
            connection.Open();
            return (CardDeleteResult)Convert.ToInt32(command.ExecuteScalar());
        }

        private static bool ExecuteCard(string sql, string id, int cardTypeId, int status, string userName)
        {
            using var connection = new SqlConnection(DBUtils.GetDBConnectionString());
            using var command = new SqlCommand(sql, connection);
            AddCardId(command, id);
            command.Parameters.Add("@CardTypeID", SqlDbType.Int).Value = cardTypeId;
            command.Parameters.Add("@Status", SqlDbType.Int).Value = status;
            // CreatedBy/UpdatedBy là varchar(20)
            command.Parameters.Add("@User", SqlDbType.VarChar, 20).Value = userName.Length > 20 ? userName[..20] : userName;
            connection.Open();
            return command.ExecuteNonQuery() > 0;
        }

        private static void AddCardId(SqlCommand command, string id) =>
            command.Parameters.Add("@ID", SqlDbType.VarChar, 21).Value = id;
    }
}
