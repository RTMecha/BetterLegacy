using UnityEngine;

using TMPro;

using BetterLegacy.Configs;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Runtime.Objects.Visual
{
    /// <summary>
    /// Class for special text objects.
    /// </summary>
    public class TextObject : VisualObject
    {
        public TextMeshPro textMeshPro;

        readonly bool autoTextAlign;
        public string text;

        Color color;

        public TextObject(GameObject gameObject, float opacity, string text, bool autoTextAlign, TextAlignmentOptions textAlignment, int renderType)
        {
            this.gameObject = gameObject;
            this.opacity = opacity;

            if (this.gameObject.TryGetComponent(out Renderer renderer))
                this.renderer = renderer;

            SetRenderType(renderType);

            textMeshPro = gameObject.GetComponent<TextMeshPro>();
            textMeshPro.enabled = true;
            this.text = text;
            SetText(this.text);
            this.autoTextAlign = autoTextAlign;

            if (autoTextAlign)
                textMeshPro.alignment = textAlignment;

            UpdateCollider();
        }

        /// <summary>
        /// Updates the text objects' collision.
        /// </summary>
        public void UpdateCollider()
        {
            if (CoreHelper.InEditor && EditorConfig.Instance.SelectTextObjectsInPreview.Value)
            {
                var collider = gameObject.AddComponent<BoxCollider2D>();
                gameObject.tag = Tags.HELPER;
                collider.isTrigger = true;
                this.collider = collider;
                CoroutineHelper.PerformAtNextFrame(() =>
                {
                    if (this.collider)
                        CoreHelper.GetColliderSize(collider, textMeshPro);
                });
            }
            else if (collider)
                CoreHelper.Destroy(collider);
        }

        /// <summary>
        /// Sets the text objects' text.
        /// </summary>
        /// <param name="text">Text to set.</param>
        public void SetText(string text)
        {
            if (textMeshPro)
                textMeshPro.text = text;
        }

        /// <summary>
        /// Gets the text objects' text.
        /// </summary>
        /// <returns>Returns the text of the text object.</returns>
        public string GetText() => textMeshPro ? textMeshPro.text : string.Empty;

        public override void SetColor(Color color)
        {
            this.color = color;
            textMeshPro.color = new Color(color.r, color.g, color.b, color.a * opacity);
        }

        public override void SetPrimaryColor(Color color) => textMeshPro.color = color;

        public override Color GetPrimaryColor() => color;

        public override void SetOrigin(Vector3 origin)
        {
            if (!gameObject)
                return;

            if (textMeshPro && autoTextAlign)
                textMeshPro.alignment = autoTextAlign ? GetAlignment(origin) : TextAlignmentOptions.Center;

            if (!autoTextAlign)
                gameObject.transform.localPosition = origin;
        }

        /// <summary>
        /// Gets the automatic alignment of the text object based on the origin.
        /// </summary>
        /// <param name="origin">Origin to align to.</param>
        /// <returns>Returns a <see cref="TextAlignmentOptions"/> from the origin.</returns>
        public static TextAlignmentOptions GetAlignment(Vector2 origin) => origin.x switch
        {
            -0.5f => origin.y == 0.5f ? TextAlignmentOptions.TopRight : origin.y == -0.5f ? TextAlignmentOptions.BottomRight : TextAlignmentOptions.Left,
            0f => origin.y == 0.5f ? TextAlignmentOptions.Top : origin.y == -0.5f ? TextAlignmentOptions.Bottom : TextAlignmentOptions.Center,
            0.5f => origin.y == 0.5f ? TextAlignmentOptions.TopRight : origin.y == -0.5f ? TextAlignmentOptions.BottomRight : TextAlignmentOptions.Right,
            _ => TextAlignmentOptions.Center,
        };

        public override void Clear()
        {
            base.Clear();
            textMeshPro = null;
        }
    }
}
