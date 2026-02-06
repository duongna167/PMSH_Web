using BaseBusiness.bc;
using BaseBusiness.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.Facade
{
    public class ShortcutKeyFacade : BaseFacadeDB
    {
        protected static ShortcutKeyFacade instance = new ShortcutKeyFacade(new ShortcutKeyModel());
        protected ShortcutKeyFacade(ShortcutKeyModel model) : base(model)
        {
        }
        public static ShortcutKeyFacade Instance
        {
            get { return instance; }
        }
        protected ShortcutKeyFacade() : base()
        {
        }
    }
}
