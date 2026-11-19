using System;
using System.Data;

namespace Security.Helpers
{
    internal static class SecurityRow
    {
        public static string Cell(DataRow d, string name)
        {
            if (d?.Table == null || !d.Table.Columns.Contains(name) || d[name] == DBNull.Value)
                return "";
            return d[name].ToString() ?? "";
        }

        public static int CellInt(DataRow d, string name)
        {
            if (d?.Table == null || !d.Table.Columns.Contains(name) || d[name] == DBNull.Value)
                return 0;
            return Convert.ToInt32(d[name]);
        }

        public static DateTime? CellDate(DataRow d, string name)
        {
            if (d?.Table == null || !d.Table.Columns.Contains(name) || d[name] == DBNull.Value)
                return null;
            return Convert.ToDateTime(d[name]);
        }
    }
}
