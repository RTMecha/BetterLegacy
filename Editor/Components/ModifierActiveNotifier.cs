
using UnityEngine;

using BetterLegacy.Core.Data;

namespace BetterLegacy.Editor.Components
{
    public class ModifierActiveNotifier : MonoBehaviour
    {
        public ModifierBase modifierBase;
        public GameObject notifier;

        void Update()
        {
            if (notifier && modifierBase != null)
                notifier.SetActive(modifierBase.type == ModifierBase.Type.Action ? modifierBase.running : modifierBase.triggered);
        }
    }
}
