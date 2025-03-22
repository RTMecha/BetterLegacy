using UnityEngine;

using BetterLegacy.Core.Data.Beatmap;

namespace BetterLegacy.Core.Optimization.Objects.Visual
{
    /// <summary>
    /// Class for polygon shape objects.
    /// </summary>
    public class PolygonObject : SolidObject
    {
        public PolygonObject(GameObject gameObject, float opacity, bool hasCollider, bool solid, bool background, bool opacityCollision, int gradientType, float gradientScale, float gradientRotation, PolygonShape polygonShape) : base(gameObject, opacity, hasCollider, solid, background, opacityCollision, gradientType, gradientScale, gradientRotation)
        {

        }
    }
}
