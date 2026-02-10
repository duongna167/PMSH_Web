using BaseBusiness.bc;
using BaseBusiness.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.Facade
{
    public class JobTitleFacade : BaseFacadeDB
    {
        protected static JobTitleFacade instance = new JobTitleFacade(new JobTitleModel());
        protected JobTitleFacade(JobTitleModel model) : base(model)
        {
        }
        public static JobTitleFacade Instance
        {
            get { return instance; }
        }
        protected JobTitleFacade() : base()
        {
        }
    }
}
