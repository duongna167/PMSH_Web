using DevExpress.XtraReports.UI;
using DevExpress.Drawing;
using DevExpress.XtraPrinting;
using System.Drawing;

namespace Cashiering.Reports
{
    public partial class RptExchangeCurrencyByCashier : DevExpress.XtraReports.UI.XtraReport
    {
        public RptExchangeCurrencyByCashier() : this(true)
        {
        }

        public RptExchangeCurrencyByCashier(bool hasTransactions)
        {
            InitializeComponent();
            BuildPrintLayout(hasTransactions);
        }

        // Keep the generated designer and its public parameter contract intact.
        // Layout and summaries live here so all PDF exports use the same rules.
        private void BuildPrintLayout(bool hasTransactions)
        {
            Bands.Clear();
            PaperKind = System.Drawing.Printing.PaperKind.A4;
            Margins = new DXMargins(40, 40, 35, 35);
            Font = new DXFont("Arial", 9F);
            ForeColor = Color.FromArgb(35, 43, 50);
            RequestParameters = false;
            foreach (DevExpress.XtraReports.Parameters.Parameter parameter in Parameters)
                parameter.Visible = false;

            float width = PageWidth - Margins.Left - Margins.Right;
            Bands.AddRange(new Band[] { new TopMarginBand { HeightF = 35 }, new BottomMarginBand { HeightF = 35 } });

            var heading = new ReportHeaderBand { HeightF = 100 };
            heading.Controls.Add(Label("FRONT OFFICE  /  FO DEPT", 0, 0, width, 18, 9, true));
            heading.Controls.Add(Label("EXCHANGE CURRENCY REPORT", 0, 23, width, 30, 19, true));
            heading.Controls.Add(Label("Reimbursement summary", 0, 55, width, 19, 10));
            var date = Label("", 0, 78, width, 18, 9);
            Bind(date, "Concat('Transaction date: ', ?TransactionDate)");
            heading.Controls.Add(date);
            Bands.Add(heading);

            if (!hasTransactions)
            {
                // Render a real empty report, without inventing a cashier or a transaction row.
                heading.HeightF = 370;
                heading.Controls.Add(Table(width, 120, 28,
                    new double[] { 42, 62, 106, 58, 124, 105, 150 },
                    new[] { "No.", "Currency", "Denomination", "Quantity", "Amount", "Exchange rate", "VND value" }, true));
                var message = Label("No exchange currency transactions found for the selected filters.",
                    0, 158, width, 40, 10);
                message.TextAlignment = TextAlignment.MiddleCenter;
                heading.Controls.Add(message);
                var emptyTotal = Table(width, 210, 34, new double[] { 497, 150 },
                    new[] { "GRAND TOTAL (VND)", "0" }, true);
                emptyTotal.Rows[0].Cells[1].TextAlignment = TextAlignment.MiddleRight;
                heading.Controls.Add(emptyTotal);
                string[] emptySignatures = { "Cashier", "Verified by", "Chief Cashier", "Witnessed by" };
                for (int i = 0; i < emptySignatures.Length; i++)
                {
                    float x = i * width / 4;
                    var role = Label(emptySignatures[i], x, 260, width / 4, 20, 9, true);
                    role.TextAlignment = TextAlignment.MiddleCenter;
                    var line = Label("______________________", x, 320, width / 4, 18, 8);
                    line.TextAlignment = TextAlignment.MiddleCenter;
                    var note = Label("Signature / Full name", x, 342, width / 4, 16, 7);
                    note.TextAlignment = TextAlignment.MiddleCenter;
                    heading.Controls.AddRange(new XRControl[] { role, line, note });
                }
                Bands.Add(new DetailBand { HeightF = 0 });
                AddPageFooter(width);
                return;
            }

            // Multiple selected cashiers/shifts must never inherit the first row's metadata.
            var cashierHeader = new GroupHeaderBand
            {
                Level = 1, HeightF = 82, RepeatEveryPage = true,
                GroupUnion = GroupUnion.WithFirstDetail
            };
            foreach (string field in new[] { "TransactionDate", "CashierName", "ShiftID", "LoginTime", "LogoutTime" })
                cashierHeader.GroupFields.Add(new GroupField(field));
            var cashier = Label("", 0, 12, width * .66F, 23, 11, true);
            Bind(cashier, "Concat('Cashier: ', [CashierName])");
            var shift = Label("", width * .66F, 12, width * .34F, 23, 10, true);
            Bind(shift, "Concat('Shift: ', [ShiftID])");
            var period = Label("", 0, 39, width, 22, 9);
            Bind(period, "Concat('From: ', [LoginTime], '     To: ', [LogoutTime])");
            cashierHeader.Controls.AddRange(new XRControl[] { cashier, shift, period,
                Label("Float No.: ____________________", 0, 62, width, 18, 8) });
            Bands.Add(cashierHeader);

            var currencyHeader = new GroupHeaderBand
            {
                Level = 0, HeightF = 48, RepeatEveryPage = true,
                GroupUnion = GroupUnion.WithFirstDetail
            };
            currencyHeader.GroupFields.Add(new GroupField("FromCurrencyID"));
            var currency = Label("", 0, 0, width, 20, 9, true);
            Bind(currency, "Concat('Currency: ', [FromCurrencyID])");
            currencyHeader.Controls.Add(currency);
            // Denomination and Quantity are the actual fields behind the old Type/Number labels.
            double[] columns = { 42, 62, 106, 58, 124, 105, 150 };
            currencyHeader.Controls.Add(Table(width, 22, 26, columns,
                new[] { "No.", "Currency", "Denomination", "Quantity", "Amount", "Exchange rate", "VND value" }, true));
            Bands.Add(currencyHeader);

            var detail = new DetailBand { HeightF = 26, KeepTogether = true };
            var rows = Table(width, 0, 26, columns, new string[7]);
            var cells = rows.Rows[0].Cells;
            Bind(cells[0], "sumRecordNumber()");
            cells[0].Summary.Running = SummaryRunning.Report;
            string[] fields = { "FromCurrencyID", "Denomination", "Quantity", "Amount", "ExchangeRate", "AmountVND" };
            for (int i = 0; i < fields.Length; i++)
                Bind(cells[i + 1], "[" + fields[i] + "]");
            for (int i = 2; i < cells.Count; i++)
            {
                cells[i].TextAlignment = TextAlignment.MiddleRight;
                cells[i].TextFormatString = i == 3 || i == 6 ? "{0:#,##0;(#,##0);0}" : "{0:#,##0.####;(#,##0.####);0}";
            }
            detail.Controls.Add(rows);
            Bands.Add(detail);

            // Only sum original amounts inside a currency group; VND can be summed across currencies.
            var currencyFooter = new GroupFooterBand { Level = 0, HeightF = 33, KeepTogether = true };
            var subtotal = Table(width, 0, 28, new double[] { 268, 124, 105, 150 },
                new[] { "Currency subtotal", "", "", "" }, true);
            Bind(subtotal.Rows[0].Cells[0], "Concat('Subtotal ', [FromCurrencyID])");
            Sum(subtotal.Rows[0].Cells[1], "Amount", SummaryRunning.Group, false);
            Sum(subtotal.Rows[0].Cells[3], "AmountVND", SummaryRunning.Group, true);
            currencyFooter.Controls.Add(subtotal);
            Bands.Add(currencyFooter);

            var cashierFooter = new GroupFooterBand { Level = 1, HeightF = 145, KeepTogether = true };
            var cashierTotal = Table(width, 0, 30, new double[] { 497, 150 },
                new[] { "CASHIER / SHIFT TOTAL (VND)", "" }, true);
            Sum(cashierTotal.Rows[0].Cells[1], "AmountVND", SummaryRunning.Group, true);
            cashierFooter.Controls.Add(cashierTotal);
            string[] signatures = { "Cashier", "Verified by", "Chief Cashier", "Witnessed by" };
            for (int i = 0; i < signatures.Length; i++)
            {
                float x = i * width / 4;
                var role = Label(signatures[i], x, 43, width / 4, 20, 9, true);
                role.TextAlignment = TextAlignment.MiddleCenter;
                var line = Label("______________________", x, 101, width / 4, 18, 8);
                line.TextAlignment = TextAlignment.MiddleCenter;
                var note = Label("Signature / Full name", x, 122, width / 4, 16, 7);
                note.TextAlignment = TextAlignment.MiddleCenter;
                cashierFooter.Controls.AddRange(new XRControl[] { role, line, note });
            }
            Bands.Add(cashierFooter);

            var footer = new ReportFooterBand { HeightF = 48, KeepTogether = true };
            var total = Table(width, 8, 34, new double[] { 497, 150 },
                new[] { "GRAND TOTAL (VND)", "" }, true);
            Sum(total.Rows[0].Cells[1], "AmountVND", SummaryRunning.Report, true);
            footer.Controls.Add(total);
            Bands.Add(footer);

            AddPageFooter(width);

            // Adding group bands can renumber existing levels. Set nesting only
            // after the collection is complete: cashier outside, currency inside.
            cashierHeader.Level = 1;
            currencyHeader.Level = 0;
            cashierFooter.Level = 1;
            currencyFooter.Level = 0;
        }

        private void AddPageFooter(float width)
        {
            var pageFooter = new PageFooterBand { HeightF = 26 };
            pageFooter.Controls.Add(Label("FO DEPT  /  Exchange currency", 0, 6, width - 120, 18, 8));
            pageFooter.Controls.Add(new XRPageInfo
            {
                BoundsF = new RectangleF(width - 120, 6, 120, 18),
                PageInfo = PageInfo.NumberOfTotal, TextFormatString = "Page {0} of {1}",
                TextAlignment = TextAlignment.MiddleRight, Font = new DXFont("Arial", 8)
            });
            Bands.Add(pageFooter);

        }

        private static XRLabel Label(string text, float x, float y, float width, float height, float size, bool bold = false)
        {
            return new XRLabel
            {
                Text = text, BoundsF = new RectangleF(x, y, width, height),
                Font = new DXFont("Arial", size, bold ? DXFontStyle.Bold : DXFontStyle.Regular),
                TextAlignment = TextAlignment.MiddleLeft, CanGrow = true,
                Padding = new PaddingInfo(0, 0, 0, 0)
            };
        }

        private static XRTable Table(float width, float y, float height, double[] weights, string[] captions, bool emphasis = false)
        {
            var table = new XRTable
            {
                BoundsF = new RectangleF(0, y, width, height),
                Borders = BorderSide.Bottom, BorderColor = Color.FromArgb(205, 211, 215),
                BackColor = emphasis ? Color.FromArgb(237, 241, 243) : Color.White,
                Font = new DXFont("Arial", 8.5F, emphasis ? DXFontStyle.Bold : DXFontStyle.Regular),
                Padding = new PaddingInfo(6, 6, 4, 4), TextAlignment = TextAlignment.MiddleLeft
            };
            table.BeginInit();
            var row = new XRTableRow();
            for (int i = 0; i < weights.Length; i++)
                row.Cells.Add(new XRTableCell { Weight = weights[i], Text = captions[i] ?? "", CanGrow = true });
            table.Rows.Add(row);
            table.EndInit();
            return table;
        }

        private static void Bind(XRControl control, string expression)
        {
            control.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", expression));
        }

        private static void Sum(XRTableCell cell, string field, SummaryRunning scope, bool vnd)
        {
            Bind(cell, "sumSum([" + field + "])");
            cell.Summary.Running = scope;
            cell.TextFormatString = vnd ? "{0:#,##0;(#,##0);0}" : "{0:#,##0.####;(#,##0.####);0}";
            cell.TextAlignment = TextAlignment.MiddleRight;
        }
    }
}
