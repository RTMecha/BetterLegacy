using BetterLegacy.Core.Helpers;
using UnityEngine;

namespace BetterLegacy.Core.Optimization.Objects.Visual
{
    /// <summary>
    /// Class for special image objects.
    /// </summary>
    public class ImageObject : VisualObject
    {
        public override GameObject GameObject { get; set; }
        public override Renderer Renderer { get; set; }
        public override Collider2D Collider { get; set; }

        SpriteRenderer spriteRenderer;

        Material material;
        readonly float opacity;

        public string path;

        public ImageObject(GameObject gameObject, float opacity, string text, bool background, Sprite imageData)
        {
            GameObject = gameObject;
            this.opacity = opacity;

            if (GameObject.TryGetComponent(out Renderer renderer))
                Renderer = renderer;

            spriteRenderer = Renderer as SpriteRenderer;

            if (background)
                GameObject.layer = 9;

            if (Renderer)
                material = Renderer.material;

            if (CoreHelper.InEditor)
            {
                var collider = gameObject.AddComponent<BoxCollider2D>();
                gameObject.tag = Tags.HELPER;
                collider.isTrigger = true;
                collider.size = Vector2.one;
                Collider = collider;
            }

            if (imageData != null)
            {
                SetSprite(imageData);
                return;
            }

            var regex = new System.Text.RegularExpressions.Regex(@"img\((.*?)\)");
            var match = regex.Match(text);

            path = match.Success ? RTFile.CombinePaths(RTFile.BasePath, match.Groups[1].ToString()) : RTFile.CombinePaths(RTFile.BasePath, text);

            var position = gameObject.transform.localPosition;
            if (!RTFile.FileExists(path))
            {
                SetDefaultSprite();
                return;
            }

            CoreHelper.StartCoroutine(AlephNetwork.DownloadImageTexture("file://" + path, SetTexture, SetDefaultSprite));
        }

        public override void SetColor(Color color) => material?.SetColor(new Color(color.r, color.g, color.b, color.a * opacity));

        public override Color GetPrimaryColor() => material.color;

        /// <summary>
        /// Sets the image objects' image to the default image.
        /// </summary>
        public void SetDefaultSprite(string onError = null) => SetSprite(LegacyPlugin.PALogoSprite);

        /// <summary>
        /// Sets the image objects' image.
        /// </summary>
        /// <param name="texture2D">Creates a sprite from this texture and applies it to the sprite renderer.</param>
        public void SetTexture(Texture2D texture2D) => SetSprite(SpriteHelper.CreateSprite(texture2D));

        /// <summary>
        /// Sets the image objects' image.
        /// </summary>
        /// <param name="sprite">Applies this to the sprite renderer</param>
        public void SetSprite(Sprite sprite) => spriteRenderer.sprite = sprite;

        public override void Clear()
        {
            GameObject = null;
            Renderer = null;
            Collider = null;
            material = null;
            spriteRenderer = null;
        }
    }
}
