using System.Collections.Generic;
using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
namespace BetterLegacy.Core.Runtime.Objects
/// This shit was deadass just stolen from my kirby game </3
{
    public class PrefabPool : Exists
    {
        readonly Dictionary<string, Stack<RTPrefabObject>> pools = new Dictionary<string, Stack<RTPrefabObject>>();
        public const int DEFAULT_CAP = 32;
        public static int Cap => CoreConfig.Instance != null ? CoreConfig.Instance.PrefabPoolMaxPerPrefab.Value : DEFAULT_CAP;
        static string GetKey(string prefabID, int repeatCount) => prefabID + "_" + repeatCount;
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
        public void Clear()
        {
            foreach (var stack in pools.Values)
                while (stack.Count > 0)
                    stack.Pop()?.Clear();
            pools.Clear();
        }
    }
}