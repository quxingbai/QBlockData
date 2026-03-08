using QBlockData.DataStructs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBlockData.Utils
{
    public static class DataUtils
    {
        public const char SplitCode = '\u001F';

        //public class TlvDataCreater
        //{
        //    /// <summary>
        //    /// Enqueue 入列
        //    /// </summary>
        //    public TlvDataCreater EQ(object Datt)
        //    {
        //        TLVData.DeserializationFromStream
        //    }
        //}

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
        public static int DecodeVarintBytes(IEnumerable<byte> Data)
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
                if (move >= 35)
                {
                    throw new Exception("数据过长");
                }
            }
            return result;
        }
    }
}
