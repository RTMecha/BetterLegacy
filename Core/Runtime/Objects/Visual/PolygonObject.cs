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

        public PolygonObject(GameObject gameObject, float opacity, bool hasCollider, bool solid, int renderType, bool opacityCollision, int gradientType, float gradientScale, float gradientRotation, int colorBlendMode, PolygonShape polygonShape) : base(gameObject, opacity, hasCollider, solid, renderType, opacityCollision, gradientType, gradientScale, gradientRotation, colorBlendMode)
        {
            meshFilter = gameObject.GetComponent<MeshFilter>();

            polygonCollider = collider as PolygonCollider2D;

            UpdatePolygon(polygonShape);
        }

        /// <summary>
        /// Updates the custom polygon.
        /// </summary>
        /// <param name="polygonShape">Polygon shape values to apply.</param>
        public void UpdatePolygon(PolygonShape polygonShape) => UpdatePolygon(polygonShape.Radius, polygonShape.Sides, polygonShape.Roundness, polygonShape.Thickness, polygonShape.Slices, polygonShape.ThicknessOffset, polygonShape.ThicknessScale, polygonShape.Angle, polygonShape.ThicknessRotation);

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
        /// <param name="rotation">Rotation offset of the polygon.</param>
        public void UpdatePolygon(float radius, int sides, float roundness, float thickness, int slices, Vector2 thicknessOffset, Vector2 thicknessScale, float angle, float thicknessRotation)
        {
            this.radius = radius;
            this.sides = sides;
            this.roundness = roundness;
            this.thickness = thickness;
            this.slices = slices;
            this.thicknessOffset = thicknessOffset;
            this.thicknessScale = thicknessScale;
            this.thicknessRotation = thicknessRotation;
            this.angle = angle;
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

            VGShapes.RoundedRingMesh(meshFilter, polygonCollider, radius, sides, roundness, thickness, slices, thicknessOffset, thicknessScale, angle, thicknessRotation);
        }

        public override void Clear()
        {
            base.Clear();
            meshFilter = null;
            polygonCollider = null;
        }
    }
}
