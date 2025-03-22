using UnityEngine;

using BetterLegacy.Core.Data.Beatmap;

namespace BetterLegacy.Core.Optimization.Objects.Visual
{
    /// <summary>
    /// Class for polygon shape objects.
    /// </summary>
    public class PolygonObject : SolidObject
    {
        public PolygonObject(GameObject gameObject, float opacity, bool hasCollider, bool solid, int renderType, bool opacityCollision, int gradientType, float gradientScale, float gradientRotation, PolygonShape polygonShape) : base(gameObject, opacity, hasCollider, solid, renderType, opacityCollision, gradientType, gradientScale, gradientRotation)
        {

        }
    }
}
