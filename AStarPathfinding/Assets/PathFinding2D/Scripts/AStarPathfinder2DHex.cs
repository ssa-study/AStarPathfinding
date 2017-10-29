using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


namespace Tsl.Math.Pathfinder
{
    // HexタイプでのAStar Pathfindingのベースクラス
    //    A B C D E F G H
    //   / \ / \ / \ / \
    // 0| A0| C0| E0| G0|
    //   \ / \ / \ / \ / \
    // 1  |B1 | D1| F1| H1|
    //   / \ / \ / \ / \ /
    // 2| A2| C2| E2| G2|
    //   \ / \ / \ / \ / \
    // 3  | B3| D3| F3| H3|

    public class AStarPathfinder2DHex : AStarPathfinder2D
    {

        protected AstarCell[,] cellMapBody;
        protected int GridColumns = 1;
        protected int GridRows = 1;
        float sin60 = Mathf.Sin(60.0f * 3.141592f / 180.0f);
        float cos60 = Mathf.Cos(60.0f * 3.141592f / 180.0f);

        public override void MakeRelation(AstarCell cell)
        {

        }

        // intで評価されるmapRectのグリッドセルで初期化
        public void MapInit(int columns, int rows)
        {

            // グリッドのサイズを計算しておく
            this.GridColumns = columns;
            this.GridRows = rows;

            // グリッドを埋め尽くすセルの配列
            this.cellMapBody = new AstarCell[(this.GridColumns + 1)/2, this.GridRows];

            // グリッドをセルの実体で埋め尽くす
            for (int iy = 0; iy < this.GridRows; ++iy)
            {
                for (int ix = 0; ix < (this.GridColumns+1)/2; ++ix)
                {
                    var cell = new AstarCell();
                    float x = ix + ((iy & 1) == 0 ? 0.0f : cos60);
                    float y = iy * sin60;
                    cell.Position = new Vector2(x, y);
                    cellMapBody[ix, iy] = cell;
                }
            }
        }

        // srcのマップをコピー cellMapBodyを共有するときに使用する
        public void MapInit(AStarPathfinder2DHex src)
        {
            this.cellMapBody = src.cellMapBody;
            this.GridColumns = src.GridColumns;
            this.GridRows = src.GridRows;
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

        private AstarCell FindCell(Vector2 pos)
        {
            int y = (int)(pos.y / this.sin60 + 0.5f);
            int odd = (y & 1) == 0 ? 0 : 1;
            int x = (int)(pos.x - (odd * cos60));
            if (y < 0 || y >= this.GridRows || x < 0 || x > (this.GridColumns + 1) / 2) return null;
            return this.cellMapBody[x, y];
        }


        // 動的なセルの追加(状態変更)
        public override AstarCell AddCellImmediate(Vector2 pos, AstarCell.Type type)
        {   // グリッドタイプの場合は、既存のセルの属性を変える
            var cell = FindCell(pos);
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
            return info[(int)AstarCell.Type.Block];
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
