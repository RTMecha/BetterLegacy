using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Data.Beatmap;

namespace BetterLegacy.Editor.Components
{
    public class ModifierActiveNotifier : MonoBehaviour
    {
        public ModifierBase modifierBase;
        public Image notifier;

        void Update()
        {
            if (notifier && modifierBase != null)
                notifier.color = RTColors.FadeColor(notifier.color, (modifierBase.type == ModifierBase.Type.Action ? modifierBase.running : modifierBase.triggered) ? 1f : 0.1f);
        }
    }
}
