using BaseBusiness.bc;
using BaseBusiness.Facade;
using BaseBusiness.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
namespace BaseBusiness.BO
{
    using Dapper;
    public class FolioDetailBO : BaseBO
    {
        private FolioDetailFacade facade = FolioDetailFacade.Instance;
        protected static FolioDetailBO instance = new FolioDetailBO();

        protected FolioDetailBO()
        {
            this.baseFacade = facade;
        }

        public static FolioDetailBO Instance
        {
            get { return instance; }
        }
        public static int GetTopInvoiceNo()
        {
            string query = @"SELECT ISNULL(MAX(CAST(InvoiceNo AS INT)), 0)FROM FolioDetail";

            return instance.GetFirst<int>(query);
        }
        public static int GetTopTransactioNo()
        {
            string query = "SELECT TOP 1 TransactionNo + (SELECT COUNT(*) \r\n                             FROM FolioDetail \r\n                             WHERE TransactionNo = (SELECT TOP 1 TransactionNo \r\n                                                   FROM FolioDetail \r\n                                                   ORDER BY id DESC)) AS NextTransactionNo\r\nFROM FolioDetail\r\nORDER BY id DESC;";
            return instance.GetFirst<int>(query);
        }
        public static FolioDetailModel GetFolioDetailMaster(string transactionNo)
        {
            string query = $"select top 1 * from FolioDetail where TransactionNo = '{transactionNo}' and (RowState in (2,1) and IsSplit = 1)";
            return instance.GetFirst<FolioDetailModel>(query);
        }
        public static FolioDetailModel GetFolioDetailMasterEdit(string invoiceNoPosting)
        {
            string query = $"select  * from FolioDetail WITH (NOLOCK) where InvoiceNo  = '{invoiceNoPosting}'";
            return instance.GetFirst<FolioDetailModel>(query);
        }
        public static decimal CalculateBalance(int reservationID)
        {
            string query = $@"
                SELECT ISNULL(SUM(fd.AmountMaster), 0) AS Amount 
                FROM FolioDetail fd WITH (NOLOCK)
                INNER JOIN Reservation r WITH (NOLOCK) ON fd.ReservationID = r.ID
                WHERE r.ConfirmationNo = (SELECT ConfirmationNo FROM Reservation WITH (NOLOCK) WHERE ID = {reservationID}) 
                AND fd.RowState = 1 AND fd.Status = 0";
            return instance.GetFirst<decimal>(query);
        }

        public static List<string> GetTransactionCodeBySelectOption(DateTime fromDate, DateTime toDate, int groupID, int subGroup, string transCode, string cashierNo, string checkNo, int rsvID, List<int> userIDs)
        {
            string userIDsString = string.Join(",", userIDs);
            string query = "select distinct TransactionCode from FolioDetail\r\n" +
                           $"where CAST(TransactionDate as Date) >= cast('{fromDate:yyyy-MM-dd}' as date) and CAST(TransactionDate as date) <= cast('{toDate:yyyy-MM-dd}' as date)\r\n" +
                           $"and TransactionGroupID = {groupID}\r\n" +
                           $"and TransactionSubgroupID = {subGroup}\r\n" +
                           (string.IsNullOrEmpty(transCode) ? "" : $"and TransactionCode = '{transCode}'\r\n") +
                           (string.IsNullOrEmpty(cashierNo) ? "" : $"and CashierNo = '{cashierNo}'\r\n") +
                           (string.IsNullOrEmpty(checkNo) ? "" : $"and CheckNo = '{checkNo}'\r\n") +
                           $"and ReservationID = {rsvID}\r\n" +
                           (string.IsNullOrEmpty(userIDsString) || userIDsString == "0" ? "" : $"and UserInsertID IN ({userIDsString})\r\n");
            return instance.GetList<string>(query);
        }
        public FolioDetailModel GetById(int id, SqlConnection conn, SqlTransaction tx)
        {
            const string sql = "SELECT ID, TransactionCode, Price, Description, TransactionDate, ArticleCode, Reference, CheckNo, UserID, UserName, UserInsertID, CreateDate,  UserUpdateID, UpdateDate FROM FolioDetail WHERE ID = @id";
            return conn.QuerySingleOrDefault<FolioDetailModel>(sql, new { id }, tx);
        }
    }
}
