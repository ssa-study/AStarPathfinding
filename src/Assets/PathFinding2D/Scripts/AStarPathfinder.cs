using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Tsl.Math.Pathfinder
{
    public class AStarPathfinder : AStarPathfinder2DOptimized
    {
        public float RayCastY = 0.5f;
        public static AStarPathfinder Instance;

        void Awake()
        {
            AStarPathfinder.Instance = this;
        }


        // DDAでレイキャスト
        // (src,target] までDDAでトレースする
        public override void RaycastCell(Vector2 src, Vector2 target, AstarCell.Type ignore, System.Func<AstarCell, bool> act)
        {
            var hits = Physics.SphereCastAll(new Vector3(src.x, this.TileSize * 0.6f, src.y),
                                            this.TileSize * 0.5f,
                                            new Vector3(target.x - src.x, 0.0f, target.y - src.y),
                                            (target - src).magnitude);
            if (hits.Any())
            {
                var pos = hits[0].transform.position;
                int index = this.cellIndex(new Vector2(pos.y, pos.z));
                AstarCell cell = null;
                if (index >= 0 && index < this.cellMapBody.Count()) cell = this.cellMapBody[index];
                act(cell);
            }
            else
            {
                act(this.CellMap(target));
            }
        }


        public void MapMakeFromScene(float rayCastY, List<string> disallowed = null)
        {
            this.EachCell(cell => cell.CellType = AstarCell.Type.Empty);
            this.EachCell(cell =>

            {
                var hits = Physics.OverlapBox(new Vector3(cell.Position.x, rayCastY, cell.Position.y),
                                                new Vector3(this.TileSize*0.5f, rayCastY*0.5f, this.TileSize*0.5f));

                if ((disallowed == null && hits.Any()) ||
                    hits.Any(h => !disallowed.Contains(h.transform.tag)))
                {   // 何かに衝突した
                    cell.CellType = AstarCell.Type.Block;
                }
            });
            this.MapMake();
        }
    }
}

