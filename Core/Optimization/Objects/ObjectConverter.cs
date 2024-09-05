using BetterLegacy.Components.Editor;
using BetterLegacy.Components.Player;
using BetterLegacy.Configs;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization.Objects.Visual;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using BaseEventKeyframe = DataManager.GameData.EventKeyframe;
using Object = UnityEngine.Object;
using ObjectType = BetterLegacy.Core.Data.BeatmapObject.ObjectType;

namespace BetterLegacy.Core.Optimization.Objects
{
    // WARNING: This class has side effects and will instantiate GameObjects
    /// <summary>
    /// Converts GameData to LevelObjects to be used by the mod
    /// </summary>
    public class ObjectConverter
    {
        public class CachedSequences
        {
            public Sequence<Vector3> Position3DSequence { get; set; }
            public Sequence<Vector2> ScaleSequence { get; set; }
            public Sequence<float> RotationSequence { get; set; }
            public Sequence<Color> ColorSequence { get; set; }

            public Sequence<float> OpacitySequence { get; set; }
            public Sequence<float> HueSequence { get; set; }
            public Sequence<float> SaturationSequence { get; set; }
            public Sequence<float> ValueSequence { get; set; }
        }

        public Dictionary<string, CachedSequences> cachedSequences = new Dictionary<string, CachedSequences>();
        public Dictionary<string, BeatmapObject> beatmapObjects = new Dictionary<string, BeatmapObject>();

        public static bool ShowEmpties { get; set; } = false;

        public static bool ShowDamagable { get; set; } = false;

        readonly GameData gameData;

        public ObjectConverter(GameData gameData)
        {
            this.gameData = gameData;

            var beatmapObjects = gameData.BeatmapObjects;
            for (int i = 0; i < beatmapObjects.Count; i++)
            {
                this.beatmapObjects.AddSet(beatmapObjects[i].id, beatmapObjects[i]);
            }

            for (int i = 0; i < beatmapObjects.Count; i++)
            {
                if (this.beatmapObjects.ContainsKey(beatmapObjects[i].id))
                    CoreHelper.StartCoroutine(CacheSequence(beatmapObjects[i]));
            }
        }

        public IEnumerator CacheSequence(BeatmapObject beatmapObject)
        {
            var collection = new CachedSequences()
            {
                Position3DSequence = GetVector3Sequence(beatmapObject.events[0], new Vector3Keyframe(0.0f, Vector3.zero, Ease.Linear, null)),
                ScaleSequence = GetVector2Sequence(beatmapObject.events[1], new Vector2Keyframe(0.0f, Vector2.one, Ease.Linear)),
            };
            collection.RotationSequence = GetFloatSequence(beatmapObject.events[2], 0, new FloatKeyframe(0.0f, 0.0f, Ease.Linear, null), collection.Position3DSequence, false);

            // Empty objects don't need a color sequence, so it is not cached
            if (ShowEmpties || beatmapObject.objectType != ObjectType.Empty)
            {
                collection.ColorSequence = GetColorSequence(beatmapObject.events[3], new ThemeKeyframe(0.0f, 0, Ease.Linear), collection.Position3DSequence);

                if (beatmapObject.events[3][0].eventValues.Length > 1)
                    collection.OpacitySequence = GetFloatSequence(beatmapObject.events[3], 1, new FloatKeyframe(0.0f, 0, Ease.Linear, null), collection.Position3DSequence, true);

                if (beatmapObject.events[3][0].eventValues.Length > 2)
                {
                    collection.HueSequence = GetFloatSequence(beatmapObject.events[3], 2, new FloatKeyframe(0.0f, 0, Ease.Linear, null), collection.Position3DSequence, true);
                    collection.SaturationSequence = GetFloatSequence(beatmapObject.events[3], 3, new FloatKeyframe(0.0f, 0, Ease.Linear, null), collection.Position3DSequence, true);
                    collection.ValueSequence = GetFloatSequence(beatmapObject.events[3], 4, new FloatKeyframe(0.0f, 0, Ease.Linear, null), collection.Position3DSequence, true);
                }
            }

            cachedSequences.AddSet(beatmapObject.id, collection);

            yield break;
        }

        public IEnumerable<ILevelObject> ToLevelObjects()
        {
            foreach (var beatmapObject in gameData.BeatmapObjects)
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
                    var sprite = ShowEmpties && beatmapObject.objectType == ObjectType.Empty ? LegacyPlugin.EmptyObjectSprite : null;
                    levelObject = ToLevelObject(beatmapObject, sprite);
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
                var sprite = ShowEmpties && beatmapObject.objectType == ObjectType.Empty ? LegacyPlugin.EmptyObjectSprite : null;
                levelObject = ToLevelObject(beatmapObject, sprite);
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

        LevelObject ToLevelObject(BeatmapObject beatmapObject, Sprite sprite = null)
        {
            var parentObjects = new List<LevelParentObject>();

            GameObject parent = null;

            if (!string.IsNullOrEmpty(beatmapObject.parent) && beatmapObjects.ContainsKey(beatmapObject.parent))
                parent = InitParentChain(beatmapObjects[beatmapObject.parent], parentObjects);

            var shape = Mathf.Clamp(beatmapObject.shape, 0, ObjectManager.inst.objectPrefabs.Count - 1);
            var shapeOption = Mathf.Clamp(beatmapObject.shapeOption, 0, ObjectManager.inst.objectPrefabs[shape].options.Count - 1);

            if (sprite != null)
            {
                shape = 6;
                shapeOption = 0;
            }

            GameObject baseObject = Object.Instantiate(ObjectManager.inst.objectPrefabs[shape].options[shapeOption], parent == null ? ObjectManager.inst.objectParent.transform : parent.transform);

            if (shape == 9)
            {
                var rtPlayer = baseObject.GetComponent<RTPlayer>();
                rtPlayer.PlayerModel = ObjectManager.inst.objectPrefabs[shape].options[shapeOption].GetComponent<RTPlayer>().PlayerModel;
                rtPlayer.playerIndex = beatmapObject.events.Count > 3 && beatmapObject.events[3].Count > 0 && beatmapObject.events[3][0].eventValues.Length > 0 ? (int)beatmapObject.events[3][0].eventValues[0] : 0;
                if (beatmapObject.tags != null && beatmapObject.tags.Has(x => x == "DontRotate"))
                {
                    rtPlayer.CanRotate = false;
                }
            }

            baseObject.transform.localScale = Vector3.one;

            var visualObject = baseObject.transform.GetChild(shape == 9 ? 1 : 0).gameObject;
            visualObject.transform.localPosition = new Vector3(beatmapObject.origin.x, beatmapObject.origin.y, beatmapObject.depth * 0.1f);
            if (shape != 9)
                visualObject.name = "Visual [ " + beatmapObject.name + " ]";

            if (shape == 9)
                baseObject.SetActive(true);

            int num = 0;
            if (parentObjects != null)
                num = parentObjects.Count;

            var p = InitLevelParentObject(beatmapObject, baseObject);
            if (parentObjects.Count > 0)
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

            if (beatmapObject.fromPrefab && !string.IsNullOrEmpty(beatmapObject.prefabInstanceID) && gameData.prefabObjects.Has(x => x.ID == beatmapObject.prefabInstanceID))
            {
                var prefabObject = gameData.prefabObjects.Find(x => x.ID == beatmapObject.prefabInstanceID);

                bool hasPosX = prefabObject.events.Count > 0 && prefabObject.events[0] != null && prefabObject.events[0].eventValues.Length > 0;
                bool hasPosY = prefabObject.events.Count > 0 && prefabObject.events[0] != null && prefabObject.events[0].eventValues.Length > 1;

                bool hasScaX = prefabObject.events.Count > 1 && prefabObject.events[1] != null && prefabObject.events[1].eventValues.Length > 0;
                bool hasScaY = prefabObject.events.Count > 1 && prefabObject.events[1] != null && prefabObject.events[1].eventValues.Length > 1;

                bool hasRot = prefabObject.events.Count > 2 && prefabObject.events[2] != null && prefabObject.events[2].eventValues.Length > 0;

                var pos = new Vector3(
                    hasPosX ? prefabObject.events[0].eventValues[0] : 0f,
                    hasPosY ? prefabObject.events[0].eventValues[1] : 0f,
                    0f);
                var sca = new Vector3(
                    hasScaX ? prefabObject.events[1].eventValues[0] : 1f,
                    hasScaY ? prefabObject.events[1].eventValues[1] : 1f,
                    1f);
                var rot = Quaternion.Euler(0f, 0f, hasRot ? prefabObject.events[2].eventValues[0] : 0f);

                if (prefabObject.events[0].random != 0)
                    pos = ObjectManager.inst.RandomVector2Parser(prefabObject.events[0]);
                if (prefabObject.events[1].random != 0)
                    sca = ObjectManager.inst.RandomVector2Parser(prefabObject.events[1]);
                if (prefabObject.events[2].random != 0)
                    rot = Quaternion.Euler(0f, 0f, ObjectManager.inst.RandomFloatParser(prefabObject.events[2]));

                prefabOffsetPosition = pos;
                prefabOffsetScale = (sca.x > 0f || sca.x < 0f) && (sca.y > 0f || sca.y < 0f) ? sca : Vector3.one;
                prefabOffsetRotation = rot.eulerAngles;
            }

            var tf = parentObjects != null && parentObjects.Count > 0 && parentObjects[parentObjects.Count - 1] && parentObjects[parentObjects.Count - 1].transform ?
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
            bool isBackground = beatmapObject.background;

            // 4 = text object
            // 6 = image object
            // 9 = player object
            VisualObject visual =
                sprite == null && beatmapObject.shape == 4 ? new TextObject(visualObject, opacity, beatmapObject.text, isBackground) :
                sprite != null || beatmapObject.shape == 6 ? new ImageObject(visualObject, opacity, beatmapObject.text, isBackground, sprite != null ? sprite : AssetManager.SpriteAssets.ContainsKey(beatmapObject.text) ? AssetManager.SpriteAssets[beatmapObject.text] : null) :
                beatmapObject.shape == 9 ? new PlayerObject(visualObject) :
                new SolidObject(visualObject, opacity, hasCollider, isSolid, isBackground, beatmapObject.opacityCollision);

            if (EditorManager.inst && shape != 9)
            {
                var obj = visualObject.AddComponent<RTObject>();
                obj.SetObject(beatmapObject);
                beatmapObject.RTObject = obj;
            }

            Object.Destroy(visualObject.GetComponent<SelectObjectInEditor>());

            var levelObject = new LevelObject(
                beatmapObject,
                cachedSequences[beatmapObject.id].ColorSequence,
                parentObjects, visual,
                cachedSequences[beatmapObject.id].OpacitySequence,
                cachedSequences[beatmapObject.id].HueSequence,
                cachedSequences[beatmapObject.id].SaturationSequence,
                cachedSequences[beatmapObject.id].ValueSequence,
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
            if (!string.IsNullOrEmpty(beatmapObject.parent) && beatmapObjects.ContainsKey(beatmapObject.parent))
            {
                var parentObject = InitParentChain(beatmapObjects[beatmapObject.parent], parentObjects);

                gameObject.transform.SetParent(parentObject.transform);
            }

            return gameObject;
        }

        LevelParentObject InitLevelParentObject(BeatmapObject beatmapObject, GameObject gameObject)
        {
            CachedSequences cachedSequences = null;

            try
            {
                if (this.cachedSequences.ContainsKey(beatmapObject.id))
                    cachedSequences = this.cachedSequences[beatmapObject.id];

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
                        position3DSequence = cachedSequences.Position3DSequence,
                        scaleSequence = cachedSequences.ScaleSequence,
                        rotationSequence = cachedSequences.RotationSequence,

                        timeOffset = beatmapObject.StartTime,

                        parentAnimatePosition = beatmapObject.GetParentType(0),
                        parentAnimateScale = beatmapObject.GetParentType(1),
                        parentAnimateRotation = beatmapObject.GetParentType(2),

                        parentOffsetPosition = beatmapObject.getParentOffset(0),
                        parentOffsetScale = beatmapObject.getParentOffset(1),
                        parentOffsetRotation = beatmapObject.getParentOffset(2),

                        parentAdditivePosition = beatmapObject.parentAdditive[0] == '1',
                        parentAdditiveScale = beatmapObject.parentAdditive[1] == '1',
                        parentAdditiveRotation = beatmapObject.parentAdditive[2] == '1',

                        parentParallaxPosition = beatmapObject.parallaxSettings[0],
                        parentParallaxScale = beatmapObject.parallaxSettings[1],
                        parentParallaxRotation = beatmapObject.parallaxSettings[2],

                        gameObject = gameObject,
                        transform = gameObject.transform,
                        id = beatmapObject.id,
                        desync = !string.IsNullOrEmpty(beatmapObject.parent) && beatmapObject.desync,
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
                        position3DSequence = new Sequence<Vector3>(pos),
                        scaleSequence = new Sequence<Vector2>(sca),
                        rotationSequence = new Sequence<float>(rot),

                        timeOffset = beatmapObject.StartTime,

                        parentAnimatePosition = beatmapObject.GetParentType(0),
                        parentAnimateScale = beatmapObject.GetParentType(1),
                        parentAnimateRotation = beatmapObject.GetParentType(2),

                        parentOffsetPosition = beatmapObject.getParentOffset(0),
                        parentOffsetScale = beatmapObject.getParentOffset(1),
                        parentOffsetRotation = beatmapObject.getParentOffset(2),

                        parentAdditivePosition = beatmapObject.parentAdditive[0] == '1',
                        parentAdditiveScale = beatmapObject.parentAdditive[1] == '1',
                        parentAdditiveRotation = beatmapObject.parentAdditive[2] == '1',

                        parentParallaxPosition = beatmapObject.parallaxSettings[0],
                        parentParallaxScale = beatmapObject.parallaxSettings[1],
                        parentParallaxRotation = beatmapObject.parallaxSettings[2],

                        gameObject = gameObject,
                        transform = gameObject.transform,
                        id = beatmapObject.id,
                        desync = !string.IsNullOrEmpty(beatmapObject.parent) && beatmapObject.desync,
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

        public Sequence<Vector3> GetVector3Sequence(List<BaseEventKeyframe> eventKeyframes, Vector3Keyframe defaultKeyframe)
        {
            var keyframes = new List<IKeyframe<Vector3>>(eventKeyframes.Count);

            var currentValue = Vector3.zero;
            IKeyframe<Vector3> currentKeyfame = null;
            int num = 0;
            foreach (var eventKeyframe in eventKeyframes)
            {
                if (!(eventKeyframe is EventKeyframe))
                    continue;

                var kf = (EventKeyframe)eventKeyframe;
                var value = new Vector3(eventKeyframe.eventValues[0], eventKeyframe.eventValues[1], eventKeyframe.eventValues.Length > 2 ? eventKeyframe.eventValues[2] : 0f);
                if (eventKeyframe.random != 0 && eventKeyframe.random != 5 && eventKeyframe.random != 6)
                {
                    var random = ObjectManager.inst.RandomVector2Parser(eventKeyframe);
                    value.x = random.x;
                    value.y = random.y;
                }

                currentValue = kf.relative && eventKeyframe.random != 6 ? new Vector3(currentValue.x, currentValue.y, 0f) + value : value;

                var isStaticHoming = eventKeyframe.random == 5 || eventKeyframe.random != 6 && eventKeyframes.Count > num + 1 && eventKeyframes[num + 1].random == 5;
                var isDynamicHoming = eventKeyframe.random == 6 || eventKeyframe.random != 6 && eventKeyframes.Count > num + 1 && eventKeyframes[num + 1].random == 6;

                currentKeyfame =
                    isStaticHoming ? new StaticVector3Keyframe(
                        eventKeyframe.eventTime,
                        currentValue,
                        Ease.GetEaseFunction(eventKeyframe.curveType.Name),
                        currentKeyfame,
                        (AxisMode)Mathf.Clamp((int)eventKeyframe.eventRandomValues[3], 0, 2)) :
                    isDynamicHoming ? new DynamicVector3Keyframe(
                        eventKeyframe.eventTime,
                        currentValue,
                        Ease.GetEaseFunction(eventKeyframe.curveType.Name),
                        eventKeyframe.eventRandomValues[2],
                        eventKeyframe.eventRandomValues[0],
                        eventKeyframe.eventRandomValues[1],
                        kf.relative,
                        currentKeyfame,
                        (AxisMode)Mathf.Clamp((int)eventKeyframe.eventRandomValues[3], 0, 2)) :
                    new Vector3Keyframe(eventKeyframe.eventTime, currentValue, Ease.GetEaseFunction(eventKeyframe.curveType.Name), currentKeyfame);

                if (!keyframes.Has(x => x.Time == currentKeyfame.Time))
                    keyframes.Add(currentKeyfame);
                num++;
            }

            // If there is no keyframe, add default
            if (keyframes.Count == 0)
                keyframes.Add(defaultKeyframe);

            return new Sequence<Vector3>(keyframes);
        }

        public Sequence<Vector2> GetVector2Sequence(List<BaseEventKeyframe> eventKeyframes, Vector2Keyframe defaultKeyframe)
        {
            List<IKeyframe<Vector2>> keyframes = new List<IKeyframe<Vector2>>(eventKeyframes.Count);

            var currentValue = Vector2.zero;
            foreach (var eventKeyframe in eventKeyframes)
            {
                if (!(eventKeyframe is EventKeyframe))
                    continue;

                var kf = (EventKeyframe)eventKeyframe;
                var value = new Vector2(eventKeyframe.eventValues[0], eventKeyframe.eventValues[1]);
                if (eventKeyframe.random != 0 && eventKeyframe.random != 6)
                {
                    var random = ObjectManager.inst.RandomVector2Parser(eventKeyframe);
                    value.x = random.x;
                    value.y = random.y;
                }
                currentValue = kf.relative ? currentValue + value : value;

                if (keyframes.Has(x => x.Time == eventKeyframe.eventTime))
                    continue;

                keyframes.Add(new Vector2Keyframe(eventKeyframe.eventTime, currentValue, Ease.GetEaseFunction(eventKeyframe.curveType.Name)));
            }

            // If there is no keyframe, add default
            if (keyframes.Count == 0)
                keyframes.Add(defaultKeyframe);

            return new Sequence<Vector2>(keyframes);
        }

        public Sequence<float> GetFloatSequence(List<BaseEventKeyframe> eventKeyframes, int index, FloatKeyframe defaultKeyframe, Sequence<Vector3> vector3Sequence, bool color)
        {
            List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>(eventKeyframes.Count);

            var currentValue = 0f;
            IKeyframe<float> currentKeyfame = null;
            int num = 0;
            foreach (var eventKeyframe in eventKeyframes)
            {
                if (!(eventKeyframe is EventKeyframe))
                    continue;

                var kf = (EventKeyframe)eventKeyframe;
                var value = eventKeyframe.random != 0 ? RandomFloatParser(eventKeyframe, index) : eventKeyframe.eventValues[index];

                currentValue = kf.relative && eventKeyframe.random != 6 && !color ? currentValue + value : value;

                var isStaticHoming = (eventKeyframe.random == 5 || eventKeyframe.random != 6 && eventKeyframes.Count > num + 1 && eventKeyframes[num + 1].random == 5) && !color;
                var isDynamicHoming = (eventKeyframe.random == 6 || eventKeyframe.random != 6 && eventKeyframes.Count > num + 1 && eventKeyframes[num + 1].random == 6) && !color;

                currentKeyfame =
                    isStaticHoming ? new StaticFloatKeyframe(
                        eventKeyframe.eventTime,
                        currentValue,
                        Ease.GetEaseFunction(eventKeyframe.curveType.Name),
                        currentKeyfame,
                        vector3Sequence) :
                    isDynamicHoming ? new DynamicFloatKeyframe(
                        eventKeyframe.eventTime,
                        currentValue,
                        Ease.GetEaseFunction(eventKeyframe.curveType.Name),
                        eventKeyframe.eventRandomValues[2],
                        eventKeyframe.eventRandomValues[0],
                        eventKeyframe.eventRandomValues[1],
                        kf.relative,
                        vector3Sequence) :
                    new FloatKeyframe(eventKeyframe.eventTime, currentValue, Ease.GetEaseFunction(eventKeyframe.curveType.Name), currentKeyfame);

                if (!keyframes.Has(x => x.Time == currentKeyfame.Time))
                    keyframes.Add(currentKeyfame);
                num++;
            }

            // If there is no keyframe, add default
            if (keyframes.Count == 0)
                keyframes.Add(defaultKeyframe);

            return new Sequence<float>(keyframes);
        }

        public Sequence<Color> GetColorSequence(List<BaseEventKeyframe> eventKeyframes, ThemeKeyframe defaultKeyframe, Sequence<Vector3> vector3Sequence)
        {
            List<IKeyframe<Color>> keyframes = new List<IKeyframe<Color>>(eventKeyframes.Count);

            int num = 0;
            foreach (BaseEventKeyframe eventKeyframe in eventKeyframes)
            {
                int value = (int)eventKeyframe.eventValues[0];

                value = Mathf.Clamp(value, 0, GameManager.inst.LiveTheme.objectColors.Count - 1);

                if (!keyframes.Has(x => x.Time == eventKeyframe.eventTime))
                    keyframes.Add(eventKeyframe.random == 6 || eventKeyframe.random != 6 && eventKeyframes.Count > num + 1 && eventKeyframes[num + 1].random == 6 ? new DynamicThemeKeyframe(eventKeyframe.eventTime, value, Ease.GetEaseFunction(eventKeyframe.curveType.Name),
                    eventKeyframe.eventRandomValues[2], eventKeyframe.eventRandomValues[0], eventKeyframe.eventRandomValues[1], false,
                    Mathf.Clamp((int)eventKeyframe.eventRandomValues[3], 0, GameManager.inst.LiveTheme.objectColors.Count - 1), vector3Sequence) :
                    new ThemeKeyframe(eventKeyframe.eventTime, value, Ease.GetEaseFunction(eventKeyframe.curveType.Name)));
                num++;
            }

            // If there is no keyframe, add default
            if (keyframes.Count == 0)
            {
                keyframes.Add(defaultKeyframe);
            }

            return new Sequence<Color>(keyframes);
        }

        public float RandomFloatParser(BaseEventKeyframe _floatEvent, int index)
        {
            float result = 0f;
            switch (_floatEvent.random)
            {
                case 1:
                    result = _floatEvent.eventRandomValues.Length > 2 && _floatEvent.eventRandomValues[2] != 0f ?
                        RTMath.RoundToNearestNumber(UnityEngine.Random.Range(_floatEvent.eventValues[index], _floatEvent.eventRandomValues[0]), _floatEvent.eventRandomValues[2]) :
                        UnityEngine.Random.Range(_floatEvent.eventValues[index], _floatEvent.eventRandomValues[0]);
                    break;
                case 2:
                    result = Mathf.Round(UnityEngine.Random.Range(_floatEvent.eventValues[index], _floatEvent.eventRandomValues[0]));
                    break;
                case 3:
                    result = (UnityEngine.Random.value > 0.5f) ? _floatEvent.eventValues[index] : _floatEvent.eventRandomValues[0];
                    break;
                case 4:
                    result = _floatEvent.eventValues[index] * _floatEvent.eventRandomValues.Length > 2 && _floatEvent.eventRandomValues[2] != 0f ?
                            RTMath.RoundToNearestNumber(UnityEngine.Random.Range(_floatEvent.eventRandomValues[0], _floatEvent.eventRandomValues[1]), _floatEvent.eventRandomValues[2]) :
                            UnityEngine.Random.Range(_floatEvent.eventRandomValues[0], _floatEvent.eventRandomValues[1]);
                    break;
            }
            return result;
        }
    }
}
