using BaseBusiness.bc;
using BaseBusiness.Facade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.BO
{
    public class hkpFacilityCodeBO : BaseBO
    {
        private hkpFacilityCodeFacade facade = hkpFacilityCodeFacade.Instance;
        protected static hkpFacilityCodeBO instance = new hkpFacilityCodeBO();

        protected hkpFacilityCodeBO()
        {
            this.baseFacade = facade;
        }

        public static hkpFacilityCodeBO Instance
        {
            get { return instance; }
        }

        public bool IsDuplicateCode(string code, long id = 0)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            return facade.Exists(
                "hkpFacilityCode",
                new Dictionary<string, object>
                {
            { "Code", code.Trim() }
                },
                id
            );
        }
    }
}
