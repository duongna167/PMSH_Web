using BaseBusiness.BO;
using BaseBusiness.Model;
using BaseBusiness.util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Profile.Commons.Helpers
{
    /// <summary>
    /// Profile.City lưu ID của bảng City (dạng chuỗi). Helper này đổi ID sang tên để hiển thị.
    /// </summary>
    public static class CityNameHelper
    {
        /// <summary>
        /// Lấy map ID (chuỗi) -> tên City.
        /// </summary>
        public static Dictionary<string, string> GetCityMap()
        {
            try
            {
                List<CityModel> list = PropertyUtils.ConvertToList<CityModel>(CityBO.Instance.FindAll());
                return list
                    .GroupBy(c => c.ID.ToString())
                    .ToDictionary(g => g.Key, g => g.First().Name ?? "");
            }
            catch (Exception)
            {
                return new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Trả về tên City nếu value là ID hợp lệ; nếu không (dữ liệu cũ lưu sẵn tên) thì giữ nguyên.
        /// </summary>
        public static string Resolve(Dictionary<string, string> map, object? value)
        {
            string raw = value == null || value == DBNull.Value ? "" : value.ToString()!.Trim();
            if (raw.Length == 0) return "";
            return map.TryGetValue(raw, out var name) && !string.IsNullOrEmpty(name) ? name : raw;
        }
    }
}
