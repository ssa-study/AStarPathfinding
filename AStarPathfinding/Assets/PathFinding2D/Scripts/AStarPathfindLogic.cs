using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Tsl.Math.Pathfinder
{

    public class AStarPathfindLogic
    { 
        public List<AstarCell> cells;
        public AstarCell startCell;
        public AstarCell goalCell;

        private int pathCount = 0;
        public bool Busy = false;
        private bool finished = false;
        private System.Action<AstarCell> MakeRelation;
        private System.Action<List<Vector2>> onGoal;
        private Dictionary<float, AstarCell> goalCandidate = new Dictionary<float, AstarCell>();

        public bool Finished {  get {  return this.finished; } }
        public int PathCount {  get {  return this.pathCount; } }

        public void PathFind(AstarCell startCell, 
                             AstarCell goalCell, 
                             System.Action<AstarCell> makeRelation, 
                             System.Action<List<Vector2>> onGoal,
                             bool initOnly = false)
        {
            if (this.Busy) throw new System.InvalidOperationException();
            this.Busy = true;
            this.startCell = startCell;
            this.goalCell = goalCell;
            this.MakeRelation = makeRelation;
            this.onGoal = onGoal;
            this.pathCount = 0;
            this.finished = false;
            this.goalCandidate.Clear();

            ScanAround(this.startCell);

            if (!initOnly)
            {
                while(!this.finished) pathFindProcess();
            }
        }

        public void pathFindProcess()
        {
            ++this.pathCount;
            if (this.cells == null)
            {
                throw new System.InvalidOperationException();
            }
            var cells = this.cells.Where(c => c.CellType == AstarCell.Type.Open)
                        ; //.OrderBy(c => c.Score).ThenBy(c => c.Cost);

            if (cells.Any())
            {
                var score = cells.Min(c => c.Score);
                // var score = cells.ElementAt(0).Score;
                foreach (var cell in cells.Where(c => Mathf.Abs(c.Score - score) < 0.1f))
                {
                    ScanAround(cell);
                    cell.CellType = AstarCell.Type.Close;
                }
            }
            else
            {   // 解決不能
                this.finished = true;
                this.onGoal(null);
                this.Busy = false;
            }
            if (this.goalCandidate.Any())
            {   // goalしたものがいる場合、open cellでgoalよりスコアが良いものが無いか探す
                var goal = this.goalCandidate.OrderBy(g => g.Key).ElementAt(0);
                if (this.pathCount == 1 || !this.cells.Any(c => c.CellType == AstarCell.Type.Open && c.Score <= goal.Key))
                {
                    pathfindFinished(goal.Value);
                }
            }
        }

        private void ScanAround(AstarCell parent)
        {
            // 接続情報を作成する
            if (!parent.RelationBuilt)
            {
                MakeRelation(parent);
            }

            foreach (var related in parent.Related)
            { 
                if (related.cell.CellType == AstarCell.Type.Goal)
                {   // !! GOAL!
                    var goalcost = parent.Cost + related.cost;
                    related.cell.Cost = goalcost;
                    related.cell.Score = goalcost;
                    if (this.goalCandidate.ContainsKey(goalcost))
                    {
                        this.goalCandidate[goalcost] = parent;
                    }
                    else
                    {
                        this.goalCandidate.Add(goalcost, parent);
                    }
                }
                float cost = parent.Cost + related.cost;
                float hint = this.goalCell.Heuristic(related.cell);
                float score = cost + hint;
                if (related.cell.CellType == AstarCell.Type.Empty || related.cell.Score > score)
                {
                    related.cell.Cost = cost;
                    related.cell.Hint = hint;
                    related.cell.Score = score;
                    related.cell.Parent = parent;
                    related.cell.CellType = AstarCell.Type.Open;
                }
            }
        }

        private void pathfindFinished(AstarCell cell)
        {
            var pathList = new List<Vector2>();
            pathList.Add(this.goalCell.Position);
            var parent = cell;
            while (parent.Parent != null)
            {
                pathList.Add(parent.Position);
                parent.CellType = AstarCell.Type.Correct;
                parent = parent.Parent;
            }
            pathList.Add(this.startCell.Position);
            pathList.Reverse();
            this.finished = true;
            this.onGoal(pathList);
            //var links = this.cells.Sum(c => c.Related.Count);
            //Debug.Log(string.Format("{0} cells {1} links", this.cells.Count, links));
            this.Busy = false;

        }

    }
}
