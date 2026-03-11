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
        /// <summary>
        /// 服务器如果没有在这些秒内发过来一个消息 那就报错
        /// </summary>
        public int ServerPingPackTimeoutSecond = 9;
        /// <summary>
        /// 是否开启服务器的超时检测，要同步开启服务器那边的超时包发送功能，要在StartConnect开启前设置
        /// </summary>
        public bool IsOpenServerPingTimeout = false;
        public event Action<TLVSocketClient> ServerPingPacketReceived;
        private DateTime? LastServerMessageDate = null;

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
            OpenTickSecond();
        }

        protected override void OnReceivedMessage(Queue<TLVData> Data, Socket Sender)
        {
            if (Data.Count == 1 && Data.Peek().ReadToString() == "PING")
            {
                ServerPingPacketReceived?.Invoke(this);
            }
            LastServerMessageDate = DateTime.Now;
            base.OnReceivedMessage(Data, Sender);
        }
        protected override void OnTickSecond(Dictionary<string, object> Args)
        {
            var now = DateTime.Now;
            if (IsOpenServerPingTimeout && LastServerMessageDate != null && (now - LastServerMessageDate.Value).TotalSeconds > ServerPingPackTimeoutSecond)
            {
                throw new SocketException();
            }
        }
    }
}
