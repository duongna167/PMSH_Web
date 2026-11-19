using System.Data;
using System.Text.RegularExpressions;
using BaseBusiness.util;
using EInvoice.Dto;
using EInvoice.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace EInvoice.Services.Implements
{
    public class EInvoiceProviderService : IEInvoiceProviderService
    {
        // Danh sách giá trị hợp lệ (theo hướng dẫn cấu hình hóa đơn điện tử)
        private static readonly string[] DataTypes = ["String", "Integer", "Decimal", "Boolean", "Date", "DateTime"];
        private static readonly string[] ControlTypes = ["Text", "Password", "Select", "Checkbox", "Textarea", "Number", "Date"];
        private static readonly int[] ColumnWidths = [4, 6, 12];

        // ---------------------------------------------------------------------------------------------
        //  Provider
        // ---------------------------------------------------------------------------------------------

        public DataTable SearchProviders()
        {
            return DataTableHelper.getTableData("spEInvoice_ProviderSearch");
        }

        public OpResult SaveProvider(ProviderSaveRequest request, string user)
        {
            var errors = new List<object>();
            string code = (request.Code ?? "").Trim().ToUpperInvariant();
            string name = (request.Name ?? "").Trim();

            if (code.Length == 0)
                errors.Add(new { field = "code", message = "Provider Code is required." });
            else if (code.Length > 50)
                errors.Add(new { field = "code", message = "Provider Code must be at most 50 characters." });
            else if (code.Any(char.IsWhiteSpace))
                errors.Add(new { field = "code", message = "Provider Code must not contain spaces." });

            if (name.Length == 0)
                errors.Add(new { field = "name", message = "Provider Name is required." });
            else if (name.Length > 150)
                errors.Add(new { field = "name", message = "Provider Name must be at most 150 characters." });

            if ((request.Description ?? "").Length > 500)
                errors.Add(new { field = "description", message = "Description must be at most 500 characters." });
            if ((request.LogoPath ?? "").Length > 500)
                errors.Add(new { field = "logoPath", message = "Logo Path must be at most 500 characters." });

            if (errors.Count > 0)
                return OpResult.Fail("Validation failed.", errors);

            SqlParameter[] param =
            [
                new SqlParameter("@ID", request.ID),
                new SqlParameter("@Code", code),
                new SqlParameter("@Name", name),
                new SqlParameter("@Description", ToDb(request.Description)),
                new SqlParameter("@LogoPath", ToDb(request.LogoPath)),
                new SqlParameter("@Inactive", request.Inactive),
                new SqlParameter("@User", user)
            ];
            return Save("spEInvoice_ProviderSave", param, request.ID, "Provider");
        }

        // ---------------------------------------------------------------------------------------------
        //  Field
        // ---------------------------------------------------------------------------------------------

        public DataTable SearchFields(int providerId)
        {
            return DataTableHelper.getTableData("spEInvoice_FieldSearch", [new SqlParameter("@ProviderID", providerId)]);
        }

        public OpResult SaveField(ProviderFieldSaveRequest request, string user)
        {
            var errors = new List<object>();
            if (request.ProviderID <= 0)
                return OpResult.Fail("Invalid provider field data.");

            string fieldCode = (request.FieldCode ?? "").Trim().ToUpperInvariant();
            string displayName = (request.DisplayName ?? "").Trim();
            string dataType = (request.DataType ?? "").Trim();
            string controlType = (request.ControlType ?? "").Trim();

            if (fieldCode.Length == 0)
                errors.Add(new { field = "fieldCode", message = "Field Code is required." });
            else if (fieldCode.Length > 100)
                errors.Add(new { field = "fieldCode", message = "Field Code must be at most 100 characters." });
            else if (fieldCode.Any(char.IsWhiteSpace))
                errors.Add(new { field = "fieldCode", message = "Field Code must not contain spaces." });

            if (displayName.Length == 0)
                errors.Add(new { field = "displayName", message = "Display Name is required." });
            else if (displayName.Length > 200)
                errors.Add(new { field = "displayName", message = "Display Name must be at most 200 characters." });

            if (!DataTypes.Contains(dataType, StringComparer.OrdinalIgnoreCase))
                errors.Add(new { field = "dataType", message = "Data Type must be one of: " + string.Join(", ", DataTypes) + "." });
            if (!ControlTypes.Contains(controlType, StringComparer.OrdinalIgnoreCase))
                errors.Add(new { field = "controlType", message = "Control Type must be one of: " + string.Join(", ", ControlTypes) + "." });
            if (!ColumnWidths.Contains(request.ColumnWidth))
                errors.Add(new { field = "columnWidth", message = "Column Width must be 4, 6 or 12." });
            if (request.MaxLength is <= 0)
                errors.Add(new { field = "maxLength", message = "Max Length must be greater than 0." });

            // Field mật khẩu phải được đánh dấu mã hóa để không lộ giá trị ra giao diện
            if (string.Equals(controlType, "Password", StringComparison.OrdinalIgnoreCase) && !request.IsEncrypted)
                errors.Add(new { field = "isEncrypted", message = "Password field must be marked as Encrypted." });

            if (!string.IsNullOrWhiteSpace(request.RegexPattern))
            {
                try { _ = new Regex(request.RegexPattern); }
                catch (ArgumentException) { errors.Add(new { field = "regexPattern", message = "Regex Pattern is not valid." }); }
            }

            // Điều kiện hiển thị (VisibleWhen): hoặc để trống cả ba, hoặc nhập đủ; toán tử chỉ có EQUALS và IN
            string whenOp = (request.VisibleWhenOperator ?? "").Trim().ToUpperInvariant();
            bool hasWhen = !string.IsNullOrWhiteSpace(request.VisibleWhenFieldCode)
                           || whenOp.Length > 0 || !string.IsNullOrWhiteSpace(request.VisibleWhenValue);
            if (hasWhen)
            {
                if (string.IsNullOrWhiteSpace(request.VisibleWhenFieldCode))
                    errors.Add(new { field = "visibleWhenFieldCode", message = "Visible When Field is required when a visible condition is set." });
                if (whenOp != "EQUALS" && whenOp != "IN")
                    errors.Add(new { field = "visibleWhenOperator", message = "Visible When Operator must be EQUALS or IN." });
                if (string.IsNullOrWhiteSpace(request.VisibleWhenValue))
                    errors.Add(new { field = "visibleWhenValue", message = "Visible When Value is required when a visible condition is set." });
            }

            if ((request.Description ?? "").Length > 500)
                errors.Add(new { field = "description", message = "Description must be at most 500 characters." });
            if ((request.Placeholder ?? "").Length > 300)
                errors.Add(new { field = "placeholder", message = "Placeholder must be at most 300 characters." });
            if ((request.GroupName ?? "").Length > 150)
                errors.Add(new { field = "groupName", message = "Group Name must be at most 150 characters." });

            if (errors.Count > 0)
                return OpResult.Fail("Validation failed.", errors);

            // Chuẩn hóa về đúng chữ hoa/thường của danh sách hợp lệ
            dataType = DataTypes.First(x => x.Equals(dataType, StringComparison.OrdinalIgnoreCase));
            controlType = ControlTypes.First(x => x.Equals(controlType, StringComparison.OrdinalIgnoreCase));

            SqlParameter[] param =
            [
                new SqlParameter("@ID", request.ID),
                new SqlParameter("@ProviderID", request.ProviderID),
                new SqlParameter("@FieldCode", fieldCode),
                new SqlParameter("@DisplayName", displayName),
                new SqlParameter("@Description", ToDb(request.Description)),
                new SqlParameter("@DataType", dataType),
                new SqlParameter("@ControlType", controlType),
                new SqlParameter("@IsRequired", request.IsRequired),
                new SqlParameter("@IsEncrypted", request.IsEncrypted),
                new SqlParameter("@IsReadOnly", request.IsReadOnly),
                new SqlParameter("@DefaultValue", ToDb(request.DefaultValue)),
                new SqlParameter("@Placeholder", ToDb(request.Placeholder)),
                new SqlParameter("@MaxLength", request.MaxLength.HasValue ? request.MaxLength.Value : DBNull.Value),
                new SqlParameter("@RegexPattern", ToDb(request.RegexPattern)),
                new SqlParameter("@GroupName", ToDb(request.GroupName)),
                new SqlParameter("@DisplayOrder", request.DisplayOrder),
                new SqlParameter("@ColumnWidth", request.ColumnWidth),
                new SqlParameter("@Inactive", request.Inactive),
                new SqlParameter("@VisibleWhenFieldCode", ToDb(request.VisibleWhenFieldCode)),
                new SqlParameter("@VisibleWhenOperator", ToDb(hasWhen ? whenOp : null)),
                new SqlParameter("@VisibleWhenValue", ToDb(request.VisibleWhenValue)),
                new SqlParameter("@User", user)
            ];
            return Save("spEInvoice_FieldSave", param, request.ID, "Provider field");
        }

        // ---------------------------------------------------------------------------------------------
        //  Option
        // ---------------------------------------------------------------------------------------------

        public DataTable SearchOptions(int fieldId)
        {
            return DataTableHelper.getTableData("spEInvoice_OptionSearch", [new SqlParameter("@FieldID", fieldId)]);
        }

        public OpResult SaveOption(ProviderFieldOptionSaveRequest request, string user)
        {
            var errors = new List<object>();
            if (request.ProviderFieldID <= 0)
                return OpResult.Fail("Invalid field option data.");

            string value = (request.OptionValue ?? "").Trim();
            string text = (request.OptionText ?? "").Trim();

            if (value.Length == 0)
                errors.Add(new { field = "optionValue", message = "Option Value is required." });
            else if (value.Length > 150)
                errors.Add(new { field = "optionValue", message = "Option Value must be at most 150 characters." });

            if (text.Length == 0)
                errors.Add(new { field = "optionText", message = "Display Text is required." });
            else if (text.Length > 250)
                errors.Add(new { field = "optionText", message = "Display Text must be at most 250 characters." });

            if (errors.Count > 0)
                return OpResult.Fail("Validation failed.", errors);

            SqlParameter[] param =
            [
                new SqlParameter("@ID", request.ID),
                new SqlParameter("@ProviderFieldID", request.ProviderFieldID),
                new SqlParameter("@OptionValue", value),
                new SqlParameter("@OptionText", text),
                new SqlParameter("@IsDefault", request.IsDefault),
                new SqlParameter("@DisplayOrder", request.DisplayOrder),
                new SqlParameter("@Inactive", request.Inactive),
                new SqlParameter("@User", user)
            ];
            return Save("spEInvoice_OptionSave", param, request.ID, "Field option");
        }

        // ---------------------------------------------------------------------------------------------
        //  Operation (API của nhà cung cấp)
        // ---------------------------------------------------------------------------------------------

        private static readonly string[] HttpMethods = ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD"];
        private static readonly string[] BodyTypes = ["JSON", "FORM_URLENCODED", "MULTIPART_FORM_DATA", "NONE", "SOAP11", "SOAP12"];

        public DataTable SearchOperations(int providerId)
        {
            return DataTableHelper.getTableData("spEInvoice_OperationSearch", [new SqlParameter("@ProviderID", providerId)]);
        }

        public OpResult SaveOperation(OperationSaveRequest request, string user)
        {
            if (request.ProviderID <= 0)
                return OpResult.Fail("Please select a provider.");

            var errors = new List<object>();
            string code = (request.OperationCode ?? "").Trim();
            string name = (request.OperationName ?? "").Trim();
            string endpoint = (request.EndpointPath ?? "").Trim();
            string method = (request.HttpMethod ?? "").Trim().ToUpperInvariant();
            string contentType = string.IsNullOrWhiteSpace(request.ContentType) ? "application/json" : request.ContentType.Trim();
            string? bodyType = string.IsNullOrWhiteSpace(request.BodyType) ? "JSON" : request.BodyType.Trim().ToUpperInvariant();
            string? action = string.IsNullOrWhiteSpace(request.BusinessActionCode) ? null : request.BusinessActionCode.Trim().ToUpperInvariant();
            string? success = string.IsNullOrWhiteSpace(request.SuccessStatusCodes) ? null : request.SuccessStatusCodes.Replace(" ", "");

            if (code.Length == 0)
                errors.Add(new { field = "operationCode", message = "Operation Code is required." });
            else if (code.Length > 50)
                errors.Add(new { field = "operationCode", message = "Operation Code must be at most 50 characters." });

            if (name.Length == 0)
                errors.Add(new { field = "operationName", message = "Operation Name is required." });
            else if (name.Length > 150)
                errors.Add(new { field = "operationName", message = "Operation Name must be at most 150 characters." });

            if (endpoint.Length == 0)
                errors.Add(new { field = "endpointPath", message = "Endpoint Path is required." });
            else if (endpoint.Length > 500)
                errors.Add(new { field = "endpointPath", message = "Endpoint Path must be at most 500 characters." });

            if (!HttpMethods.Contains(method))
                errors.Add(new { field = "httpMethod", message = "HTTP Method must be one of: " + string.Join(", ", HttpMethods) + "." });
            if (!BodyTypes.Contains(bodyType!))
                errors.Add(new { field = "bodyType", message = "Body Type must be one of: " + string.Join(", ", BodyTypes) + "." });
            if (request.TimeoutSeconds <= 0 || request.TimeoutSeconds > 600)
                errors.Add(new { field = "timeoutSeconds", message = "Timeout must be between 1 and 600 seconds." });
            if (contentType.Length > 100)
                errors.Add(new { field = "contentType", message = "Content Type must be at most 100 characters." });

            if (action != null && (action.Length > 50 || !Regex.IsMatch(action, "^[A-Z0-9_]+$")))
                errors.Add(new { field = "businessActionCode", message = "Business Action Code must contain only A-Z, 0-9 and underscore (max 50)." });

            if (success != null && !Regex.IsMatch(success, @"^\d{3}(,\d{3})*$"))
                errors.Add(new { field = "successStatusCodes", message = "Success Status Codes must be a comma-separated list of HTTP codes, e.g. 200,201." });

            // Bật xác thực mà chưa chọn Operation đăng nhập vẫn hợp lệ (API dùng khóa cố định trong header);
            // ngược lại, đã chọn Operation đăng nhập thì phải bật xác thực.
            if (!request.IsAuthenticationRequired && request.AuthenticationOperationID > 0)
                errors.Add(new { field = "authenticationOperationID", message = "Turn on Authentication Required or clear the Authentication Operation." });

            if ((request.Description ?? "").Length > 500)
                errors.Add(new { field = "description", message = "Description must be at most 500 characters." });
            if ((request.ExternalMethodCode ?? "").Length > 50)
                errors.Add(new { field = "externalMethodCode", message = "External Method Code must be at most 50 characters." });
            if ((request.ActionCode ?? "").Length > 50)
                errors.Add(new { field = "actionCode", message = "Action Code must be at most 50 characters." });

            if (errors.Count > 0)
                return OpResult.Fail("Validation failed.", errors);

            SqlParameter[] param =
            [
                new SqlParameter("@ID", request.ID),
                new SqlParameter("@ProviderID", request.ProviderID),
                new SqlParameter("@OperationCode", code),
                new SqlParameter("@OperationName", name),
                new SqlParameter("@BusinessActionCode", ToDb(action)),
                new SqlParameter("@HttpMethod", method),
                new SqlParameter("@EndpointPath", endpoint),
                new SqlParameter("@ContentType", contentType),
                new SqlParameter("@BodyType", ToDb(bodyType)),
                new SqlParameter("@TimeoutSeconds", request.TimeoutSeconds),
                new SqlParameter("@IsAuthenticationRequired", request.IsAuthenticationRequired),
                new SqlParameter("@AuthenticationOperationID", request.AuthenticationOperationID > 0 ? request.AuthenticationOperationID : DBNull.Value),
                new SqlParameter("@ExternalMethodCode", ToDb(request.ExternalMethodCode)),
                new SqlParameter("@ActionCode", ToDb(request.ActionCode)),
                new SqlParameter("@SuccessStatusCodes", ToDb(success)),
                new SqlParameter("@Description", ToDb(request.Description)),
                new SqlParameter("@DisplayOrder", request.DisplayOrder),
                new SqlParameter("@Inactive", request.Inactive),
                new SqlParameter("@User", user)
            ];
            return Save("spEInvoice_OperationSave", param, request.ID, "Operation");
        }

        // ---------------------------------------------------------------------------------------------
        //  Mapping (tham số request / response của Operation) và Response Rule
        // ---------------------------------------------------------------------------------------------

        private static readonly string[] MappingLocations = ["BODY", "HEADER", "QUERY", "PATH", "FORM", "RESPONSE"];
        private static readonly string[] MappingSourceTypes =
            ["COLLECTION", "CONFIG", "CONSTANT", "CUSTOM", "DOCUMENT", "INVOICE", "INVOICEDETAIL", "PROPERTY", "RESPONSE", "SESSION", "STATIC", "SYSTEM"];
        private static readonly string[] MappingDataTypes =
            ["STRING", "INT", "INTEGER", "DECIMAL", "BOOL", "BOOLEAN", "DATETIME", "DATE", "ARRAY", "OBJECT", "UNIX_MILLISECONDS"];
        private static readonly string[] ConditionOperators =
            ["=", "==", "!=", ">", ">=", "<", "<=", "CONTAINS", "STARTSWITH", "ENDSWITH", "ISNULL", "ISNOTNULL"];

        public DataTable SearchMappings(int operationId)
        {
            return DataTableHelper.getTableData("spEInvoice_MappingSearch", [new SqlParameter("@OperationID", operationId)]);
        }

        public DataTable SearchRules(int operationId)
        {
            return DataTableHelper.getTableData("spEInvoice_RuleSearch", [new SqlParameter("@OperationID", operationId)]);
        }

        public OpResult SaveMapping(MappingSaveRequest request, string user)
        {
            if (request.OperationID <= 0)
                return OpResult.Fail("Operation is required.");

            var errors = new List<object>();
            string location = (request.MappingLocation ?? "").Trim().ToUpperInvariant();
            string target = (request.TargetPath ?? "").Trim();
            string sourceType = (request.SourceType ?? "").Trim().ToUpperInvariant();
            string dataType = string.IsNullOrWhiteSpace(request.DataType) ? "STRING" : request.DataType.Trim().ToUpperInvariant();
            string? condOp = string.IsNullOrWhiteSpace(request.ConditionOperator) ? null : request.ConditionOperator.Trim().ToUpperInvariant();
            string? condType = string.IsNullOrWhiteSpace(request.ConditionSourceType) ? null : request.ConditionSourceType.Trim().ToUpperInvariant();

            if (!MappingLocations.Contains(location))
                errors.Add(new { field = "mappingLocation", message = "Mapping Location must be one of: " + string.Join(", ", MappingLocations) + "." });

            if (target.Length == 0)
                errors.Add(new { field = "targetPath", message = "Target Path is required." });
            else if (target.Length > 500)
                errors.Add(new { field = "targetPath", message = "Target Path must be at most 500 characters." });

            if (!MappingSourceTypes.Contains(sourceType))
                errors.Add(new { field = "sourceType", message = "Source Type must be one of: " + string.Join(", ", MappingSourceTypes) + "." });
            if (!MappingDataTypes.Contains(dataType))
                errors.Add(new { field = "dataType", message = "Data Type must be one of: " + string.Join(", ", MappingDataTypes) + "." });

            if (string.IsNullOrWhiteSpace(request.SourceValue) && string.IsNullOrWhiteSpace(request.DefaultValue))
                errors.Add(new { field = "sourceValue", message = "Source Value or Default Value is required." });

            if (request.DisplayOrder < 0)
                errors.Add(new { field = "displayOrder", message = "Display Order cannot be negative." });

            if (condOp != null && !ConditionOperators.Contains(condOp))
                errors.Add(new { field = "conditionOperator", message = "Condition Operator is invalid." });
            if (condOp != null && condType == null)
                errors.Add(new { field = "conditionSourceType", message = "Condition Source Type is required when a Condition Operator is set." });
            if (condType != null && !MappingSourceTypes.Contains(condType))
                errors.Add(new { field = "conditionSourceType", message = "Condition Source Type is invalid." });

            if ((request.FormatString ?? "").Length > 100)
                errors.Add(new { field = "formatString", message = "Format String must be at most 100 characters." });
            if ((request.PrefixValue ?? "").Length > 200)
                errors.Add(new { field = "prefixValue", message = "Prefix must be at most 200 characters." });
            if ((request.SuffixValue ?? "").Length > 200)
                errors.Add(new { field = "suffixValue", message = "Suffix must be at most 200 characters." });
            if ((request.ConditionSourceValue ?? "").Length > 500)
                errors.Add(new { field = "conditionSourceValue", message = "Condition Source Value must be at most 500 characters." });
            if ((request.ConditionExpectedValue ?? "").Length > 500)
                errors.Add(new { field = "conditionExpectedValue", message = "Condition Expected Value must be at most 500 characters." });

            if (errors.Count > 0)
                return OpResult.Fail("Validation failed.", errors);

            // Chỉ cắt khoảng trắng đầu/cuối ở giá trị nguồn; giữ nguyên nội dung bên trong (template, JSON, XML)
            SqlParameter[] param =
            [
                new SqlParameter("@ID", request.ID),
                new SqlParameter("@OperationID", request.OperationID),
                new SqlParameter("@MappingLocation", location),
                new SqlParameter("@TargetPath", target),
                new SqlParameter("@SourceType", sourceType),
                new SqlParameter("@SourceValue", ToDb(request.SourceValue)),
                new SqlParameter("@DataType", dataType),
                new SqlParameter("@IsRequired", request.IsRequired),
                new SqlParameter("@DefaultValue", ToDb(request.DefaultValue)),
                new SqlParameter("@FormatString", ToDb(request.FormatString)),
                new SqlParameter("@OmitWhenNull", request.OmitWhenNull),
                new SqlParameter("@PrefixValue", ToDb(request.PrefixValue)),
                new SqlParameter("@SuffixValue", ToDb(request.SuffixValue)),
                new SqlParameter("@DisplayOrder", request.DisplayOrder),
                new SqlParameter("@Inactive", request.Inactive),
                new SqlParameter("@ConditionSourceType", ToDb(condType)),
                new SqlParameter("@ConditionSourceValue", ToDb(request.ConditionSourceValue)),
                new SqlParameter("@ConditionOperator", ToDb(condOp)),
                new SqlParameter("@ConditionExpectedValue", ToDb(request.ConditionExpectedValue)),
                new SqlParameter("@User", user)
            ];
            return Save("spEInvoice_MappingSave", param, request.ID, "Mapping");
        }

        // ---------------------------------------------------------------------------------------------
        //  Hàm dùng chung
        // ---------------------------------------------------------------------------------------------

        /// <summary>Gọi SP lưu; SP báo lỗi nghiệp vụ bằng RAISERROR nên message được trả nguyên về giao diện.</summary>
        private static OpResult Save(string procedure, SqlParameter[] param, int id, string label)
        {
            try
            {
                DataTable result = DataTableHelper.getTableData(procedure, param);
                int newId = result.Rows.Count > 0 ? Convert.ToInt32(result.Rows[0]["ID"]) : id;
                string message = id > 0 ? $"{label} updated successfully." : $"{label} created successfully.";
                return OpResult.Ok(message, new { id = newId });
            }
            catch (Exception ex)
            {
                return OpResult.Fail(ex.Message);
            }
        }

        private static object ToDb(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
        }
    }
}
