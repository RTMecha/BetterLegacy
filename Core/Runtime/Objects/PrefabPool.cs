using System.Collections.Generic;

using BetterLegacy.Configs;
using BetterLegacy.Core.Data;

namespace BetterLegacy.Core.Runtime.Objects
{
    /// <summary>
    /// Handles pooling of <see cref="RTPrefabObject"/>.
    /// </summary>
    public class PrefabPool : Exists
    {
        #region Values

        readonly Dictionary<string, Stack<RTPrefabObject>> pools = new Dictionary<string, Stack<RTPrefabObject>>();

        /// <summary>
        /// The default cap.
        /// </summary>
        public const int DEFAULT_CAP = 32;

        /// <summary>
        /// Total amount of allowed pooled prefabs.
        /// </summary>
        public static int Cap => CoreConfig.Instance != null ? CoreConfig.Instance.PrefabPoolMaxPerPrefab.Value : DEFAULT_CAP;

        #endregion

        #region Functions

        static string GetKey(string prefabID, int repeatCount) => prefabID + "_" + repeatCount;

        /// <summary>
        /// Tries to get a pooled <see cref="RTPrefabObject"/>.
        /// </summary>
        /// <param name="prefabID">Prefab reference ID.</param>
        /// <param name="repeatCount">Repeat count.</param>
        /// <param name="runtimeObject">Runtime object of the prefab object.</param>
        /// <returns>Returns <see langword="true"/> if a pooled prefab was taken out of the pool, otherwise returns <see langword="false"/>.</returns>
        public bool TryGet(string prefabID, int repeatCount, out RTPrefabObject runtimeObject)
        {
            runtimeObject = null;
            if (pools.TryGetValue(GetKey(prefabID, repeatCount), out var stack) && stack.Count > 0)
            {
                runtimeObject = stack.Pop();
                return runtimeObject;
            }
            return false;
        }

        /// <summary>
        /// Returns a <see cref="RTPrefabObject"/> to the pool.
        /// </summary>
        /// <param name="runtimeObject">Runtime object to return.</param>
        public void Return(RTPrefabObject runtimeObject)
        {
            if (!runtimeObject || !runtimeObject.PrefabObject || !runtimeObject.Prefab)
            {
                runtimeObject?.Clear();
                return;
            }
            var key = GetKey(runtimeObject.Prefab.id, runtimeObject.PrefabObject.RepeatCount);
            if (!pools.TryGetValue(key, out var stack))
                pools[key] = stack = new Stack<RTPrefabObject>();
            if (stack.Count >= Cap)
            {
                runtimeObject.Clear();
                return;
            }
            stack.Push(runtimeObject);
        }

        /// <summary>
        /// Clears the pool.
        /// </summary>
        public void Clear()
        {
            foreach (var stack in pools.Values)
                while (stack.Count > 0)
                    stack.Pop()?.Clear();
            pools.Clear();
        }

        #endregion
    }
}