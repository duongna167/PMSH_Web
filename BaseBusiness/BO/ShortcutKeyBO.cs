using BaseBusiness.bc;
using BaseBusiness.Facade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseBusiness.BO
{
    public class ShortcutKeyBO : BaseBO
    {
        private ShortcutKeyFacade facade = ShortcutKeyFacade.Instance;
        protected static ShortcutKeyBO instance = new ShortcutKeyBO();

        protected ShortcutKeyBO()
        {
            this.baseFacade = facade;
        }

        public static ShortcutKeyBO Instance
        {
            get { return instance; }
        }
    }
}
