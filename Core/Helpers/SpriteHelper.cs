using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using UnityEngine;

namespace BetterLegacy.Core.Helpers
{
    public static class SpriteHelper
    {
        public static List<List<Sprite>> RoundedSprites = new List<List<Sprite>>
        {
            new List<Sprite>(),
            new List<Sprite>(),
            new List<Sprite>(),
            new List<Sprite>(),
            new List<Sprite>(),
        };

        /// <summary>
        /// Inits SpriteHelper data.
        /// </summary>
        public static void Init()
        {
            try
            {
                var assetBundle = AssetBundle.LoadFromFile(RTFile.GetAsset($"builtin/sprites.asset"));

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
            catch (Exception ex)
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

        public static void SaveSprite(Sprite sprite, string path) => File.WriteAllBytes(path, RTFile.FileIsFormat(path, FileFormat.JPG) ? sprite.texture.EncodeToJPG() : sprite.texture.EncodeToPNG());

        public static Sprite CreateSprite(Texture2D texture2D) => Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f);

        public static Sprite GetRoundedSprite(int roundness, RoundedSide side)
            => RoundedSprites[Mathf.Clamp(roundness, 1, 5) - 1][(int)side];

        public static void SetRoundedSprite(UnityEngine.UI.Image image, int roundness, RoundedSide side)
        {
            if (roundness < 1)
            {
                image.sprite = null;
                return;
            }

            image.sprite = GetRoundedSprite(roundness, side);
            image.type = UnityEngine.UI.Image.Type.Sliced;
        }

        public static string SpriteToString(Sprite sprite) => TextureToString(sprite.texture);
        public static string TextureToString(Texture2D texture2D) => Convert.ToBase64String(texture2D.EncodeToPNG());

        public static Sprite StringToSprite(string str, TextureFormat textureFormat = TextureFormat.ARGB32, bool mipChain = false, TextureWrapMode textureWrapMode = TextureWrapMode.Clamp, FilterMode filterMode = FilterMode.Point) => LoadSprite(Convert.FromBase64String(str), textureFormat, mipChain, textureWrapMode, filterMode);

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

        #region Capture Frame

        // RenderTexture to Texture2D code from https://discussions.unity.com/t/sprite-create-and-rendertexture/216782
        // Inspect(CoreHelper.AssignRenderTexture(EventManager.inst.cam, 512, 512, 10f))
        // Inspect(CoreHelper.AssignRenderTexture(512, 512, 10f))
        // Inspect(CoreHelper.AssignRenderTexture(512, 512, 10f, -10f))
        public static Sprite CaptureFrame(Camera camera, bool move = true, int width = 512, int height = 512, float offsetX = 0f, float offsetY = 0f, float rotationOffset = 0f)
        {
            //var alpha = camera.backgroundColor.a;
            //camera.backgroundColor = RTColors.FadeColor(camera.backgroundColor, 1f);
            //Arcade.Managers.RTEventManager.inst.glitchCam.enabled = false;
            //var isOrtho = camera.orthographic;
            //var zoom = camera.orthographicSize;
            //if (isOrtho)
            //    camera.orthographicSize = 1f;

            // Prep camera position so it renders the offset area
            var origPosition = camera.transform.localPosition;
            var origRotation = camera.transform.localEulerAngles;

            if (move)
            {
                camera.transform.localPosition = new Vector3(offsetX, offsetY);
                camera.transform.localEulerAngles = new Vector3(0f, 0f, rotationOffset);
            }

            // Get render texture
            var renderTexture = new RenderTexture(width, height, 0);
            renderTexture.name = DEFAULT_TEXTURE_NAME;

            var currentActiveRT = RenderTexture.active;
            RenderTexture.active = renderTexture;

            // Assign render texture to camera and render the camera
            camera.targetTexture = renderTexture;
            camera.Render();

            // Create a new Texture2D and read the RenderTexture image into it
            var texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);

            texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            texture.Apply();

            var sprite = CreateSprite(texture);
            sprite.name = texture.name;
            camera.targetTexture = null;

            // Reset to defaults
            RenderTexture.active = currentActiveRT;

            renderTexture.Release();
            CoreHelper.Destroy(renderTexture);
            renderTexture = null;

            // Reset camera position and rotation to normal
            if (move)
            {
                camera.transform.localPosition = origPosition;
                camera.transform.localEulerAngles = origRotation;
            }

            //Graphics.CopyTexture(renderTexture, texture);

            //if (isOrtho)
            //    camera.orthographicSize = zoom;
            //Arcade.Managers.RTEventManager.inst.glitchCam.enabled = true;
            //camera.backgroundColor = RTColors.FadeColor(camera.backgroundColor, alpha);

            //texture.SetPixels(new List<Color>().Fill(texture.width * texture.height, LSFunctions.LSColors.transparent).ToArray());

            return sprite;
        }

        public static Sprite RenderToSprite(this RenderTexture renderTexture)
        {
            // Create a new Texture2D and read the RenderTexture image into it
            var texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);

            texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            texture.Apply();

            var sprite = CreateSprite(texture);
            sprite.name = texture.name;
            return sprite;
        }

        public const string DEFAULT_TEXTURE_NAME = "Default_Texture";

        #endregion

        #region Combiner

        /*
var bg = SpriteHelper.CaptureFrame(RTLevel.Cameras.BG);
var fg = SpriteHelper.CaptureFrame(RTLevel.Cameras.FG);

Inspect(SpriteHelper.CombineTextures(512, 512, bg.texture, fg.texture));
         */

        public static Texture2D CombineTextures(int width, int height, params Texture2D[] textures)
        {
            var combinedTexture = new Texture2D(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    foreach (var texture in textures)
                    {
                        combinedTexture.SetPixel(x, y, texture.GetPixel(x, y));
                    }
                }
            }
            return combinedTexture;
        }

        // Code below from https://github.com/awteeter/Unity-Sprite-Combiner

        /// <summary>
        /// Data class representing various properties about a texture to be combined in code
        /// </summary>
        public sealed class TextureData
        {
            public Texture2D texture;
            public Vector2 texturePosition;
            public Vector2 textureScale = Vector2.one;
            public float textureRotation;

            public TextureData(Texture2D texture) => this.texture = texture;

            public TextureData(Texture2D texture, Vector2 texturePosition, Vector2 textureScale, float textureRotation)
            {
                this.texture = texture;
                this.texturePosition = texturePosition;
                this.textureScale = textureScale;
                this.textureRotation = textureRotation;
            }
        }

        /// <summary>
        /// Small class representing settings to influence final texture creation
        /// </summary>
        public class TextureCombinerSettings
        {
            public FilterMode filterMode = FilterMode.Point;
            public TextureFormat textureFormat = TextureFormat.RGBA32;
            public TextureWrapMode wrapMode = TextureWrapMode.Clamp;
            [Range(0, 1f)]
            public float alphaClipThreshold;
            public bool alphaBlend = true;
        }

        /// <summary>
        /// Main class for creating the combined texture that will be used by a combined sprite
        /// </summary>
        public static class TextureCombiner
        {
            /// <summary>
            /// Method to get a combined texture created from component textures
            /// </summary>
            /// <param name="textureData">The collection of texture data objects to use in texture creation</param>
            /// <param name="combinedWidth">The combined total width of the final combined texture</param>
            /// <param name="combinedHeight">The combined total height of the final combined texture</param>
            /// <param name="settings">The settings used to influence final texture creation</param>
            /// <returns>Texture2D The combined texture</returns>
            public static Texture2D CombineTextures(TextureData[] textureData, int combinedWidth, int combinedHeight, TextureCombinerSettings settings)
            {
                if (settings == null) { return null; }

                var combinedTexture = new Texture2D(combinedWidth, combinedHeight, settings.textureFormat, false)
                {
                    filterMode = settings.filterMode,
                    wrapMode = settings.wrapMode,
                };

                Color32 baseCol = Color.clear;

                Color32[] fillCols = new Color32[combinedTexture.width * combinedTexture.height];
                for (int i = 0; i < fillCols.Length; i++)
                    fillCols[i] = baseCol;

                combinedTexture.SetPixels32(fillCols);

                for (int i = 0; i < textureData.Length; i++)
                {
                    TextureData td = textureData[i];
                    if (td == null || td.texture == null) { continue; }

                    var tex = td.texture;

                    if (td.textureScale != Vector2.one) // Scale texture if applicable
                        tex = GetScaledTexture(tex, td.textureScale);

                    if (td.textureRotation != 0) // Rotate texture if applicable
                        tex = GetRotatedTexture(tex, td.textureRotation);

                    Vector2 texturePos = td.texturePosition;
                    Color[] pixels = tex.GetPixels();

                    for (int y = 0; y < tex.height; y++)
                    {
                        for (int x = 0; x < tex.width; x++)
                        {
                            Color texCol = pixels[(y * tex.width) + x];

                            int
                                xIndex = Mathf.RoundToInt(x + texturePos.x),
                                yIndex = Mathf.RoundToInt(y + texturePos.y);

                            Color col = combinedTexture.GetPixel(xIndex, yIndex);

                            if (settings.alphaBlend && texCol.a < col.a) // Alpha colour blending
                            {
                                texCol = new Color
                                (
                                    (col.r * (1 - texCol.a)) + (texCol.r * texCol.a),
                                    (col.g * (1 - texCol.a)) + (texCol.g * texCol.a),
                                    (col.b * (1 - texCol.a)) + (texCol.b * texCol.a),
                                    col.a
                                );
                            }

                            if (texCol.a > settings.alphaClipThreshold && texCol.a >= col.a)
                                combinedTexture.SetPixel(xIndex, yIndex, texCol);
                        }
                    }
                }
                combinedTexture.Apply();

                return combinedTexture;
            }

            /// <summary>
            /// Local function to rotate the given texture by the given angle
            /// </summary>
            /// <param name="tex">The texture to rotate</param>
            /// <param name="angle">The angle in degrees to rotate the texture</param>
            /// <returns>Texture2D The rotated texture</returns>
            static Texture2D GetRotatedTexture(Texture2D tex, float angle)
            {
                float diagonal = Mathf.Sqrt((tex.width * tex.width) + (tex.height * tex.height));
                int
                    rotatedWidth = Mathf.CeilToInt(diagonal),
                    rotatedHeight = Mathf.CeilToInt(diagonal);

                var rotatedTexture = new Texture2D(rotatedWidth, rotatedHeight, tex.format, false)
                {
                    filterMode = tex.filterMode,
                    wrapMode = tex.wrapMode,
                };

                Vector2 center = new Vector2(tex.width / 2f, tex.height / 2f);
                for (int x = 0; x < rotatedWidth; x++)
                {
                    for (int y = 0; y < rotatedHeight; y++)
                    {
                        Vector2 pos = new Vector2(x, y);
                        pos -= center;
                        pos = Quaternion.Euler(0, 0, angle) * pos;
                        pos += center;

                        int
                            oldX = Mathf.RoundToInt(pos.x),
                            oldY = Mathf.RoundToInt(pos.y);

                        Color col;
                        if (oldX < 0 || oldX >= tex.width || oldY < 0 || oldY >= tex.height)
                            col = Color.clear;
                        else
                            col = tex.GetPixel(oldX, oldY);

                        rotatedTexture.SetPixel(x, y, col);
                    }
                }
                rotatedTexture.Apply();

                return rotatedTexture;
            }

            /// <summary>
            /// Local function to scale the given texture by the given dimensions
            /// </summary>
            /// <param name="tex">The texture to scale</param>
            /// <param name="scale">The dimensions to scale the texture in</param>
            /// <returns>Texture2D The scaled texture</returns>
            static Texture2D GetScaledTexture(Texture2D tex, Vector2 scale)
            {
                int
                    scaledWidth = Mathf.RoundToInt(tex.width * scale.x),
                    scaledHeight = Mathf.RoundToInt(tex.height * scale.y);

                var scaledTex = new Texture2D(scaledWidth, scaledHeight, tex.format, false)
                {
                    filterMode = tex.filterMode,
                    wrapMode = tex.wrapMode,
                };

                for (int x = 0; x < scaledWidth; x++)
                {
                    for (int y = 0; y < scaledHeight; y++)
                    {
                        int
                            xIndex = Mathf.RoundToInt(x / scale.x),
                            yIndex = Mathf.RoundToInt(y / scale.y);

                        Color col = tex.GetPixel(xIndex, yIndex);
                        scaledTex.SetPixel(x, y, col);
                    }
                }
                scaledTex.Apply();

                return scaledTex;
            }
        }

        #endregion
    }
}
