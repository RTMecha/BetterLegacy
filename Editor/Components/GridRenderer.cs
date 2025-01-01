using BetterLegacy.Editor.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Editor.Components
{
    /// <summary>
    /// Timeline GridRenderer graphic class from Dev Branch.
    /// </summary>
    public class GridRenderer : Graphic
    {
        public bool isTimeline;

        public Vector2 gridCellSpacing = new Vector2(1f, 1f);
        public Vector2Int gridCellSize = new Vector2Int(1, 1);
        public Vector2 gridSize = new Vector2(1f, 1f);

        public float thickness = 2f;

        float width;

        float height;

        float cellWidth;

        float cellHeight;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            width = rectTransform.rect.width;
            height = rectTransform.rect.height;

            cellWidth = (width / gridSize.x);
            cellHeight = (height / gridSize.y);

            int x = gridCellSize.x;
            int y = gridCellSize.y;

            int num = 0;
            for (int i = 0; i < y; i++)
            {
                for (int j = 0; j < x; j++)
                {
                    DrawCell(j, i, num, vh);
                    num++;
                }
            }
        }

        public static float multi = 154f;
        void DrawCell(int x, int y, int index, VertexHelper vh)
        {
            float posX = (cellWidth * x) + (isTimeline ? (RTEditor.inst.bpmOffset * multi) : 0f);
            float posY = cellHeight * y;

            var simpleVert = UIVertex.simpleVert;

            simpleVert.color = color;

            simpleVert.position = new Vector3(posX, posY);
            vh.AddVert(simpleVert);

            simpleVert.position = new Vector3(posX, posY + cellHeight);
            vh.AddVert(simpleVert);

            simpleVert.position = new Vector3(posX + cellWidth, posY + cellHeight);
            vh.AddVert(simpleVert);

            simpleVert.position = new Vector3(posX + cellWidth, posY);
            vh.AddVert(simpleVert);

            float thick = Mathf.Sqrt(thickness * thickness / 2f);
            simpleVert.position = new Vector3(posX + thick, posY + thick);
            vh.AddVert(simpleVert);

            simpleVert.position = new Vector3(posX + thick, posY + (cellHeight - thick));
            vh.AddVert(simpleVert);

            simpleVert.position = new Vector3(posX + (cellWidth - thick), posY + (cellHeight - thick));
            vh.AddVert(simpleVert);

            simpleVert.position = new Vector3(posX + (cellWidth - thick), posY + thick);
            vh.AddVert(simpleVert);

            int tri = index * 8;
            vh.AddTriangle(tri, tri + 1, tri + 5);
            vh.AddTriangle(tri + 5, tri + 4, tri);
            vh.AddTriangle(tri + 1, tri + 2, tri + 6);
            vh.AddTriangle(tri + 6, tri + 5, tri + 1);
            vh.AddTriangle(tri + 2, tri + 3, tri + 7);
            vh.AddTriangle(tri + 7, tri + 6, tri + 2);
            vh.AddTriangle(tri + 3, tri, tri + 4);
            vh.AddTriangle(tri + 4, tri + 7, tri + 3);
        }

        public void UpdateUI()
        {
            base.SetVerticesDirty();
        }
    }
}
