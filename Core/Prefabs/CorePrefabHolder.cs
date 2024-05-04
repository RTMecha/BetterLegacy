using UnityEngine;

namespace BetterLegacy.Core.Prefabs
{
    public class CorePrefabHolder
    {
        public static CorePrefabHolder Instance { get; set; }
        public CorePrefabHolder()
        {
            Instance = this;

            var gameObject = new GameObject("Core Prefab Holder");
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            PrefabParent = gameObject.transform;

            NumberInputField = new GameObject("Input Field");
            NumberInputField.transform.SetParent(PrefabParent);
        }

        public Transform PrefabParent { get; set; }

        public GameObject NumberInputField { get; set; }
    }
}
