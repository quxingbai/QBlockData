using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBlockData.Utils
{
    static class QBlockDataVersion
    {
        public static string Version = "1.1";

        public static string toString()
        {
            return "版本：" + Version + ",创建于：" + DateTime.Now;
        }
    }
}
