using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBlockData.Utils
{
    public static class DataUtils
    {
        public static IEnumerable<byte> EncodeVarintBytes(int Data)
        {
            List<byte> result = new List<byte>();
            while (Data > 0)
            {
                byte b = (byte)(Data & 0x7F);
                Data >>= 7;
                if (Data > 0)
                {
                    b |= 0x80;
                }
                result.Add(b);
            }
            return result;
        }
        public static int DecodeVarintBytes(byte[] Data)
        {
            int result = 0, move = 0;
            foreach (var d in Data)
            {
                result |= (d & 0x7F) << move;
                if ((d & 0x80) == 0)
                {
                    break;
                }
                move += 7;
            }
            return result;
        }
        public static int VarintReadStart(Func<byte> ReadNext)
        {
            int result = 0, move = 0;
            while (true)
            {
                var b = ReadNext();
                result |= (b & 0x7F) << move;
                move += 7;
                if ((b & 0x80) == 0) break;
                if (move >= 32)
                {
                    throw new Exception("数据过长");
                }
            }
            return result;
        }
    }
}
