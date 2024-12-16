using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
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

        Material material;
        readonly float opacity;

        public string path;

        public ImageObject(GameObject gameObject, float opacity, string text, bool background, Sprite imageData)
        {
            GameObject = gameObject;
            this.opacity = opacity;

            if (GameObject.TryGetComponent(out Renderer renderer))
                Renderer = renderer;

            if (background)
                GameObject.layer = 9;

            if (Renderer)
                material = Renderer.material;

            if (CoreHelper.InEditor)
            {
                var collider = gameObject.AddComponent<BoxCollider2D>();
                gameObject.tag = "Helper";
                collider.isTrigger = true;
                collider.size = Vector2.one;
                Collider = collider;
            }

            if (imageData != null)
            {
                ((SpriteRenderer)Renderer).sprite = imageData;
                return;
            }

            var regex = new System.Text.RegularExpressions.Regex(@"img\((.*?)\)");
            var match = regex.Match(text);

            path = match.Success ? RTFile.CombinePaths(RTFile.BasePath, match.Groups[1].ToString()) : RTFile.CombinePaths(RTFile.BasePath, text);

            var position = gameObject.transform.localPosition;
            if (!RTFile.FileExists(path))
            {
                ((SpriteRenderer)Renderer).sprite = ArcadeManager.inst.defaultImage;
                gameObject.transform.localPosition = position;
                return;
            }

            CoreHelper.StartCoroutine(AlephNetworkManager.DownloadImageTexture("file://" + path, x =>
            {
                ((SpriteRenderer)Renderer).sprite = SpriteHelper.CreateSprite(x);
                gameObject.transform.localPosition = position;
            }, onError =>  { ((SpriteRenderer)Renderer).sprite = ArcadeManager.inst.defaultImage; }));
        }

        public override void SetColor(Color color) => material?.SetColor(new Color(color.r, color.g, color.b, color.a * opacity));

        public override Color GetPrimaryColor() => material.color;

        public override void Clear()
        {
            GameObject = null;
            Renderer = null;
            Collider = null;
            material = null;
        }
    }
}
