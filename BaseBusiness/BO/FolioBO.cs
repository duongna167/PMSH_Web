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
    public class FolioBO : BaseBO
    {
        private FolioFacade facade = FolioFacade.Instance;
        protected static FolioBO instance = new FolioBO();

        protected FolioBO()
        {
            this.baseFacade = facade;
        }

        public static FolioBO Instance
        {
            get { return instance; }
        }

        public static List<FolioModel> GetFolioNo(int reservationID)
        {
            string query = $@"SELECT * FROM FOLIO WITH (NOLOCK) 
                         WHERE ReservationID={reservationID} order by FolioNo asc";
            return instance.GetList<FolioModel>(query);
        }

        public static List<FolioModel> GetFolioNoByReservationID(int reservationID,int folioNo)
        {
            string query = $"select * from Folio where ReservationID = {reservationID} and FolioNo = {folioNo}";
            return instance.GetList<FolioModel>(query);
        }
    }
}
