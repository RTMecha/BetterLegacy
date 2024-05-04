using BetterLegacy.Core.Data;
using UnityEngine;

namespace BetterLegacy.Components
{
    public class DestroyModifierResult : MonoBehaviour
    {
        void OnDestroy()
        {
            if (Modifier != null)
                Modifier.Result = null;
        }

        public Modifier<BeatmapObject> Modifier { get; set; }
    }
}
