using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace QBlockData.DataStructs
{
    public class LinkNodeBlockData
    {
        public string Key { get; set; }
        public long StartPosition { get; set; }
        public long DataLength { get; set; }
        public long ContentLength { get; set; }
        public long HeaderLength { get; set; }
        public long Length => HeaderLength + ContentLength;
        public int Index { get; set; }
        public string ToString(string SplitCodes)
        {
            return Key + SplitCodes + ContentLength + SplitCodes + DataLength + SplitCodes;
        }
        public byte[] ToBytes(string SplitCodes)
        {
            return Encoding.UTF8.GetBytes(ToString(SplitCodes));
        }
    }
    public class LinkNodeBlockDataController
    {

        private long CreateNodeAddSize = 15;
        private List<LinkNodeBlockData> EmptyNodes = new List<LinkNodeBlockData>();
        private Stream _Stream = null;
        private object _PositionObjectLock = new object();
        private string SplitCodes = "$";
        private byte[] SplitCodesByte = Encoding.UTF8.GetBytes("$");

        public LinkNodeBlockDataController(Stream Stream)
        {
            this._Stream = Stream;
        }

        public bool AddOrUpdate(String Key, byte[] Content)
        {


            var keyBytes = Encoding.UTF8.GetBytes(Key);
            long Length = keyBytes.Length + Content.Length;


            var emptyNode = FindEmptyNode(Length);
            QueryNodes(node =>
            {
                if (node.Key == Key)
                {
                    if (node.Length >= Length)
                    {
                        emptyNode = node;
                    }
                    else
                    {
                        DeleteNode(node);
                    }
                    return false;
                }
                return true;
            });

            if (emptyNode == null)
            {
                emptyNode = CreateLinkNodeBlockData(Key, Content.Length);
            }
            SetPositionLock(emptyNode.StartPosition, () =>
            {
                _Stream.Write(new byte[emptyNode.Length]);
                _Stream.Position = emptyNode.StartPosition;
                _Stream.Write(emptyNode.ToBytes(SplitCodes));
                _Stream.Write(Content);
            });
            return true;
        }
        private void QueryNodes(Func<LinkNodeBlockData, bool> NodeQueryed, long StartPos = 0)
        {
            LinkNodeBlockData result = new LinkNodeBlockData();
            byte[] readBytes = new byte[1024];
            int readLen = -1;
            SetPositionLock(StartPos, () =>
            {
                long StartPosition = StartPos;
                while ((readLen = _Stream.Read(readBytes)) != 0)
                {
                    var readedString = Encoding.UTF8.GetString(readBytes,0,readLen);
                    int SplitCount = 4;//Header+Content的分割数量
                    int SplitConnection = 0;
                    List<char> ReadedDatas = new List<char>();
                    for (int i = 0; i < readedString.Length; i++)
                    {
                        var readChar= readedString[i];
                        if (readChar == SplitCodes[0])
                        {

                        }
                        else
                        {
                            ReadedDatas.Add(readChar);
                        }

                    }
                }
            });

        }
        public bool Delete(string Key)
        {
            var result = false;
            QueryNodes((n) =>
            {
                if (n.Key == Key)
                {
                    result = DeleteNode(n);
                }
                return Key != n.Key;
            });
            return result;
        }
        private LinkNodeBlockData QueryNode(string Key)
        {
            LinkNodeBlockData data = null;
            QueryNodes((n) =>
            {
                if (n.Key == Key)
                {
                    data = n;
                    return false;
                }
                return true;
            });
            return data;
        }
        public byte[]? Query(string Key)
        {
            byte[] bs = null;
            var n = QueryNode(Key);
            if (n == null) return null;
            bs = new byte[n.ContentLength];
            SetPositionLock(n.StartPosition, () =>
            {
                _Stream.Read(bs, 0, bs.Length); 
            });
            return bs;
        }
        private bool DeleteNode(LinkNodeBlockData n)
        {
            bool r = false;
            SetPositionLock(n.StartPosition, () =>
            {
                n.DataLength = 0;
                n.Key = "NONE";
                _Stream.Write(Encoding.UTF8.GetBytes(n.ToString()));
                r = true;
            });
            return r;
        }
        private LinkNodeBlockData CreateLinkNodeBlockData(String Key, long DataLength)
        {
            LinkNodeBlockData node = null;
            SetPositionLock(_Stream.Length, () =>
            {
                node = new LinkNodeBlockData()
                {
                    Key = Key,
                    DataLength = DataLength,
                    ContentLength = DataLength+ CreateNodeAddSize,
                    HeaderLength = Encoding.UTF8.GetByteCount(Key),
                    StartPosition = _Stream.Position

                };
            });
            return node;
        }
        private void SetPositionLock(long NewPosition, Action ExecuteCodeAct)
        {
            lock (_PositionObjectLock)
            {
                _Stream.Position = NewPosition;
                ExecuteCodeAct();
            }
        }

        public LinkNodeBlockData? FindEmptyNode(long NeedTotalLength)
        {
            return EmptyNodes.Where(w => w.Length >= NeedTotalLength).FirstOrDefault();
        }
    }

}
