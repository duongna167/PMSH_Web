using System.Data;
using BaseBusiness.util;
using Microsoft.Data.SqlClient;
using NightAudit.Services.Interfaces;

namespace NightAudit.Services.Implements
{
    /// <summary>Port của frmExportRevenue_New.LoadData (PMS desktop): giữ nguyên công thức nợ/có, làm tròn và thứ tự dòng.</summary>
    public class ExportRevenueService : IExportRevenueService
    {
        /// <summary>Đọc số từ DataRow; rỗng/NULL = 0 (như TextUtils.ToDecimal của bản desktop).</summary>
        private static decimal Dec(DataRow r, string col)
        {
            if (!r.Table.Columns.Contains(col) || r[col] == DBNull.Value) return 0;
            return decimal.TryParse(Convert.ToString(r[col], System.Globalization.CultureInfo.InvariantCulture),
                System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal v) ? v : 0;
        }

        private static string Str(DataRow r, string col) =>
            r.Table.Columns.Contains(col) && r[col] != DBNull.Value ? Convert.ToString(r[col]) ?? "" : "";

        private static string ConfigValue(string key)
        {
            DataTable dt = DataTableHelper.ExecuteQuery("SELECT KeyValue FROM ConfigSystem WHERE KeyName = @k",
                new[] { new SqlParameter("@k", key) });
            return dt.Rows.Count > 0 ? Convert.ToString(dt.Rows[0]["KeyValue"]) ?? "" : "";
        }

        public ExportRevenueResult Search(DateTime date, bool summary)
        {
            var result = new ExportRevenueResult();
            date = date.Date;

            // 1) Mọi transaction code phát sinh trong ngày (trừ GroupType = 2) phải đã được map kế toán
            DataTable unmapped = DataTableHelper.ExecuteQuery(
                "SELECT DISTINCT a.TransactionCode FROM FolioDetail a WITH (NOLOCK) " +
                "JOIN Transactions b ON a.TransactionCode = b.Code " +
                "WHERE a.TransactionDate >= @d AND a.TransactionDate < DATEADD(day, 1, @d) AND b.GroupType <> 2 " +
                "AND a.TransactionCode NOT IN (SELECT TransactionCode FROM InterfaceToAcc WHERE AccountCode <> '' AND TransactionCode IS NOT NULL)",
                new[] { new SqlParameter("@d", date) });
            if (unmapped.Rows.Count > 0)
            {
                string list = string.Join("; ", unmapped.AsEnumerable().Select(r => Convert.ToString(r["TransactionCode"])));
                result.Error = "This Transaction Code : " + list + "; not in Mapping";
                return result;
            }

            string accountGuestLedger = ConfigValue("AccountCodeGuestLedger");
            string customerGuestLedger = ConfigValue("CustomerCodeGuestLedger");

            // 2) Dữ liệu tham chiếu: bảng map tài khoản (lấy dòng đầu theo TransactionCode) và tỷ giá USD -> VND
            var map = new Dictionary<string, DataRow>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in DataTableHelper.ExecuteQuery("SELECT * FROM InterfaceToAcc WITH (NOLOCK) ORDER BY TransactionCode").Rows)
                map.TryAdd(Str(r, "TransactionCode"), r);

            decimal exchange = TextUtils.ExchangeCurrency(date, "USD", "VND", 1);
            if (exchange == 0)
            {
                result.Error = "Exchange rate USD to VND is not defined for " + date.ToString("dd/MM/yyyy") + ".";
                return result;
            }

            // 3) Tổng doanh thu = số ghi Nợ của dòng "Công nợ khách hàng"
            decimal debitTotal = 0;
            DataTable sum = DataTableHelper.getTableData("spExportSumAmount", new[] { new SqlParameter("@dtpFromDate", date) });
            if (sum.Rows.Count > 0 && (Str(sum.Rows[0], "AmountUSD") != "" || Str(sum.Rows[0], "AmountVND") != ""))
                debitTotal = Dec(sum.Rows[0], "AmountUSD") * exchange + Dec(sum.Rows[0], "AmountVND");

            // 4) Các transaction cần export (Summary / Detail)
            DataTable group = DataTableHelper.getTableData(summary ? "spExportQuerySum_New" : "spExportQueryDetail_New",
                new[] { new SqlParameter("@dtpFromDate", date) });

            string prefix = date.ToString("dd/MM/yyyy").Substring(0, 5);
            var rows = new List<ExportRevenueRow>();
            bool guestLedgerAdded = false;

            foreach (DataRow g in group.Rows)
            {
                string code = Str(g, "Code");
                if (code == "8508") continue;

                string groupType = Str(g, "GroupType");
                bool inner = Str(g, "xxx") == "1"; // công nợ chi tiết bên trong (dòng Payment)

                // Chèn dòng Guest Ledger (Nợ = tổng doanh thu) ngay trước dòng đầu tiên của nhóm doanh thu
                if (groupType == "0" && !guestLedgerAdded)
                {
                    guestLedgerAdded = true;
                    rows.Add(new ExportRevenueRow
                    {
                        AccountCode = accountGuestLedger,
                        CustomerCode = customerGuestLedger,
                        Description = prefix + " " + "< Công nợ khách hàng >",
                        DebitUSD = Math.Round(debitTotal / exchange, 2),
                        DebitVND = Math.Round(debitTotal, 0),
                        NhomDK = 2
                    });
                }

                DataRow? m = inner ? g : (map.TryGetValue(code, out DataRow? found) ? found : null);
                if (m == null)
                {
                    result.Error = "Transaction Code " + code + " is not in Mapping.";
                    return result;
                }

                var row = new ExportRevenueRow
                {
                    AccountCode = inner ? Str(m, "Code") : Str(m, "AccountCode"),
                    CustomerCode = inner ? "" : Str(m, "MaCN_ACC"),
                    Description = prefix + Str(m, "Description"),
                    NhomDK = int.TryParse(groupType, out int gt) && gt == 1 ? 1 : 2
                };

                // Desktop: Payment hiện ở cột Nợ, Revenue hiện ở cột Có (đảo so với SP). CreditUSD tại bước này luôn 0
                // vì bản desktop chia cho DebitVND của dòng mới (chưa có giá trị).
                row.CreditVND = Math.Round(Dec(g, "DebitVND") + Dec(g, "DebitUSD") * exchange, 0);
                row.CreditUSD = 0;
                row.DebitVND = Math.Round(Dec(g, "CreditVND") + Dec(g, "CreditUSD") * exchange, 0);
                row.DebitUSD = Math.Round(row.CreditVND / exchange, 2);

                // Công nợ chi tiết của Payment: giữ đúng chiều Nợ/Có của SP
                if (inner)
                {
                    row.CreditVND = Math.Round(Dec(g, "CreditVND") + Dec(g, "CreditUSD") * exchange, 0);
                    row.CreditUSD = Math.Round(row.DebitVND / exchange, 2);
                    row.DebitVND = Math.Round(Dec(g, "DebitVND") + Dec(g, "DebitUSD") * exchange, 0);
                    row.DebitUSD = Math.Round(row.CreditVND / exchange, 2);
                }

                // Số Nợ âm thì chuyển sang cột Có và đổi nhóm định khoản
                if (row.DebitVND < 0)
                {
                    row.CreditVND = -row.DebitVND;
                    row.NhomDK = 2;
                    row.DebitVND = 0;
                }
                if (row.DebitUSD < 0)
                {
                    row.CreditUSD = -row.DebitUSD;
                    row.NhomDK = 2;
                    row.DebitUSD = 0;
                }

                if (inner)
                {
                    row.MaBoPhan = "";
                    row.MaDoanhThu = "";
                    row.MaBPDC = "";
                }
                else
                {
                    // Desktop tách MaBP_ACC theo ZoneCode nhưng phép so sánh "H2" luôn sai nên kết quả cuối luôn là MaBP_ACC
                    row.MaBoPhan = Str(m, "MaBP_ACC");
                    row.MaDoanhThu = Str(m, "MaDT_ACC");
                    row.MaBPDC = Str(m, "MaBPDC");
                }

                rows.Add(row);
            }

            // Sắp xếp như lưới desktop: NhomDK tăng dần, DebitUSD giảm dần (OrderBy ổn định giữ thứ tự gốc khi bằng nhau)
            result.Rows = rows.OrderBy(r => r.NhomDK).ThenByDescending(r => r.DebitUSD).ToList();
            return result;
        }

        // ================================================================
        // Các bản By market / Sun System / By invoice
        // (frmExportRevenueByMarket, frmExportRevenue_SunSystem, frmExportRevenue_FLC_V1)
        // ================================================================

        private const string CongNo = "< Công nợ khách hàng >";

        public (List<(int Id, string Name)> Markets, List<(int Id, string Name)> Sources) GetMarketSourceLookups()
        {
            static List<(int, string)> Load(string sql) =>
                DataTableHelper.ExecuteQuery(sql).AsEnumerable()
                    .Select(r => (Convert.ToInt32(r["ID"]), Convert.ToString(r["Name"]) ?? "")).ToList();
            return (Load("SELECT ID, Name FROM Market ORDER BY Name"), Load("SELECT ID, Name FROM Source ORDER BY Name"));
        }

        /// <summary>Làm tròn như Math.Round của desktop (banker's rounding mặc định).</summary>
        private static decimal R(decimal v, int digits) => Math.Round(v, digits);

        public ExportAccountingResult SearchAccounting(string mode, DateTime date, string marketIds, string sourceIds)
        {
            var res = new ExportAccountingResult();
            date = date.Date;
            bool invoice = mode == "invoice";
            bool market = mode == "market";

            // 1) Mọi transaction code phát sinh trong ngày phải đã được map (bản By invoice chỉ cần có dòng map, không cần AccountCode)
            DataTable unmapped = DataTableHelper.ExecuteQuery(
                "SELECT DISTINCT TransactionCode FROM FolioDetail WITH (NOLOCK) " +
                "WHERE TransactionDate >= @d AND TransactionDate < DATEADD(day, 1, @d) " +
                "AND TransactionCode NOT IN (SELECT TransactionCode FROM InterfaceToAcc WHERE " +
                (invoice ? "" : "AccountCode <> '' AND ") + "TransactionCode IS NOT NULL)",
                new[] { new SqlParameter("@d", date) });
            if (unmapped.Rows.Count > 0)
            {
                res.Error = "This Transaction Code : " + string.Join("; ", unmapped.AsEnumerable().Select(r => Convert.ToString(r["TransactionCode"]))) + "; not in Mapping";
                return res;
            }

            string accountGuestLedger = ConfigValue("AccountCodeGuestLedger");
            string customerGuestLedger = ConfigValue("CustomerCodeGuestLedger");

            var map = new Dictionary<string, DataRow>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in DataTableHelper.ExecuteQuery("SELECT * FROM InterfaceToAcc WITH (NOLOCK) ORDER BY TransactionCode").Rows)
                map.TryAdd(Str(r, "TransactionCode"), r);
            var zones = DataTableHelper.ExecuteQuery("SELECT ID, ZoneCode FROM RoomType WITH (NOLOCK)").AsEnumerable()
                .ToDictionary(r => Convert.ToString(r["ID"]) ?? "", r => Convert.ToString(r["ZoneCode"]) ?? "");

            decimal ex = TextUtils.ExchangeCurrency(date, "USD", "VND", 1);
            if (ex == 0)
            {
                res.Error = "Exchange rate USD to VND is not defined for " + date.ToString("dd/MM/yyyy") + ".";
                return res;
            }

            var p = new[] { new SqlParameter("@dtpFromDate", date) };
            static SqlParameter[] Clone(SqlParameter[] a) => a.Select(x => new SqlParameter(x.ParameterName, x.Value)).ToArray();

            // 2) Tổng Payment (Có) trừ thêm tiền giảm trừ doanh thu (Rebate); tổng doanh thu (Nợ)
            decimal creditTotal = 0, debitTotal = 0;
            DataTable pay = DataTableHelper.getTableData("spExportPayment", Clone(p));
            if (pay.Rows.Count > 0 && (Str(pay.Rows[0], "AmountUSD") != "" || Str(pay.Rows[0], "AmountVND") != ""))
                creditTotal = -1 * Dec(pay.Rows[0], "AmountUSD") * ex - Dec(pay.Rows[0], "AmountVND");
            DataTable rebate = DataTableHelper.getTableData("spExportRebate", Clone(p));
            if (rebate.Rows.Count > 0 && (Str(rebate.Rows[0], "AmountUSD") != "" || Str(rebate.Rows[0], "AmountVND") != ""))
                creditTotal += -1 * (Dec(rebate.Rows[0], "AmountUSD") * ex) - Dec(rebate.Rows[0], "AmountVND");
            DataTable sum = DataTableHelper.getTableData(invoice ? "spExportSumAmount_Ver1" : "spExportSumAmount", Clone(p));
            bool hasSum = sum.Rows.Count > 0 && (Str(sum.Rows[0], "AmountUSD") != "" || Str(sum.Rows[0], "AmountVND") != "");
            if (hasSum) debitTotal = Dec(sum.Rows[0], "AmountUSD") * ex + Dec(sum.Rows[0], "AmountVND");

            // 3) Các transaction cần export
            DataTable group;
            if (market)
            {
                var gp = Clone(p).ToList();
                gp.Add(new SqlParameter("@MarketID", marketIds ?? ""));
                gp.Add(new SqlParameter("@SourceID", sourceIds ?? ""));
                group = DataTableHelper.getTableData("spExportQuerySumByMarket", gp.ToArray());
            }
            else
                group = DataTableHelper.getTableData(invoice ? "spExportQuerySum_Ver1" : "spExportQuerySum", Clone(p));

            string prefix = date.ToString("dd/MM/yyyy").Substring(0, 5);
            var rows = new List<ExportRevenueRow>(); // đúng thứ tự phát sinh (Excel By market xuất theo thứ tự này)
            bool guestLedgerAdded = false, rebateAdded = false;

            foreach (DataRow g in group.Rows)
            {
                string code = Str(g, "Code");
                if (code == "8508") continue;
                string groupType = Str(g, "GroupType");

                // Dòng giảm trừ doanh thu (Rebate) chèn trước dòng đầu tiên của nhóm doanh thu — chỉ bản By market / Sun System
                if (!invoice && !rebateAdded && groupType == "0")
                {
                    rebateAdded = true;
                    DataTable reb = DataTableHelper.getTableData("spExportSqlSummary", Clone(p));
                    foreach (DataRow d in reb.Rows)
                    {
                        if (!map.TryGetValue(Str(d, "Code"), out DataRow? rm))
                        {
                            res.Error = "Transaction Code " + Str(d, "Code") + " is not in Mapping.";
                            return res;
                        }
                        var rr = new ExportRevenueRow
                        {
                            NhomDK = 1,
                            AccountCode = Str(rm, "AccountCode"),
                            CustomerCode = Str(rm, "MaCN_ACC"),
                            Description = prefix + " " + Str(rm, "Description"),
                            DebitVND = R(Dec(d, "DebitVND") + Dec(d, "DebitUSD") * ex, 0)
                        };
                        rr.DebitUSD = R(rr.DebitVND / ex, 2);
                        rr.CreditVND = R(Dec(d, "CreditVND") + Dec(d, "CreditUSD") * ex, 0);
                        rr.CreditUSD = R(rr.CreditVND / ex, 2);
                        // Desktop: số Nợ âm thì đổi nhóm định khoản và đặt Có = 0 (số Nợ âm được giữ nguyên)
                        if (rr.DebitVND < 0) { rr.NhomDK = 2; rr.CreditVND = 0; }
                        if (rr.DebitUSD < 0) { rr.NhomDK = 2; rr.CreditUSD = 0; }

                        // Bộ phận: tách MaBP_ACC theo zone của loại phòng (H2 có nhiều bộ phận thì lấy phần tử thứ 2)
                        string bp = Str(rm, "MaBP_ACC");
                        string[] arr = bp.Split(',');
                        rr.MaBoPhan = zones.TryGetValue(Str(d, "RoomTypeID"), out string? z) && z == "H2" && arr.Length > 1 ? arr[1].Trim() : bp;
                        rr.MaDoanhThu = Str(rm, "MaDT_ACC");
                        rr.MaBPDC = Str(rm, "MaBPDC");
                        rows.Add(rr);
                    }
                }

                // Dòng Guest Ledger ngay trước dòng đầu tiên của nhóm doanh thu
                if (groupType == "0" && !guestLedgerAdded)
                {
                    guestLedgerAdded = true;
                    // Ô Total Credit Amount của desktop = tổng Payment (và giảm trừ) ghi ở dòng Guest Ledger bên Có
                    res.TotalCredit = creditTotal.ToString("###,###,###", System.Globalization.CultureInfo.InvariantCulture);
                    rows.Add(new ExportRevenueRow
                    {
                        AccountCode = accountGuestLedger,
                        CustomerCode = customerGuestLedger,
                        Description = prefix + " " + CongNo,
                        CreditUSD = R(creditTotal / ex, 2),
                        CreditVND = R(creditTotal, 0),
                        NhomDK = 1
                    });

                    if (!invoice)
                    {
                        rows.Add(new ExportRevenueRow
                        {
                            AccountCode = accountGuestLedger,
                            CustomerCode = customerGuestLedger,
                            Description = prefix + " " + CongNo,
                            DebitUSD = R(debitTotal / ex, 2),
                            DebitVND = R(debitTotal, 0),
                            NhomDK = 2
                        });
                    }
                    else if (hasSum)
                    {
                        // Bản By invoice: mỗi hóa đơn một dòng công nợ
                        foreach (DataRow s in sum.Rows)
                        {
                            decimal vnd = Dec(s, "AmountUSD") * ex + Dec(s, "AmountVND");
                            rows.Add(new ExportRevenueRow
                            {
                                AccountCode = accountGuestLedger,
                                CustomerCode = customerGuestLedger,
                                Description = prefix + " " + CongNo,
                                DebitUSD = R(vnd / ex, 2),
                                DebitVND = R(vnd, 0),
                                InvoiceNo = Str(s, "InvoiceNo"),
                                NhomDK = 2
                            });
                        }
                    }
                }

                // Ô Total Debit Amount của desktop = Nợ VND của dòng transaction 903 (công nợ khách hàng)
                if (code == "903") res.TotalDebit = Dec(g, "DebitVND").ToString("###,###,###", System.Globalization.CultureInfo.InvariantCulture);

                // Dòng tổng của transaction
                if (!map.TryGetValue(code, out DataRow? m))
                {
                    res.Error = "Transaction Code " + code + " is not in Mapping.";
                    return res;
                }
                var row = new ExportRevenueRow
                {
                    AccountCode = Str(m, "AccountCode"),
                    CustomerCode = Str(m, "MaCN_ACC"),
                    Description = prefix + Str(m, "Description"),
                    NhomDK = int.TryParse(groupType, out int gt) && gt == 1 ? 1 : 2,
                    CreditVND = R(Dec(g, "CreditVND") + Dec(g, "CreditUSD") * ex, 0),
                    DebitVND = R(Dec(g, "DebitVND") + Dec(g, "DebitUSD") * ex, 0),
                    TransactionCode = code,
                    InvoiceNo = invoice ? Str(g, "InvoiceNo") : ""
                };
                row.CreditUSD = R(row.CreditVND / ex, 2);
                row.DebitUSD = R(row.DebitVND / ex, 2);

                if (row.DebitVND < 0) { row.CreditVND = -row.DebitVND; row.NhomDK = 2; row.DebitVND = 0; }
                if (row.DebitUSD < 0) { row.CreditUSD = -row.DebitUSD; row.NhomDK = 2; row.DebitUSD = 0; }

                // Desktop so sánh ZoneCode == "H2" bằng object nên luôn sai: bộ phận luôn là MaBP_ACC
                row.MaBoPhan = Str(m, "MaBP_ACC");
                row.MaDoanhThu = Str(m, "MaDT_ACC");
                row.MaBPDC = Str(m, "MaBPDC");
                rows.Add(row);
            }

            res.Rows = rows.OrderBy(r => r.NhomDK).ThenByDescending(r => r.DebitUSD).ToList();

            if (market) BuildMarketExcel(res, rows);
            else BuildAccountingOutputExcel(res, rows, date, invoice);
            return res;
        }

        /// <summary>Excel By market (file mẫu ImportToFLC): xuất đúng thứ tự phát sinh, dữ liệu từ dòng 6.</summary>
        private static void BuildMarketExcel(ExportAccountingResult res, List<ExportRevenueRow> rows)
        {
            res.ExcelStartRow = 6;
            res.ExcelFileName = "ImportToFLC.xlsx";
            res.ExcelHeaders = new() { [3] = "Số hóa đơn", [4] = "Ngày", [8] = "Mã TK", [9] = "Tên TK", [10] = "Mã khách", [11] = "Tên khách", [12] = "Phát sinh nợ", [13] = "Phát sinh có", [14] = "Diễn giải", [16] = "Bộ phận", [22] = "Nhóm ĐK" };
            res.ExcelRows = rows.Select(r => new Dictionary<int, object?>
            {
                [8] = r.AccountCode, [9] = r.AccountName, [3] = r.InvoiceNo, [10] = r.CustomerCode, [11] = r.CustomerName,
                [12] = r.DebitVND, [13] = r.CreditVND, [14] = r.Description, [16] = r.MaBoPhan, [22] = r.NhomDK
            }).ToList();
        }

        /// <summary>
        /// Excel Sun System / By invoice: desktop ghi các dòng vào bảng AccountingOutPut rồi đọc lại theo thứ tự D_C, AccountCode
        /// (VND = Nợ + Có, D_C = "D" nếu NhomDK = 1, ngược lại "C"). Web tính trực tiếp trong bộ nhớ, không ghi bảng này.
        /// </summary>
        private static void BuildAccountingOutputExcel(ExportAccountingResult res, List<ExportRevenueRow> rows, DateTime date, bool invoice)
        {
            var ci = StringComparer.OrdinalIgnoreCase;
            var list = rows.Select(r => new
            {
                Row = r,
                DC = r.NhomDK == 1 ? "D" : "C",
                Vnd = r.DebitVND + r.CreditVND
            }).OrderBy(x => x.DC, ci).ThenBy(x => x.Row.AccountCode, ci).Select(x => new Out { Row = x.Row, DC = x.DC, Vnd = x.Vnd }).ToList();

            bool IsCongNo(Out o) => o.Row.Description.Contains(CongNo);

            if (!invoice)
            {
                // ===== Sun System (file mẫu ImportToAVANI): dữ liệu từ dòng 2, mã tài khoản theo cấu hình của khách sạn AVANI =====
                decimal totalD = 0, totalC = 0;
                foreach (var o in list)
                {
                    if (o.DC == "D" && !IsCongNo(o)) totalD += o.Vnd;
                    if (o.DC == "C" && !IsCongNo(o)) totalC += o.Vnd;
                    // Dòng công nợ của tài khoản 13110000 đảo chiều Nợ/Có
                    if (o.Row.AccountCode == "13110000" && IsCongNo(o)) o.DC = o.DC == "C" ? "D" : "C";
                }
                foreach (var o in list)
                {
                    if (o.DC == "C" && o.Row.AccountCode == "13110000" && IsCongNo(o)) o.Vnd = totalD;
                    else if (o.DC == "D" && o.Row.AccountCode == "13110000" && IsCongNo(o)) o.Vnd = totalC;
                }

                string[] saleCodes = { "800", "923", "913", "900", "911", "912", "910", "914", "903", "924", "922", "926", "920", "943" };
                res.ExcelStartRow = 2;
                res.ExcelFileName = "ImportToAVANI.xlsx";
                res.ExcelHeaders = new() { [1] = "Mã TK", [2] = "Kỳ", [3] = "Ngày", [4] = "Số tiền", [5] = "D/C", [6] = "Loại", [7] = "Tham chiếu", [8] = "Diễn giải", [10] = "Tiền tệ", [11] = "Số tiền gốc", [12] = "Bộ phận", [13] = "ANAL_T1", [15] = "ANAL_T3", [16] = "ANAL_T4", [19] = "ANAL_T7", [20] = "ANAL_T8", [26] = "Diễn giải 2" };
                foreach (var o in list)
                {
                    string acc = o.Row.AccountCode;
                    var c = new Dictionary<int, object?>
                    {
                        [1] = acc,
                        [2] = date.ToString("yyyyMM"),
                        [3] = date.ToString("ddMMyyyy"),
                        [4] = o.Vnd,
                        [5] = o.DC,
                        [6] = "SHJ",
                        [7] = "SHJ" + date.ToString("yyMM") + "0" + date.ToString("dd"),
                        [8] = o.Row.Description,
                        [10] = "VND",
                        [11] = o.Vnd,
                        [12] = o.Row.MaBoPhan,
                        [13] = o.Row.MaDoanhThu,
                        [26] = o.Row.Description
                    };
                    if (acc == "13110000" && IsCongNo(o)) c[19] = "S";
                    if (acc == "33311000") c[15] = "O10";
                    if (acc == "33311000" || acc == "11110100" || acc == "51133550") c[16] = "Z";
                    if (acc == "11110100" || acc == "51133550") c[19] = "S";
                    if (saleCodes.Contains(o.Row.TransactionCode)) c[19] = "S";
                    if (acc == "11110100" || acc == "51133550") c[20] = "01";
                    res.ExcelRows.Add(c);
                }
                return;
            }

            // ===== By invoice (file Export_Revenue_By_Invoice): dữ liệu từ dòng 6 =====
            // Nhóm định khoản: đánh số theo số hóa đơn tăng dần; các dòng Có (NhomDK = 1) không phải công nợ dùng nhóm cuối cùng
            static int ToInt(string s) => int.TryParse(s, out int n) ? n : 0;
            var invoices = list.Select(o => o.Row.InvoiceNo).Where(s => s != "" && s != "0").Select(ToInt).Distinct().OrderBy(n => n).ToList();
            int step = 0;
            foreach (int inv in invoices)
            {
                step++;
                foreach (var o in list.Where(o => ToInt(o.Row.InvoiceNo) == inv)) o.DinhKhoan = step;
            }
            step++;
            foreach (var o in list)
            {
                if (IsCongNo(o)) o.DC = o.DC == "C" ? "D" : o.DC == "D" ? "C" : o.DC;
                if (!IsCongNo(o) && o.Row.NhomDK == 1) o.DinhKhoan = step;
            }

            res.ExcelStartRow = 6;
            res.ExcelFileName = "Export_Revenue_By_Invoice.xlsx";
            res.ExcelHeaders = new() { [1] = "Mã TK", [2] = "Tên TK", [3] = "Mã khách", [4] = "Tên khách", [5] = "Phát sinh nợ", [6] = "Phát sinh có", [7] = "Diễn giải", [9] = "Bộ phận", [13] = "Mã DT", [15] = "Nhóm ĐK", [16] = "Số hóa đơn", [17] = "Ngày hóa đơn" };
            foreach (var o in list)
            {
                res.ExcelRows.Add(new Dictionary<int, object?>
                {
                    [1] = o.Row.AccountCode,
                    [2] = o.Row.AccountName,
                    [3] = o.Row.CustomerCode,
                    [4] = o.Row.CustomerName,
                    [5] = o.DC == "D" ? o.Vnd : 0m,
                    [6] = o.DC == "C" ? o.Vnd : 0m,
                    [7] = o.Row.Description,
                    [9] = o.Row.MaBoPhan,
                    [13] = o.Row.MaDoanhThu,
                    [15] = o.DinhKhoan?.ToString() ?? "",
                    [16] = o.Row.InvoiceNo,
                    [17] = date.ToString("dd/MM/yyyy")
                });
            }
        }

        /// <summary>Một dòng của bảng AccountingOutPut khi tính Excel.</summary>
        private class Out
        {
            public ExportRevenueRow Row = null!;
            public string DC = "";
            public decimal Vnd;
            public int? DinhKhoan;
        }
    }
}
