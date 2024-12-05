using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBlockData.DataStructs
{
    /// <summary>
    /// 数据缓存管理
    /// </summary>
    public class BlockDataTemp
    {
        private MemoryBlockData Data = null;
        private long MaxTempSize = 0;
        private long NowTempSize = 0;
        //private Queue<(string,long)> Keys = new Queue<(string,long)>();
        private Dictionary<string, long> Keys = new Dictionary<string, long>();
        public BlockDataTemp(long MaxTempSize)
        {
            this.MaxTempSize = MaxTempSize;
        }
        public void AddOrUpdate(string Key, byte[] TempData)
        {
            Keys.Add(Key, TempData.Length);
            if (Data == null) Data = new MemoryBlockData(TempData.Length + 10);
            Data.AddOrUpdate(Key, TempData);
            NowTempSize += TempData.Length;
            while (NowTempSize > MaxTempSize&&Keys.Any())
            {
                Delete(Keys.First().Key);
            }
        }
        public byte[]? Query(string key)
        {
            return Data.Query(key);
        }
        public bool Delete(string key)
        {
            if(Keys.ContainsKey(key))
            {
                var dataLen = Keys[key];
                Keys.Remove(key);
                Data.Delete(key);
                NowTempSize-= dataLen;
                return true;
            }
            return false;
        }
        public long GetUsingTempSize() => NowTempSize;
        public long GetUsingTempRealMemorySize() => Data.ContentBlockCount * Data.BlockSize;
    }
}
