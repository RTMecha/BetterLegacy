using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace BetterLegacy.Components
{
    public class ActiveState : MonoBehaviour
    {
        /// <summary>
        /// Function to run when the object is enabled / disabled.
        /// </summary>
        public Action<bool> onStateChanged;

        void OnEnable() => onStateChanged?.Invoke(true);

        void OnDisable() => onStateChanged?.Invoke(false);
    }
}
