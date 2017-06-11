using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Tsl.Math.Pathfinder
{
    public class AStarPathfinder3DMap : AStarPathfinder2DOptimized
    {
        public float RayCastHalfExtents = 0.5f; // レイキャスト時の衝突判定する(x,y,z)方向の半径
        public List<string> DisallowTags = new List<string>{"Transparent"};
        public float RayCastY = 0.5f;
        public static AStarPathfinder3DMap Instance;
        public bool DrawNodeInfomation = false;

        void Awake()
        {
            AStarPathfinder3DMap.Instance = this;
        }

        void Update()
        {
            if (this.DrawNodeInfomation)
            {
                AStarPathfinder3DMap.Instance.EachCell(drawCell);
                if (this.logic.Finished)
                {
                    AStarPathfinder3DMap.Instance.EachCell(drawCellCorrect);
                }
            }
        }

#if false
        // DDAでレイキャスト
        // (src,target] までDDAでトレースする
        public override void RaycastCell(Vector2 src, Vector2 target, AstarCell.Type ignore, System.Func<AstarCell, bool> act)
        {
            var hits = Physics.SphereCastAll(new Vector3(src.x, this.TileSize * 0.6f, src.y),
                                            this.RayCastHalfExtents, // this.TileSize * 0.5f,
                                            new Vector3(target.x - src.x, 0.0f, target.y - src.y),
                                            (target - src).magnitude);

            foreach (var h in hits)
            {
                if (this.DisallowTags.Contains(h.transform.tag)) continue;
                var pos = hits[0].transform.position;
                int index = this.cellIndex(new Vector2(pos.y, pos.z));
                AstarCell cell = null;
                if (index >= 0 && index < this.cellMapBody.Count()) cell = this.cellMapBody[index];
                act(cell);
                return;
            }
            act(this.CellMap(target));
        }

#else
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
#endif


        public void MapMakeFromScene(float rayCastY)
        {
            const int IgnoreRayCastLayer = ~(1 << 2);
            this.EachCell(cell => cell.CellType = AstarCell.Type.Empty);
            this.EachCell(cell =>

            {
                var hits = Physics.OverlapBox(new Vector3(cell.Position.x, rayCastY, cell.Position.y),
                                                new Vector3(this.RayCastHalfExtents, rayCastY, RayCastHalfExtents),
                                                Quaternion.identity,
                                                IgnoreRayCastLayer);


                if (hits.Any(h => !this.DisallowTags.Contains(h.transform.tag)))                                                   
                {   // 何かに衝突した
                    cell.CellType = AstarCell.Type.Block;
                }
            });
            this.MapMake();
        }

        private void drawCell(AstarCell cell)
        {
            float x=  cell.Position.x;
            float y = cell.Position.y;
            float t = this.TileSize * 0.4f;
            Color[] coltbl = { Color.green, // empty
                                Color.blue, Color.yellow, Color.white, Color.gray, Color.black, Color.red, new Color(0.1f,0.1f,0.1f,0.1f),
                                Color.red };
            var color = coltbl[(int)cell.CellType];
            if (cell.CellType != Tsl.Math.Pathfinder.AstarCell.Type.Removed)
            {
                Debug.DrawLine(new Vector3(x - t, 0.1f, y - t), new Vector3(x + t, 0.1f, y - t), color, 1.0f, false);
                Debug.DrawLine(new Vector3(x + t, 0.1f, y - t), new Vector3(x + t, 0.1f, y + t), color, 1.0f, false);
                Debug.DrawLine(new Vector3(x + t, 0.1f, y + t), new Vector3(x - t, 0.1f, y + t), color, 1.0f, false);
                Debug.DrawLine(new Vector3(x - t, 0.1f, y + t), new Vector3(x - t, 0.1f, y - t), color, 1.0f, false);
            
                foreach(var r in cell.Related)
                {
                    var p = r.cell.Position;
                    Debug.DrawLine(new Vector3(x, 0.1f, y), new Vector3(p.x, 0.1f, p.y), new Color(0.0f,1.0f,1.0f,0.2f));
                }
            }
        }
        private void drawCellCorrect(AstarCell cell)
        {
            if (cell.CellType == AstarCell.Type.Correct || cell.CellType == AstarCell.Type.Start || cell.CellType == AstarCell.Type.Goal)
            {
                foreach(var r in cell.Related)
                {
                    if (r.cell.CellType == Tsl.Math.Pathfinder.AstarCell.Type.Correct)
                    {
                        var p = r.cell.Position;
                        Debug.DrawLine(new Vector3(cell.Position.x, 0.1f, cell.Position.y), new Vector3(p.x, 0.1f, p.y), Color.red, 1.0f, false);
                    }
                }
            }
        }

    }
}

