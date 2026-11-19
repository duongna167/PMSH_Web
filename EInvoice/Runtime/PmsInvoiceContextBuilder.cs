using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;

namespace EInvoice.Runtime
{
    /// <summary>Thông tin kết nối tới DB nguồn (PMS) của một Property. Mật khẩu đã được giải mã.</summary>
    public sealed class PmsSourceConfig
    {
        public int PropertyID { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string DatabaseIP { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string DatabaseUser { get; set; } = string.Empty;
        public string DatabasePassword { get; set; } = string.Empty;
        public string InvoiceSearchSP { get; set; } = string.Empty;
        public string InvoiceDetailSP { get; set; } = string.Empty;

        /// <summary>Chuỗi kết nối tới DB nguồn (kết nối động theo từng Property).</summary>
        public string BuildConnectionString()
        {
            if (string.IsNullOrWhiteSpace(DatabaseIP) || string.IsNullOrWhiteSpace(DatabaseName)
                || string.IsNullOrWhiteSpace(DatabaseUser) || string.IsNullOrWhiteSpace(DatabasePassword))
                throw new ProviderBuildException($"Cấu hình database của {Code} chưa đầy đủ.");

            return new SqlConnectionStringBuilder
            {
                DataSource = DatabaseIP,
                InitialCatalog = DatabaseName.Replace("[", "").Replace("]", ""),
                UserID = DatabaseUser,
                Password = DatabasePassword,
                TrustServerCertificate = true,
                ConnectTimeout = 15
            }.ConnectionString;
        }
    }

    public sealed class ProviderInvoiceHeaderRuntimeModel
    {
        public int ID { get; set; }

        public int FolioVATID { get; set; }

        /// <summary>
        /// ID bản ghi nguồn dùng chung cho Provider mapping.
        /// Với nguồn PMS, bản ghi nguồn là FolioVAT.
        /// </summary>
        public int SourceRecordID => FolioVATID;

        public string RoomNo { get; set; }
            = string.Empty;

        public string FolioNo { get; set; } = string.Empty;

        public int FolioID { get; set; }

        public int ReservationID { get; set; }

        public int ProfileID { get; set; }

        public string InvoiceNo { get; set; }
            = string.Empty;

        public DateTime InvoiceDate { get; set; }

        public string SerialNo { get; set; }
            = string.Empty;

        public string TransactionUuid { get; set; }
            = string.Empty;

        public string CustomerName { get; set; }
            = string.Empty;

        public string CustomerCompany { get; set; }
            = string.Empty;

        // Alias dùng cho mapping provider khác
        public string CompanyName { get; set; }
            = string.Empty;

        public string CustomerTaxCode { get; set; }
            = string.Empty;

        // Alias dùng cho mapping provider khác
        public string TaxCode { get; set; }
            = string.Empty;

        public string CustomerAddress { get; set; }
            = string.Empty;

        public string CustomerPhone { get; set; }
            = string.Empty;

        public string CustomerEmail { get; set; }
            = string.Empty;

        public string PaymentMethod { get; set; }
            = string.Empty;

        public string Currency { get; set; }
            = "VND";

        public string CurrencyCode { get; set; }
            = "VND";

        public decimal AmountBeforeTax { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal AmountIncludedTax { get; set; }

        public decimal TaxableAmount { get; set; }

        public decimal DiscountAmount { get; set; }

        public string AmountInWords { get; set; }
            = string.Empty;

        public string Note { get; set; }
            = string.Empty;

        public string Comment { get; set; }
            = string.Empty;

        public bool PaymentStatus { get; set; }
    }

    /// <summary>
    /// Dựng context (header, chi tiết, tổng tiền, kết quả Upload trước đó...) của một hóa đơn PMS để đưa vào engine.
    /// Đọc FolioVAT / FolioVATDetail từ DB nguồn của Property, chuẩn hóa sai số làm tròn và kiểm tra công thức tổng tiền.
    /// Chuyển từ EInvoiceController của PMSH_Web, giữ nguyên logic.
    /// </summary>
    public sealed class PmsInvoiceContextBuilder
    {
        /// <summary>Tạo ProviderRequestContext cho hóa đơn PMS (FolioVATID) của Property.</summary>
        public ProviderRequestContext Create(PmsSourceConfig source, Dictionary<string, object?> config, int folioVatId,
            PreviousInvoiceResult? previousInvoiceResult, int? userId, string? userName, string? loginName)
        {
            ProviderInvoiceHeaderRuntimeModel invoice;
            List<ProviderInvoiceDetailRuntimeModel> invoiceDetails;
            using (SqlConnection propertyConnection = new SqlConnection(source.BuildConnectionString()))
            {
                propertyConnection.Open();
                invoice = GetRuntimeInvoiceHeader(propertyConnection, folioVatId);
                // Hóa đơn PMS lấy chi tiết từ FolioVATDetail; InvoiceDetailSP chỉ dùng cho nguồn POS.
                invoiceDetails = GetRuntimeInvoiceDetailsLegacy(propertyConnection, folioVatId);
            }

            // Chuẩn hóa sai số làm tròn dưới 1 VND tại tầng canonical của PMS rồi tính lại tổng ở header từ chi tiết
            NormalizePmsCanonicalInvoiceAmounts(invoiceDetails, invoice.Currency);

            decimal normalizedAmountBeforeTax = invoiceDetails.Sum(x => x.AmountBeforeTax);
            decimal normalizedTaxAmount = invoiceDetails.Sum(x => x.TaxAmount);
            decimal normalizedTotalAmount = invoiceDetails.Sum(x => x.TotalAmount);
            decimal normalizedTaxableAmount = invoiceDetails.Sum(x => x.TaxableAmount);
            decimal normalizedDiscountAmount = invoiceDetails.Sum(x => x.DiscountAmount);

            invoice.AmountBeforeTax = normalizedAmountBeforeTax;
            invoice.TaxableAmount = normalizedTaxableAmount;
            invoice.TaxAmount = normalizedTaxAmount;
            invoice.TotalAmount = normalizedTotalAmount;
            invoice.AmountIncludedTax = normalizedTotalAmount;
            invoice.DiscountAmount = normalizedDiscountAmount;
            invoice.AmountInWords = ConvertVndAmountToWords(normalizedTotalAmount);

            ValidateInvoiceRuntimeContext(invoice, invoiceDetails);

            List<List<object?>> invoiceDetailRows = BuildInvoiceDetailRows(invoiceDetails);

            var taxBreakdowns = invoiceDetails
                .GroupBy(x => x.VATRate)
                .Select(g => new
                {
                    TaxPercentage = g.Key,
                    TaxableAmount = g.Sum(x => x.TaxableAmount),
                    TaxAmount = g.Sum(x => x.TaxAmount)
                })
                .ToList();

            // Kết quả Upload thành công trước đó (dùng cho bước Publish)
            var previousResponse = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (previousInvoiceResult != null)
            {
                previousResponse["Key"] = previousInvoiceResult.Key;
                previousResponse["InvoiceNumber"] = previousInvoiceResult.InvoiceNumber;
                previousResponse["InvoiceForm"] = previousInvoiceResult.InvoiceForm;
                previousResponse["InvoiceSerial"] = previousInvoiceResult.InvoiceSerial;
                previousResponse["InvoiceDate"] = previousInvoiceResult.InvoiceDate;
                previousResponse["VerificationCode"] = previousInvoiceResult.VerificationCode;
                previousResponse["KeySearch"] = previousInvoiceResult.KeySearch;
            }

            var context = new ProviderRequestContext
            {
                Invoice = invoice,
                InvoiceDetail = invoiceDetails,
                Property = new
                {
                    PropertyID = source.PropertyID,
                    PropertyCode = source.Code,
                    PropertyName = source.Name,
                    SourceType = source.SourceType
                },
                Config = config,
                PreviousResponse = previousResponse
            };

            context.Collections["Items"] = invoiceDetails;
            context.Collections["TaxBreakdowns"] = taxBreakdowns;

            context.Session["UserID"] = userId;
            context.Session["UserName"] = userName;
            context.Session["LoginName"] = loginName;

            context.System["CurrentDate"] = DateTime.Today;
            context.System["CurrentDateTime"] = DateTime.Now;
            context.System["UtcNow"] = DateTime.UtcNow;
            context.System["GUID"] = Guid.NewGuid().ToString();
            context.System["NewGuid"] = Guid.NewGuid().ToString();

            // Mảng chi tiết dạng positional array (mapping của Provider nào cần thì dùng)
            context.System["InvoiceDetailRows"] = invoiceDetailRows;
            context.System["SourceType"] = source.SourceType;
            context.System["SourceRecordID"] = folioVatId;
            context.System["ProviderKey"] = $"{source.PropertyID}_{source.SourceType}_{folioVatId}";

            return context;
        }

        // =====================================================================================================
        //  Đọc header / chi tiết hóa đơn từ DB nguồn
        // =====================================================================================================

        private ProviderInvoiceHeaderRuntimeModel GetRuntimeInvoiceHeader(SqlConnection connection, int folioVatId)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (connection.State != ConnectionState.Open)
            {
                throw new ProviderBuildException("Property database connection chưa được mở.");
            }

            if (folioVatId <= 0)
            {
                throw new ProviderBuildException("FolioVATID không hợp lệ.");
            }

            const string sql = @"
                SELECT TOP 1
                    fv.ID,
                    fv.FolioID,
                    fv.ReservationID,
                    fv.ProfileID,
                    fv.VATDate,
                    fv.InvoiceNo,
                    fv.SerialNo,

                    fv.Customer_Name,
                    fv.Customer_Company,
                    fv.Customer_TaxCode,
                    fv.Customer_Address,
                    fv.Customer_Tel,

                    fv.PaymentMethod,
                    fv.Note,

                    fv.Amount,
                    fv.AmountVAT5,
                    fv.AmountVAT10,
                    fv.AmountIncludedTax,

                    f.FolioNo AS FolioNo,
                    r.RoomNo AS RoomNo

                FROM dbo.FolioVAT fv

                LEFT JOIN dbo.Folio f
                    ON f.ID = fv.FolioID

                LEFT JOIN dbo.Reservation r
                    ON r.ID = fv.ReservationID

                WHERE fv.ID = @FolioVATID;
            ";

            using SqlCommand command = new SqlCommand(sql, connection);

            command.Parameters
                .Add("@FolioVATID", SqlDbType.Int)
                .Value = folioVatId;

            using SqlDataReader reader = command.ExecuteReader();

            if (!reader.Read())
            {
                throw new ProviderBuildException($"Không tìm thấy FolioVAT ID {folioVatId}.");
            }

            string customerCompany = GetReaderString(reader, "Customer_Company");

            string customerTaxCode = GetReaderString(reader, "Customer_TaxCode");

            string note = GetReaderString(reader, "Note");

            decimal totalAmount = GetReaderDecimal(reader, "AmountIncludedTax");

            return new ProviderInvoiceHeaderRuntimeModel
            {
                ID = GetReaderInt(reader, "ID"),

                FolioVATID = GetReaderInt(reader, "ID"),

                FolioID = GetReaderInt(reader, "FolioID"),

                /*
                 * FolioNo nghiệp vụ.
                 * Không dùng FolioID thay thế.
                 */
                FolioNo = GetReaderString(reader, "FolioNo"),

                ReservationID = GetReaderInt(reader, "ReservationID"),

                /*
                 * RoomNo lấy từ Reservation của PMS.
                 */
                RoomNo = GetReaderString(reader, "RoomNo"),

                ProfileID = GetReaderInt(reader, "ProfileID"),

                InvoiceNo = GetReaderString(reader, "InvoiceNo"),

                InvoiceDate = GetReaderDateTime(reader, "VATDate") ?? DateTime.Today,

                SerialNo = GetReaderString(reader, "SerialNo"),

                TransactionUuid = Guid.NewGuid().ToString(),

                CustomerName = GetReaderString(reader, "Customer_Name"),

                CustomerCompany = customerCompany,

                CompanyName = customerCompany,

                CustomerTaxCode = customerTaxCode,

                TaxCode = customerTaxCode,

                CustomerAddress = GetReaderString(reader, "Customer_Address"),

                CustomerPhone = GetReaderString(reader, "Customer_Tel"),

                /*
                 * Chưa xác định source email trong PMS.
                 * Không tự đoán field.
                 */
                CustomerEmail = string.Empty,

                PaymentMethod = GetReaderString(reader, "PaymentMethod"),

                Currency = "VND",

                CurrencyCode = "VND",

                /*
                 * Header ban đầu lấy từ FolioVAT.
                 *
                 * CreatePreviewRequestContext()
                 * sẽ normalize lại từ Detail.
                 */
                AmountBeforeTax = GetReaderDecimal(reader, "Amount"),

                TaxAmount = GetReaderDecimal(reader, "AmountVAT5") + GetReaderDecimal(reader, "AmountVAT10"),

                TotalAmount = totalAmount,

                AmountIncludedTax = totalAmount,

                Note = note,

                Comment = note,

                PaymentStatus = false
            };
        }

        private List<ProviderInvoiceDetailRuntimeModel> GetRuntimeInvoiceDetailsLegacy(SqlConnection connection, int folioVatId)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (connection.State != ConnectionState.Open)
            {
                throw new ProviderBuildException("Property database connection chưa được mở.");
            }

            if (folioVatId <= 0)
            {
                throw new ProviderBuildException("FolioVATID không hợp lệ.");
            }

            const string sql = @"
                SELECT
                    d.ID,
                    d.FolioDetailID,
                    d.FolioVATID,
                    d.Amount,
                    d.VAT,
                    d.VATAmount,
                    d.AmountIncludedTax,
                    d.Svc,
                    d.SvcAmount,
                    d.Price,
                    d.Quantity,

                    fd.TransactionDate,
                    fd.TransactionCode,
                    fd.Description,
                    fd.Supplement,
                    fd.Reference,
                    fd.CurrencyID

                FROM dbo.FolioVATDetail d

                LEFT JOIN dbo.FolioDetail fd
                    ON fd.ID = d.FolioDetailID

                WHERE d.FolioVATID = @FolioVATID

                ORDER BY d.ID;
            ";

            var details = new List<ProviderInvoiceDetailRuntimeModel>();

            using SqlCommand command = new SqlCommand(sql, connection);

            command.Parameters.Add("@FolioVATID", SqlDbType.Int).Value = folioVatId;

            using SqlDataReader reader = command.ExecuteReader();

            int lineNumber = 1;

            while (reader.Read())
            {
                decimal quantity = GetReaderDecimal(reader, "Quantity");

                if (quantity <= 0)
                {
                    quantity = 1m;
                }

                decimal amountBeforeTax = GetReaderDecimal(reader, "Amount");

                decimal taxAmount = GetReaderDecimal(reader, "VATAmount");

                decimal amountIncludedTax = GetReaderDecimal(reader, "AmountIncludedTax");

                decimal unitPrice = GetReaderDecimal(reader, "Price");

                if (unitPrice == 0m && quantity > 0m)
                {
                    unitPrice = amountBeforeTax / quantity;
                }

                string itemCode = GetReaderString(reader, "TransactionCode");

                string itemName = GetReaderString(reader, "Description");

                if (string.IsNullOrWhiteSpace(itemCode))
                {
                    itemCode = "SERVICE";
                }

                if (string.IsNullOrWhiteSpace(itemName))
                {
                    itemName = "Dịch vụ";
                }

                decimal vatRate = GetReaderDecimal(reader, "VAT");

                decimal[] validVatRates = { 0m, 5m, 8m, 10m, -1m, -2m, -9m };

                if (!validVatRates.Contains(vatRate))
                {
                    throw new ProviderBuildException($"Dòng chi tiết ID " + $"{GetReaderInt(reader, "ID")} " + $"có VATRate không hợp lệ: {vatRate}.");
                }

                details.Add(new ProviderInvoiceDetailRuntimeModel
                {
                    LineNumber = lineNumber,

                    ID = GetReaderInt(reader, "ID"),

                    FolioDetailID = GetReaderInt(reader, "FolioDetailID"),

                    ItemCode = itemCode,

                    ItemName = itemName,

                    UnitName = "Lần",

                    Quantity = quantity,

                    UnitPrice = unitPrice,

                    AmountBeforeTax = amountBeforeTax,

                    VATRate = vatRate,

                    TaxAmount = taxAmount,

                    DiscountRate = 0m,

                    DiscountAmount = 0m,

                    ServiceChargeRate = reader["Svc"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["Svc"]),

                    ServiceChargeAmount = reader["SvcAmount"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["SvcAmount"]),

                    TotalAmount = amountIncludedTax,

                    Supplement = GetReaderString(reader, "Supplement"),

                    Reference = GetReaderString(reader, "Reference")
                }
                );

                lineNumber++;
            }

            if (details.Count == 0)
            {
                throw new ProviderBuildException($"FolioVAT ID {folioVatId} " + "không có dữ liệu FolioVATDetail.");
            }

            return details;
        }

        private static void NormalizePmsCanonicalInvoiceAmounts(IReadOnlyList<ProviderInvoiceDetailRuntimeModel> details, string? currency)
        {
            if (details == null || details.Count == 0)
            {
                return;
            }

            /*
             * Runtime PMS hiện dùng VND. Giữ ngưỡng nhỏ hơn cho ngoại tệ để
             * không vô tình chấp nhận sai số lớn nếu sau này mở rộng currency.
             */
            decimal roundingTolerance = string.Equals(currency, "VND", StringComparison.OrdinalIgnoreCase) ? 1.00m : 0.01m;

            decimal sourceTotal = details.Sum(x => x.TotalAmount);

            decimal calculatedTotal = details.Sum(x => x.TaxableAmount + x.TaxAmount - x.DiscountAmount);

            decimal difference = sourceTotal - calculatedTotal;

            if (Math.Abs(difference) <= 0.0001m)
            {
                return;
            }

            /*
             * Không sửa dữ liệu khi sai lệch vượt phạm vi làm tròn. Validation
             * phía sau sẽ trả lỗi với đầy đủ Total/Taxable/Tax/Discount.
             */
            if (Math.Abs(difference) > roundingTolerance)
            {
                return;
            }

            ProviderInvoiceDetailRuntimeModel? adjustmentLine = details
                .Where(x => x.VATRate > 0m)
                .OrderByDescending(x => Math.Abs(x.TaxAmount))
                .FirstOrDefault();

            if (adjustmentLine == null)
            {
                return;
            }

            decimal adjustedTaxAmount = adjustmentLine.TaxAmount + difference;

            if (adjustedTaxAmount < 0m)
            {
                return;
            }

            adjustmentLine.TaxAmount = adjustedTaxAmount;
        }

        private static void ValidateInvoiceRuntimeContext(ProviderInvoiceHeaderRuntimeModel invoice, IReadOnlyList<ProviderInvoiceDetailRuntimeModel> details)
        {
            if (invoice == null)
            {
                throw new ProviderBuildException("Invoice runtime đang null.");
            }

            if (details == null || details.Count == 0)
            {
                throw new ProviderBuildException("Invoice không có detail để validate.");
            }

            decimal detailAmountBeforeTax = details.Sum(x => x.AmountBeforeTax);

            decimal detailTaxableAmount = details.Sum(x => x.TaxableAmount);

            decimal detailTaxAmount = details.Sum(x => x.TaxAmount);

            decimal detailTotalAmount = details.Sum(x => x.TotalAmount);

            decimal detailServiceChargeAmount = details.Sum(x => x.ServiceChargeAmount);

            /*
             * Sai số cho decimal/currency.
             *
             * Không dùng == trực tiếp để tránh lệch rất nhỏ
             * do dữ liệu source có decimal scale khác nhau.
             */
            const decimal tolerance = 0.01m;

            static bool IsDifferent(decimal left, decimal right, decimal toleranceValue)
            {
                return Math.Abs(left - right) > toleranceValue;
            }


            /*
             * ==================================================
             * 1. HEADER VS DETAIL
             * ==================================================
             */

            if (IsDifferent(invoice.AmountBeforeTax, detailAmountBeforeTax, tolerance))
            {
                throw new ProviderBuildException("AmountBeforeTax không khớp. " + $"Header={invoice.AmountBeforeTax}, " + $"Detail={detailAmountBeforeTax}.");
            }

            if (IsDifferent(invoice.TaxableAmount, detailTaxableAmount, tolerance))
            {
                throw new ProviderBuildException("TaxableAmount không khớp. " + $"Header={invoice.TaxableAmount}, " + $"Detail={detailTaxableAmount}.");
            }

            if (IsDifferent(invoice.TaxAmount, detailTaxAmount, tolerance))
            {
                throw new ProviderBuildException("TaxAmount không khớp. " + $"Header={invoice.TaxAmount}, " + $"Detail={detailTaxAmount}.");
            }

            if (IsDifferent(invoice.TotalAmount, detailTotalAmount, tolerance))
            {
                throw new ProviderBuildException("TotalAmount không khớp. " + $"Header={invoice.TotalAmount}, " + $"Detail={detailTotalAmount}.");
            }


            /*
             * ==================================================
             * 2. TAXABLE AMOUNT FORMULA
             * ==================================================
             *
             * Canonical runtime hiện định nghĩa:
             *
             * TaxableAmount =
             * AmountBeforeTax + ServiceChargeAmount
             */

            decimal expectedTaxableAmount = detailAmountBeforeTax + detailServiceChargeAmount;

            if (IsDifferent(detailTaxableAmount, expectedTaxableAmount, tolerance))
            {
                throw new ProviderBuildException("TaxableAmount không đúng công thức canonical. " + $"TaxableAmount={detailTaxableAmount}, " + $"AmountBeforeTax={detailAmountBeforeTax}, " + $"ServiceChargeAmount={detailServiceChargeAmount}.");
            }


            /*
             * ==================================================
             * 3. TOTAL FORMULA
             * ==================================================
             */

            decimal expectedTotalAmount = detailTaxableAmount + detailTaxAmount - details.Sum(x => x.DiscountAmount);

            if (IsDifferent(detailTotalAmount, expectedTotalAmount, tolerance))
            {
                throw new ProviderBuildException("TotalAmount không đúng công thức canonical. " + $"Total={detailTotalAmount}, " + $"Taxable={detailTaxableAmount}, " + $"Tax={detailTaxAmount}, " + $"Discount={details.Sum(x => x.DiscountAmount)}.");
            }


            /*
             * ==================================================
             * 4. DETAIL BASIC VALIDATION
             * ==================================================
             */

            foreach (ProviderInvoiceDetailRuntimeModel detail in details)
            {
                if (detail.LineNumber <= 0)
                {
                    throw new ProviderBuildException("Detail có LineNumber không hợp lệ.");
                }

                if (detail.Quantity <= 0m)
                {
                    throw new ProviderBuildException($"Dòng {detail.LineNumber} " + "có Quantity <= 0.");
                }

                if (detail.AmountBeforeTax < 0m)
                {
                    throw new ProviderBuildException($"Dòng {detail.LineNumber} " + "có AmountBeforeTax âm.");
                }

                if (detail.ServiceChargeAmount < 0m)
                {
                    throw new ProviderBuildException($"Dòng {detail.LineNumber} " + "có ServiceChargeAmount âm.");
                }

                if (detail.TaxAmount < 0m)
                {
                    throw new ProviderBuildException($"Dòng {detail.LineNumber} " + "có TaxAmount âm.");
                }

                if (detail.TotalAmount < 0m)
                {
                    throw new ProviderBuildException($"Dòng {detail.LineNumber} " + "có TotalAmount âm.");
                }
            }
        }

        private static List<List<object?>> BuildInvoiceDetailRows(IEnumerable<ProviderInvoiceDetailRuntimeModel> invoiceDetails)
        {
            var rows = new List<List<object?>>();

            foreach (ProviderInvoiceDetailRuntimeModel item in invoiceDetails)
            {
                /*
                 * Thứ tự phải khớp hoàn toàn với:
                 *
                 * ProcessType
                 * ItemCode
                 * ItemName
                 * UOM
                 * Quantity
                 * Price
                 * Amount
                 * TaxRate
                 * TaxAmount
                 * DiscountRate
                 * DiscountAmount
                 */
                rows.Add(new List<object?>
                    {
                        "1",

                        item.ItemCode,

                        item.ItemName,

                        item.UnitName,

                        item.Quantity,

                        item.UnitPrice,

                        item.AmountBeforeTax,

                        item.VATRate,

                        item.TaxAmount,

                        item.DiscountRate,

                        item.DiscountAmount
                    }
                );
            }

            return rows;
        }

        // =====================================================================================================
        //  Tiền bằng chữ và hàm đọc DataReader
        // =====================================================================================================

        private static string ConvertVndAmountToWords(decimal amount)
        {
            // Tiền VND không sử dụng phần thập phân trong payload này.
            long roundedAmount = decimal.ToInt64(decimal.Round(amount, 0, MidpointRounding.AwayFromZero));

            if (roundedAmount == 0)
            {
                return "Không đồng chẵn";
            }

            bool isNegative = roundedAmount < 0;

            ulong absoluteAmount;

            if (roundedAmount == long.MinValue)
            {
                absoluteAmount = (ulong)long.MaxValue + 1UL;
            }
            else
            {
                absoluteAmount = (ulong)Math.Abs(roundedAmount);
            }

            string words = ReadVietnameseInteger(absoluteAmount);

            if (isNegative)
            {
                words = "Âm " + words;
            }
            else
            {
                words = char.ToUpperInvariant(words[0]) + words.Substring(1);
            }

            return words.Trim() + " đồng chẵn";
        }

        private static string ReadVietnameseInteger(ulong number)
        {
            string[] scaleNames =
            {
                "",
                "nghìn",
                "triệu",
                "tỷ",
                "nghìn tỷ",
                "triệu tỷ",
                "tỷ tỷ"
            };

            var groups = new List<int>();

            while (number > 0)
            {
                groups.Add((int)(number % 1000));

                number /= 1000;
            }

            var parts = new List<string>();

            for (int index = groups.Count - 1; index >= 0; index--)
            {
                int groupValue = groups[index];

                if (groupValue == 0)
                {
                    continue;
                }

                bool readFullHundreds = index < groups.Count - 1;

                string groupWords = ReadVietnameseThreeDigits(groupValue, readFullHundreds);

                if (!string.IsNullOrWhiteSpace(groupWords))
                {
                    string scale = index < scaleNames.Length ? scaleNames[index] : string.Empty;

                    parts.Add(string.IsNullOrWhiteSpace(scale) ? groupWords : groupWords + " " + scale);
                }
            }

            return string.Join(" ", parts).Replace("  ", " ").Trim();
        }

        private static string ReadVietnameseThreeDigits(int number, bool readFullHundreds)
        {
            string[] digits =
            {
                "không",
                "một",
                "hai",
                "ba",
                "bốn",
                "năm",
                "sáu",
                "bảy",
                "tám",
                "chín"
            };

            int hundreds = number / 100;

            int tens = (number % 100) / 10;

            int units = number % 10;

            var parts = new List<string>();

            /*
             * Ví dụ:
             * 1005 = một nghìn không trăm lẻ năm.
             */
            if (hundreds > 0 || readFullHundreds)
            {
                parts.Add(digits[hundreds]);

                parts.Add("trăm");
            }

            if (tens > 1)
            {
                parts.Add(digits[tens]);

                parts.Add("mươi");

                if (units == 1)
                {
                    parts.Add("mốt");
                }
                else if (units == 4)
                {
                    parts.Add("tư");
                }
                else if (units == 5)
                {
                    parts.Add("lăm");
                }
                else if (units > 0)
                {
                    parts.Add(digits[units]);
                }
            }
            else if (tens == 1)
            {
                parts.Add("mười");

                if (units == 5)
                {
                    parts.Add("lăm");
                }
                else if (units > 0)
                {
                    parts.Add(digits[units]);
                }
            }
            else if (units > 0)
            {
                if (hundreds > 0 || readFullHundreds)
                {
                    parts.Add("lẻ");
                }

                parts.Add(digits[units]);
            }

            return string.Join(" ", parts);
        }

        private static string GetReaderString(SqlDataReader reader, string columnName)
        {
            if (!ReaderHasColumn(reader, columnName))
            {
                return string.Empty;
            }

            object value = reader[columnName];

            return value == DBNull.Value ? string.Empty : Convert.ToString(value)?.Trim() ?? string.Empty;
        }

        private static int GetReaderInt(SqlDataReader reader, string columnName)
        {
            if (!ReaderHasColumn(reader, columnName) || reader[columnName] == DBNull.Value)
            {
                return 0;
            }

            return int.TryParse(Convert.ToString(reader[columnName]), out int result) ? result : 0;
        }

        private static decimal GetReaderDecimal(SqlDataReader reader, string columnName)
        {
            if (!ReaderHasColumn(reader, columnName) || reader[columnName] == DBNull.Value)
            {
                return 0m;
            }

            return decimal.TryParse(Convert.ToString(reader[columnName]), out decimal result) ? result : 0m;
        }

        private static DateTime? GetReaderDateTime(SqlDataReader reader, string columnName)
        {
            if (!ReaderHasColumn(reader, columnName) || reader[columnName] == DBNull.Value)
            {
                return null;
            }

            return DateTime.TryParse(Convert.ToString(reader[columnName]), out DateTime result) ? result : null;
        }

        // helper lấy chuỗi đầu tiên không rỗng

        private static bool ReaderHasColumn(SqlDataReader reader, string columnName)
        {
            for (int index = 0; index < reader.FieldCount; index++)
            {
                if (reader.GetName(index).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // =====================================================================================================
        //  Đọc kết quả Upload trước đó từ RawResponse trong nhật ký
        // =====================================================================================================

        public PreviousInvoiceResult? ParsePreviousInvoiceResult(string rawResponse)
        {
            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                return null;
            }

            try
            {
                using JsonDocument outerDocument = JsonDocument.Parse(rawResponse);

                JsonElement outerRoot = outerDocument.RootElement;

                /*
                 * Một số log có thể lưu toàn bộ JSON dưới dạng
                 * một chuỗi JSON đã escape.
                 */
                if (outerRoot.ValueKind == JsonValueKind.String)
                {
                    string innerRaw = outerRoot.GetString() ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(innerRaw))
                    {
                        return null;
                    }

                    return ParsePreviousInvoiceResult(innerRaw);
                }

                /*
                 * Cấu trúc phổ biến:
                 * {
                 *   "Success": 1,
                 *   "Code": 0,
                 *   "Message": "[{...}]"
                 * }
                 */
                if (TryGetJsonPropertyIgnoreCase(outerRoot, "Message", out JsonElement messageElement))
                {
                    string messageJson;

                    if (messageElement.ValueKind == JsonValueKind.String)
                    {
                        messageJson = messageElement.GetString() ?? string.Empty;
                    }
                    else
                    {
                        messageJson = messageElement.GetRawText();
                    }

                    if (string.IsNullOrWhiteSpace(messageJson))
                    {
                        return null;
                    }

                    return ParseInvoiceResultPayload(messageJson);
                }

                /*
                 * Trường hợp RawResponse đã là mảng kết quả
                 * hoặc là object hóa đơn trực tiếp.
                 */
                return ParseInvoiceResultElement(outerRoot);
            }
            catch (JsonException)
            {

                return ParsePreviousSoapInvoiceResult(rawResponse);

                return null;
            }
        }

        private static PreviousInvoiceResult? ParsePreviousSoapInvoiceResult(string rawResponse)
        {
            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                return null;
            }

            try
            {
                XDocument document = XDocument.Parse(rawResponse);

                XElement? resultElement = document
                    .Descendants()
                    .FirstOrDefault(element => element.Name.LocalName.EndsWith("Result", StringComparison.OrdinalIgnoreCase));

                string result = resultElement?.Value?.Trim() ?? string.Empty;

                if (!result.StartsWith("OK:", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                /*
                 * VNPT ImportInvByPattern:
                 * OK:1/002;C26MVH-260829
                 */
                Match match = Regex.Match(result, @"^OK:(?<pattern>[^;]+);(?<serial>[^-]+)-(?<invoiceNo>.+)$", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));

                if (!match.Success)
                {
                    return null;
                }

                string invoiceNo = match.Groups["invoiceNo"].Value.Trim();

                if (string.IsNullOrWhiteSpace(invoiceNo))
                {
                    return null;
                }

                return new PreviousInvoiceResult
                {
                    /*
                     * key XML đã gửi cho VNPT chính là TransID.
                     * Với response hiện tại, invoiceNo cũng là 260829.
                     */
                    Key = invoiceNo,

                    InvoiceNumber = invoiceNo,

                    InvoiceForm = match.Groups["pattern"].Value.Trim(),

                    InvoiceSerial = match.Groups["serial"].Value.Trim(),

                    KeySearch = invoiceNo
                };
            }
            catch (System.Xml.XmlException)
            {
                return null;
            }
            catch (RegexMatchTimeoutException)
            {
                return null;
            }
        }

        private PreviousInvoiceResult? ParseInvoiceResultPayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return null;
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(payload);

                return ParseInvoiceResultElement(document.RootElement);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private PreviousInvoiceResult? ParseInvoiceResultElement(JsonElement root)
        {
            JsonElement invoiceElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                if (root.GetArrayLength() == 0)
                {
                    return null;
                }

                invoiceElement = root[0];
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                invoiceElement = root;
            }
            else
            {
                return null;
            }

            string key = GetJsonValueAsString(invoiceElement, "key");

            string invoiceNumber = GetJsonValueAsString(invoiceElement, "invoiceNo");

            string invoiceForm = GetJsonValueAsString(invoiceElement, "pattern");

            string invoiceSerial = GetJsonValueAsString(invoiceElement, "serial");

            string invoiceDate = GetJsonValueAsString(invoiceElement, "invoiceDate");

            string verificationCode = GetJsonValueAsString(invoiceElement, "verificationCode");

            string keySearch = GetJsonValueAsString(invoiceElement, "keySearch");

            /*
             * InvoiceNumber là trường bắt buộc quan trọng nhất
             * đối với bước Publish.
             */
            if (string.IsNullOrWhiteSpace(invoiceNumber))
            {
                return null;
            }

            return new PreviousInvoiceResult
            {
                Key = key,
                InvoiceNumber = invoiceNumber,
                InvoiceForm = invoiceForm,
                InvoiceSerial = invoiceSerial,
                InvoiceDate = invoiceDate,
                VerificationCode = verificationCode,
                KeySearch = keySearch
            };
        }

        private static bool TryGetJsonPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement propertyValue)
        {
            propertyValue = default;

            if (element.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            foreach (
                JsonProperty property
                in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    propertyValue = property.Value;
                    return true;
                }
            }

            return false;
        }

        private static string GetJsonValueAsString(JsonElement element, string propertyName)
        {
            if (!TryGetJsonPropertyIgnoreCase(element, propertyName, out JsonElement value))
            {
                return string.Empty;
            }

            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString() ?? string.Empty,

                JsonValueKind.Number => value.ToString(),

                JsonValueKind.True => "true",

                JsonValueKind.False => "false",

                _ => string.Empty
            };
        }
    }
}
