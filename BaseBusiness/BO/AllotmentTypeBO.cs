using BaseBusiness.bc;
using BaseBusiness.Facade;

namespace BaseBusiness.BO
{
    public class AllotmentTypeBO : BaseBO
    {
        private AllotmentTypeFacade facade = AllotmentTypeFacade.Instance;
        protected static AllotmentTypeBO instance = new AllotmentTypeBO();

        protected AllotmentTypeBO()
        {
            this.baseFacade = facade;
        }

        public static AllotmentTypeBO Instance
        {
            get { return instance; }
        }

        public bool IsDuplicateCode(string code, long id = 0)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            return IsDuplicateCode(
             "AllotmentType",
             "Code",
             code.Trim(),
             id
            );

        }
    }
}
