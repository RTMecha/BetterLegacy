using BetterLegacy.Core.Data.Beatmap;
using UnityEngine;

namespace BetterLegacy.Core.Components
{
    /// <summary>
    /// Component for setting a modifiers' result to null when the GameObject is destroyed.
    /// </summary>
    public class DestroyModifierResult : MonoBehaviour
    {
        void OnDestroy()
        {
            if (Modifier != null)
                Modifier.Result = null;
        }

        /// <summary>
        /// Modifier reference.
        /// </summary>
        public Modifier<BeatmapObject> Modifier { get; set; }
    }
}
