using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Tsl.Math.Pathfinder
{
    // GridベースのASterセルを、リンク形式で結合するために最適化を行う
    // GridMode = trueで、GridベースのA* Pathfindingと互換性を持たせる
    // GridMode = falseの場合は、ノード間を最短ルートで結ぶ
    public class AStarPathfinder2DOptimized : AStarPathfinder2DGrid
    {
        public bool GridMode = true; // 最適化時に、結果をグリッドのリストに変換する
        public bool PreCalculateRelation = true; // 接続情報を事前に計算する
         
        private AstarCell.Type[,] cellType;

        public static AStarPathfinder2DOptimized Instance;

        void Awake()
        {
            AStarPathfinder2DOptimized.Instance = this;
        }

        // 接続情報を生成する
        public override void MakeRelation(AstarCell cell)
        {
            if (this.logic.cells == null)
            {
                Debug.LogError("this.logic.cells is null");
                throw new System.InvalidOperationException();
            }
            // if (!this.logic.cells.Contains(cell)) this.logic.cells.Add(cell);
            setGridRelatedSearchRaycast(cell, true);
            cell.RelationBuilt = true;
        }

        // 到達可能なノードを全ノードからレイキャストして調べる
        public void setGridRelatedSearchRaycast(AstarCell parent, bool newCell = false)
        {
            foreach (var cell in this.logic.cells.Where(c => c.IsValidCell()))
            {
                if (cell == parent) continue;
                var fromCell = cell.Find(parent);
                if (fromCell.cell != null)
                {   // 相手から自分が見えている場合
                    if (!parent.Contains(cell))
                    {
                        parent.AddRelated(cell, fromCell.cost);
                    }
                    continue;
                }
                // raycast
                float cost = 0;
                var prevPos = parent.Position;
                if (!this.GridMode)
                {   // 中間グリッドを飛ばして最短距離で結ぶ場合
                    RaycastCell(parent.Position, cell.Position, AstarCell.Type.Removed,
                            rcell =>
                            {
                                if (rcell == null) return true;
                                if (rcell.CellType != AstarCell.Type.Removed)
                                {   // 何かあった
                                    if (rcell.CellType != AstarCell.Type.Block)
                                    {   // ブロックもしくは圏外ではない場合
                                        if (rcell == cell)
                                        {   // 見つかった!
                                            if (!parent.Contains(cell))
                                            {
                                                cost = (cell.Position - parent.Position).magnitude;
                                                parent.AddRelated(cell, cost);
                                                if (newCell && !cell.Contains(parent))
                                                {
                                                    cell.AddRelated(parent, cost);
                                                }
                                            }
                                        }
                                    }
                                    return true;
                                }
                                else
                                {
                                    return false;
                                }
                            });
                }
                else
                {   // グリッドモード
                    RaycastCell(parent.Position, cell.Position, AstarCell.Type.NoIgnore,
                        rcell =>
                        {
                            // グリッドモードではコストを計算する
                            var nowpos = rcell.Position;
                            cost += (nowpos - prevPos).magnitude;
                            prevPos = nowpos;
                            if (rcell.CellType != AstarCell.Type.Removed)
                            {   // 何かあった
                                if (rcell.CellType != AstarCell.Type.Block)
                                {   // ブロックもしくは圏外ではない場合
                                    if (rcell == cell)
                                    {   // 見つかった!
                                        if (!parent.Contains(cell))
                                        {
                                            if (!this.GridMode) cost = (cell.Position - parent.Position).magnitude;
                                            parent.AddRelated(cell, cost);
                                            if (newCell && !cell.Contains(parent))
                                            {
                                                cell.AddRelated(parent, cost);
                                            }
                                        }
                                    }
                                }
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        });

                }
            }
        }


        public override AstarCell AddCellImmediate(Vector2 pos, AstarCell.Type type)
        {
            if (this.GridMode)
            {
                return base.AddCellImmediate(pos, type);
            }
            else
            {
                AstarCell cell = null;
                if (type == AstarCell.Type.Start || type == AstarCell.Type.Goal)
                {   // スタートかゴールがブロックの場合はエラー
                    if (CellMap(pos).CellType == AstarCell.Type.Block)
                    {
                        return null;
                    }
                }
                else
                {   // StartとGoal以外はセルを探す
                    cell = CellMap(pos);
                }

                if (cell == null)
                {
                    cell = new AstarCell();
                    cell.CellType = type;
                    cell.Position = pos;
                    setCellType(cell);
                    MakeRelation(cell);
                    this.logic.cells.Add(cell);
                }
                else
                {
                    cell.CellType = type;
                    setCellType(cell);
                    MakeRelation(cell);
                    if (!this.logic.cells.Contains(cell)) this.logic.cells.Add(cell);
                }
                return cell;
            }
        }


        // (src,target] までDDAでトレースする
        // DDAでレイキャスト
        // (src,target] までDDAでトレースする
        public virtual void RaycastCell(Vector2 src, Vector2 target, AstarCell.Type ignore, System.Func<AstarCell, bool> act)
        {
            int ix = (int)((src.x - this.MapRect.x) / this.TileSize + 0.5f);
            int iy = (int)((src.y - this.MapRect.y) / this.TileSize + 0.5f);
            int tx = (int)((target.x - this.MapRect.x) / this.TileSize + 0.5f);
            int ty = (int)((target.y - this.MapRect.y) / this.TileSize + 0.5f);


            int sx = ix > tx ? -1 : 1;
            int sy = iy > ty ? -1 : 1;
            int dx = Mathf.Abs(tx - ix);
            int dy = Mathf.Abs(ty - iy);
            if (dx == dy)
            {   //ななめ45度
                while (true)
                {
                    ix += sx;
                    iy += sy;
                    if (--dx < 0) break;
                    if (this.cellType[ix, iy] != ignore)
                    {
                        if (act(this.Cell(ix, iy))) return;
                    }
                }
            }
            else if (dx > dy)
            {   // 横に長い
                int r = dx / 2;
                while (sx > 0 ? ix < tx : ix > tx)
                {
                    ix += sx;
                    r -= dy;
                    if (r < 0)
                    {
                        r += dx;
                        iy += sy;
                    }
                    if (this.cellType[ix, iy] != ignore)
                    {
                        if (act(this.Cell(ix, iy))) return;
                    }
                }
            }
            else
            {   // 縦に長い
                int r = dy / 2;
                while (sy > 0 ? iy < ty : iy > ty)
                {
                    iy += sy;
                    r -= dx;
                    if (r < 0)
                    {
                        r += dy;
                        ix += sx;
                    }
                    if (this.cellType[ix, iy] != ignore)
                    {
                        if (act(this.Cell(ix, iy))) return;
                    }
                }
            }
        }

        // 基本的なAStartでは処理が冗長のため、最適化を行う
        // 障害物の凸頂点以外のノードを削除する
        public void MapMake()
        {
            foreach (var cell in this.cellMapBody)
            {
                if (cell.CellType != AstarCell.Type.Block)
                    cell.CellType = AstarCell.Type.Empty;
                cell.Related.Clear();
            }

            // 8方向全部がEmptyであるセルを削除
            var octcells = this.cellMapBody.Where(c =>
            {
                if (c.CellType != AstarCell.Type.Empty) return false;
                for (int ix = -1; ix < 2; ++ix)
                {
                    for (int iy = -1; iy < 2; ++iy)
                    {
                        if (ix == 0 && iy == 0) continue;
                        Vector2 ixy = new Vector2(ix * this.TileSize, iy * this.TileSize);
                        if (this.CellType(c.Position + ixy) != AstarCell.Type.Empty) return false;
                    }
                }
                return true;
            }
            ).ToList();
            foreach (var cell in octcells)
            {
                cell.CellType = AstarCell.Type.Removed;
            }
            var emptyCells = this.cellMapBody.Where(c => c.CellType == AstarCell.Type.Empty);

            // 3x3周囲のセル情報をセットする
            System.Action<Vector2, AstarCell.Type[,]> set3x3 = (pos, ar) =>
            {
                for (int ix = -1; ix < 2; ++ix)
                {
                    for (int iy = -1; iy < 2; ++iy)
                    {
                        if (ix == 0 && iy == 0) continue;
                        float x = pos.x + (ix * this.TileSize);
                        float y = pos.y + (iy * this.TileSize);
                        ar[ix + 1, iy + 1] = this.CellType(new Vector2(x, y));
                    }
                }
            };
            // 角以外のセルを削除
            List<AstarCell> removeList = new List<AstarCell>();
            foreach (var cell in emptyCells)
            {
                var pos = cell.Position;
                AstarCell.Type[,] ar = new AstarCell.Type[3, 3]; // check around
                set3x3(pos, ar);
                // 縦横3連ブロックチェック
                for (int inv = 0; inv < 2; ++inv)
                {
                    // x,ｙを反転させる
                    System.Func<int, int, AstarCell.Type> m = (x, y) => { return inv == 0 ? ar[x, y] : ar[y, x]; };

                    // 両サイド
                    for (int s = 0; s <= 2; s += 2)
                    {
                        int u = 2 - s; // 逆サイド
                        if (m(s, 0) == m(s, 1) && m(s, 1) == m(s, 2) && m(s, 0) == AstarCell.Type.Block)
                        {   // サイドがすべてブロック
                            if (m(1, 0) == AstarCell.Type.Empty && m(1, 2) == AstarCell.Type.Empty)
                            {   // 上下3連でEmpty
                                if (m(u, 0) != AstarCell.Type.Block
                                    && m(u, 1) != AstarCell.Type.Block
                                    && m(u, 2) != AstarCell.Type.Block)
                                {
                                    removeList.Add(cell);
                                }
                            }
                            if (m(1, 0) == AstarCell.Type.Empty || m(1, 2) == AstarCell.Type.Empty)
                            {   // 凹角の判断
                                if ((m(1, 2) == AstarCell.Type.Block && m(u, 2) == AstarCell.Type.Block && m(u, 0) != AstarCell.Type.Block)
                                || (m(1, 0) == AstarCell.Type.Block && m(u, 0) == AstarCell.Type.Block && m(u, 2) != AstarCell.Type.Block))
                                {
                                    removeList.Add(cell);
                                }
                            }
                        }
                    }
                }
            }
            foreach (var cell in removeList)
            {
                cell.CellType = AstarCell.Type.Removed;
            }
            removeList.Clear();

            if (!this.GridMode)
            {   // グリッドモードでない場合は、さらに最適化を進める
                foreach (var cell in emptyCells.Where(c => c.CellType == AstarCell.Type.Empty))
                {
                    var pos = cell.Position;
                    AstarCell.Type[,] ar = new AstarCell.Type[3, 3]; // check around
                    set3x3(pos, ar);
                    // 90度つず回転する[x,y][y,x][-x,y][x,-ｙ]
                    for (int angle = 0; angle < 8; ++angle)
                    {
                        System.Func<int, int, AstarCell.Type> m = (x, y) =>
                        {
                            switch (angle)
                            {
                                default:
                                case 0: return ar[x, y];
                                case 1: return ar[y, 2 - x];
                                case 2: return ar[2 - x, 2 - y];
                                case 3: return ar[2 - y, x];
                                case 4: return ar[y, x];
                                case 5: return ar[x, 2 - y];
                                case 6: return ar[2 - y, 2 - x];
                                case 7: return ar[2 - x, y];
                            }
                        };
                        // 角にある3点を1点にする
                        // □ □ □      □ □ □ 
                        // □ * *   => □ □ *
                        // ? ■ ?      ? ■ *
                        if (m(0, 0) == AstarCell.Type.Removed && m(1, 0) == AstarCell.Type.Removed && m(2, 0) == AstarCell.Type.Removed
                         && m(0, 1) == AstarCell.Type.Removed && m(1, 1) == AstarCell.Type.Empty && m(2, 1) == AstarCell.Type.Empty
                         /*&& m(0,2) == AstarCell.Type.Block*/ && m(1, 2) == AstarCell.Type.Block /*&& m(2, 2) == AstarCell.Type.Empty*/)
                        {
                            removeList.Add(cell);
                        }
                        // 冗長な斜めの線を細くする
                        //  □  * !□      □ * * 
                        //  *  *  ■   => * □ ■
                        // !□  ■  ■      * ■ ■
                        if (angle < 4 &&
                            m(0, 0) == AstarCell.Type.Removed && m(1, 0) == AstarCell.Type.Empty && m(2, 0) != AstarCell.Type.Removed
                         && m(0, 1) == AstarCell.Type.Empty && m(1, 1) == AstarCell.Type.Empty && m(2, 1) == AstarCell.Type.Block
                         && m(0, 2) != AstarCell.Type.Removed && m(1, 2) == AstarCell.Type.Block && m(2, 2) == AstarCell.Type.Block)
                        {
                            removeList.Add(cell);
                        }
                        // 3連続の中を抜く
                        //  ?  *  ?      ?  *  ?
                        // !*  *  !*  => ?  □  ?
                        //  ?  *  ?      ?  *  ?
                        if ((angle == 0 || angle == 4) &&
                            m(1,0) == AstarCell.Type.Empty && m(1,1) == AstarCell.Type.Empty && m( 1,2) == AstarCell.Type.Empty
                          && (m(0,1) != AstarCell.Type.Empty && m(2,1) != AstarCell.Type.Empty)
                            )
                        {
                            removeList.Add(cell);
                        }
                        // 斜め3連続の中を抜く
                        // !□  ?  *     □  □  *
                        //  □  *  □  => □  □  □
                        //  *  ? !□     *  □  □
                        if ((angle == 0 || angle == 7) &&
                            m(0,0) != AstarCell.Type.Empty /*&& m(1,0) == AstarCell.Type.Removed */ && m(2,0) == AstarCell.Type.Empty
                        /*&& m(0,1) == AstarCell.Type.Removed*/ && m(1,1) == AstarCell.Type.Empty /*&& m(2,1) == AstarCell.Type.Removed*/
                        && m(0,2) == AstarCell.Type.Empty /*&& m(1,2) == AstarCell.Type.Removed */ && m(2,2) != AstarCell.Type.Empty)
                        {
                            removeList.Add(cell);
                        }
                    }
                }

                foreach (var cell in removeList)
                {
                    cell.CellType = AstarCell.Type.Removed;
                }
            }

            // キャッシュ用セルを作成
            this.cellType = new AstarCell.Type[this.GridWidth, this.GridHeight];
            foreach (var cell in this.cellMapBody)
            {
                setCellType(cell);
            }

            // 有効なセルの登録
            this.logic.cells = this.cellMapBody.Where(c => c.CellType == AstarCell.Type.Empty).Select(c => c as AstarCell).ToList();

            // 残ったセルに対して、接続情報をセットする
            if (this.PreCalculateRelation)
            {
                foreach (var cell in this.logic.cells)
                {
                    setGridRelatedSearchRaycast(cell, true);
                    cell.RelationBuilt = true;
                }
            }

            this.MapReady = true;
        }

        private void setCellType(AstarCell cell)
        {
            int ix = (int)((cell.Position.x - this.MapRect.x) / this.TileSize + 0.5f);
            int iy = (int)((cell.Position.y - this.MapRect.y) / this.TileSize + 0.5f);
            if (ix < 0 || ix >= this.GridWidth || iy < 0 || iy > this.GridHeight)
            {
                Debug.LogError(string.Format("invalid cell position({0},{1})", cell.Position.x, cell.Position.y));

            }
            else
            {
                this.cellType[ix, iy] = cell.CellType;
            }
        }

        // linesで示される経路をグリッドベースに変換する
        public List<Vector2> FillGrid(List<Vector2> lines)
        {
            if (lines == null) return null;
            List<Vector2> result = new List<Vector2>();
            for (int i = 0; i < lines.Count - 1; ++i)
            {
                if (i == 0) result.Add(lines[i]);
                RaycastCell(lines[i], lines[i + 1], AstarCell.Type.Block, cell =>
                {
                    if (cell != null)
                    {
                        if (cell.CellType == AstarCell.Type.Removed) cell.CellType = AstarCell.Type.SkipPoint;
                        result.Add(cell.Position);
                    }
                    return false;
                });
            }
            return result;
        }

    }
}

