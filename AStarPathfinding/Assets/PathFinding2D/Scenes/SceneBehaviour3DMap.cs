using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using AStarPathfinder3DMap = Tsl.Math.Pathfinder.AStarPathfinder3DMap;
using AstarCell  = Tsl.Math.Pathfinder.AstarCell;



public class SceneBehaviour3DMap : MonoBehaviour {
    [System.Serializable]
    public class BlockData
    {
        public int Count;
        public Vector3 BlockSize;
    }

    public Rect MapRect = new Rect(-50,-50, 100, 100); // マップ全体の大きさ
    public float TileSize = 1.0f;
    public float RayCastY = 0.5f; // 光線追跡するときのy位置
    public Transform MapRoot;
    public Transform StartObject;
    public Transform GoalObject;
    public GameObject BlockPrefab;
    public List<BlockData> Blocks = new List<BlockData>();

    public UnityEngine.UI.Text MessageText;
    public UnityEngine.UI.Text TestText;


    private bool goled = false;
    private float distance = 0;

    private int fixedUpdateCount = 0;
    private List<Vector2> lines;

    // Use this for initialization
    void Start () 
    {
        makeBlocks();
        AStarPathfinder3DMap.Instance.MapInit(this.MapRect, this.TileSize);
    }

    void FixedUpdate()
    {
        ++this.fixedUpdateCount;
    }

    private int interval = 0;
    private void Update()
    {
        if (this.interval-- == 0)
        {
            if (AStarPathfinder3DMap.Instance.MapReady)
            {
                var info = AStarPathfinder3DMap.Instance.Info();
                this.MessageText.text = string.Format("{0} Nodes {1} Blocks\n{2} Links\n{3} Paths\ndistance={4}",
                    info[(int)AstarCell.Type.Empty] + info[(int)AstarCell.Type.Open] + info[(int)AstarCell.Type.Close],
                    info[(int)AstarCell.Type.Block],
                    info[(int)AstarCell.Type.Links],
                    AStarPathfinder3DMap.Instance.PathCount,
                    this.distance);
            }
            this.interval = 30;
        }
        if (this.lines != null)
        {
            for (int i = 0; i < lines.Count - 1; ++i)
            {
                Debug.DrawLine(new Vector3(this.lines[i].x, this.RayCastY, this.lines[i].y),
                    new Vector3(this.lines[i + 1].x, this.RayCastY+10.0f, this.lines[i + 1].y),
                    Color.magenta); //, 9999.0f, false);
            }
        }
    }


    public void Reset()
    {
        AStarPathfinder3DMap.Instance.Reset();
        AStarPathfinder3DMap.Instance.MapMake();
        this.goled = false;
        this.lines = null;
    }

    // (x,z)をvector2(x,y)に変換
    private Vector2 toVector2(Vector3 pos)
    {
        return new Vector2(pos.x, pos.z);
    }

    public void OnClickStartButton()
    {
        if (this.goled)
        {
            Reset();
        }
        var now = System.DateTime.Now;
        AStarPathfinder3DMap.Instance.PathFind(toVector2(this.StartObject.position), toVector2(this.GoalObject.position), r => 
        {
            var past = (System.DateTime.Now - now).TotalSeconds;
            this.TestText.text = string.Format("{0} second", past);
            DrawLine(r);
            this.goled = true;
        }, Tsl.Math.Pathfinder.AStarPathfinder2DGrid.ExecuteMode.Sync);
    }

    private void DrawLine(List<Vector2> lines)
    {
        this.lines = new List<Vector2>();
        this.distance = 0.0f;
        if (lines != null)
        {
            for (int i = 0; i < lines.Count - 1; ++i)
            {
                this.lines.Add(lines[i]);
                this.distance += (lines[i+1] - lines[i]).magnitude;
            }
        this.lines.Add(lines[lines.Count - 1]);
        }
}

    private void makeBlocks()
    {
        var objectRoot = this.MapRoot.Find("object");
        for (int n = 0; n < objectRoot.childCount; ++n)
        {
            var child = objectRoot.GetChild(n);
            Destroy(child.gameObject);
        }

        System.Func<Vector3> randomPosition = () =>
        {
            return new Vector3(Random.Range(this.MapRect.xMin, this.MapRect.xMax), 0.0f, Random.Range(this.MapRect.yMin, this.MapRect.yMax));
        };
        foreach(var block in this.Blocks)
        {
            for (int n = 0; n < block.Count; ++n)
            {
                var ins = Instantiate(this.BlockPrefab);
                ins.transform.SetParent(this.MapRoot.Find("object"), false);
                ins.transform.position = randomPosition();
                ins.transform.localScale = block.BlockSize;
            }
        }
    }

    public void OnClickMapMakeButton()
    {
        AStarPathfinder3DMap.Instance.MapMakeFromScene(this.RayCastY);
    }

    public void OnClickRandomMake()
    {
        OnClickClear();
        makeBlocks();
    }
    
    public void OnClickClear()
    {
        AStarPathfinder3DMap.Instance.EachCell(cell => cell.CellType = AstarCell.Type.Removed);
        this.lines = null;
    }

    public void OnClickAutoTest()
    {
        StartCoroutine(AutoTest());
    }

    private IEnumerator waitFixedUpdate()
    {
        int prev = this.fixedUpdateCount;
        while (prev == this.fixedUpdateCount) yield break;
    }

    private IEnumerator AutoTest()
    {
        double basicTime = 0.0;
        int testCount = 0;
        for (int cnt = 0; cnt < 10; ++cnt)
        {
            OnClickClear();
            yield return waitFixedUpdate();
            OnClickRandomMake();
            yield return waitFixedUpdate();
            Reset();
            yield return waitFixedUpdate();
            OnClickMapMakeButton();
            yield return waitFixedUpdate();
            yield return null;

            this.goled = false;
            var now = System.DateTime.Now;
            AStarPathfinder3DMap.Instance.PathFind(toVector2(this.StartObject.position), toVector2(this.GoalObject.position), r => 
            {
                basicTime += (System.DateTime.Now - now).TotalSeconds;
                DrawLine(r);
                this.goled = true;
            }, Tsl.Math.Pathfinder.AStarPathfinder2D.ExecuteMode.Sync);

            while (!this.goled) yield return null;
            yield return waitFixedUpdate();
            yield return null;
            ++testCount;
            TestText.text = string.Format("{0} tests\n{1:0.000} / {2:0.000}", testCount, basicTime, basicTime/testCount);

            yield return null;
            Reset();
        }
    }
}
