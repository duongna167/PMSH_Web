using EInvoice.Runtime;
using EInvoice.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace EInvoice.Services.Implements
{
    /// <summary>
    /// Tạo hóa đơn mới (New Summary Invoice) — port từ FolioVATService của PMSH_Web (btnOK_Click của WinForms).
    /// Đọc bằng stored procedure có sẵn, ghi FolioVAT/FolioVATDetail bằng SQL trong một transaction.
    /// Tất cả chạy trên DB PMS của Property được chọn (kết nối lấy từ cấu hình Property).
    /// </summary>
    public class NewInvoiceService : INewInvoiceService
    {
        // Số dòng tối đa trên một hóa đơn (AccountingUtils.MaxVatRow / MaxVatRow_Summary của WinForms)
        private const int MaxVatRowDefault = 10;
        private const int MaxVatRowSummary = 20;

        #region Kết nối tới DB của Property

        /// <summary>Chuỗi kết nối tới DB PMS của Property (chỉ chấp nhận Property đang hoạt động, nguồn PMS).</summary>
        private static string ConnectionOf(int propertyId, out string propertyName)
        {
            if (propertyId <= 0) throw new ProviderBuildException("Please select property!");
            PmsSourceConfig source = EInvoiceRuntimeService.LoadSource(propertyId);
            propertyName = string.IsNullOrWhiteSpace(source.Name) ? source.Code : source.Name;
            return source.BuildConnectionString();
        }

        private static DataTable RunProc(string connectionString, string procedure, params SqlParameter[] parameters)
        {
            var table = new DataTable();
            using var conn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand(procedure, conn) { CommandType = CommandType.StoredProcedure, CommandTimeout = 120 };
            cmd.Parameters.AddRange(parameters);
            new SqlDataAdapter(cmd).Fill(table);
            return table;
        }

        private static DateTime BusinessDateOf(SqlConnection conn, SqlTransaction? tx = null)
        {
            using var cmd = new SqlCommand("SELECT TOP 1 BusinessDate FROM BusinessDate WITH (NOLOCK) ORDER BY ID", conn, tx);
            object? v = cmd.ExecuteScalar();
            return v == null || v == DBNull.Value ? DateTime.Today : Convert.ToDateTime(v);
        }

        #endregion

        #region Đọc dữ liệu

        public (string PropertyName, DateTime BusinessDate) GetContext(int propertyId)
        {
            string cs = ConnectionOf(propertyId, out string name);
            using var conn = new SqlConnection(cs);
            conn.Open();
            return (name, BusinessDateOf(conn));
        }

        public DataTable SearchFolios(int propertyId, string confirmationNo, string folioNo, string roomNo, DateTime fromDate, DateTime toDate)
        {
            string cs = ConnectionOf(propertyId, out _);
            return RunProc(cs, "spVAT_SearchFolioForVAT",
                new SqlParameter("@ConfirmationNo", confirmationNo ?? ""),
                new SqlParameter("@FolioNo", folioNo ?? ""),
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@RoomNo", roomNo ?? ""),
                new SqlParameter("@AccountingCode", ""),
                new SqlParameter("@CaseCode", ""),
                new SqlParameter("@LicenseID", 0),
                new SqlParameter("@VATType", 2),
                new SqlParameter("@TransactionCode", ""));
        }

        public DataTable GetFolioDetails(int propertyId, int folioId, DateTime fromDate, DateTime toDate)
        {
            return GetFolioDetails(ConnectionOf(propertyId, out _), folioId, fromDate, toDate);
        }

        private static DataTable GetFolioDetails(string cs, int folioId, DateTime fromDate, DateTime toDate)
        {
            return RunProc(cs, "spVAT_GetFolioDetailNotInVAT",
                new SqlParameter("@FolioID", folioId),
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@CaseCode", ""),
                new SqlParameter("@TransactionCode", ""));
        }

        public DataTable GetFolioTransactions(int propertyId, int folioId)
        {
            string cs = ConnectionOf(propertyId, out _);
            return RunProc(cs, "spSearchTransactionInFolio", new SqlParameter("@FolioID", folioId));
        }

        #endregion

        #region Tạo hóa đơn

        public (List<int> CreatedIds, List<string> Errors) Create(int propertyId, NewInvoiceRequest req, string loginName)
        {
            var createdIds = new List<int>();
            var errors = new List<string>();

            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.StartInvoiceNo < 1) throw new Exception("Invoice no. cannot be blank");
            if (string.IsNullOrWhiteSpace(req.SerialNo)) throw new Exception("Invoice serial cannot be blank");
            if (string.IsNullOrWhiteSpace(req.FormNo)) throw new Exception("Form no. cannot be blank");
            if (req.FolioIds == null || req.FolioIds.Count == 0) throw new Exception("No folio selected.");

            string cs = ConnectionOf(propertyId, out _);

            // Toàn bộ giao dịch chưa xuất hóa đơn của các folio đã chọn
            DataTable tblData = LoadAllFolioDetails(cs, req);

            // Điều kiện lọc giao dịch cho từng hóa đơn tùy kiểu gộp/tách (arrSelect của WinForms)
            var (arrSelect, maxVatRow, folioIdsToProcess) = BuildSelectConditions(req, tblData);

            // Báo trùng số hóa đơn trước khi tạo (WinForms hỏi "invoice already exists, continue?")
            if (!req.IgnoreDuplicateInvoiceNo)
            {
                var duplicates = CheckDuplicateInvoiceNos(cs, req, folioIdsToProcess, arrSelect, maxVatRow, tblData);
                if (duplicates.Count > 0) throw new DuplicateInvoiceException(duplicates);
            }

            using var conn = new SqlConnection(cs);
            conn.Open();
            using SqlTransaction tx = conn.BeginTransaction();
            try
            {
                DateTime sysDate = DateTime.Now;
                DateTime businessDate = BusinessDateOf(conn, tx);
                var headerCols = ColumnsOf(conn, tx, "FolioVAT");        // chỉ ghi các cột có trong DB của Property
                var detailCols = ColumnsOf(conn, tx, "FolioVATDetail");
                int currentInvoiceNo = req.StartInvoiceNo;

                foreach (int folioId in folioIdsToProcess)
                {
                    var info = LoadFolioInfo(conn, tx, folioId, req, tblData);

                    foreach (string template in arrSelect)
                    {
                        DataRow[] rows = tblData.Select(string.Format(template, folioId, "<> 0"));
                        if (rows.Length == 0) continue;

                        // Mỗi nhóm tối đa maxVatRow dòng cho một hóa đơn
                        for (int start = 0; start < rows.Length; start += maxVatRow)
                        {
                            int end = Math.Min(start + maxVatRow, rows.Length);

                            // Số hóa đơn kế tiếp chưa dùng (bỏ qua số đã tồn tại)
                            string invoiceNo = FormatInvoiceNo(currentInvoiceNo);
                            while (InvoiceNoExists(conn, tx, req.FormNo, req.SerialNo, invoiceNo))
                            {
                                currentInvoiceNo++;
                                invoiceNo = FormatInvoiceNo(currentInvoiceNo);
                            }
                            currentInvoiceNo++;

                            int invoiceId = InsertHeader(conn, tx, headerCols, req, info, invoiceNo, loginName, sysDate, businessDate);

                            decimal sumAmount = 0, sumAmountInc = 0, vat5 = 0, vat10 = 0;
                            for (int i = start; i < end; i++)
                            {
                                var d = InsertDetail(conn, tx, detailCols, invoiceId, rows[i], loginName, sysDate);
                                sumAmount += d.Amount;
                                sumAmountInc += d.AmountIncludedTax;
                                if (d.VAT == 5) vat5 += d.VATAmount;
                                if (d.VAT == 10) vat10 += d.VATAmount;
                            }

                            UpdateHeaderTotals(conn, tx, headerCols, invoiceId, sumAmount, vat5, vat10, sumAmountInc);
                            createdIds.Add(invoiceId);
                        }
                    }
                }

                tx.Commit();
                return (createdIds, errors);
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                createdIds.Clear(); // đã rollback: không có hóa đơn nào được tạo
                errors.Add(ex.Message);
                return (createdIds, errors);
            }
        }

        /// <summary>Điều kiện lọc giao dịch theo kiểu gộp/tách hóa đơn (tương đương WinForms).</summary>
        private static (string[] ArrSelect, int MaxVatRow, List<int> FolioIds) BuildSelectConditions(NewInvoiceRequest req, DataTable tblData)
        {
            switch (req.GroupType)
            {
                case 1: // tất cả folio gộp trong một hóa đơn theo loại: không lọc theo folio (FolioID = 0)
                    return (new[] { "TransactionTypeID {1}" }, MaxVatRowSummary, new List<int> { 0 });

                case 2: // gộp theo từng folio
                case 3: // gộp theo loại dịch vụ
                    return (new[] { "FolioID ={0} AND TransactionTypeID {1}" }, MaxVatRowSummary, req.FolioIds.ToList());

                default: // 0: tách riêng theo tỷ lệ VAT
                    var conditions = new List<string>
                    {
                        // VAT 0
                        "FolioID ={0} AND VAT = 0 AND TransactionGroupID = 6 AND Amount < 0",
                        "FolioID ={0} AND VAT = 0 AND TransactionTypeID {1} AND (TransactionGroupID <> 6 OR Amount > 0) AND ServiceCode <> 'PMT'",
                        // VAT 5
                        "FolioID ={0} AND VAT = 5 AND TransactionGroupID = 6 AND Amount < 0",
                        "FolioID ={0} AND ((VAT = 5 AND TransactionTypeID {1} AND (TransactionGroupID <> 6 OR Amount > 0)) OR (VAT = 0 AND ServiceCode = 'PMT'))",
                        // VAT 10
                        "FolioID ={0} AND VAT = 10 AND TransactionGroupID = 6 AND Amount < 0",
                        "FolioID ={0} AND VAT = 10 AND TransactionTypeID {1} AND (TransactionGroupID <> 6 OR Amount > 0)"
                    };

                    // Bản WinForms chỉ biết VAT 0/5/10. Giao dịch có thuế suất khác (ví dụ 8%) sẽ không khớp điều kiện nào và
                    // bị bỏ sót khi tạo hóa đơn, nên thêm điều kiện riêng (cùng quy tắc) cho mỗi thuế suất lạ trong dữ liệu.
                    if (tblData.Columns.Contains("VAT"))
                    {
                        foreach (int rate in tblData.AsEnumerable()
                                     .Where(r => r["VAT"] != DBNull.Value).Select(r => Convert.ToInt32(r["VAT"])).Distinct()
                                     .Where(v => v != 0 && v != 5 && v != 10).OrderBy(v => v))
                        {
                            conditions.Add($"FolioID ={{0}} AND VAT = {rate} AND TransactionGroupID = 6 AND Amount < 0");
                            conditions.Add($"FolioID ={{0}} AND VAT = {rate} AND TransactionTypeID {{1}} AND (TransactionGroupID <> 6 OR Amount > 0)");
                        }
                    }
                    return (conditions.ToArray(), MaxVatRowDefault, req.FolioIds.ToList());
            }
        }

        private static DataTable LoadAllFolioDetails(string cs, NewInvoiceRequest req)
        {
            DataTable? all = null;
            foreach (int folioId in req.FolioIds)
            {
                DataTable dt = GetFolioDetails(cs, folioId, req.FromDate, req.ToDate);
                all ??= dt.Clone();
                foreach (DataRow dr in dt.Rows) all.ImportRow(dr);
            }
            return all ?? new DataTable();
        }

        private static List<string> CheckDuplicateInvoiceNos(string cs, NewInvoiceRequest req, List<int> folioIds, string[] arrSelect, int maxVatRow, DataTable tblData)
        {
            var duplicates = new List<string>();
            int currentNo = req.StartInvoiceNo;

            using var conn = new SqlConnection(cs);
            conn.Open();
            foreach (int folioId in folioIds)
            {
                foreach (string template in arrSelect)
                {
                    int count = tblData.Select(string.Format(template, folioId, "<> 0")).Length;
                    for (int done = 0; done < count; done += maxVatRow)
                    {
                        string invoiceNo = FormatInvoiceNo(currentNo++);
                        if (InvoiceNoExists(conn, null, req.FormNo, req.SerialNo, invoiceNo)) duplicates.Add(invoiceNo);
                    }
                }
            }
            return duplicates;
        }

        private static string FormatInvoiceNo(int no) => no.ToString().PadLeft(7, '0');

        private static bool InvoiceNoExists(SqlConnection conn, SqlTransaction? tx, string formNo, string serialNo, string invoiceNo)
        {
            const string sql = @"SELECT COUNT(1) FROM FolioVAT WITH (NOLOCK)
                                 WHERE ISNULL(FormNo,'') = @FormNo AND ISNULL(SerialNo,'') = @SerialNo AND ISNULL(InvoiceNo,'') = @InvoiceNo";
            using var cmd = new SqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@FormNo", formNo ?? "");
            cmd.Parameters.AddWithValue("@SerialNo", serialNo ?? "");
            cmd.Parameters.AddWithValue("@InvoiceNo", invoiceNo ?? "");
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        #endregion

        #region Ghi dữ liệu

        /// <summary>Thông tin folio/khách dùng cho phần đầu hóa đơn.</summary>
        private class FolioInfo
        {
            public int FolioId, ReservationId, ProfileId;
            public string Account = "", Telephone = "", Fax = "", TaxCode = "", FullAccount = "", HomeAddress = "";
        }

        private static FolioInfo LoadFolioInfo(SqlConnection conn, SqlTransaction tx, int folioId, NewInvoiceRequest req, DataTable tblData)
        {
            var info = new FolioInfo();

            if (folioId > 0)
            {
                var folio = ReadRow(conn, tx, "SELECT ID, ReservationID, ProfileID FROM Folio WITH (NOLOCK) WHERE ID = @ID", folioId);
                if (folio != null)
                {
                    info.FolioId = folioId;
                    info.ReservationId = ToInt(folio["ReservationID"]);
                    info.ProfileId = ToInt(folio["ProfileID"]);
                }
            }
            else if (req.IsSameProfile && tblData.Rows.Count > 0)
            {
                // Kiểu "tất cả folio trong 1 hóa đơn" (folioId = 0), cùng profile: lấy khách của folio đầu tiên
                var first = ReadRow(conn, tx, "SELECT ID, ProfileID FROM Folio WITH (NOLOCK) WHERE ID = @ID", ToInt(tblData.Rows[0]["FolioID"]));
                if (first != null) info.ProfileId = ToInt(first["ProfileID"]);
            }

            if (info.ProfileId > 0)
            {
                var pr = ReadRow(conn, tx, "SELECT ID, Account, Telephone, Fax, TaxCode, FullAccount, HomeAddress FROM Profile WITH (NOLOCK) WHERE ID = @ID", info.ProfileId);
                if (pr != null)
                {
                    info.Account = ToStr(pr["Account"]);
                    info.Telephone = ToStr(pr["Telephone"]);
                    info.Fax = ToStr(pr["Fax"]);
                    info.TaxCode = ToStr(pr["TaxCode"]);
                    info.FullAccount = ToStr(pr["FullAccount"]);
                    info.HomeAddress = ToStr(pr["HomeAddress"]);
                }
            }
            return info;
        }

        /// <summary>
        /// Các cột thật sự có của một bảng trong DB Property. Mỗi Property có thể ở một phiên bản cấu trúc khác nhau
        /// (ví dụ DB cũ chưa có ProfileCompanyID), nên chỉ ghi những cột tồn tại.
        /// </summary>
        private static HashSet<string> ColumnsOf(SqlConnection conn, SqlTransaction tx, string table)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var cmd = new SqlCommand("SELECT name FROM sys.columns WHERE object_id = OBJECT_ID(@t)", conn, tx);
            cmd.Parameters.AddWithValue("@t", table);
            using var rd = cmd.ExecuteReader();
            while (rd.Read()) set.Add(rd.GetString(0));
            return set;
        }

        /// <summary>INSERT chỉ gồm các cột có trong bảng; trả về ID mới (SCOPE_IDENTITY) hoặc 0 nếu không cần.</summary>
        private static int InsertDynamic(SqlConnection conn, SqlTransaction tx, string table, HashSet<string> columns,
            IEnumerable<KeyValuePair<string, object?>> values, bool returnId)
        {
            var use = values.Where(v => columns.Contains(v.Key)).ToList();
            string cols = string.Join(", ", use.Select(v => v.Key));
            string pars = string.Join(", ", use.Select((v, i) => "@p" + i));
            // Lấy ID mới bằng SCOPE_IDENTITY(): bảng của một số Property có trigger, khi đó SQL Server không cho dùng OUTPUT không có INTO
            string sql = $"INSERT INTO {table} ({cols}) VALUES ({pars})" + (returnId ? "; SELECT CAST(SCOPE_IDENTITY() AS int)" : "");

            using var cmd = new SqlCommand(sql, conn, tx);
            for (int i = 0; i < use.Count; i++) cmd.Parameters.AddWithValue("@p" + i, use[i].Value ?? DBNull.Value);
            return returnId ? Convert.ToInt32(cmd.ExecuteScalar()) : cmd.ExecuteNonQuery();
        }

        private static int InsertHeader(SqlConnection conn, SqlTransaction tx, HashSet<string> columns, NewInvoiceRequest req, FolioInfo info,
            string invoiceNo, string loginName, DateTime sysDate, DateTime businessDate)
        {
            bool isGroupByAll = req.GroupType == 1;
            bool isSummary = req.GroupType == 1 || req.GroupType == 2 || req.GroupType == 3;

            // Hóa đơn gộp tất cả folio mà không cùng profile: không gắn khách cụ thể
            bool generic = isGroupByAll && !req.IsSameProfile;
            int profileId = generic ? 0 : info.ProfileId;

            var v = new Dictionary<string, object?>
            {
                ["ReservationID"] = info.ReservationId,
                ["FolioID"] = info.FolioId,
                ["VATDate"] = req.VATDate,
                ["FormNo"] = req.FormNo.Trim(),
                ["SerialNo"] = req.SerialNo.Trim(),
                ["InvoiceNo"] = invoiceNo,
                ["Amount"] = 0m,
                ["AmountVAT5"] = 0m,
                ["AmountVAT10"] = 0m,
                ["AmountIncludedTax"] = 0m,
                ["PaymentMethod"] = req.PaymentMethod ?? "",
                ["ExchangeRate"] = req.ExchangeRate,
                ["CurrencyID"] = req.CurrencyID ?? "VND",
                ["Note"] = req.Note ?? "",
                ["Customer_Name"] = generic ? "Hóa đơn tổng hợp" : info.Account,
                ["Customer_Company"] = generic ? "" : info.FullAccount,
                ["Customer_Address"] = generic ? "" : info.HomeAddress,
                ["Customer_Tel"] = generic ? "" : info.Telephone,
                ["Customer_Fax"] = generic ? "" : info.Fax,
                ["Customer_TaxCode"] = generic ? "" : info.TaxCode,
                ["Status"] = (byte)1,
                ["CreatedDate"] = sysDate,
                ["CreatedBy"] = loginName ?? "",
                ["UpdatedDate"] = sysDate,
                ["UpdatedBy"] = loginName ?? "",
                ["CountPrint"] = (byte)0,
                ["CountSend"] = (byte)0,
                ["IsManualCreate"] = false,
                ["LicenseID"] = (byte)0,
                ["IsSummaryVAT"] = isSummary,
                ["PrintVATID"] = 0,
                ["SummaryVATType"] = (byte)(req.GroupType >= 1 && req.GroupType <= 3 ? req.GroupType : 0),
                ["IsNoName"] = false,
                ["IsMasterVAT"] = false,
                ["TransactionDate"] = businessDate,
                ["IsModify"] = false,
                ["IsCancel"] = false,
                ["IsSameProfile"] = req.IsSameProfile,
                ["ProfileID"] = profileId,
                ["IsTransfer"] = false,
                ["ProfileCompanyID"] = profileId // theo các hóa đơn gần nhất trong DB (bản cũ của web không gán)
            };
            return InsertDynamic(conn, tx, "FolioVAT", columns, v, returnId: true);
        }

        private static (decimal Amount, decimal AmountIncludedTax, int VAT, decimal VATAmount) InsertDetail(
            SqlConnection conn, SqlTransaction tx, HashSet<string> columns, int invoiceId, DataRow dr, string loginName, DateTime sysDate)
        {
            decimal amount = ToDec(dr, "Amount");
            int vat = (int)ToDec(dr, "VAT");
            decimal vatAmount = ToDec(dr, "VATAmount");
            decimal amountInc = ToDec(dr, "AmountIncludedTax");

            var v = new Dictionary<string, object?>
            {
                ["FolioDetailID"] = (int)ToDec(dr, "ID"),
                ["FolioVATID"] = invoiceId,
                ["Amount"] = amount,
                ["VAT"] = vat,
                ["VATAmount"] = vatAmount,
                ["AmountIncludedTax"] = amountInc,
                ["Status"] = (byte)1,
                ["CreatedDate"] = sysDate,
                ["CreatedBy"] = loginName ?? "",
                ["UpdatedDate"] = sysDate,
                ["UpdatedBy"] = loginName ?? "",
                ["Svc"] = ToDec(dr, "SVC"),
                ["SvcAmount"] = ToDec(dr, "SvcAmount"),
                ["Price"] = ToDec(dr, "Price"),
                ["Quantity"] = Math.Round(ToDec(dr, "Quantity"), MidpointRounding.AwayFromZero)
            };
            InsertDynamic(conn, tx, "FolioVATDetail", columns, v, returnId: false);

            return (amount, amountInc, vat, vatAmount);
        }

        private static void UpdateHeaderTotals(SqlConnection conn, SqlTransaction tx, HashSet<string> columns, int invoiceId,
            decimal amount, decimal vat5, decimal vat10, decimal amountInc)
        {
            var v = new Dictionary<string, object?>
            {
                ["Amount"] = amount,
                ["AmountVAT5"] = vat5,
                ["AmountVAT10"] = vat10,
                ["AmountIncludedTax"] = amountInc,
                ["IsModify"] = amountInc < 0 // tổng âm: đánh dấu hóa đơn điều chỉnh
            };
            var use = v.Where(x => columns.Contains(x.Key)).ToList();
            string set = string.Join(", ", use.Select((x, i) => $"{x.Key} = @p{i}"));

            using var cmd = new SqlCommand($"UPDATE FolioVAT SET {set} WHERE ID = @ID", conn, tx);
            for (int i = 0; i < use.Count; i++) cmd.Parameters.AddWithValue("@p" + i, use[i].Value ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ID", invoiceId);
            cmd.ExecuteNonQuery();
        }

        #endregion

        #region Tiện ích

        private static DataRow? ReadRow(SqlConnection conn, SqlTransaction tx, string sql, int id)
        {
            using var cmd = new SqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@ID", id);
            var dt = new DataTable();
            new SqlDataAdapter(cmd).Fill(dt);
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        private static int ToInt(object? v) => v == null || v == DBNull.Value ? 0 : Convert.ToInt32(v);
        private static string ToStr(object? v) => v == null || v == DBNull.Value ? "" : Convert.ToString(v) ?? "";

        private static decimal ToDec(DataRow dr, string column)
        {
            if (!dr.Table.Columns.Contains(column)) return 0;
            var v = dr[column];
            return v == DBNull.Value ? 0 : Convert.ToDecimal(v);
        }

        #endregion
    }
}
