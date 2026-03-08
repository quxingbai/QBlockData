using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBlockData.Utils
{
    public static class OrderUtils
    {
        public static T BeforeceAct<T>(Action act, Func<T> result)
        {
            act();
            return result();
        }
    }
}
