using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Tsl.Math.Pathfinder
{
    // グリッドベースのPathFinder
    public class AStarPathfinder2DBasic : AStarPathfinder2DGrid
    {
        public static AStarPathfinder2DBasic Instance;

        void Awake()
        {
            AStarPathfinder2DBasic.Instance = this;
        }

        public void MapMake()
        {
            this.logic.cells = this.cellMapBody.Select(c => c as AstarCell).ToList();
            foreach (var cell in this.cellMapBody)
            {
                if (cell.CellType != AstarCell.Type.Block)
                    cell.CellType = AstarCell.Type.Empty;
                MakeRelation(cell);
            }
        }

        // 上下左右斜めのグリッドに対してリンクを作成する
        public override void MakeRelation(AstarCell parent)
        {
            parent.Related.Clear();
            if (parent.CellType == AstarCell.Type.Block) return;
            int x = (int)parent.Position.x;
            int y = (int)parent.Position.y;
            for (int dx = -1; dx < 2; ++dx)
            {
                for (int dy = -1; dy < 2; ++dy)
                {
                    if (dx == 0 && dy == 0) continue;
                    float nx = x + dx * this.TileSize;
                    float ny = y + dy * this.TileSize;
                    Vector2 n = new Vector2(nx, ny);
                    if (!this.MapRect.Contains(n)) continue;
                    var cell = this.CellMap(n);
                    if (cell != null && cell.CellType != AstarCell.Type.Block)
                    {
                        parent.AddRelated(this.CellMap(n), parent.Heuristic(cell));
                    }
                }
            }
        }
    }
}
