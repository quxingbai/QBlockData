using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBlockData.DataStructs
{
    /// <summary>
    /// 数据缓存管理
    /// </summary>
    public class BlockDataTemp:IDisposable
    {
        private MemoryBlockData Data = null;
        private long MaxTempSize = 0;
        private long NowTempSize = 0;
        //private Queue<(string,long)> Keys = new Queue<(string,long)>();
        private Dictionary<string, long> Keys = new Dictionary<string, long>();
        private List<string> KeysQueue = new List<string>();
        public event Action<IEnumerable<string>> DeletedKeys = null;
        public BlockDataTemp(long MaxTempSize)
        {
            this.MaxTempSize = MaxTempSize;
        }
        public void AddOrUpdate(string Key, byte[] TempData)
        {
            if (Data == null) Data = new MemoryBlockData(TempData.Length + 10);
            if (Keys.ContainsKey(Key))
            {
                Delete(Key);
            }
            Keys.Add(Key, TempData.Length);
            KeysQueue.Add(Key);
            Data.AddOrUpdate(Key, TempData);
            NowTempSize = Keys.Sum(s => s.Value);
            List<string> KeysToRemove = new List<string>();
            while (NowTempSize > MaxTempSize && KeysQueue.Count > 1)
            {
                var k = KeysQueue.First();
                Delete(k);
                KeysToRemove.Add(k);
            }
            if (KeysToRemove.Count > 0)
            {
                DeletedKeys?.Invoke(KeysToRemove);
            }
        }
        public byte[]? Query(string key)
        {
            return Data?.Query(key);
        }
        public bool Delete(string key)
        {
            if (Data != null && Keys.ContainsKey(key))
            {
                var dataLen = Keys[key];
                Keys.Remove(key);
                Data.Delete(key);
                KeysQueue.Remove(key);
                NowTempSize -= dataLen;
                return true;
            }
            return false;
        }
        public long GetUsingTempSize() => NowTempSize;
        public long GetUsingTempRealMemorySize() => Data.ContentBlockCount * Data.BlockSize;

        public void Dispose()
        {
            Data.Dispose();
        }
    }
}
