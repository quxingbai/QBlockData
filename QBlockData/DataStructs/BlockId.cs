using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace QBlockData.DataStructs
{
    public class BlockId
    {
        private string Source { get; set; }
        public int Index = -1;
        public int? Size = null;
        public void UpdateSource(String Source)
        {
            this.Source = Source;
            string i = Source;
            if (i.IndexOf('.') != -1)
            {
                var t = i.Split('.');
                this.Index = int.Parse(t[0]);
                this.Size = int.Parse(t[1]);

            }
            else
            {
                this.Index = int.Parse(i);
            }
        }
    }
}
