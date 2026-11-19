using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace EInvoice.Runtime
{
    /*
     * Dựng request gửi nhà cung cấp từ cấu hình Mapping: Body/Header/Query/Path/Form, Document (XML/SOAP) và Collection.
     * Gộp từ DynamicProviderDocumentBuilder + DynamicProviderRequestBuilder của PMSH_Web, giữ nguyên logic.
     */
    public class DynamicProviderDocumentBuilder
    {
        private readonly IConfiguration _configuration;

        public DynamicProviderDocumentBuilder(
            IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private sealed class XmlBuildUnit
        {
            public int DisplayOrder { get; set; }

            public int TieBreakerID { get; set; }

            public ProviderDocumentFieldRuntimeModel?
                NormalField
            { get; set; }

            public IReadOnlyList<ProviderDocumentFieldRuntimeModel>?
                CollectionFields
            { get; set; }
        }

        public string Build(int providerId, string documentCode, ProviderRequestContext context)
        {
            if (providerId <= 0)
            {
                throw new ProviderBuildException("ProviderID không hợp lệ khi build document.");
            }

            if (string.IsNullOrWhiteSpace(documentCode))
            {
                throw new ProviderBuildException("DocumentCode không được để trống.");
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            List<ProviderDocumentFieldRuntimeModel> fields = GetDocumentFields(providerId, documentCode);

            if (fields.Count == 0)
            {
                throw new ProviderBuildException($"Không tìm thấy Document Definition " + $"'{documentCode}' " + $"cho ProviderID {providerId}.");
            }

            string documentType = fields[0].DocumentType?.Trim().ToUpperInvariant() ?? string.Empty;

            return documentType switch
            {
                "XML" => BuildXml(fields, context),

                _ => throw new ProviderBuildException($"DocumentType " + $"'{documentType}' " + "chưa được hỗ trợ.")
            };
        }

        private List<ProviderDocumentFieldRuntimeModel> GetDocumentFields(int providerId, string documentCode)
        {
            const string sql = @"
                SELECT
                    ID,
                    ProviderID,
                    OperationCode,
                    JsonPath,
                    SourceType,
                    SourceValue,
                    DataType,
                    IsRequired,
                    DefaultValue,
                    FormatString,
                    OmitWhenNull,
                    DisplayOrder,
                    DocumentType,
                    CollectionName,
                    ItemPath,
                    RootName
                FROM dbo.EInvoiceProviderBodyField
                WHERE ProviderID = @ProviderID
                  AND UPPER(LTRIM(RTRIM(OperationCode)))
                      = UPPER(LTRIM(RTRIM(@DocumentCode)))
                  AND Inactive = 0
                ORDER BY DisplayOrder, ID;
            ";

            var result = new List<ProviderDocumentFieldRuntimeModel>();

            using SqlConnection connection = CreateConnection();

            connection.Open();

            using SqlCommand command = new SqlCommand(sql, connection);

            command.Parameters.Add("@ProviderID", SqlDbType.Int).Value = providerId;

            command.Parameters.Add("@DocumentCode", SqlDbType.NVarChar, 100).Value = documentCode.Trim();

            using SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                result.Add(
                    new ProviderDocumentFieldRuntimeModel
                    {
                        ID = Convert.ToInt32(reader["ID"]),

                        ProviderID = Convert.ToInt32(reader["ProviderID"]),

                        DocumentCode = Convert.ToString(reader["OperationCode"])?.Trim() ?? string.Empty,

                        TargetPath = Convert.ToString(reader["JsonPath"])?.Trim() ?? string.Empty,

                        SourceType = Convert.ToString(reader["SourceType"])?.Trim().ToUpperInvariant() ?? string.Empty,

                        SourceValue = Convert.ToString(reader["SourceValue"])?.Trim() ?? string.Empty,

                        DataType = Convert.ToString(reader["DataType"])?.Trim().ToUpperInvariant() ?? "STRING",

                        IsRequired = reader["IsRequired"] != DBNull.Value && Convert.ToBoolean(reader["IsRequired"]),

                        DefaultValue = reader["DefaultValue"] == DBNull.Value ? null : Convert.ToString(reader["DefaultValue"]),

                        FormatString = reader["FormatString"] == DBNull.Value ? null : Convert.ToString(reader["FormatString"]),

                        OmitWhenNull = reader["OmitWhenNull"] != DBNull.Value && Convert.ToBoolean(reader["OmitWhenNull"]),

                        DisplayOrder = reader["DisplayOrder"] == DBNull.Value ? 0 : Convert.ToInt32(reader["DisplayOrder"]),

                        DocumentType = Convert.ToString(reader["DocumentType"])?.Trim().ToUpperInvariant() ?? string.Empty,

                        CollectionName = reader["CollectionName"] == DBNull.Value ? null : Convert.ToString(reader["CollectionName"])?.Trim(),

                        ItemPath = reader["ItemPath"] == DBNull.Value ? null : Convert.ToString(reader["ItemPath"])?.Trim(),

                        RootName = Convert.ToString(reader["RootName"])?.Trim() ?? string.Empty
                    }
                );
            }

            return result;
        }

        private SqlConnection CreateConnection()
        {
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ProviderBuildException("Không tìm thấy DefaultConnection " + "để load Provider Document.");
            }

            return new SqlConnection(connectionString);
        }

        private string BuildXml(
    IReadOnlyList<ProviderDocumentFieldRuntimeModel> fields,
    ProviderRequestContext context)
        {
            if (fields == null || fields.Count == 0)
            {
                throw new ProviderBuildException(
                    "XML Document không có field.");
            }

            string rootName =
                fields
                    .Select(x => x.RootName)
                    .FirstOrDefault(
                        x => !string.IsNullOrWhiteSpace(x))
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(rootName))
            {
                throw new ProviderBuildException(
                    "XML RootName chưa được cấu hình.");
            }

            XElement root =
                new XElement(rootName);


            /*
             * ============================================================
             * GLOBAL XML BUILD ORDER
             * ============================================================
             *
             * Không còn:
             *
             *     build normal trước
             *     build collection sau
             *
             * vì cách đó phá DisplayOrder toàn document.
             *
             * Mỗi NORMAL field là một build unit.
             *
             * Mỗi COLLECTION + ItemPath là một build unit,
             * lấy DisplayOrder nhỏ nhất của group làm vị trí của collection.
             *
             * Sau đó toàn bộ build units được sort chung.
             * ============================================================
             */


            /*
             * ------------------------------------------------------------
             * 1. NORMAL FIELD UNITS
             * ------------------------------------------------------------
             */
            var normalUnits =
                fields
                    .Where(x =>
                        !string.Equals(
                            x.SourceType?.Trim(),
                            "COLLECTION_ITEM",
                            StringComparison.OrdinalIgnoreCase))
                    .Select(x => new XmlBuildUnit
                    {
                        DisplayOrder = x.DisplayOrder,

                        TieBreakerID = x.ID,

                        NormalField = x,

                        CollectionFields = null
                    })
                    .ToList();


            /*
             * ------------------------------------------------------------
             * 2. COLLECTION UNITS
             * ------------------------------------------------------------
             *
             * Ví dụ:
             *
             * Items
             * + HDon.DLHDon.NDHDon.DSHHDVu.HHDVu
             *
             * trở thành 1 build unit.
             *
             * TaxBreakdowns
             * + HDon.DLHDon.NDHDon.TToan.THTTLTSuat.LTSuat
             *
             * trở thành build unit khác.
             */
            var collectionUnits =
                fields
                    .Where(x =>
                        string.Equals(
                            x.SourceType?.Trim(),
                            "COLLECTION_ITEM",
                            StringComparison.OrdinalIgnoreCase))
                    .GroupBy(x => new
                    {
                        CollectionName =
                            x.CollectionName?.Trim()
                            ?? string.Empty,

                        ItemPath =
                            x.ItemPath?.Trim()
                            ?? string.Empty
                    })
                    .Select(group =>
                    {
                        List<ProviderDocumentFieldRuntimeModel>
                            groupFields =
                                group
                                    .OrderBy(x => x.DisplayOrder)
                                    .ThenBy(x => x.ID)
                                    .ToList();

                        return new XmlBuildUnit
                        {
                            DisplayOrder =
                                groupFields.Min(x => x.DisplayOrder),

                            TieBreakerID =
                                groupFields.Min(x => x.ID),

                            NormalField = null,

                            CollectionFields =
                                groupFields
                        };
                    })
                    .ToList();


            /*
             * ------------------------------------------------------------
             * 3. MERGE + GLOBAL SORT
             * ------------------------------------------------------------
             */
            List<XmlBuildUnit> buildUnits =
                normalUnits
                    .Concat(collectionUnits)
                    .OrderBy(x => x.DisplayOrder)
                    .ThenBy(x => x.TieBreakerID)
                    .ToList();


            /*
             * ------------------------------------------------------------
             * 4. BUILD DOCUMENT THEO GLOBAL ORDER
             * ------------------------------------------------------------
             */
            foreach (XmlBuildUnit unit in buildUnits)
            {
                /*
                 * =============================================
                 * NORMAL FIELD
                 * =============================================
                 */
                if (unit.NormalField != null)
                {
                    ProviderDocumentFieldRuntimeModel field =
                        unit.NormalField;

                    object? rawValue =
                        ResolveSourceValue(
                            field,
                            context);

                    if (IsEmptyValue(rawValue))
                    {
                        rawValue =
                            field.DefaultValue;
                    }

                    if (IsEmptyValue(rawValue))
                    {
                        if (field.IsRequired)
                        {
                            throw new ProviderBuildException(
                                $"Document field "
                                + $"'{field.TargetPath}' "
                                + "là Required nhưng "
                                + "không lấy được giá trị từ "
                                + $"{field.SourceType}:"
                                + $"{field.SourceValue}.");
                        }

                        if (field.OmitWhenNull)
                        {
                            continue;
                        }
                    }

                    object? convertedValue =
                        ConvertDocumentValue(
                            rawValue,
                            field.DataType,
                            field.FormatString);

                    SetXmlValue(
                        root,
                        field.TargetPath,
                        convertedValue);

                    continue;
                }


                /*
                 * =============================================
                 * COLLECTION GROUP
                 * =============================================
                 */
                if (unit.CollectionFields != null &&
                    unit.CollectionFields.Count > 0)
                {
                    BuildXmlCollectionGroup(
                        root,
                        unit.CollectionFields,
                        context);
                }
            }


            /*
             * ============================================================
             * RETURN
             * ============================================================
             */
            return root.ToString(
                SaveOptions.DisableFormatting);
        }

        private void BuildXmlCollectionGroup(
    XElement root,
    IReadOnlyList<ProviderDocumentFieldRuntimeModel> fields,
    ProviderRequestContext context)
        {
            if (fields == null || fields.Count == 0)
            {
                return;
            }

            ProviderDocumentFieldRuntimeModel firstField =
                fields[0];

            string collectionName =
                firstField.CollectionName?.Trim()
                ?? string.Empty;

            string itemPath =
                firstField.ItemPath?.Trim()
                ?? string.Empty;


            /*
             * ============================================================
             * VALIDATE COLLECTION CONFIG
             * ============================================================
             */
            if (string.IsNullOrWhiteSpace(
                collectionName))
            {
                throw new ProviderBuildException(
                    "CollectionName chưa được cấu hình.");
            }

            if (string.IsNullOrWhiteSpace(
                itemPath))
            {
                throw new ProviderBuildException(
                    $"ItemPath của collection "
                    + $"'{collectionName}' "
                    + "chưa được cấu hình.");
            }


            /*
             * ============================================================
             * GET COLLECTION FROM CONTEXT
             * ============================================================
             */
            object? collectionObject =
                GetCollection(
                    context,
                    collectionName);

            if (collectionObject == null)
            {
                throw new ProviderBuildException(
                    $"Không tìm thấy collection "
                    + $"'{collectionName}'.");
            }

            if (collectionObject
                is not System.Collections.IEnumerable enumerable)
            {
                throw new ProviderBuildException(
                    $"Collection "
                    + $"'{collectionName}' "
                    + "không phải IEnumerable.");
            }


            /*
             * ============================================================
             * BUILD ITEMS
             * ============================================================
             */
            foreach (object? item in enumerable)
            {
                if (item == null)
                {
                    continue;
                }

                XElement itemElement =
                    CreateXmlItemElement(
                        root,
                        itemPath);

                foreach (
                    ProviderDocumentFieldRuntimeModel field
                    in fields
                        .OrderBy(x => x.DisplayOrder)
                        .ThenBy(x => x.ID))
                {
                    object? rawValue =
                        ResolveObjectProperty(
                            item,
                            field.SourceValue);

                    if (IsEmptyValue(rawValue))
                    {
                        rawValue =
                            field.DefaultValue;
                    }

                    if (IsEmptyValue(rawValue))
                    {
                        if (field.IsRequired)
                        {
                            throw new ProviderBuildException(
                                $"Collection field "
                                + $"'{field.TargetPath}' "
                                + "là Required nhưng "
                                + "không lấy được "
                                + $"{collectionName}:"
                                + $"{field.SourceValue}.");
                        }

                        if (field.OmitWhenNull)
                        {
                            continue;
                        }
                    }

                    object? converted =
                        ConvertDocumentValue(
                            rawValue,
                            field.DataType,
                            field.FormatString);

                    SetXmlValue(
                        itemElement,
                        field.TargetPath,
                        converted);
                }
            }
        }

        //private void BuildXmlCollections(XElement root, IReadOnlyList<ProviderDocumentFieldRuntimeModel> fields, ProviderRequestContext context)
        //{
        //    if (fields == null || fields.Count == 0)
        //    {
        //        return;
        //    }

        //    var groups = fields.GroupBy(x => new
        //    {
        //        CollectionName = x.CollectionName?.Trim() ?? string.Empty,

        //        ItemPath = x.ItemPath?.Trim() ?? string.Empty
        //    }
        //        );

        //    foreach (var group in groups)
        //    {
        //        string collectionName = group.Key.CollectionName;

        //        string itemPath = group.Key.ItemPath;

        //        if (string.IsNullOrWhiteSpace(collectionName))
        //        {
        //            throw new ProviderBuildException("CollectionName chưa được cấu hình.");
        //        }

        //        if (string.IsNullOrWhiteSpace(itemPath))
        //        {
        //            throw new ProviderBuildException($"ItemPath của collection " + $"'{collectionName}' " + "chưa được cấu hình.");
        //        }

        //        object? collectionObject = GetCollection(context, collectionName);

        //        if (collectionObject == null)
        //        {
        //            throw new ProviderBuildException($"Không tìm thấy collection " + $"'{collectionName}'.");
        //        }

        //        if (collectionObject
        //            is not System.Collections.IEnumerable enumerable)
        //        {
        //            throw new ProviderBuildException($"Collection " + $"'{collectionName}' " + "không phải IEnumerable.");
        //        }

        //        foreach (object? item in enumerable)
        //        {
        //            if (item == null)
        //            {
        //                continue;
        //            }

        //            XElement itemElement = CreateXmlItemElement(root, itemPath);

        //            foreach (ProviderDocumentFieldRuntimeModel field in group.OrderBy(x => x.DisplayOrder))
        //            {
        //                object? rawValue = ResolveObjectProperty(item, field.SourceValue);

        //                if (IsEmptyValue(rawValue))
        //                {
        //                    rawValue = field.DefaultValue;
        //                }

        //                if (IsEmptyValue(rawValue))
        //                {
        //                    if (field.IsRequired)
        //                    {
        //                        throw new ProviderBuildException($"Collection field " + $"'{field.TargetPath}' " + $"là Required nhưng " + $"không lấy được " + $"{collectionName}:" + $"{field.SourceValue}.");
        //                    }

        //                    if (field.OmitWhenNull)
        //                    {
        //                        continue;
        //                    }
        //                }

        //                object? converted = ConvertDocumentValue(rawValue, field.DataType, field.FormatString);

        //                SetXmlValue(itemElement, field.TargetPath, converted);
        //            }
        //        }
        //    }
        //}

        private static object? GetCollection(ProviderRequestContext context, string collectionName)
        {
            if (context.Collections == null)
            {
                return null;
            }

            return context.Collections.TryGetValue(collectionName, out object? value) ? value : null;
        }

        private static XElement CreateXmlItemElement(XElement root, string itemPath)
        {
            string[] parts = itemPath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length == 0)
            {
                throw new ProviderBuildException("Collection ItemPath không hợp lệ.");
            }

            XElement current = root;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                string nodeName = parts[i];

                XElement? child = current.Element(nodeName);

                if (child == null)
                {
                    child = new XElement(nodeName);

                    current.Add(child);
                }

                current = child;
            }

            string itemNodeName = parts[^1];

            XElement itemElement = new XElement(itemNodeName);

            current.Add(itemElement);

            return itemElement;
        }

        private static object? ResolveSourceValue(ProviderDocumentFieldRuntimeModel field, ProviderRequestContext context)
        {
            string sourceType = field.SourceType?.Trim().ToUpperInvariant() ?? string.Empty;

            string sourceValue = field.SourceValue?.Trim() ?? string.Empty;

            return sourceType switch
            {
                "INVOICE" => ResolveObjectProperty(context.Invoice, sourceValue),

                "CONFIG" => ResolveDictionaryValue(context.Config, sourceValue),

                "SYSTEM" => ResolveDictionaryValue(context.System, sourceValue),

                "SESSION" => ResolveDictionaryValue(context.Session, sourceValue),

                "STATIC" => sourceValue,

                _ => throw new ProviderBuildException($"Document SourceType " + $"'{sourceType}' " + "không được hỗ trợ.")
            };
        }

        private static object? ResolveObjectProperty(object? source, string propertyName)
        {
            if (source == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return null;
            }

            PropertyInfo? property = source.GetType()
                                        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                        .FirstOrDefault(x => x.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));

            if (property == null)
            {
                throw new ProviderBuildException($"Không tìm thấy Source " + $"'{propertyName}' " + $"trong " + $"{source.GetType().Name}.");
            }

            return property.GetValue(source);
        }

        private static object? ResolveDictionaryValue(IReadOnlyDictionary<string, object?>? dictionary, string key)
        {
            if (dictionary == null || string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            return dictionary.TryGetValue(key, out object? value) ? value : null;
        }

        private static bool IsEmptyValue(object? value)
        {
            if (value == null)
            {
                return true;
            }

            if (value is string text)
            {
                return string.IsNullOrWhiteSpace(text);
            }

            return false;
        }

        private static object? ConvertDocumentValue(object? rawValue, string? dataType, string? formatString)
        {
            if (rawValue == null)
            {
                return null;
            }

            string type =
                dataType
                    ?.Trim()
                    .ToUpperInvariant()
                ?? "STRING";

            switch (type)
            {
                case "STRING":
                    return Convert.ToString(
                        rawValue,
                        CultureInfo.InvariantCulture
                    );

                case "INTEGER":
                    return Convert.ToInt32(
                        rawValue,
                        CultureInfo.InvariantCulture
                    );

                case "DECIMAL":
                    return Convert.ToDecimal(
                        rawValue,
                        CultureInfo.InvariantCulture
                    );

                case "BOOLEAN":
                    return Convert.ToBoolean(
                        rawValue,
                        CultureInfo.InvariantCulture
                    );

                case "DATE":
                case "DATETIME":
                    {
                        DateTime date =
                            Convert.ToDateTime(
                                rawValue,
                                CultureInfo.InvariantCulture
                            );

                        if (!string.IsNullOrWhiteSpace(
                            formatString))
                        {
                            return date.ToString(
                                formatString,
                                CultureInfo.InvariantCulture
                            );
                        }

                        return type == "DATE"
                            ? date.ToString(
                                "yyyy-MM-dd",
                                CultureInfo.InvariantCulture
                            )
                            : date.ToString(
                                "yyyy-MM-ddTHH:mm:ss",
                                CultureInfo.InvariantCulture
                            );
                    }

                default:
                    throw new ProviderBuildException(
                        $"Document DataType "
                        + $"'{dataType}' "
                        + "không được hỗ trợ."
                    );
            }
        }

        private static void SetXmlValue(XElement root, string targetPath, object? value)
        {
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                throw new ProviderBuildException("XML TargetPath đang trống.");
            }

            string[] nodes = targetPath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (nodes.Length == 0)
            {
                return;
            }

            XElement current = root;

            foreach (string nodeName in nodes)
            {
                XElement? child = current.Element(nodeName);

                if (child == null)
                {
                    child = new XElement(nodeName);

                    current.Add(child);
                }

                current = child;
            }

            current.Value = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }
    }

    public class DynamicProviderRequestBuilder
    {

        private readonly DynamicProviderDocumentBuilder _documentBuilder;

        public DynamicProviderRequestBuilder(DynamicProviderDocumentBuilder documentBuilder)
        {
            _documentBuilder = documentBuilder ?? throw new ArgumentNullException(nameof(documentBuilder));
        }
        public ProviderRequestResult Build(ProviderOperationRuntimeModel operation, IEnumerable<ProviderMappingModel> mappings, ProviderRequestContext context)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (mappings == null)
            {
                throw new ArgumentNullException(nameof(mappings));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (operation.Inactive)
            {
                throw new ProviderBuildException($"Operation '{operation.OperationCode}' đang Inactive.");
            }
            string baseUrl = GetRequiredConfigString(context, "BASE_URL");

            var result = new ProviderRequestResult
            {
                OperationID = operation.ID,

                OperationCode = operation.OperationCode ?? string.Empty,

                HttpMethod = NormalizeHttpMethod(operation.HttpMethod),

                BaseUrl = baseUrl,

                Endpoint = operation.Endpoint?.Trim() ?? string.Empty,

                BodyType = string.IsNullOrWhiteSpace(operation.BodyType) ? "JSON" : operation.BodyType.Trim().ToUpperInvariant(),

                ActionCode = operation.ActionCode?.Trim() ?? string.Empty,

                ExternalMethodCode = operation.ExternalMethodCode?.Trim() ?? string.Empty
            };

            var activeMappings = mappings
                .Where(x => !x.Inactive)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.ID)
                .ToList();


            foreach (var m in activeMappings)
            {
            }


            foreach (var mapping in activeMappings)
            {
                ApplyMapping(mapping, context, result, operation.ProviderID);
            }


            result.Url = BuildFinalUrl(result);


            foreach (var h in result.Headers)
            {
            }


            return result;
        }

        private void ApplyMapping(ProviderMappingModel mapping, ProviderRequestContext context, ProviderRequestResult result, int providerId)
        {

            string location = Normalize(mapping.MappingLocation);

            string targetPath = mapping.TargetPath?.Trim() ?? string.Empty;


            /*
             * ==================================================
             * 1. VALIDATE MAPPING CƠ BẢN
             * ==================================================
             */

            if (string.IsNullOrWhiteSpace(location))
            {
                result.SkippedMappings.Add($"Mapping ID {mapping.ID}: MappingLocation trống.");

                return;
            }

            if (string.IsNullOrWhiteSpace(targetPath))
            {
                throw new ProviderBuildException($"Mapping ID {mapping.ID} chưa có TargetPath.");
            }


            /*
             * ==================================================
             * 2. RESPONSE MAPPING
             * ==================================================
             *
             * RESPONSE không phải dữ liệu dùng để build request.
             *
             * Nó chỉ được sử dụng SAU KHI Provider trả response
             * tại ParseDynamicResponse().
             *
             * Vì vậy PHẢI return ở đây,
             * trước ResolveMappingValue().
             * ==================================================
             */

            if (location == "RESPONSE")
            {
                result.SkippedMappings.Add($"RESPONSE:{targetPath} - Không dùng khi build request.");


                return;
            }


            /*
             * ==================================================
             * 3. CONDITION
             * ==================================================
             */

            if (!EvaluateCondition(
    mapping,
    context,
    providerId))
            {
                result.SkippedMappings.Add($"{location}:{targetPath} - Condition không thỏa mãn.");

                return;
            }


            /*
             * ==================================================
             * 4. BODY ARRAY
             * ==================================================
             */

            if (location == "BODY" &&
    ContainsArrayPath(targetPath))
            {
                ApplyBodyArrayMapping(
                    mapping,
                    context,
                    result,
                    providerId
                );

                return;
            }


            /*
             * ==================================================
             * 5. RESOLVE SOURCE VALUE
             * ==================================================
             */

            object? rawValue = ResolveMappingValue(mapping.SourceType, mapping.SourceValue, context, providerId);



            /*
             * ==================================================
             * 6. DEFAULT VALUE
             * ==================================================
             */

            if (IsNullOrEmpty(rawValue))
            {
                rawValue = mapping.DefaultValue;
            }


            /*
             * ==================================================
             * 7. REQUIRED / OMIT NULL
             * ==================================================
             */

            if (IsNullOrEmpty(rawValue))
            {
                if (mapping.IsRequired)
                {
                    throw new ProviderBuildException($"Mapping '{targetPath}' là Required " + $"nhưng không lấy được giá trị từ " + $"{mapping.SourceType}:{mapping.SourceValue}.");
                }

                if (mapping.OmitWhenNull)
                {
                    result.SkippedMappings.Add($"{location}:{targetPath} - Giá trị null.");

                    return;
                }
            }


            /*
             * ==================================================
             * 8. CONVERT
             * ==================================================
             */

            object? convertedValue = ConvertMappingValue(rawValue, mapping.DataType, mapping.FormatString);



            /*
             * ==================================================
             * 9. PREFIX / SUFFIX
             * ==================================================
             */

            convertedValue = ApplyPrefixSuffix(convertedValue, mapping.PrefixValue, mapping.SuffixValue);


            /*
             * ==================================================
             * 10. APPLY REQUEST MAPPING
             * ==================================================
             */

            switch (location)
            {
                case "HEADER":
                    AddStringValue(result.Headers, targetPath, convertedValue);
                    break;


                case "QUERY":
                    AddStringValue(result.QueryParameters, targetPath, convertedValue);
                    break;


                case "PATH":
                    AddStringValue(result.PathParameters, targetPath, convertedValue);
                    break;


                case "FORM":
                    AddStringValue(result.FormFields, targetPath, convertedValue);
                    break;


                case "BODY":
                    SetJsonPath(result.Body, targetPath, convertedValue);
                    break;


                default: throw new ProviderBuildException($"MappingLocation '{location}' " + "không được hỗ trợ.");
            }



            result.AppliedMappings.Add($"{location}:{targetPath}");
        }

        private object? ResolveMappingValue(
    string? sourceType,
    string? sourceValue,
    ProviderRequestContext context,
    int providerId)
        {
            string normalizedSourceType =
                Normalize(sourceType);

            string path =
                sourceValue?.Trim()
                ?? string.Empty;

            return normalizedSourceType switch
            {
                "INVOICE" =>
                    GetValueByPath(
                        context.Invoice,
                        path
                    ),

                "INVOICEDETAIL" =>
                    GetValueByPath(
                        context.InvoiceDetail,
                        path
                    ),

                "PROPERTY" =>
                    GetValueByPath(
                        context.Property,
                        path
                    ),

                "CONFIG" =>
                    GetDictionaryValue(
                        context.Config,
                        path
                    ),

                "SESSION" =>
                    GetDictionaryValue(
                        context.Session,
                        path
                    ),

                "SYSTEM" =>
                    GetDictionaryValue(
                        context.System,
                        path
                    ),

                "CONSTANT" =>
                    sourceValue,

                "STATIC" =>
                    sourceValue,

                "RESPONSE" =>
                    GetValueByPath(
                        context.PreviousResponse,
                        path
                    ),

                "COLLECTION" =>
                    GetCollectionValue(
                        sourceValue,
                        context
                    ),

                "CUSTOM" =>
                    ResolveCustomValue(
                        sourceValue,
                        context
                    ),

                "DOCUMENT" =>
                    _documentBuilder.Build(
                        providerId,
                        path,
                        context
                    ),

                _ =>
                    throw new ProviderBuildException(
                        $"SourceType '{sourceType}' "
                        + "không được hỗ trợ."
                    )
            };
        }

        private object? GetValueByPath(object? source, string? path)
        {
            if (source == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return source;
            }

            object? current = source;

            string[] parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (string part in parts)
            {
                if (current == null)
                {
                    return null;
                }

                current = GetSingleValue(current, part);
            }

            return current;
        }

        private object? GetSingleValue(object source, string name)
        {
            if (source is JsonObject jsonObject)
            {
                var property = jsonObject.FirstOrDefault(x => string.Equals(x.Key, name, StringComparison.OrdinalIgnoreCase));

                return property.Value;
            }

            if (source is IDictionary<string, object?> dictionary)
            {
                var item = dictionary.FirstOrDefault(x => string.Equals(x.Key, name, StringComparison.OrdinalIgnoreCase));

                return item.Value;
            }

            if (source is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (JsonProperty property in jsonElement.EnumerateObject())
                    {
                        if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                        {
                            return property.Value;
                        }
                    }
                }

                return null;
            }

            PropertyInfo? propertyInfo = source.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

            if (propertyInfo != null)
            {
                return propertyInfo.GetValue(source);
            }

            FieldInfo? fieldInfo = source.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

            return fieldInfo?.GetValue(source);
        }

        private object? GetDictionaryValue(Dictionary<string, object?> dictionary, string key)
        {
            if (dictionary == null || string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            if (dictionary.TryGetValue(key, out object? value))
            {
                return value;
            }

            var item = dictionary.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));

            return item.Value;
        }

        private object? ConvertMappingValue(object? value, string? dataType, string? formatString)
        {
            if (value == null)
            {
                return null;
            }

            string type = Normalize(dataType);

            try
            {
                return type switch
                {
                    "" or "STRING" => FormatAsString(value, formatString),

                    "INT" or "INTEGER" => Convert.ToInt32(value, CultureInfo.InvariantCulture),

                    "DECIMAL" => ConvertDecimalValue(value, formatString),

                    "DATE" => ConvertToDateString(value, formatString ?? "yyyy-MM-dd"),

                    "DATETIME" => ConvertToDateString(value, formatString ?? "yyyy-MM-ddTHH:mm:ss"),

                    "ARRAY" => ConvertToJsonArray(value),

                    "OBJECT" => ConvertToJsonObject(value),

                    "BOOLEAN" or "BOOL" or "BIT" => ConvertToProviderBoolean(value),

                    "LONG" or "INT64" => Convert.ToInt64(value, CultureInfo.InvariantCulture),

                    "UNIX_MILLISECONDS" => ConvertToUnixMilliseconds(value),

                    _ => throw new ProviderBuildException($"DataType '{dataType}' không được hỗ trợ.")
                };
            }
            catch (ProviderBuildException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ProviderBuildException($"Không thể convert giá trị '{value}' " + $"sang DataType '{dataType}'.", ex);
            }
        }

        private static decimal ConvertDecimalValue(object value, string? formatString)
        {
            decimal decimalValue = Convert.ToDecimal(value, CultureInfo.InvariantCulture);

            if (string.IsNullOrWhiteSpace(formatString))
            {
                return decimalValue;
            }

            string format = formatString.Trim().ToUpperInvariant();

            if (format.StartsWith("ROUND:"))
            {
                string digitsText = format.Substring("ROUND:".Length);

                if (!int.TryParse(digitsText, out int digits))
                {
                    throw new ProviderBuildException($"FormatString '{formatString}' " + "không hợp lệ cho DECIMAL.");
                }

                if (digits < 0 || digits > 28)
                {
                    throw new ProviderBuildException($"Số chữ số round '{digits}' " + "không hợp lệ.");
                }

                return Math.Round(decimalValue, digits, MidpointRounding.AwayFromZero);
            }

            throw new ProviderBuildException($"FormatString '{formatString}' " + "không được hỗ trợ cho DECIMAL.");
        }

        private static long ConvertToUnixMilliseconds(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                throw new ProviderBuildException("Không thể chuyển null sang UNIX_MILLISECONDS.");
            }

            DateTime dateTime;

            if (value is DateTime dt)
            {
                dateTime = dt;
            }
            else if (value is DateTimeOffset dto)
            {
                return dto.ToUnixTimeMilliseconds();
            }
            else
            {
                if (!DateTime.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out dateTime))
                {
                    throw new ProviderBuildException($"Giá trị '{value}' không phải DateTime hợp lệ.");
                }
            }

            /*
             * Nếu DateTime từ PMS không chứa timezone,
             * xem nó là giờ local của server.
             */
            DateTimeOffset dateTimeOffset = dateTime.Kind switch
            {
                DateTimeKind.Utc => new DateTimeOffset(dateTime, TimeSpan.Zero),

                DateTimeKind.Local => new DateTimeOffset(dateTime),

                _ => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Local))
            };

            return dateTimeOffset.ToUnixTimeMilliseconds();
        }

        private static bool ConvertToProviderBoolean(object? value)
        {
            if (value == null ||
                value == DBNull.Value)
            {
                throw new ProviderBuildException("Không thể chuyển giá trị null sang BOOLEAN.");
            }

            if (value is bool booleanValue)
            {
                return booleanValue;
            }

            if (value is byte byteValue)
            {
                return byteValue != 0;
            }

            if (value is short shortValue)
            {
                return shortValue != 0;
            }

            if (value is int intValue)
            {
                return intValue != 0;
            }

            if (value is long longValue)
            {
                return longValue != 0;
            }

            if (value is decimal decimalValue)
            {
                return decimalValue != 0m;
            }

            string text = Convert.ToString(value)?.Trim().ToUpperInvariant() ?? string.Empty;

            return text switch
            {
                "TRUE" => true,
                "FALSE" => false,

                "1" => true,
                "0" => false,

                "YES" => true,
                "NO" => false,

                "Y" => true,
                "N" => false,

                "ON" => true,
                "OFF" => false,

                _ => throw new ProviderBuildException($"Giá trị '{value}' không thể chuyển sang BOOLEAN.")
            };
        }

        private string FormatAsString(object value, string? formatString)
        {
            if (!string.IsNullOrWhiteSpace(formatString) && value is IFormattable formattable)
            {
                return formattable.ToString(formatString, CultureInfo.InvariantCulture);
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private bool ConvertToBoolean(object value)
        {
            if (value is bool boolValue)
            {
                return boolValue;
            }

            string text = Convert.ToString(value)?.Trim() ?? string.Empty;

            return text.ToUpperInvariant() switch
            {
                "1" => true,
                "TRUE" => true,
                "YES" => true,
                "Y" => true,
                "0" => false,
                "FALSE" => false,
                "NO" => false,
                "N" => false,
                _ => Convert.ToBoolean(value)
            };
        }

        private string ConvertToDateString(object value, string format)
        {
            DateTime dateValue;

            if (value is DateTime dateTime)
            {
                dateValue = dateTime;
            }
            else if (value is DateOnly dateOnly)
            {
                dateValue = dateOnly.ToDateTime(TimeOnly.MinValue);
            }
            else
            {
                dateValue = Convert.ToDateTime(value, CultureInfo.InvariantCulture);
            }

            return dateValue.ToString(format, CultureInfo.InvariantCulture);
        }

        private JsonArray ConvertToJsonArray(object value)
        {
            if (value is JsonArray jsonArray)
            {
                return jsonArray;
            }

            if (value is IEnumerable enumerable && value is not string)
            {
                var result = new JsonArray();

                foreach (object? item in enumerable)
                {
                    result.Add(JsonSerializer.SerializeToNode(item));
                }

                return result;
            }

            string json = Convert.ToString(value) ?? "[]";

            return JsonNode.Parse(json) as JsonArray ?? new JsonArray();
        }

        private JsonObject ConvertToJsonObject(object value)
        {
            if (value is JsonObject jsonObject)
            {
                return jsonObject;
            }

            if (value is string jsonText)
            {
                return JsonNode.Parse(jsonText) as JsonObject ?? new JsonObject();
            }

            return JsonSerializer.SerializeToNode(value) as JsonObject ?? new JsonObject();
        }

        private object? ApplyPrefixSuffix(object? value, string? prefix, string? suffix)
        {
            if (string.IsNullOrEmpty(prefix) && string.IsNullOrEmpty(suffix))
            {
                return value;
            }

            if (value == null)
            {
                return null;
            }

            string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

            return $"{prefix}{text}{suffix}";
        }

        private bool EvaluateCondition(
    ProviderMappingModel mapping,
    ProviderRequestContext context,
    int providerId)
        {
            bool hasCondition = !string.IsNullOrWhiteSpace(mapping.ConditionSourceType) || !string.IsNullOrWhiteSpace(mapping.ConditionSourceValue) || !string.IsNullOrWhiteSpace(mapping.ConditionOperator);

            if (!hasCondition)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(mapping.ConditionSourceType) || string.IsNullOrWhiteSpace(mapping.ConditionOperator))
            {
                throw new ProviderBuildException($"Condition của Mapping ID {mapping.ID} " + "chưa được cấu hình đầy đủ.");
            }

            object? actualValue =
    ResolveMappingValue(
        mapping.ConditionSourceType,
        mapping.ConditionSourceValue,
        context,
        providerId
    );

            string operation = Normalize(mapping.ConditionOperator);

            string expectedValue = mapping.ConditionExpectedValue ?? string.Empty;

            return operation switch
            {
                "=" => CompareString(actualValue, expectedValue) == 0,

                "!=" => CompareString(actualValue, expectedValue) != 0,

                ">" => CompareNumber(actualValue, expectedValue) > 0,

                ">=" => CompareNumber(actualValue, expectedValue) >= 0,

                "<" => CompareNumber(actualValue, expectedValue) < 0,

                "<=" => CompareNumber(actualValue, expectedValue) <= 0,

                "CONTAINS" => ToText(actualValue).Contains(expectedValue, StringComparison.OrdinalIgnoreCase),

                "STARTSWITH" => ToText(actualValue).StartsWith(expectedValue, StringComparison.OrdinalIgnoreCase),

                "ENDSWITH" => ToText(actualValue).EndsWith(expectedValue, StringComparison.OrdinalIgnoreCase),

                "ISNULL" => IsNullOrEmpty(actualValue),

                "ISNOTNULL" => !IsNullOrEmpty(actualValue),

                _ => throw new ProviderBuildException($"ConditionOperator '{operation}' " + "không được hỗ trợ.")
            };
        }

        private int CompareString(object? actualValue, string expectedValue)
        {
            return string.Compare(ToText(actualValue), expectedValue, StringComparison.OrdinalIgnoreCase);
        }

        private int CompareNumber(object? actualValue, string expectedValue)
        {
            if (!decimal.TryParse(ToText(actualValue), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal actual))
            {
                throw new ProviderBuildException($"Giá trị '{actualValue}' không phải số.");
            }

            if (!decimal.TryParse(expectedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal expected))
            {
                throw new ProviderBuildException($"Expected Value '{expectedValue}' không phải số.");
            }

            return actual.CompareTo(expected);
        }

        private void AddStringValue(Dictionary<string, string> dictionary, string key, object? value)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ProviderBuildException("Mapping TargetPath không được để trống.");
            }

            dictionary[key] = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        /// <summary>
        /// Xác định TargetPath có chứa collection segment.
        ///
        /// Ví dụ:
        /// itemInfo[].itemName
        /// payments[].paymentMethodName
        /// </summary>
        private static bool ContainsArrayPath(string? targetPath)
        {
            return !string.IsNullOrWhiteSpace(targetPath) && targetPath.Contains("[]", StringComparison.Ordinal);
        }

        /// <summary>
        /// Build một BODY mapping có TargetPath chứa [].
        ///
        /// Không phụ thuộc provider.
        ///
        /// Hỗ trợ:
        /// - COLLECTION source: một giá trị cho mỗi dòng.
        /// - Scalar source: tạo array có một phần tử.
        /// - DefaultValue.
        /// - Required.
        /// - OmitWhenNull.
        /// - Prefix/Suffix.
        /// - DataType conversion cho từng phần tử.
        /// </summary>
        private void ApplyBodyArrayMapping(
     ProviderMappingModel mapping,
     ProviderRequestContext context,
     ProviderRequestResult result,
     int providerId)
        {
            string targetPath = mapping.TargetPath?.Trim() ?? string.Empty;

            IReadOnlyList<object?> sourceValues = ResolveArrayMappingValues(
        mapping,
        context,
        providerId
    );

            /*
             * Không có collection value.
             */
            if (sourceValues.Count == 0)
            {
                if (!IsNullOrEmpty(mapping.DefaultValue))
                {
                    sourceValues = new List<object?>
                    {
                        mapping.DefaultValue
                    };
                }
            }

            if (sourceValues.Count == 0)
            {
                if (mapping.IsRequired)
                {
                    throw new ProviderBuildException($"Mapping '{targetPath}' là Required " + $"nhưng không lấy được collection từ " + $"{mapping.SourceType}:" + $"{mapping.SourceValue}.");
                }

                if (mapping.OmitWhenNull)
                {
                    result.SkippedMappings.Add($"BODY:{targetPath} - Collection rỗng.");

                    return;
                }
            }

            var convertedValues = new List<object?>();

            foreach (object? sourceValue in sourceValues)
            {
                object? value = sourceValue;

                if (IsNullOrEmpty(value))
                {
                    value = mapping.DefaultValue;
                }

                /*
                 * Required được kiểm tra trên từng dòng.
                 */
                if (IsNullOrEmpty(value))
                {
                    if (mapping.IsRequired)
                    {
                        throw new ProviderBuildException($"Mapping '{targetPath}' là Required " + "nhưng có một phần tử collection " + $"không lấy được giá trị từ " + $"{mapping.SourceType}:" + $"{mapping.SourceValue}.");
                    }

                    convertedValues.Add(null);
                    continue;
                }

                object? convertedValue = ConvertMappingValue(value, mapping.DataType, mapping.FormatString);

                convertedValue = ApplyPrefixSuffix(convertedValue, mapping.PrefixValue, mapping.SuffixValue);

                convertedValues.Add(convertedValue);
            }

            SetJsonArrayPath(result.Body, targetPath, convertedValues);

            result.AppliedMappings.Add($"BODY:{targetPath}");
        }

        /// <summary>
        /// Resolve dữ liệu nguồn cho mapping TargetPath có [].
        ///
        /// COLLECTION:
        ///   Items.ItemName
        ///   InvoiceDetails.Quantity
        ///   ItemName
        ///
        /// sẽ trả:
        ///   [dòng1.ItemName, dòng2.ItemName, ...]
        ///
        /// Các SourceType scalar khác sẽ được bọc thành
        /// một collection có một phần tử.
        /// </summary>
        private IReadOnlyList<object?> ResolveArrayMappingValues(ProviderMappingModel mapping, ProviderRequestContext context, int providerId)
        {
            string sourceType = Normalize(mapping.SourceType);

            if (sourceType == "COLLECTION")
            {
                return ResolveCollectionProjection(mapping.SourceValue, context);
            }

            /*
             * Cho phép mapping như:
             *
             * payments[].paymentMethodName
             * ← INVOICE:PaymentMethod
             *
             * Scalar sẽ trở thành array có 1 object.
             */
            object? scalarValue = ResolveMappingValue(mapping.SourceType, mapping.SourceValue, context, providerId);

            if (IsNullOrEmpty(scalarValue))
            {
                return Array.Empty<object?>();
            }

            /*
             * Nếu source thực tế đã là collection,
             * chuyển về list.
             */
            if (scalarValue is IEnumerable enumerable && scalarValue is not string && scalarValue is not JsonObject && scalarValue is not JsonValue)
            {
                var values = new List<object?>();

                foreach (object? item in enumerable)
                {
                    values.Add(item);
                }

                return values;
            }

            return new List<object?>
                {
                    scalarValue
                };
        }

        /// <summary>
        /// Lấy một property từ từng phần tử của InvoiceDetail.
        ///
        /// SourceValue được hỗ trợ:
        ///
        /// Items.ItemName
        /// InvoiceDetail.ItemName
        /// InvoiceDetails.ItemName
        /// Details.ItemName
        /// ItemName
        ///
        /// Prefix tên collection được bỏ đi.
        /// Phần còn lại là path trong từng item.
        /// </summary>
        private IReadOnlyList<object?> ResolveCollectionProjection(string? sourceValue, ProviderRequestContext context)
        {
            string path = sourceValue?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(path))
            {
                return Array.Empty<object?>();
            }

            string[] parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length == 0)
            {
                return Array.Empty<object?>();
            }

            string collectionName = parts[0];



            object? collectionSource = null;

            /*
             * ==================================================
             * 1. COLLECTION REGISTRY
             * ==================================================
             *
             * Ví dụ:
             *
             * Collections["Items"]
             * Collections["TaxBreakdowns"]
             * Collections["Payments"]
             */
            if (context.Collections != null && context.Collections.TryGetValue(collectionName, out object? dynamicCollection))
            {
                collectionSource = dynamicCollection;
            }

            /*
             * ==================================================
             * 2. BACKWARD COMPATIBILITY
             * ==================================================
             *
             * Mapping cũ vẫn có thể dùng:
             *
             * Items.ItemName
             * Details.ItemName
             * InvoiceDetail.ItemName
             *
             * Nếu chưa đăng ký trong Collections
             * thì fallback về InvoiceDetail.
             */
            if (collectionSource == null && IsInvoiceDetailCollectionAlias(collectionName))
            {
                collectionSource = context.InvoiceDetail;
            }

            if (collectionSource == null)
            {
                throw new ProviderBuildException($"Không tìm thấy collection '{collectionName}'.");
            }

            if (collectionSource is not IEnumerable enumerable || collectionSource is string)
            {
                throw new ProviderBuildException($"Collection '{collectionName}' không phải IEnumerable.");
            }

            /*
             * Ví dụ:
             *
             * Items.ItemName
             *      ↓
             * collectionName = Items
             * itemPath       = ItemName
             *
             * TaxBreakdowns.TaxAmount
             *      ↓
             * collectionName = TaxBreakdowns
             * itemPath       = TaxAmount
             */
            string itemPath = parts.Length > 1 ? string.Join(".", parts.Skip(1)) : string.Empty;

            var values = new List<object?>();

            foreach (object? item in enumerable)
            {
                object? value = string.IsNullOrWhiteSpace(itemPath) ? item : GetValueByPath(item, itemPath);

                values.Add(value);
            }

            return values;
        }

        private static bool IsInvoiceDetailCollectionAlias(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            string normalized = name.Trim().ToUpperInvariant();

            return normalized is
                "ITEM" or
                "ITEMS" or
                "DETAIL" or
                "DETAILS" or
                "INVOICEDETAIL" or
                "INVOICEDETAILS" or
                "COLLECTION";
        }

        /// <summary>
        /// Bỏ tên collection ở đầu SourceValue.
        ///
        /// Items.ItemName          → ItemName
        /// Details.Quantity        → Quantity
        /// InvoiceDetail.TaxAmount → TaxAmount
        /// ItemName                → ItemName
        /// Items                   → ""
        /// </summary>
        private static string NormalizeCollectionItemPath(string? sourceValue)
        {
            string path = sourceValue?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            string[] parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length == 0)
            {
                return string.Empty;
            }

            string firstPart = parts[0].Trim();

            string[] collectionAliases =
            {
                "ITEM",
                "ITEMS",
                "DETAIL",
                "DETAILS",
                "INVOICEDETAIL",
                "INVOICEDETAILS",
                "COLLECTION"
            };

            bool startsWithCollectionAlias = collectionAliases.Contains(firstPart, StringComparer.OrdinalIgnoreCase);

            if (!startsWithCollectionAlias)
            {
                return path;
            }

            if (parts.Length == 1)
            {
                return string.Empty;
            }

            return string.Join(".", parts.Skip(1));
        }

        /// <summary>
        /// Gắn một danh sách giá trị vào TargetPath chứa [].
        ///
        /// Ví dụ:
        ///
        /// TargetPath:
        /// itemInfo[].itemName
        ///
        /// Values:
        /// ["A", "B"]
        ///
        /// Result:
        /// {
        ///   "itemInfo": [
        ///     { "itemName": "A" },
        ///     { "itemName": "B" }
        ///   ]
        /// }
        ///
        /// Các mapping khác dùng cùng itemInfo[] sẽ được
        /// merge vào object theo cùng index.
        /// </summary>
        private void SetJsonArrayPath(JsonObject root, string path, IReadOnlyList<object?> values)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ProviderBuildException("TargetPath array không hợp lệ.");
            }

            const string arrayMarker = "[]";

            int arrayMarkerIndex = path.IndexOf(arrayMarker, StringComparison.Ordinal);

            if (arrayMarkerIndex < 0)
            {
                throw new ProviderBuildException($"TargetPath '{path}' không chứa [].");
            }

            /*
             * Ví dụ:
             * parentPath = ""
             * arrayPropertyName = itemInfo
             * childPath = itemName
             *
             * Hoặc:
             * parentPath = invoiceData
             * arrayPropertyName = itemInfo
             */
            string pathBeforeArray = path[..arrayMarkerIndex].Trim().TrimEnd('.');

            string childPath = path[(arrayMarkerIndex + arrayMarker.Length)..].Trim().TrimStart('.');

            string[] beforeParts = pathBeforeArray.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (beforeParts.Length == 0)
            {
                throw new ProviderBuildException($"TargetPath array '{path}' " + "không có tên property.");
            }

            string arrayPropertyName = beforeParts[^1];

            JsonObject parentObject = GetOrCreateJsonObjectPath(root, beforeParts.Take(beforeParts.Length - 1));

            JsonArray targetArray;

            if (parentObject[arrayPropertyName] is JsonArray existingArray)
            {
                targetArray = existingArray;
            }
            else
            {
                targetArray = new JsonArray();

                parentObject[arrayPropertyName] = targetArray;
            }

            for (int index = 0; index < values.Count; index++)
            {
                /*
                 * Đảm bảo array có đủ số phần tử.
                 */
                while (targetArray.Count <= index)
                {
                    targetArray.Add(new JsonObject());
                }

                /*
                 * Trường hợp path kết thúc tại []:
                 *
                 * itemInfo[]
                 *
                 * gán nguyên value vào phần tử array.
                 */
                if (string.IsNullOrWhiteSpace(childPath))
                {
                    targetArray[index] = ConvertToJsonNode(values[index]);

                    continue;
                }

                JsonObject itemObject;

                if (targetArray[index] is JsonObject existingItemObject)
                {
                    itemObject = existingItemObject;
                }
                else
                {
                    itemObject = new JsonObject();

                    targetArray[index] = itemObject;
                }

                /*
                 * Dùng lại SetJsonPath cho phần path bên
                 * trong mỗi phần tử.
                 *
                 * Ví dụ:
                 * taxInfo.taxPercentage
                 */
                SetJsonPath(itemObject, childPath, values[index]);
            }
        }

        /// <summary>
        /// Tạo hoặc lấy chuỗi JsonObject cha.
        ///
        /// Ví dụ parts:
        /// invoiceData, content
        ///
        /// sẽ tạo:
        /// {
        ///   "invoiceData": {
        ///     "content": {}
        ///   }
        /// }
        /// </summary>
        private JsonObject GetOrCreateJsonObjectPath(JsonObject root, IEnumerable<string> parts)
        {
            JsonObject current = root;

            foreach (string rawPart in parts)
            {
                string part = rawPart?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(part))
                {
                    continue;
                }

                if (current[part] is JsonObject existingObject)
                {
                    current = existingObject;

                    continue;
                }

                var newObject = new JsonObject();

                current[part] = newObject;

                current = newObject;
            }

            return current;
        }

        private void SetJsonPath(JsonObject root, string path, object? value)
        {

            if (ContainsArrayPath(path))
            {
                throw new ProviderBuildException($"TargetPath '{path}' chứa []. " + "Hãy sử dụng SetJsonArrayPath().");
            }

            string[] parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length == 0)
            {
                throw new ProviderBuildException("TargetPath của BODY không hợp lệ.");
            }

            JsonObject current = root;

            for (int index = 0; index < parts.Length - 1; index++)
            {
                string part = parts[index];

                if (current[part] is not JsonObject child)
                {
                    child = new JsonObject();
                    current[part] = child;
                }

                current = child;
            }

            string finalProperty = parts[^1];

            current[finalProperty] = ConvertToJsonNode(value);
        }

        private JsonNode? ConvertToJsonNode(object? value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is JsonNode jsonNode)
            {
                return jsonNode.DeepClone();
            }

            return JsonSerializer.SerializeToNode(value);
        }

        private string BuildFinalUrl(ProviderRequestResult result)
        {
            string baseUrl = result.BaseUrl.TrimEnd('/');

            string endpoint = result.Endpoint.Trim();

            if (!string.IsNullOrWhiteSpace(endpoint) && !endpoint.StartsWith("/"))
            {
                endpoint = "/" + endpoint;
            }

            string url = baseUrl + endpoint;

            foreach (KeyValuePair<string, string> item in result.PathParameters)
            {
                string encodedValue = Uri.EscapeDataString(item.Value ?? string.Empty);

                url = url.Replace("{" + item.Key + "}", encodedValue, StringComparison.OrdinalIgnoreCase);
            }

            if (result.QueryParameters.Count > 0)
            {
                string queryString = string.Join("&", result.QueryParameters.Select(x => Uri.EscapeDataString(x.Key) + "=" + Uri.EscapeDataString(x.Value ?? string.Empty)));

                url += url.Contains('?') ? "&" + queryString : "?" + queryString;
            }

            return url;
        }

        private static string Normalize(string? value)
        {
            return value?
                .Trim()
                .ToUpperInvariant()
                ?? string.Empty;
        }

        private static string NormalizeHttpMethod(string? value)
        {
            string method = Normalize(value);

            return string.IsNullOrWhiteSpace(method) ? "POST" : method;
        }

        private static string ToText(object? value)
        {
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static bool IsNullOrEmpty(object? value)
        {
            if (value == null)
            {
                return true;
            }

            if (value is string text)
            {
                return string.IsNullOrWhiteSpace(text);
            }

            if (value is JsonElement element && (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined))
            {
                return true;
            }

            return false;
        }

        private string GetRequiredConfigString(ProviderRequestContext context, string configKey)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (string.IsNullOrWhiteSpace(configKey))
            {
                throw new ProviderBuildException("Config key không hợp lệ.");
            }

            object? value = GetDictionaryValue(context.Config, configKey);

            string text = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ProviderBuildException($"Không tìm thấy cấu hình bắt buộc '{configKey}'.");
            }

            return text;
        }

        private object? GetCollectionValue(string? sourceValue, ProviderRequestContext context)
        {
            string path = sourceValue?.Trim() ?? string.Empty;

            /*
             * Nếu không khai báo SourceValue,
             * giữ backward compatibility:
             * trả collection detail cũ.
             */
            if (string.IsNullOrWhiteSpace(path))
            {
                return context.InvoiceDetail;
            }

            string[] parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length == 0)
            {
                return context.InvoiceDetail;
            }

            string collectionName = parts[0];

            object? collectionSource = null;

            /*
             * ==================================================
             * 1. COLLECTION REGISTRY
             * ==================================================
             *
             * Ví dụ:
             *
             * Items
             * TaxBreakdowns
             * Payments
             */
            if (context.Collections != null && context.Collections.TryGetValue(collectionName, out object? dynamicCollection))
            {
                collectionSource = dynamicCollection;
            }

            /*
             * ==================================================
             * 2. BACKWARD COMPATIBILITY
             * ==================================================
             *
             * Mapping cũ:
             *
             * ITEMS
             * DETAILS
             * INVOICEDETAIL
             *
             * vẫn fallback về InvoiceDetail.
             */
            if (collectionSource == null && IsInvoiceDetailCollectionAlias(collectionName))
            {
                collectionSource = context.InvoiceDetail;
            }

            if (collectionSource == null)
            {
                return null;
            }

            /*
             * SourceValue chỉ là:
             *
             * Items
             *
             * hoặc:
             *
             * TaxBreakdowns
             *
             * => trả nguyên collection.
             */
            if (parts.Length == 1)
            {
                return collectionSource;
            }

            /*
             * Ví dụ:
             *
             * Items.ItemName
             *
             * collectionName = Items
             * remainingPath  = ItemName
             */
            string remainingPath = string.Join(".", parts.Skip(1));

            return GetValueByPath(collectionSource, remainingPath);
        }

        private object? ResolveCustomValue(string? sourceValue, ProviderRequestContext context)
        {
            if (string.IsNullOrWhiteSpace(sourceValue))
            {
                return null;
            }

            string template = sourceValue.Trim();

            /*
             * Nếu là JSON object/array thì parse JSON và
             * resolve tất cả placeholder bên trong.
             */
            if (template.StartsWith("{") || template.StartsWith("["))
            {
                JsonNode? node = JsonNode.Parse(template);

                if (node == null)
                {
                    return null;
                }

                return ResolveJsonTemplateNode(node, context);
            }

            /*
             * Nếu chỉ là biểu thức hoặc chuỗi bình thường.
             */
            return ResolveTemplateString(template, context);
        }

        private JsonNode? ResolveJsonTemplateNode(JsonNode? node, ProviderRequestContext context)
        {
            if (node == null)
            {
                return null;
            }

            if (node is JsonObject jsonObject)
            {
                var result = new JsonObject();

                foreach (KeyValuePair<string, JsonNode?> property in jsonObject)
                {
                    result[property.Key] = ResolveJsonTemplateNode(property.Value, context);
                }

                return result;
            }

            if (node is JsonArray jsonArray)
            {
                var result = new JsonArray();

                foreach (JsonNode? item in jsonArray)
                {
                    result.Add(ResolveJsonTemplateNode(item, context));
                }

                return result;
            }

            if (node is JsonValue jsonValue && jsonValue.TryGetValue(out string? stringValue))
            {
                object? resolved = ResolveTemplateString(stringValue, context);

                return ConvertToJsonNode(resolved);
            }

            // Number, bool, null giữ nguyên
            return node.DeepClone();
        }

        private object? ResolveTemplateString(string? template, ProviderRequestContext context)
        {
            if (template == null)
            {
                return null;
            }

            MatchCollection matches = Regex.Matches(template, @"\$\{([^{}]+)\}");

            if (matches.Count == 0)
            {
                return template;
            }

            /*
             * Chuỗi chỉ có đúng một placeholder:
             * giữ nguyên kiểu dữ liệu gốc.
             *
             * Ví dụ ${INVOICE.Amount} sẽ trả decimal,
             * không ép thành string.
             */
            if (matches.Count == 1 && matches[0].Value == template)
            {
                return EvaluateTemplateExpression(matches[0].Groups[1].Value, context);
            }

            /*
             * Chuỗi có nhiều placeholder hoặc kèm text:
             * nối thành string.
             */
            string result = template;

            foreach (Match match in matches)
            {
                object? value = EvaluateTemplateExpression(match.Groups[1].Value, context);

                result = result.Replace(match.Value, Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
            }

            return result;
        }

        private object? EvaluateTemplateExpression(string expression, ProviderRequestContext context)
        {
            string[] parts = expression.Split('|', StringSplitOptions.TrimEntries);

            string sourcePath = parts[0];

            object? value = ResolveContextPath(sourcePath, context);

            for (int index = 1; index < parts.Length; index++)
            {
                value = ApplyTemplateFilter(value, parts[index], context);
            }

            return value;
        }

        private object? ResolveContextPath(string sourcePath, ProviderRequestContext context)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return null;
            }

            int dotIndex = sourcePath.IndexOf('.');

            if (dotIndex <= 0)
            {
                throw new ProviderBuildException($"CUSTOM path không hợp lệ: {sourcePath}");
            }

            string sourceType = sourcePath[..dotIndex].Trim().ToUpperInvariant();

            string path = sourcePath[(dotIndex + 1)..].Trim();

            return sourceType switch
            {
                "INVOICE" => GetValueByPath(context.Invoice, path),

                "INVOICEDETAIL" => GetValueByPath(context.InvoiceDetail, path),

                "PROPERTY" => GetValueByPath(context.Property, path),

                "CONFIG" => GetDictionaryValue(context.Config, path),

                "SESSION" => GetDictionaryValue(context.Session, path),

                "SYSTEM" => GetDictionaryValue(context.System, path),

                "RESPONSE" => GetValueByPath(context.PreviousResponse, path),

                _ => throw new ProviderBuildException($"CUSTOM source '{sourceType}' không được hỗ trợ.")
            };
        }

        private object? ApplyTemplateFilter(object? value, string filterExpression, ProviderRequestContext context)
        {
            if (string.IsNullOrWhiteSpace(filterExpression))
            {
                return value;
            }

            string filterName;
            string? argument = null;

            int separatorIndex = filterExpression.IndexOf(':');

            if (separatorIndex >= 0)
            {
                filterName = filterExpression[..separatorIndex]
                        .Trim()
                        .ToLowerInvariant();

                argument = filterExpression[(separatorIndex + 1)..]
                        .Trim();
            }
            else
            {
                filterName = filterExpression
                        .Trim()
                        .ToLowerInvariant();
            }

            return filterName switch
            {
                "date" => FormatDateValue(value, argument ?? "dd/MM/yyyy"),

                "trim" => Convert.ToString(value)?.Trim(),

                "upper" => Convert.ToString(value)?.ToUpperInvariant(),

                "lower" => Convert.ToString(value)?.ToLowerInvariant(),

                "default" => string.IsNullOrWhiteSpace(Convert.ToString(value)) ? argument : value,

                "string" => Convert.ToString(value, CultureInfo.InvariantCulture),

                "integer" => ConvertToInteger(value),

                "decimal" => ConvertToDecimal(value),

                "boolean" => value == null ? null : ConvertToBoolean(value),

                /*
                 * Nối một chuỗi hằng.
                 * Ví dụ: |append:-ABC
                 */
                "append" => string.Concat(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty, argument ?? string.Empty),

                /*
                 * Nối giá trị lấy từ context path.
                 * Ví dụ:
                 * |appendpath:CONFIG.CLIENT_CODE
                 */
                "appendpath" => string
                            .Concat(Convert
                            .ToString(value, CultureInfo.InvariantCulture) ?? string.Empty, Convert
                            .ToString(ResolveContextPath(argument ?? string.Empty, context), CultureInfo.InvariantCulture) ?? string.Empty
                    ),

                "md5" => ComputeMd5(value),

                _ => throw new ProviderBuildException($"CUSTOM filter '{filterName}' không được hỗ trợ.")
            };
        }

        private string FormatDateValue(object? value, string format)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value is DateTime dateTime)
            {
                return dateTime.ToString(format, CultureInfo.InvariantCulture);
            }

            if (DateTime.TryParse(Convert.ToString(value), out DateTime parsed))
            {
                return parsed.ToString(format, CultureInfo.InvariantCulture);
            }

            throw new ProviderBuildException($"Giá trị '{value}' không phải ngày hợp lệ.");
        }

        private int? ConvertToInteger(object? value)
        {
            if (value == null)
            {
                return null;
            }

            return int.TryParse(Convert.ToString(value), out int result) ? result : throw new ProviderBuildException($"Không thể chuyển '{value}' thành Integer.");
        }

        private decimal? ConvertToDecimal(object? value)
        {
            if (value == null)
            {
                return null;
            }

            return decimal.TryParse(Convert.ToString(value), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result) ? result : throw new ProviderBuildException($"Không thể chuyển '{value}' thành Decimal.");
        }

        private string ComputeMd5(object? value)
        {
            string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

            byte[] inputBytes = Encoding.UTF8.GetBytes(text);

            byte[] hashBytes = MD5.HashData(inputBytes);

            return Convert
                .ToHexString(hashBytes)
                .ToLowerInvariant();
        }
    }
}
