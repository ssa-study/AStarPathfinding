using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Tsl.Math.Pathfinder
{
    public class AStarPathfinder2DGrid : MonoBehaviour
    {
        public float TileSize = 1.0f;
        public int ProcessCoroutineFactor = 1; // CoroutineでPathfindを実行するときの重み
        protected AstarCell[] cellMapBody;
        protected Rect MapRect = new Rect(0, 0, 16, 16);
        protected List<Vector2> pathList; // 結果を一時的に保存する
        
        protected AStarPathfindLogic logic = new AStarPathfindLogic();

        public enum ExecuteMode
        {
            Sync, // 最後までノンストップ
            ASync, // Coroutineで実行
            StepFirst, // 1ステップずつ
            StepNext, // 1ステップずつ
        }

        private int GridWidth { get { return (int)(this.MapRect.width / this.TileSize); } }
        private int GridHeight { get { return (int)(this.MapRect.height / this.TileSize); } }

        // positionから該当するセルのインデックスを取得する。
        // 見つからない場合は-1
        protected int cellIndex(Vector2 p)
        {
            int ix = (int)((p.x - this.MapRect.x) / this.TileSize + 0.5f);
            int iy = (int)((p.y - this.MapRect.y) / this.TileSize + 0.5f);
            if (p.x < this.MapRect.x || p.y < this.MapRect.y) return -1;
            if (ix < 0 || ix >= this.GridWidth || iy < 0 || iy >= this.GridHeight) return -1;
            return iy * this.GridWidth + ix;
        }

        public AstarCell CellMap(Vector2 p)
        {
            int index = cellIndex(p);
            if (index < 0 || index >= cellMapBody.Count())
            {
                Debug.LogError(string.Format("Invalid position: ({0},{1})", p.x, p.y));
                return null;
            }
            return cellMapBody[index];
        }

        public void EachCell(System.Action<AstarCell> act)
        {
            foreach(var cell in this.cellMapBody)
            {
                act(cell);
            }
        }

        public AstarCell.Type CellType(Vector2 p)
        {
            int index = cellIndex(p);
            if (index < 0 || index >= cellMapBody.Count()) return AstarCell.Type.Block;
            return cellMapBody[index].CellType;
        }
        public AstarCell.Type CellType(int x, int y)
        {
            if (x < 0 || y < 0 || x >=this.GridWidth || y >= this.GridHeight) return AstarCell.Type.Block; 
            return cellMapBody[y * this.GridWidth + x].CellType;
        }
        public AstarCell Cell(int x, int y)
        {
            if (x < 0 || y < 0 || x >=this.GridWidth || y >= this.GridHeight) return null; 
            return cellMapBody[y * this.GridWidth + x];
        }

        // 動的なセルの追加(状態変更)
        public AstarCell SetCellTypeImmediate(Vector2 pos, AstarCell.Type type)
        {
            var cell = CellMap(pos);
            cell.CellType = type;
            MakeRelation(cell);
            return cell;
        }

        // セル間の接続情報の生成
        public virtual void MakeRelation(AstarCell cell) { throw new System.NotImplementedException(); }

        public int[] Info()
        {
            int[] counts = new int[(int)AstarCell.Type.Links + 1];
            EachCell(cell =>
            {
                counts[(int)cell.CellType] += 1;
                counts[(int)(int)AstarCell.Type.Links] += cell.Related.Count;
            });
            return counts;
        }

        public int NumOfNodes
        {
            get { return this.cellMapBody.Where(c => c.CellType != AstarCell.Type.Removed && c.CellType != AstarCell.Type.Block).Count(); }
        }
        public int NumOfBlocks
        {
            get { return this.cellMapBody.Count(c => c.CellType == AstarCell.Type.Block); }
        }
        public int NumOfLinks
        {
            get { return this.cellMapBody.Where(c => c.CellType != AstarCell.Type.Removed && c.CellType != AstarCell.Type.Block).Select(c => c.Related.Count).Sum(); }
        }

        public int PathCount
        {
            get { return this.logic == null ? 0 : this.logic.PathCount; }
        }

        // intで評価されるmapRectのグリッドセルで初期化
        public void MapInit(Rect mapRect, float tilesize = 0.0f)
        {
            if (tilesize != 0.0f) this.TileSize = tilesize;
            this.MapRect = mapRect;
            this.cellMapBody = new AstarCell[this.GridHeight * this.GridWidth];
            for (float y = mapRect.y; y < mapRect.yMax; y += this.TileSize)
            {
                for (float x = mapRect.x; x < mapRect.xMax; x += this.TileSize)
                {
                    var cell = new AstarCell();
                    cell.Position = new Vector2(x, y);
                    cellMapBody[cellIndex(cell.Position)] = cell;
                }
            }
        }
        // srcのマップをコピー
        public void MapInit(AStarPathfinder2DGrid src)
        {
            this.TileSize = src.TileSize;
            this.MapRect = src.MapRect;
            this.cellMapBody = src.cellMapBody;
        }

        public void Reset()
        {
            foreach (var cell in this.cellMapBody)
            {
                if (cell.CellType != AstarCell.Type.Block)
                {
                    cell.Reset();
                }
            }
        }

        public void PathFind(Vector2 start,
                             Vector2 goal,
                             System.Action<List<Vector2>> onEnd = null,
                             ExecuteMode mode = ExecuteMode.ASync)
        {
            if (mode != ExecuteMode.StepNext)
            {
                var startCell = SetCellTypeImmediate(start, AstarCell.Type.Start);
                var goalCell = SetCellTypeImmediate(goal, AstarCell.Type.Goal);
                this.logic.PathFind(startCell, goalCell, this.MakeRelation, onEnd, mode != ExecuteMode.Sync);
            }
            switch(mode)
            {
                case ExecuteMode.Sync:
                    break;
                case ExecuteMode.ASync:
                    StartCoroutine(pathFindProcessCoroutine());
                    break;
                case ExecuteMode.StepFirst:
                case ExecuteMode.StepNext:
                    this.logic.pathFindProcess();
                    break;
                default:
                    throw new System.InvalidOperationException();
            }
        }

        // CoroutineでPathfindを実行する
        private IEnumerator pathFindProcessCoroutine()
        {
            while(!this.logic.Finished)
            {
                for (int i = 0; i <= this.ProcessCoroutineFactor && !this.logic.Finished; ++i)
                {
                    this.logic.pathFindProcess();
                }
                yield return null;
            }
        }
    }

}
