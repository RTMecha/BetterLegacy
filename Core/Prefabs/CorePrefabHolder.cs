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

            Particles = Creator.NewGameObject("Particles", PrefabParent);
            var visualObject = Creator.NewGameObject("visual", Particles.transform);
            var particleSystem = visualObject.AddComponent<ParticleSystem>();
            var particleSystemRenderer = visualObject.GetComponent<ParticleSystemRenderer>();

            gameObject.SetActive(false);
        }

        public static void Init() => Instance = new CorePrefabHolder();

        public Transform PrefabParent { get; set; }

        public GameObject NumberInputField { get; set; }

        public GameObject Particles { get; set; }
    }
}
