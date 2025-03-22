using QBlockData.DataStructs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace QBlockData.Sockets.TLVSockets
{
    public class TLVSocketClient : TLVSocketBase
    {
        public TLVSocketClient(ProtocolType protocolType) : base(protocolType)
        {
        }
        public int SendToServer(IEnumerable<TLVData> Datas)
        {
            return SendTLVData(Datas, this);
        }
        public void StartConnect(EndPoint ServerTarget)
        {
            Connect(ServerTarget);
            IsRunning = true;
            CreateSocketReceive(this);
        }

    }
}
