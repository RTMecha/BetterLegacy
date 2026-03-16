using System.Collections.Generic;

using UnityEngine;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Runtime.Objects.Visual
{
    /// <summary>
    /// Class for polygon shape objects.
    /// </summary>
    public class PolygonObject : SolidObject
    {
        public PolygonObject(GameObject gameObject, float opacity, bool hasCollider, bool solid, int renderType, bool opacityCollision, int gradientType, float gradientScale, float gradientRotation, int colorBlendMode, PolygonShape polygonShape) : base(gameObject, opacity, hasCollider, solid, renderType, opacityCollision, gradientType, gradientScale, gradientRotation, colorBlendMode)
        {
            meshFilter = gameObject.GetComponent<MeshFilter>();

            polygonCollider = collider as PolygonCollider2D;

            UpdatePolygon(polygonShape);
        }

        #region Values

        MeshFilter meshFilter;
        PolygonCollider2D polygonCollider;

        float radius = 0.5f;
        int sides = 3;
        float roundness;
        float thickness = 1f;
        int slices = 3;
        Vector2 thicknessOffset;
        Vector2 thicknessScale = Vector2.one;
        float thicknessRotation = 0f;
        float angle = 0f;
        float alternate = 1f;
        List<Vector2> points;
        PolygonType type;

        #endregion

        #region Functions

        public override void Clear()
        {
            base.Clear();
            meshFilter = null;
            polygonCollider = null;
        }

        #region Update Polygon

        /// <summary>
        /// Updates the custom polygon.
        /// </summary>
        /// <param name="polygonShape">Polygon shape values to apply.</param>
        public void UpdatePolygon(PolygonShape polygonShape)
        {
            if (polygonShape.Type == PolygonType.Vector && polygonShape.Points != null)
                UpdatePolygon(polygonShape.Points);
            else
                UpdatePolygon(polygonShape.Radius, polygonShape.Sides, polygonShape.Roundness, polygonShape.Thickness, polygonShape.Slices, polygonShape.ThicknessOffset, polygonShape.ThicknessScale, polygonShape.Angle, polygonShape.ThicknessRotation, polygonShape.Alternate);
        }

        /// <summary>
        /// Updates the custom polygon.
        /// </summary>
        /// <param name="points">Points to apply to the shape.</param>
        public void UpdatePolygon(List<Vector2> points)
        {
            type = PolygonType.Vector;
            this.points = points;
            UpdatePolygon();
        }

        /// <summary>
        /// Updates the custom polygon.
        /// </summary>
        /// <param name="radius">Radius of the shape.</param>
        /// <param name="sides">How many sides the shape has.</param>
        /// <param name="roundness">Roundness of the shapes' corners.</param>
        /// <param name="thickness">Thickness of the shape outline.</param>
        /// <param name="slices">Slices to cut away from the shape.</param>
        /// <param name="thicknessOffset">Offset of the center outline.</param>
        /// <param name="thicknessScale">Scale of the center outline.</param>
        /// <param name="angle">Rotation offset of the polygon.</param>
        /// <param name="thicknessRotation">Rotation offset of the center outline.</param>
        public void UpdatePolygon(float radius, int sides, float roundness, float thickness, int slices, Vector2 thicknessOffset, Vector2 thicknessScale, float angle, float thicknessRotation, float alternate)
        {
            type = PolygonType.Ring;
            this.radius = radius;
            this.sides = sides;
            this.roundness = roundness;
            this.thickness = thickness;
            this.slices = slices;
            this.thicknessOffset = thicknessOffset;
            this.thicknessScale = thicknessScale;
            this.thicknessRotation = thicknessRotation;
            this.angle = angle;
            this.alternate = alternate;
            UpdatePolygon();
        }

        /// <summary>
        /// Updates the custom polygon.
        /// </summary>
        public void UpdatePolygon()
        {
            if (!meshFilter)
            {
                CoreHelper.LogError($"Mesh Filter doesn't exist!");
                return;
            }
            
            if (!polygonCollider)
            {
                CoreHelper.LogError($"Polygon Collider doesn't exist!");
                return;
            }

            if (type == PolygonType.Vector && points != null)
                VGShapes.PolygonMesh(meshFilter, polygonCollider, points);
            else
                VGShapes.RoundedRingMesh(meshFilter, polygonCollider, radius, sides, roundness, thickness, slices, thicknessOffset, thicknessScale, angle, thicknessRotation, alternate);
        }

        #endregion

        #endregion
    }
}
