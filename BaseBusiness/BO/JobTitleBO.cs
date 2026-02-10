using BaseBusiness.bc;
using BaseBusiness.Facade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.BO
{
    public class JobTitleBO : BaseBO
    {
        private JobTitleFacade facade = JobTitleFacade.Instance;
        protected static JobTitleBO instance = new JobTitleBO();

        protected JobTitleBO()
        {
            this.baseFacade = facade;
        }

        public static JobTitleBO Instance
        {
            get { return instance; }
        }
    }
}
