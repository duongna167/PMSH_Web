using System.Data;
using BaseBusiness.util;
using Microsoft.Data.SqlClient;
using NightAudit.Services.Interfaces;

namespace NightAudit.Services.Implements
{
    public class MapTransactionService : IMapTransactionService
    {
        /// <summary>Các cột được phép sửa/thêm và độ dài tối đa trong DB (PerDT là số nguyên, độ dài = 0).</summary>
        public static readonly Dictionary<string, int> Columns = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ProfitCenter"] = 100,
            ["TransactionCode"] = 100,
            ["Description"] = 510,
            ["AccountCode"] = 30,
            ["TK_No"] = 30,
            ["TK_Co"] = 30,
            ["TK_Co_Deposit"] = 30,
            ["TK_No_DT"] = 30,
            ["MaBP_ACC"] = 100,
            ["MaDT_ACC"] = 100,
            ["MaCN_ACC"] = 100,
            ["PerDT"] = 0,
            ["MaBCDT"] = 100,
            ["MaBPDC"] = 100
        };

        public DataTable GetAll() =>
            DataTableHelper.ExecuteQuery(
                "SELECT ID, ProfitCenter, TransactionCode, Description, AccountCode, TK_No, TK_Co, TK_Co_Deposit, TK_No_DT, " +
                "MaBP_ACC, MaDT_ACC, MaCN_ACC, PerDT, MaBCDT, MaBPDC FROM InterfaceToAcc ORDER BY ID");

        /// <summary>Tên cột đúng chữ hoa/thường theo danh sách cho phép (chống chèn tên cột tùy ý vào câu SQL).</summary>
        private static string Canonical(string column) => Columns.Keys.First(k => k.Equals(column, StringComparison.OrdinalIgnoreCase));

        /// <summary>Tham số SQL cho một cột; PerDT rỗng = NULL.</summary>
        private static SqlParameter Param(string column, string? value)
        {
            string name = "@" + column;
            if (column.Equals("PerDT", StringComparison.OrdinalIgnoreCase))
                return new SqlParameter(name, int.TryParse(value, out int n) ? n : DBNull.Value);
            return new SqlParameter(name, (object?)value ?? DBNull.Value);
        }

        private static List<(string Column, string? Value)> Pairs(Dictionary<string, string?> values) =>
            values.Where(v => Columns.ContainsKey(v.Key)).Select(v => (Canonical(v.Key), v.Value)).ToList();

        public int Add(Dictionary<string, string?> values)
        {
            var pairs = Pairs(values);
            string names = string.Join(", ", pairs.Select(p => p.Column));
            string pars = string.Join(", ", pairs.Select(p => "@" + p.Column));

            return DataTableHelper.ExecuteInsertAndReturnId(
                $"INSERT INTO InterfaceToAcc ({names}) VALUES ({pars}); SELECT CAST(SCOPE_IDENTITY() AS int)",
                pairs.Select(p => Param(p.Column, p.Value)).ToArray());
        }

        public bool Update(int id, Dictionary<string, string?> values)
        {
            var pairs = Pairs(values);
            if (pairs.Count == 0) return false;

            string set = string.Join(", ", pairs.Select(p => $"{p.Column} = @{p.Column}"));
            var parameters = pairs.Select(p => Param(p.Column, p.Value)).ToList();
            parameters.Add(new SqlParameter("@ID", id));

            return DataTableHelper.ExecuteNonQueryText($"UPDATE InterfaceToAcc SET {set} WHERE ID = @ID", parameters.ToArray()) > 0;
        }

        public bool Delete(int id) =>
            DataTableHelper.ExecuteNonQueryText("DELETE FROM InterfaceToAcc WHERE ID = @ID", new[] { new SqlParameter("@ID", id) }) > 0;
    }
}
