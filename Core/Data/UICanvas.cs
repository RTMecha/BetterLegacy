using System.Collections;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core.Helpers;

using UnityObject = UnityEngine.Object;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Represents a canvas object.
    /// </summary>
    public class UICanvas : Exists
    {
        public UICanvas() { }

        public GameObject GameObject { get; set; }
        public Canvas Canvas { get; set; }
        public CanvasGroup CanvasGroup { get; set; }
        public CanvasScaler CanvasScaler { get; set; }

        /// <summary>
        /// Initializes the canvas object.
        /// </summary>
        /// <param name="name">Name of the canvas.</param>
        /// <param name="parent">Canvas parent.</param>
        /// <param name="dontDestroy">If the canvas shouldn't be destroyed on scene load.</param>
        /// <param name="sortingOrder">Order of the canvas.</param>
        public void Init(string name, Transform parent, bool dontDestroy = false, int sortingOrder = 10000)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);
            if (dontDestroy)
                UnityObject.DontDestroyOnLoad(gameObject);

            gameObject.transform.localScale = Vector3.one * CoreHelper.ScreenScale;
            var rectTransform = gameObject.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(960f, 540f);
            rectTransform.sizeDelta = new Vector2(1920f, 1080f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.zero;

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None | AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Tangent | AdditionalCanvasShaderChannels.Normal;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.scaleFactor = CoreHelper.ScreenScale;
            canvas.sortingOrder = sortingOrder;

            var canvasGroup = gameObject.AddComponent<CanvasGroup>();

            var canvasScaler = gameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);

            gameObject.AddComponent<GraphicRaycaster>();

            CoreHelper.Log($"Canvas Scale Factor: {canvas.scaleFactor}\nResoultion: {new Vector2(Screen.width, Screen.height)}");

            GameObject = gameObject;
            Canvas = canvas;
            CanvasScaler = canvasScaler;
            CanvasGroup = canvasGroup;
        }

        /// <summary>
        /// Sets the canvas to world space rendering.
        /// </summary>
        /// <param name="layer">Render Layer of the canvas to appear on.</param>
        /// <param name="worldCamera">Camera that renders the canvas.</param>
        public void SetWorldSpace(int layer, Camera worldCamera) => CoroutineHelper.StartCoroutine(ISetWorldSpace(layer, worldCamera));

        IEnumerator ISetWorldSpace(int layer, Camera worldCamera)
        {
            Canvas.scaleFactor = 1f;
            CanvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            GameObject.layer = layer;
            Canvas.worldCamera = worldCamera;
            Canvas.renderMode = RenderMode.ScreenSpaceCamera;
            yield return null;
            Canvas.renderMode = RenderMode.WorldSpace;
        }

        public static implicit operator Canvas(UICanvas canvas) => canvas.Canvas;
    }
}
