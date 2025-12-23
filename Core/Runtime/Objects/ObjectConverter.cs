using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Components;

using ObjectType = BetterLegacy.Core.Data.Beatmap.BeatmapObject.ObjectType;

namespace BetterLegacy.Core.Runtime.Objects
{
    // WARNING: This class has side effects and will instantiate GameObjects
    /// <summary>
    /// Converts Beatmap Objects to Runtime Objects to be used by the mod
    /// </summary>
    public class ObjectConverter : Exists
    {
        readonly RTLevelBase runtimeLevel;

        public ObjectConverter(RTLevelBase runtimeLevel) => this.runtimeLevel = runtimeLevel;

        #region Runtime Objects

        public bool SkipRuntimeObject(BeatmapObject beatmapObject) => EditorConfig.Instance.OnlyShowDamagable.Value && beatmapObject.objectType != ObjectType.Normal || !EditorConfig.Instance.ShowEmpties.Value && beatmapObject.objectType == ObjectType.Empty || !beatmapObject.detailMode.CanSpawn();

        public IEnumerable<IRTObject> ToRuntimeObjects() => ToRuntimeObjects(GameData.Current.beatmapObjects);

        public IEnumerable<IRTObject> ToRuntimeObjects(IEnumerable<BeatmapObject> beatmapObjects)
        {
            foreach (var beatmapObject in beatmapObjects)
            {
                if (SkipRuntimeObject(beatmapObject))
                {
                    if (beatmapObject.runtimeObject && beatmapObject.runtimeObject.parentObjects != null)
                        beatmapObject.runtimeObject.parentObjects.Clear();
                    beatmapObject.runtimeObject = null;
                    continue;
                }

                RTBeatmapObject runtimeObject = null;

                try
                {
                    runtimeObject = ToRuntimeObject(beatmapObject);
                }
                catch (Exception e)
                {
                    var stringBuilder = new StringBuilder();
                    stringBuilder.AppendLine($"{RTLevel.className}Failed to convert object '{beatmapObject.id}' to {nameof(RTBeatmapObject)}.");
                    stringBuilder.AppendLine($"Exception: {e.Message}");
                    stringBuilder.AppendLine(e.StackTrace);

                    Debug.LogError(stringBuilder.ToString());
                }

                if (runtimeObject)
                    yield return runtimeObject;
            }
        }

        public IRTObject ToIRuntimeObject(BeatmapObject beatmapObject)
        {
            if (SkipRuntimeObject(beatmapObject))
            {
                if (beatmapObject.runtimeObject && beatmapObject.runtimeObject.parentObjects != null)
                    beatmapObject.runtimeObject.parentObjects.Clear();
                beatmapObject.runtimeObject = null;
                return null;
            }

            RTBeatmapObject runtimeObject = null;

            try
            {
                runtimeObject = ToRuntimeObject(beatmapObject);
            }
            catch (Exception e)
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"{RTLevel.className}Failed to convert object '{beatmapObject.id}' to {nameof(RTBeatmapObject)}.");
                stringBuilder.AppendLine($"Exception: {e.Message}");
                stringBuilder.AppendLine(e.StackTrace);

                Debug.LogError(stringBuilder.ToString());
            }

            return runtimeObject;
        }

        RTBeatmapObject ToRuntimeObject(BeatmapObject beatmapObject)
        {
            var parentObjects = new List<ParentObject>(ParentObject.DEFAULT_PARENT_CHAIN_CAPACITY);

            GameObject parent = null;

            beatmapObject.CachedParent = null;
            if (!string.IsNullOrEmpty(beatmapObject.Parent) && GameData.Current.beatmapObjects.TryFind(x => x.id == beatmapObject.Parent, out BeatmapObject beatmapObjectParent))
            {
                beatmapObject.CachedParent = beatmapObjectParent;
                parent = InitParentChain(beatmapObjectParent, parentObjects);
            }

            var shape = Mathf.Clamp(beatmapObject.Shape, 0, ObjectManager.inst.objectPrefabs.Count - 1);
            var shapeOption = Mathf.Clamp(beatmapObject.ShapeOption, 0, ObjectManager.inst.objectPrefabs[shape].options.Count - 1);
            var shapeType = (ShapeType)shape;

            GameObject baseObject = UnityObject.Instantiate(ObjectManager.inst.objectPrefabs[shape].options[shapeOption], parent ? parent.transform : runtimeLevel.SpawnParent);
            
            baseObject.transform.localScale = Vector3.one;

            var visualObject = baseObject.transform.GetChild(0).gameObject;
            if (beatmapObject.ShapeType != ShapeType.Text || !beatmapObject.autoTextAlign)
                visualObject.transform.localPosition = new Vector3(beatmapObject.origin.x, beatmapObject.origin.y, beatmapObject.Depth * BeatmapObject.DEPTH_VISUAL_MULTIPLY);
            visualObject.name = "Visual [ " + beatmapObject.name + " ]";

            parentObjects.Insert(0, ToParentObject(beatmapObject, baseObject));

            baseObject.name = beatmapObject.name;

            var top = Creator.NewGameObject($"top - [{beatmapObject.name}]", runtimeLevel.SpawnParent);

            var firstParent = parentObjects[parentObjects.Count - 1];
            var tf = firstParent?.transform ?? baseObject.transform;

            tf.SetParent(top.transform);
            tf.localScale = Vector3.one;

            baseObject.SetActive(true);
            visualObject.SetActive(true);

            // Init visual object wrapper
            float opacity = beatmapObject.objectType == ObjectType.Helper ? BeatmapObject.HELPER_OPACITY : 1.0f;
            bool deco = beatmapObject.objectType == ObjectType.Helper ||
                               beatmapObject.objectType == ObjectType.Decoration;

            bool isSolid = beatmapObject.objectType == ObjectType.Solid;

            VisualObject visual = shapeType switch
            {
                ShapeType.Text => new TextObject(visualObject, opacity, beatmapObject.text, beatmapObject.autoTextAlign, TextObject.GetAlignment(beatmapObject.origin), (int)beatmapObject.renderLayerType),
                ShapeType.Image => new ImageObject(visualObject, opacity, beatmapObject.text, (int)beatmapObject.renderLayerType, GameData.Current.GetAssets().GetSprite(beatmapObject.text)),
                ShapeType.Polygon => new PolygonObject(visualObject, opacity, deco, isSolid, (int)beatmapObject.renderLayerType, beatmapObject.opacityCollision, (int)beatmapObject.gradientType, beatmapObject.gradientScale, beatmapObject.gradientRotation, (int)beatmapObject.colorBlendMode, beatmapObject.polygonShape),
                _ => new SolidObject(visualObject, opacity, deco, isSolid, (int)beatmapObject.renderLayerType, beatmapObject.opacityCollision, (int)beatmapObject.gradientType, beatmapObject.gradientScale, beatmapObject.gradientRotation, (int)beatmapObject.colorBlendMode),
            };

            if (CoreHelper.InEditor)
            {
                visualObject.SetActive(!beatmapObject.editorData.hidden);

                if (beatmapObject.editorData.selectable)
                {
                    var obj = visualObject.AddComponent<SelectObject>();
                    obj.SetObject(beatmapObject);
                    beatmapObject.selector = obj;
                }
            }

            UnityObject.Destroy(visualObject.GetComponent<SelectObjectInEditor>());

            visual.colorSequence = beatmapObject.cachedSequences.ColorSequence;
            visual.secondaryColorSequence = beatmapObject.cachedSequences.SecondaryColorSequence;

            var runtimeObject = new RTBeatmapObject(beatmapObject, parentObjects, visual, runtimeLevel);

            runtimeObject.SetActive(false);

            beatmapObject.ParentRuntime = runtimeLevel;
            beatmapObject.runtimeObject = runtimeObject;

            return runtimeObject;
        }

        public GameObject InitParentChain(BeatmapObject beatmapObject, List<ParentObject> parentObjects)
        {
            var gameObject = new GameObject(beatmapObject.name);

            parentObjects.Add(ToParentObject(beatmapObject, gameObject));

            // Has parent - init parent (recursive)
            BeatmapObject beatmapObjectParent = beatmapObject.CachedParent;
            var parent = beatmapObject.Parent;
            if (beatmapObjectParent || !string.IsNullOrEmpty(parent) && GameData.Current.beatmapObjects.TryFind(x => x.id == parent, out beatmapObjectParent))
            {
                beatmapObject.CachedParent = beatmapObjectParent;
                var parentObject = InitParentChain(beatmapObjectParent, parentObjects);

                gameObject.transform.SetParent(parentObject.transform);
            }
            if (string.IsNullOrEmpty(parent))
                beatmapObject.CachedParent = null;

            return gameObject;
        }

        public ParentObject ToParentObject(BeatmapObject beatmapObject, GameObject gameObject)
        {
            CachedSequences cachedSequences = beatmapObject.cachedSequences;

            ParentObject parentObject = null;

            try
            {
                if (cachedSequences)
                    parentObject = new ParentObject
                    {
                        positionSequence = cachedSequences.PositionSequence,
                        scaleSequence = cachedSequences.ScaleSequence,
                        rotationSequence = cachedSequences.RotationSequence,

                        parentAnimatePosition = beatmapObject.GetParentType(0),
                        parentAnimateScale = beatmapObject.GetParentType(1),
                        parentAnimateRotation = beatmapObject.GetParentType(2),

                        parentOffsetPosition = beatmapObject.parentOffsets[0],
                        parentOffsetScale = beatmapObject.parentOffsets[1],
                        parentOffsetRotation = beatmapObject.parentOffsets[2],

                        parentAdditivePosition = beatmapObject.GetParentAdditive(0),
                        parentAdditiveScale = beatmapObject.GetParentAdditive(1),
                        parentAdditiveRotation = beatmapObject.GetParentAdditive(2),

                        parentParallaxPosition = beatmapObject.parallaxSettings[0],
                        parentParallaxScale = beatmapObject.parallaxSettings[1],
                        parentParallaxRotation = beatmapObject.parallaxSettings[2],

                        gameObject = gameObject,
                        transform = gameObject.transform,
                        id = beatmapObject.id,
                        desync = !string.IsNullOrEmpty(beatmapObject.Parent) && beatmapObject.desync,
                        beatmapObject = beatmapObject
                    };
                else
                {
                    var pos = new List<IKeyframe<Vector3>>();
                    pos.Add(new Vector3Keyframe(0f, Vector3.zero, Ease.Linear));

                    var sca = new List<IKeyframe<Vector2>>();
                    sca.Add(new Vector2Keyframe(0f, Vector2.one, Ease.Linear));

                    var rot = new List<IKeyframe<float>>();
                    rot.Add(new FloatKeyframe(0f, 0f, Ease.Linear));

                    parentObject = new ParentObject
                    {
                        positionSequence = new Sequence<Vector3>(pos),
                        scaleSequence = new Sequence<Vector2>(sca),
                        rotationSequence = new Sequence<float>(rot),

                        parentAnimatePosition = beatmapObject.GetParentType(0),
                        parentAnimateScale = beatmapObject.GetParentType(1),
                        parentAnimateRotation = beatmapObject.GetParentType(2),

                        parentOffsetPosition = beatmapObject.GetParentOffset(0),
                        parentOffsetScale = beatmapObject.GetParentOffset(1),
                        parentOffsetRotation = beatmapObject.GetParentOffset(2),

                        parentAdditivePosition = beatmapObject.parentAdditive[0] == '1',
                        parentAdditiveScale = beatmapObject.parentAdditive[1] == '1',
                        parentAdditiveRotation = beatmapObject.parentAdditive[2] == '1',

                        parentParallaxPosition = beatmapObject.parallaxSettings[0],
                        parentParallaxScale = beatmapObject.parallaxSettings[1],
                        parentParallaxRotation = beatmapObject.parallaxSettings[2],

                        gameObject = gameObject,
                        transform = gameObject.transform,
                        id = beatmapObject.id,
                        desync = !string.IsNullOrEmpty(beatmapObject.Parent) && beatmapObject.desync,
                        beatmapObject = beatmapObject
                    };
                } // In case the CashedSequence is null, set defaults.

                beatmapObject.detatched = false;
            }
            catch (Exception e)
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"Failed to init level parent object for '{beatmapObject.id}'.");
                stringBuilder.AppendLine($"Exception: {e.Message}");
                stringBuilder.AppendLine(e.StackTrace);

                Debug.LogError(stringBuilder.ToString());
            }

            return parentObject;
        }

        #endregion

        #region Runtime Modifiers

        public bool SkipRuntimeModifiers(BeatmapObject beatmapObject) => beatmapObject.modifiers.IsEmpty() || !beatmapObject.detailMode.CanSpawn();

        public IEnumerable<IRTObject> ToRuntimeModifiers() => ToRuntimeModifiers(GameData.Current.beatmapObjects);

        public IEnumerable<IRTObject> ToRuntimeModifiers(IEnumerable<BeatmapObject> beatmapObjects)
        {
            foreach (var beatmapObject in beatmapObjects)
            {
                if (SkipRuntimeModifiers(beatmapObject))
                {
                    beatmapObject.runtimeModifiers = null;
                    continue;
                }

                var runtimeModifiers = ToRuntimeModifiers(beatmapObject);

                if (runtimeModifiers)
                    yield return runtimeModifiers;
            }
        }

        public IRTObject ToIRuntimeModifiers(BeatmapObject beatmapObject) => SkipRuntimeModifiers(beatmapObject) ? null : ToRuntimeModifiers(beatmapObject);

        RTModifiers ToRuntimeModifiers(BeatmapObject beatmapObject)
        {
            var runtimeModifiers = new RTModifiers(
                    beatmapObject.modifiers, beatmapObject, beatmapObject.orderModifiers,
                    beatmapObject.ignoreLifespan ? -SoundManager.inst.MusicLength : beatmapObject.StartTime,
                    beatmapObject.ignoreLifespan ? SoundManager.inst.MusicLength : beatmapObject.StartTime + beatmapObject.SpawnDuration,
                    runtimeLevel
                );
            beatmapObject.ParentRuntime = runtimeLevel;
            beatmapObject.runtimeModifiers = runtimeModifiers;
            return runtimeModifiers;
        }

        #endregion

        #region Runtime BG Objects

        public bool SkipRuntimeBGObject(BackgroundObject backgroundObject) => !CoreConfig.Instance.ShowBackgroundObjects.Value || !backgroundObject.active;

        public IEnumerable<IRTObject> ToRuntimeBGObjects() => ToRuntimeBGObjects(GameData.Current.BackgroundObjects);

        public IEnumerable<IRTObject> ToRuntimeBGObjects(IEnumerable<BackgroundObject> backgroundObjects)
        {
            foreach (var backgroundObject in backgroundObjects)
            {
                if (SkipRuntimeBGObject(backgroundObject))
                {
                    backgroundObject.runtimeObject = null;
                    continue;
                }

                RTBackgroundObject runtimeObject = null;

                try
                {
                    runtimeObject = ToRuntimeBGObject(backgroundObject);
                }
                catch (Exception e)
                {
                    var stringBuilder = new StringBuilder();
                    stringBuilder.AppendLine($"{RTLevel.className}Failed to convert object '{backgroundObject.id}' to {nameof(RTBackgroundObject)}.");
                    stringBuilder.AppendLine($"Exception: {e.Message}");
                    stringBuilder.AppendLine(e.StackTrace);

                    Debug.LogError(stringBuilder.ToString());
                }

                if (runtimeObject)
                    yield return runtimeObject;
            }
        }

        public IRTObject ToIRuntimeBGObject(BackgroundObject backgroundObject)
        {
            if (SkipRuntimeBGObject(backgroundObject))
            {
                backgroundObject.runtimeObject = null;
                return null;
            }

            RTBackgroundObject runtimeObject = null;

            try
            {
                runtimeObject = ToRuntimeBGObject(backgroundObject);
            }
            catch (Exception e)
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"{RTLevel.className}Failed to convert object '{backgroundObject.id}' to {nameof(RTBackgroundObject)}.");
                stringBuilder.AppendLine($"Exception: {e.Message}");
                stringBuilder.AppendLine(e.StackTrace);

                Debug.LogError(stringBuilder.ToString());
            }

            return runtimeObject;
        }

        RTBackgroundObject ToRuntimeBGObject(BackgroundObject backgroundObject)
        {
            var renderers = new List<Renderer>();

            var parent = runtimeLevel.SpawnParent;

            if (!string.IsNullOrEmpty(backgroundObject.layer) &&
                runtimeLevel.backgroundLayers.TryFind(x => x.backgroundLayer && x.backgroundLayer.id == backgroundObject.layer, out BackgroundLayerObject backgroundLayerObject) &&
                backgroundLayerObject.gameObject)
            {
                parent = backgroundLayerObject.gameObject.transform;
            }

            var baseObject = Creator.NewGameObject(backgroundObject.name, parent);

            var gameObject = BackgroundManager.inst.backgroundPrefab.Duplicate(baseObject.transform, backgroundObject.name);
            gameObject.layer = 9;
            gameObject.transform.localPosition = new Vector3(backgroundObject.pos.x, backgroundObject.pos.y, 32f + backgroundObject.depth * 10f);
            gameObject.transform.localScale = new Vector3(backgroundObject.scale.x, backgroundObject.scale.y, backgroundObject.zscale);
            gameObject.transform.localRotation = Quaternion.Euler(new Vector3(backgroundObject.rotation.x, backgroundObject.rotation.y, backgroundObject.rot));

            var renderer = gameObject.GetComponent<Renderer>();
            renderer.material = LegacyResources.objectMaterial;

            CoreHelper.Destroy(gameObject.GetComponent<SelectBackgroundInEditor>());
            CoreHelper.Destroy(gameObject.GetComponent<BoxCollider>());

            renderers.Add(renderer);

            if (backgroundObject.drawFade)
            {
                int depth = backgroundObject.iterations;

                for (int i = 1; i < depth - backgroundObject.depth; i++)
                {
                    var gameObject2 = BackgroundManager.inst.backgroundFadePrefab.Duplicate(gameObject.transform, $"{backgroundObject.name} Fade [{i}]");

                    gameObject2.transform.localPosition = new Vector3(0f, 0f, i);
                    gameObject2.transform.localScale = Vector3.one;
                    gameObject2.transform.localRotation = Quaternion.Euler(Vector3.zero);
                    gameObject2.layer = 9;

                    var renderer2 = gameObject2.GetComponent<Renderer>();
                    renderer2.material = LegacyResources.objectMaterial;

                    renderers.Add(renderer2);
                }
            }

            var runtimeObject = new RTBackgroundObject(backgroundObject, renderers, runtimeLevel);

            runtimeObject.SetActive(false);
            runtimeObject.UpdateShape(backgroundObject.Shape, backgroundObject.ShapeOption);

            if (CoreHelper.InEditor)
                runtimeObject.hidden = backgroundObject.editorData.hidden;

            backgroundObject.ParentRuntime = runtimeLevel;
            backgroundObject.runtimeObject = runtimeObject;

            return runtimeObject;
        }

        public IEnumerable<BackgroundLayerObject> ToBackgroundLayerObjects() => ToBackgroundLayerObjects(GameData.Current.BackgroundLayers);

        public IEnumerable<BackgroundLayerObject> ToBackgroundLayerObjects(IEnumerable<BackgroundLayer> backgroundLayers)
        {
            foreach (var backgroundLayer in backgroundLayers)
            {
                var backgroundLayerObject = ToBackgroundLayerObject(backgroundLayer);
                yield return backgroundLayerObject;
            }
        }

        public BackgroundLayerObject ToBackgroundLayerObject(BackgroundLayer backgroundLayer)
        {
            var gameObject = Creator.NewGameObject("Background Layer", runtimeLevel.SpawnParent);
            gameObject.transform.localPosition = new Vector3(0f, 0f, backgroundLayer.depth);

            var backgroundLayerObject = new BackgroundLayerObject()
            {
                gameObject = gameObject,
                backgroundLayer = backgroundLayer,
            };

            backgroundLayer.runtimeObject = backgroundLayerObject;

            return backgroundLayerObject;
        }

        #endregion

        #region Runtime BG Modifiers

        public bool SkipRuntimeModifiers(BackgroundObject backgroundObject) => backgroundObject.modifiers.IsEmpty();

        public IEnumerable<IRTObject> ToRuntimeBGModifiers() => ToRuntimeBGModifiers(GameData.Current.backgroundObjects);

        public IEnumerable<IRTObject> ToRuntimeBGModifiers(IEnumerable<BackgroundObject> backgroundObjects)
        {
            foreach (var backgroundObject in backgroundObjects)
            {
                if (SkipRuntimeModifiers(backgroundObject))
                {
                    backgroundObject.runtimeModifiers = null;
                    continue;
                }

                var runtimeModifiers = ToRuntimeModifiers(backgroundObject);

                if (runtimeModifiers)
                    yield return runtimeModifiers;
            }
        }

        public IRTObject ToIRuntimeModifiers(BackgroundObject backgroundObject) => SkipRuntimeModifiers(backgroundObject) ? null : ToRuntimeModifiers(backgroundObject);

        RTModifiers ToRuntimeModifiers(BackgroundObject backgroundObject)
        {
            var runtimeModifiers = new RTModifiers(
                    backgroundObject.modifiers, backgroundObject, backgroundObject.orderModifiers,
                    backgroundObject.ignoreLifespan ? -SoundManager.inst.MusicLength : backgroundObject.StartTime,
                    backgroundObject.ignoreLifespan ? SoundManager.inst.MusicLength : backgroundObject.StartTime + backgroundObject.SpawnDuration,
                    runtimeLevel
                );
            backgroundObject.ParentRuntime = runtimeLevel;
            backgroundObject.runtimeModifiers = runtimeModifiers;
            return runtimeModifiers;
        }

        #endregion

        #region Runtime Prefab Objects

        public bool SkipRuntimePrefabObject(PrefabObject prefabObject) => !prefabObject.detailMode.CanSpawn();

        public IEnumerable<IRTObject> ToRuntimePrefabObjects(IEnumerable<PrefabObject> prefabObjects)
        {
            foreach (var prefabObject in prefabObjects)
            {
                if (SkipRuntimePrefabObject(prefabObject))
                {
                    if (prefabObject.runtimeObject && prefabObject.runtimeObject.ParentObjects != null)
                        prefabObject.runtimeObject.ParentObjects.Clear();
                    prefabObject.runtimeObject = null;
                    continue;
                }

                RTPrefabObject runtimePrefabObject = null;

                try
                {
                    runtimePrefabObject = ToRuntimePrefabObject(prefabObject.GetPrefab(), prefabObject);
                }
                catch (Exception e)
                {
                    var stringBuilder = new StringBuilder();
                    stringBuilder.AppendLine($"{RTLevel.className}Failed to convert object '{prefabObject.id}' to {nameof(RTBeatmapObject)}.");
                    stringBuilder.AppendLine($"Exception: {e.Message}");
                    stringBuilder.AppendLine(e.StackTrace);

                    Debug.LogError(stringBuilder.ToString());
                }

                if (runtimePrefabObject)
                    yield return runtimePrefabObject;
            }
        }

        public IRTObject ToIRuntimePrefabObject(Prefab prefab, PrefabObject prefabObject)
        {
            if (SkipRuntimePrefabObject(prefabObject))
            {
                if (prefabObject.runtimeObject && prefabObject.runtimeObject.ParentObjects != null)
                    prefabObject.runtimeObject.ParentObjects.Clear();
                prefabObject.runtimeObject = null;
                return null;
            }

            return ToRuntimePrefabObject(prefab, prefabObject);
        }

        RTPrefabObject ToRuntimePrefabObject(Prefab prefab, PrefabObject prefabObject)
        {
            var runtimeObject = new RTPrefabObject(prefab, prefabObject, runtimeLevel);
            runtimeObject.Load();
            prefabObject.ParentRuntime = runtimeLevel;
            prefabObject.runtimeObject = runtimeObject;

            return runtimeObject;
        }

        #endregion

        #region Runtime Prefab Modifiers

        public bool SkipRuntimeModifiers(PrefabObject prefabObject) => prefabObject.modifiers.IsEmpty() || !prefabObject.detailMode.CanSpawn();

        public IEnumerable<IRTObject> ToRuntimePrefabModifiers() => ToRuntimePrefabModifiers(GameData.Current.prefabObjects);

        public IEnumerable<IRTObject> ToRuntimePrefabModifiers(IEnumerable<PrefabObject> prefabObjects)
        {
            foreach (var prefabObject in prefabObjects)
            {
                if (SkipRuntimeModifiers(prefabObject))
                {
                    prefabObject.runtimeModifiers = null;
                    continue;
                }

                var runtimeModifiers = ToRuntimeModifiers(prefabObject.runtimeObject?.Prefab ?? prefabObject.GetPrefab(), prefabObject);

                if (runtimeModifiers)
                    yield return runtimeModifiers;
            }
        }

        public IRTObject ToIRuntimeModifiers(Prefab prefab, PrefabObject prefabObject) => SkipRuntimeModifiers(prefabObject) ? null : ToRuntimeModifiers(prefab, prefabObject);

        RTModifiers ToRuntimeModifiers(Prefab prefab, PrefabObject prefabObject)
        {
            var runtimeModifiers = new RTPrefabModifiers(
                    prefabObject.modifiers, prefabObject, prefabObject.orderModifiers,
                    prefabObject.ignoreLifespan ? -SoundManager.inst.MusicLength : prefabObject.StartTime + prefab.offset,
                    prefabObject.ignoreLifespan ? SoundManager.inst.MusicLength : prefabObject.StartTime + prefab.offset + prefabObject.SpawnDuration,
                    runtimeLevel
                );
            prefabObject.ParentRuntime = runtimeLevel;
            prefabObject.runtimeModifiers = runtimeModifiers;
            return runtimeModifiers;
        }

        #endregion

        #region Sequences

        public static Vector3Keyframe DefaultVector3Keyframe => new Vector3Keyframe(0f, Vector3.zero, Ease.Linear);
        public static Vector2Keyframe DefaultVector2Keyframe => new Vector2Keyframe(0f, Vector2.one, Ease.Linear);
        public static FloatKeyframe DefaultFloatKeyframe => new FloatKeyframe(0f, 0f, Ease.Linear);
        public static ThemeKeyframe DefaultThemeKeyframe => new ThemeKeyframe(0f, 0, 0f, 0f, 0f, 0f, Ease.Linear);

        public IEnumerator ICacheSequence(BeatmapObject beatmapObject)
        {
            var collection = new CachedSequences()
            {
                PositionSequence = GetVector3Sequence(beatmapObject, beatmapObject.events[0], DefaultVector3Keyframe),
                ScaleSequence = GetVector2Sequence(beatmapObject, beatmapObject.events[1], DefaultVector2Keyframe),
            };
            collection.RotationSequence = GetFloatSequence(beatmapObject, beatmapObject.events[2], 0, DefaultFloatKeyframe, collection.PositionSequence, false);

            // Empty objects don't need a color sequence, so it is not cached
            if (EditorConfig.Instance.ShowEmpties.Value || beatmapObject.objectType != ObjectType.Empty)
            {
                collection.ColorSequence = GetColorSequence(beatmapObject, beatmapObject.events[3], DefaultThemeKeyframe);

                if (beatmapObject.gradientType != 0)
                    collection.SecondaryColorSequence = GetColorSequence(beatmapObject, beatmapObject.events[3], DefaultThemeKeyframe, true);
            }

            beatmapObject.cachedSequences = collection;

            yield break;
        }

        public CachedSequences CacheSequence(BeatmapObject beatmapObject) => beatmapObject.cachedSequences = CreateSequence(beatmapObject);

        public CachedSequences CreateSequence(BeatmapObject beatmapObject)
        {
            var collection = beatmapObject.cachedSequences ?? new CachedSequences();
            UpdateCachedSequence(beatmapObject, collection);
            return collection;
        }

        public void UpdateCachedSequence(BeatmapObject beatmapObject, CachedSequences collection)
        {
            collection.PositionSequence = GetVector3Sequence(beatmapObject, beatmapObject.events[0], DefaultVector3Keyframe);
            collection.ScaleSequence = GetVector2Sequence(beatmapObject, beatmapObject.events[1], DefaultVector2Keyframe);
            collection.RotationSequence = GetFloatSequence(beatmapObject, beatmapObject.events[2], 0, DefaultFloatKeyframe, collection.PositionSequence, false);

            // Empty objects don't need a color sequence, so it is not cached
            if (EditorConfig.Instance.ShowEmpties.Value || beatmapObject.objectType != ObjectType.Empty)
            {
                collection.ColorSequence = GetColorSequence(beatmapObject, beatmapObject.events[3], DefaultThemeKeyframe);

                if (beatmapObject.gradientType != 0)
                    collection.SecondaryColorSequence = GetColorSequence(beatmapObject, beatmapObject.events[3], DefaultThemeKeyframe, true);
                else
                    collection.SecondaryColorSequence = null;
            }
            else
            {
                collection.ColorSequence = null;
                collection.SecondaryColorSequence = null;
            }
        }

        public static Sequence<Vector3> GetVector3Sequence(PAObjectBase obj, List<EventKeyframe> eventKeyframes, Vector3Keyframe defaultKeyframe) => new Sequence<Vector3>(GetVector3Keyframes(obj, eventKeyframes, defaultKeyframe));

        public static List<IKeyframe<Vector3>> GetVector3Keyframes(PAObjectBase obj, List<EventKeyframe> eventKeyframes, Vector3Keyframe defaultKeyframe)
        {
            var keyframes = new List<IKeyframe<Vector3>>(eventKeyframes.Count);

            var currentValue = Vector3.zero;
            IKeyframe<Vector3> currentKeyfame = null;
            int num = 0;
            foreach (var eventKeyframe in eventKeyframes)
            {
                var value = new Vector3(eventKeyframe.values[0], eventKeyframe.values[1], eventKeyframe.values.Length > 2 ? eventKeyframe.values[2] : 0f);
                if (eventKeyframe.random != 0 && eventKeyframe.random != 5 && eventKeyframe.random != 6)
                {
                    var random = RandomHelper.KeyframeRandomizer.RandomizeVector2Keyframe(obj.id, eventKeyframe, num);
                    value.x = random.x;
                    value.y = random.y;
                }

                currentValue = eventKeyframe.relative ? new Vector3(currentValue.x, currentValue.y, 0f) + value : value;

                currentKeyfame = eventKeyframe.random switch
                {
                    5 => new StaticVector3Keyframe(eventKeyframe.time, currentValue, Ease.GetEaseFunction(eventKeyframe.curve), (AxisMode)Mathf.Clamp((int)eventKeyframe.randomValues[3], 0, 2), eventKeyframe.relative, eventKeyframe.homingPriority, eventKeyframe.playerIndex),
                    6 => new DynamicVector3Keyframe(eventKeyframe.time, currentValue, Ease.GetEaseFunction(eventKeyframe.curve),
                        eventKeyframe.randomValues[2], eventKeyframe.randomValues[0], eventKeyframe.randomValues[1],
                        eventKeyframe.flee, (AxisMode)Mathf.Clamp((int)eventKeyframe.randomValues[3], 0, 2), eventKeyframe.relative, eventKeyframe.homingPriority, eventKeyframe.playerIndex),
                    _ => new Vector3Keyframe(eventKeyframe.time, currentValue, Ease.GetEaseFunction(eventKeyframe.curve), eventKeyframe.relative),
                };

                if (!keyframes.Has(x => x.Time == currentKeyfame.Time))
                    keyframes.Add(currentKeyfame);
                num++;
            }

            // If there is no keyframe, add default
            if (keyframes.IsEmpty())
                keyframes.Add(defaultKeyframe);

            return keyframes;
        }

        public static Sequence<Vector2> GetVector2Sequence(PAObjectBase obj, List<EventKeyframe> eventKeyframes, Vector2Keyframe defaultKeyframe) => new Sequence<Vector2>(GetVector2Keyframes(obj, eventKeyframes, defaultKeyframe));

        public static List<IKeyframe<Vector2>> GetVector2Keyframes(PAObjectBase obj, List<EventKeyframe> eventKeyframes, Vector2Keyframe defaultKeyframe)
        {
            List<IKeyframe<Vector2>> keyframes = new List<IKeyframe<Vector2>>(eventKeyframes.Count);

            var currentValue = Vector2.zero;
            int num = 0;
            foreach (var eventKeyframe in eventKeyframes)
            {
                var value = new Vector2(eventKeyframe.values[0], eventKeyframe.values[1]);
                if (eventKeyframe.random != 0 && eventKeyframe.random != 6)
                {
                    var random = RandomHelper.KeyframeRandomizer.RandomizeVector2Keyframe(obj.id, eventKeyframe, num);
                    value.x = random.x;
                    value.y = random.y;
                }

                currentValue = eventKeyframe.relative ? currentValue + value : value;

                if (keyframes.Has(x => x.Time == eventKeyframe.time))
                    continue;

                keyframes.Add(new Vector2Keyframe(eventKeyframe.time, currentValue, Ease.GetEaseFunction(eventKeyframe.curve.ToString())));
                num++;
            }

            // If there is no keyframe, add default
            if (keyframes.IsEmpty())
                keyframes.Add(defaultKeyframe);

            return keyframes;
        }

        public static Sequence<float> GetFloatSequence(PAObjectBase obj, List<EventKeyframe> eventKeyframes, int index, FloatKeyframe defaultKeyframe, Sequence<Vector3> vector3Sequence = null, bool color = false)
            => new Sequence<float>(GetFloatKeyframes(obj, eventKeyframes, index, defaultKeyframe, vector3Sequence, color));

        public static List<IKeyframe<float>> GetFloatKeyframes(PAObjectBase obj, List<EventKeyframe> eventKeyframes, int index, FloatKeyframe defaultKeyframe, Sequence<Vector3> vector3Sequence = null, bool color = false)
        {
            List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>(eventKeyframes.Count);

            var currentValue = 0f;
            IKeyframe<float> currentKeyfame = null;
            int num = 0;
            foreach (var eventKeyframe in eventKeyframes)
            {
                var value = eventKeyframe.random != 0 ? RandomHelper.KeyframeRandomizer.RandomizeFloatKeyframe(obj.id, eventKeyframe, index, num) : eventKeyframe.values[index];

                currentValue = eventKeyframe.relative && !color ? currentValue + value : value;

                currentKeyfame = eventKeyframe.random switch
                {
                    5 => new StaticFloatKeyframe(eventKeyframe.time, currentValue, Ease.GetEaseFunction(eventKeyframe.curve), vector3Sequence, eventKeyframe.relative, eventKeyframe.homingPriority, eventKeyframe.playerIndex),
                    6 => new DynamicFloatKeyframe(eventKeyframe.time, currentValue, Ease.GetEaseFunction(eventKeyframe.curve),
                        eventKeyframe.randomValues[2], eventKeyframe.randomValues[0], eventKeyframe.randomValues[1],
                        eventKeyframe.flee, vector3Sequence, eventKeyframe.relative, eventKeyframe.homingPriority, eventKeyframe.playerIndex),
                    _ => new FloatKeyframe(eventKeyframe.time, currentValue, Ease.GetEaseFunction(eventKeyframe.curve), eventKeyframe.relative),
                };

                if (!keyframes.Has(x => x.Time == currentKeyfame.Time))
                    keyframes.Add(currentKeyfame);
                num++;
            }

            // If there is no keyframe, add default
            if (keyframes.IsEmpty())
                keyframes.Add(defaultKeyframe);

            return keyframes;
        }

        public static Sequence<Color> GetColorSequence(PAObjectBase obj, List<EventKeyframe> eventKeyframes, ThemeKeyframe defaultKeyframe, bool getSecondary = false) => new Sequence<Color>(GetColorKeyframes(obj, eventKeyframes, defaultKeyframe, getSecondary));

        public static List<IKeyframe<Color>> GetColorKeyframes(PAObjectBase obj, List<EventKeyframe> eventKeyframes, ThemeKeyframe defaultKeyframe, bool getSecondary = false)
        {
            List<IKeyframe<Color>> keyframes = new List<IKeyframe<Color>>(eventKeyframes.Count);

            int num = 0;
            int index = getSecondary ? 5 : 0;

            foreach (var eventKeyframe in eventKeyframes)
            {
                if (keyframes.Has(x => x.Time == eventKeyframe.time))
                    continue;

                int value = (int)eventKeyframe.values[index];
                value = Mathf.Clamp(value, 0, ThemeManager.inst.Current.objectColors.Count - 1);

                IKeyframe<Color> currentKeyfame = eventKeyframe.random switch
                {
                    1 => new ColorKeyframe(eventKeyframe.time, RTColors.HexToColor(eventKeyframe.GetStringValue(getSecondary ? 1 : 0, RTColors.WHITE_HEX_CODE)), Ease.GetEaseFunction(eventKeyframe.curve)),
                    _ => new ThemeKeyframe(eventKeyframe.time, value, eventKeyframe.values[index + 1], eventKeyframe.values[index + 2], eventKeyframe.values[index + 3], eventKeyframe.values[index + 4], Ease.GetEaseFunction(eventKeyframe.curve)),
                };

                keyframes.Add(currentKeyfame);

                num++;
            }

            // If there is no keyframe, add default
            if (keyframes.IsEmpty())
                keyframes.Add(defaultKeyframe);

            return keyframes;
        }

        #endregion
    }
}
