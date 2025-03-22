using QBlockData.DataStructs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QBlockData
{
    public class BaseTypeFileBlockDataShell
    {
        protected BlockDataMemoryBoxController Target = null;
        /// <summary>
        /// 读写任意类型
        /// </summary>
        /// <param name="Target">想要使用的Controller类型</param>
        public BaseTypeFileBlockDataShell(BlockDataMemoryBoxController Target)
        {
            this.Target = Target;
        }
        public bool AddString(String Key, String Value)
        {
            return Target.Add(Key, Encoding.UTF8.GetBytes(Value));
        }
        public bool AddOrUpdateString(String Key, String Value)
        {
            return Target.AddOrUpdate(Key, Encoding.UTF8.GetBytes(Value));
        }
        public string? QueryString(String Key)
        {
            var q = Target.Query(Key);
            return q == null ? null : Encoding.UTF8.GetString(q);
        }
        public bool AddInt32(String Key, int Value) => AddString(Key, Value.ToString());
        public int? QueryInt(String Key)
        {
            var q = QueryString(Key);
            return q == null ? null : int.Parse(q);
        }
        public bool AddInt64(String Key, long Value) => AddString(Key, Value.ToString());
        public long? QueryInt64(String Key)
        {
            var q = QueryString(Key);
            return q == null ? null : long.Parse(q);
        }
        public bool AddDouble(String Key, double Value) => AddString(Key, Value.ToString());
        public double? QueryDouble(String Key)
        {
            var q = QueryString(Key);
            return q == null ? null : double.Parse(q);
        }
        public bool AddStringArray(String Key, IEnumerable<string> Array, string SplitCode = ",")
        {
            StringBuilder sb = new StringBuilder();
            foreach (string i in Array)
            {
                if (sb.Length != 0) sb.Append(SplitCode);
                sb.Append(i);
            }
            return AddString(Key, sb.ToString());
        }
        public string[]? QueryStringArray(string Key, string SplitCode = ",")
        {
            var qs = QueryString(Key);
            if (qs == null) return null;
            List<String> ls = new List<string>();
            foreach (var i in qs.Split(SplitCode))
            {
                ls.Add(i);
            }
            return ls.ToArray();
        }
        /// <summary>
        /// 添加一个Class Property不能有Array
        /// </summary>
        public bool AddClass(String Key, object Data, String SplitCode = ",")
        {
            List<string> ls = new List<string>();
            foreach (var i in Data.GetType().GetProperties())
            {
                ls.Add(i.GetValue(Data).ToString());
            }
            return AddStringArray(Key, ls, SplitCode);
        }
        public T QueryClass<T>(String Key, String SplitCode = ",")
        {
            var props = typeof(T).GetProperties();
            var data = QueryStringArray(Key, SplitCode);
            if (data == null) return default(T);
            T t = Activator.CreateInstance<T>();
            if (data.Length < props.Length) return t;
            int counter = 0;
            foreach (var i in t.GetType().GetProperties())
            {
                object val = data[counter++];
                if (i.PropertyType == typeof(int))
                {
                    val = int.Parse(val.ToString());
                }
                else if (i.PropertyType == typeof(double))
                {
                    val = double.Parse(val.ToString());
                }
                else if (i.PropertyType == typeof(bool))
                {
                    val = bool.Parse(val.ToString());
                }
                else if (i.PropertyType == typeof(long))
                {
                    val = long.Parse(val.ToString());
                }
                else if (i.PropertyType == typeof(float))
                {
                    val = float.Parse(val.ToString());
                }
                i.SetValue(t, val);
                if (counter == data.Length)
                {
                    break;
                }
            }
            return t;
        }
        public TLVData QueryTlvData(string Key)
        {
            var bs = Target.Query(Key);
            return TLVData.Deserialization(bs);
        }
        public bool AddOrUpdateTlvDatas(string Key, IEnumerable<TLVData> Data)
        {
            MemoryStream ms = new MemoryStream();
            foreach (var i in Data)
            {
                ms.Write(i.Serialization());
            }
            var r = Target.AddOrUpdate(Key, ms.ToArray());
            ms.Dispose();
            return r;
        }
        public bool Delete(String Key) => Target.Delete(Key);
        public bool UpdateString(String Key, String Value) => Target.Update(Key, Encoding.UTF8.GetBytes(Value));
        public BlockDataMemoryBoxController GetControllerTarget() => Target;
    }
}
