using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using BaseBusiness.util;
using EInvoice.Dto;
using EInvoice.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace EInvoice.Services.Implements
{
    /// <summary>
    /// Mã hóa / giải mã bí mật (mật khẩu DB, mật khẩu/token của nhà cung cấp) bằng AES-256-CBC.
    /// Khóa và IV lấy từ cấu hình "Encryption:Key" (32 ký tự) và "Encryption:IV" (16 ký tự) trong appsettings.
    /// Giá trị đã mã hóa có tiền tố "enc:"; giá trị không có tiền tố được coi là dữ liệu cũ dạng chữ thường
    /// (Decrypt trả nguyên văn) để dữ liệu chuyển từ hệ thống cũ vẫn đọc được và sẽ được mã hóa ở lần lưu kế tiếp.
    /// Giai đoạn chạy hóa đơn dùng EInvoiceSecret.Decrypt để lấy lại giá trị thật.
    /// </summary>
    public static class EInvoiceSecret
    {
        private const string Prefix = "enc:";
        private static byte[]? _key;
        private static byte[]? _iv;

        public static void Configure(string key, string iv)
        {
            byte[] k = Encoding.UTF8.GetBytes(key);
            byte[] v = Encoding.UTF8.GetBytes(iv);
            if (k.Length != 32) throw new InvalidOperationException("Encryption:Key must be 32 bytes.");
            if (v.Length != 16) throw new InvalidOperationException("Encryption:IV must be 16 bytes.");
            _key = k;
            _iv = v;
        }

        public static bool IsEncrypted(string? value) =>
            !string.IsNullOrEmpty(value) && value.StartsWith(Prefix, StringComparison.Ordinal);

        public static string Encrypt(string plain)
        {
            if (string.IsNullOrEmpty(plain)) return plain;
            EnsureConfigured();
            using Aes aes = Aes.Create();
            aes.Key = _key!;
            aes.IV = _iv!;
            using ICryptoTransform enc = aes.CreateEncryptor();
            byte[] data = Encoding.UTF8.GetBytes(plain);
            return Prefix + Convert.ToBase64String(enc.TransformFinalBlock(data, 0, data.Length));
        }

        public static string Decrypt(string? stored)
        {
            if (string.IsNullOrEmpty(stored)) return string.Empty;
            if (!IsEncrypted(stored)) return stored; // dữ liệu cũ dạng chữ thường
            EnsureConfigured();
            using Aes aes = Aes.Create();
            aes.Key = _key!;
            aes.IV = _iv!;
            using ICryptoTransform dec = aes.CreateDecryptor();
            byte[] data = Convert.FromBase64String(stored.Substring(Prefix.Length));
            return Encoding.UTF8.GetString(dec.TransformFinalBlock(data, 0, data.Length));
        }

        private static void EnsureConfigured()
        {
            if (_key == null || _iv == null)
                throw new InvalidOperationException("Encryption key is not configured.");
        }
    }

    public class EInvoicePropertyService : IEInvoicePropertyService
    {
        private static readonly Regex SpName = new(@"^[A-Za-z0-9_\.\[\]]+$", RegexOptions.Compiled);

        public EInvoicePropertyService(IConfiguration configuration)
        {
            string key = configuration["Encryption:Key"] ?? throw new InvalidOperationException("Encryption:Key is missing in configuration.");
            string iv = configuration["Encryption:IV"] ?? throw new InvalidOperationException("Encryption:IV is missing in configuration.");
            EInvoiceSecret.Configure(key, iv);
        }

        // ---------------------------------------------------------------------------------------------
        //  Đọc dữ liệu
        // ---------------------------------------------------------------------------------------------

        public DataTable SearchProperties(int id = 0)
        {
            return DataTableHelper.getTableData("spEInvoice_PropertySearch", [new SqlParameter("@ID", id)]);
        }

        public DataTable GetProviderFields(int propertyId, int providerId)
        {
            DataTable fields = DataTableHelper.getTableData("spEInvoice_PropertyFieldValues",
                [new SqlParameter("@PropertyID", propertyId), new SqlParameter("@ProviderID", providerId)]);

            // Field mã hóa: không trả giá trị đã lưu ra giao diện, chỉ báo là đã có giá trị
            foreach (DataRow row in fields.Rows)
                if (Convert.ToBoolean(row["IsEncrypted"]))
                    row["SavedValue"] = DBNull.Value;
            return fields;
        }

        public DataTable GetProviderOptions(int providerId)
        {
            return DataTableHelper.getTableData("spEInvoice_OptionsByProvider", [new SqlParameter("@ProviderID", providerId)]);
        }

        // ---------------------------------------------------------------------------------------------
        //  Lưu
        // ---------------------------------------------------------------------------------------------

        public OpResult SaveProperty(PropertySaveRequest request, string user)
        {
            var errors = new List<object>();
            string code = (request.Code ?? "").Trim();
            string name = (request.Name ?? "").Trim();
            string sourceType = (request.SourceType ?? "").Trim().ToUpperInvariant();
            string ip = (request.DatabaseIP ?? "").Trim();
            string dbName = (request.DatabaseName ?? "").Trim();
            string dbUser = (request.DatabaseUser ?? "").Trim();
            string searchSp = (request.InvoiceSearchSP ?? "").Trim();
            string detailSp = (request.InvoiceDetailSP ?? "").Trim();

            Require(errors, "code", "Code", code, 50);
            Require(errors, "name", "Name", name, 100);
            Require(errors, "databaseIP", "Database IP", ip, 50);
            Require(errors, "databaseName", "Database Name", dbName, 50);
            Require(errors, "databaseUser", "Database User", dbUser, 50);

            if (sourceType != "PMS")
                errors.Add(new { field = "sourceType", message = "Only PMS source is supported at this time." });
            if ((request.Description ?? "").Trim().Length > 100)
                errors.Add(new { field = "description", message = "Description must be at most 100 characters." });
            if ((request.TaxCode ?? "").Trim().Length > 50)
                errors.Add(new { field = "taxCode", message = "Tax Code must be at most 50 characters." });

            string password = request.DatabasePassword ?? "";
            if (request.ID <= 0 && password.Length == 0)
                errors.Add(new { field = "databasePassword", message = "Database Password is required." });
            else if (password.Length > 100)
                errors.Add(new { field = "databasePassword", message = "Database Password must be at most 100 characters." });

            if (searchSp.Length == 0)
                errors.Add(new { field = "invoiceSearchSP", message = "Invoice Search SP is required." });
            else if (searchSp.Length > 200 || !SpName.IsMatch(searchSp))
                errors.Add(new { field = "invoiceSearchSP", message = "Invoice Search SP is not a valid stored procedure name." });
            if (detailSp.Length > 0 && (detailSp.Length > 200 || !SpName.IsMatch(detailSp)))
                errors.Add(new { field = "invoiceDetailSP", message = "Invoice Detail SP is not a valid stored procedure name." });

            if (request.ProviderID <= 0)
                errors.Add(new { field = "providerID", message = "Please select an e-invoice provider." });

            if (errors.Count > 0)
                return OpResult.Fail("Validation failed.", errors);

            // Kiểm tra và chuẩn hóa giá trị field cấu hình của Provider
            var valueErrors = new List<object>();
            string valuesXml = BuildValuesXml(request, valueErrors);
            if (valueErrors.Count > 0)
                return OpResult.Fail("Validation failed.", valueErrors);

            SqlParameter[] param =
            [
                new SqlParameter("@ID", request.ID),
                new SqlParameter("@Code", code),
                new SqlParameter("@Name", name),
                new SqlParameter("@Description", ToDb(request.Description)),
                new SqlParameter("@SourceType", sourceType),
                new SqlParameter("@TaxCode", ToDb(request.TaxCode)),
                new SqlParameter("@DatabaseIP", ip),
                new SqlParameter("@DatabaseName", dbName),
                new SqlParameter("@DatabaseUser", dbUser),
                // Rỗng khi sửa = giữ mật khẩu cũ (SP nhận NULL)
                new SqlParameter("@DatabasePassword", password.Length == 0 ? DBNull.Value : EInvoiceSecret.Encrypt(password)),
                new SqlParameter("@InvoiceSearchSP", searchSp),
                new SqlParameter("@InvoiceDetailSP", ToDb(detailSp)),
                new SqlParameter("@Inactive", request.Inactive),
                new SqlParameter("@ProviderID", request.ProviderID),
                new SqlParameter("@Values", SqlDbType.Xml) { Value = valuesXml },
                new SqlParameter("@User", user)
            ];

            try
            {
                DataTable result = DataTableHelper.getTableData("spEInvoice_PropertySave", param);
                int id = result.Rows.Count > 0 ? Convert.ToInt32(result.Rows[0]["ID"]) : request.ID;
                return OpResult.Ok(request.ID > 0 ? "Property E-Invoice updated successfully." : "Property E-Invoice created successfully.", new { id });
            }
            catch (Exception ex)
            {
                return OpResult.Fail(ex.Message);
            }
        }

        /// <summary>
        /// Kiểm tra từng field theo cấu hình của Provider (bắt buộc, độ dài, regex, kiểu số, lựa chọn của Select)
        /// và dựng XML giá trị gửi cho SP. Field bị ẩn do điều kiện VisibleWhen không bị bắt buộc.
        /// Field mã hóa được mã hóa tại đây; KeepExistingValue = true thì giữ nguyên giá trị đã lưu.
        /// </summary>
        private string BuildValuesXml(PropertySaveRequest request, List<object> errors)
        {
            DataTable fields = DataTableHelper.getTableData("spEInvoice_PropertyFieldValues",
                [new SqlParameter("@PropertyID", request.ID), new SqlParameter("@ProviderID", request.ProviderID)]);
            DataTable options = GetProviderOptions(request.ProviderID);

            // Giá trị người dùng gửi, theo FieldCode (để đánh giá điều kiện hiển thị)
            var byField = request.Values.GroupBy(v => v.ProviderFieldID).ToDictionary(g => g.Key, g => g.Last());
            var byCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow f in fields.Rows)
            {
                int fid = Convert.ToInt32(f["ProviderFieldID"]);
                if (byField.TryGetValue(fid, out var input))
                    byCode[Convert.ToString(f["FieldCode"])!] = (input.FieldValue ?? "").Trim();
            }

            var root = new XElement("values");
            var known = new HashSet<int>();

            foreach (DataRow f in fields.Rows)
            {
                int fid = Convert.ToInt32(f["ProviderFieldID"]);
                known.Add(fid);
                string code = Convert.ToString(f["FieldCode"]) ?? "";
                string display = Convert.ToString(f["DisplayName"]) ?? code;
                string control = Convert.ToString(f["ControlType"]) ?? "Text";
                string dataType = Convert.ToString(f["DataType"]) ?? "String";
                bool required = Convert.ToBoolean(f["IsRequired"]);
                bool encrypted = Convert.ToBoolean(f["IsEncrypted"]);
                bool hasSaved = Convert.ToBoolean(f["HasSavedValue"]);

                byField.TryGetValue(fid, out var input);
                string value = (input?.FieldValue ?? "").Trim();
                bool keep = input?.KeepExistingValue == true && hasSaved && value.Length == 0;

                string key = "values_" + fid; // tên field trên form để gắn lỗi
                bool visible = IsVisible(f, byCode);

                if (visible && required && value.Length == 0 && !keep)
                {
                    errors.Add(new { field = key, message = $"{display} is required." });
                    continue;
                }
                if (input == null && !keep)
                    continue; // không gửi giá trị thì không ghi

                if (value.Length > 0)
                {
                    if (f["MaxLength"] != DBNull.Value && value.Length > Convert.ToInt32(f["MaxLength"]))
                        errors.Add(new { field = key, message = $"{display} must be at most {Convert.ToInt32(f["MaxLength"])} characters." });

                    string regex = f["RegexPattern"] == DBNull.Value ? "" : Convert.ToString(f["RegexPattern"])!;
                    if (regex.Length > 0 && !SafeRegexMatch(regex, value))
                        errors.Add(new { field = key, message = $"{display} has an invalid format." });

                    if (control == "Number" || dataType == "Integer")
                    {
                        if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                            errors.Add(new { field = key, message = $"{display} must be an integer." });
                    }
                    else if (dataType == "Decimal")
                    {
                        if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                            errors.Add(new { field = key, message = $"{display} must be a number." });
                    }

                    if (control == "Select")
                    {
                        bool ok = options.AsEnumerable().Any(o => Convert.ToInt32(o["ProviderFieldID"]) == fid
                                                                  && string.Equals(Convert.ToString(o["OptionValue"]), value, StringComparison.Ordinal));
                        if (!ok)
                            errors.Add(new { field = key, message = $"{display}: the selected value is not a valid option." });
                    }
                    if (control == "Checkbox")
                        value = value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1" ? "true" : "false";
                }

                string stored = (encrypted && value.Length > 0) ? EInvoiceSecret.Encrypt(value) : value;
                var v = new XElement("v", new XAttribute("id", fid), new XAttribute("keep", keep ? 1 : 0));
                if (!keep && stored.Length > 0) v.Value = stored;
                root.Add(v);
            }

            // Giá trị gửi lên cho field không thuộc Provider (hoặc đã ngừng dùng)
            foreach (int id in byField.Keys.Where(id => !known.Contains(id)))
                errors.Add(new { field = "values_" + id, message = $"Configuration field {id} does not belong to the selected provider." });

            return root.ToString(SaveOptions.DisableFormatting);
        }

        /// <summary>Điều kiện hiển thị: EQUALS (bằng) hoặc IN (một trong danh sách phân cách bằng dấu phẩy).</summary>
        private static bool IsVisible(DataRow field, Dictionary<string, string> valuesByCode)
        {
            string whenCode = field["VisibleWhenFieldCode"] == DBNull.Value ? "" : Convert.ToString(field["VisibleWhenFieldCode"])!.Trim();
            if (whenCode.Length == 0) return true;

            string op = field["VisibleWhenOperator"] == DBNull.Value ? "" : Convert.ToString(field["VisibleWhenOperator"])!.Trim().ToUpperInvariant();
            string expected = field["VisibleWhenValue"] == DBNull.Value ? "" : Convert.ToString(field["VisibleWhenValue"])!.Trim();
            valuesByCode.TryGetValue(whenCode, out string? actual);
            actual = (actual ?? "").Trim();

            return op switch
            {
                "EQUALS" => string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
                "IN" => expected.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                                .Any(x => string.Equals(x, actual, StringComparison.OrdinalIgnoreCase)),
                _ => true
            };
        }

        private static bool SafeRegexMatch(string pattern, string value)
        {
            try { return Regex.IsMatch(value, pattern, RegexOptions.None, TimeSpan.FromMilliseconds(200)); }
            catch (ArgumentException) { return true; }          // pattern lỗi do cấu hình: không chặn người dùng
            catch (RegexMatchTimeoutException) { return false; }
        }

        // ---------------------------------------------------------------------------------------------
        //  Xóa
        // ---------------------------------------------------------------------------------------------

        public OpResult DeleteProperty(int id)
        {
            try
            {
                DataTableHelper.getTableData("spEInvoice_PropertyDelete", [new SqlParameter("@ID", id)]);
                return OpResult.Ok("Property E-Invoice deleted successfully.");
            }
            catch (Exception ex)
            {
                return OpResult.Fail(ex.Message);
            }
        }

        // ---------------------------------------------------------------------------------------------
        //  Kiểm tra kết nối
        // ---------------------------------------------------------------------------------------------

        public OpResult TestConnection(PropertyTestRequest request)
        {
            string ip = (request.DatabaseIP ?? "").Trim();
            string dbName = (request.DatabaseName ?? "").Trim().Replace("[", "").Replace("]", "");
            string dbUser = (request.DatabaseUser ?? "").Trim();
            string password = request.DatabasePassword ?? "";
            string searchSp = (request.InvoiceSearchSP ?? "").Trim();
            string detailSp = (request.InvoiceDetailSP ?? "").Trim();

            // Không nhập mật khẩu và đang sửa Property đã lưu: dùng mật khẩu đã lưu (giải mã ở server)
            if (password.Length == 0 && request.ID > 0)
            {
                DataTable saved = DataTableHelper.getTableData("spEInvoice_PropertyConnection", [new SqlParameter("@ID", request.ID)]);
                if (saved.Rows.Count > 0 && saved.Rows[0]["Database_Password"] != DBNull.Value)
                    password = EInvoiceSecret.Decrypt(Convert.ToString(saved.Rows[0]["Database_Password"]));
            }

            if (ip.Length == 0 || dbName.Length == 0 || dbUser.Length == 0 || password.Length == 0)
                return OpResult.Fail("Database IP, Name, User and Password are required to test the connection.");

            var builder = new SqlConnectionStringBuilder
            {
                DataSource = ip,
                InitialCatalog = dbName,
                UserID = dbUser,
                Password = password,
                TrustServerCertificate = true,
                ConnectTimeout = 8
            };

            try
            {
                using var conn = new SqlConnection(builder.ConnectionString);
                conn.Open();

                var warnings = new List<string>();
                foreach (string sp in new[] { searchSp, detailSp }.Where(s => s.Length > 0 && SpName.IsMatch(s)))
                {
                    using var cmd = new SqlCommand("SELECT OBJECT_ID(@n, N'P')", conn);
                    cmd.Parameters.AddWithValue("@n", sp);
                    if (cmd.ExecuteScalar() is null or DBNull)
                        warnings.Add($"Stored procedure '{sp}' was not found in database '{dbName}'.");
                }

                string message = warnings.Count == 0
                    ? $"Connected to database '{dbName}' on {ip} successfully."
                    : $"Connected to database '{dbName}' on {ip}, but: " + string.Join(" ", warnings);
                return OpResult.Ok(message, new { warnings });
            }
            catch (Exception ex)
            {
                return OpResult.Fail("Cannot connect: " + ex.Message);
            }
        }

        // ---------------------------------------------------------------------------------------------
        //  Mã hóa dữ liệu cũ
        // ---------------------------------------------------------------------------------------------

        public OpResult EncryptLegacySecrets(string user)
        {
            try
            {
                int passwords = 0, values = 0;

                // Mật khẩu DB của Property
                DataTable pw = DataTableHelper.ExecuteQuery(
                    "SELECT ID, Database_Password FROM dbo.Property_Einvoice WHERE ISNULL(Database_Password, N'') <> N'' AND Database_Password NOT LIKE N'enc:%'");
                foreach (DataRow row in pw.Rows)
                {
                    DataTableHelper.ExecuteNonQueryText(
                        "UPDATE dbo.Property_Einvoice SET Database_Password = @P, UpdatedBy = @U, UpdatedDate = GETDATE() WHERE ID = @ID",
                        [
                            new SqlParameter("@P", EInvoiceSecret.Encrypt(Convert.ToString(row["Database_Password"])!)),
                            new SqlParameter("@U", user),
                            new SqlParameter("@ID", Convert.ToInt32(row["ID"]))
                        ]);
                    passwords++;
                }

                // Giá trị của các field được đánh dấu Encrypted (mật khẩu, API key của nhà cung cấp)
                DataTable vals = DataTableHelper.ExecuteQuery(
                    @"SELECT v.ID, v.FieldValue
                      FROM dbo.Property_EInvoiceProviderValue v
                      INNER JOIN dbo.EInvoiceProviderField f ON f.ID = v.ProviderFieldID
                      WHERE f.IsEncrypted = 1 AND ISNULL(v.FieldValue, N'') <> N'' AND v.FieldValue NOT LIKE N'enc:%'");
                foreach (DataRow row in vals.Rows)
                {
                    DataTableHelper.ExecuteNonQueryText(
                        "UPDATE dbo.Property_EInvoiceProviderValue SET FieldValue = @P, UpdatedBy = @U, UpdatedDate = GETDATE() WHERE ID = @ID",
                        [
                            new SqlParameter("@P", EInvoiceSecret.Encrypt(Convert.ToString(row["FieldValue"])!)),
                            new SqlParameter("@U", user),
                            new SqlParameter("@ID", Convert.ToInt32(row["ID"]))
                        ]);
                    values++;
                }

                return OpResult.Ok($"Encrypted {passwords} database password(s) and {values} provider secret value(s).", new { passwords, values });
            }
            catch (Exception ex)
            {
                return OpResult.Fail(ex.Message);
            }
        }

        // ---------------------------------------------------------------------------------------------
        //  Hàm dùng chung
        // ---------------------------------------------------------------------------------------------

        private static void Require(List<object> errors, string field, string label, string value, int max)
        {
            if (value.Length == 0)
                errors.Add(new { field, message = $"{label} is required." });
            else if (value.Length > max)
                errors.Add(new { field, message = $"{label} must be at most {max} characters." });
        }

        private static object ToDb(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
        }
    }
}
