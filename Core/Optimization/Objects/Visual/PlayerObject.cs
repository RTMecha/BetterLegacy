using BetterLegacy.Core.Components.Player;
using UnityEngine;

namespace BetterLegacy.Core.Optimization.Objects.Visual
{
    /// <summary>
    /// Class for special player objects.
    /// </summary>
    public class PlayerObject : VisualObject
    {
        public RTPlayer player;

        public PlayerObject(GameObject gameObject, int playerIndex, bool dontRotate, int shapeOption)
        {
            this.gameObject = gameObject;

            player = gameObject.transform.parent.GetComponent<RTPlayer>();
            player.Model = ObjectManager.inst.objectPrefabs[9].options[shapeOption].GetComponent<RTPlayer>().Model;
            player.playerIndex = playerIndex;
            player.CanRotate = !dontRotate;
        }

        public override void SetColor(Color color) { }

        public override Color GetPrimaryColor() => Color.white;

        public override void Clear()
        {
            base.Clear();
        }
    }
}
