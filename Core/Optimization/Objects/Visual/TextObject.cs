using TMPro;
using UnityEngine;

namespace BetterLegacy.Core.Optimization.Objects.Visual
{
    /// <summary>
    /// Class for special text objects.
    /// </summary>
    public class TextObject : VisualObject
    {
        public override GameObject GameObject { get; set; }
        public override Renderer Renderer { get; set; }
        public override Collider2D Collider { get; set; }

        public TextMeshPro textMeshPro;
        readonly float opacity;

        public string text;

        public TextObject(GameObject gameObject, float opacity, string text, bool background)
        {
            GameObject = gameObject;
            this.opacity = opacity;

            if (GameObject.TryGetComponent(out Renderer renderer))
                Renderer = renderer;

            if (background)
                GameObject.layer = 9;

            textMeshPro = gameObject.GetComponent<TextMeshPro>();
            textMeshPro.enabled = true;
            this.text = text;
            SetText(this.text);
        }

        public void SetText(string text)
        {
            if (textMeshPro)
                textMeshPro.text = text;
        }

        public override void SetColor(Color color) => textMeshPro.color = new Color(color.r, color.g, color.b, color.a * opacity);

        public override Color GetPrimaryColor() => textMeshPro.color;

        public override void Clear()
        {
            GameObject = null;
            Renderer = null;
            Collider = null;
            textMeshPro = null;
        }
    }
}
