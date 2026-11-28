using System.Collections.Generic;
using System.Data;
using System.Linq;
using BaseBusiness.util;
using Microsoft.Data.SqlClient;

namespace Miscellaneous.Services.Implements
{
    /// <summary>Config Computer - Card Reader (frmComputerWithCardReader, frmComputerWithCardReaderSearch).</summary>
    public partial class MiscellaneousService
    {
        /// <param name="cardReaderIds">Dạng 1,2,3; chuỗi rỗng là tất cả.</param>
        public DataTable ComputerCardReaders(string cardReaderIds) =>
            DataTableHelper.getTableData("spComputerWithCardReaderSearch",
                new[] { new SqlParameter("@CardReaderID", cardReaderIds) });

        public DataTable CardReaders() => QueryTable("SELECT ID, CardReader FROM dbo.CardReader ORDER BY ID");

        public DataTable Computers() => QueryTable("SELECT ID, Name, Description FROM dbo.Computer ORDER BY Name");

        /// <summary>Gán đầu đọc cho các máy: mỗi máy được chọn chỉ còn đúng đầu đọc này (UpdateData).</summary>
        public bool SaveComputerCardReader(int cardReaderId, IReadOnlyList<int> computerIds)
        {
            using var connection = new SqlConnection(DBUtils.GetDBConnectionString());
            connection.Open();
            using var transaction = connection.BeginTransaction();

            using (var check = new SqlCommand("SELECT COUNT(1) FROM dbo.CardReader WHERE ID = @ID", connection, transaction))
            {
                check.Parameters.Add("@ID", SqlDbType.Int).Value = cardReaderId;
                if (Convert.ToInt32(check.ExecuteScalar()) == 0) return false;
            }

            foreach (var computerId in computerIds.Distinct())
            {
                using var command = new SqlCommand(@"
                    IF NOT EXISTS (SELECT 1 FROM dbo.Computer WHERE ID = @ComputerID)
                        RETURN;
                    DELETE FROM dbo.ConfigComputer_CardReader
                    WHERE ComputerID = @ComputerID AND ISNULL(CardReaderID, 0) <> @CardReaderID;
                    IF NOT EXISTS (SELECT 1 FROM dbo.ConfigComputer_CardReader WITH (UPDLOCK, HOLDLOCK)
                                   WHERE ComputerID = @ComputerID AND CardReaderID = @CardReaderID)
                        INSERT INTO dbo.ConfigComputer_CardReader (ComputerID, CardReaderID)
                        VALUES (@ComputerID, @CardReaderID);", connection, transaction);
                command.Parameters.Add("@ComputerID", SqlDbType.Int).Value = computerId;
                command.Parameters.Add("@CardReaderID", SqlDbType.Int).Value = cardReaderId;
                command.ExecuteNonQuery();
            }

            transaction.Commit();
            return true;
        }

        public int DeleteComputerCardReaders(IReadOnlyList<int> ids)
        {
            using var connection = new SqlConnection(DBUtils.GetDBConnectionString());
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var deleted = 0;
            foreach (var id in ids.Distinct())
            {
                using var command = new SqlCommand("DELETE FROM dbo.ConfigComputer_CardReader WHERE ID = @ID", connection, transaction);
                command.Parameters.Add("@ID", SqlDbType.Int).Value = id;
                deleted += command.ExecuteNonQuery();
            }

            transaction.Commit();
            return deleted;
        }

        private static DataTable QueryTable(string sql)
        {
            using var connection = new SqlConnection(DBUtils.GetDBConnectionString());
            using var adapter = new SqlDataAdapter(sql, connection);
            var table = new DataTable();
            adapter.Fill(table);
            return table;
        }
    }
}
