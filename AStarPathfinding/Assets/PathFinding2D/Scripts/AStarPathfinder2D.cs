using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Tsl.Math.Pathfinder
{
    public abstract class AStarPathfinder2D : MonoBehaviour
    {
        public enum ExecuteMode
        {
            Sync, // 最後までノンストップ
            ASync, // Coroutineで実行
            StepFirst, // 1ステップずつ
            StepNext, // 1ステップずつ
        }

        public int ProcessCoroutineFactor = 1; // CoroutineでPathfindを実行するときの重み
        public bool MapReady = false;
        public bool DebugHaltMode = false; // DebugHalt == trueで一時停止する
        public bool DebugHalt = false; // 一時停止（デバッグ用)

        protected List<Vector2> pathList; // 結果を一時的に保存する
        protected AStarPathfindLogic logic = new AStarPathfindLogic();


        private class PathFindQueue
        {
            public Vector2 start;
            public Vector2 end;
            public System.Action<List<Vector2>> onEnd;
        }

        private Queue<PathFindQueue> pathFindQueue = new Queue<PathFindQueue>();
        private ExecuteMode executeMode = ExecuteMode.Sync;


        // 動的なセルの追加(状態変更)
        public abstract AstarCell AddCellImmediate(Vector2 pos, AstarCell.Type type);
        // 動的なセルの削除
        public abstract void RemoveCell(AstarCell cell);
        // セル間の接続情報の生成
        public abstract void MakeRelation(AstarCell cell);
        // セルやノード情報の初期化
        public abstract void Reset(bool allReset = true);


        // PathFindをキューに入れて実行する
        public void InsertInQueue(Vector2 start, Vector2 end, System.Action<List<Vector2>> act)
        {
            this.pathFindQueue.Enqueue(new PathFindQueue { start = start, end = end, onEnd = act });
        }

        public void PathFind(Vector2 start,
                             Vector2 goal,
                             System.Action<List<Vector2>> onEnd = null,
                             ExecuteMode mode = ExecuteMode.ASync)
        {
            this.executeMode = mode;
            System.Action<List<Vector2>> onFinish = r =>
            {
                if (this.DebugHaltMode && r == null)
                {
                    this.DebugHalt = true;
                }
                else
                {
                    onEnd(r);
                }
            };

            if (mode != ExecuteMode.StepNext)
            {   // 初回のPathFind
                var startCell = AddCellImmediate(start, AstarCell.Type.Start);
                var goalCell = AddCellImmediate(goal, AstarCell.Type.Goal);
                if (startCell == null || goalCell == null)
                {
                    Debug.LogWarning("start or gold is in block");
                    onEnd(null);
                    return;
                }
                this.logic.PathFind(startCell, goalCell, this.MakeRelation, onFinish, mode != ExecuteMode.Sync);
                return;
            }
            switch (mode)
            {
                case ExecuteMode.Sync:
                    break;
                case ExecuteMode.ASync:
                    pathFindProcessCoroutine();
                    break;
                case ExecuteMode.StepFirst:
                case ExecuteMode.StepNext:
                    this.logic.pathFindProcess();
                    break;
                default:
                    throw new System.InvalidOperationException();
            }
        }

        public int PathCount
        {
            get { return this.logic == null ? 0 : this.logic.PathCount; }
        }


        // pathFindProcessを分割実行する
        private void pathFindProcessCoroutine()
        {
            for (int i = 0; i <= this.ProcessCoroutineFactor && !this.logic.Finished; ++i)
            {
                this.logic.pathFindProcess();
            }
        }

        void FixedUpdate()
        {
            if (this.DebugHaltMode && this.DebugHalt) return;
            if (this.MapReady && this.pathFindQueue.Any() && !this.logic.Busy)
            {   // キューにタスクがあって計算中ではない場合
                Reset(false);
                var param = this.pathFindQueue.Dequeue();
                PathFind(param.start, param.end, param.onEnd, ExecuteMode.ASync);
            }
            else if (this.logic.Busy && this.executeMode == ExecuteMode.ASync)
            {
                pathFindProcessCoroutine();
            }
        }

    }
}

