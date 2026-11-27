using BaseBusiness.util;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;

namespace Profile.Services.Implements
{
    /// <summary>
    /// Xuất kết quả Search Profile ra .xlsx (tương đương Excel / Ctrl+E trên WinForm frmProfileSearch).
    /// Đọc spProfileSearch_Test bằng SqlDataReader và ghi file kiểu streaming (OpenXmlWriter)
    /// nên xuất được toàn bộ (hàng trăm nghìn dòng) mà không giữ hết dữ liệu trong bộ nhớ.
    /// </summary>
    public static class ProfileSearchExcelExporter
    {
        private sealed record Col(string Header, string Source, double Width);

        // Excel tối đa 1.048.576 dòng, trừ 1 dòng tiêu đề
        public const int MaxExcelDataRows = 1_048_575;

        // Cùng thứ tự / ẩn hiện cột như grid Search Profile (type "1" = Travel Agent)
        private static List<Col> GetColumns(string type)
        {
            bool isAgent = type == "1";
            var cols = new List<Col>
            {
                new("Code", "Code", 14)
            };
            if (!isAgent) cols.Add(new("Vip", "VIP", 8));
            cols.Add(new("Account", "Account", 35));
            if (isAgent) cols.Add(new("Tax Code", "TaxCode", 16));
            if (!isAgent)
            {
                cols.Add(new("Date Of Birth", "DateOfBirth", 14));
                cols.Add(new("PassPort", "PassPort", 16));
                cols.Add(new("Identity Card", "IdentityCard", 16));
            }
            cols.Add(new("City", "City", 18));
            cols.Add(new("Nat", "Nationality", 8));
            cols.Add(new("Address", "Address", 35));
            cols.Add(new("HandPhone", "HandPhone", 16));
            cols.Add(new("Telephone", "Telephone", 16));
            cols.Add(new("Email", "Email", 28));
            cols.Add(new("Website", "Website", 20));
            if (!isAgent) cols.Add(new("Tax Code", "TaxCode", 16));
            cols.Add(new("Company", "FullAccount", 28));
            cols.Add(new("Business Address", "HomeAddress", 30));
            if (isAgent) cols.Add(new("ARNo", "ARNo", 12));
            cols.Add(new("Postal Code", "PostalCode", 12));
            cols.Add(new("Keyword", "Keyword", 16));
            if (isAgent) cols.Add(new("Acct.Contact", "AcctContact", 18));
            cols.Add(new("Description", "Description", 30));
            if (!isAgent)
            {
                cols.Add(new("Return", "ReturnGuest", 8));
                cols.Add(new("Stay No", "StayNo", 8));
            }
            return cols;
        }

        // Giống ProfileBO.GetAllProfileTest: "6" (All) và rỗng => ""
        private static string MapType(string? type) =>
            string.IsNullOrEmpty(type) || type == "6" ? "" : type;

        /// <returns>Số dòng dữ liệu đã ghi</returns>
        public static int Write(Stream output, string? code, string? account, string? firstName,
            string? keyWord, string? city, string? type, bool showSaleInCharge)
        {
            var columns = GetColumns(type ?? "");
            var cityMap = Profile.Commons.Helpers.CityNameHelper.GetCityMap();
            int rowCount = 0;

            using var connection = new SqlConnection(DBUtils.GetDBConnectionString());
            using var command = new SqlCommand("spProfileSearch_Test", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 600
            };
            command.Parameters.AddWithValue("@Code", code ?? "");
            command.Parameters.AddWithValue("@Account", account ?? "");
            command.Parameters.AddWithValue("@FirstName", firstName ?? "");
            command.Parameters.AddWithValue("@Keyword", keyWord ?? "");
            command.Parameters.AddWithValue("@City", city ?? "");
            command.Parameters.AddWithValue("@Type", MapType(type));
            command.Parameters.AddWithValue("@ShowSaleInCharge", showSaleInCharge ? "true" : "");
            command.Parameters.AddWithValue("@Skip", 0);
            command.Parameters.AddWithValue("@Take", MaxExcelDataRows);

            connection.Open();
            using var reader = command.ExecuteReader(CommandBehavior.SequentialAccess);

            // Vị trí cột trong reader (-1 nếu SP không trả cột đó)
            var ordinals = columns.Select(c =>
            {
                try { return reader.GetOrdinal(c.Source); } catch (IndexOutOfRangeException) { return -1; }
            }).ToArray();
            // SequentialAccess yêu cầu đọc theo thứ tự tăng dần của ordinal
            var readOrder = Enumerable.Range(0, columns.Count)
                .Where(i => ordinals[i] >= 0)
                .OrderBy(i => ordinals[i])
                .ToArray();
            var letters = Enumerable.Range(0, columns.Count).Select(ColumnLetter).ToArray();
            var values = new string?[columns.Count];

            using (var document = SpreadsheetDocument.Create(output, SpreadsheetDocumentType.Workbook))
            {
                var workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
                stylesPart.Stylesheet = BuildStylesheet();
                stylesPart.Stylesheet.Save();

                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();

                using (var writer = OpenXmlWriter.Create(worksheetPart))
                {
                    writer.WriteStartElement(new Worksheet());

                    // Cố định dòng tiêu đề
                    writer.WriteElement(new SheetViews(new SheetView(
                        new Pane
                        {
                            VerticalSplit = 1,
                            TopLeftCell = "A2",
                            ActivePane = PaneValues.BottomLeft,
                            State = PaneStateValues.Frozen
                        })
                    { WorkbookViewId = 0 }));

                    writer.WriteStartElement(new Columns());
                    for (int i = 0; i < columns.Count; i++)
                    {
                        writer.WriteElement(new Column
                        {
                            Min = (uint)(i + 1),
                            Max = (uint)(i + 1),
                            Width = columns[i].Width,
                            CustomWidth = true
                        });
                    }
                    writer.WriteEndElement(); // Columns

                    writer.WriteStartElement(new SheetData());

                    // Dòng tiêu đề (style 1 = in đậm)
                    WriteRow(writer, 1, letters, columns.Select(c => (string?)c.Header).ToArray(), styleIndex: 1);

                    while (reader.Read())
                    {
                        Array.Clear(values);
                        foreach (int i in readOrder)
                        {
                            int ordinal = ordinals[i];
                            if (reader.IsDBNull(ordinal)) continue;
                            string value = Convert.ToString(reader.GetValue(ordinal)) ?? "";
                            if (columns[i].Source == "DateOfBirth") value = value.Replace('.', '/'); // dd.MM.yyyy -> dd/MM/yyyy
                            else if (columns[i].Source == "City") value = Profile.Commons.Helpers.CityNameHelper.Resolve(cityMap, value); // ID -> tên
                            values[i] = value;
                        }
                        rowCount++;
                        WriteRow(writer, (uint)(rowCount + 1), letters, values, styleIndex: 0);
                    }

                    writer.WriteEndElement(); // SheetData

                    writer.WriteElement(new AutoFilter
                    {
                        Reference = $"A1:{letters[^1]}{rowCount + 1}"
                    });

                    writer.WriteEndElement(); // Worksheet
                }

                workbookPart.Workbook.AppendChild(new Sheets(new Sheet
                {
                    Id = workbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1,
                    Name = "Profiles"
                }));

                // AutoFilter cần defined name _xlnm._FilterDatabase để Excel nhận đúng
                workbookPart.Workbook.AppendChild(new DefinedNames(new DefinedName
                {
                    Name = "_xlnm._FilterDatabase",
                    LocalSheetId = 0,
                    Hidden = true,
                    Text = $"'Profiles'!$A$1:${letters[^1]}${rowCount + 1}"
                }));

                workbookPart.Workbook.Save();
            }

            return rowCount;
        }

        private static void WriteRow(OpenXmlWriter writer, uint rowIndex, string[] letters, string?[] values, uint styleIndex)
        {
            writer.WriteStartElement(new Row(), new[] { new OpenXmlAttribute("r", null!, rowIndex.ToString()) });
            for (int i = 0; i < values.Length; i++)
            {
                string? value = values[i];
                if (string.IsNullOrEmpty(value)) continue;

                var attributes = new List<OpenXmlAttribute>
                {
                    new("r", null!, letters[i] + rowIndex),
                    new("t", null!, "inlineStr")
                };
                if (styleIndex != 0) attributes.Add(new("s", null!, styleIndex.ToString()));

                writer.WriteStartElement(new Cell(), attributes);
                writer.WriteElement(new InlineString(new Text(CleanXml(value)) { Space = SpaceProcessingModeValues.Preserve }));
                writer.WriteEndElement(); // Cell
            }
            writer.WriteEndElement(); // Row
        }

        // Loại ký tự điều khiển không hợp lệ trong XML (dữ liệu nhập tay đôi khi có)
        private static string CleanXml(string value)
        {
            bool clean = true;
            foreach (char ch in value)
            {
                if (ch < 0x20 && ch != '\t' && ch != '\n' && ch != '\r') { clean = false; break; }
            }
            if (clean) return value;

            var sb = new StringBuilder(value.Length);
            foreach (char ch in value)
            {
                if (ch >= 0x20 || ch == '\t' || ch == '\n' || ch == '\r') sb.Append(ch);
            }
            return sb.ToString();
        }

        private static string ColumnLetter(int index)
        {
            string letter = "";
            index++;
            while (index > 0)
            {
                int mod = (index - 1) % 26;
                letter = (char)('A' + mod) + letter;
                index = (index - 1) / 26;
            }
            return letter;
        }

        private static Stylesheet BuildStylesheet()
        {
            return new Stylesheet(
                new Fonts(
                    new Font(),                       // 0: mặc định
                    new Font(new Bold())              // 1: đậm
                ),
                new Fills(
                    new Fill(new PatternFill { PatternType = PatternValues.None }),
                    new Fill(new PatternFill { PatternType = PatternValues.Gray125 })
                ),
                new Borders(new Border()),
                new CellFormats(
                    new CellFormat(),                                  // 0
                    new CellFormat { FontId = 1, ApplyFont = true }    // 1: tiêu đề
                ));
        }
    }
}
