using System;
using System.Collections.Generic;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class BackgroundObject : Exists
    {
        public BackgroundObject() { }

        public void SetShape(int shape, int shapeOption)
        {
            this.shape = shape;
            this.shapeOption = shapeOption;
            UpdateShape();
        }

        public void UpdateShape()
        {
            var paShape = ShapeManager.inst.GetShape3D(shape, shapeOption);
            foreach (var gameObject in gameObjects)
            {
                if (gameObject.TryGetComponent(out MeshFilter meshFilter) && paShape.mesh)
                    meshFilter.mesh = paShape.mesh;
            }
        }

        #region Values

        public string id = LSText.randomString(16);
        public bool active = true;
        public string name = "Background";

        // todo: change background objects to be in the main editor timeline and have the same start time system as beatmap objects.
        public ObjectEditorData editorData;

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

        public List<List<Modifier<BackgroundObject>>> modifiers = new List<List<Modifier<BackgroundObject>>>();

        /// <summary>
        /// If the order of triggers and actions matter.
        /// </summary>
        public bool orderModifiers = false;

        public Vector3 positionOffset;
        public Vector3 scaleOffset;
        public Vector3 rotationOffset;

        #endregion

        #region References

        public bool Enabled { get; set; } = true;

        public GameObject BaseObject => !gameObjects.IsEmpty() ? gameObjects[0] : null;

        public List<GameObject> gameObjects = new List<GameObject>();
        public List<Transform> transforms = new List<Transform>();
        public List<Renderer> renderers = new List<Renderer>();

        #endregion

        #endregion

        #region Methods

        public static BackgroundObject DeepCopy(BackgroundObject orig)
        {
            var backgroundObject = new BackgroundObject()
            {
                id = orig.id,
                active = orig.active,
                drawFade = orig.drawFade,
                depth = orig.depth,
                name = orig.name,
                pos = orig.pos,
                reactiveScale = orig.reactiveScale,
                reactiveSize = orig.reactiveSize,
                reactiveType = orig.reactiveType,
                rot = orig.rot,
                scale = orig.scale,
                text = orig.text,
                iterations = orig.iterations,
                shape = orig.shape,
                shapeOption = orig.shapeOption,
                flat = orig.flat,
                zposition = orig.zposition,
                zscale = orig.zscale,
                rotation = orig.rotation,

                color = orig.color,
                hue = orig.hue,
                saturation = orig.saturation,
                value = orig.value,

                fadeColor = orig.fadeColor,
                fadeHue = orig.fadeHue,
                fadeSaturation = orig.fadeSaturation,
                fadeValue = orig.fadeValue,

                reactiveCol = orig.reactiveCol,
                reactiveColSample = orig.reactiveColSample,
                reactiveColIntensity = orig.reactiveColIntensity,
                reactivePosSamples = orig.reactivePosSamples,
                reactivePosIntensity = orig.reactivePosIntensity,
                reactiveRotSample = orig.reactiveRotSample,
                reactiveRotIntensity = orig.reactiveRotIntensity,
                reactiveScaSamples = orig.reactiveScaSamples,
                reactiveScaIntensity = orig.reactiveScaIntensity,
                reactiveZIntensity = orig.reactiveZIntensity,
                reactiveZSample = orig.reactiveZSample,

                tags = orig.tags.Clone(),
                orderModifiers = orig.orderModifiers,
            };

            for (int i = 0; i < orig.modifiers.Count; i++)
            {
                backgroundObject.modifiers.Add(new List<Modifier<BackgroundObject>>());
                for (int j = 0; j < orig.modifiers[i].Count; j++)
                {
                    var modifier = Modifier<BackgroundObject>.DeepCopy(orig.modifiers[i][j], backgroundObject);
                    backgroundObject.modifiers[i].Add(modifier);
                }
            }

            return backgroundObject;
        }

        public static BackgroundObject Parse(JSONNode jn)
        {
            var backgroundObject = new BackgroundObject();

            if (jn["id"] != null)
                backgroundObject.id = jn["id"];

            if (jn["active"] != null)
                backgroundObject.active = jn["active"].AsBool;

            if (jn["name"] != null)
                backgroundObject.name = jn["name"];

            if (jn["text"] != null)
                backgroundObject.text = jn["text"];

            backgroundObject.pos = new Vector2(jn["pos"]["x"].AsFloat, jn["pos"]["y"].AsFloat);
            backgroundObject.scale = new Vector2(jn["size"]["x"].AsFloat, jn["size"]["y"].AsFloat);

            backgroundObject.rot = jn["rot"].AsFloat;
            backgroundObject.color = jn["color"].AsInt;
            if (jn["layer"] != null)
                backgroundObject.depth = jn["layer"].AsInt;
            else
                backgroundObject.depth = jn["depth"].AsInt;

            var reactiveType = jn["r_set"]["type"];
            if (reactiveType != null)
            {
                if (reactiveType == "LOW")
                    backgroundObject.reactiveType = ReactiveType.Bass;
                else if (reactiveType == "MID")
                    backgroundObject.reactiveType = ReactiveType.Mids;
                else if (reactiveType == "HIGH")
                    backgroundObject.reactiveType = ReactiveType.Treble;
                else
                    backgroundObject.reactiveType = Parser.TryParse(jn["r_set"]["type"], true, ReactiveType.None);
            }

            if (jn["r_set"]["scale"] != null)
                backgroundObject.reactiveScale = jn["r_set"]["scale"].AsFloat;

            if (jn["fade"] != null)
                backgroundObject.drawFade = jn["fade"].AsBool;

            if (jn["hue"] != null)
                backgroundObject.hue = jn["sat"].AsFloat;
            if (jn["sat"] != null)
                backgroundObject.saturation = jn["sat"].AsFloat;
            if (jn["val"] != null)
                backgroundObject.value = jn["val"].AsFloat;

            if (jn["fade_hue"] != null)
                backgroundObject.fadeHue = jn["fade_sat"].AsFloat;
            if (jn["fade_sat"] != null)
                backgroundObject.fadeSaturation = jn["fade_sat"].AsFloat;
            if (jn["fade_val"] != null)
                backgroundObject.fadeValue = jn["fade_val"].AsFloat;

            if (jn["zposition"] != null)
                backgroundObject.zposition = jn["zposition"].AsFloat;
            
            if (jn["zpos"] != null)
                backgroundObject.zposition = jn["zpos"].AsFloat;

            if (jn["zscale"] != null)
                backgroundObject.zscale = jn["zscale"].AsFloat;
            
            if (jn["zsca"] != null)
                backgroundObject.zscale = jn["zsca"].AsFloat;

            if (jn["depth"] != null)
                backgroundObject.iterations = jn["depth"].AsInt;
            
            if (jn["iter"] != null)
                backgroundObject.iterations = jn["iter"].AsInt;

            if (jn["s"] != null)
                backgroundObject.shape = jn["s"].AsInt;
            
            if (jn["so"] != null)
                backgroundObject.shapeOption = jn["so"].AsInt;

            if (jn["r_offset"] != null && jn["r_offset"]["x"] != null && jn["r_offset"]["y"] != null)
                backgroundObject.rotation = new Vector2(jn["r_offset"]["x"].AsFloat, jn["r_offset"]["y"].AsFloat);

            if (jn["color_fade"] != null)
                backgroundObject.fadeColor = jn["color_fade"].AsInt;

            if (jn["rc"] != null)
            {
                try
                {
                    if (jn["rc"]["pos"] != null && jn["rc"]["pos"]["i"] != null && jn["rc"]["pos"]["i"]["x"] != null && jn["rc"]["pos"]["i"]["y"] != null)
                        backgroundObject.reactivePosIntensity = new Vector2(jn["rc"]["pos"]["i"]["x"].AsFloat, jn["rc"]["pos"]["i"]["y"].AsFloat);
                    if (jn["rc"]["pos"] != null && jn["rc"]["pos"]["s"] != null && jn["rc"]["pos"]["s"]["x"] != null && jn["rc"]["pos"]["s"]["y"] != null)
                        backgroundObject.reactivePosSamples = new Vector2Int(jn["rc"]["pos"]["s"]["x"].AsInt, jn["rc"]["pos"]["s"]["y"].AsInt);

                    if (jn["rc"]["z"] != null && jn["rc"]["z"]["i"] != null)
                        backgroundObject.reactiveZIntensity = jn["rc"]["z"]["i"].AsFloat;
                    if (jn["rc"]["z"] != null && jn["rc"]["z"]["s"] != null)
                        backgroundObject.reactiveZSample = jn["rc"]["z"]["s"].AsInt;

                    if (jn["rc"]["sca"] != null && jn["rc"]["sca"]["i"] != null && jn["rc"]["sca"]["i"]["x"] != null && jn["rc"]["sca"]["i"]["y"] != null)
                        backgroundObject.reactiveScaIntensity = new Vector2(jn["rc"]["sca"]["i"]["x"].AsFloat, jn["rc"]["sca"]["i"]["y"].AsFloat);
                    if (jn["rc"]["sca"] != null && jn["rc"]["sca"]["s"] != null && jn["rc"]["sca"]["s"]["x"] != null && jn["rc"]["sca"]["s"]["y"] != null)
                        backgroundObject.reactiveScaSamples = new Vector2Int(jn["rc"]["sca"]["s"]["x"].AsInt, jn["rc"]["sca"]["s"]["y"].AsInt);

                    if (jn["rc"]["rot"] != null && jn["rc"]["rot"]["i"] != null)
                        backgroundObject.reactiveRotIntensity = jn["rc"]["rot"]["i"].AsFloat;
                    if (jn["rc"]["rot"] != null && jn["rc"]["rot"]["s"] != null)
                        backgroundObject.reactiveRotSample = jn["rc"]["rot"]["s"].AsInt;

                    if (jn["rc"]["col"] != null && jn["rc"]["col"]["i"] != null)
                        backgroundObject.reactiveColIntensity = jn["rc"]["col"]["i"].AsFloat;
                    if (jn["rc"]["col"] != null && jn["rc"]["col"]["s"] != null)
                        backgroundObject.reactiveColSample = jn["rc"]["col"]["s"].AsInt;
                    if (jn["rc"]["col"] != null && jn["rc"]["col"]["c"] != null)
                        backgroundObject.reactiveCol = jn["rc"]["col"]["c"].AsInt;
                }
                catch (Exception ex)
                {
                    CoreHelper.Log($"Failed to load settings.\nEXCEPTION: {ex.Message}\nSTACKTRACE: {ex.StackTrace}");
                }
            }

            if (jn["tags"] != null)
                for (int i = 0; i < jn["tags"].Count; i++)
                    backgroundObject.tags.Add(jn["tags"][i]);

            if (jn["ordmod"] != null)
                backgroundObject.orderModifiers = jn["ordmod"].AsBool;

            for (int i = 0; i < jn["modifiers"].Count; i++)
            {
                backgroundObject.modifiers.Add(new List<Modifier<BackgroundObject>>());
                for (int j = 0; j < jn["modifiers"][i].Count; j++)
                {
                    var modifier = Modifier<BackgroundObject>.Parse(jn["modifiers"][i][j], backgroundObject);
                    if (ModifiersHelper.VerifyModifier(modifier, ModifiersManager.defaultBackgroundObjectModifiers))
                        backgroundObject.modifiers[i].Add(modifier);
                }
            }

            if (jn["ed"] != null)
                backgroundObject.editorData = ObjectEditorData.Parse(jn["ed"]);

            return backgroundObject;
        }

        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");

            jn["id"] = id;

            if (!active)
                jn["active"] = active;
            jn["name"] = name;

            if (tags != null && !tags.IsEmpty())
                for (int i = 0; i < tags.Count; i++)
                    jn["tags"][i] = tags[i];

            if (pos.x != 0f || pos.y != 0f)
            {
                jn["pos"]["x"] = pos.x;
                jn["pos"]["y"] = pos.y;
            }

            if (scale.x != 0f || scale.y != 0f)
            {
                jn["size"]["x"] = scale.x;
                jn["size"]["y"] = scale.y;
            }

            if (rot != 0f)
                jn["rot"] = rot;

            if (color != 0)
                jn["color"] = color;

            if (hue != 0f)
                jn["hue"] = hue;
            if (saturation != 0f)
                jn["sat"] = saturation;
            if (value != 0f)
                jn["val"] = value;
            if (fadeHue != 0f)
                jn["fade_hue"] = fadeHue;
            if (fadeSaturation != 0f)
                jn["fade_sat"] = fadeSaturation;
            if (fadeValue != 0f)
                jn["fade_val"] = fadeValue;

            if (depth != 0)
                jn["depth"] = depth;

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

            if (fadeColor != 0)
                jn["color_fade"] = fadeColor;

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

            if (orderModifiers)
                jn["ordmod"] = orderModifiers;

            for (int i = 0; i < modifiers.Count; i++)
                for (int j = 0; j < modifiers[i].Count; j++)
                    jn["modifiers"][i][j] = modifiers[i][j].ToJSON();

            if (editorData)
                jn["ed"] = editorData.ToJSON();

            return jn;
        }

        public void SetTransform(int toType, Vector3 value)
        {
            switch (toType)
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

        public void SetTransform(int toType, int toAxis, float value)
        {
            switch (toType)
            {
                case 0: {
                        if (toAxis == 0)
                            positionOffset.x = value;
                        if (toAxis == 1)
                            positionOffset.y = value;
                        if (toAxis == 2)
                            positionOffset.z = value;

                        break;
                    }
                case 1: {
                        if (toAxis == 0)
                            scaleOffset.x = value;
                        if (toAxis == 1)
                            scaleOffset.y = value;
                        if (toAxis == 2)
                            scaleOffset.z = value;

                        break;
                    }
                case 2: {
                        if (toAxis == 0)
                            rotationOffset.x = value;
                        if (toAxis == 1)
                            rotationOffset.y = value;
                        if (toAxis == 2)
                            rotationOffset.z = value;

                        break;
                    }
            }
        }

        public void ResetOffsets()
        {
            positionOffset = Vector3.zero;
            scaleOffset = Vector3.zero;
            rotationOffset = Vector3.zero;
        }

        #endregion

        #region Operators

        public override string ToString() => name;

        #endregion
    }
}
