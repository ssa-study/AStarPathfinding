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
        public bool MapReady = false;
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

        private int GridWidth { get { return (int)(this.MapRect.width / this.TileSize + 0.000001f); } }
        private int GridHeight { get { return (int)(this.MapRect.height / this.TileSize + 0.000001f); } }

        class PathFindQueue
        { 
            public Vector2 start;
            public Vector2 end;
            public System.Action<List<Vector2>> onEnd;
        }

        Queue<PathFindQueue> pathFindQueue = new Queue<PathFindQueue>();
        System.DateTime startTime;


        void FixedUpdate()
        {
            if (this.MapReady && this.pathFindQueue.Any() && !this.logic.Busy)
            {   // キューにタスクがあって計算中ではない場合
                Reset(false);
                var param = this.pathFindQueue.Dequeue();
                PathFind(param.start, param.end, param.onEnd, ExecuteMode.ASync);
            }
            else if (this.logic.Busy && this.executeMode == ExecuteMode.ASync)
            {
                pathFindProcessCoroutine();
            }
        }

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
            if (type == AstarCell.Type.Start || type == AstarCell.Type.Goal)
            {
                var cell = new AstarCell();
                cell.CellType = type;
                cell.Position = pos;
                return cell;
            }
            else
            {
                var cell = CellMap(pos);
                cell.CellType = type;
                MakeRelation(cell);
                return cell;
            }
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
            for (float y = mapRect.y; y <= mapRect.yMax - this.TileSize; y += this.TileSize)
            {
                for (float x = mapRect.x; x <= mapRect.xMax - this.TileSize; x += this.TileSize)
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

        public void Reset(bool allReset = true)
        {
            foreach (var cell in this.cellMapBody)
            {
                if (cell.CellType != AstarCell.Type.Block)
                {
                    cell.Reset(allReset);
                }
            }
        }

        public void InsertInQueue(Vector2 start, Vector2 end, System.Action<List<Vector2>> act)
        {
            this.pathFindQueue.Enqueue(new PathFindQueue { start = start, end = end, onEnd = act});
        }


        ExecuteMode executeMode = ExecuteMode.Sync;

        public void PathFind(Vector2 start,
                             Vector2 goal,
                             System.Action<List<Vector2>> onEnd = null,
                             ExecuteMode mode = ExecuteMode.ASync)
        {
            this.startTime = System.DateTime.Now;
            this.executeMode = mode;
            System.Action<List<Vector2>> onFinish = r =>
            {
                Debug.Log(string.Format("PathFind time: {0} second", (System.DateTime.Now - this.startTime).TotalSeconds));
                onEnd(r);
            };

            if (mode != ExecuteMode.StepNext)
            {
                var startCell = SetCellTypeImmediate(start, AstarCell.Type.Start);
                var goalCell = SetCellTypeImmediate(goal, AstarCell.Type.Goal);
                this.logic.PathFind(startCell, goalCell, this.MakeRelation, onFinish, mode != ExecuteMode.Sync);
            }
            switch(mode)
            {
                case ExecuteMode.Sync:
                    break;
                case ExecuteMode.ASync:
                    pathFindProcessCoroutine();
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
        private void pathFindProcessCoroutine()
        {
            for (int i = 0; i <= this.ProcessCoroutineFactor && !this.logic.Finished; ++i)
            {
                this.logic.pathFindProcess();
            }
        }
    }

}
