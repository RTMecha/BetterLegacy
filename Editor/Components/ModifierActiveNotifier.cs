﻿using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

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
                notifier.color = LSColors.fadeColor(notifier.color, (modifierBase.type == ModifierBase.Type.Action ? modifierBase.running : modifierBase.triggered) ? 1f : 0.1f);
        }
    }
}
