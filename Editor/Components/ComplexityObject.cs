using System;

using UnityEngine;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Editor.Components
{
    /// <summary>
    /// Component for updating a UI elements' editor complexity.
    /// </summary>
    public class ComplexityObject : MonoBehaviour
    {
        [SerializeField]
        /// <summary>
        /// Complexity of the element.
        /// </summary>
        public Complexity complexity;
        [SerializeField]
        /// <summary>
        /// If the element can only appear on the exact complexity.
        /// </summary>
        public bool onlySpecificComplexity;
        [SerializeField]
        /// <summary>
        /// Path of the complexity value in the complexity.json file.
        /// </summary>
        public string path;
        [SerializeField]
        /// <summary>
        /// If complexity detection is disabled.
        /// </summary>
        public bool disabled;
        [SerializeField]
        /// <summary>
        /// If the element is active with <see cref="disabled"/> is on.
        /// </summary>
        public bool active = true;
        /// <summary>
        /// Gets the additional visible state of the element (e.g. for cases where an element should be disabled in a specific state).
        /// </summary>
        public Func<bool> visible;
        /// <summary>
        /// If the element should be visible based on the <see cref="visible"/> function.
        /// </summary>
        public bool Visible => visible == null || visible.Invoke();
        /// <summary>
        /// Function to run when the active state is updated.
        /// </summary>
        public Action<ComplexityObject> onUpdate;

        /// <summary>
        /// Updates the active state of all elements.
        /// </summary>
        public static void UpdateAll()
        {
            var objs = FindObjectsOfType<ComplexityObject>();
            for (int i = 0; i < objs.Length; i++)
                objs[i].UpdateActiveState();
        }

        /// <summary>
        /// Updates the active state of this element.
        /// </summary>
        public void UpdateActiveState()
        {
            try
            {
                onUpdate?.Invoke(this);
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Could not run {nameof(onUpdate)} function due to the exception: {ex}");
            }

            if (disabled)
            {
                CoreHelper.SetGameObjectActive(gameObject, active && Visible);
                return;
            }
            CoreHelper.SetGameObjectActive(gameObject, EditorHelper.CheckComplexity(complexity, onlySpecificComplexity) && Visible);
        }
    }
}
