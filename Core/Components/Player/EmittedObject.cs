using UnityEngine;

namespace BetterLegacy.Core.Components.Player
{
    /// <summary>
    /// Represents a spawned object.
    /// </summary>
    public class EmittedObject : RTPlayerObject
    {
        #region Values

        public float opacity;
        public float colorTween;
        public int startColor;
        public int endColor;
        public string startCustomColor;
        public string endCustomColor;

        #endregion

        #region Functions

        public override void UpdateObject(int index)
        {
            base.UpdateObject(index);
            if (!visualObject || !visualObject.activeSelf || !renderer)
                return;

            var startColor = RTColors.GetPlayerColor(index, this.startColor, opacity, startCustomColor);
            var endColor = RTColors.GetPlayerColor(index, this.endColor, opacity, endCustomColor);
            renderer.material.color = Color.Lerp(startColor, endColor, colorTween);
        }

        #endregion
    }

}
