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
            string query = $@"
                DECLARE @ConfNo varchar(50) = (SELECT TOP 1 ConfirmationNo FROM Reservation WITH (NOLOCK) WHERE ID = {reservationID})
                SELECT ID, ARNo, FolioDate, FolioNo, ReservationID, ISNULL(ProfileID,0) as ProfileID, AccountName, ISNULL(Status,0) as Status, ISNULL(IsMasterFolio,0) as IsMasterFolio, ConfirmationNo, ISNULL(BalanceUSD,0) as BalanceUSD, ISNULL(BalanceVND,0) as BalanceVND, ISNULL(IsPrintVAT,0) as IsPrintVAT, ISNULL(CreateDate,GETDATE()) as CreateDate, ISNULL(UpdateDate,GETDATE()) as UpdateDate, ISNULL(UserUpdateID,0) as UserUpdateID, ISNULL(UserInsertID,0) as UserInsertID 
                FROM FOLIO WITH (NOLOCK) 
                WHERE (ReservationID = {reservationID} AND ISNULL(IsMasterFolio,0) = 0)
                   OR (ConfirmationNo = @ConfNo AND ISNULL(IsMasterFolio,0) = 1)
                ORDER BY FolioNo ASC";
            return instance.GetList<FolioModel>(query);
        }

        public static List<FolioModel> GetFolioNoByReservationID(int reservationID,int folioNo)
        {
            string query = $"select * from Folio where ReservationID = {reservationID} and FolioNo = {folioNo}";
            return instance.GetList<FolioModel>(query);
        }

    }
}
