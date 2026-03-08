using QBlockData.Sockets.TLVSockets;
using QBlockData.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBlockData.DataStructs.QRDataBase
{
    public class QRDataBase : IDisposable
    {
        public String Name { get; private set; }
        public DateTime CreateDate { get; private set; }
        private IEnumerable<QDataBaseTableHeader> TableHeaders { get; set; }

        private TLVPointData DataBaseInfomationFileData = null;
        private FileStream DataBaseInfoStream = null;
        private FileStream DataContentStream = null;
        private Dictionary<string, QDataBaseTableHeader> Tables = new Dictionary<string, QDataBaseTableHeader>();
        public QRDataBase(string DataBaseFilePath)
        {
            var fileStr = new FileStream(DataBaseFilePath + ".qdb", FileMode.OpenOrCreate);
            DataBaseInfoStream = fileStr;
            DataBaseInfomationFileData = new TLVPointData(fileStr);
        }
        #region datastr

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
        private class QDataBaseTableHeader
        {
            public String Name { get; set; }
            public IEnumerable<QDataBaseTableVertion> ColumnVertions { get; set; }
            public IEnumerable<MemoryPointr> DataRangePointers { get; set; }

        }
        public class QDataBaseTableVertion
        {
            public int? Vertion { get; set; } = null;
            public IEnumerable<QDataBaseTableColumn> Columns { get; set; }
        }
        public class QDataBaseTableColumn
        {
            public String Name { get; set; }
            public TLVDataTags DataType { get; set; }
            public Dictionary<string, object> DataTags = new Dictionary<string, object>();
            public bool IsDeleted() => DataTags.ContainsKey("IsDeleted");
            public bool CanWriteNull() => DataTags.ContainsKey("WriteNull");
            public object GetDefaultValue() => DataTags["Default"];

            public void SetDelete()
            {
                DataTags.Add("IsDeleted", "");
            }
            public void SetWriteNull(bool CanWrite)
            {
                if (CanWrite)
                {
                    DataTags.Add("WriteNull", "");
                }
                else
                {
                    DataTags.Remove("WriteNull");
                }
            }
            public void SetDefaultValue(object data)
            {
                var bs = TLVData.ObjectToBytes(DataType, data);
                var val = TLVData.ReadTo(DataType, bs);
                DataTags.Add("Default", val);
            }
            public override string ToString()
            {
                char splitChar = ',';
                StringBuilder sb = new StringBuilder();
                sb.Append(Name);
                sb.Append(splitChar);
                sb.Append((int)DataType);
                sb.Append(splitChar);
                bool writeSplit = false;
                foreach (var i in DataTags)
                {
                    if (writeSplit)
                    {
                        sb.Append('|');
                    }
                    sb.Append(i.Key);
                    sb.Append("=");
                    sb.Append(i.Value);
                    writeSplit = true;
                }
                return sb.ToString();
            }
            public static QDataBaseTableColumn FromStringFormat(string Text)
            {
                var d = Text.Split(',');
                var dt = new QDataBaseTableColumn()
                {
                    Name = d[0],
                    DataType = (TLVDataTags)int.Parse(d[1]),
                };
                foreach (var i in d[3].Split('|'))
                {
                    var kv = i.Split('=');
                    dt.DataTags.Add(kv[0], TLVData.ReadTo(dt.DataType, Encoding.UTF8.GetBytes(kv[1])));
                }
                return dt;
            }
        }
        #endregion


        public void Dispose()
        {
            DataBaseInfomationFileData.Dispose();
        }

    }
}
