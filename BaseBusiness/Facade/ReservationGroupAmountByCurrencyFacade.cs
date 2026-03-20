using BaseBusiness.bc;
using BaseBusiness.Model;


namespace BaseBusiness.Facade
{
    public class ReservationGroupAmountByCurrencyFacade : BaseFacadeDB
    {
        protected static ReservationGroupAmountByCurrencyFacade instance = new(new ReservationGroupAmountByCurrencyModel());
        protected ReservationGroupAmountByCurrencyFacade(ReservationGroupAmountByCurrencyModel model) : base(model)
        {
        }
        public static ReservationGroupAmountByCurrencyFacade Instance
        {
            get { return instance; }
        }
        protected ReservationGroupAmountByCurrencyFacade() : base()
        {
        }
    }
}
