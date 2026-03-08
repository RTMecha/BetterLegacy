using UnityEngine;

using BetterLegacy.Configs;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Runtime.Objects.Visual
{
    /// <summary>
    /// Class for special image objects.
    /// </summary>
    public class ImageObject : VisualObject
    {
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

            UpdateImage(text, imageData);
        }

        #region Values

        /// <summary>
        /// Sprite renderer of the image object.
        /// </summary>
        public SpriteRenderer spriteRenderer;

        /// <summary>
        /// Material of the image object.
        /// </summary>
        public Material material;

        /// <summary>
        /// Path to the image.
        /// </summary>
        public string path;

        Color color;

        #endregion

        #region Functions

        /// <summary>
        /// Updates the image objects' collision.
        /// </summary>
        public void UpdateCollider()
        {
            // check if image objects can be selectable
            if (ProjectArrhythmia.State.InEditor && EditorConfig.Instance.SelectImageObjectsInPreview.Value)
            {
                var collider = gameObject.AddComponent<BoxCollider2D>();
                gameObject.tag = Tags.HELPER;
                collider.isTrigger = true;
                CoreHelper.GetColliderSize(collider, spriteRenderer);
                this.collider = collider;
            }
            else if (collider)
                CoreHelper.Destroy(collider);
        }

        public override void Clear()
        {
            base.Clear();
            material = null;
            spriteRenderer = null;
        }

        #region Colors

        public override void SetColor(Color color)
        {
            this.color = color;
            material?.SetColor(new Color(color.r, color.g, color.b, color.a * opacity));
        }

        public override void SetPrimaryColor(Color color) => material.color = color;

        public override Color GetPrimaryColor() => color;

        #endregion

        #region Update Image

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

            // support really old image system
            var regex = new System.Text.RegularExpressions.Regex(@"img\((.*?)\)");
            var match = regex.Match(text);

            path =
                // allow asset pack images
                AssetPack.TryGetFile(text, out string assetPackFile) ? assetPackFile :
                // use old image system (idk why this was the way it was done but whatever)
                match.Success ? RTFile.CombinePaths(RTFile.BasePath, match.Groups[1].ToString()) : 
                // get the full path
                RTFile.CombinePaths(RTFile.BasePath, text);

            if (!RTFile.FileExists(path))
            {
                SetDefaultSprite();
                return;
            }

            CoroutineHelper.StartCoroutine(AlephNetwork.DownloadImageTexture("file://" + path, SetTexture, SetDefaultSprite));
        }

        /// <summary>
        /// Sets the image objects' image to the default image.
        /// </summary>
        public void SetDefaultSprite(string onError = null) => SetSprite(LegacyPlugin.PALogoSprite);

        /// <summary>
        /// Sets the image objects' image.
        /// </summary>
        /// <param name="texture2D">Creates a sprite from this texture and applies it to the sprite renderer.</param>
        public void SetTexture(Texture2D texture2D)
        {
            texture2D.filterMode = FilterMode.Point;
            SetSprite(SpriteHelper.CreateSprite(texture2D));
        }

        /// <summary>
        /// Sets the image objects' image.
        /// </summary>
        /// <param name="sprite">Applies this to the sprite renderer</param>
        public void SetSprite(Sprite sprite)
        {
            spriteRenderer.sprite = sprite;
            UpdateCollider();
        }

        #endregion

        #endregion
    }
}
