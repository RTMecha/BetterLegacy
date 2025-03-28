﻿using System;
using System.Collections.Generic;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;

using BaseBackground = DataManager.GameData.BackgroundObject;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class BackgroundObject : BaseBackground
    {
        public BackgroundObject()
        {
            try
            {
                shape = ShapeManager.inst.Shapes3D[0][0];
            }
            catch
            {
                shape = new Shape("", 0, 0, null, null, Shape.Property.RegularObject);
            }
        }

        public void SetShape(int shape, int shapeOption)
        {
            this.shape = Shape.DeepCopy(ShapeManager.inst.GetShape3D(shape, shapeOption));
            foreach (var gameObject in gameObjects)
            {
                if (gameObject.TryGetComponent(out MeshFilter meshFilter) && this.shape.mesh)
                    meshFilter.mesh = this.shape.mesh;
            }
        }

        #region Values

        #region Transforms

        public Vector2 rotation = Vector2.zero;
        public Shape shape;
        public float zscale = 10f;
        public int depth = 9;
        public float zposition;

        #endregion

        #region Reactive

        public Vector2Int reactivePosSamples;
        public Vector2Int reactiveScaSamples;
        public int reactiveRotSample;
        public int reactiveColSample;
        public int reactiveCol;

        public Vector2 reactivePosIntensity;
        public Vector2 reactiveScaIntensity;
        public float reactiveRotIntensity;
        public float reactiveColIntensity;

        public bool reactiveIncludesZ;
        public float reactiveZIntensity;
        public int reactiveZSample;

        #endregion

        #region Colors

        public float hue;
        public float saturation;
        public float value;

        public float fadeHue;
        public float fadeSaturation;
        public float fadeValue;

        int fadeColor;
        public int FadeColor
        {
            get => Mathf.Clamp(fadeColor, 0, 8);
            set => fadeColor = Mathf.Clamp(value, 0, 8);
        }

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

        public static BackgroundObject DeepCopy(BackgroundObject bg)
        {
            var b = new BackgroundObject()
            {
                active = bg.active,
                color = bg.color,
                FadeColor = bg.FadeColor,
                drawFade = bg.drawFade,
                kind = bg.kind,
                layer = bg.layer,
                name = bg.name,
                pos = bg.pos,
                reactive = bg.reactive,
                reactiveScale = bg.reactiveScale,
                reactiveSize = bg.reactiveSize,
                reactiveType = bg.reactiveType,
                rot = bg.rot,
                scale = bg.scale,
                text = bg.text,
                depth = bg.depth,
                shape = bg.shape,
                zposition = bg.zposition,
                zscale = bg.zscale,
                rotation = bg.rotation,
                hue = bg.hue,
                saturation = bg.saturation,
                value = bg.value,
                fadeHue = bg.fadeHue,
                fadeSaturation = bg.fadeSaturation,
                fadeValue = bg.fadeValue,

                reactiveCol = bg.reactiveCol,
                reactiveColSample = bg.reactiveColSample,
                reactiveColIntensity = bg.reactiveColIntensity,
                reactivePosSamples = bg.reactivePosSamples,
                reactivePosIntensity = bg.reactivePosIntensity,
                reactiveRotSample = bg.reactiveRotSample,
                reactiveRotIntensity = bg.reactiveRotIntensity,
                reactiveScaSamples = bg.reactiveScaSamples,
                reactiveScaIntensity = bg.reactiveScaIntensity,
                reactiveZIntensity = bg.reactiveZIntensity,
                reactiveZSample = bg.reactiveZSample,

                tags = bg.tags.Clone(),
                orderModifiers = bg.orderModifiers,
            };

            for (int i = 0; i < bg.modifiers.Count; i++)
            {
                b.modifiers.Add(new List<Modifier<BackgroundObject>>());
                for (int j = 0; j < bg.modifiers[i].Count; j++)
                {
                    var modifier = Modifier<BackgroundObject>.DeepCopy(bg.modifiers[i][j], b);
                    b.modifiers[i].Add(modifier);
                }
            }

            return b;
        }

        public static BackgroundObject Parse(JSONNode jn)
        {
            var active = true;
            if (jn["active"] != null)
                active = jn["active"].AsBool;

            string name;
            if (jn["name"] != null)
                name = jn["name"];
            else
                name = "Background";

            int kind;
            if (jn["kind"] != null)
                kind = jn["kind"].AsInt;
            else
                kind = 1;

            string text;
            if (jn["text"] != null)
                text = jn["text"];
            else
                text = "";

            var pos = new Vector2(jn["pos"]["x"].AsFloat, jn["pos"]["y"].AsFloat);
            var scale = new Vector2(jn["size"]["x"].AsFloat, jn["size"]["y"].AsFloat);

            var rot = jn["rot"].AsFloat;
            var color = jn["color"].AsInt;
            var layer = jn["layer"].AsInt;

            var reactive = false;
            if (jn["r_set"] != null)
                reactive = true;

            if (jn["r_set"]["active"] != null)
                reactive = jn["r_set"]["active"].AsBool;

            var reactiveType = ReactiveType.LOW;
            if (jn["r_set"]["type"] != null)
                reactiveType = (ReactiveType)Enum.Parse(typeof(ReactiveType), jn["r_set"]["type"]);

            float reactiveScale = 1f;
            if (jn["r_set"]["scale"] != null)
                reactiveScale = jn["r_set"]["scale"].AsFloat;

            bool drawFade = true;
            if (jn["fade"] != null)
                drawFade = jn["fade"].AsBool;

            #region New stuff

            float hue = 0f;
            float sat = 0f;
            float val = 0f;
            if (jn["hue"] != null)
                hue = jn["sat"].AsFloat;
            if (jn["sat"] != null)
                sat = jn["sat"].AsFloat;
            if (jn["val"] != null)
                val = jn["val"].AsFloat;

            float fadeHue = 0f;
            float fadeSat = 0f;
            float fadeVal = 0f;
            if (jn["fade_hue"] != null)
                fadeHue = jn["fade_sat"].AsFloat;
            if (jn["fade_sat"] != null)
                fadeSat = jn["fade_sat"].AsFloat;
            if (jn["fade_val"] != null)
                fadeVal = jn["fade_val"].AsFloat;

            float zposition = 0f;
            if (jn["zposition"] != null)
                zposition = jn["zposition"].AsFloat;

            float zscale = 10f;
            if (jn["zscale"] != null)
                zscale = jn["zscale"].AsFloat;

            int depth = 9;
            if (jn["depth"] != null)
                depth = jn["depth"].AsInt;

            Shape shape = ShapeManager.inst.Shapes3D[0][0];
            if (jn["s"] != null && jn["so"] != null)
                shape = ShapeManager.inst.GetShape3D(jn["s"].AsInt, jn["so"].AsInt);

            Vector2 rotation = Vector2.zero;
            if (jn["r_offset"] != null && jn["r_offset"]["x"] != null && jn["r_offset"]["y"] != null)
                rotation = new Vector2(jn["r_offset"]["x"].AsFloat, jn["r_offset"]["y"].AsFloat);

            int fadeColor = 0;
            if (jn["color_fade"] != null)
                fadeColor = jn["color_fade"].AsInt;

            var reactivePosIntensity = Vector2.zero;
            var reactivePosSamples = Vector2Int.zero;
            var reactiveZIntensity = 0f;
            var reactiveZSample = 0;
            var reactiveScaIntensity = Vector2.zero;
            var reactiveScaSamples = Vector2Int.zero;
            var reactiveRotIntensity = 0f;
            var reactiveRotSample = 0;
            var reactiveColIntensity = 0f;
            var reactiveColSample = 0;
            var reactiveCol = 0;

            if (jn["rc"] != null)
            {
                try
                {
                    if (jn["rc"]["pos"] != null && jn["rc"]["pos"]["i"] != null && jn["rc"]["pos"]["i"]["x"] != null && jn["rc"]["pos"]["i"]["y"] != null)
                        reactivePosIntensity = new Vector2(jn["rc"]["pos"]["i"]["x"].AsFloat, jn["rc"]["pos"]["i"]["y"].AsFloat);
                    if (jn["rc"]["pos"] != null && jn["rc"]["pos"]["s"] != null && jn["rc"]["pos"]["s"]["x"] != null && jn["rc"]["pos"]["s"]["y"] != null)
                        reactivePosSamples = new Vector2Int(jn["rc"]["pos"]["s"]["x"].AsInt, jn["rc"]["pos"]["s"]["y"].AsInt);

                    //if (jn["rc"]["z"] != null && jn["rc"]["active"] != null)
                    //	bg.reactiveIncludesZ = jn["rc"]["z"]["active"].AsBool;

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

            #endregion

            var bg = new BackgroundObject
            {
                active = active,
                name = name,
                kind = kind,
                text = text,
                pos = pos,
                scale = scale,
                rot = rot,
                color = color,
                layer = layer,
                reactive = reactive,
                reactiveType = reactiveType,
                reactiveScale = reactiveScale,
                drawFade = drawFade,

                hue = hue,
                saturation = sat,
                value = val,
                fadeHue = fadeHue,
                fadeSaturation = fadeSat,
                fadeValue = fadeVal,

                zposition = zposition,
                zscale = zscale,
                depth = depth,
                shape = shape,
                rotation = rotation,
                FadeColor = fadeColor,
                reactivePosIntensity = reactivePosIntensity,
                reactivePosSamples = reactivePosSamples,
                reactiveZIntensity = reactiveZIntensity,
                reactiveZSample = reactiveZSample,
                reactiveScaIntensity = reactiveScaIntensity,
                reactiveScaSamples = reactiveScaSamples,
                reactiveRotIntensity = reactiveRotIntensity,
                reactiveRotSample = reactiveRotSample,
                reactiveColIntensity = reactiveColIntensity,
                reactiveColSample = reactiveColSample,
                reactiveCol = reactiveCol,
            };

            if (jn["tags"] != null)
                for (int i = 0; i < jn["tags"].Count; i++)
                    bg.tags.Add(jn["tags"][i]);

            if (jn["ordmod"] != null)
                bg.orderModifiers = jn["ordmod"].AsBool;

            for (int i = 0; i < jn["modifiers"].Count; i++)
            {
                bg.modifiers.Add(new List<Modifier<BackgroundObject>>());
                for (int j = 0; j < jn["modifiers"][i].Count; j++)
                {
                    var modifier = Modifier<BackgroundObject>.Parse(jn["modifiers"][i][j], bg);
                    if (ModifiersHelper.VerifyModifier(modifier, ModifiersManager.defaultBackgroundObjectModifiers))
                        bg.modifiers[i].Add(modifier);
                }
            }

            return bg;
        }

        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");

            if (!active)
                jn["active"] = active.ToString();
            jn["name"] = name.ToString();

            if (tags != null && !tags.IsEmpty())
                for (int i = 0; i < tags.Count; i++)
                    jn["tags"][i] = tags[i];

            //jn["kind"] = kind.ToString(); // Unused so no need to save to JSON. Luckily Legacy should handle this being missing.
            jn["pos"]["x"] = pos.x.ToString();
            jn["pos"]["y"] = pos.y.ToString();
            jn["size"]["x"] = scale.x.ToString();
            jn["size"]["y"] = scale.y.ToString();
            jn["rot"] = rot.ToString();
            jn["color"] = color.ToString();
            if (hue != 0f)
                jn["hue"] = hue.ToString();
            if (saturation != 0f)
                jn["sat"] = saturation.ToString();
            if (value != 0f)
                jn["val"] = value.ToString();
            if (fadeHue != 0f)
                jn["fade_hue"] = fadeHue.ToString();
            if (fadeSaturation != 0f)
                jn["fade_sat"] = fadeSaturation.ToString();
            if (fadeValue != 0f)
                jn["fade_val"] = fadeValue.ToString();

            jn["layer"] = layer.ToString();

            if (!drawFade)
                jn["fade"] = drawFade.ToString();

            if (zposition != 0f)
                jn["zposition"] = zposition.ToString();

            if (zscale != 10f)
                jn["zscale"] = zscale.ToString();

            if (depth != 9)
                jn["depth"] = depth.ToString();

            if (shape.Type != 0 || shape.Option != 0)
            {
                jn["s"] = shape.Type.ToString();
                jn["so"] = shape.Option.ToString();
            }

            if (FadeColor != 0)
                jn["color_fade"] = FadeColor.ToString();

            if (rotation.x != 0f || rotation.y != 0f)
            {
                jn["r_offset"]["x"] = rotation.x.ToString();
                jn["r_offset"]["y"] = rotation.y.ToString();
            }

            if (reactivePosIntensity.x != 0f || reactivePosIntensity.y != 0f)
            {
                jn["rc"]["pos"]["i"]["x"] = reactivePosIntensity.x.ToString();
                jn["rc"]["pos"]["i"]["y"] = reactivePosIntensity.y.ToString();
            }

            if (reactivePosSamples.x != 0 || reactivePosSamples.y != 0)
            {
                jn["rc"]["pos"]["s"]["x"] = reactivePosSamples.x.ToString();
                jn["rc"]["pos"]["s"]["y"] = reactivePosSamples.y.ToString();
            }

            if (reactiveZIntensity != 0f)
                jn["rc"]["z"]["i"] = reactiveZIntensity.ToString();
            if (reactiveZSample != 0)
                jn["rc"]["z"]["s"] = reactiveZSample.ToString();

            if (reactiveScaIntensity.x != 0f || reactiveScaIntensity.y != 0f)
            {
                jn["rc"]["sca"]["i"]["x"] = reactiveScaIntensity.x.ToString();
                jn["rc"]["sca"]["i"]["y"] = reactiveScaIntensity.y.ToString();
            }
            
            if (reactiveScaSamples.x != 0 || reactiveScaSamples.y != 0)
            {
                jn["rc"]["sca"]["s"]["x"] = reactiveScaSamples.x.ToString();
                jn["rc"]["sca"]["s"]["y"] = reactiveScaSamples.y.ToString();
            }

            if (reactiveRotIntensity != 0f)
                jn["rc"]["rot"]["i"] = reactiveRotIntensity.ToString();
            if (reactiveRotSample != 0)
                jn["rc"]["rot"]["s"] = reactiveRotSample.ToString();

            if (reactiveColIntensity != 0f)
                jn["rc"]["col"]["i"] = reactiveColIntensity.ToString();
            if (reactiveColSample != 0)
                jn["rc"]["col"]["s"] = reactiveColSample.ToString();
            if (reactiveCol != 0)
                jn["rc"]["col"]["c"] = reactiveCol.ToString();

            if (reactive)
            {
                jn["r_set"]["type"] = reactiveType.ToString();
                jn["r_set"]["scale"] = reactiveScale.ToString();
            }

            if (orderModifiers)
                jn["ordmod"] = orderModifiers.ToString();

            for (int i = 0; i < modifiers.Count; i++)
                for (int j = 0; j < modifiers[i].Count; j++)
                    jn["modifiers"][i][j] = modifiers[i][j].ToJSON();

            return jn;
        }

        public void SetTransform(int toType, Vector3 value)
        {
            switch (toType)
            {
                case 0:
                    {
                        positionOffset = value;
                        break;
                    }
                case 1:
                    {
                        scaleOffset = value;
                        break;
                    }
                case 2:
                    {
                        rotationOffset = value;
                        break;
                    }
            }
        }

        public void SetTransform(int toType, int toAxis, float value)
        {
            switch (toType)
            {
                case 0:
                    {
                        if (toAxis == 0)
                            positionOffset.x = value;
                        if (toAxis == 1)
                            positionOffset.y = value;
                        if (toAxis == 2)
                            positionOffset.z = value;

                        break;
                    }
                case 1:
                    {
                        if (toAxis == 0)
                            scaleOffset.x = value;
                        if (toAxis == 1)
                            scaleOffset.y = value;
                        if (toAxis == 2)
                            scaleOffset.z = value;

                        break;
                    }
                case 2:
                    {
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

        public static implicit operator bool(BackgroundObject exists) => exists != null;

        public override string ToString() => name;

        #endregion
    }
}
