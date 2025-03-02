using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBlockData.DataStructs
{
    public class BlockDataMemoryBox:IDisposable
    {
        protected object _PositionObjectLock = new object();
        protected Stream Source = null;
        public long FileSize => Source.Length;
        public BlockDataMemoryBox(Stream Stream)
        {
            this.Source = Stream;
        }

        public (long, long) FillBytes(long Start, int Length)
        {
            //Source.Position = Start;
            byte[] bs = new byte[Length];
            SetPositionLock(Start, () =>
            {
                Source.Write(bs, 0, Length);
            });
            return (Start, Source.Position);
        }
        public byte[] Read(long Start, int Length)
        {
            //Source.Position = Start;
            byte[] bs = new byte[Length];
            SetPositionLock(Start, () =>
            {
                Source.Read(bs, 0, Length);
            });
            return bs;
        }
        public void Write(long Start, byte[] Data)
        {
            //Source.Position = Start;
            SetPositionLock(Start, () =>
            {
                Source.Write(Data);
            });
        }
        public void Write(BlockMemory StartData, int BlockSize, byte[] Data)
        {
            int DataPackIndex = 0;
            Queue<byte[]> DataPacks = new Queue<byte[]>();
            for (int i = 0; i < Data.Length; i += BlockSize)
            {
                byte[] bs = new byte[BlockSize];
                for (int ii = 0; ii < bs.Length; ii++)
                {
                    if (i + ii < Data.Length) bs[ii] = Data[i + ii];
                }
                DataPacks.Enqueue(bs);
            }
            StartData.ForeachNexts(b =>
            {
                //Source.Position = BlockSize * b.BlockIndex;
                SetPositionLock(BlockSize * b.BlockIndex, () =>
                {
                    Source.Write(DataPacks.Dequeue());
                    DataPackIndex++;
                });
            });
        }
        public void FlushStream()
        {
            Source.Flush();
        }
        private void SetPositionLock(long NewPosition, Action ExecuteCodeAct)
        {
            lock (_PositionObjectLock)
            {
                Source.Position = NewPosition;
                ExecuteCodeAct();
            }
        }

        public void Dispose()
        {
            Source.Dispose();
        }
    }
}
