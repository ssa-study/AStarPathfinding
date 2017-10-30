using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using AStarPathfinder2DHex = Tsl.Math.Pathfinder.AStarPathfinder2DHex;
using AstarCell = Tsl.Math.Pathfinder.AstarCell;

public class SceneBehaviourUIHexMap : MonoBehaviour {

    public Transform MapRoot;
    public GameObject CellPrefab;
    public Rect MapRect = new Rect(0,0,16,16);
    public float TileSize = 1.0f;
    public Vector2 StartPoint = new Vector2(0, 0);
    public Vector2 GoalPoint;
    public UnityEngine.UI.Text MessageText;
    public UnityEngine.UI.Text TestText;

    private bool goled = false;

    private Tsl.UI.Pathfinder.Cell[,] cellMap;
    private float distance = 0.0f;

    // Use this for initialization
    void Start () {
        AStarPathfinder2DHex.Instance.MapInit(32,16);
        int w = (int)this.MapRect.width;
        int h = (int)this.MapRect.height;
        this.cellMap = new Tsl.UI.Pathfinder.Cell[w,h];
        float scale = 300.0f / 16.0f;
        float xofs = 20.0f -160.0f;
        float yofs = -20.0f + 160.0f;
        for (int y = 0; y < w; ++y)
        {
            for (int x = 0; x < h; ++x)
            {
                var cell = Instantiate(CellPrefab.gameObject) as GameObject;
                this.cellMap[x, y] = cell.GetComponent<Tsl.UI.Pathfinder.Cell>();
                cell.transform.SetParent(this.MapRoot, false);
                var acell = AStarPathfinder2DHex.Instance.CellMap(x, y);
                this.cellMap[x, y].AstarCell = acell;
                cell.transform.localPosition = new Vector3(xofs + acell.Position.x * scale, yofs - acell.Position.y * scale, 0.0f);
                if (x == 0 && y == 0)
                {
                    this.StartPoint = acell.Position;
                }
                if (x == h-1 && y == w - 1)
                {
                    this.GoalPoint = acell.Position;
                }
            }

        }
        AStarPathfinder2DHex.Instance.MapMake();
    }

    private void Update()
    {
        var info = AStarPathfinder2DHex.Instance.Info();
        this.MessageText.text = string.Format("{0} Nodes\n{1} Links\n{2} Paths\ndistance={3}",
            AStarPathfinder2DHex.Instance.NumOfNodes(info),
            AStarPathfinder2DHex.Instance.NumOfLinks(info),
            AStarPathfinder2DHex.Instance.PathCount,
            this.distance);

    }

    public void Reset()
    {
        AStarPathfinder2DHex.Instance.Reset();
        this.goled = false;
    }
    bool usingOptimize = false;

    public void OnClickStartButton()
    {
        AStarPathfinder2DHex.Instance.MapMake();
        if (this.goled)
        {
            Reset();
        }
        else
        {
            AStarPathfinder2DHex.Instance.PathFind(this.StartPoint, this.GoalPoint, r =>
            {
                this.distance = DrawLine(r);
                this.goled = true;
            });
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
        }
        else
        {
            this.usingOptimize = false;
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
                AStarPathfinder2DHex.Instance.CellMap((int)pos.x, (int)pos.y).CellType = AstarCell.Type.Block;
                pos.x += dir ? this.TileSize : 0.0f;
                pos.y += dir ? 0.0f : this.TileSize;
            }
        }
    }
    
    public void OnClickClear()
    {
        AStarPathfinder2DHex.Instance.EachCell(cell => cell.CellType = AstarCell.Type.Removed);
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

            while (true)
            {
                int col = Random.Range(0, 32);
                int row = Random.Range(0, 16);
                var cell = AStarPathfinder2DHex.Instance.CellMap(col, row);
                if (cell != null && cell.CellType != AstarCell.Type.Block)
                {
                    this.StartPoint = cell.Position;
                    break;
                }
            }
            while (true)
            {
                int col = Random.Range(28, 32);
                int row = Random.Range(12, 16);
                var cell = AStarPathfinder2DHex.Instance.CellMap(col, row);
                if (cell != null && cell.CellType != AstarCell.Type.Block)
                {
                    this.GoalPoint = cell.Position;
                    break;
                }
            }
            this.goled = false;
            var now = System.DateTime.Now;
            float basicDistance = 0.0f;
            float optimizedDistance = 0.0f;
            AStarPathfinder2DHex.Instance.PathFind(this.StartPoint, this.GoalPoint, r => 
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
            now = System.DateTime.Now;
            AStarPathfinder2DHex.Instance.PathFind(this.StartPoint, this.GoalPoint, r => 
            {
                optimizedTime += (System.DateTime.Now - now).TotalSeconds;
                optimizedDistance = DrawLine(r);
                this.goled = true;
            }, Tsl.Math.Pathfinder.AStarPathfinder2D.ExecuteMode.Sync);
            while (!this.goled) yield return null;
            yield return null;

           

            TestText.text = string.Format("{0} tests\n       basic: {1:0.000} / {2:0.000}\noptimized: {3:0.000} / {4:0.000}",
                             testCount, basicTime, basicTime/testCount, optimizedTime,  optimizedTime/testCount);

        }
    }
}
