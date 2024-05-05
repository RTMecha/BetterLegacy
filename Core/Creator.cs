using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Core
{
    public static class Creator
    {
        public static GameObject NewGameObject(string name, Transform parent)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);
            gameObject.transform.localScale = Vector3.one;
            return gameObject;
        }

        public static GameObject NewUIObject(string name, Transform parent)
        {
            var gameObject = NewGameObject(name, parent);
            gameObject.AddComponent<RectTransform>();

            return gameObject;
        }
    }
}
