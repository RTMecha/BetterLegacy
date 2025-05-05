using System;
using System.Collections.Generic;

using UnityEngine;

using LSFunctions;

using ILMath;

using SimpleJSON;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime.Objects;
using BetterLegacy.Editor.Data;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Represents an object that appears in the background and can fade. Looks like the towers from the PS2 startup.
    /// </summary>
    public class BackgroundObject : PAObject<BackgroundObject>, IPrefabable, ILifetime<AutoKillType>, ITransformable, IEvaluatable
    {
        public BackgroundObject() : base() { }

        public void SetShape(int shape, int shapeOption)
        {
            this.shape = shape;
            this.shapeOption = shapeOption;
            runtimeObject?.UpdateShape(shape, shapeOption);
        }

        #region Values

        public bool active = true;
        public string name = "Background";

        public string layer = string.Empty;

        // todo: change background objects to be in the main editor timeline and have the same start time system as beatmap objects.
        public ObjectEditorData editorData = new ObjectEditorData();

        #region Timing

        float startTime;
        public float StartTime { get => startTime; set => startTime = value; }

        /// <summary>
        /// Object despawn behavior.
        /// </summary>
        public AutoKillType autoKillType;

        public AutoKillType AutoKillType { get => autoKillType; set => autoKillType = value; }

        /// <summary>
        /// Autokill time offset.
        /// </summary>
        public float autoKillOffset;

        public float AutoKillOffset { get => autoKillOffset; set => autoKillOffset = value; }

        public bool Alive
        {
            get
            {
                var time = AudioManager.inst.CurrentAudioSource.time;
                var st = StartTime;
                var akt = autoKillType;
                var ako = autoKillOffset;
                var l = SpawnDuration;
                return time >= st && (time <= l + st && akt != AutoKillType.NoAutokill && akt != AutoKillType.SongTime || akt == AutoKillType.NoAutokill || time < ako && akt == AutoKillType.SongTime);
            }
        }

        /// <summary>
        /// Total length of the objects' sequence.
        /// </summary>
        public float Length => 0f;

        public float SpawnDuration => GetObjectLifeLength(0.0f, true);

        #endregion

        #region Transforms

        public Vector2 pos = Vector2.zero;
        public Vector2 scale = Vector2.one;
        public float rot;
        public Vector2 rotation = Vector2.zero;
        public float zscale = 10f;
        public int iterations = 9;
        public float zposition;
        public int depth;
        public bool drawFade = true;

        #endregion

        #region Shape

        public int shape;
        public int shapeOption;
        public string text = string.Empty;
        public bool flat;

        #endregion

        #region Reactive

        public bool IsReactive => reactiveType != ReactiveType.None;

        public ReactiveType reactiveType = ReactiveType.None;
        public float reactiveScale = 1f;

        public Vector2Int reactivePosSamples;
        public Vector2Int reactiveScaSamples;
        public int reactiveRotSample;
        public int reactiveColSample;
        public int reactiveCol;

        public Vector2 reactivePosIntensity;
        public Vector2 reactiveScaIntensity;
        public float reactiveRotIntensity;
        public float reactiveColIntensity;

        public float reactiveZIntensity;
        public int reactiveZSample;

        public Vector2 reactiveSize = new Vector2(0f, 0f);

        public enum ReactiveType
        {
            None,
            Bass,
            Mids,
            Treble,
            Custom
        }

        #endregion

        #region Colors

        public int color;
        public float hue;
        public float saturation;
        public float value;

        public int fadeColor;
        public float fadeHue;
        public float fadeSaturation;
        public float fadeValue;

        #endregion

        #region Modifiers

        public List<string> tags = new List<string>();

        public List<Modifier<BackgroundObject>> modifiers = new List<Modifier<BackgroundObject>>();

        /// <summary>
        /// If modifiers ignore the lifespan restriction.
        /// </summary>
        public bool ignoreLifespan = false;

        /// <summary>
        /// If the order of triggers and actions matter.
        /// </summary>
        public bool orderModifiers = false;

        public Vector3 positionOffset;
        public Vector3 scaleOffset;
        public Vector3 rotationOffset;

        public Vector3 PositionOffset { get => positionOffset; set => positionOffset = value; }
        public Vector3 ScaleOffset { get => scaleOffset; set => scaleOffset = value; }
        public Vector3 RotationOffset { get => rotationOffset; set => rotationOffset = value; }

        /// <summary>
        /// If the modifiers are currently active.
        /// </summary>
        public bool ModifiersActive
        {
            get
            {
                var startTime = ignoreLifespan ? 0f : StartTime;
                var killTime = ignoreLifespan ? SoundManager.inst.MusicLength : StartTime + SpawnDuration;
                return AudioManager.inst.CurrentAudioSource.time >= startTime && AudioManager.inst.CurrentAudioSource.time <= killTime;
            }
        }

        #endregion

        #region Prefab

        /// <summary>
        /// Used for objects spawned from a Prefab Object.
        /// </summary>
        public string originalID;

        /// <summary>
        /// If the object is spawned from a prefab.
        /// </summary>
        public bool fromPrefab;

        /// <summary>
        /// Prefab reference ID.
        /// </summary>
        public string prefabID = string.Empty;

        /// <summary>
        /// Prefab Object reference ID.
        /// </summary>
        public string prefabInstanceID = string.Empty;

        public string OriginalID { get => originalID; set => originalID = value; }

        public string PrefabID { get => prefabID; set => prefabID = value; }

        public string PrefabInstanceID { get => prefabInstanceID; set => prefabInstanceID = value; }

        public bool FromPrefab { get => fromPrefab; set => fromPrefab = value; }

        #endregion

        #region References

        public bool Enabled { get; set; } = true;

        public RTBackgroundObject runtimeObject;

        public RTModifiers<BackgroundObject> runtimeModifiers;

        /// <summary>
        /// Used for editor.
        /// </summary>
        public TimelineObject timelineObject;

        #endregion

        #endregion

        #region Methods

        public override void CopyData(BackgroundObject orig, bool newID = true)
        {
            id = newID ? LSText.randomNumString(16) : orig.id;
            name = orig.name;
            startTime = orig.startTime;
            autoKillType = orig.autoKillType;
            autoKillOffset = orig.autoKillOffset;
            active = orig.active;
            drawFade = orig.drawFade;
            depth = orig.depth;
            layer = orig.layer;
            pos = orig.pos;
            reactiveScale = orig.reactiveScale;
            reactiveSize = orig.reactiveSize;
            reactiveType = orig.reactiveType;
            rot = orig.rot;
            scale = orig.scale;
            text = orig.text;
            iterations = orig.iterations;
            shape = orig.shape;
            shapeOption = orig.shapeOption;
            flat = orig.flat;
            zposition = orig.zposition;
            zscale = orig.zscale;
            rotation = orig.rotation;

            this.SetPrefabReference(orig);

            color = orig.color;
            hue = orig.hue;
            saturation = orig.saturation;
            value = orig.value;

            fadeColor = orig.fadeColor;
            fadeHue = orig.fadeHue;
            fadeSaturation = orig.fadeSaturation;
            fadeValue = orig.fadeValue;

            reactiveCol = orig.reactiveCol;
            reactiveColSample = orig.reactiveColSample;
            reactiveColIntensity = orig.reactiveColIntensity;
            reactivePosSamples = orig.reactivePosSamples;
            reactivePosIntensity = orig.reactivePosIntensity;
            reactiveRotSample = orig.reactiveRotSample;
            reactiveRotIntensity = orig.reactiveRotIntensity;
            reactiveScaSamples = orig.reactiveScaSamples;
            reactiveScaIntensity = orig.reactiveScaIntensity;
            reactiveZIntensity = orig.reactiveZIntensity;
            reactiveZSample = orig.reactiveZSample;

            tags = orig.tags.Clone();
            ignoreLifespan = orig.ignoreLifespan;
            orderModifiers = orig.orderModifiers;
            modifiers.Clear();
            for (int i = 0; i < orig.modifiers.Count; i++)
                modifiers.Add(Modifier<BackgroundObject>.DeepCopy(orig.modifiers[i], this));

            editorData = ObjectEditorData.DeepCopy(orig.editorData);
        }

        public override void ReadJSONVG(JSONNode jn, Version version = default)
        {
            flat = true;
            if (jn["t"] != null)
            {
                pos = jn["t"]["p"].AsVector2();
                scale = jn["t"]["s"].AsVector2();
                rot = jn["t"]["r"].AsFloat;
            }

            if (jn["s"] != null)
            {
                shape = jn["s"]["s"].AsInt; // wtf
                shapeOption = jn["s"]["so"].AsInt;
                text = jn["s"]["t"] ?? string.Empty;
            }
        }

        public override void ReadJSON(JSONNode jn)
        {
            if (jn["id"] != null)
                id = jn["id"];

            if (jn["piid"] != null)
                prefabInstanceID = jn["piid"];

            if (jn["pid"] != null)
                prefabID = jn["pid"];

            if (jn["active"] != null)
                active = jn["active"].AsBool;

            if (jn["name"] != null)
                name = jn["name"];

            StartTime = jn["st"].AsFloat;
            autoKillType = (AutoKillType)jn["akt"].AsInt;
            autoKillOffset = jn["ako"].AsFloat;

            pos = new Vector2(jn["pos"]["x"].AsFloat, jn["pos"]["y"].AsFloat);
            scale = new Vector2(jn["size"]["x"].AsFloat, jn["size"]["y"].AsFloat);

            rot = jn["rot"].AsFloat;

            if (jn["layer"] != null)
                depth = jn["layer"].AsInt;
            else
                depth = jn["d"].AsInt;

            if (jn["l"] != null)
                layer = jn["l"];

            if (jn["zposition"] != null)
                zposition = jn["zposition"].AsFloat;

            if (jn["zpos"] != null)
                zposition = jn["zpos"].AsFloat;

            if (jn["zscale"] != null)
                zscale = jn["zscale"].AsFloat;

            if (jn["zsca"] != null)
                zscale = jn["zsca"].AsFloat;

            if (jn["depth"] != null)
                iterations = jn["depth"].AsInt;

            if (jn["iter"] != null)
                iterations = jn["iter"].AsInt;

            if (jn["s"] != null)
                shape = jn["s"].AsInt;

            if (jn["so"] != null)
                shapeOption = jn["so"].AsInt;

            if (jn["text"] != null)
                text = jn["text"];

            if (jn["col"] != null)
            {
                color = jn["col"]["slot"].AsInt;
                hue = jn["col"]["hue"].AsFloat;
                saturation = jn["col"]["sat"].AsFloat;
                value = jn["col"]["val"].AsFloat;
            }
            else
            {
                color = jn["color"].AsInt;
                hue = jn["sat"].AsFloat;
                saturation = jn["sat"].AsFloat;
                value = jn["val"].AsFloat;
            }

            if (jn["fade"] != null)
                drawFade = jn["fade"].AsBool;

            if (jn["fade_col"] != null)
            {
                fadeColor = jn["fade_col"]["slot"].AsInt;
                fadeHue = jn["fade_col"]["hue"].AsFloat;
                fadeSaturation = jn["fade_col"]["sat"].AsFloat;
                fadeValue = jn["fade_col"]["val"].AsFloat;
            }
            else
            {
                fadeColor = jn["color_fade"].AsInt;
                fadeHue = jn["fade_hue"].AsFloat;
                fadeSaturation = jn["fade_sat"].AsFloat;
                fadeValue = jn["fade_val"].AsFloat;
            }

            if (jn["r_offset"] != null && jn["r_offset"]["x"] != null && jn["r_offset"]["y"] != null)
                rotation = new Vector2(jn["r_offset"]["x"].AsFloat, jn["r_offset"]["y"].AsFloat);

            var reactiveType = jn["r_set"]["type"];
            if (reactiveType != null)
            {
                if (reactiveType == "LOW")
                    this.reactiveType = ReactiveType.Bass;
                else if (reactiveType == "MID")
                    this.reactiveType = ReactiveType.Mids;
                else if (reactiveType == "HIGH")
                    this.reactiveType = ReactiveType.Treble;
                else
                    this.reactiveType = Parser.TryParse(jn["r_set"]["type"], true, ReactiveType.None);
            }

            if (jn["r_set"]["scale"] != null)
                reactiveScale = jn["r_set"]["scale"].AsFloat;

            if (jn["rc"] != null)
            {
                try
                {
                    if (jn["rc"]["pos"] != null && jn["rc"]["pos"]["i"] != null && jn["rc"]["pos"]["i"]["x"] != null && jn["rc"]["pos"]["i"]["y"] != null)
                        reactivePosIntensity = new Vector2(jn["rc"]["pos"]["i"]["x"].AsFloat, jn["rc"]["pos"]["i"]["y"].AsFloat);
                    if (jn["rc"]["pos"] != null && jn["rc"]["pos"]["s"] != null && jn["rc"]["pos"]["s"]["x"] != null && jn["rc"]["pos"]["s"]["y"] != null)
                        reactivePosSamples = new Vector2Int(jn["rc"]["pos"]["s"]["x"].AsInt, jn["rc"]["pos"]["s"]["y"].AsInt);

                    if (jn["rc"]["z"] != null && jn["rc"]["z"]["i"] != null)
                        reactiveZIntensity = jn["rc"]["z"]["i"].AsFloat;
                    if (jn["rc"]["z"] != null && jn["rc"]["z"]["s"] != null)
                        reactiveZSample = jn["rc"]["z"]["s"].AsInt;

                    if (jn["rc"]["sca"] != null && jn["rc"]["sca"]["i"] != null && jn["rc"]["sca"]["i"]["x"] != null && jn["rc"]["sca"]["i"]["y"] != null)
                        reactiveScaIntensity = new Vector2(jn["rc"]["sca"]["i"]["x"].AsFloat, jn["rc"]["sca"]["i"]["y"].AsFloat);
                    if (jn["rc"]["sca"] != null && jn["rc"]["sca"]["s"] != null && jn["rc"]["sca"]["s"]["x"] != null && jn["rc"]["sca"]["s"]["y"] != null)
                        reactiveScaSamples = new Vector2Int(jn["rc"]["sca"]["s"]["x"].AsInt, jn["rc"]["sca"]["s"]["y"].AsInt);

                    if (jn["rc"]["rot"] != null && jn["rc"]["rot"]["i"] != null)
                        reactiveRotIntensity = jn["rc"]["rot"]["i"].AsFloat;
                    if (jn["rc"]["rot"] != null && jn["rc"]["rot"]["s"] != null)
                        reactiveRotSample = jn["rc"]["rot"]["s"].AsInt;

                    if (jn["rc"]["col"] != null && jn["rc"]["col"]["i"] != null)
                        reactiveColIntensity = jn["rc"]["col"]["i"].AsFloat;
                    if (jn["rc"]["col"] != null && jn["rc"]["col"]["s"] != null)
                        reactiveColSample = jn["rc"]["col"]["s"].AsInt;
                    if (jn["rc"]["col"] != null && jn["rc"]["col"]["c"] != null)
                        reactiveCol = jn["rc"]["col"]["c"].AsInt;
                }
                catch (Exception ex)
                {
                    CoreHelper.Log($"Failed to load settings.\nEXCEPTION: {ex.Message}\nSTACKTRACE: {ex.StackTrace}");
                }
            }

            tags.Clear();
            if (jn["tags"] != null)
                for (int i = 0; i < jn["tags"].Count; i++)
                    tags.Add(jn["tags"][i]);

            if (jn["iglif"] != null)
                ignoreLifespan = jn["iglif"].AsBool;

            if (jn["ordmod"] != null)
                orderModifiers = jn["ordmod"].AsBool;

            modifiers.Clear();
            for (int i = 0; i < jn["modifiers"].Count; i++)
            {
                var modifierJN = jn["modifiers"][i];

                // this is for backwards compatibility due to modifiers having multiple lists previously.
                if (modifierJN.IsArray)
                {
                    orderModifiers = true;
                    var list = new List<Modifier<BackgroundObject>>();
                    for (int j = 0; j < jn["modifiers"][i].Count; j++)
                    {
                        var modifier = Modifier<BackgroundObject>.Parse(jn["modifiers"][i][j], this);
                        if (ModifiersHelper.VerifyModifier(modifier, ModifiersManager.defaultBackgroundObjectModifiers))
                            list.Add(modifier);
                    }
                    list.Sort((a, b) => a.type.CompareTo(b.type));
                    modifiers.AddRange(list);
                }
                else
                {
                    var modifier = Modifier<BackgroundObject>.Parse(jn["modifiers"][i], this);
                    if (ModifiersHelper.VerifyModifier(modifier, ModifiersManager.defaultBackgroundObjectModifiers))
                        modifiers.Add(modifier);
                }
            }

            if (jn["ed"] != null)
                editorData = ObjectEditorData.Parse(jn["ed"]);
        }

        public override JSONNode ToJSONVG()
        {
            var jn = Parser.NewJSONObject();

            jn["id"] = id;
            jn["c"] = color;

            if (pos.x != 0f || pos.y != 0f)
                jn["t"]["p"] = pos.ToJSON(false);
            if (scale.x != 0f || scale.y != 0f)
                jn["t"]["s"] = scale.ToJSON(false);
            if (rot != 0f)
                jn["t"]["r"] = rot;

            if (shape != 0)
                jn["s"]["s"] = shape;
            if (shapeOption != 0)
                jn["s"]["so"] = shapeOption;
            if (!string.IsNullOrEmpty(text))
                jn["s"]["t"] = text;

            return jn;
        }

        public override JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");

            jn["id"] = id;
            if (!string.IsNullOrEmpty(prefabID))
                jn["pid"] = prefabID;

            if (!string.IsNullOrEmpty(prefabInstanceID))
                jn["piid"] = prefabInstanceID;

            if (!active)
                jn["active"] = active;
            jn["name"] = name;

            if (StartTime != 0f)
                jn["st"] = StartTime;

            if (autoKillType != AutoKillType.NoAutokill)
                jn["akt"] = (int)autoKillType;
            if (autoKillOffset != 0f)
                jn["ako"] = autoKillOffset;

            if (tags != null && !tags.IsEmpty())
                for (int i = 0; i < tags.Count; i++)
                    jn["tags"][i] = tags[i];

            if (pos.x != 0f || pos.y != 0f)
                jn["pos"] = pos.ToJSON(false);

            if (scale.x != 0f || scale.y != 0f)
                jn["size"] = scale.ToJSON(false);

            if (rot != 0f)
                jn["rot"] = rot;

            if (color != 0)
                jn["col"]["slot"] = color;
            if (hue != 0f)
                jn["col"]["hue"] = hue;
            if (saturation != 0f)
                jn["col"]["sat"] = saturation;
            if (value != 0f)
                jn["col"]["val"] = value;

            if (fadeColor != 0)
                jn["fade_col"]["slot"] = fadeColor;
            if (fadeHue != 0f)
                jn["fade_col"]["hue"] = fadeHue;
            if (fadeSaturation != 0f)
                jn["fade_col"]["sat"] = fadeSaturation;
            if (fadeValue != 0f)
                jn["fade_col"]["val"] = fadeValue;

            if (depth != 0)
                jn["d"] = depth;

            if (!string.IsNullOrEmpty(layer))
                jn["l"] = layer;

            if (!drawFade)
                jn["fade"] = drawFade;

            if (zposition != 0f)
                jn["zpos"] = zposition;

            if (zscale != 10f)
                jn["zsca"] = zscale;

            if (iterations != 9)
                jn["iter"] = iterations;

            if (shape != 0)
                jn["s"] = shape;
            
            if (shapeOption != 0)
                jn["so"] = shapeOption;

            if (flat)
                jn["flat"] = flat;

            if (rotation.x != 0f || rotation.y != 0f)
            {
                jn["r_offset"]["x"] = rotation.x;
                jn["r_offset"]["y"] = rotation.y;
            }

            if (IsReactive)
            {
                jn["r_set"]["type"] = reactiveType.ToString();
                jn["r_set"]["scale"] = reactiveScale;

                if (reactiveType == ReactiveType.Custom)
                {
                    if (reactivePosIntensity.x != 0f || reactivePosIntensity.y != 0f)
                    {
                        jn["rc"]["pos"]["i"]["x"] = reactivePosIntensity.x;
                        jn["rc"]["pos"]["i"]["y"] = reactivePosIntensity.y;
                    }

                    if (reactivePosSamples.x != 0 || reactivePosSamples.y != 0)
                    {
                        jn["rc"]["pos"]["s"]["x"] = reactivePosSamples.x;
                        jn["rc"]["pos"]["s"]["y"] = reactivePosSamples.y;
                    }

                    if (reactiveZIntensity != 0f)
                        jn["rc"]["z"]["i"] = reactiveZIntensity;
                    if (reactiveZSample != 0)
                        jn["rc"]["z"]["s"] = reactiveZSample;

                    if (reactiveScaIntensity.x != 0f || reactiveScaIntensity.y != 0f)
                    {
                        jn["rc"]["sca"]["i"]["x"] = reactiveScaIntensity.x;
                        jn["rc"]["sca"]["i"]["y"] = reactiveScaIntensity.y;
                    }

                    if (reactiveScaSamples.x != 0 || reactiveScaSamples.y != 0)
                    {
                        jn["rc"]["sca"]["s"]["x"] = reactiveScaSamples.x;
                        jn["rc"]["sca"]["s"]["y"] = reactiveScaSamples.y;
                    }

                    if (reactiveRotIntensity != 0f)
                        jn["rc"]["rot"]["i"] = reactiveRotIntensity;
                    if (reactiveRotSample != 0)
                        jn["rc"]["rot"]["s"] = reactiveRotSample;

                    if (reactiveColIntensity != 0f)
                        jn["rc"]["col"]["i"] = reactiveColIntensity;
                    if (reactiveColSample != 0)
                        jn["rc"]["col"]["s"] = reactiveColSample;
                    if (reactiveCol != 0)
                        jn["rc"]["col"]["c"] = reactiveCol;
                }
            }

            if (ignoreLifespan)
                jn["iglif"] = ignoreLifespan;
            if (orderModifiers)
                jn["ordmod"] = orderModifiers;

            for (int i = 0; i < modifiers.Count; i++)
                jn["modifiers"][i] = modifiers[i].ToJSON();

            if (editorData.ShouldSerialize)
                jn["ed"] = editorData.ToJSON();

            return jn;
        }

        public float GetObjectLifeLength(float offset = 0f, bool noAutokill = false, bool collapse = false) => collapse && editorData.collapse ? 0.2f : autoKillType switch
        {
            AutoKillType.NoAutokill => noAutokill ? AudioManager.inst.CurrentAudioSource.clip.length - startTime : Length + offset,
            AutoKillType.LastKeyframe => Length + offset,
            AutoKillType.LastKeyframeOffset => Length + autoKillOffset + offset,
            AutoKillType.FixedTime => autoKillOffset,
            AutoKillType.SongTime => (startTime >= autoKillOffset) ? 0.1f : (autoKillOffset - startTime),
            _ => 0f,
        };

        public void ResetOffsets()
        {
            positionOffset = Vector3.zero;
            scaleOffset = Vector3.zero;
            rotationOffset = Vector3.zero;
        }

        public Vector3 GetTransformOffset(int type) => type switch
        {
            0 => positionOffset,
            1 => scaleOffset,
            _ => rotationOffset,
        };

        public void SetTransform(int type, Vector3 value)
        {
            switch (type)
            {
                case 0: {
                        positionOffset = value;
                        break;
                    }
                case 1: {
                        scaleOffset = value;
                        break;
                    }
                case 2: {
                        rotationOffset = value;
                        break;
                    }
            }
        }

        public void SetTransform(int type, int axis, float value)
        {
            switch (type)
            {
                case 0: {
                        positionOffset[axis] = value;
                        break;
                    }
                case 1: {
                        scaleOffset[axis] = value;
                        break;
                    }
                case 2: {
                        rotationOffset[axis] = value;
                        break;
                    }
            }
        }

        public IRTObject GetRuntimeObject() => runtimeObject;

        #region Evaluation

        public void SetOtherObjectVariables(Dictionary<string, float> variables)
        {
            //variables["otherIntVariable"] = integerVariable;

            variables["otherPositionOffsetX"] = positionOffset.x;
            variables["otherPositionOffsetY"] = positionOffset.y;
            variables["otherPositionOffsetZ"] = positionOffset.z;

            variables["otherScaleOffsetX"] = scaleOffset.x;
            variables["otherScaleOffsetY"] = scaleOffset.y;
            variables["otherScaleOffsetZ"] = scaleOffset.z;

            variables["otherRotationOffsetX"] = rotationOffset.x;
            variables["otherRotationOffsetY"] = rotationOffset.y;
            variables["otherRotationOffsetZ"] = rotationOffset.z;

            if (!runtimeObject)
                return;

            var gameObject = runtimeObject.BaseObject;

            if (!gameObject)
                return;

            var transform = gameObject.transform;

            variables["otherVisualPosX"] = transform.position.x;
            variables["otherVisualPosY"] = transform.position.y;
            variables["otherVisualPosZ"] = transform.position.z;

            variables["otherVisualScaX"] = transform.lossyScale.x;
            variables["otherVisualScaY"] = transform.lossyScale.y;
            variables["otherVisualScaZ"] = transform.lossyScale.z;

            variables["otherVisualRotX"] = transform.rotation.eulerAngles.x;
            variables["otherVisualRotY"] = transform.rotation.eulerAngles.y;
            variables["otherVisualRotZ"] = transform.rotation.eulerAngles.z;
        }

        public void SetObjectVariables(Dictionary<string, float> variables)
        {
            //variables["intVariable"] = integerVariable;

            variables["positionOffsetX"] = positionOffset.x;
            variables["positionOffsetY"] = positionOffset.y;
            variables["positionOffsetZ"] = positionOffset.z;

            variables["scaleOffsetX"] = scaleOffset.x;
            variables["scaleOffsetY"] = scaleOffset.y;
            variables["scaleOffsetZ"] = scaleOffset.z;

            variables["rotationOffsetX"] = rotationOffset.x;
            variables["rotationOffsetY"] = rotationOffset.y;
            variables["rotationOffsetZ"] = rotationOffset.z;

            if (!runtimeObject)
                return;

            var gameObject = runtimeObject.BaseObject;

            if (!gameObject)
                return;

            var transform = gameObject.transform;

            variables["visualPosX"] = transform.position.x;
            variables["visualPosY"] = transform.position.y;
            variables["visualPosZ"] = transform.position.z;

            variables["visualScaX"] = transform.lossyScale.x;
            variables["visualScaY"] = transform.lossyScale.y;
            variables["visualScaZ"] = transform.lossyScale.z;

            variables["visualRotX"] = transform.rotation.eulerAngles.x;
            variables["visualRotY"] = transform.rotation.eulerAngles.y;
            variables["visualRotZ"] = transform.rotation.eulerAngles.z;
        }

        public void SetObjectFunctions(Dictionary<string, MathFunction> functions) { }

        #endregion

        #endregion

        #region Operators

        public override bool Equals(object obj) => obj is BackgroundObject paObj && id == paObj.id;

        public override int GetHashCode() => base.GetHashCode();

        public override string ToString() => $"{id} - {name}";

        #endregion
    }
}
