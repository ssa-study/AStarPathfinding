using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Tsl.Math.Pathfinder
{
    // 2DグリッドタイプでのAStar Pathfindingのベースクラス
    public abstract class AStarPathfinder2DGrid : AStarPathfinder2D
    {
        public float TileSize = 1.0f;

        protected AstarCell[] cellMapBody;
        protected int GridWidth = 1;
        protected int GridHeight = 1;


        // intで評価されるmapRectのグリッドセルで初期化
        public void MapInit(Rect mapRect, float tilesize = 0.0f)
        {
            if (tilesize != 0.0f) this.TileSize = tilesize;
            this.MapRect = mapRect;

            // グリッドのサイズを計算しておく
            this.GridWidth  = (int)(this.MapRect.width / this.TileSize + 0.5f);
            this.GridHeight = (int)(this.MapRect.height / this.TileSize + 0.5f);

            // グリッドを埋め尽くすセルの配列
            this.cellMapBody = new AstarCell[this.GridHeight * this.GridWidth];

            // グリッドをセルの実体で埋め尽くす
            for (int iy = 0; iy < this.GridHeight; ++iy)
            {
                float y = mapRect.y + iy * this.TileSize;
                for (int ix = 0; ix < this.GridWidth; ++ix)
                {
                    float x = mapRect.x + ix * this.TileSize;
                    var cell = new AstarCell();
                    cell.Position = new Vector2(x, y);
                    cellMapBody[cellIndex(cell.Position)] = cell;
                }
            }
        }

        // srcのマップをコピー cellMapBodyを共有するときに使用する
        public void MapInit(AStarPathfinder2DGrid src)
        {
            this.TileSize = src.TileSize;
            this.MapRect = src.MapRect;
            this.cellMapBody = src.cellMapBody;
            this.GridHeight = src.GridHeight;
            this.GridWidth = src.GridWidth;
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
            foreach (var cell in this.cellMapBody)
            {
                act(cell);
            }
        }
        public void EachLogicCell(System.Action<AstarCell> act)
        {
            foreach (var cell in this.logic.cells)
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
        public override AstarCell AddCellImmediate(Vector2 pos, AstarCell.Type type)
        {   // グリッドタイプの場合は、既存のセルの属性を変える
            var cell = CellMap(pos);
            cell.CellType = type;
            MakeRelation(cell);
            return cell;
        }

        // 動的なセルの削除
        public override void RemoveCell(AstarCell cell)
        {
            foreach (var target in cell.Related)
            {   // taget はcellからの接続先
                var found = target.cell.Related.Find(c => c.cell == cell);
                if (found.cell == cell)
                {
                    target.cell.Related.Remove(found);
                }
            }
            this.logic.cells.Remove(cell);
        }

        // セルのタイプ別に集計する。
        public int[] Info()
        {
            int[] counts = new int[(int)AstarCell.Type.Links + 1];
            EachCell(cell =>
            {
                counts[(int)cell.CellType] += 1;
                counts[(int)AstarCell.Type.Links] += cell.Related.Count;
            });
            return counts;
        }

        // ノードの数を返す。事前にInfo関数を実行して、その結果を引数として渡す
        public int NumOfNodes(int[] info)
        {
            return info[(int)AstarCell.Type.Empty] + info[(int)AstarCell.Type.Open] + info[(int)AstarCell.Type.Close];
        }
        // ブロックノードの数を返す。事前にInfo関数を実行して、その結果を引数として渡す
        public int NumOfBlocks(int[] info)
        {
            return info [(int)AstarCell.Type.Block];
        }

        // ノード間のリンク数を返す。事前にInfo関数を実行して、その結果を引数として渡す
        public int NumOfLinks(int[] info)
        {
            return info[(int)AstarCell.Type.Links];
        }


        public override void Reset(bool allReset = true)
        {
            if (this.logic.startCell != null) RemoveCell(this.logic.startCell);
            if (this.logic.goalCell != null) RemoveCell(this.logic.goalCell);

            foreach (var cell in this.cellMapBody)
            {
                if (cell.CellType != AstarCell.Type.Block)
                {
                    cell.Reset(allReset);
                }
            }
        }
    }
}
