using System.Collections.Generic;

using BetterLegacy.Configs;
using BetterLegacy.Core.Data;

namespace BetterLegacy.Core.Runtime.Objects
{
    // keeps spawned prefabs around so we can reuse them instead of rebuilding
    // keyed by id + repeat count since repeat count changes how many objects get made
    public class PrefabPool : Exists
    {
        readonly Dictionary<string, Stack<RTPrefabObject>> pools = new Dictionary<string, Stack<RTPrefabObject>>();
        public const int DEFAULT_CAP = 32;
        // max pooled instances we keep per prefab
        public static int Cap => CoreConfig.Instance != null ? CoreConfig.Instance.PrefabPoolMaxPerPrefab.Value : DEFAULT_CAP;

        static string GetKey(string prefabID, int repeatCount) => prefabID + "_" + repeatCount;

        // grab a pooled one if we have it
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

        // put it back in the pool, or just kill it if we're full
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

        // clear it all out (level unload)
        public void Clear()
        {
            foreach (var stack in pools.Values)
                while (stack.Count > 0)
                    stack.Pop()?.Clear();
            pools.Clear();
        }
    }
}
