using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using BetterLegacy.Core.Data;

namespace BetterLegacy.Components.Editor
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
