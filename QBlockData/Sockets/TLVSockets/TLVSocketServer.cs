using QBlockData.DataStructs;
using QBlockData.Utils;
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

    public class TLVSocketServer : TLVSocketBase
    {
        /// <summary>
        /// 要在StartListen方法执行前执行  心跳检测
        /// </summary>
        public bool IsCheckedOnlineTime { get; set; }
        public event Action<TLVSocketServer, TLVSocketClientData> ClientJoin;
        public event Action<TLVSocketServer, TLVSocketClientData> ClientLeave;
        public class TLVSocketClientData
        {
            public Socket Socket { get; set; }
            public EndPoint EndPoint { get; set; }
            public int Life { get; set; }
            public void SetLifeToMax()
            {
                Life = 60;
            }
        }
        private Dictionary<Socket, TLVSocketClientData> Clients = new Dictionary<Socket, TLVSocketClientData>();
        public TLVSocketServer(ProtocolType protocolType) : base(protocolType)
        {
            Received += TLVSocketServer_Received;
        }

        private void TLVSocketServer_Received(TLVSocketBase arg1, Queue<TLVData> arg2, Socket arg3)
        {
            Clients[arg3].SetLifeToMax();
        }

        protected virtual void OnClientJoin(EndPoint Endpoint, TLVSocketClientData Data)
        {
            ClientJoin?.Invoke(this, Data);
        }
        public int SendToClients(IEnumerable<TLVData> Datas)
        {
            return SendTLVData(Datas, Clients.Keys);
        }
        public int SendToClient(IEnumerable<TLVData> Datas, IEnumerable<Socket> Targets)
        {
            return SendTLVData(Datas, Targets);
        }
        protected override void SocketReceiveTargetError(Socket Target, Exception ErrorMessage)
        {
            var t = Clients[Target];
            Clients.Remove(Target);
            ClientLeave?.Invoke(this, t);
        }
        public void StartListen(int Port, int MaxListenCount = 10)
        {
            IsRunning = true;
            Bind(new IPEndPoint(_LocalAnyAddress, Port));
            Task.Run(() =>
            {
                while (IsRunning)
                {
                    base.Listen(MaxListenCount);
                    var Client = Accept();
                    TLVSocketClientData ClientData = new TLVSocketClientData()
                    {
                        EndPoint = Client.RemoteEndPoint,
                        Socket = Client,
                        Life = 60,
                    };
                    Clients.Add(Client, ClientData);
                    OnClientJoin(ClientData.EndPoint, ClientData);
                    CreateSocketReceive(Client);
                }
            });
            if (IsCheckedOnlineTime)
            {
                //心跳包检测
                Task.Run(() =>
                {
                    while (IsCheckedOnlineTime && IsRunning)
                    {
                        foreach (var client in Clients)
                        {
                            var i = client.Value;
                            i.Life -= 1;
                            if (i.Life <= 0)
                            {
                                ClientLeave?.Invoke(this, i);
                                Clients.Remove(client.Key);
                            }
                        }
                        Thread.Sleep(1000);
                    }
                });
            }
        }



    }
}
