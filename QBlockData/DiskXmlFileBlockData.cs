using QBlockData.DataStructs;
using QBlockData.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace QBlockData
{
    /// <summary>
    /// 这个相比于XmlFileBlockData要更节约内存，它不会把数据保存在内存中 而是在磁盘中
    /// </summary>
    public class DiskXmlFileBlockData : BlockDataMemoryBoxController
    {
        private FileStream _SourceFile = null;
        private string _SourceFilePath = null;
        private readonly static XmlWriterSettings _XmlSettings = new XmlWriterSettings() { Indent = true, Encoding = Encoding.UTF8 };
        private object _Lock = new object();
        private Dictionary<string, string> InfomationItems = new Dictionary<string, string>();
        private enum XmlReaderActionTypes
        {
            ToContent,
            ToInfomation,
            ToEmptyBlocks,
            ToKeys,
        }
        private enum XmlReaderCopyActionTypes
        {
            CopyInfomation, CopyEmptyBlocks, CopyKeys
        }
        public DiskXmlFileBlockData(string FileName, int BlockSize = 50) : base(new BlockDataMemoryBox(File.Open(FileName + ".bdmb", FileMode.OpenOrCreate)), BlockSize)
        {
            _SourceFilePath = FileName;
            if (!File.Exists(FileName))
            {
                _SourceFile = File.Create(FileName);
                UseXmlWriter(w => InitSourceWriter(w));
            }
            else
            {
                _SourceFile = new FileStream(FileName, FileMode.Open);
            }
            UseXmlReader(r =>
            {
                _XmlReaderReadformat(r, (readFormat, readType) =>
                {
                    switch (readType)
                    {
                        case XmlReaderActionTypes.ToContent:
                            break;
                        case XmlReaderActionTypes.ToInfomation:
                            {
                                XmlReaderToEnd(readFormat, "Infomation", rd =>
                                {
                                    var rname = rd.Name;
                                    rd.Read();
                                    InfomationItems.Add(rname, rd.Value);
                                }, () => true);
                            }
                            break;
                        case XmlReaderActionTypes.ToEmptyBlocks:
                            {
                                r.Read();
                                foreach (var i in readFormat.Value.Split(','))
                                {

                                    if (i.Trim() == "") continue;
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
                            break;
                        case XmlReaderActionTypes.ToKeys:
                            {

                            }
                            break;
                        default:
                            break;
                    }
                });
            });
            if (InfomationItems.ContainsKey("BlockSize"))
            {
                var bs = InfomationItems["BlockSize"];
                base.BlockSize = int.Parse(bs);
            }
        }
        private void InitSourceWriter(XmlWriter w)
        {
            InitSourceWriterSteps(w, out var a, out var b, out var c, out var d);
            a();
            b();
            c();
            d();
        }
        private void InitSourceWriterSteps(XmlWriter w, out Action InfomationWrite, out Action EmptyBlocksWrite, out Action KeysWrite, out Action CompletAction)
        {
            w.WriteStartDocument();
            w.WriteStartElement("Content");
            InfomationWrite = () =>
            {
                w.WriteStartElement("Infomation");
                w.WriteElementString("BlockSize", base.BlockSize.ToString());
                w.WriteElementString("Vertion", (GetType().Name) + " - " + QBlockDataVersion.toString());
            };
            EmptyBlocksWrite = () =>
            {
                w.WriteEndElement();
                w.WriteStartElement("EmptyBlocks");
            };
            KeysWrite = () =>
            {
                w.WriteEndElement();
                w.WriteStartElement("Keys");
            };
            CompletAction = () =>
            {
                w.WriteEndElement();
                w.WriteEndElement();
                w.WriteEndDocument();
            };

        }
        private void UseXmlWriter(Action<XmlWriter> Writer)
        {
            lock (_Lock)
            {
                _SourceFile.Position = 0;
                XmlWriter writer = XmlWriter.Create(_SourceFile, _XmlSettings);
                Writer.Invoke(writer);
                writer.Dispose();
                _SourceFile.Flush(true);
            }
        }
        private void UseXmlReader(Action<XmlReader> Reader)
        {
            lock (_Lock)
            {
                _SourceFile.Position = 0;
                XmlReader reader = XmlReader.Create(_SourceFile);
                Reader.Invoke(reader);
                reader.Dispose();

            }
        }
        //private void XmlReaderChildrenElements(XmlReader reader, Action<XmlReader> readCallBack, Func<bool> CanNextFind)
        //{
        //    while (reader.Read() && CanNextFind())
        //    {
        //        if (reader.Name == "") continue;
        //        if (reader.NodeType == XmlNodeType.EndElement)
        //        {
        //            break;
        //        }
        //        readCallBack(reader);

        //    }
        //}
        private void XmlReaderToEnd(XmlReader reader, String EndNodeName, Action<XmlReader> readCallBack, Func<bool> CanNextFind)
        {
            while (reader.Read() && CanNextFind())
            {
                if (reader.Name == "") continue;
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == EndNodeName)
                {
                    break;
                }
                if (reader.NodeType != XmlNodeType.EndElement)
                {
                    readCallBack(reader);
                }
            }
        }

        /// <summary>
        /// 根据这个数据结构所封装的一个格式化读取
        /// </summary>
        /// <param name="reader">数据源Xml</param>
        /// <param name="ReaderAction">每次读出来的数据类型，使用的时候要吧reader.Read()然后才能正常拿到数据，因为还在顶级Element。顶级Element可以Copy</param>
        private void _XmlReaderReadformat(XmlReader reader, Action<XmlReader, XmlReaderActionTypes> ReaderAction, Func<bool>? CanNextRead = null)
        {
            while (reader.Read())
            {
                var rb = reader;
                if (reader.Name == "" || reader.NodeType == XmlNodeType.EndElement) continue;
                switch (rb.Name)
                {
                    case "Content":
                        ReaderAction(rb, XmlReaderActionTypes.ToContent);
                        ; break;
                    case "Infomation":
                        ReaderAction(rb, XmlReaderActionTypes.ToInfomation);
                        ; break;
                    case "EmptyBlocks":
                        ReaderAction(rb, XmlReaderActionTypes.ToEmptyBlocks);
                        ; break;
                    case "Keys":
                        ReaderAction(rb, XmlReaderActionTypes.ToKeys);
                        ; break;
                }

            }
        }
        /// <summary>
        /// 执行一个拷贝数据文件的行为
        /// </summary>
        /// <param name="reader">提供数据源Xml</param>
        /// <param name="CopyTo">复制到流</param>
        /// <param name="IsCopyData">选择是否复制这个类型的数据 复制返回True 拒绝返回False，如果拒绝返回就要自己实现复制过程</param>
        /// <returns></returns>
        private Stream _XmlReaderReadformatCopy(XmlReader reader, Stream CopyTo, Func<XmlReader, XmlWriter, XmlReaderCopyActionTypes, bool> IsCopyData)
        {
            Stream stream = CopyTo;
            XmlWriter write = XmlWriter.Create(stream, _XmlSettings);
            _XmlReaderReadformat(reader, (r, t) =>
            {
                switch (t)
                {
                    case XmlReaderActionTypes.ToContent:
                        {
                            write.WriteStartElement("Content");
                        }
                        break;
                    case XmlReaderActionTypes.ToInfomation:
                        if (IsCopyData(r, write, XmlReaderCopyActionTypes.CopyInfomation)) write.WriteNode(r, true);
                        break;
                    case XmlReaderActionTypes.ToEmptyBlocks:
                        if (IsCopyData(r, write, XmlReaderCopyActionTypes.CopyEmptyBlocks)) write.WriteNode(r, true);
                        break;
                    case XmlReaderActionTypes.ToKeys:
                        if (IsCopyData(r, write, XmlReaderCopyActionTypes.CopyKeys)) write.WriteNode(r, true);
                        break;
                    default:
                        break;
                }
            });
            write.WriteEndElement();
            write.Dispose();
            return stream;
        }
        /// <summary>
        /// 创建一个缓存文件 在方法结束后会替换到真正数据文件原本的位置
        /// </summary>
        /// <param name="UsingTempFile">在这里改变文件</param>
        private void StartUpdateXml(Action<FileStream> UsingTempFile)
        {
            var dxmPath = this._SourceFilePath + ".dxs";
            FileStream ms = new FileStream(dxmPath, FileMode.Create);
            UsingTempFile(ms);
            ms.Dispose();
            _SourceFile.Dispose();
            File.Delete(_SourceFilePath);
            File.Move(dxmPath, _SourceFilePath);
            _SourceFile = File.Open(_SourceFilePath, FileMode.Open);
        }

        public override bool Add(string Key, byte[] Data)
        {
            if (HasKey(Key)) return false;
            StartUpdateXml((ms) =>
            {
                var emptyMemory = FindEmptyMemorys(Data.Length);
                var emidList = emptyMemory.ToIdList() + (Data.Length % BlockSize != 0 ? ("." + (Data.Length % BlockSize)) : "");
                Content.Write(emptyMemory, BlockSize, Data);
                Content.FlushStream();

                UseXmlReader(r =>
                {
                    _XmlReaderReadformatCopy(r, ms, (copyR, copyWrite, type) =>
                    {
                        if (type == XmlReaderCopyActionTypes.CopyKeys)
                        {
                            copyWrite.WriteStartElement("Keys");
                            copyWrite.WriteStartElement("K");
                            copyWrite.WriteAttributeString("Key", Key);
                            copyWrite.WriteString(emidList);
                            copyWrite.WriteEndElement();

                            XmlReaderToEnd(copyR, "Keys", rd =>
                            {
                                copyWrite.WriteNode(rd, true);
                            }, () => true);

                            copyWrite.WriteEndElement();
                            return false;
                        }
                        else if (type == XmlReaderCopyActionTypes.CopyEmptyBlocks)
                        {
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
                                copyWrite.WriteStartElement("EmptyBlocks");
                                copyWrite.WriteString(idlist);
                                copyWrite.WriteEndElement();
                            }
                            if (IsUsingTemp)
                            {
                                TempDataTryDelete(Key);
                            }
                            return false;
                        }

                        return true;
                    });
                });
            });

            return true;
            //var dxmPath = this._SourceFilePath + ".dxs";
            //FileStream ms = new FileStream(dxmPath, FileMode.Create);
            ////XmlWriter w = XmlWriter.Create(ms, _XmlSettings);
            //var emptyMemory = FindEmptyMemorys(Data.Length);
            //var emidList = emptyMemory.ToIdList() + (Data.Length % BlockSize != 0 ? ("." + (Data.Length % BlockSize)) : "");
            //Content.Write(emptyMemory, BlockSize, Data);
            //Content.FlushStream();

            //UseXmlReader(r =>
            //{
            //    _XmlReaderReadformatCopy(r, ms, (copyR,copyWrite, type) =>
            //    {
            //        if(type== XmlReaderCopyActionTypes.CopyKeys)
            //        {
            //            copyWrite.WriteStartElement("Keys");
            //            copyWrite.WriteStartElement("K");
            //            copyWrite.WriteAttributeString("Key", Key);
            //            copyWrite.WriteString(emidList);
            //            copyWrite.WriteEndElement();
            //            XmlReaderChildrenElements(r, key =>
            //            {
            //                copyWrite.WriteNode(key, true);
            //            });
            //            copyWrite.WriteEndElement();
            //            return false;
            //        }
            //        return true;
            //    });
            //});

            ////UseXmlReader(r =>
            ////{
            ////    while (r.Read())
            ////    {
            ////        if (r.Name == "Content")
            ////        {
            ////            w.WriteStartElement("Content");
            ////            XmlReaderChildrenElements(r, contentItem =>
            ////            {
            ////                Console.WriteLine(r.Name);
            ////                if (r.Name == "Keys")
            ////                {
            ////                    w.WriteStartElement("Keys");
            ////                    w.WriteStartElement("K");
            ////                    w.WriteAttributeString("Key", Key);
            ////                    w.WriteString(emidList);
            ////                    w.WriteEndElement();
            ////                    XmlReaderChildrenElements(r, key =>
            ////                    {
            ////                        w.WriteNode(key, true);
            ////                    });
            ////                    w.WriteEndElement();
            ////                }
            ////                else
            ////                {
            ////                    w.WriteNode(r, true);
            ////                }
            ////            });
            ////            w.WriteEndDocument();
            ////        }

            ////    }
            ////});
            ////w.Dispose();
            //ms.Dispose();
            //_SourceFile.Dispose();
            //File.Delete(_SourceFilePath);
            //File.Move(dxmPath, _SourceFilePath);
            //_SourceFile = File.Open(_SourceFilePath, FileMode.Open);
            //return true;
        }

        public override bool Delete(string Key)
        {
            bool result = false;
            var node = HasKeyNode(Key);
            if (node == null) return false;
            string key = Key, size = node.Value.Value;
            UseXmlReader(r => StartUpdateXml(ms =>
            {

                _XmlReaderReadformatCopy(r, ms, (reader, copyWrite, type) =>
                {
                    if (type == XmlReaderCopyActionTypes.CopyKeys)
                    {
                        copyWrite.WriteStartElement("Keys");
                        XmlReaderToEnd(reader, "Keys", (rd) =>
                        {
                            if (!(r.GetAttribute("Key") == Key))
                            {
                                copyWrite.WriteNode(rd, true);
                            }
                            else
                            {
                                rd.Read();
                                size = rd.Value;
                                result = true;
                            }
                        }, () => true);
                        copyWrite.WriteEndElement();
                        return false;
                    }
                    else if (type == XmlReaderCopyActionTypes.CopyEmptyBlocks)
                    {
                        IdStringListToRealIdList(size, (i) =>
                        {
                            int index = int.Parse(i.IndexOf('.') != -1 ? i.Split('.')[0] : i);
                            EmptyBlockIndexs.Push(index);
                        });

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
                            copyWrite.WriteStartElement("EmptyBlocks");
                            copyWrite.WriteString(idlist);
                            copyWrite.WriteEndElement();
                        }
                        if (IsUsingTemp)
                        {
                            TempDataTryDelete(Key);
                        }
                        return false;
                    }
                    return true;
                });





            }));
            return result;




        }

        public override bool HasKey(string Key)
        {
            return HasKeyNode(Key) != null;
        }
        private KeyValuePair<string, string>? HasKeyNode(string Key)
        {

            KeyValuePair<string, string>? result = null;

            UseXmlReader(r => _XmlReaderReadformat(r, (reader, type) =>
            {
                if (type == XmlReaderActionTypes.ToKeys)
                {
                    XmlReaderToEnd(reader, "Keys", (k) =>
                    {
                        var tag = k.GetAttribute("Key");
                        if (k.GetAttribute("Key") == Key)
                        {
                            k.Read();
                            result = new KeyValuePair<string, string>(Key, k.Value);
                        }
                    }, () => result == null);
                }
            }));
            return result;

        }
        public override byte[] Query(string Key)
        {
            if (IsUsingTemp && TempDataTryQuery(Key, out var data))
            {
                return data;
            }
            MemoryStream ms = new MemoryStream();
            var kn = HasKeyNode(Key);
            if (kn == null) return null;
            var ids = kn.Value.Value.Split(',');
            IdStringListToRealIdList(kn.Value.Value, (i) =>
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
        public override void Dispose()
        {
            base.Dispose();
            this._SourceFile.Dispose();
        }

        public override IEnumerable<string> GetKeys()
        {
            List<string> ks = new List<string>();
            UseXmlReader(r => _XmlReaderReadformat(r, (rd, type) =>
            {
                ks.Add(rd.GetAttribute("Key"));
            }, () => true));
            return ks;
        }

    }
}
