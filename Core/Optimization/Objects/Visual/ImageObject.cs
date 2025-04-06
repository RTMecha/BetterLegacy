using UnityEngine;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Optimization.Objects.Visual
{
    /// <summary>
    /// Class for special image objects.
    /// </summary>
    public class ImageObject : VisualObject
    {
        SpriteRenderer spriteRenderer;

        Material material;

        public string path;

        public ImageObject(GameObject gameObject, float opacity, string text, int renderType, Sprite imageData)
        {
            this.gameObject = gameObject;
            this.opacity = opacity;

            if (this.gameObject.TryGetComponent(out Renderer renderer))
                this.renderer = renderer;

            spriteRenderer = this.renderer as SpriteRenderer;

            SetRenderType(renderType);

            if (this.renderer)
                material = this.renderer.material;

            if (CoreHelper.InEditor)
            {
                var collider = gameObject.AddComponent<BoxCollider2D>();
                gameObject.tag = Tags.HELPER;
                collider.isTrigger = true;
                collider.size = Vector2.one;
                this.collider = collider;
            }

            UpdateImage(text, imageData);
        }

        /// <summary>
        /// Updates the image object.
        /// </summary>
        /// <param name="text">Path to the image file.</param>
        /// <param name="imageData">Pre-loaded image data.</param>
        public void UpdateImage(string text, Sprite imageData)
        {
            if (imageData)
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

            CoroutineHelper.StartCoroutine(AlephNetwork.DownloadImageTexture("file://" + path, SetTexture, SetDefaultSprite));
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
            base.Clear();
            material = null;
            spriteRenderer = null;
        }
    }
}
