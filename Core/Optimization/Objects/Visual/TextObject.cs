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
        public override Transform Top { get; set; }
        public override Renderer Renderer { get; set; }
        public override Collider2D Collider { get; set; }

        public readonly TextMeshPro TextMeshPro;
        readonly float opacity;

        public string Text { get; set; }

        public TextObject(GameObject gameObject, Transform top, float opacity, string text, bool background)
        {
            GameObject = gameObject;
            Top = top;
            this.opacity = opacity;

            if (GameObject.TryGetComponent(out Renderer renderer))
                Renderer = renderer;

            if (background)
            {
                GameObject.layer = 9;
                //Renderer.material = GameStorageManager.inst.bgMaterial;
            }

            TextMeshPro = gameObject.GetComponent<TextMeshPro>();
            TextMeshPro.enabled = true;
            Text = text;
            SetText(Text);
        }

        public void SetText(string text)
        {
            if (TextMeshPro)
                TextMeshPro.text = text;
        }

        public override void SetColor(Color color) => TextMeshPro.color = new Color(color.r, color.g, color.b, color.a * opacity);
    }
}
