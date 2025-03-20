using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

using BetterLegacy.Configs;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization.Objects.Visual;
using BetterLegacy.Editor.Components;

using Object = UnityEngine.Object;
using ObjectType = BetterLegacy.Core.Data.Beatmap.BeatmapObject.ObjectType;

namespace BetterLegacy.Core.Optimization.Objects
{
    // WARNING: This class has side effects and will instantiate GameObjects
    /// <summary>
    /// Converts GameData to LevelObjects to be used by the mod
    /// </summary>
    public class ObjectConverter : Exists
    {
        public class CachedSequences
        {
            public Sequence<Vector3> PositionSequence { get; set; }
            public Sequence<Vector2> ScaleSequence { get; set; }
            public Sequence<float> RotationSequence { get; set; }
            public Sequence<Color> ColorSequence { get; set; }
            public Sequence<Color> SecondaryColorSequence { get; set; }
        }

        public Dictionary<string, CachedSequences> cachedSequences = new Dictionary<string, CachedSequences>();
        public Dictionary<string, BeatmapObject> beatmapObjects = new Dictionary<string, BeatmapObject>();

        public static bool ShowEmpties { get; set; } = false;

        public static bool ShowDamagable { get; set; } = false;

        readonly GameData gameData;

        public ObjectConverter(GameData gameData)
        {
            this.gameData = gameData;

            var beatmapObjects = gameData.beatmapObjects;
            for (int i = 0; i < beatmapObjects.Count; i++)
            {
                if (this.beatmapObjects.ContainsKey(beatmapObjects[i].id))
                    CoreHelper.LogError($"Object with ID \"{beatmapObjects[i].id}\" already exists!");

                this.beatmapObjects[beatmapObjects[i].id] = beatmapObjects[i];
            }

            for (int i = 0; i < beatmapObjects.Count; i++)
                CacheSequence(beatmapObjects[i]);
        }

        public IEnumerable<ILevelObject> ToLevelObjects()
        {
            foreach (var beatmapObject in gameData.beatmapObjects)
            {
                if (VerifyObject(beatmapObject))
                {
                    if (beatmapObject.levelObject != null && beatmapObject.levelObject.parentObjects != null)
                        beatmapObject.levelObject.parentObjects.Clear();
                    if (beatmapObject.levelObject != null)
                        beatmapObject.levelObject = null;
                    continue;
                }

                LevelObject levelObject = null;

                try
                {
                    levelObject = ToLevelObject(beatmapObject);
                }
                catch (Exception e)
                {
                    var stringBuilder = new StringBuilder();
                    stringBuilder.AppendLine($"{Updater.className}Failed to convert object '{beatmapObject.id}' to {nameof(LevelObject)}.");
                    stringBuilder.AppendLine($"Exception: {e.Message}");
                    stringBuilder.AppendLine(e.StackTrace);

                    Debug.LogError(stringBuilder.ToString());
                }

                if (levelObject != null)
                    yield return levelObject;
            }
        }

        public bool VerifyObject(BeatmapObject beatmapObject) => ShowDamagable && beatmapObject.objectType != ObjectType.Normal || !ShowEmpties && beatmapObject.objectType == ObjectType.Empty || beatmapObject.LDM && CoreConfig.Instance.LDM.Value;

        public ILevelObject ToILevelObject(BeatmapObject beatmapObject)
        {
            if (VerifyObject(beatmapObject))
            {
                if (beatmapObject.levelObject != null && beatmapObject.levelObject.parentObjects != null)
                    beatmapObject.levelObject.parentObjects.Clear();
                if (beatmapObject.levelObject != null)
                    beatmapObject.levelObject = null;
                return null;
            }

            LevelObject levelObject = null;

            try
            {
                levelObject = ToLevelObject(beatmapObject);
            }
            catch (Exception e)
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"{Updater.className}Failed to convert object '{beatmapObject.id}' to {nameof(LevelObject)}.");
                stringBuilder.AppendLine($"Exception: {e.Message}");
                stringBuilder.AppendLine(e.StackTrace);

                Debug.LogError(stringBuilder.ToString());
            }

            return levelObject ?? null;
        }

        LevelObject ToLevelObject(BeatmapObject beatmapObject)
        {
            var parentObjects = new List<LevelParentObject>();

            GameObject parent = null;

            if (!string.IsNullOrEmpty(beatmapObject.Parent) && beatmapObjects.TryGetValue(beatmapObject.Parent, out BeatmapObject beatmapObjectParent))
                parent = InitParentChain(beatmapObjectParent, parentObjects);

            var shape = Mathf.Clamp(beatmapObject.shape, 0, ObjectManager.inst.objectPrefabs.Count - 1);
            var shapeOption = Mathf.Clamp(beatmapObject.shapeOption, 0, ObjectManager.inst.objectPrefabs[shape].options.Count - 1);
            var shapeType = (ShapeType)shape;

            GameObject baseObject = Object.Instantiate(ObjectManager.inst.objectPrefabs[shape].options[shapeOption], parent ? parent.transform : ObjectManager.inst.objectParent.transform);
            
            baseObject.transform.localScale = Vector3.one;

            var visualObject = baseObject.transform.GetChild(shapeType == ShapeType.Player ? 1 : 0).gameObject;
            if (beatmapObject.ShapeType != ShapeType.Text || !beatmapObject.autoTextAlign)
                visualObject.transform.localPosition = new Vector3(beatmapObject.origin.x, beatmapObject.origin.y, beatmapObject.Depth * 0.1f);
            if (shapeType != ShapeType.Player)
                visualObject.name = "Visual [ " + beatmapObject.name + " ]";

            if (shapeType == ShapeType.Player)
                baseObject.SetActive(true);

            int num = parentObjects.Count;

            var p = InitLevelParentObject(beatmapObject, baseObject);
            if (!parentObjects.IsEmpty())
                parentObjects.Insert(0, p);
            else
                parentObjects.Add(p);

            baseObject.name = beatmapObject.name;

            var top = new GameObject($"top - [{beatmapObject.name}]");
            top.transform.SetParent(ObjectManager.inst.objectParent.transform);
            top.transform.localScale = Vector3.one;

            var prefabOffsetPosition = Vector3.zero;
            var prefabOffsetScale = Vector3.one;
            var prefabOffsetRotation = Vector3.zero;

            if (beatmapObject.fromPrefab && !string.IsNullOrEmpty(beatmapObject.prefabInstanceID) && gameData.prefabObjects.TryFind(x => x.id == beatmapObject.prefabInstanceID, out PrefabObject prefabObject))
            {
                bool hasPosX = prefabObject.events.Count > 0 && prefabObject.events[0] != null && prefabObject.events[0].values.Length > 0;
                bool hasPosY = prefabObject.events.Count > 0 && prefabObject.events[0] != null && prefabObject.events[0].values.Length > 1;

                bool hasScaX = prefabObject.events.Count > 1 && prefabObject.events[1] != null && prefabObject.events[1].values.Length > 0;
                bool hasScaY = prefabObject.events.Count > 1 && prefabObject.events[1] != null && prefabObject.events[1].values.Length > 1;

                bool hasRot = prefabObject.events.Count > 2 && prefabObject.events[2] != null && prefabObject.events[2].values.Length > 0;

                var pos = new Vector3(
                    hasPosX ? prefabObject.events[0].values[0] : 0f,
                    hasPosY ? prefabObject.events[0].values[1] : 0f,
                    0f);
                var sca = new Vector3(
                    hasScaX ? prefabObject.events[1].values[0] : 1f,
                    hasScaY ? prefabObject.events[1].values[1] : 1f,
                    1f);
                var rot = Quaternion.Euler(0f, 0f, hasRot ? prefabObject.events[2].values[0] : 0f);

                if (prefabObject.events[0].random != 0)
                    pos = RandomHelper.KeyframeRandomizer.RandomizeVector2Keyframe(prefabObject.events[0]);
                if (prefabObject.events[1].random != 0)
                    sca = RandomHelper.KeyframeRandomizer.RandomizeVector2Keyframe(prefabObject.events[1]);
                if (prefabObject.events[2].random != 0)
                    rot = Quaternion.Euler(0f, 0f, RandomHelper.KeyframeRandomizer.RandomizeFloatKeyframe(prefabObject.events[2]));

                prefabOffsetPosition = pos;
                prefabOffsetScale = sca.x != 0f && sca.y != 0f ? sca : Vector3.one;
                prefabOffsetRotation = rot.eulerAngles;
            }

            var tf = !parentObjects.IsEmpty() && parentObjects[parentObjects.Count - 1] && parentObjects[parentObjects.Count - 1].transform ?
                parentObjects[parentObjects.Count - 1].transform : baseObject.transform;

            tf.SetParent(top.transform);
            tf.localScale = Vector3.one;

            baseObject.SetActive(true);
            visualObject.SetActive(true);

            // Init visual object wrapper
            float opacity = beatmapObject.objectType == ObjectType.Helper ? 0.35f : 1.0f;
            bool hasCollider = beatmapObject.objectType == ObjectType.Helper ||
                               beatmapObject.objectType == ObjectType.Decoration;

            bool isSolid = beatmapObject.objectType == ObjectType.Solid;
            bool isBackground = beatmapObject.renderLayerType == BeatmapObject.RenderLayerType.Background;
            bool dontRotate = shapeType == ShapeType.Player && beatmapObject.tags != null && beatmapObject.tags.Has(x => x == "DontRotate");
            int playerIndex = shapeType == ShapeType.Player && beatmapObject.events.Count > 3 && beatmapObject.events[3].Count > 0 && beatmapObject.events[3][0].values.Length > 0 ? (int)beatmapObject.events[3][0].values[0] : 0;

            VisualObject visual = shapeType switch
            {
                ShapeType.Text => new TextObject(visualObject, opacity, beatmapObject.text, beatmapObject.autoTextAlign, TextObject.GetAlignment(beatmapObject.origin), isBackground),
                ShapeType.Image => new ImageObject(visualObject, opacity, beatmapObject.text, isBackground, AssetManager.SpriteAssets.TryGetValue(beatmapObject.text, out Sprite spriteAsset) ? spriteAsset : null),
                ShapeType.Player => new PlayerObject(visualObject, playerIndex, dontRotate, shapeOption),
                ShapeType.Polygon => new PolygonObject(visualObject, opacity, hasCollider, isSolid, isBackground, beatmapObject.opacityCollision, (int)beatmapObject.gradientType, beatmapObject.polygonShapeSettings),
                _ => new SolidObject(visualObject, opacity, hasCollider, isSolid, isBackground, beatmapObject.opacityCollision, (int)beatmapObject.gradientType),
            };

            if (CoreHelper.InEditor && shapeType != ShapeType.Player)
            {
                var obj = visualObject.AddComponent<SelectObject>();
                obj.SetObject(beatmapObject);
                beatmapObject.selector = obj;

                Object.Destroy(visualObject.GetComponent<SelectObjectInEditor>());
            }

            var cachedSequence = cachedSequences[beatmapObject.id];

            visual.colorSequence = cachedSequence.ColorSequence;
            visual.secondaryColorSequence = cachedSequence.SecondaryColorSequence;

            var levelObject = new LevelObject(
                beatmapObject,
                parentObjects, visual,
                prefabOffsetPosition, prefabOffsetScale, prefabOffsetRotation);

            levelObject.SetActive(false);

            beatmapObject.levelObject = levelObject;

            return levelObject;
        }

        GameObject InitParentChain(BeatmapObject beatmapObject, List<LevelParentObject> parentObjects)
        {
            var gameObject = new GameObject(beatmapObject.name);

            parentObjects.Add(InitLevelParentObject(beatmapObject, gameObject));

            // Has parent - init parent (recursive)
            if (!string.IsNullOrEmpty(beatmapObject.Parent) && beatmapObjects.TryGetValue(beatmapObject.Parent, out BeatmapObject beatmapObjectParent))
            {
                var parentObject = InitParentChain(beatmapObjectParent, parentObjects);

                gameObject.transform.SetParent(parentObject.transform);
            }

            return gameObject;
        }

        LevelParentObject InitLevelParentObject(BeatmapObject beatmapObject, GameObject gameObject)
        {
            CachedSequences cachedSequences = null;

            try
            {
                if (this.cachedSequences.TryGetValue(beatmapObject.id, out CachedSequences c))
                    cachedSequences = c;

            }
            catch (Exception e)
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"Failed to init level parent object sequence for '{beatmapObject.id}'.");
                stringBuilder.AppendLine($"Exception: {e.Message}");
                stringBuilder.AppendLine(e.StackTrace);

                Debug.LogError(stringBuilder.ToString());
            }

            LevelParentObject levelParentObject = null;

            try
            {
                if (cachedSequences != null)
                    levelParentObject = new LevelParentObject
                    {
                        positionSequence = cachedSequences.PositionSequence,
                        scaleSequence = cachedSequences.ScaleSequence,
                        rotationSequence = cachedSequences.RotationSequence,

                        timeOffset = beatmapObject.StartTime,

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
                        BeatmapObject = beatmapObject
                    };
                else
                {
                    var pos = new List<IKeyframe<Vector3>>();
                    pos.Add(new Vector3Keyframe(0f, Vector3.zero, Ease.Linear, null));

                    var sca = new List<IKeyframe<Vector2>>();
                    sca.Add(new Vector2Keyframe(0f, Vector2.one, Ease.Linear));

                    var rot = new List<IKeyframe<float>>();
                    rot.Add(new FloatKeyframe(0f, 0f, Ease.Linear, null));

                    levelParentObject = new LevelParentObject
                    {
                        positionSequence = new Sequence<Vector3>(pos),
                        scaleSequence = new Sequence<Vector2>(sca),
                        rotationSequence = new Sequence<float>(rot),

                        timeOffset = beatmapObject.StartTime,

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
                        BeatmapObject = beatmapObject
                    };
                } // In case the CashedSequence is null, set defaults.
            }
            catch (Exception e)
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"Failed to init level parent object for '{beatmapObject.id}'.");
                stringBuilder.AppendLine($"Exception: {e.Message}");
                stringBuilder.AppendLine(e.StackTrace);

                Debug.LogError(stringBuilder.ToString());
            }

            return levelParentObject;
        }

        #region Sequences

        public static Vector3Keyframe DefaultVector3Keyframe => new Vector3Keyframe(0f, Vector3.zero, Ease.Linear);
        public static Vector2Keyframe DefaultVector2Keyframe => new Vector2Keyframe(0f, Vector2.one, Ease.Linear);
        public static FloatKeyframe DefaultFloatKeyframe => new FloatKeyframe(0f, 0f, Ease.Linear);
        public static ThemeKeyframe DefaultThemeKeyframe => new ThemeKeyframe(0f, 0, 0f, 0f, 0f, 0f, Ease.Linear);

        public IEnumerator ICacheSequence(BeatmapObject beatmapObject)
        {
            var collection = new CachedSequences()
            {
                PositionSequence = GetVector3Sequence(beatmapObject.events[0], DefaultVector3Keyframe),
                ScaleSequence = GetVector2Sequence(beatmapObject.events[1], DefaultVector2Keyframe),
            };
            collection.RotationSequence = GetFloatSequence(beatmapObject.events[2], 0, DefaultFloatKeyframe, collection.PositionSequence, false);

            // Empty objects don't need a color sequence, so it is not cached
            if (ShowEmpties || beatmapObject.objectType != ObjectType.Empty)
            {
                collection.ColorSequence = GetColorSequence(beatmapObject.events[3], DefaultThemeKeyframe);

                if (beatmapObject.gradientType != 0)
                    collection.SecondaryColorSequence = GetColorSequence(beatmapObject.events[3], DefaultThemeKeyframe, true);
            }

            cachedSequences[beatmapObject.id] = collection;

            yield break;
        }

        public CachedSequences CacheSequence(BeatmapObject beatmapObject) => cachedSequences[beatmapObject.id] = CreateSequence(beatmapObject);

        public CachedSequences CreateSequence(BeatmapObject beatmapObject)
        {
            var collection = new CachedSequences();
            UpdateCachedSequence(beatmapObject, collection);
            return collection;
        }

        public void UpdateCachedSequence(BeatmapObject beatmapObject, CachedSequences collection)
        {
            collection.PositionSequence = GetVector3Sequence(beatmapObject.events[0], DefaultVector3Keyframe);
            collection.ScaleSequence = GetVector2Sequence(beatmapObject.events[1], DefaultVector2Keyframe);
            collection.RotationSequence = GetFloatSequence(beatmapObject.events[2], 0, DefaultFloatKeyframe, collection.PositionSequence, false);

            // Empty objects don't need a color sequence, so it is not cached
            if (ShowEmpties || beatmapObject.objectType != ObjectType.Empty)
            {
                collection.ColorSequence = GetColorSequence(beatmapObject.events[3], DefaultThemeKeyframe);

                if (beatmapObject.gradientType != 0)
                    collection.SecondaryColorSequence = GetColorSequence(beatmapObject.events[3], DefaultThemeKeyframe, true);
            }
        }

        public static Sequence<Vector3> GetVector3Sequence(List<EventKeyframe> eventKeyframes, Vector3Keyframe defaultKeyframe) => new Sequence<Vector3>(GetVector3Keyframes(eventKeyframes, defaultKeyframe));

        public static List<IKeyframe<Vector3>> GetVector3Keyframes(List<EventKeyframe> eventKeyframes, Vector3Keyframe defaultKeyframe)
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
                    var random = RandomHelper.KeyframeRandomizer.RandomizeVector2Keyframe(eventKeyframe);
                    value.x = random.x;
                    value.y = random.y;
                }

                currentValue = eventKeyframe.relative && eventKeyframe.random != 6 ? new Vector3(currentValue.x, currentValue.y, 0f) + value : value;

                var isStaticHoming = eventKeyframe.random == 5 || eventKeyframe.random != 6 && eventKeyframes.Count > num + 1 && eventKeyframes[num + 1].random == 5;
                var isDynamicHoming = eventKeyframe.random == 6 || eventKeyframe.random != 6 && eventKeyframes.Count > num + 1 && eventKeyframes[num + 1].random == 6;

                currentKeyfame =
                    isStaticHoming ? new StaticVector3Keyframe(
                        eventKeyframe.time,
                        currentValue,
                        Ease.GetEaseFunction(eventKeyframe.curve.ToString()),
                        currentKeyfame,
                        (AxisMode)Mathf.Clamp((int)eventKeyframe.randomValues[3], 0, 2)) :
                    isDynamicHoming ? new DynamicVector3Keyframe(
                        eventKeyframe.time,
                        currentValue,
                        Ease.GetEaseFunction(eventKeyframe.curve.ToString()),
                        eventKeyframe.randomValues[2],
                        eventKeyframe.randomValues[0],
                        eventKeyframe.randomValues[1],
                        eventKeyframe.relative,
                        currentKeyfame,
                        (AxisMode)Mathf.Clamp((int)eventKeyframe.randomValues[3], 0, 2)) :
                    new Vector3Keyframe(eventKeyframe.time, currentValue, Ease.GetEaseFunction(eventKeyframe.curve.ToString()), currentKeyfame);

                if (!keyframes.Has(x => x.Time == currentKeyfame.Time))
                    keyframes.Add(currentKeyfame);
                num++;
            }

            // If there is no keyframe, add default
            if (keyframes.IsEmpty())
                keyframes.Add(defaultKeyframe);

            return keyframes;
        }

        public static Sequence<Vector2> GetVector2Sequence(List<EventKeyframe> eventKeyframes, Vector2Keyframe defaultKeyframe) => new Sequence<Vector2>(GetVector2Keyframes(eventKeyframes, defaultKeyframe));

        public static List<IKeyframe<Vector2>> GetVector2Keyframes(List<EventKeyframe> eventKeyframes, Vector2Keyframe defaultKeyframe)
        {
            List<IKeyframe<Vector2>> keyframes = new List<IKeyframe<Vector2>>(eventKeyframes.Count);

            var currentValue = Vector2.zero;
            foreach (var eventKeyframe in eventKeyframes)
            {
                var value = new Vector2(eventKeyframe.values[0], eventKeyframe.values[1]);
                if (eventKeyframe.random != 0 && eventKeyframe.random != 6)
                {
                    var random = RandomHelper.KeyframeRandomizer.RandomizeVector2Keyframe(eventKeyframe);
                    value.x = random.x;
                    value.y = random.y;
                }

                currentValue = eventKeyframe.relative ? currentValue + value : value;

                if (keyframes.Has(x => x.Time == eventKeyframe.time))
                    continue;

                keyframes.Add(new Vector2Keyframe(eventKeyframe.time, currentValue, Ease.GetEaseFunction(eventKeyframe.curve.ToString())));
            }

            // If there is no keyframe, add default
            if (keyframes.IsEmpty())
                keyframes.Add(defaultKeyframe);

            return keyframes;
        }

        public static Sequence<float> GetFloatSequence(List<EventKeyframe> eventKeyframes, int index, FloatKeyframe defaultKeyframe, Sequence<Vector3> vector3Sequence = null, bool color = false)
            => new Sequence<float>(GetFloatKeyframes(eventKeyframes, index, defaultKeyframe, vector3Sequence, color));

        public static List<IKeyframe<float>> GetFloatKeyframes(List<EventKeyframe> eventKeyframes, int index, FloatKeyframe defaultKeyframe, Sequence<Vector3> vector3Sequence = null, bool color = false)
        {
            List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>(eventKeyframes.Count);

            var currentValue = 0f;
            IKeyframe<float> currentKeyfame = null;
            int num = 0;
            foreach (var eventKeyframe in eventKeyframes)
            {
                var value = eventKeyframe.random != 0 ? RandomHelper.KeyframeRandomizer.RandomizeFloatKeyframe(eventKeyframe, index) : eventKeyframe.values[index];

                currentValue = eventKeyframe.relative && eventKeyframe.random != 6 && !color ? currentValue + value : value;

                var isStaticHoming = (eventKeyframe.random == 5 || eventKeyframe.random != 6 && eventKeyframes.Count > num + 1 && eventKeyframes[num + 1].random == 5) && !color;
                var isDynamicHoming = (eventKeyframe.random == 6 || eventKeyframe.random != 6 && eventKeyframes.Count > num + 1 && eventKeyframes[num + 1].random == 6) && !color;

                currentKeyfame =
                    isStaticHoming ? new StaticFloatKeyframe(
                        eventKeyframe.time,
                        currentValue,
                        Ease.GetEaseFunction(eventKeyframe.curve.ToString()),
                        currentKeyfame,
                        vector3Sequence) :
                    isDynamicHoming ? new DynamicFloatKeyframe(
                        eventKeyframe.time,
                        currentValue,
                        Ease.GetEaseFunction(eventKeyframe.curve.ToString()),
                        eventKeyframe.randomValues[2],
                        eventKeyframe.randomValues[0],
                        eventKeyframe.randomValues[1],
                        eventKeyframe.relative,
                        vector3Sequence) :
                    new FloatKeyframe(eventKeyframe.time, currentValue, Ease.GetEaseFunction(eventKeyframe.curve.ToString()), currentKeyfame);

                if (!keyframes.Has(x => x.Time == currentKeyfame.Time))
                    keyframes.Add(currentKeyfame);
                num++;
            }

            // If there is no keyframe, add default
            if (keyframes.IsEmpty())
                keyframes.Add(defaultKeyframe);

            return keyframes;
        }

        public static Sequence<Color> GetColorSequence(List<EventKeyframe> eventKeyframes, ThemeKeyframe defaultKeyframe, bool getSecondary = false) => new Sequence<Color>(GetColorKeyframes(eventKeyframes, defaultKeyframe, getSecondary));

        public static List<IKeyframe<Color>> GetColorKeyframes(List<EventKeyframe> eventKeyframes, ThemeKeyframe defaultKeyframe, bool getSecondary = false)
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

                keyframes.Add(new ThemeKeyframe(eventKeyframe.time, value, eventKeyframe.values[index + 1], eventKeyframe.values[index + 2], eventKeyframe.values[index + 3], eventKeyframe.values[index + 4], Ease.GetEaseFunction(eventKeyframe.curve.ToString())));

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
