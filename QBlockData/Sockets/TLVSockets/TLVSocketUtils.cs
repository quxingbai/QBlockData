using QBlockData.DataStructs;
using QBlockData.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
                    });
                }
                if (WriteStartAndEndHeader)
                {
                    dd.Add(TLVDataEnd);
                }
                return dd;
            }
        }

        public static class NetUtil
        {
            public static IEnumerable<IPAddress> GetLocalIpv4List()
            {
                //List<IPAddress> ipl = new List<IPAddress>();
                //var host = Dns.GetHostEntry(Dns.GetHostName());
                //foreach (var ip in host.AddressList)
                //{
                //    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                //    {
                //        ipl.Add(ip);
                //    }
                //}
                //return ipl;
                var usableIPs = NetworkInterface.GetAllNetworkInterfaces()
                        .Where(n => n.OperationalStatus == OperationalStatus.Up)
                        .Where(n => n.GetIPProperties().GatewayAddresses.Any(g => !g.Address.Equals(IPAddress.Any)))
                        .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                        .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                        .Select(a => a.Address)
                        .ToList();
                return usableIPs;
            }
        }
    }
}
