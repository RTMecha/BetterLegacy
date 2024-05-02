using BetterLegacy.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
