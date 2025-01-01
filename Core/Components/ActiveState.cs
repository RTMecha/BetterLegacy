using System;

using UnityEngine;

namespace BetterLegacy.Core.Components
{
    /// <summary>
    /// Runs a function when the <see cref="GameObject.activeSelf"/> state is changed.
    /// </summary>
    public class ActiveState : MonoBehaviour
    {
        /// <summary>
        /// Function to run when the object is enabled / disabled.
        /// </summary>
        public Action<bool> onStateChanged;

        /// <summary>
        /// Sets the active state changed function.
        /// </summary>
        /// <param name="onStateChanged">Function to set.</param>
        public void SetFunction(Action<bool> onStateChanged) => this.onStateChanged = onStateChanged;

        void OnEnable() => onStateChanged?.Invoke(true);

        void OnDisable() => onStateChanged?.Invoke(false);
    }
}
