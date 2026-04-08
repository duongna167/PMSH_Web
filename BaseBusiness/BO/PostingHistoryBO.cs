using BaseBusiness.bc;
using BaseBusiness.Facade;
using BaseBusiness.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.BO
{
    public class PostingHistoryBO : BaseBO
    {
        private const int BillingPostingHistoryMaxRows = 500;
        private const string BillingPostingHistorySelectColumns = @"
            SELECT TOP ({0})
                ID,
                ActionType,
                ActionText,
                ActionDate,
                ActionUser,
                InvoiceNo,
                Amount,
                Supplement,
                Code,
                Description,
                TransactionDate,
                ReasonCode,
                ReasonText,
                Terminal,
                Machine,
                Action_FolioID,
                AfterAction_FolioID,
                Property
            FROM PostingHistory WITH (NOLOCK)";

        private PostingHistoryFacade facade = PostingHistoryFacade.Instance;
        protected static PostingHistoryBO instance = new PostingHistoryBO();

        protected PostingHistoryBO()
        {
            this.baseFacade = facade;
        }

        public static PostingHistoryBO Instance
        {
            get { return instance; }
        }
        public static List<PostingHistoryModel> GetPostingHistoryByFolio(int folio)
        {
            if (folio <= 0)
            {
                return new List<PostingHistoryModel>();
            }

            string query =
                string.Format(BillingPostingHistorySelectColumns, BillingPostingHistoryMaxRows)
                + $@"
                WHERE AfterAction_FolioID = {folio}
                ORDER BY ActionDate DESC, ID DESC";
            return instance.GetList<PostingHistoryModel>(query);
        }
        public static List<PostingHistoryModel> GetPostingHistoryByInvoiceNo(int invoiceNo)
        {
            if (invoiceNo <= 0)
            {
                return new List<PostingHistoryModel>();
            }

            string query =
                string.Format(BillingPostingHistorySelectColumns, BillingPostingHistoryMaxRows)
                + $@"
                WHERE InvoiceNo = '{invoiceNo}'
                ORDER BY ActionDate DESC, ID DESC";
            return instance.GetList<PostingHistoryModel>(query);
        }
    }
}
