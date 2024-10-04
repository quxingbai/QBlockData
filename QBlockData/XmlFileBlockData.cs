using QBlockData.DataStructs;
using QBlockData.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace QBlockData
{
    public class XmlFileBlockData : BlockDataMemoryBoxController
    {
        protected XmlDocument XmlSource = new XmlDocument();
        protected XmlNode _NODE_Infomation => XmlSource["Content"]["Infomation"];
        protected XmlNode _NODE_EmptyBlocks => XmlSource["Content"]["EmptyBlocks"];
        protected XmlNode _NODE_Keys => XmlSource["Content"]["Keys"];
        protected string FileName = null;
        public XmlFileBlockData(string FileName, int BlockSize = 50) : base(new BlockDataMemoryBox(File.Open(FileName + ".bdmb", FileMode.OpenOrCreate)), BlockSize)
        {
            this.FileName = FileName;
            if (!File.Exists(FileName))
            {
                var d = XmlSource.CreateXmlDeclaration("1.0", "utf-8", "");
                XmlSource.AppendChild(d);
                var c = XmlSource.CreateElement("Content");
                XmlSource.AppendChild(c);
                var i = XmlSource.CreateElement("Infomation");
                c.AppendChild(i);
                i.InnerText = (QBlockDataVersion.toString());
                var e = XmlSource.CreateElement("EmptyBlocks");
                c.AppendChild(e);
                var k = XmlSource.CreateElement("Keys");
                c.AppendChild(k);
            }
            else
            {
                XmlSource.Load(FileName);
                foreach (var i in _NODE_EmptyBlocks.InnerText.Split(','))
                {
                    if (i == "") continue;
                    base.EmptyBlockIndexs.Push(int.Parse(i));
                }
            }
        }
        public override bool Add(string Key, byte[] Data)
        {
            if (HasKey(Key)) return false;
            if (Data.Length == 0) Data = new byte[1];
            var memorys = FindEmptyMemorys(Data.Length);
            var keyNode = XmlSource.CreateElement("K");
            var keynodeKey = XmlSource.CreateAttribute("Key");
            keynodeKey.InnerText = Key;
            keyNode.Attributes.Append(keynodeKey);
            keyNode.InnerText = memorys.ToIdList();
            //如果数据和数据块大小不一样就得更新一下id头文件写法
            if (Data.Length % BlockSize != 0)
            {
                keyNode.InnerText += "." + (Data.Length % BlockSize);
            }
            _NODE_Keys.AppendChild(keyNode);
            //向Xml同步EmptyBlockIndexs
            {

                BlockMemory bm = null;
                foreach (var empyt in EmptyBlockIndexs)
                {
                    var b = new BlockMemory() { BackBlock = bm, BlockIndex = empyt };
                    if (bm != null) bm.NextBlock = b;
                    bm = b;
                }
                var idlist = bm == null ? "" : (bm.GetFirst().ToIdList());
                _NODE_EmptyBlocks.InnerText = idlist;
            }
            Content.Write(memorys, BlockSize, Data);
            this.Save();
            Content.FlushStream();
            return true;
        }

        public override bool Delete(string Key)
        {
            var kn = HasKeyNode(Key);
            if (kn == null) return false;
            IdStringListToRealIdList(kn.InnerText, (i) =>
            {
                int index = int.Parse(i.IndexOf('.') != -1 ? i.Split('.')[0] : i);
                EmptyBlockIndexs.Push(index);
            });

            if (_NODE_EmptyBlocks.InnerText != "") _NODE_EmptyBlocks.InnerText += ',';
            //向Xml同步EmptyBlockIndexs
            {
                List<BlockMemory> bmGroups = new List<BlockMemory>();

                BlockMemory bm = null;
                foreach (var empyt in EmptyBlockIndexs)
                {
                    var b = new BlockMemory() { BackBlock = bm, BlockIndex = empyt };
                    if (bm != null) bm.NextBlock = b;
                    bm = b;
                }
                var idlist = bm == null ? "" : (bm.GetFirst().ToIdList());
                _NODE_EmptyBlocks.InnerText = idlist;
            }

            ////直接把被删除的内存空间扔给EmptyBlockIndexs
            //_NODE_EmptyBlocks.InnerText += _NODE_EmptyBlocks.InnerText.Length == 0 ? "" : ",";
            //_NODE_EmptyBlocks.InnerText += kn.InnerText;

            _NODE_Keys.RemoveChild(kn);
            Save();
            return true;
        }
        protected void Save()
        {
            XmlSource.Save(FileName);
        }
        public override byte[]? Query(string Key)
        {
            MemoryStream ms = new MemoryStream();
            var kn = HasKeyNode(Key);
            if (kn == null) return null;
            var ids = kn.InnerText.Split(',');
            IdStringListToRealIdList(kn.InnerText, (i) =>
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

        public override bool HasKey(string Key)
        {
            return HasKeyNode(Key) != null;
        }

        protected virtual XmlNode? HasKeyNode(string Key)
        {
            foreach (XmlNode n in _NODE_Keys.ChildNodes)
            {
                if (n.Attributes["Key"].InnerText == Key) return n;
            }
            return null;
        }

    }
}
