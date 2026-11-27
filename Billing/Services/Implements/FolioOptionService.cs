using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace Billing.Services.Implements
{
    /// <summary>
    /// Port từ WinForm frmCshFolioOption: chuẩn bị dữ liệu cho các kiểu in Folio.
    /// Mỗi kiểu in (FolioStyle) có một hàm Build riêng; client chỉ việc render bản in.
    /// </summary>
    public class FolioOptionService
    {
        #region Kiểu in
        public const string STYLE_BILLING_INFOR = "BILLING_INFOR";
        public const string STYLE_BILLING_INFOR_QUANTITY = "BILLING_INFOR_QUANTITY";
        public const string STYLE_BILLING_INFOR_BY_SELECTED = "BILLING_INFOR_BY_SELECTED";
        public const string STYLE_FOLIO_BY_DATE = "FOLIO_BY_DATE";
        public const string STYLE_FOLIO_DETAIL = "FOLIO_DETAIL";
        public const string STYLE_FOLIO_SUMMARY = "FOLIO_SUMMARY";
        public const string STYLE_FOLIO_VAT_GUEST = "FOLIO_VAT_GUEST";
        public const string STYLE_FOLIO_VAT_GUEST_NONAME = "FOLIO_VAT_GUEST_NONAME";
        public const string STYLE_FOLIO_VAT_MASTER = "FOLIO_VAT_MASTER";
        public const string STYLE_FOLIO_VAT_GUEST_SUMMARY = "FOLIO_VAT_GUEST_SUMMARY";
        public const string STYLE_FOLIO_VAT_GUEST_DETAIL = "FOLIO_VAT_GUEST_DETAIL";

        /// <summary>Các kiểu in đã port xong trên web.</summary>
        public static readonly HashSet<string> SupportedStyles = new(StringComparer.OrdinalIgnoreCase)
        {
            STYLE_BILLING_INFOR,
            STYLE_BILLING_INFOR_QUANTITY,
            STYLE_BILLING_INFOR_BY_SELECTED,
            STYLE_FOLIO_BY_DATE,
            STYLE_FOLIO_DETAIL,
            STYLE_FOLIO_SUMMARY,
            STYLE_FOLIO_VAT_GUEST,
            STYLE_FOLIO_VAT_GUEST_NONAME,
            STYLE_FOLIO_VAT_MASTER,
            STYLE_FOLIO_VAT_GUEST_SUMMARY,
            STYLE_FOLIO_VAT_GUEST_DETAIL
        };
        #endregion

        private const string DateFormat = "dd/MM/yyyy";
        private const string FmtVND = "###,###,###";
        private const string FmtUSD = "###,###,###.00";
        private static readonly CultureInfo NumCulture = CultureInfo.GetCultureInfo("en-US");

        #region Context (tương đương LoadInformation bên WinForm)
        public class FolioContext
        {
            public FolioModel Folio { get; set; }
            public ReservationModel Reservation { get; set; }
            public DateTime BusinessDate { get; set; }
            public DateTime ExchangeDate { get; set; }
            /// <summary>Ngày tỷ giá trước khi chặn theo BusinessDate (các kiểu by Date/Summary/Selected dùng ngày này).</summary>
            public DateTime ExchangeDateRaw { get; set; }
            public DateTime ArrivalDate { get; set; }
            public DateTime DepartureDate { get; set; }
            public bool IsMasterReservation { get; set; }
        }

        public FolioContext LoadContext(int folioId)
        {
            var mF = (FolioModel)FolioBO.Instance.FindByPrimaryKey(folioId);
            if (mF == null || mF.ID == 0)
                throw new Exception("Folio not found.");
            var mR = (ReservationModel)ReservationBO.Instance.FindByPrimaryKey(mF.ReservationID);
            if (mR == null)
                throw new Exception("Reservation not found.");

            var ctx = new FolioContext
            {
                Folio = mF,
                Reservation = mR,
                BusinessDate = TextUtils.GetBusinessDate(),
                ArrivalDate = mR.ArrivalDate,
                DepartureDate = mR.DepartureDate,
                IsMasterReservation = (mR.ReservationNo ?? "").Equals("0")
            };

            // Nếu là master (ReservationNo = 0) -> lấy ngày đến/đi của cả group
            if (ctx.IsMasterReservation)
            {
                DataTable srcDate = TextUtils.Select("Select FirstArrival,LastDeparture from ReservationGroup where ConfirmationNo='" + SqlSafe(mR.ConfirmationNo) + "'");
                if (srcDate.Rows.Count > 0)
                {
                    ctx.ArrivalDate = Convert.ToDateTime(srcDate.Rows[0]["FirstArrival"]);
                    ctx.DepartureDate = Convert.ToDateTime(srcDate.Rows[0]["LastDeparture"]);
                }
            }

            // Ngày tính tỷ giá
            if (TextUtils.CompareDate(ctx.BusinessDate, ctx.DepartureDate) >= 0)
                ctx.ExchangeDate = ctx.DepartureDate;
            else
                ctx.ExchangeDate = mR.Status == 2 ? ctx.DepartureDate : ctx.BusinessDate;

            ctx.ExchangeDateRaw = ctx.ExchangeDate;
            if (TextUtils.CompareDate(ctx.BusinessDate, ctx.ExchangeDate) <= 0)
                ctx.ExchangeDate = ctx.BusinessDate;

            return ctx;
        }
        #endregion

        #region Cấu hình VAT/SVC (LoadVATInfor)
        public class VatConfig
        {
            public string VAT5 = "", VAT10 = "", SVC = "", TAX = "", INVOICE_CODE = "", ROOMCHARGE = "",
                MINIBAR = "", HotelID = "", MASSAGE = "", TELEPHONE = "", CODE_INVOICE_MINIBAR = "";
        }

        public VatConfig LoadVATInfor()
        {
            var cfg = new VatConfig();
            DataTable dt = TextUtils.Select("Select KeyName, KeyValue from ConfigSystem where KeyName ='HotelID' Or KeyName='CODE_ROOMTYPE_CHARGE' or KeyName='VAT5' or KeyName='VAT10' or KeyName='SVC' or KeyName='TAX#' or KeyName='CODE_ROOMCHARGE_VAT' or KeyName='CODE_INVOICE_VAT' or KeyName='CODE_MINIBAR_VAT' or KeyName ='CODE_BEAUTY_MASSAGE_VAT' or KeyName ='CODE_TELEPHONE_VAT' or KeyName='CODE_INVOICE' ");
            foreach (DataRow r in dt.Rows)
            {
                string key = r["KeyName"]?.ToString() ?? "";
                string val = r["KeyValue"]?.ToString() ?? "";
                switch (key)
                {
                    case "VAT5": cfg.VAT5 = val; break;
                    case "VAT10": cfg.VAT10 = val; break;
                    case "SVC": cfg.SVC = val; break;
                    case "TAX#": cfg.TAX = val; break;
                    case "CODE_INVOICE_VAT": cfg.INVOICE_CODE = val; break;
                    case "CODE_ROOMCHARGE_VAT": cfg.ROOMCHARGE = val; break;
                    case "CODE_MINIBAR_VAT": cfg.MINIBAR = val; break;
                    case "HotelID": cfg.HotelID = val; break;
                    case "CODE_BEAUTY_MASSAGE_VAT": cfg.MASSAGE = val; break;
                    case "CODE_TELEPHONE_VAT": cfg.TELEPHONE = val; break;
                    case "CODE_INVOICE": cfg.CODE_INVOICE_MINIBAR = val; break;
                }
            }
            // WinForm nối các mã invoice thành chuỗi 'a','b' để đưa vào SP
            if (cfg.INVOICE_CODE.Length > 0)
            {
                var parts = cfg.INVOICE_CODE.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim());
                cfg.INVOICE_CODE = string.Join("','", parts);
            }
            return cfg;
        }
        #endregion

        #region Lấy dữ liệu
        /// <summary>spSearchInformationInFolioHistory (GetSource bên WinForm)</summary>
        public DataTable GetSource(int folioId, DateTime fromDate, DateTime toDate)
        {
            return TextUtils.getTable("spSearchInformationInFolioHistory",
                new SqlParameter("@FolioID", " FolioID=" + folioId),
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate));
        }

        /// <summary>spSearchInformationInFolioHistoryBySelected (GetSourceBySelected): @InvoiceNo dạng a','b','c</summary>
        public DataTable GetSourceBySelected(int folioId, DateTime fromDate, DateTime toDate, string[] invoiceNos)
        {
            string list = string.Join("','", invoiceNos
                .Select(x => (x ?? "").Trim())
                .Where(x => x.Length > 0 && x.All(char.IsLetterOrDigit)));
            return TextUtils.getTable("spSearchInformationInFolioHistoryBySelected",
                new SqlParameter("@FolioID", " FolioID=" + folioId),
                new SqlParameter("@FromDate", fromDate),
                new SqlParameter("@ToDate", toDate),
                new SqlParameter("@InvoiceNo", list));
        }
        #endregion

        #region Kết quả trả về cho client
        public class PrintHeader
        {
            public string HotelName { get; set; } = "";
            public string Name { get; set; } = "";
            public string Address { get; set; } = "";
            public string MemberShip { get; set; } = "";
            public string AR { get; set; } = "";
            public string Group { get; set; } = "";
            public string Company { get; set; } = "";
            public string FolioNo { get; set; } = "";
            public string ConfirmNo { get; set; } = "";
            public string Room { get; set; } = "";
            public string Arrival { get; set; } = "";
            public string Departure { get; set; } = "";
            public string Cashier { get; set; } = "";
            public string DateTime { get; set; } = "";
            public string Date { get; set; } = "";
            public string User { get; set; } = "";
            public string Text1 { get; set; } = "";
            public string Text2 { get; set; } = "";
        }

        public class PrintRow
        {
            public string Date { get; set; } = "";
            public string Code { get; set; } = "";
            public string Text { get; set; } = "";
            public string InvoiceNo { get; set; } = "";
            public string Quantity { get; set; } = "";
            public string Price { get; set; } = "";
            /// <summary>true = khoản thanh toán (Credit, GroupType = 1)</summary>
            public bool IsCredit { get; set; }
            public string VND { get; set; } = "";
            public string USD { get; set; } = "";
            /// <summary>Giá trị số để client cộng nhóm (theo ngày / theo giao dịch)</summary>
            public decimal ValueVND { get; set; }
            public decimal ValueUSD { get; set; }
        }

        /// <summary>Một dòng của khối tổng (Text1..Text4 bên Crystal Report).</summary>
        public class SummaryLine
        {
            public string Date { get; set; } = "";
            public string Label { get; set; } = "";
            public string VND { get; set; } = "";
            public string USD { get; set; } = "";
            public bool IsSeparator { get; set; }
            public bool IsTotal { get; set; }
        }

        public class PrintResult
        {
            public string Style { get; set; } = "";
            public bool Vietnamese { get; set; }
            public PrintHeader Header { get; set; } = new();
            public List<PrintRow> Rows { get; set; } = new();
            public List<SummaryLine> Summary { get; set; } = new();
            public string Exchange { get; set; } = "";
            public string AmountVND { get; set; } = "";
            public string AmountUSD { get; set; } = "";
            public string AmountTextVND { get; set; } = "";
            public string AmountTextUSD { get; set; } = "";
        }
        #endregion

        #region Họ Billing Information: BILLING_INFOR / _QUANTITY / _BY_SELECTED / FOLIO_BY_DATE / FOLIO_SUMMARY
        /// <summary>
        /// Các kiểu này bên WinForm dùng chung GetSource(+BySelected) + PROCESS_BILLING_INFOR + ProcessSourceView(Source,1),
        /// chỉ khác report (.rpt) và vài chi tiết:
        ///  - BILLING_INFOR          : Company = Company + Agent, có tiếng Việt, tỷ giá chặn theo BusinessDate, tổng = số dư Folio.
        ///  - BILLING_INFOR_QUANTITY : như trên nhưng Company chỉ CompanyName (report có cột số lượng/đơn giá).
        ///  - BILLING_INFOR_BY_SELECTED : lấy theo các InvoiceNo được chọn, tổng KHÔNG thay bằng số dư Folio.
        ///  - FOLIO_BY_DATE / FOLIO_SUMMARY : luôn tiếng Anh, tổng = tỷ giá * USD + VND (nhóm theo ngày / theo giao dịch).
        /// </summary>
        public PrintResult BuildBillingFamily(string style, int folioId, DateTime fromDate, DateTime toDate, bool isVietnamese,
            string cashierName, string[] invoiceNos)
        {
            bool bySelected = style == STYLE_BILLING_INFOR_BY_SELECTED;
            bool englishOnly = style == STYLE_FOLIO_BY_DATE || style == STYLE_FOLIO_SUMMARY;
            if (englishOnly) isVietnamese = false;

            var ctx = LoadContext(folioId);
            DataTable source;
            if (bySelected)
            {
                if (invoiceNos == null || invoiceNos.Length == 0)
                    throw new Exception("Please select transactions on Billing screen first.");
                source = GetSourceBySelected(folioId, fromDate, toDate, invoiceNos);
            }
            else
                source = GetSource(folioId, fromDate, toDate);

            var header = BuildHeader(ctx, cashierName);
            if (style != STYLE_BILLING_INFOR)
                header.Company = ctx.Reservation.CompanyName ?? "";

            var result = new PrintResult { Style = style, Vietnamese = isVietnamese, Header = header };

            // Bảng chi tiết: ProcessSourceView(Source, 1) -> chỉ các khoản Debit (RowState=1, GroupType<>1)
            foreach (DataRow r in source.Rows)
            {
                if (Str(r, "RowState") != "1" || Str(r, "GroupType") == "1") continue;
                result.Rows.Add(ToPrintRow(r, false));
            }

            decimal[] amtVND, amtUSD;
            result.Summary = ProcessBillingInfor(source, folioId, isVietnamese, bySelected, out amtVND, out amtUSD);

            // Tỷ giá: chỉ BILLING_INFOR(_QUANTITY) chặn theo BusinessDate
            DateTime exDate = (style == STYLE_BILLING_INFOR || style == STYLE_BILLING_INFOR_QUANTITY) ? ctx.ExchangeDate : ctx.ExchangeDateRaw;
            decimal exchange = TextUtils.ExchangeCurrency(exDate, "USD", "VND", 1);
            result.Exchange = exchange.ToString(FmtVND, NumCulture);

            decimal totalVND = exchange * amtUSD[6] + amtVND[6];
            decimal totalUSD = exchange != 0 ? amtUSD[6] + amtVND[6] / exchange : amtUSD[6];
            // BILLING_INFOR(_QUANTITY): lấy luôn số dư Folio (sửa lỗi lệch 1 đồng như WinForm)
            if ((style == STYLE_BILLING_INFOR || style == STYLE_BILLING_INFOR_QUANTITY)
                && TryGetFolioBalance(folioId, out decimal balVND, out decimal balUSD))
            {
                totalVND = balVND;
                totalUSD = balUSD;
            }

            result.AmountUSD = totalUSD.ToString(FmtUSD, NumCulture);
            result.AmountVND = totalVND.ToString(FmtVND, NumCulture);
            result.AmountTextUSD = AmountToWords(totalUSD, true, isVietnamese);
            result.AmountTextVND = AmountToWords(totalVND, false, isVietnamese);
            return result;
        }

        public PrintResult BuildBillingInfor(int folioId, DateTime fromDate, DateTime toDate, bool isVietnamese, string cashierName)
            => BuildBillingFamily(STYLE_BILLING_INFOR, folioId, fromDate, toDate, isVietnamese, cashierName, null);
        #endregion

        #region Style = FOLIO_DETAIL
        /// <summary>
        /// Port FOLIO_DETAIL: ProcessSourceView(Source, 0) (cả Debit + Credit) + PROCESS_FOLIO_DETAIL
        /// (Balance, Total.Incl, Net Amount, SVC, VAT5, VAT10, TAX theo từng loại tiền).
        /// </summary>
        public PrintResult BuildFolioDetail(int folioId, DateTime fromDate, DateTime toDate, string cashierName, string userName,
            string text1, string text2)
        {
            var ctx = LoadContext(folioId);
            DataTable source = GetSource(folioId, fromDate, toDate);

            var header = BuildHeader(ctx, cashierName);
            header.Group = ctx.Reservation.GroupName ?? "";
            header.Company = ctx.Reservation.CompanyName ?? "";
            header.ConfirmNo = ctx.Reservation.ConfirmationNo ?? "";
            header.User = userName ?? "";
            header.Text1 = text1 ?? "";
            header.Text2 = text2 ?? "";

            var result = new PrintResult { Style = STYLE_FOLIO_DETAIL, Header = header };

            foreach (DataRow r in source.Rows)
            {
                if (Str(r, "RowState") != "1") continue;
                result.Rows.Add(ToPrintRow(r, Str(r, "GroupType") == "1"));
            }

            if (source.Rows.Count == 0) return result;

            var cfg = LoadVATInfor();
            decimal netV = 0, svcV = 0, vat5V = 0, vat10V = 0, taxV = 0, paidV = 0;
            decimal netU = 0, svcU = 0, vat5U = 0, vat10U = 0, taxU = 0, paidU = 0;
            string tSvc = "", tVat5 = "", tVat10 = "", tTax = "";

            foreach (DataRow r in source.Rows)
            {
                bool isVND = Str(r, "Currency") == "VND";
                string code = Str(r, "TransactionCode");
                decimal total = Dec(r, "TotalAmount");
                if (ToInt(Str(r, "GroupType")) != 1)
                {
                    // Giữ nguyên cách so khớp của WinForm: VAT5/TAX/SVC so bằng, VAT10 dùng Contains
                    if (code.Length == 0) { }
                    else if (code == cfg.VAT5) { tVat5 = Str(r, "Text"); if (isVND) vat5V += total; else vat5U += total; }
                    else if (code.Length > 0 && cfg.VAT10.Contains(code)) { tVat10 = Str(r, "Text"); if (isVND) vat10V += total; else vat10U += total; }
                    else if (code == cfg.TAX) { tTax = Str(r, "Text"); if (isVND) taxV += total; else taxU += total; }
                    else if (code == cfg.SVC) { tSvc = Str(r, "Text"); if (isVND) svcV += total; else svcU += total; }

                    if (Str(r, "RowState") == "1")
                    {
                        if (isVND) netV += Dec(r, "Amount"); else netU += Dec(r, "Amount");
                    }
                }
                else
                {
                    if (isVND) paidV += total; else paidU += total;
                }
            }

            decimal amtV = netV + vat5V + vat10V + taxV + svcV;
            decimal amtU = netU + vat5U + vat10U + taxU + svcU;
            decimal balV = amtV + paidV;
            decimal balU = amtU + paidU;

            void Add(string label, decimal v, decimal u, bool total = false) => result.Summary.Add(new SummaryLine
            {
                Label = label,
                VND = v.ToString(FmtVND, NumCulture),
                USD = u.ToString(FmtUSD, NumCulture),
                IsTotal = total
            });

            Add("Balance", balV, balU, true);
            Add("Total.Incl", amtV, amtU);
            Add("Net Amount", netV, netU);
            if (tSvc.Length > 0) Add(tSvc, svcV, svcU);
            if (tVat5.Length > 0) Add(tVat5, vat5V, vat5U);
            if (tVat10.Length > 0) Add(tVat10, vat10V, vat10U);
            if (tTax.Length > 0) Add(tTax, taxV, taxU);

            return result;
        }
        #endregion

        #region Style = FOLIO_VAT_* (ShowReportVATWithConfirmNo / ShowReportVAT)

        /// <summary>Thông tin nhập ở hộp thoại VAT (frmCshVATInfor bên WinForm, Params[1..12]).</summary>
        public class VatInfo
        {
            public string Arrival { get; set; } = "";      // Params[1]
            public string Departure { get; set; } = "";    // Params[2]
            public string Adult { get; set; } = "";        // Params[3]
            public string FolioNo { get; set; } = "";      // Params[5]
            public string GuestName { get; set; } = "";    // Params[6]
            public string TaxNumber { get; set; } = "";    // Params[7]
            public string Address { get; set; } = "";      // Params[8]
            public string Company { get; set; } = "";      // Params[9]
            public string InvoiceNo { get; set; } = "";    // Params[10]
            public string SerialNo { get; set; } = "";     // Params[11]
            public string Description { get; set; } = ""; // Params[12]
        }

        public class VatRow
        {
            public DateTime MinDate { get; set; }
            public DateTime MaxDate { get; set; }
            public string Date { get; set; } = "";
            public string TypeCode { get; set; } = "";
            public string TypeName { get; set; } = "";
            public string Description { get; set; } = "";
            public int TypeVAT { get; set; } = -1;
            public string UnitQty { get; set; } = "";
            public decimal Quantity { get; set; }
            public decimal Price { get; set; }
            public decimal Amount { get; set; }
            public decimal Svc { get; set; }
            public decimal Vat5 { get; set; }
            public decimal Vat10 { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal MinibarType { get; set; }
            public bool HasSVC { get; set; }
            public bool IsTotalRow { get; set; }
        }

        public class VatPrintResult
        {
            public string Style { get; set; } = "";
            /// <summary>GUEST (PROCESS_FOLIO_VAT_FOR_FIVE_STAR) | LOCAL (PROCESS_FOLIO_VAT_NEW)</summary>
            public string Mode { get; set; } = "";
            public string Currency { get; set; } = "VND";
            public string HotelName { get; set; } = "";
            public string Room { get; set; } = "";
            public string Arrival { get; set; } = "";
            public string Departure { get; set; } = "";
            public string Adult { get; set; } = "";
            public string FolioNo { get; set; } = "";
            public string GuestName { get; set; } = "";
            public string Company { get; set; } = "";
            public string TaxNumber { get; set; } = "";
            public string Address { get; set; } = "";
            public string InvoiceNo { get; set; } = "";
            public string SerialNo { get; set; } = "";
            public string Description { get; set; } = "";
            public string ConfirmNo { get; set; } = "";
            public string CRSNo { get; set; } = "";
            public string Cashier { get; set; } = "";
            public string Date { get; set; } = "";
            public List<VatRow> Rows { get; set; } = new();
            /// <summary>[0]Net [1]SVC [2]VAT5 [3]VAT10 [4]Gross</summary>
            public decimal[] LineVAT5 { get; set; } = new decimal[5];
            public decimal[] LineVAT10 { get; set; } = new decimal[5];
            public decimal[] Line { get; set; } = new decimal[5];
            public string AmountVND { get; set; } = "";
            public string AmountVNDToWord { get; set; } = "";
            public string Exchange { get; set; } = "";
            public string TotalGrossToWord { get; set; } = "";
            /// <summary>CheckValidPrintVAT(false): folio đã in hóa đơn VAT (PrintVAT.Status=1) thì chỉ cho xem, không cho in.</summary>
            public bool AllowPrinting { get; set; } = true;
            public string PrintedMessage { get; set; } = "";
        }

        /// <summary>Giá trị mặc định cho hộp thoại thông tin VAT.</summary>
        public VatInfo GetVatInfoDefaults(int folioId)
        {
            var ctx = LoadContext(folioId);
            var mF = ctx.Folio;
            var mR = ctx.Reservation;
            var info = new VatInfo
            {
                Arrival = ctx.ArrivalDate.ToString("yyyy-MM-dd"),
                Departure = ctx.DepartureDate.ToString("yyyy-MM-dd"),
                Adult = mR.NoOfAdult.ToString(),
                FolioNo = mF.ID.ToString(),
                GuestName = mF.AccountName ?? "",
                Company = mR.CompanyName ?? "",
                Address = mR.Address ?? ""
            };
            // Mã số thuế / địa chỉ: ưu tiên profile công ty, không có thì lấy profile của folio
            int profileId = mR.ProfileCompanyId > 0 ? mR.ProfileCompanyId : mF.ProfileID;
            if (profileId > 0)
            {
                try
                {
                    DataTable dt = TextUtils.Select("select TaxCode, Address from Profile with (nolock) where ID = " + profileId);
                    if (dt.Rows.Count > 0)
                    {
                        info.TaxNumber = dt.Rows[0]["TaxCode"]?.ToString() ?? "";
                        string addr = dt.Rows[0]["Address"]?.ToString() ?? "";
                        if (addr.Length > 0) info.Address = addr;
                    }
                }
                catch { }
            }
            return info;
        }

        /// <summary>CheckValidPrintVAT: trả về false + thông báo nếu folio đã in hóa đơn VAT.</summary>
        public bool CheckValidPrintVAT(int folioId, out string message)
        {
            message = "";
            try
            {
                DataTable tbl = TextUtils.Select("select ID, FullName, [DateTime] from PrintVAT a with (nolock) where a.FolioNo = " + folioId + " and a.Status = 1");
                if (tbl.Rows.Count > 0)
                {
                    string printDate = tbl.Rows[0]["DateTime"] == DBNull.Value ? "" : Convert.ToDateTime(tbl.Rows[0]["DateTime"]).ToString(DateFormat);
                    message = string.Format("Folio were printed invoice VAT: Invoice No: {0}, Print date: {1}, Employee: {2}. You must cancel printed bill before print new bill.",
                        tbl.Rows[0]["ID"], printDate, tbl.Rows[0]["FullName"]);
                    return false;
                }
            }
            catch { }
            return true;
        }

        /// <summary>Print History (frmPrintVATSearch): các lần in hóa đơn VAT của folio.</summary>
        public DataTable GetPrintVatHistory(int folioId)
        {
            return TextUtils.Select("select * from PrintVAT with (nolock) where FolioNo = " + folioId + " order by ID desc");
        }

        /// <summary>
        /// Port btnPreview_Click nhánh FOLIO_VAT (form web chỉ có VND như WinForm):
        ///  - Print for Guest -> ShowReportVATWithConfirmNo(0,0,vatType,"PrintFolio_GuestVAT[_Detail]","VND") -> PROCESS_FOLIO_VAT_FOR_FIVE_STAR
        ///  - Print for Local -> ShowReportVAT(0,1,vatType,"PrintFolio_GuestVAT_Local","","VND") -> PROCESS_FOLIO_VAT_NEW
        /// vatType: 0 = Guest name, 1 = No name, 2 = Master.
        /// </summary>
        public VatPrintResult BuildFolioVat(string style, int folioId, bool printForLocal, VatInfo info, string cashierName)
        {
            const string currency = "VND";
            var ctx = LoadContext(folioId);
            var mR = ctx.Reservation;
            var cfg = LoadVATInfor();
            info ??= new VatInfo();

            int vatType = style == STYLE_FOLIO_VAT_GUEST_NONAME ? 1 : (style == STYLE_FOLIO_VAT_MASTER ? 2 : 0);

            decimal[] lineVat5, lineVat10, line;
            List<VatRow> rows;
            if (!printForLocal)
            {
                // GetSourceByVATForGuest chặn ngày tỷ giá theo BusinessDate
                rows = ProcessFolioVatForFiveStar(folioId, cfg, currency, true, ctx.ExchangeDate, out lineVat5, out lineVat10, out line);
                if (rows.Count == 0)
                    throw new Exception("No transaction to print VAT invoice.");
            }
            else
            {
                rows = ProcessFolioVatNew(folioId, cfg, currency, ctx.ExchangeDateRaw, out lineVat5, out lineVat10, out line);
                if (rows.Count == 0 || rows[0].TotalAmount == 0)
                    throw new Exception("No transaction to print VAT invoice.");
            }

            var result = new VatPrintResult
            {
                Style = style,
                Mode = printForLocal ? "LOCAL" : "GUEST",
                Currency = currency,
                HotelName = GetHotelName(),
                Room = GetVatRoomNo(mR, info.FolioNo, cfg),
                Arrival = FormatInputDate(info.Arrival),
                Departure = FormatInputDate(info.Departure),
                Adult = info.Adult ?? "",
                FolioNo = info.FolioNo ?? "",
                GuestName = vatType == 0 ? (info.GuestName ?? "") : "",
                Company = info.Company ?? "",
                TaxNumber = info.TaxNumber ?? "",
                Address = info.Address ?? "",
                InvoiceNo = info.InvoiceNo ?? "",
                SerialNo = info.SerialNo ?? "",
                Description = info.Description ?? "",
                ConfirmNo = mR.ConfirmationNo ?? "",
                CRSNo = (mR.CRSNo ?? "").Trim().Length > 0 ? mR.CRSNo.Trim() : (mR.GroupCode ?? "").Trim(),
                Cashier = cashierName ?? "",
                Date = ctx.BusinessDate.ToString(DateFormat),
                LineVAT5 = lineVat5,
                LineVAT10 = lineVat10,
                Line = line
            };

            // Guest VAT Summary: in một dòng tổng, nội dung lấy từ Description nhập tay
            if (!printForLocal && style == STYLE_FOLIO_VAT_GUEST_SUMMARY)
            {
                decimal sum = rows.Sum(r => r.TotalAmount);
                var last = rows[rows.Count - 1];
                rows = new List<VatRow>
                {
                    new VatRow
                    {
                        Date = last.Date, MinDate = last.MinDate, MaxDate = last.MaxDate,
                        Description = info.Description ?? "", TypeCode = last.TypeCode, TypeName = last.TypeName,
                        IsTotalRow = true, Amount = sum, TotalAmount = sum
                    }
                };
            }
            result.Rows = rows;

            // VND: tỷ giá = 1, số tiền bằng chữ đọc tiếng Việt (TextUtils.NumericToString)
            decimal amountVND = Math.Round(line[4], 0, MidpointRounding.AwayFromZero);
            result.AmountVND = amountVND.ToString("#,##0", NumCulture);
            result.Exchange = "1";
            result.AmountVNDToWord = amountVND == 0 ? "" : NumberWords.Vietnamese(Math.Abs(amountVND));
            result.TotalGrossToWord = result.AmountVNDToWord;

            result.AllowPrinting = CheckValidPrintVAT(folioId, out string msg);
            result.PrintedMessage = msg;
            return result;
        }

        /// <summary>Hotel 11, 12: in danh sách phòng của cả đoàn (giống WinForm), còn lại in phòng của reservation.</summary>
        private static string GetVatRoomNo(ReservationModel mR, string folioNo, VatConfig cfg)
        {
            if ((cfg.HotelID == "11" || cfg.HotelID == "12") && int.TryParse(folioNo, out int fid) && fid > 0)
            {
                try
                {
                    DataTable dt = TextUtils.Select("select r.ConfirmationNo from Reservation r join Folio f on r.ID = f.ReservationID where f.ID = " + fid);
                    string conf = dt.Rows.Count > 0 ? dt.Rows[0][0]?.ToString() ?? "" : "";
                    if (conf.Length > 0 && conf.All(char.IsLetterOrDigit))
                    {
                        DataTable rooms = TextUtils.Select("Select RoomNo from Reservation where MainGuest = 1 and ConfirmationNo = '" + conf + "'");
                        return string.Join(", ", rooms.Rows.Cast<DataRow>().Select(r => r["RoomNo"]?.ToString() ?? ""));
                    }
                }
                catch { }
            }
            return mR.RoomNo ?? "";
        }

        private static string FormatInputDate(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            string[] formats = { "yyyy-MM-dd", "dd/MM/yyyy" };
            return DateTime.TryParseExact(s.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d)
                ? d.ToString(DateFormat) : s;
        }

        /// <summary>Một dòng nguồn của spSearchTransactionByVAT / spSearchTransactionByVATForGuest.</summary>
        private class VatSrc
        {
            public DateTime Date;
            public string Code = "", Text = "", CurrencyID = "", TransactionNo = "", TypeCode = "", TypeName = "";
            public decimal Amount, Price;
            public int Quantity;
        }

        /// <summary>TransactionTypeInfor bên WinForm (Clear() không reset Date / MinibarType / HasSVC).</summary>
        private class VatInfor
        {
            public DateTime Date = DateTime.MinValue;
            public string TypeCode = "", TypeName = "", UnitQty = "";
            public decimal Amount, Svc, Vat, MinibarType, Price;
            public int TypeVat = -1, Quantity;
            public bool HasSVC;
            public void Clear()
            {
                TypeCode = ""; TypeName = ""; Amount = 0; Svc = 0; Vat = 0; TypeVat = -1; Quantity = 0; UnitQty = ""; Price = 0;
            }
        }

        private static decimal RoundAmt(decimal v, int digits)
        {
            try { return Math.Round(v, digits, MidpointRounding.AwayFromZero); } catch { return 0; }
        }

        /// <summary>GetSourceByVATForGuest: spSearchTransactionByVATForGuest + quy đổi về 1 loại tiền + tách Beauty/Massage.</summary>
        private List<VatSrc> GetSourceByVATForGuest(int folioId, VatConfig cfg, string currency, bool getTotalRows, DateTime exchangeDate)
        {
            DataTable dt = TextUtils.getTable("spSearchTransactionByVATForGuest",
                new SqlParameter("@FolioID", folioId.ToString()),
                new SqlParameter("@InvoiceCode", cfg.INVOICE_CODE),
                new SqlParameter("@GetTotalRows", getTotalRows));
            decimal rate = TextUtils.ExchangeCurrency(exchangeDate, "USD", "VND", 1);
            var list = new List<VatSrc>();
            foreach (DataRow r in dt.Rows)
            {
                var s = ReadVatSrc(r);
                // Dùng 2 số lẻ để tránh làm tròn gây lẻ 1 đồng (như WinForm)
                if (!currency.Equals(s.CurrencyID))
                    s.Amount = s.CurrencyID == "USD" ? RoundAmt(rate * s.Amount, 2) : (rate != 0 ? RoundAmt(s.Amount / rate, 2) : 0);
                else
                    s.Amount = RoundAmt(s.Amount, 2);
                list.Add(s);
            }

            // Tách Beauty, Massage thành các dòng chi tiết (FolioDetail PostType=3, RowState=2)
            var extra = new List<VatSrc>();
            if (cfg.MASSAGE.Length > 0)
            {
                foreach (var s in list)
                {
                    if (!s.Code.Contains(cfg.MASSAGE) || !s.TransactionNo.All(char.IsLetterOrDigit) || s.TransactionNo.Length == 0) continue;
                    DataTable det = TextUtils.Select("Select * from FolioDetail with (nolock) where PostType = 3 And RowState = 2 And InvoiceNo = '" + s.TransactionNo + "'");
                    foreach (DataRow d in det.Rows)
                    {
                        string tCode = Str(d, "TransactionCode");
                        GetTypeCode(tCode, out string typeCode, out string typeName);
                        extra.Add(new VatSrc
                        {
                            Date = s.Date,
                            Code = tCode,
                            Text = Str(d, "Description"),
                            Amount = Dec(d, "Amount"),
                            CurrencyID = s.CurrencyID,
                            TransactionNo = Str(d, "InvoiceNo"),
                            TypeCode = typeCode,
                            TypeName = typeName,
                            Quantity = ToInt(Str(d, "Quantity")),
                            Price = Dec(d, "Price")
                        });
                    }
                }
            }
            list.AddRange(extra);
            return list;
        }

        /// <summary>GetSourceByVAT: spSearchTransactionByVAT + quy đổi về loại tiền in.</summary>
        private List<VatSrc> GetSourceByVAT(int folioId, VatConfig cfg, string currency, DateTime exchangeDate)
        {
            DataTable dt = TextUtils.getTable("spSearchTransactionByVAT",
                new SqlParameter("@FolioID", folioId.ToString()),
                new SqlParameter("@Currency", currency),
                new SqlParameter("@InvoiceCode", cfg.INVOICE_CODE));
            decimal rate = TextUtils.ExchangeCurrency(exchangeDate, "USD", "VND", 1);
            var list = new List<VatSrc>();
            foreach (DataRow r in dt.Rows)
            {
                var s = ReadVatSrc(r);
                if (!currency.Equals(s.CurrencyID))
                    s.Amount = s.CurrencyID == "USD" ? RoundAmt(rate * s.Amount, 0) : (rate != 0 ? RoundAmt(s.Amount / rate, 2) : 0);
                list.Add(s);
            }
            return list;
        }

        private static VatSrc ReadVatSrc(DataRow r)
        {
            DateTime date = DateTime.MinValue;
            try { if (r.Table.Columns.Contains("Date") && r["Date"] != DBNull.Value) date = Convert.ToDateTime(r["Date"]); } catch { }
            return new VatSrc
            {
                Date = date,
                Code = Str(r, "Code"),
                Text = Str(r, "Text"),
                Amount = Dec(r, "Amount"),
                CurrencyID = Str(r, "CurrencyID"),
                TransactionNo = Str(r, "TransactionNo"),
                TypeCode = Str(r, "TypeCode"),
                TypeName = Str(r, "TypeName"),
                Quantity = (int)Dec(r, "Quantity"),
                Price = Dec(r, "Price")
            };
        }

        private static void GetTypeCode(string transactionCode, out string typeCode, out string typeName)
        {
            typeCode = ""; typeName = "";
            DataTable da = TextUtils.Select("Select a.Code, a.Name From VATType a, Transactions b Where b.VATTypeID = a.ID And b.Code = '" + SqlSafe(transactionCode) + "'");
            if (da.Rows.Count > 0)
            {
                typeCode = da.Rows[0]["Code"]?.ToString() ?? "";
                typeName = da.Rows[0]["Name"]?.ToString() ?? "";
            }
        }

        /// <summary>Phân loại khoản theo cấu hình (WinForm dùng string.Contains trên chuỗi cấu hình).</summary>
        private static void ClassifyVat(VatInfor inf, VatSrc s, VatConfig cfg)
        {
            if (cfg.SVC.Contains(s.Code)) inf.Svc += s.Amount;
            else if (cfg.VAT5.Contains(s.Code)) { inf.Vat += s.Amount; inf.TypeVat = 0; }
            else if (cfg.VAT10.Contains(s.Code)) { inf.Vat += s.Amount; inf.TypeVat = 1; }
            else inf.Amount += s.Amount;
        }

        private static bool IsMinibarRow(VatSrc s, VatConfig cfg)
        {
            // Code Invoice dùng chung Minibar và Laundry: chỉ tính Minibar khi Text bắt đầu bằng "MINIBAR"
            if (s.Code == cfg.CODE_INVOICE_MINIBAR && s.Text.Length >= 7 && s.Text.Substring(0, 7).ToUpper() != "MINIBAR")
                return false;
            return true;
        }

        /// <summary>Port PROCESS_FOLIO_VAT_FOR_FIVE_STAR (in hóa đơn VAT cho khách).</summary>
        private List<VatRow> ProcessFolioVatForFiveStar(int folioId, VatConfig cfg, string currency, bool getTotalRows, DateTime exchangeDate,
            out decimal[] lineVat5, out decimal[] lineVat10, out decimal[] line)
        {
            var source = GetSourceByVATForGuest(folioId, cfg, currency, getTotalRows, exchangeDate);
            var temp = new List<VatRow>();

            int roomNight = 0;
            if (cfg.ROOMCHARGE.Trim().Length > 0)
            {
                try
                {
                    DataTable dtr = TextUtils.Select("Select count(*) RoomNight From FolioDetail with (nolock) where FolioID = " + folioId +
                        " and TransactionCode in (" + cfg.ROOMCHARGE + ") and RoomID <> 0 And RowState = 1");
                    if (dtr.Rows.Count > 0) roomNight = ToInt(dtr.Rows[0][0]?.ToString());
                }
                catch { }
            }

            string transNo = "";
            var inf = new VatInfor();
            bool any = false;

            VatRow FindGroup() => temp.FirstOrDefault(r =>
                (r.TypeCode == inf.TypeCode && r.TypeVAT == inf.TypeVat && r.MinibarType == inf.MinibarType)
                || (r.MinibarType == inf.MinibarType && r.MinibarType == 1));

            for (int i = 0; i < source.Count; i++)
            {
                var s = source[i];
                if (cfg.MASSAGE.Contains(s.Code)) continue;
                any = true;

                if (transNo != s.TransactionNo && i == 0)
                {
                    // Dòng đầu tiên
                    inf.Date = s.Date; inf.TypeCode = s.TypeCode; inf.TypeName = s.TypeName;
                    inf.Quantity = s.Quantity; inf.Price = s.Price;
                    if (getTotalRows) inf.HasSVC = true;
                    if (cfg.ROOMCHARGE.Contains(s.Code)) { inf.Quantity = roomNight; inf.UnitQty = "Đ.phòng"; }
                    if (cfg.MINIBAR.Contains(s.Code))
                    {
                        if (IsMinibarRow(s, cfg)) { inf.MinibarType = 1; inf.TypeName = "Ăn và uống (Minibar)"; inf.HasSVC = false; }
                    }
                    else if (cfg.TELEPHONE.Contains(s.Code)) inf.HasSVC = false;
                    ClassifyVat(inf, s, cfg);
                    transNo = s.TransactionNo;
                }
                else if (transNo == s.TransactionNo)
                {
                    ClassifyVat(inf, s, cfg);
                }
                else
                {
                    // Khác giao dịch -> ghi nhóm cũ
                    var g = FindGroup();
                    if (g == null)
                    {
                        if (inf.Date != DateTime.MinValue)
                            temp.Add(NewFiveStarRow(inf, getTotalRows, true));
                    }
                    else
                    {
                        if (inf.Date.Date < g.MinDate.Date) g.MinDate = inf.Date;
                        if (inf.Date.Date > g.MaxDate.Date) g.MaxDate = inf.Date;
                        if (inf.TypeVat == -1)
                        {
                            g.Amount += inf.Amount;
                            g.Price += inf.Price;
                            g.Svc = inf.Svc;
                            g.Vat5 = 0; g.Vat10 = 0;
                            if (g.Quantity == 0) { g.Quantity = inf.Quantity; g.UnitQty = inf.UnitQty; }
                            g.TotalAmount += inf.Amount + inf.Svc;
                        }
                        else AddVatToGroup(g, inf);
                    }

                    // Gán thông tin mới
                    inf.Clear();
                    if (cfg.ROOMCHARGE.Contains(s.Code)) { inf.Quantity = roomNight; inf.UnitQty = "Đ.phòng"; }
                    if (getTotalRows) inf.HasSVC = true;
                    inf.Date = s.Date; inf.TypeCode = s.TypeCode; inf.TypeName = s.TypeName;
                    ClassifyVat(inf, s, cfg);
                    inf.Quantity = s.Quantity;
                    inf.Price = s.Price;
                    if (cfg.MINIBAR.Contains(s.Code))
                    {
                        if (IsMinibarRow(s, cfg)) { inf.MinibarType = 1; inf.TypeName = "Ăn và uống (Minibar)"; inf.HasSVC = false; }
                    }
                    else if (cfg.TELEPHONE.Contains(s.Code)) inf.HasSVC = false;
                    else inf.MinibarType = 0;
                    transNo = s.TransactionNo;
                }
            }

            // Dòng cuối
            if (any)
            {
                var g = FindGroup();
                if (g == null)
                    temp.Add(NewFiveStarRow(inf, getTotalRows, false));
                else
                {
                    if (inf.Date.Date < g.MinDate.Date) g.MinDate = inf.Date;
                    if (inf.Date.Date > g.MaxDate.Date) g.MaxDate = inf.Date;
                    if (inf.TypeVat == -1)
                    {
                        if (g.Quantity <= 0) { g.Quantity = inf.Quantity; g.UnitQty = inf.UnitQty; }
                        g.Amount += inf.Amount;
                        g.Svc = inf.Svc;
                        g.Vat5 = 0; g.Vat10 = 0;
                        g.TotalAmount += inf.Amount + inf.Svc;
                    }
                    else AddVatToGroup(g, inf);
                }
            }

            FinishVatRows(temp, out lineVat5, out lineVat10, out line);
            return temp;
        }

        private static VatRow NewFiveStarRow(VatInfor inf, bool getTotalRows, bool withPrice)
        {
            var r = new VatRow
            {
                MinDate = inf.Date, MaxDate = inf.Date,
                TypeCode = inf.TypeCode, TypeName = inf.TypeName, TypeVAT = inf.TypeVat,
                MinibarType = inf.MinibarType, HasSVC = inf.HasSVC, IsTotalRow = getTotalRows,
                Amount = inf.Amount, Quantity = inf.Quantity, UnitQty = inf.UnitQty, Svc = inf.Svc,
                Price = withPrice ? inf.Price : 0
            };
            if (inf.TypeVat == -1) r.TotalAmount = inf.Amount + inf.Svc;
            else
            {
                if (inf.TypeVat == 0) r.Vat5 = inf.Vat; else r.Vat10 = inf.Vat;
                r.TotalAmount = inf.Amount + inf.Svc + inf.Vat;
            }
            return r;
        }

        private static void AddVatToGroup(VatRow g, VatInfor inf)
        {
            g.Amount += inf.Amount;
            g.Svc += inf.Svc;
            if (inf.TypeVat == 0) g.Vat5 += inf.Vat; else g.Vat10 += inf.Vat;
            g.TotalAmount += inf.Amount + inf.Svc + inf.Vat;
        }

        /// <summary>Port PROCESS_FOLIO_VAT_NEW (in hóa đơn VAT dạng Local).</summary>
        private List<VatRow> ProcessFolioVatNew(int folioId, VatConfig cfg, string currency, DateTime exchangeDate,
            out decimal[] lineVat5, out decimal[] lineVat10, out decimal[] line)
        {
            var source = GetSourceByVAT(folioId, cfg, currency, exchangeDate);
            var temp = new List<VatRow>();
            string transNo = "";
            var inf = new VatInfor();

            void Flush()
            {
                var g = temp.FirstOrDefault(r => r.TypeCode == inf.TypeCode && r.TypeVAT == inf.TypeVat);
                if (g == null)
                {
                    var r = new VatRow
                    {
                        MinDate = inf.Date, MaxDate = inf.Date,
                        TypeCode = inf.TypeCode, TypeName = inf.TypeName, TypeVAT = inf.TypeVat, Amount = inf.Amount
                    };
                    if (inf.TypeVat == -1) r.TotalAmount = inf.Amount;
                    else
                    {
                        r.Svc = inf.Svc;
                        if (inf.TypeVat == 0) r.Vat5 = inf.Vat; else r.Vat10 = inf.Vat;
                        r.TotalAmount = inf.Amount + inf.Svc + inf.Vat;
                    }
                    temp.Add(r);
                }
                else
                {
                    if (inf.Date.Date < g.MinDate.Date) g.MinDate = inf.Date;
                    if (inf.Date.Date > g.MaxDate.Date) g.MaxDate = inf.Date;
                    if (inf.TypeVat == -1)
                    {
                        g.Amount += inf.Amount;
                        g.Svc = 0; g.Vat5 = 0; g.Vat10 = 0;
                        g.TotalAmount += inf.Amount;
                    }
                    else AddVatToGroup(g, inf);
                }
            }

            for (int i = 0; i < source.Count; i++)
            {
                var s = source[i];
                if (transNo != s.TransactionNo && i == 0)
                {
                    inf.Date = s.Date; inf.TypeCode = s.TypeCode; inf.TypeName = s.TypeName;
                    ClassifyVat(inf, s, cfg);
                    // Giữ nguyên hành vi WinForm
                    inf.Svc = (s.Code == "5" || s.Code == "9") ? 1 : 0;
                    transNo = s.TransactionNo;
                }
                else if (transNo == s.TransactionNo)
                {
                    ClassifyVat(inf, s, cfg);
                }
                else
                {
                    Flush();
                    inf.Clear();
                    inf.Date = s.Date; inf.TypeCode = s.TypeCode; inf.TypeName = s.TypeName;
                    ClassifyVat(inf, s, cfg);
                    transNo = s.TransactionNo;
                }
            }
            if (source.Count > 0) Flush();

            FinishVatRows(temp, out lineVat5, out lineVat10, out line);
            return temp;
        }

        /// <summary>Phần "Xử lí Param": Description, chuỗi ngày dd/MM(-dd/MM), cộng các dòng VAT5 / VAT10 / tổng.</summary>
        private static void FinishVatRows(List<VatRow> temp, out decimal[] v5, out decimal[] v10, out decimal[] line)
        {
            v5 = new decimal[5]; v10 = new decimal[5]; line = new decimal[5];
            foreach (var r in temp)
            {
                r.Description = r.TypeName;
                r.Date = r.MinDate.Date == r.MaxDate.Date
                    ? r.MinDate.ToString("dd/MM")
                    : r.MinDate.ToString("dd/MM") + "-" + r.MaxDate.ToString("dd/MM");
                if (r.TypeVAT == 0)
                {
                    v5[0] += r.Amount; v5[1] += r.Svc; v5[2] += r.Vat5; v5[4] += r.TotalAmount;
                }
                else if (r.TypeVAT == 1)
                {
                    v10[0] += r.Amount; v10[1] += r.Svc; v10[3] += r.Vat10; v10[4] += r.TotalAmount;
                }
                else
                {
                    line[0] += r.Amount; line[4] += r.TotalAmount;
                }
            }
            for (int k = 0; k < 5; k++) line[k] += v5[k] + v10[k];
        }
        #endregion

        private PrintRow ToPrintRow(DataRow r, bool isCredit)
        {
            string cur = Str(r, "Currency");
            decimal total = Dec(r, "TotalAmount");
            return new PrintRow
            {
                Date = FormatDate(r, "Date"),
                Code = Str(r, "TransactionCode"),
                Text = Str(r, "Text"),
                InvoiceNo = Str(r, "InvoiceNo"),
                Quantity = Str(r, "Quantity"),
                Price = r.Table.Columns.Contains("Price") && r["Price"] != DBNull.Value
                    ? (cur == "USD" ? FormatUSD(Dec(r, "Price")) : FormatVND(Dec(r, "Price"))) : "",
                IsCredit = isCredit,
                VND = cur == "VND" ? FormatVND(total) : "",
                USD = cur == "USD" ? FormatUSD(total) : "",
                ValueVND = cur == "VND" ? total : 0,
                ValueUSD = cur == "USD" ? total : 0
            };
        }

        #region PROCESS_BILLING_INFOR

        /// <summary>
        /// Port PROCESS_BILLING_INFOR: tính Net / SVC / VAT5 / VAT10 / TAX / các khoản đã thanh toán / Balance.
        /// bySelected = true -> không thay Balance bằng số dư Folio (giống WinForm).
        /// </summary>
        public List<SummaryLine> ProcessBillingInfor(DataTable source, int folioId, bool isVietnamese, bool bySelected,
            out decimal[] amountVND, out decimal[] amountUSD)
        {
            var lines = new List<SummaryLine>();
            decimal[] vnd = new decimal[7];
            decimal[] usd = new decimal[7];
            amountVND = vnd;
            amountUSD = usd;
            if (source.Rows.Count == 0) return lines;

            var cfg = LoadVATInfor();
            var payments = new List<SummaryLine>();

            foreach (DataRow r in source.Rows)
            {
                string cur = Str(r, "Currency");
                string code = Str(r, "TransactionCode");
                if (ToInt(Str(r, "GroupType")) != 1)
                {
                    if (ToBool(r, "IsSplit")) continue;
                    int idx;
                    // WinForm dùng string.Contains trên chuỗi cấu hình (giữ nguyên hành vi)
                    if (code.Length > 0 && cfg.SVC.Contains(code)) idx = 1;
                    else if (code.Length > 0 && cfg.VAT5.Contains(code)) idx = 2;
                    else if (code.Length > 0 && cfg.VAT10.Contains(code)) idx = 3;
                    else if (code.Length > 0 && cfg.TAX.Contains(code)) idx = 4;
                    else idx = 0;

                    // Net amount cộng theo Amount, các khoản thuế/phí cộng theo TotalAmount
                    decimal value = idx == 0 ? Dec(r, "Amount") : Dec(r, "TotalAmount");
                    if (cur == "VND") vnd[idx] += value;
                    if (cur == "USD") usd[idx] += value;
                }
                else
                {
                    decimal total = Dec(r, "TotalAmount");
                    if (cur == "VND")
                    {
                        payments.Add(new SummaryLine { Date = FormatDate(r, "Date"), Label = Str(r, "Text"), VND = total.ToString(FmtVND, NumCulture) });
                        vnd[5] += total;
                    }
                    if (cur == "USD")
                    {
                        payments.Add(new SummaryLine { Date = FormatDate(r, "Date"), Label = Str(r, "Text"), USD = total.ToString(FmtUSD, NumCulture) });
                        usd[5] += total;
                    }
                }
            }

            string[] labelsEN = { "Net Amount", "Service Charge", "VAT 5%", "VAT 10%", "VAT 8%" };
            string[] labelsVN = { "Cộng chưa thuế/Net Amount", "Cộng thuế dịch vụ/ Service Charge", "Cộng thuế VAT 5%", "Cộng thuế VAT 10%", "Cộng thuế VAT khác" };
            for (int i = 0; i < 5; i++)
            {
                if (vnd[i] == 0 && usd[i] == 0) continue;
                lines.Add(new SummaryLine
                {
                    Label = isVietnamese ? labelsVN[i] : labelsEN[i],
                    VND = vnd[i] != 0 ? vnd[i].ToString(FmtVND, NumCulture) : "",
                    USD = usd[i] != 0 ? usd[i].ToString(FmtUSD, NumCulture) : ""
                });
            }

            // Dòng trống rồi tới các khoản đã thanh toán
            lines.Add(new SummaryLine());
            lines.AddRange(payments);
            lines.Add(new SummaryLine { IsSeparator = true });

            vnd[6] = vnd[0] + vnd[1] + vnd[2] + vnd[3] + vnd[4] + vnd[5];
            usd[6] = usd[0] + usd[1] + usd[2] + usd[3] + usd[4] + usd[5];
            if (!bySelected && TryGetFolioBalance(folioId, out decimal balVND, out decimal balUSD))
            {
                vnd[6] = balVND;
                usd[6] = balUSD;
            }

            lines.Add(new SummaryLine
            {
                Label = isVietnamese ? "Tổng thanh toán/ Balance" : "Balance",
                VND = vnd[6] != 0 ? vnd[6].ToString(FmtVND, NumCulture) : "",
                USD = usd[6] != 0 ? usd[6].ToString(FmtUSD, NumCulture) : "",
                IsTotal = true
            });
            return lines;
        }
        #endregion

        #region Helpers
        private PrintHeader BuildHeader(FolioContext ctx, string cashierName)
        {
            var mF = ctx.Folio;
            var mR = ctx.Reservation;
            return new PrintHeader
            {
                HotelName = GetHotelName(),
                Name = mF.AccountName ?? "",
                Address = mR.Address ?? "",
                MemberShip = mR.MemberNo ?? "",
                AR = mF.ARNo ?? "",
                Group = mR.GroupCode ?? "",
                Company = (mR.CompanyName ?? "") + (mR.AgentName ?? ""),
                FolioNo = mF.ID.ToString(),
                ConfirmNo = mF.ConfirmationNo ?? "",
                Room = ctx.IsMasterReservation ? "" : (mR.RoomNo ?? ""),
                Arrival = ctx.ArrivalDate.ToString(DateFormat),
                Departure = ctx.DepartureDate.ToString(DateFormat),
                Cashier = cashierName ?? "",
                DateTime = ctx.BusinessDate.ToString("dd/MM/yyyy") + " " + DateTime.Now.ToString("H:mm tt", NumCulture),
                Date = ctx.BusinessDate.ToString(DateFormat)
            };
        }

        public static string GetHotelName()
        {
            try
            {
                DataTable dt = TextUtils.Select("SELECT TOP 1 PropertyName FROM Property");
                if (dt.Rows.Count > 0)
                {
                    string name = dt.Rows[0][0]?.ToString() ?? "";
                    return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());
                }
            }
            catch { }
            return "";
        }

        private static bool TryGetFolioBalance(int folioId, out decimal vnd, out decimal usd)
        {
            vnd = 0; usd = 0;
            DataTable dt = TextUtils.Select("select BalanceVND, BalanceUSD from Folio where ID = " + folioId);
            if (dt.Rows.Count == 0) return false;
            vnd = dt.Rows[0][0] == DBNull.Value ? 0 : Convert.ToDecimal(dt.Rows[0][0]);
            usd = dt.Rows[0][1] == DBNull.Value ? 0 : Convert.ToDecimal(dt.Rows[0][1]);
            return true;
        }

        /// <summary>Số tiền bằng chữ, giữ nguyên quy tắc của WinForm (Minus/Âm, US dollars/Dong, tiếng Việt).</summary>
        public static string AmountToWords(decimal amount, bool isUSD, bool isVietnamese)
        {
            if (amount == 0) return "";
            decimal abs = Math.Abs(amount);
            // Làm tròn theo định dạng hiển thị trước khi đọc
            abs = isUSD ? Math.Round(abs, 2) : Math.Round(abs, 0);
            string words = isVietnamese
                ? NumberWords.Vietnamese(abs)
                : NumberWords.English((double)abs, isUSD ? 1 : 0);
            if (amount < 0)
                words = (isVietnamese ? "(Âm) " : "(Minus) ") + words;
            return words;
        }

        private static string SqlSafe(string s) => (s ?? "").Replace("'", "''");
        private static string Str(DataRow r, string col) =>
            r.Table.Columns.Contains(col) && r[col] != DBNull.Value ? r[col].ToString() : "";
        private static decimal Dec(DataRow r, string col)
        {
            if (!r.Table.Columns.Contains(col) || r[col] == DBNull.Value) return 0;
            try { return Convert.ToDecimal(r[col]); } catch { return 0; }
        }
        private static bool ToBool(DataRow r, string col)
        {
            if (!r.Table.Columns.Contains(col) || r[col] == DBNull.Value) return false;
            try { return Convert.ToBoolean(r[col]); } catch { return Str(r, col) == "1"; }
        }
        private static int ToInt(string s) => int.TryParse(s, out int v) ? v : 0;
        private static string FormatDate(DataRow r, string col)
        {
            if (!r.Table.Columns.Contains(col) || r[col] == DBNull.Value) return "";
            try { return Convert.ToDateTime(r[col]).ToString(DateFormat); } catch { return r[col].ToString(); }
        }
        private static string FormatVND(decimal v) => v.ToString(FmtVND, NumCulture);
        private static string FormatUSD(decimal v) => v.ToString(FmtUSD, NumCulture);
        #endregion
    }

    /// <summary>Đọc số thành chữ - port từ WinForm (NumberToEnglish + NumericToString).</summary>
    public static class NumberWords
    {
        #region Tiếng Việt
        public static string Vietnamese(decimal num)
        {
            try
            {
                string[] dvi = { "", "ngàn", "triệu", "tỷ", "ngàn" };
                string result = "";
                long intNum = (long)num;
                for (int i = 0; i < 5; i++)
                {
                    long doc = intNum % 1000;
                    if (doc > 0) result = ReadThree(doc, intNum < 1000) + " " + dvi[i] + " " + result;
                    intNum /= 1000;
                }
                result += "đồng" + ((((long)num) % 1000 == 0) ? " chẵn" : "");
                string t = result.Trim();
                if (t.Length > 5 && t.Substring(0, 1) == "m" && t.Substring(3, 1) == "i")
                    result = "Mười " + t.Remove(0, 5);
                result = result.Trim();
                return result.Length > 0 ? char.ToUpper(result[0]) + result.Substring(1) : result;
            }
            catch { return ""; }
        }

        private static string ReadThree(long num, bool dau)
        {
            long tram = num / 100;
            long chuc = (num % 100) / 10;
            long dvi = num % 10;
            return (((tram == 0) && dau) ? "" : (" " + Digit(tram) + " trăm"))
                + ((chuc == 0) ? (((tram == 0) && dau) ? "" : ((dvi == 0) ? "" : " lẻ")) : ((chuc == 1) ? " mười" : (" " + Digit(chuc) + " mươi")))
                + (((dvi == 5) && (chuc > 0)) ? " lăm" : ((dvi == 0) ? "" : " " + Digit(dvi)));
        }

        private static string Digit(long n)
        {
            string[] number = { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };
            return number[n];
        }
        #endregion

        #region English (Mode 0 = Dong, 1 = US dollars)
        public static string English(double numb, int mode)
        {
            string s = numb.ToString("0.##", CultureInfo.InvariantCulture);
            return ChangeToWords(s, true, mode).Replace("  ", " ").Trim();
        }

        private static string ChangeToWords(string numb, bool isCurrency, int mode)
        {
            string val = "", wholeNo = numb, points = "", andStr = "", pointStr = "";
            string endStr = "";
            int decimalPlace = 0;
            try
            {
                decimalPlace = numb.IndexOf(".");
                if (decimalPlace > 0)
                {
                    wholeNo = numb.Substring(0, decimalPlace);
                    points = numb.Substring(decimalPlace + 1);
                    if (points.Length == 1) points += "0";
                    if (Convert.ToInt32(points) > 0)
                    {
                        andStr = isCurrency ? "and " : "point";
                        endStr = isCurrency && Convert.ToInt32(points) > 1 ? "Cents " : "Cent ";
                        pointStr = points.Length < 2 ? TranslateCents(points) : Tens(points);
                    }
                }
                if (mode == 1)
                {
                    string unit = Convert.ToDecimal(wholeNo, CultureInfo.InvariantCulture) > 1 ? " US dollars " : " US dollar ";
                    val = string.Format("{0} {1}{2} {3}", TranslateWholeNumber(wholeNo).Trim() + unit, andStr, pointStr, endStr);
                }
                else
                {
                    val = string.Format("{0} {1}{2} {3}", TranslateWholeNumber(wholeNo).Trim(), andStr, pointStr, endStr);
                }
            }
            catch { }
            // VND thêm "Dong"; USD đã có "US dollar(s)" (và "Cents" nếu có phần lẻ) trong val
            return mode == 0 ? val + "Dong" : val;
        }

        private static string TranslateWholeNumber(string number)
        {
            string word = "";
            try
            {
                bool isDone = false;
                double dblAmt = Convert.ToDouble(number, CultureInfo.InvariantCulture);
                if (dblAmt > 0)
                {
                    bool beginsZero = number.StartsWith("0");
                    int numDigits = number.Length;
                    int pos = 0;
                    string place = "";
                    switch (numDigits)
                    {
                        case 1: word = Ones(number); isDone = true; break;
                        case 2: word = Tens(number); isDone = true; break;
                        case 3: pos = (numDigits % 3) + 1; place = " Hundred "; break;
                        case 4: case 5: case 6: pos = (numDigits % 4) + 1; place = " Thousand "; break;
                        case 7: case 8: case 9: pos = (numDigits % 7) + 1; place = " Million "; break;
                        case 10: case 11: case 12: pos = (numDigits % 10) + 1; place = " Billion "; break;
                        default: isDone = true; break;
                    }
                    if (!isDone)
                    {
                        word = TranslateWholeNumber(number.Substring(0, pos)) + place + TranslateWholeNumber(number.Substring(pos));
                        if (beginsZero) word = " and " + word.Trim();
                    }
                    if (word.Trim().Equals(place.Trim())) word = "";
                }
            }
            catch { }
            return word.Trim();
        }

        private static string Tens(string digit)
        {
            int digt = Convert.ToInt32(digit);
            string[] teens = { "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
            string[] tens = { "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };
            if (digt >= 10 && digt <= 19) return teens[digt - 10];
            if (digt >= 20 && digt % 10 == 0) return tens[digt / 10];
            if (digt > 20) return tens[digt / 10] + " " + Ones((digt % 10).ToString());
            if (digt > 0) return Ones(digt.ToString());
            return "";
        }

        private static string Ones(string digit)
        {
            string[] ones = { "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine" };
            int d = Convert.ToInt32(digit);
            return d >= 0 && d <= 9 ? ones[d] : "";
        }

        private static string TranslateCents(string cents)
        {
            string cts = "";
            foreach (char c in cents)
                cts += " " + (c == '0' ? "Zero" : Ones(c.ToString()));
            return cts;
        }
        #endregion
    }
}
