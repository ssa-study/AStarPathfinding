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
            if (lines == null) return null;
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



    }
}

