using QBlockData.DataStructs;
using QBlockData.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace QBlockData.Sockets.TLVSockets
{

    public class TLVSocketBase : Socket
    {
        protected static readonly IPAddress _LocalIpAddress = IPAddress.Parse("127.0.0.1");
        public bool IsRunning { get; protected set; } = false;
        public event Action<TLVSocketBase, Queue<TLVData>, Socket> Received;
        public TLVSocketBase(ProtocolType protocolType) : base(AddressFamily.InterNetwork, SocketType.Stream, protocolType)
        {
        }


        protected virtual int SendToTLVData(IEnumerable<TLVData> Data, IEnumerable<EndPoint> Targets)
        {
            int result = 0;
            MemoryStream ms = new MemoryStream();
            foreach (var i in Data)
            {
                ms.Write(i.Serialization());
            }
            foreach (var t in Targets)
            {
                result += SendTo(ms.ToArray(), t);
            }
            ms.Dispose();
            return result;
        }
        protected virtual int SendToTLVData(IEnumerable<TLVData> Data, EndPoint Target)
        {
            return SendToTLVData(Data, new EndPoint[] { Target });
        }
        protected virtual int SendTLVData(IEnumerable<TLVData> Data, IEnumerable<Socket> Targets)
        {
            var result = 0;
            MemoryStream ms = new MemoryStream();
            foreach (var i in Data)
            {
                ms.Write(i.Serialization());
            }
            foreach (var t in Targets)
            {
                result += t.Send(ms.ToArray());
            }
            ms.Dispose();
            return result;
        }
        protected virtual int SendTLVData(IEnumerable<TLVData> Data, Socket Target)
        {
            return SendTLVData(Data, new Socket[] { Target });
        }
        protected virtual void OnReceivedMessage(Queue<TLVData> Data, Socket Sender)
        {
            Received?.Invoke(this, Data, Sender);
        }
        protected virtual Task CreateSocketReceive(Socket ReceiveTarget)
        {
            return Task.Run(() =>
            {

                byte[] readBuffer = new byte[1024];
                bool hasHead = false;
                try
                {
                    while (IsRunning)
                    {
                        Queue<TLVData> Datas = new Queue<TLVData>();
                        MemoryStream Readed = new MemoryStream();
                        Queue<byte> LastEndIfQueue = new Queue<byte>();
                        var readLen = 0;
                        while ((readLen = ReceiveTarget.Receive(readBuffer)) != -1)
                        {
                            MemoryStream lastsMm = new MemoryStream();
                            Readed.Write(readBuffer, 0, readLen);
                            lastsMm.Write(readBuffer, 0, readLen);
                            foreach (var i in lastsMm.ToArray())
                            {
                                LastEndIfQueue.Enqueue(i);
                            }
                            while (LastEndIfQueue.Count > TLVSocketUtils.Commands.TLVDataEnd.Length)
                            {
                                LastEndIfQueue.Dequeue();
                            }
                            if (Readed.Length >= (TLVSocketUtils.Commands.TLVDataEnd.Length + TLVSocketUtils.Commands.TLVDataStart.Length))
                            {
                                //判断最后的一部分能不能组成一个TLVDataEnd  如果能了正式结束接受 否则继续等
                                if (LastEndIfQueue.SequenceEqual(TLVSocketUtils.Commands.TLVDataEnd.Data))
                                {
                                    break;
                                }
                            }
                            lastsMm.Dispose();
                            lastsMm = null;
                        }
                        Readed.Position = 0;

                        TLVData.DeserializationFromStream(Readed, data =>
                        {
                            if (data.Tag == TLVDataTags.String && data.Equals(TLVSocketUtils.Commands.TLVDataStart))
                            {
                                Datas = new Queue<TLVData>();
                                hasHead = true;
                            }
                            else if (data.Tag == TLVDataTags.String && data.Equals(TLVSocketUtils.Commands.TLVDataEnd))
                            {
                                OnReceivedMessage(Datas, ReceiveTarget);
                                hasHead = false;
                            }
                            else if (hasHead)
                            {
                                Datas.Enqueue(data);
                            }
                        });




                        //byte[] readBuffer = new byte[1024];
                        //bool hasHead = false;

                        //while (IsRunning)
                        //{
                        //    Queue<TLVData> Datas = new Queue<TLVData>();
                        //    MemoryStream Readed = new MemoryStream();
                        //    var readLen = 0;
                        //    while ((readLen= ReceiveTarget.Receive(readBuffer))!=-1)
                        //    {
                        //        Readed.Write(readBuffer,0,readLen);
                        //        if (readLen != readBuffer.Length) break;
                        //    }
                        //    Readed.Position = 0;

                        //    TLVData.DeserializationFromStream(Readed, data =>
                        //    {
                        //        if (data.Tag == TLVDataTags.String && data.Equals(TLVSocketUtils.Commands.TLVDataStart))
                        //        {
                        //            Datas = new Queue<TLVData>();
                        //            hasHead = true;
                        //        }
                        //        else if (data.Tag == TLVDataTags.String && data.Equals(TLVSocketUtils.Commands.TLVDataEnd))
                        //        {
                        //            OnReceivedMessage(Datas, ReceiveTarget);
                        //            hasHead = false;
                        //        }
                        //        else if (hasHead)
                        //        {
                        //            Datas.Enqueue(data);
                        //        }
                        //    });
                    }
                }
                catch(Exception error)
                {
                    SocketReceiveTargetError(ReceiveTarget, error);
                }
            });
        }
        protected virtual void SocketReceiveTargetError(Socket Target,Exception ErrorMessage)
        {

        }
    }
}
