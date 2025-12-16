using UnityEngine;

namespace BetterLegacy.Core.Prefabs
{
    public class CorePrefabHolder
    {
        public static CorePrefabHolder Instance { get; set; }

        public CorePrefabHolder()
        {
            var gameObject = new GameObject("Core Prefab Holder");
            UnityObject.DontDestroyOnLoad(gameObject);
            PrefabParent = gameObject.transform;

            NumberInputField = new GameObject("Input Field");
            NumberInputField.transform.SetParent(PrefabParent);
        }

        public static void Init() => Instance = new CorePrefabHolder();

        public Transform PrefabParent { get; set; }

        public GameObject NumberInputField { get; set; }
    }
}
