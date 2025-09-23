using UnityEngine;

using BetterLegacy.Core.Data.Modifiers;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Components
{
    /// <summary>
    /// Component for setting a modifiers' result to null when the GameObject is destroyed.
    /// </summary>
    public class DestroyModifierResult : MonoBehaviour
    {
        /// <summary>
        /// Initializes the destroy modifier result.
        /// </summary>
        /// <param name="gameObject">Game object to add the component to.</param>
        /// <param name="modifier">Modifier to set.</param>
        public static void Init(GameObject gameObject, Modifier modifier)
        {
            var onDestroy = gameObject.AddComponent<DestroyModifierResult>();
            onDestroy.Modifier = modifier;
        }

        void OnDestroy()
        {
            if (!Modifier)
                return;

            ModifiersHelper.OnRemoveCache(Modifier);
            Modifier.Result = default;
            Modifier = null;
        }

        /// <summary>
        /// Modifier reference.
        /// </summary>
        public Modifier Modifier { get; set; }
    }
}
