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

        void Awake()
        {
            AStarPathfinder3DMap.Instance = this;
        }


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
    }
}

