using UnityEngine;

using BetterLegacy.Core.Data;

namespace BetterLegacy.Core
{
    public static class Creator
    {
        public static GameObject NewGameObject(string name, Transform parent, int siblingIndex = -1)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);
            if (siblingIndex != -1)
                gameObject.transform.SetSiblingIndex(siblingIndex);
            gameObject.transform.localScale = Vector3.one;

            return gameObject;
        }

        public static GameObject NewPersistentGameObject(string name)
        {
            var gameObject = new GameObject(name);
            Object.DontDestroyOnLoad(gameObject);
            return gameObject;
        }

        public static GameObject NewUIObject(string name, Transform parent, int siblingIndex = -1)
        {
            var gameObject = NewGameObject(name, parent, siblingIndex);
            gameObject.AddComponent<RectTransform>();

            return gameObject;
        }

        public static GameObject NewUIObject(string name, Transform parent, RectValues rectValues, int siblingIndex = -1)
        {
            var gameObject = NewGameObject(name, parent, siblingIndex);
            var rectTransform = gameObject.AddComponent<RectTransform>();
            rectValues.AssignToRectTransform(rectTransform);

            return gameObject;
        }
    }
}
