using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using AStarPathfinder2DOptimized = Tsl.Math.Pathfinder.AStarPathfinder2DOptimized;
using AStarPathfinder2DTraditional = Tsl.Math.Pathfinder.AStarPathfinder2DTraditional;
using AstarCell = Tsl.Math.Pathfinder.AstarCell;

public class SceneBehaviourUIMap : MonoBehaviour {

    public Transform MapRoot;
    public GameObject CellPrefab;
    public Rect MapRect = new Rect(0,0,16,16);
    public float TileSize = 1.0f;
    public Vector2 StartPoint = new Vector2(0, 0);
    public Vector2 GoalPoint = new Vector2(15, 15);
    public UnityEngine.UI.Text MessageText;
    public UnityEngine.UI.Text TestText;

    private bool goled = false;

    private Tsl.UI.Pathfinder.Cell[,] cellMap;
    private float distance = 0.0f;

    // Use this for initialization
    void Start () {
        AStarPathfinder2DOptimized.Instance.MapInit(this.MapRect);
        int w = (int)this.MapRect.width;
        int h = (int)this.MapRect.height;
        this.cellMap = new Tsl.UI.Pathfinder.Cell[w,h];
        for (int y = 0; y < w; ++y)
        {
            for (int x = 0; x < h; ++x)
            {
                var cell = Instantiate(CellPrefab.gameObject) as GameObject;
                this.cellMap[x, y] = cell.GetComponent<Tsl.UI.Pathfinder.Cell>();
                cell.transform.SetParent(this.MapRoot, false);
                this.cellMap[x, y].AstarCell = AStarPathfinder2DOptimized.Instance.CellMap(new Vector2(x, y));
            }

        }
        AStarPathfinder2DTraditional.Instance.MapInit(AStarPathfinder2DOptimized.Instance);
    }

    private void Update()
    {
        var info = AStarPathfinder2DOptimized.Instance.Info();
        this.MessageText.text = string.Format("{0} Nodes\n{1} Links\n{2} Paths\ndistance={3}",
            AStarPathfinder2DOptimized.Instance.NumOfNodes(info),
            AStarPathfinder2DOptimized.Instance.NumOfLinks(info),
            AStarPathfinder2DOptimized.Instance.PathCount,
            this.distance);

    }

    public void Reset()
    {
        AStarPathfinder2DOptimized.Instance.Reset();
        AStarPathfinder2DTraditional.Instance.Reset();
        this.goled = false;
    }
    bool usingOptimize = false;

    public void OnClickStartButton()
    {
        if (this.goled)
        {
            Reset();
        }
        else
        {
            if (this.usingOptimize)
            {
                AStarPathfinder2DOptimized.Instance.PathFind(this.StartPoint, this.GoalPoint, r =>
                {
                    r = AStarPathfinder2DOptimized.Instance.FillGrid(r);
                    this.distance = DrawLine(r);
                    this.goled = true;
                });
            }
            else
            {
                AStarPathfinder2DTraditional.Instance.PathFind(this.StartPoint, this.GoalPoint, r =>
                {
                    this.distance = DrawLine(r);
                    this.goled = true;
                });
            }
        }
    }

    private float DrawLine(List<Vector2> lines)
    {
        float distance = 0.0f;
        if (lines != null)
        {
            for (int i = 0; i < lines.Count - 1; ++i)
            {
                distance += (lines[i+1] - lines[i]).magnitude;
            }
        }
        return distance;
    }

    public void OnClickMapMakeButton(bool opt)
    {
        if (opt)
        {
            this.usingOptimize = true;
            AStarPathfinder2DOptimized.Instance.MapMake();
        }
        else
        {
            this.usingOptimize = false;
            AStarPathfinder2DTraditional.Instance.MapMake();
        }
    }

    public void OnClickRandomMake()
    {
        for (int n = 0; n < 10; ++n)
        {
            int l = UnityEngine.Random.Range(1,10);
            Vector2 pos = new Vector2(Random.Range(this.MapRect.xMin, this.MapRect.width),
                                      Random.Range(this.MapRect.yMin, this.MapRect.height));
            bool dir = Random.Range(0,2) == 0;
            var range = new Rect(this.MapRect.xMin, this.MapRect.yMin, this.MapRect.width - this.TileSize, this.MapRect.height - this.TileSize);
            while(l-- != 0)
            {
                if (!range.Contains(pos)) break;
                AStarPathfinder2DOptimized.Instance.CellMap(pos).CellType = AstarCell.Type.Block;
                pos.x += dir ? this.TileSize : 0.0f;
                pos.y += dir ? 0.0f : this.TileSize;
            }
        }
    }
    
    public void OnClickClear()
    {
        AStarPathfinder2DOptimized.Instance.EachCell(cell => cell.CellType = AstarCell.Type.Removed);
    }

    public void OnClickAutoTest()
    {
        StartCoroutine(AutoTest());
    }
    private IEnumerator AutoTest()
    {
        double basicTime = 0.0;
        double optimizedTime = 0.0;
        int testCount = 0;
        for (int cnt = 0; cnt < 100; ++cnt)
        {
            OnClickClear();
            OnClickRandomMake();
            OnClickRandomMake();
            OnClickRandomMake();

            Reset();
            AStarPathfinder2DTraditional.Instance.MapMake();

            do
            {
                this.StartPoint = new Vector2(Random.Range(this.MapRect.x, this.MapRect.width / 3 - this.TileSize),
                                              Random.Range(this.MapRect.y, this.MapRect.height / 3 - this.TileSize));
            } while(AStarPathfinder2DOptimized.Instance.CellMap(this.StartPoint).CellType == AstarCell.Type.Block);
            do
            {
                this.GoalPoint = new Vector2(Random.Range(this.MapRect.x, this.MapRect.width / 3) + this.MapRect.width * 2 / 3 - this.TileSize,
                                         Random.Range(this.MapRect.y, this.MapRect.width / 3) + this.MapRect.width * 2 / 3 - this.TileSize);
            } while(AStarPathfinder2DOptimized.Instance.CellMap(this.GoalPoint).CellType == AstarCell.Type.Block);
            this.goled = false;
            var now = System.DateTime.Now;
            float basicDistance = 0.0f;
            float optimizedDistance = 0.0f;
            AStarPathfinder2DTraditional.Instance.PathFind(this.StartPoint, this.GoalPoint, r => 
            {
                basicTime += (System.DateTime.Now - now).TotalSeconds;
                basicDistance = DrawLine(r);
                this.distance = basicDistance;
                this.goled = true;
            }, Tsl.Math.Pathfinder.AStarPathfinder2D.ExecuteMode.Sync);

            while (!this.goled) yield return null;
            yield return null;

            Reset();
            this.goled = false;
            AStarPathfinder2DOptimized.Instance.MapMake();
            now = System.DateTime.Now;
            AStarPathfinder2DOptimized.Instance.PathFind(this.StartPoint, this.GoalPoint, r => 
            {
                r = AStarPathfinder2DOptimized.Instance.FillGrid(r);
                optimizedTime += (System.DateTime.Now - now).TotalSeconds;
                optimizedDistance = DrawLine(r);
                this.goled = true;
            }, Tsl.Math.Pathfinder.AStarPathfinder2D.ExecuteMode.Sync);
            while (!this.goled) yield return null;
            yield return null;

            if (basicDistance != 0.0f)
            {
                ++testCount;
                if ((AStarPathfinder2DOptimized.Instance.GridMode && Mathf.Abs(basicDistance - optimizedDistance) > 0.01f)
                    || (!AStarPathfinder2DOptimized.Instance.GridMode 
                        && (basicDistance/ optimizedDistance) < 0.9f || (optimizedDistance / basicDistance) < 0.9f))
                {
                    Debug.LogWarning(string.Format("distance not equal opt:{0} as {1}", optimizedDistance, basicDistance));
                    if (AStarPathfinder2DOptimized.Instance.GridMode)
                    {
                        break;
                    }
                }
            }

            TestText.text = string.Format("{0} tests\n       basic: {1:0.000} / {2:0.000}\noptimized: {3:0.000} / {4:0.000}",
                             testCount, basicTime, basicTime/testCount, optimizedTime,  optimizedTime/testCount);

        }
    }
}
