using QBlockData.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBlockData.DataStructs
{
    public enum TLVDataTags
    {
        Int, Long, Double, String, Bytes, Float, DateTime
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

        public byte[] Serialization()
        {
            return Serialization(this);
        }
        public override string ToString()
        {
            return $"Tag: {Tag}, Length: {Length}, Data: {BitConverter.ToString(Data)}";
        }
        public static byte[] Serialization(TLVData DataSource)
        {
            var tag = (byte)(int)DataSource.Tag;
            var len = DataUtils.EncodeVarintBytes(DataSource.Length).ToArray();
            var data = DataSource.Data;
            byte[] result = new byte[1 + len.Length + data.Length];
            result[0] = tag;
            len.CopyTo(result, 1);
            data.CopyTo(result, 1 + len.Length);
            return result;
        }
        public static TLVData Deserialization(byte[] DataSource)
        {
            TLVDataTags tag = (TLVDataTags)(int)DataSource[0];
            int position = 1;
            int length = DataUtils.VarintReadStart(() => DataSource[position++]);
            byte[] data = new byte[DataSource.Length - position];
            Array.Copy(DataSource, position,data,0, data.Length);
            return new TLVData()
            {
                Data = data,
                Tag = tag,
            };
        }
        public static void DeserializationFromStream(Stream DataStream, Action<TLVData> ReadedResult)
        {
            while (true)
            {
                var read = DataStream.ReadByte();
                if (read == -1) break;
                TLVDataTags tag = (TLVDataTags)read;
                int length = DataUtils.VarintReadStart(() => (byte)DataStream.ReadByte());
                byte[] data = new byte[length];
                DataStream.Read(data, 0, length);
                ReadedResult.Invoke(new TLVData()
                {
                    Data = data,
                    Tag = tag,
                });
            }
        }
    }

}
