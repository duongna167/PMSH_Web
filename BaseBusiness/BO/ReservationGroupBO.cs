using BaseBusiness.bc;
using BaseBusiness.Facade;

namespace BaseBusiness.BO
{
    public class ReservationGroupBO : BaseBO
    {
        private readonly ReservationGroupFacade facade = ReservationGroupFacade.Instance;
        protected static ReservationGroupBO instance = new();

        protected ReservationGroupBO()
        {
            this.baseFacade = facade;
        }

        public static ReservationGroupBO Instance
        {
            get { return instance; }
        }

    }
}
