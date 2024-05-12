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
        public override Transform Top { get; set; }
        public override Renderer Renderer { get; set; }
        public override Collider2D Collider { get; set; }

        readonly Material material;
        readonly float opacity;

        public string Path { get; set; }

        public ImageObject(GameObject gameObject, Transform top, float opacity, string text, bool background, Sprite imageData)
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

            if (Renderer)
                material = Renderer.material;

            if (EditorManager.inst)
            {
                var collider = gameObject.AddComponent<BoxCollider2D>();
                gameObject.tag = "Helper";
                collider.isTrigger = true;
                collider.size = Vector2.one;
                Collider = collider;
            }

            var local = gameObject.transform.localPosition;

            if (imageData != null)
            {
                ((SpriteRenderer)Renderer).sprite = imageData;
                return;
            }

            var regex = new System.Text.RegularExpressions.Regex(@"img\((.*?)\)");
            var match = regex.Match(text);

            Path = match.Success ? RTFile.BasePath + match.Groups[1].ToString() : RTFile.BasePath + text;

            if (!RTFile.FileExists(Path))
            {
                ((SpriteRenderer)Renderer).sprite = ArcadeManager.inst.defaultImage;
                return;
            }

            CoreHelper.StartCoroutine(AlephNetworkManager.DownloadImageTexture("file://" + Path, delegate (Texture2D x)
            {
                ((SpriteRenderer)Renderer).sprite = SpriteManager.CreateSprite(x);
                gameObject.transform.localPosition = local;
                gameObject.transform.localPosition = local;
                gameObject.transform.localPosition = local;
            }, delegate (string onError)
            {
                ((SpriteRenderer)Renderer).sprite = ArcadeManager.inst.defaultImage;
            }));
        }

        public override void SetColor(Color color) => material?.SetColor(new Color(color.r, color.g, color.b, color.a * opacity));
    }
}
