using QBlockData.DataStructs;
using QBlockData.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

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
                    if (i.IndexOf('~') != -1)
                    {
                        var data = i.Split('~');
                        int Start = int.Parse(data[0]);
                        int End = int.Parse(data[1]);
                        for (int it = Start; it <= End; it++)
                        {
                            base.EmptyBlockIndexs.Push(it);
                        }
                    }
                    else
                    {
                        base.EmptyBlockIndexs.Push(int.Parse(i));
                    }
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
            if (IsUsingTemp)
            {
                TempDataTrySet(Key, Data);
            }
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
            if (IsUsingTemp)
            {
                TempDataTryDelete(Key);
            }
            return true;
        }
        protected void Save()
        {
            XmlSource.Save(FileName);
        }
        public override byte[]? Query(string Key)
        {
            if (IsUsingTemp && TempDataTryQuery(Key, out var data))
            {
                return data;
            }
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
            var queryResult = ms.ToArray();
            if (IsUsingTemp)
            {
                TempDataTrySet(Key, queryResult);
            }
            return queryResult;
        }

        public override bool HasKey(string Key)
        {
            return HasKeyNode(Key) != null;
        }

        protected virtual XmlNode? HasKeyNode(string Key)
        {

            //if (_NODE_Keys.ChildNodes.Count != 0)
            //{
            //    var keyNodes = _NODE_Keys.ChildNodes;
            //    int leftPointer = 0, rightPointer = keyNodes.Count - 1;
            //    do
            //    {
            //        if (leftPointer > rightPointer) return null;
            //        var l = _NODE_Keys.ChildNodes[leftPointer];
            //        if (l.Attributes["Key"].InnerText == Key) return l;
            //        var r = _NODE_Keys.ChildNodes[rightPointer];
            //        if (r.Attributes["Key"].InnerText == Key) return r;
            //        leftPointer++;
            //        if (leftPointer == rightPointer) return null;
            //        rightPointer--;

            //    } while (true);
            //}
            //return null;


            foreach (XmlNode n in _NODE_Keys.ChildNodes)
            {
                if (n.Attributes["Key"].InnerText == Key) return n;
            }
            return null;

            //这个方法是先从头部找几个 没找到再从尾部找，也就是双指针 数据在中间是最慢。
            //干活好像有点什么问题
            //bool findType = true;
            //var keyNodes = _NODE_Keys.ChildNodes;
            //int findLeft = 0, findRight = keyNodes.Count - 1;
            //int findCount = 0;
            //while (findLeft <= findRight)
            //{
            //    var findNode = keyNodes[findType ? findLeft : findRight];
            //    if (findNode.Attributes["Key"].InnerText == Key) return findNode;
            //    findLeft += (findType ? 1 : 0);
            //    findRight += (findType ? 0 : -1);
            //    findCount++;
            //    if (findCount == 5)
            //    {
            //        findType = !findType;
            //        findCount = 0;
            //    }
            //}

            return null;
        }

        public override IEnumerator<string> GetEnumerator()
        {
            return GetKeys().GetEnumerator();
        }

        public override IEnumerable<string> GetKeys()
        {
            List<string> ks = new List<string>();
            foreach (XmlNode item in _NODE_Keys)
            {
                ks.Add(item.Attributes["Key"].Value);
            }
            return ks;
        }
    }
}
