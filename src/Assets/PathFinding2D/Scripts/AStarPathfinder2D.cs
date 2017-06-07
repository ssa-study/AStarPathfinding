using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Tsl.Math.Pathfinder
{
    public class AStarPathfinder2D : AStarPathfinder2DOptimized
    {
        public static AStarPathfinder2D Instance;

        void Awake()
        {
            AStarPathfinder2D.Instance = this;
        }

        public List<Vector2> FillGrid(List<Vector2> lines)
        {
            List<Vector2> result = new List<Vector2>();
            for (int i = 0; i < lines.Count - 1; ++i)
            {
                if (i == 0) result.Add(lines[i]);
                AStarPathfinder2D.Instance.RaycastCell(lines[i], lines[i + 1], AstarCell.Type.Block, cell =>
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


        // DDAでレイキャスト
        // (src,target] までDDAでトレースする
        public override void RaycastCell(Vector2 src, Vector2 target, AstarCell.Type ignore, System.Func<AstarCell, bool> act)
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
                    if (this.CellType(ix, iy) != ignore)
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
                    if (this.CellType(ix, iy) != ignore)
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
                    if (this.CellType(ix, iy) != ignore)
                    {
                        if (act(this.Cell(ix, iy))) return;
                    }
                }
            }
        }

    }
}

