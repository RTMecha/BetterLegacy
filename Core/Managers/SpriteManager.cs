using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BetterLegacy.Core.Managers
{
    public class SpriteManager : MonoBehaviour
    {
        public static SpriteManager inst;

        public static List<List<Sprite>> RoundedSprites = new List<List<Sprite>>
        {
            new List<Sprite>(),
            new List<Sprite>(),
            new List<Sprite>(),
            new List<Sprite>(),
            new List<Sprite>(),
        };

        void Awake()
        {
            inst = this;

            try
            {
                var assetBundle = AssetBundle.LoadFromFile($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}sprites.asset");

                var allAssetNames = assetBundle.GetAllAssetNames();

                for (int i = 0; i < allAssetNames.Length; i++)
                {
                    var name = allAssetNames[i].Replace(Path.GetDirectoryName(allAssetNames[i]).Replace("\\", "/") + "/", "");

                    var sprite = assetBundle.LoadAsset<Sprite>(name);

                    var regex = new Regex("square_([1-5])_");

                    var match = regex.Match(name);

                    if (match.Success && int.TryParse(match.Groups[1].ToString(), out int num))
                    {
                        RoundedSprites[num - 1].Add(sprite);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        public static Sprite LoadSprite(string path, TextureFormat textureFormat = TextureFormat.ARGB32, bool mipChain = false, TextureWrapMode textureWrapMode = TextureWrapMode.Clamp, FilterMode filterMode = FilterMode.Point)
        {
            return LoadSprite(File.ReadAllBytes(path), textureFormat, mipChain, textureWrapMode, filterMode);
        }

        public static Sprite LoadSprite(byte[] bytes, TextureFormat textureFormat = TextureFormat.ARGB32, bool mipChain = false, TextureWrapMode textureWrapMode = TextureWrapMode.Clamp, FilterMode filterMode = FilterMode.Point)
        {
            var texture2d = new Texture2D(2, 2, textureFormat, mipChain);
            texture2d.LoadImage(bytes);

            texture2d.wrapMode = textureWrapMode;
            texture2d.filterMode = filterMode;
            texture2d.Apply();

            return CreateSprite(texture2d);
        }

        public static void SaveSprite(Sprite sprite, string path) => File.WriteAllBytes(path, sprite.texture.EncodeToPNG());

        public static Sprite CreateSprite(Texture2D texture2D) => Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);

        public static Sprite GetRoundedSprite(int roundness, RoundedSide side)
            => RoundedSprites[Mathf.Clamp(roundness, 1, 5) - 1][(int)side];

        public static void SetRoundedSprite(UnityEngine.UI.Image image, int roundness, RoundedSide side)
        {
            image.sprite = GetRoundedSprite(roundness, side);
            image.type = UnityEngine.UI.Image.Type.Sliced;
        }

        public enum RoundedSide
        {
            Bottom,
            Bottom_Left_E,
            Bottom_Left_I,
            Bottom_Right_E,
            Bottom_Right_I,
            Left,
            Right,
            Top,
            Top_Left_E,
            Top_Left_I,
            Top_Right_E,
            Top_Right_I,
            W,
        }
    }
}
