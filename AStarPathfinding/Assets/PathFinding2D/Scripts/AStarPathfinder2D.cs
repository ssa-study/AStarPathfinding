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
            Sync, // �Ō�܂Ńm���X�g�b�v
            ASync, // Coroutine�Ŏ��s
            StepFirst, // 1�X�e�b�v����
            StepNext, // 1�X�e�b�v����
        }

        public int ProcessCoroutineFactor = 1; // Coroutine��Pathfind�����s����Ƃ��̏d��
        public bool MapReady = false;
        public bool DebugHaltMode = false; // DebugHalt == true�ňꎞ��~����
        public bool DebugHalt = false; // �ꎞ��~�i�f�o�b�O�p)

        protected List<Vector2> pathList; // ���ʂ��ꎞ�I�ɕۑ�����
        protected AStarPathfindLogic logic = new AStarPathfindLogic();


        private class PathFindQueue
        {
            public Vector2 start;
            public Vector2 end;
            public System.Action<List<Vector2>> onEnd;
        }

        private Queue<PathFindQueue> pathFindQueue = new Queue<PathFindQueue>();
        private ExecuteMode executeMode = ExecuteMode.Sync;


        // ���I�ȃZ���̒ǉ�(��ԕύX)
        public abstract AstarCell AddCellImmediate(Vector2 pos, AstarCell.Type type);
        // ���I�ȃZ���̍폜
        public abstract void RemoveCell(AstarCell cell);
        // �Z���Ԃ̐ڑ����̐���
        public abstract void MakeRelation(AstarCell cell);
        // �Z����m�[�h���̏�����
        public abstract void Reset(bool allReset = true);


        // PathFind���L���[�ɓ���Ď��s����
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
            {   // �����PathFind
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


        // pathFindProcess�𕪊����s����
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
            {   // �L���[�Ƀ^�X�N�������Čv�Z���ł͂Ȃ��ꍇ
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

