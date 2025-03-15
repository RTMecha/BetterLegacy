using UnityEngine;

namespace BetterLegacy.Core.Optimization.Objects.Visual
{
    /// <summary>
    /// Class for special player objects.
    /// </summary>
    public class PlayerObject : VisualObject
    {
        public PlayerObject(GameObject gameObject) => this.gameObject = gameObject;

        public override void SetColor(Color color) { }

        public override Color GetPrimaryColor() => Color.white;

        public override void Clear()
        {
            base.Clear();
        }
    }
}
