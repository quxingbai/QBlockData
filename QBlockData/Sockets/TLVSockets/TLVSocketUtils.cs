using QBlockData.DataStructs;
using QBlockData.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBlockData.Sockets.TLVSockets
{
    public static class TLVSocketUtils
    {
        public static class Commands
        {
            public static readonly TLVData TLVDataStart = new TLVData() { Tag = TLVDataTags.String, Data = Encoding.UTF8.GetBytes("TLV_START") };
            public static readonly TLVData TLVDataEnd = new TLVData() { Tag = TLVDataTags.String, Data = Encoding.UTF8.GetBytes("TLV_END") };
            public static readonly IEnumerable<TLVData> Ping = CreateStringTlvDataCommand("PING");
            public static IEnumerable<TLVData> CreateStringTlvDataCommand(string CommandString)
            {
                TLVData data = new TLVData()
                {
                    Data = Encoding.UTF8.GetBytes(CommandString),
                    Tag = TLVDataTags.String,
                };
                return new TLVData[] { TLVDataStart, data, TLVDataEnd };
            }
            public static IEnumerable<TLVData> CreateStringTlvDataCommands(params string[] CommandString)
            {
                List<TLVData> datas = new List<TLVData>();
                datas.Add(TLVDataStart);
                foreach (var i in CommandString)
                {
                    TLVData data = new TLVData()
                    {
                        Data = Encoding.UTF8.GetBytes(i),
                        Tag = TLVDataTags.String,
                    };
                    datas.Add(data);
                }
                datas.Add(TLVDataEnd);
                return datas.ToArray();
            }
            public static IEnumerable<TLVData> CreateCommands(bool WriteStartAndEndHeader = true, params (TLVDataTags, object)[] CommandDatas)
            {
                List<TLVData> dd = new List<TLVData>();
                if (WriteStartAndEndHeader)
                {
                    dd.Add(TLVDataStart);
                }
                foreach (var i in CommandDatas)
                {
                    dd.Add(new TLVData()
                    {
                        Tag = i.Item1,
                        Data = TLVData.ObjectToBytes(i.Item1, i.Item2)
                    }) ;
                }
                if (WriteStartAndEndHeader)
                {
                    dd.Add(TLVDataEnd);
                }
                return dd;
            }
        }
    }
}
