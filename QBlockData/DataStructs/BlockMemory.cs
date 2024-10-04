using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QBlockData.DataStructs
{
    public class BlockMemory
    {
        public long BlockIndex { get; set; }
        public BlockMemory NextBlock { get; set; }
        public BlockMemory BackBlock { get; set; }

        /// <summary>
        /// 获取第一个节点
        /// </summary>
        /// <returns></returns>
        public BlockMemory GetFirst()
        {
            BlockMemory bm = this;
            while (bm.BackBlock != null)
            {
                bm = bm.BackBlock;
            }
            return bm;
        }
        /// <summary>
        /// 获取以此为起点所有NextBlock的数量
        /// </summary>
        /// <returns></returns>
        public int GetLinkCount()
        {
            int i = 1;
            BlockMemory bm = this;
            while (bm.NextBlock != null)
            {
                bm = bm.NextBlock;
                i += 1;
            }
            return i;
        }
        /// <summary>
        /// 遍历所有子节点
        /// </summary>
        public void ForeachNexts(Action<BlockMemory> Foreacth)
        {
            BlockMemory bm = this;
            Foreacth.Invoke(bm);
            while (bm.NextBlock != null)
            {
                bm = bm.NextBlock;
                Foreacth.Invoke(bm);
            }
        }
        public string ToIdList(Char SplitChars = ',')
        {
            StringBuilder sb = new StringBuilder();
            long linkStart = -1, linkEnd = -1, linkCount = 1;
            ForeachNexts(n =>
            {
                if (n.BlockIndex == 202)
                {
                    int ixxx = 1;
                }
                if (linkStart == -1)
                {
                    if (sb.Length != 0) sb.Append(SplitChars);
                    sb.Append(n.BlockIndex);
                    linkStart = n.BlockIndex;
                }
                else if (Math.Abs((n.BlockIndex - linkStart)) == linkCount)
                {
                    linkEnd = n.BlockIndex;
                    linkCount++;
                    if (n.NextBlock == null || Math.Abs((n.NextBlock.BlockIndex - linkStart)) != linkCount)
                    {
                        sb.Append("~");
                        sb.Append(linkEnd);
                        linkStart = -1; linkEnd = -1; linkCount = 1;
                    }
                }
                else
                {
                    if (sb.Length != 0) sb.Append(SplitChars);
                    sb.Append(n.BlockIndex);
                    linkStart = -1; linkEnd = -1; linkCount = 1;
                }
                //else
                //{
                //    if (sb.Length != 0) sb.Append(SplitChars);
                //    if(linkStart!=-1)
                //    linkStart=-1;linkEnd=-1;linkCount = 1;
                //}
                //if (sb.Length != 0) sb.Append(SplitChars);
                //sb.Append(n.BlockIndex);
            });
            if (linkEnd != -1)
            {
                sb.Append("~");
                sb.Append(linkEnd);
                linkStart = -1; linkEnd = -1; linkCount = 1;
            }
            return sb.ToString();
        }
    }
}
