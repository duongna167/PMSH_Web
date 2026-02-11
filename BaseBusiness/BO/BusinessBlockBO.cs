using BaseBusiness.bc;
using BaseBusiness.Facade;
using BaseBusiness.Model;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace BaseBusiness.BO
{
    using Dapper;
    public class BusinessBlockBO : BaseBO

    {
        private BusinessBlockFacade facade = BusinessBlockFacade.Instance;
        protected static BusinessBlockBO instance = new BusinessBlockBO();

        protected BusinessBlockBO()
        {
            this.baseFacade = facade;
        }

        public static BusinessBlockBO Instance
        {
            get { return instance; }
        }
        public string GetRoomNoById(int id, SqlConnection conn, SqlTransaction tx)
        {
            const string sql = "SELECT RoomNo FROM BusinessBlock WHERE ID = @id";
            return conn.QuerySingleOrDefault<string>(sql, new { id }, tx);
        }
        public void Update(long id, string code, SqlConnection conn, SqlTransaction tx)
        {
            string sql = "UPDATE BusinessBlock SET Code = @Code WHERE ID = @Id";
            conn.Execute(sql, new { Code = code, Id = id }, tx);
        }

        public bool IsDuplicateCode(string code, long id = 0)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            return IsDuplicateCode(
             "BusinessBlock",
             "Code",
             code.Trim(),
             id
            );

        }

    }
}
