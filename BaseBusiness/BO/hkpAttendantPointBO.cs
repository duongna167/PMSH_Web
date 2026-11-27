using BaseBusiness.bc;
using BaseBusiness.Facade;
using BaseBusiness.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.BO
{
    public class hkpAttendantPointBO : BaseBO
    {
        private hkpAttendantPointFacade facade = hkpAttendantPointFacade.Instance;
        protected static hkpAttendantPointBO instance = new hkpAttendantPointBO();

        protected hkpAttendantPointBO()
        {
            this.baseFacade = facade;
        }

        public static hkpAttendantPointBO Instance
        {
            get { return instance; }
        }
        public static List<hkpAttendantPointModel> GethkpAttendantPoint(
          DateTime fromDate,
          DateTime toDate,
          string attendant)
        {
            string query = "SELECT * FROM hkpAttendantPoint WHERE 1=1";

            if (fromDate > DateTime.MinValue && toDate > DateTime.MinValue)
            {
                string from = fromDate.ToString("yyyy/MM/dd");
                string to = toDate.AddDays(1).ToString("yyyy/MM/dd");

                query += $@"
            AND AttendantDate >= '{from}'
            AND AttendantDate <  '{to}'";
            }

            if (!string.IsNullOrWhiteSpace(attendant))
            {
                // Chỉ ghép các số hợp lệ vào câu lệnh (chặn SQL injection)
                var ids = attendant.Split(',')
                                   .Select(value => int.TryParse(value.Trim(), out var id) ? id : (int?)null)
                                   .Where(id => id.HasValue)
                                   .Select(id => id!.Value)
                                   .ToList();
                query += ids.Count > 0 ? $" AND AttendantID IN ({string.Join(",", ids)})" : " AND 1 = 0";
            }

            return instance.GetList<hkpAttendantPointModel>(query);
        }



    }
}
