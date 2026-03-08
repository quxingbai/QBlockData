using QBlockData.DataStructs;
using QBlockData.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QBlockData
{
    public class TLVPointData : BlockDataMemoryBoxController, IDisposable
    {
        private class MemoryPointr
        {
            public bool IsEmpty { get; set; }
            public long BlockLength { get; set; }
            public long PointerStartPosition { get; set; }
            public long PointerEndPosition { get; set; }

            public long GetContentEnd()
            {
                return PointerEndPosition + BlockLength;
            }
        }
        private Stream _Stream = null;
        private int BlockSize = 1024;
        //private List<MemoryPointr> EmptyPointers = new List<MemoryPointr>();
        private Dictionary<long, MemoryPointr> EmptyPointers = new Dictionary<long, MemoryPointr>();
        private Dictionary<string, long> MemoryPointerKeyValue = new Dictionary<string, long>();
        public TLVPointData(Stream UsingStream) : base(null, 0)
        {
            _Stream = UsingStream;
        }
        public TLVPointData(String FileName) : base(null, 0)
        {
            FileInfo f = new FileInfo(FileName);
            f.Directory.Create();
            _Stream = new FileStream(FileName, FileMode.OpenOrCreate);
        }


        /// <summary>
        /// 创建或者找一块空白数据
        /// </summary>
        private MemoryPointr FindOrCreateEmptyDataPointer(long NeedSize)
        {
            lock (EmptyPointers)
            {
                long findedKey = -1;
                foreach (var k in EmptyPointers.OrderBy(o => o.Value.BlockLength).Select(s => s.Key))
                {
                    if (EmptyPointers[k].BlockLength >= NeedSize)
                    {
                        findedKey = k;
                    }
                }
                if (findedKey != -1)
                {
                    var d = EmptyPointers[findedKey];
                    EmptyPointers.Remove(findedKey);
                    return d;
                }
            }
            var p = WriteMemoryPointer(_Stream.Length, true, NeedSize);
            long w = NeedSize;
            int blockSize = 1048576;
            SetStreamPosition(_Stream.Length, () =>
            {
                while (w >= blockSize)
                {
                    _Stream.Write(new byte[blockSize]);
                    w -= blockSize;
                }
                if (w != 0) _Stream.Write(new byte[w]);
            });
            return p;
        }
        /// <summary>
        /// 写入一个数据指针
        /// </summary>
        private MemoryPointr WriteMemoryPointer(long StreamPositionStart, bool IsEmpty, long DataLength)
        {
            MemoryPointr mp = null;
            Queue<TLVData> pointTlv = new Queue<TLVData>();
            pointTlv.Enqueue(new TLVData(TLVDataTags.Int, IsEmpty ? 0 : 1));
            pointTlv.Enqueue(new TLVData(TLVDataTags.Long, DataLength));
            var writeInfo = WriteTlvDatas(StreamPositionStart, pointTlv);
            mp = new MemoryPointr()
            {
                IsEmpty = IsEmpty,
                PointerStartPosition = writeInfo.Item1,
                BlockLength = DataLength,
                PointerEndPosition = writeInfo.Item2
            };
            return mp;
        }
        /// <summary>
        /// 设置数据指针是否为空
        /// </summary>
        private MemoryPointr SetMemoryPointerState(MemoryPointr Pointer, bool IsEmpty)
        {
            return WriteMemoryPointer(Pointer.PointerStartPosition, IsEmpty, Pointer.BlockLength);
        }
        private void ReadMemoryPointers(long Start, Func<MemoryPointr, bool> PointerResult)
        {
            bool next = true;
            SetStreamPosition(Start, () =>
            {
                Queue<TLVData> queue = new Queue<TLVData>();
                TLVData.DeserializationFromStream(_Stream, (d =>
                {
                    queue.Enqueue(d);
                    if (queue.Count == 2)
                    {
                        var rdd = new MemoryPointr()
                        {
                            IsEmpty = queue.Dequeue().ReadToInt() == 1,
                            BlockLength = queue.Dequeue().ReadToLong(),
                            PointerStartPosition = Start,
                            PointerEndPosition = _Stream.Position
                        };
                        next = PointerResult.Invoke(rdd);
                        _Stream.Position = rdd.GetContentEnd();
                        //如果是空的就给塞进去空列表 Key是当前的所在Start
                        if (!EmptyPointers.ContainsKey(rdd.PointerStartPosition))
                        {
                            EmptyPointers.Add(rdd.PointerStartPosition, rdd);
                        }
                    }
                }), () => next);
            });
        }
        private void SetStreamPosition(long Pos, Action Execute)
        {
            lock (_Stream)
            {
                _Stream.Position = Pos;
                Execute();
            }
        }
        /// <summary>
        /// 在指定位置写一组TlvData
        /// </summary>
        private (long, long) WriteTlvDatas(long StartPosition, IEnumerable<TLVData> Datas)
        {
            var start = _Stream.Position; long end = 0;
            SetStreamPosition(StartPosition, () =>
            {
                foreach (var i in Datas)
                {
                    var d = i.Serialization();
                    _Stream.Write(d);
                    end += d.Length;
                }
                end += start;
            });
            return (start, end);
        }
        private (string, Lazy<byte[]>)? ReadMemoryPointerData(MemoryPointr Pointer)
        {
            if (Pointer.IsEmpty) return null;
            Lazy<byte[]> rd = null;
            string key = null;
            SetStreamPosition(Pointer.PointerEndPosition, () =>
            {
                TLVData.DeserializationFromStream(_Stream, (d) =>
                {
                    key = d.ReadToString();
                    var rstart = _Stream.Position;
                    rd = new Lazy<byte[]>(() =>
                    {
                        byte[] bs = null;
                        SetStreamPosition(rstart, () =>
                        {
                            TLVData.DeserializationFromStream(_Stream, (data) =>
                            {
                                bs = data.ReadToBytes();
                            }, () => false);
                        });
                        return bs;
                    });
                }, () => false);
            });
            return key == null ? null : (key, rd);
        }
        private void QueryKeyValueDatas(Func<MemoryPointr, (string, Lazy<byte[]>), bool> QueryResult,long Start=0)
        {
            ReadMemoryPointers(Start, pointer =>
            {
                bool n = true;
                if (pointer.IsEmpty)
                {

                }
                else
                {
                    var data = ReadMemoryPointerData(pointer);
                    if (data != null)
                    {
                        n = QueryResult.Invoke(pointer, data.Value);
                    }
                }
                return n;
            });
        }
        public override bool Add(string Key, byte[] Data)
        {
            Queue<TLVData> queue = new Queue<TLVData>();
            queue.Enqueue(new TLVData(TLVDataTags.String, Key));
            queue.Enqueue(new TLVData(TLVDataTags.Bytes, Data));
            MemoryStream ms = new MemoryStream();
            foreach (var i in queue)
            {
                ms.Write(i.Serialization());
            }
            var pointer = FindOrCreateEmptyDataPointer(ms.Length);
            WriteTlvDatas(pointer.PointerEndPosition, queue);
            if (IsUsingTemp)
            {
                DataTempSetting.AddOrUpdate(Key, Data);
            }
            return true;
        }


        public override bool Delete(string Key)
        {
            var r = QueryResult(Key);
            if (r == null) return false;
            var pointer = r.Value.Item1;
            SetMemoryPointerState(pointer, true);
            if (IsUsingTemp)
            {
                DataTempSetting.Delete(Key);
            }
            return true;
        }

        public override byte[] Query(string Key)
        {
            var r = QueryResult(Key);
            return r == null ? null : r.Value.Item2.Item2.Value;
        }
        /// <summary>
        /// 遍历所有内容
        /// </summary>
        /// <param name="QueryResult">返回True继续 返回False终止</param>
        public void QueryForeach(Func<string, Lazy<byte[]>?, bool> QueryResult)
        {
            QueryKeyValueDatas((p, d) =>
            {
                return QueryResult(d.Item1,d.Item2);
            });
        }
        private (MemoryPointr, (string, Lazy<byte[]>))? QueryResult(String Key)
        {
            (MemoryPointr, (string, Lazy<byte[]>))? r = null;
            QueryKeyValueDatas((p, d) =>
            {
                if (d.Item1 == Key)
                {
                    r = (p, d);
                    return false;
                }
                return true;
            });
            return r;
        }
        public override bool HasKey(string Key)
        {
            if (IsUsingTemp)
            {
                var d = DataTempSetting.Query(Key);
                return d != null;
            }
            return QueryResult(Key) != null;
        }

        public override IEnumerable<string> GetKeys()
        {
            List<string> keys = new List<string>();
            QueryKeyValueDatas((p, d) =>
            {
                keys.Add(d.Item1);
                return true;
            });
            return keys;
        }


        public new void Dispose()
        {
            _Stream.Dispose();
            DataTempSetting.Dispose();
            base.Dispose();
        }
    }
}
