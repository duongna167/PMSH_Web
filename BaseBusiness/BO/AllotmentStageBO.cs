using BaseBusiness.bc;
using BaseBusiness.Facade;

namespace BaseBusiness.BO
{
    public class AllotmentStageBO :  BaseBO
    {
        private AllotmentStageFacade facade = AllotmentStageFacade.Instance;
        protected static AllotmentStageBO instance = new AllotmentStageBO();

        protected AllotmentStageBO()
        {
            this.baseFacade = facade;
        }

        public static AllotmentStageBO Instance
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
