using BaseBusiness.bc;
using BaseBusiness.Facade;
using BaseBusiness.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace BaseBusiness.BO
{
    using Dapper;
    public class RoomBO : BaseBO
    {
        private RoomFacade facade = RoomFacade.Instance;
        protected static RoomBO instance = new RoomBO();

        protected RoomBO()
        {
            this.baseFacade = facade;
        }

        public static RoomBO Instance
        {
            get { return instance; }
        }
        public static List<RoomModel> GetRoomZone(string roomtype, string zone)
        {
            int roomtypeInt = int.TryParse(roomtype, out var temp) ? temp : 0;
            // Chuyển zone thành chuỗi "1,2,3" => "'1','2','3'" (nếu là chuỗi)
            var zoneList = zone.Split(',').Select(z => $"'{z.Trim()}'");
            var zoneInClause = string.Join(",", zoneList);

            string query = $@"
        SELECT *
        FROM Room WITH(NOLOCK) 
        WHERE ({roomtypeInt} = 0 OR RoomTypeID = {roomtypeInt})
          AND (ZoneID IN ({zoneInClause}) OR '{zone}' = '0')  AND HKStatusID = 7";

            return instance.GetList<RoomModel>(query);
        }
        public static int GetNumberOfRoom(int roomTypeID)
        {
            string query = $"DECLARE @roomType AS INT\r\nSET @roomType = {roomTypeID}\r\n\r\nSELECT COUNT(*) \r\nFROM Room \r\nLEFT JOIN RoomType ON Room.RoomTypeID = RoomType.ID \r\nWHERE \r\n    (@roomType = 0 OR Room.RoomTypeID = @roomType)\r\n    AND (RoomType.IsPseudo = 0 OR RoomType.ID IS NULL) ";
            return instance.GetFirst<int>(query);
        }
        public static List<RoomModel> GetRoomCountPlan()
        {
            string query = $@"Select * from Room WITH (NOLOCK) where RoomTypeCode<>'XXX'";

            return instance.GetList<RoomModel>(query);
        }
        public static List<RoomModel> GetFloorPlan(string block, string suffix, string name, string zone)
        {
            string condition = "";
            string safeZone = zone?.Replace("'", "''") ?? "";
            if (suffix == "-A")
            {
                condition = "CONVERT(Int, RoomNo) < 020000";
            }
            else
            {
                condition = "CONVERT(Int, RoomNo) > 019999";
            }

            string query = $@"
        SELECT r.*
        FROM Room r
        JOIN RoomType rt ON r.RoomTypeCode = rt.Code
        WHERE {condition}
          AND r.RoomTypeID != 8
          AND r.BlockID = N'{block}'
          AND r.floor + '{suffix}' = N'{name}'
          AND (
                '{safeZone}' = '' 
                OR rt.ZoneCode IN (
                    SELECT value FROM STRING_SPLIT('{safeZone}', ',')
                )
              )
        ORDER BY CONVERT(Int, r.RoomNo)
    ";

            return instance.GetList<RoomModel>(query);
        }
        public string GetRoomNoById(int id, SqlConnection conn, SqlTransaction tx)
        {
            const string sql = "SELECT RoomNo FROM Room WHERE ID = @id";
            return conn.QuerySingleOrDefault<string>(sql, new { id }, tx);
        }

        public static List<RoomModel> GetRoom(string query)
        {


            return instance.GetList<RoomModel>(query);
        }
        public static List<ReservationDTO> UpdateRoomStatus(string query)
        {


            return instance.GetList<ReservationDTO>(query);
        }
        public class ReservationDTO
        {
            public string RoomNo { get; set; }       // Số phòng
            public int NoOfAdult { get; set; }       // Số người lớn
            public decimal Surcharge { get; set; }   // Phụ phí
            public string Country { get; set; }      // Quốc gia
        }

    }
}
