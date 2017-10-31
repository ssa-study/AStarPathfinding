using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AstarCell = Tsl.Math.Pathfinder.AstarCell;

namespace Tsl.UI.Pathfinder
{
    public class Cell : MonoBehaviour
    {
        public Text Text;
        public Image BaseImage;
        public Image Image;
        public AstarCell AstarCell;
        public Color[] CellColor = new Color[9];
        public Color[] CostColor = new Color[10];

        public AstarCell.Type CellType
        {
            get { return this.AstarCell.CellType; }
            set { this.AstarCell.CellType = value; }
        }
        // Use this for initialization
        void Start()
        {
            var btn = GetComponent<Button>();
            btn.onClick.AddListener(() => { onClick(); });
        }

        // Update is called once per frame
        void Update()
        {
            if (this.AstarCell != null)
            {
                this.Image.color = this.CellColor[(int)this.AstarCell.CellType];
                this.Text.text = string.Format("C:{0:0.00}\nH:{1:0.00}\nS:{2:0.00}",
                    this.AstarCell.Cost, this.AstarCell.Hint, this.AstarCell.Score);
                if (this.BaseImage != null)
                {
                    this.BaseImage.color = costColor(this.AstarCell.MoveCost);
                }
            }
        }

        private Color costColor(float cost)
        {
            if (cost < 1.0f) return Color.white;
            if (cost > 10.0f) return Color.black;
            int c = (int)(cost - 1.0f);
            return this.CostColor[c];
        }

        private void onClick()
        {
            if (/*this.CellType == AstarCell.Type.Empty || */this.CellType == AstarCell.Type.Correct)
            {
                Tsl.Math.Pathfinder.AStarPathfinder2DOptimized.Instance.setGridRelatedSearchRaycast(this.AstarCell);
                var newtype = this.CellType == AstarCell.Type.Empty ? AstarCell.Type.Correct : AstarCell.Type.Empty;
                foreach (var cell in this.AstarCell.Related)
                {
                    cell.cell.CellType = newtype;
                }
                this.CellType = newtype;
            }
            else if (this.CellType == AstarCell.Type.Block) this.CellType = AstarCell.Type.Removed;
            else if (this.CellType == AstarCell.Type.Removed || this.CellType == AstarCell.Type.Empty)
            {
                if (this.BaseImage != null)
                {
                    if (this.AstarCell.MoveCost < 10.0f)
                    {
                        this.AstarCell.MoveCost += 1.0f;
                    }
                    else
                    {
                        this.CellType = AstarCell.Type.Block;
                        this.AstarCell.MoveCost = 1.0f;
                    }
                }
                else
                {
                        this.CellType = AstarCell.Type.Block;
                }
            }
        }
    }

}

