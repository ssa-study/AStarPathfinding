using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using AStarPathfinder = Tsl.Math.Pathfinder.AStarPathfinder;
using AstarCell  = Tsl.Math.Pathfinder.AstarCell;

public class SceneBehaviour3DMap : MonoBehaviour {

    public Rect MapRect = new Rect(-50,-50, 100, 100); // マップ全体の大きさ
    public float TileSize = 1.0f;
    public float RayCastY = 0.5f; // 光線追跡するときのy位置
    public List<string> DisallowTags = new List<string>();
    public bool DrawNodeInfomation = true; 
    public Transform MapRoot;
    public Transform StartObject;
    public Transform GoalObject;

    public UnityEngine.UI.Text MessageText;
    public UnityEngine.UI.Text TestText;


    private bool goled = false;
    private float distance = 0;

    // Use this for initialization
    void Start () {
        AStarPathfinder.Instance.MapInit(this.MapRect, this.TileSize);
    }

    private int interval = 0;
    private void Update()
    {
        if (this.interval-- == 0)
        {
            var info = AStarPathfinder.Instance.Info();
            this.MessageText.text = string.Format("{0} Nodes {1} Blocks\n{2} Links\n{3} Paths\ndistance={4}",
                info[(int)AstarCell.Type.Empty] + info[(int)AstarCell.Type.Open] + info[(int)AstarCell.Type.Close],
                info[(int)AstarCell.Type.Block],
                info[(int)AstarCell.Type.Links],
                AStarPathfinder.Instance.PathCount,
                this.distance);
            this.interval = 30;
        }

        if (this.DrawNodeInfomation)
        {
            AStarPathfinder.Instance.EachCell(drawCell);
            if (this.goled)
            {
                AStarPathfinder.Instance.EachCell(drawCellCorrect);
            }
        }
    }

    private void drawCell(AstarCell cell)
    {
        float x=  cell.Position.x;
        float y = cell.Position.y;
        float t = this.TileSize * 0.4f;
        Color[] coltbl = { Color.green, // empty
                            Color.blue, Color.yellow, Color.white, Color.gray, Color.black, Color.red, new Color(0.1f,0.1f,0.1f,0.1f),
                            Color.red };
        var color = coltbl[(int)cell.CellType];
        if (cell.CellType != Tsl.Math.Pathfinder.AstarCell.Type.Removed)
        {
            Debug.DrawLine(new Vector3(x - t, 0.1f, y - t), new Vector3(x + t, 0.1f, y - t), color, 1.0f, false);
            Debug.DrawLine(new Vector3(x + t, 0.1f, y - t), new Vector3(x + t, 0.1f, y + t), color, 1.0f, false);
            Debug.DrawLine(new Vector3(x + t, 0.1f, y + t), new Vector3(x - t, 0.1f, y + t), color, 1.0f, false);
            Debug.DrawLine(new Vector3(x - t, 0.1f, y + t), new Vector3(x - t, 0.1f, y - t), color, 1.0f, false);
            
            foreach(var r in cell.Related)
            {
                var p = r.cell.Position;
                Debug.DrawLine(new Vector3(x, 0.1f, y), new Vector3(p.x, 0.1f, p.y), new Color(0.0f,1.0f,1.0f,0.2f));
            }
        }
    }
    private void drawCellCorrect(AstarCell cell)
    {
        if (cell.CellType == AstarCell.Type.Correct || cell.CellType == AstarCell.Type.Start || cell.CellType == AstarCell.Type.Goal)
        {
            foreach(var r in cell.Related)
            {
                if (r.cell.CellType == Tsl.Math.Pathfinder.AstarCell.Type.Correct)
                {
                    var p = r.cell.Position;
                    Debug.DrawLine(new Vector3(cell.Position.x, 0.1f, cell.Position.y), new Vector3(p.x, 0.1f, p.y), Color.red, 1.0f, false);
                }
            }
        }
    }

    public void Reset()
    {
        AStarPathfinder.Instance.Reset();
        AStarPathfinder.Instance.MapMake();
        this.goled = false;
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
        AStarPathfinder.Instance.PathFind(toVector2(this.StartObject.position), toVector2(this.GoalObject.position), r => 
        {
            var past = (System.DateTime.Now - now).TotalSeconds;
            this.TestText.text = string.Format("{0} second", past);
            DrawLine(r);
            this.goled = true;
        }, Tsl.Math.Pathfinder.AStarPathfinder2DGrid.ExecuteMode.Sync);
    }

    private void DrawLine(List<Vector2> lines)
    {
        this.distance = 0.0f;
        if (lines != null)
        {
            for (int i = 0; i < lines.Count - 1; ++i)
            {
                this.distance += (lines[i+1] - lines[i]).magnitude;
            }
        }
    }

    public void OnClickMapMakeButton()
    {
        AStarPathfinder.Instance.MapMakeFromScene(this.RayCastY, this.DisallowTags);
    }

    public void OnClickRandomMake()
    {
    #if false
        for (int n = 0; n < 10; ++n)
        {
            int l = UnityEngine.Random.Range(1,10);
            Vector2 pos = new Vector2(Random.Range(this.MapRect.xMin, this.MapRect.width),
                                      Random.Range(this.MapRect.yMin, this.MapRect.height));
            bool dir = Random.Range(0,2) == 0;
            while(l-- != 0)
            {
                if (!this.MapRect.Contains(pos)) break;
                AStarPathfinder2D.Instance.CellMap(pos).CellType = Tsl.Math.Pathfinder.AstarCell.Type.Block;
                pos.x += dir ? this.TileSize : 0.0f;
                pos.y += dir ? 0.0f : this.TileSize;
            }
        }
    #endif
    }
    
    public void OnClickClear()
    {
        AStarPathfinder.Instance.EachCell(cell => cell.CellType = AstarCell.Type.Removed);
    }

    public void OnClickAutoTest()
    {
        StartCoroutine(AutoTest());
    }
    private IEnumerator AutoTest()
    {
        double basicTime = 0.0;
        int testCount = 0;
        for (int cnt = 0; cnt < 100; ++cnt)
        {
            OnClickClear();
            OnClickRandomMake();
            OnClickRandomMake();
            OnClickRandomMake();
            Reset();
            OnClickMapMakeButton();
            yield return null;

            this.goled = false;
            var now = System.DateTime.Now;
            AStarPathfinder.Instance.PathFind(toVector2(this.StartObject.position), toVector2(this.GoalObject.position), r => 
            {
                basicTime += (System.DateTime.Now - now).TotalSeconds;
                DrawLine(r);
                this.goled = true;
            }, Tsl.Math.Pathfinder.AStarPathfinder2D.ExecuteMode.Sync);

            while (!this.goled) yield return null;
            yield return null;
            ++testCount;
            TestText.text = string.Format("{0} tests\n{1:0.000} / {2:0.000}", testCount, basicTime, basicTime/testCount);

            yield return null;
            Reset();
        }
    }
}
