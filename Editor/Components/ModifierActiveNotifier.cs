using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Data.Beatmap;

namespace BetterLegacy.Editor.Components
{
    public class ModifierActiveNotifier : MonoBehaviour
    {
        public Modifier modifier;
        public Image notifier;

        void Update()
        {
            if (notifier && modifier)
                notifier.color = RTColors.FadeColor(notifier.color, (modifier.type == Modifier.Type.Action ? modifier.running : modifier.triggered) ? 1f : 0.1f);
        }
    }
}
