using QBlockData.DataStructs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBlockData
{
    public class MemoryBlockData : BlockDataMemoryBoxController,IDisposable
    {
        private Dictionary<string, string> Keys = new Dictionary<string, string>();
        public MemoryBlockData(int BlockSize = 50) : base(new BlockDataMemoryBox(new MemoryStream()), BlockSize)
        {
        }

        public override bool Add(string Key, byte[] Data)
        {
            if (HasKey(Key)) return false;
            var ms = FindEmptyMemorys(Data.Length);
            var ls = ms.ToIdList();
            Content.Write(ms, BlockSize, Data);
            if (Data.Length % BlockSize != 0)
            {
                ls += "." + (Data.Length % BlockSize);
            }
            Keys.Add(Key, ls);
            return true;
        }

        public override bool Delete(string Key)
        {
            if (!HasKey(Key)) return false;
            IdStringListToRealIdListBI(Keys[Key], (d) =>
            {
                EmptyBlockIndexs.Push(d.Index);
            });
            Keys.Remove(Key);
            return true;
        }

        public override IEnumerator<string> GetEnumerator()
        {
            return Keys.Keys.GetEnumerator();
        }

        public override IEnumerable<string> GetKeys()
        {
            return Keys.Keys;
        }

        public override bool HasKey(string Key)
        {
            return Keys.ContainsKey(Key);
        }

        public override byte[] Query(string Key)
        {
            MemoryStream ms = new MemoryStream();
            if (!HasKey(Key)) return null;
            IdStringListToRealIdList(Keys[Key], (i) =>
            {
                if (i.IndexOf('.') != -1)
                {
                    var t = i.Split('.');
                    int index = int.Parse(t[0]);
                    int size = int.Parse(t[1]);
                    ms.Write(Content.Read(index * BlockSize, size));
                }
                else
                {
                    int iss = int.Parse(i);
                    ms.Write(Content.Read(iss * BlockSize, BlockSize));
                }
            });
            return ms.ToArray();
        }
    }
}
