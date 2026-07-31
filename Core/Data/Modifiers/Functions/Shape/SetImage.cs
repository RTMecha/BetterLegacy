using System.Collections.Generic;

using UnityEngine;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetImage : ModifierActionBase
    {
        #region Constructors

        public SetImage(bool isGroup)
        {
            this.isGroup = isGroup;
            Name = "setImage";
            if (isGroup)
                Name += "Other";
            SetupModifier("Path", "0", "0", "1", "1", "0", "0");
            if (isGroup)
                Modifier.values.Insert(1, "Object Group");
            IsGroup = isGroup;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Shape;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        readonly bool isGroup;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var value = modifier.GetValue(0, modifierLoop.variables);
            value = FormatStringVariables(value, modifierLoop.variables);
            var textureOffset = new Vector2(modifier.GetFloat(isGroup ? 2 : 1, 0f, modifierLoop.variables), modifier.GetFloat(isGroup ? 3 : 2, 0f, modifierLoop.variables));
            var textureScale = new Vector2(modifier.GetFloat(isGroup ? 4 : 3, 1f, modifierLoop.variables), modifier.GetFloat(isGroup ? 5 : 4, 0f, modifierLoop.variables));
            var filterMode = Parser.TryParse(modifier.GetValue(isGroup ? 6 : 5, modifierLoop.variables), FilterMode.Point);
            var wrapMode = Parser.TryParse(modifier.GetValue(isGroup ? 7 : 6, modifierLoop.variables), TextureWrapMode.Repeat);

            if (isGroup)
            {
                if (modifierLoop.reference is not IPrefabable prefabable)
                    return;

                var tag = modifier.GetValue(1, modifierLoop.variables);

                var cache = modifier.GetResultOrDefault(() =>
                {
                    var cache = new ImageGroupCache
                    {
                        tag = tag,
                        beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, prefabable, tag),
                    };
                    return cache;
                });
                if (cache.tag != tag)
                {
                    cache.tag = tag;
                    cache.beatmapObjects = GameData.Current.FindObjectsWithTag(modifier, prefabable, tag);
                }
                var list = cache.beatmapObjects;
                if (list.IsEmpty())
                    return;

                if (cache.value == value)
                {
                    cache.SetImageOffsets(textureOffset, textureScale);
                    return;
                }
                cache.value = value;

                Sprite sprite = null;
                if (prefabable.FromPrefab && prefabable.GetPrefab() is Prefab prefab && prefab.assets.sprites.TryFind(x => x.name == value, out SpriteAsset spriteAsset))
                    sprite = spriteAsset.sprite;
                else
                    sprite = GameData.Current.assets.GetSprite(value);

                if (sprite)
                {
                    cache.SetImage(sprite.texture, textureOffset, textureScale, filterMode, wrapMode);
                    return;
                }

                var assetPath = AssetPack.GetFile(value);
                var path = RTFile.FileExists(assetPath) ? assetPath : RTFile.CombinePaths(RTFile.BasePath, value);
                if (!RTFile.FileExists(path))
                {
                    cache.SetImage(LegacyPlugin.PALogoSprite.texture, textureOffset, textureScale, filterMode, wrapMode);
                    return;
                }

                CoroutineHelper.StartCoroutine(AlephNetwork.DownloadImageTexture("file://" + path,
                    callback: texture2D => cache.SetImage(texture2D, textureOffset, textureScale, filterMode, wrapMode),
                    onError: (string onError, long responseCode, string errorMsg) => cache.SetImage(LegacyPlugin.PALogoSprite.texture, textureOffset, textureScale, filterMode, wrapMode)));
                return;
            }

            if (modifierLoop.reference is not BeatmapObject beatmapObject || !beatmapObject.runtimeObject)
                return;

            if (modifier.constant)
            {
                if (beatmapObject.runtimeObject.visualObject is ImageObject imageObject)
                {
                    imageObject.material.mainTextureOffset = textureOffset;
                    imageObject.material.mainTextureScale = textureScale;
                }
                else if (beatmapObject.runtimeObject.visualObject is SolidObject solidObject)
                {
                    solidObject.material.mainTextureOffset = textureOffset;
                    solidObject.material.mainTextureScale = textureScale;
                }

                if (!modifier.TryGetResult(out string oldPath) || oldPath != value)
                {
                    modifier.Result = value;
                    SetImageFunction(value, beatmapObject, textureOffset, textureScale, filterMode, wrapMode);
                }
            }
            else
                SetImageFunction(value, beatmapObject, textureOffset, textureScale, filterMode, wrapMode);
        }

        static void SetImageFunction(string value, BeatmapObject beatmapObject, Vector2 textureOffset, Vector2 textureScale, FilterMode filterMode, TextureWrapMode wrapMode)
        {
            var sprite = beatmapObject.GetSprite(value);

            if (beatmapObject.runtimeObject.visualObject is ImageObject imageObject)
            {
                imageObject.material.mainTextureOffset = textureOffset;
                imageObject.material.mainTextureScale = textureScale;

                if (sprite)
                {
                    imageObject.SetSprite(sprite);
                    return;
                }

                var path = RTFile.CombinePaths(RTFile.BasePath, value);

                if (!RTFile.FileExists(path))
                {
                    imageObject.SetDefaultSprite();
                    return;
                }

                CoroutineHelper.StartCoroutine(AlephNetwork.DownloadImageTexture("file://" + path, callback: texture2D => imageObject.SetTexture(texture2D, filterMode, wrapMode), onError: imageObject.SetDefaultSprite));
            }
            else if (beatmapObject.runtimeObject.visualObject is SolidObject solidObject && solidObject.renderer)
            {
                var renderer = solidObject.renderer;
                if (!renderer)
                    return;

                renderer.material.mainTextureOffset = textureOffset;
                renderer.material.mainTextureScale = textureScale;

                if (sprite)
                {
                    renderer.material.SetTexture("_MainTex", sprite.texture);
                    return;
                }

                var assetPath = AssetPack.GetFile(value);
                var path = RTFile.FileExists(assetPath) ? assetPath : RTFile.CombinePaths(RTFile.BasePath, value);
                if (!RTFile.FileExists(path))
                    return;

                CoroutineHelper.StartCoroutine(AlephNetwork.DownloadImageTexture("file://" + path,
                    texture2D =>
                    {
                        if (!beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not SolidObject solidObject)
                            return;

                        texture2D.filterMode = filterMode;
                        texture2D.wrapMode = wrapMode;
                        var renderer = solidObject.renderer;
                        if (renderer)
                            renderer.material.SetTexture("_MainTex", texture2D);
                    }));
            }
        }

        public override bool IsCompatible(IModifyable modifyable) => isGroup || modifyable is IShapeable shapeable && shapeable.ShapeType == ShapeType.Image;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            var index = 0;
            if (isGroup)
            {
                modifierCard.PrefabGroupOnly(modifier, reference);
                modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 1);
                index++;
            }

            modifierCard.StringGenerator(modifier, reference, "Path", 0 + index);
            modifierCard.SingleGenerator(modifier, reference, "Texture Offset X", 1 + index);
            modifierCard.SingleGenerator(modifier, reference, "Texture Offset Y", 2 + index);
            modifierCard.SingleGenerator(modifier, reference, "Texture Scale X", 3 + index, 1f);
            modifierCard.SingleGenerator(modifier, reference, "Texture Scale Y", 4 + index, 1f);
            modifierCard.DropdownGenerator(modifier, reference, "Filter Mode", 5 + index, CoreHelper.ToOptionData<FilterMode>());
            modifierCard.DropdownGenerator(modifier, reference, "Wrap Mode", 6 + index, CoreHelper.ToOptionData<TextureWrapMode>());
        }

        #endregion

        #region Sub Classes

        public class ImageGroupCache
        {
            public string tag;
            public List<BeatmapObject> beatmapObjects;
            public string value;

            public void SetImageOffsets(Vector2 textureOffset, Vector2 textureScale)
            {
                foreach (var bm in beatmapObjects)
                {
                    if (bm.ShapeType == ShapeType.Image && bm.runtimeObject && bm.runtimeObject.visualObject is ImageObject imageObject)
                        if (imageObject.material)
                        {
                            imageObject.material.mainTextureOffset = textureOffset;
                            imageObject.material.mainTextureScale = textureScale;
                        }
                        else if (bm.runtimeObject && bm.runtimeObject.visualObject is SolidObject solidObject)
                            if (solidObject.material)
                            {
                                solidObject.material.mainTextureOffset = textureOffset;
                                solidObject.material.mainTextureScale = textureScale;
                            }
                }
            }

            public void SetImage(Texture2D texture2D, Vector2 textureOffset, Vector2 textureScale, FilterMode filterMode, TextureWrapMode wrapMode)
            {
                foreach (var bm in beatmapObjects)
                {
                    if (bm.ShapeType == ShapeType.Image && bm.runtimeObject && bm.runtimeObject.visualObject is ImageObject imageObject)
                    {
                        if (imageObject.material)
                        {
                            imageObject.material.mainTextureOffset = textureOffset;
                            imageObject.material.mainTextureScale = textureScale;
                        }
                        imageObject.SetTexture(texture2D, filterMode, wrapMode);
                    }
                    else if (bm.runtimeObject && bm.runtimeObject.visualObject is SolidObject solidObject)
                    {
                        texture2D.filterMode = filterMode;
                        texture2D.wrapMode = wrapMode;
                        if (solidObject.material)
                        {
                            solidObject.material.mainTextureOffset = textureOffset;
                            solidObject.material.mainTextureScale = textureScale;
                        }
                        solidObject.material.SetTexture("_MainTex", texture2D);
                    }
                }
            }
        }

        #endregion
    }
}
