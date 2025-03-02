using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace QBlockData.DataStructs
{

    public abstract class BlockDataMemoryBoxController : IEnumerable<string>, IDisposable
    {
        public int BlockSize { get; set; }
        public long ContentBlockCount => Content.FileSize / BlockSize;//内容一共被分为多少个内存块
        protected Stack<int> EmptyBlockIndexs = new Stack<int>();
        protected BlockDataMemoryBox Content = null;
        protected BlockDataTemp DataTempSetting = null;

        public bool IsUsingTemp => DataTempSetting != null;
        public long UsingTempSize => DataTempSetting.GetUsingTempSize();

        public BlockDataMemoryBoxController(BlockDataMemoryBox MemoryBox, int BlockSize = 50)
        {
            this.BlockSize = BlockSize;
            Content = MemoryBox;
        }
        public BlockDataMemoryBoxController UseQueryTemp(long MaxTempSize)
        {
            DataTempSetting = new BlockDataTemp(MaxTempSize);
            return this;
        }
        /// <summary>
        /// 获取一段空的内存区间
        /// </summary>
        /// <param name="NeedSize"></param>
        /// <returns></returns>
        protected virtual BlockMemory FindEmptyMemorys(long NeedSize)
        {
            //如果是单纯的声明空间
            if (NeedSize == 0)
            {
                return new BlockMemory()
                {
                    BlockIndex = ContentBlockCount
                };
            }
            BlockMemory Result = null;


            long count = NeedSize / BlockSize;
            count += (NeedSize % BlockSize != 0) ? 1 : 0;
            var indexs = CreateOrGetEmptyBlockDataIndex(count);
            BlockMemory nowSetBlock = null;
            foreach (var i in indexs)
            {
                var back = nowSetBlock;
                nowSetBlock = new BlockMemory()
                {
                    BackBlock = back,
                    BlockIndex = i
                };
                if (back != null)
                {
                    back.NextBlock = nowSetBlock;
                }
                if (Result == null)
                {
                    Result = nowSetBlock;
                }
            }
            return Result;


            //BlockMemory Result = null;
            //long count = NeedSize / BlockSize;
            //if (NeedSize % BlockSize != 0)
            //{
            //    count += 1;
            //}
            //while (count > 0 && EmptyBlockIndexs.TryPop(out int emptyBlock))
            //{
            //    var data = new BlockMemory()
            //    {
            //        BlockIndex = emptyBlock,
            //        BackBlock = Result,
            //        NextBlock = null
            //    };
            //    if (Result != null) Result.NextBlock = data;
            //    Result = data;
            //    count--;
            //}
            //if (count == 0) return Result.GetFirst();
            //var totalBlockCount = ContentBlockCount;
            //var idx = totalBlockCount;
            //while (count > 0)
            //{
            //    var data = new BlockMemory()
            //    {
            //        BlockIndex = idx++,
            //        BackBlock = Result,
            //        NextBlock = null
            //    };
            //    if (Result != null) Result.NextBlock = data;
            //    Result = data;
            //    count--;
            //}
            //return Result.GetFirst();
        }
        private IEnumerable<long> CreateOrGetEmptyBlockDataIndex(long count)
        {
            List<long> indexs = new List<long>();
            while (indexs.Count < count && EmptyBlockIndexs.TryPop(out var empty))
            {
                indexs.Add(empty);
            }
            int createOffset = 0;
            while (indexs.Count < count)
            {
                indexs.Add(createOffset + ContentBlockCount);
                createOffset++;
            }
            return indexs;
        }
        public abstract bool Add(String Key, byte[] Data);
        public abstract bool Delete(String Key);
        public virtual bool Update(string Key, byte[] Data)
        {
            if (!Delete(Key)) return false;
            return Add(Key, Data);
        }
        public virtual bool AddOrUpdate(string key, byte[] Data)
        {
            if (HasKey(key)) return Update(key, Data);
            else return Add(key, Data);
        }
        public abstract byte[] Query(String Key);
        public abstract bool HasKey(String Key);
        /// <summary>
        /// 将头文件中存储的BlockIndex描述词转换为每一个对应的块指向Id
        /// </summary>
        /// <param name="IdStringList">元数据</param>
        /// <param name="IdStrRead">转换后读取到的数据 例如1000.5</param>
        protected void IdStringListToRealIdList(String IdStringList, Action<string> IdStrRead)
        {
            var ids = IdStringList.Split(',');
            foreach (var i in ids)
            {
                //如果是连贯内存空间
                if (i.IndexOf('~') != -1)
                {
                    var t = i.Split('~');
                    long start = long.Parse(t[0]), end = 0;
                    int size = BlockSize;
                    if (t[1].IndexOf('.') != -1)
                    {
                        var tt = t[1].Split('.');
                        end = long.Parse(tt[0]);
                        size = int.Parse(tt[1]);
                    }
                    if (start < end)
                    {
                        for (long index = start; index <= end; index++)
                        {
                            IdStrRead(index.ToString() + (index == end ? "." + size : ""));
                        }
                    }
                    else
                    {
                        for (long index = start; index >= end; index--)
                        {
                            IdStrRead(index.ToString() + (index == end ? "." + size : ""));
                        }
                    }

                }
                //如果是单块内存
                else
                {

                    //如果数据块没满
                    if (i.IndexOf('.') != -1)
                    {
                        var t = i.Split('.');
                        int index = int.Parse(t[0]);
                        int size = int.Parse(t[1]);
                        IdStrRead(index + "." + size);
                    }
                    else
                    {
                        int iss = int.Parse(i);
                        IdStrRead(iss.ToString());
                    }
                }
            }

        }
        /// <summary>
        /// 将头文件中存储的BlockIndex描述词转换为每一个对应的块指向Id
        /// 重载的格式化方法
        /// </summary>
        /// <param name="IdStringList">元数据</param>
        /// <param name="IdStrRead">转换后读取到的数据 例如1000.5</param>
        protected void IdStringListToRealIdListBI(String IdStringList, Action<BlockId> IdStrRead)
        {
            BlockId temp = new BlockId();
            IdStringListToRealIdList(IdStringList, (str) =>
            {
                temp.UpdateSource(str);
                IdStrRead(temp);
            });

        }

        /// <summary>
        /// 尝试设置一个缓存数据
        /// </summary>
        protected bool TempDataTrySet(string key, byte[] TempData)
        {
            if (DataTempSetting == null) throw new Exception("使用缓存功能需要调用 UseQueryTemp 方法");
            DataTempSetting?.AddOrUpdate(key, TempData);
            return true;
        }
        /// <summary>
        /// 尝试删除一个缓存数据
        /// </summary>
        protected bool TempDataTryDelete(string key)
        {
            if (DataTempSetting == null) throw new Exception("使用缓存功能需要调用 UseQueryTemp 方法");
            return DataTempSetting.Delete(key);
        }
        /// <summary>
        /// 尝试读取缓存数据
        /// </summary>
        protected bool TempDataTryQuery(string key, out byte[] data)
        {
            if (DataTempSetting == null) throw new Exception("使用缓存功能需要调用 UseQueryTemp 方法");
            data = DataTempSetting?.Query(key);
            return data != null;
        }

        public abstract IEnumerable<String> GetKeys();
        public virtual void Dispose()
        {
            if (IsUsingTemp)
            {
                DataTempSetting.Dispose();
            }
            Content.Dispose();

        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetKeys().GetEnumerator();
        }
        public virtual IEnumerator<string> GetEnumerator()
        {
            return GetKeys().GetEnumerator();
        }
    }
}
