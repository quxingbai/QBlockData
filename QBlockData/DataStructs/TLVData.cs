using QBlockData.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBlockData.DataStructs
{
    public enum TLVDataTags
    {
        Int, Long, Double, String, Bytes, Short, DateTime,
        /// <summary>
        /// 数据长度标识1=True 0=false
        /// </summary>
        Boolean
    }
    public class TLVData
    {
        public byte[] Data { get; set; }
        public TLVDataTags Tag { get; set; }
        public int Length => Data.Length;

        public TLVData()
        {
            Data = Array.Empty<byte>(); // 确保 Data 不为 null
        }
        public TLVData(TLVDataTags tag, object data)
        {
            this.Tag = tag;
            this.Data = data == null ? Array.Empty<byte>() : ObjectToBytes(tag, data);
        }
        public byte[] Serialization()
        {
            return Serialization(this);
        }
        public override string ToString()
        {
            return $"Tag: {Tag}, Length: {Length}, Data: {(Tag == TLVDataTags.Bytes ? BitConverter.ToString(Data) : ReadTo().ToString())}";
        }
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (!(obj is TLVData)) return false;
            var target = obj as TLVData;
            return Length == target.Length && Tag == target.Tag && Data.SequenceEqual(target.Data);
        }
        public static TLVDataTags GetDataType(Object data)
        {
            switch (data.GetType().Name)
            {
                case "Int": return TLVDataTags.Int;
                case "Long": return TLVDataTags.Long;
                case "Double": return TLVDataTags.Double;
                case "String": return TLVDataTags.String;
                case "ByteArray": return TLVDataTags.Bytes;
                case "Short": return TLVDataTags.Short;
                case "DateTime": return TLVDataTags.DateTime;
            }
            throw new("未命名此类型的解析方式 " +data.GetType().Name);
        }
        public int ReadToInt() => (int)ReadTo();
        public long ReadToLong() => (long)ReadTo();
        public double ReadToDouble() => (double)ReadTo();
        public string ReadToString() => ReadTo().ToString();
        public byte[] ReadToBytes() => Data;
        public short ReadToShort() => (short)ReadTo();
        public DateTime ReadToDateTime() => (DateTime)ReadTo();
        public bool ReadToBoolean() => (bool)ReadTo();

        public static byte[] ObjectToBytes(TLVDataTags Tag, object Data)
        {
            byte[] bs = null;
            switch (Tag)
            {
                case TLVDataTags.Int:
                    bs = BitConverter.GetBytes((int)Data);
                    break;
                case TLVDataTags.Long:
                    bs = BitConverter.GetBytes((long)Data);
                    break;
                case TLVDataTags.Double:
                    bs = BitConverter.GetBytes((double)Data);
                    break;
                case TLVDataTags.String:
                    bs = Encoding.UTF8.GetBytes(Data.ToString());
                    break;
                case TLVDataTags.Bytes:
                    bs = (byte[])Data;
                    break;
                case TLVDataTags.Short:
                    bs = BitConverter.GetBytes((float)Data);
                    break;
                case TLVDataTags.DateTime:
                    bs = BitConverter.GetBytes(((DateTime)Data).Ticks);
                    break;
                case TLVDataTags.Boolean:
                    bs = new byte[((bool)Data) ? 1 : 0];
                    break;
                    ;
                default:
                    throw new Exception("数据转换失败");
                    break;
            }
            return bs;
        }
        public object ReadTo(TLVDataTags? ToDataType = null)
        {
            TLVDataTags tag = ToDataType ?? this.Tag;
            return ReadTo(tag,Data);
        }
        public static object ReadTo(TLVDataTags ToDataType, byte[] Data)
        {
            switch (ToDataType)
            {
                case TLVDataTags.Int:
                    return BitConverter.ToInt32(Data);
                case TLVDataTags.Long:
                    return BitConverter.ToInt64(Data);
                case TLVDataTags.Double:
                    return BitConverter.ToDouble(Data);
                case TLVDataTags.String:
                    return Encoding.UTF8.GetString(Data);
                case TLVDataTags.Bytes:
                    return Data;
                case TLVDataTags.Short:
                    return BitConverter.ToInt16(Data);
                case TLVDataTags.DateTime:
                    return new DateTime(BitConverter.ToInt64(Data));
                case TLVDataTags.Boolean:
                    return Data.Length == 1;
            }
            throw new Exception("数据转换失败");
        }
        public static byte[] Serialization(TLVData DataSource)
        {
            var tag = (byte)(int)DataSource.Tag;
            var len = DataUtils.EncodeVarintBytes(DataSource.Length).ToArray();
            var data = DataSource.Data;
            if (data.Length == 0)
            {
                byte[] result = new byte[2];
                result[0] = tag;
                result[1] = 0;
                return result;
            }
            else
            {
                byte[] result = new byte[1 + len.Length + data.Length];
                result[0] = tag;
                len.CopyTo(result, 1);
                data.CopyTo(result, 1 + len.Length);
                return result;
            }
        }

        public static TLVData Deserialization(byte[] DataSource)
        {
            TLVDataTags tag = (TLVDataTags)(int)DataSource[0];
            int position = 1;
            int length = DataUtils.VarintReadStart(() => DataSource[position++]);
            byte[] data = new byte[DataSource.Length - position];
            Array.Copy(DataSource, position, data, 0, data.Length);
            return new TLVData()
            {
                Data = data,
                Tag = tag,
            };



        }
        public static void DeserializationFromStream(Stream DataStream, Action<TLVData> ReadedResult, Func<bool> CanNextRead = null)
        {
            if (DataStream.Length == 0) return;
            while (true)
            {
                var read = DataStream.ReadByte();
                if (read == -1) break;
                TLVDataTags tag = (TLVDataTags)read;
                //int length = DataUtils.VarintReadStart(() => (byte)DataStream.ReadByte());
                int length = 0;
                List<int> bs = new List<int>();
                var rd = 0;
                while ((rd = DataStream.ReadByte()) != -1)
                {
                    bs.Add(rd);
                    if ((rd & 0x80) == 0) break;
                }
                length = DataUtils.DecodeVarintBytes(bs.Select(s => (byte)s));
                byte[] data = new byte[length];
                DataStream.Read(data, 0, length);
                ReadedResult.Invoke(new TLVData()
                {
                    Data = data,
                    Tag = tag,
                });
                if (!(CanNextRead?.Invoke() ?? true)) break;
            }
        }

    }

}
