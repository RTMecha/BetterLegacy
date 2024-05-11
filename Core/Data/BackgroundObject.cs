using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using SimpleJSON;
using System;
using System.Collections.Generic;
using UnityEngine;
using BaseBackground = DataManager.GameData.BackgroundObject;

namespace BetterLegacy.Core.Data
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

        public BackgroundObject(BaseBackground bg)
        {
            active = bg.active;
            color = bg.color;
            drawFade = bg.drawFade;
            kind = bg.kind;
            layer = bg.layer;
            name = bg.name;
            pos = bg.pos;
            reactive = bg.reactive;
            reactiveScale = bg.reactiveScale;
            reactiveSize = bg.reactiveSize;
            reactiveType = bg.reactiveType;
            rot = bg.rot;
            scale = bg.scale;
            text = bg.text;

            shape = ShapeManager.inst.Shapes3D[0][0];
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

        public GameObject BaseObject => gameObjects.Count > 0 ? gameObjects[0] : null;

        public List<GameObject> gameObjects = new List<GameObject>();
        public List<Transform> transforms = new List<Transform>();
        public List<Renderer> renderers = new List<Renderer>();

        public Vector2Int reactivePosSamples;
        public Vector2Int reactiveScaSamples;
        public int reactiveRotSample;
        public int reactiveColSample;
        public int reactiveCol;

        public Vector2 reactivePosIntensity;
        public Vector2 reactiveScaIntensity;
        public float reactiveRotIntensity;
        public float reactiveColIntensity;

        public Vector2 rotation = Vector2.zero;
        public Shape shape;
        public float zscale = 10f;
        public int depth = 9;
        public float zposition;

        int fadeColor;
        public int FadeColor
        {
            get => Mathf.Clamp(fadeColor, 0, 8);
            set => fadeColor = Mathf.Clamp(value, 0, 8);
        }

        public bool reactiveIncludesZ;
        public float reactiveZIntensity;
        public int reactiveZSample;

        public Vector3 positionOffset;
        public Vector3 scaleOffset;
        public Vector3 rotationOffset;

        public List<List<Modifier<BackgroundObject>>> modifiers = new List<List<Modifier<BackgroundObject>>>();

        public bool Enabled { get; set; } = true;

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
                zscale = bg.zscale,
                rotation = bg.rotation,

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

            for (int i = 0; i < jn["modifiers"].Count; i++)
            {
                bg.modifiers.Add(new List<Modifier<BackgroundObject>>());
                for (int j = 0; j < jn["modifiers"][i].Count; j++)
                {
                    var modifier = Modifier<BackgroundObject>.Parse(jn["modifiers"][i][j], bg);
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
            //jn["kind"] = kind.ToString(); // Unused so no need to save to JSON. Luckily Legacy should handle this being missing.
            jn["pos"]["x"] = pos.x.ToString();
            jn["pos"]["y"] = pos.y.ToString();
            jn["size"]["x"] = scale.x.ToString();
            jn["size"]["y"] = scale.y.ToString();
            jn["rot"] = rot.ToString();
            jn["color"] = color.ToString();
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

            for (int i = 0; i < modifiers.Count; i++)
                for (int j = 0; j < modifiers[i].Count; j++)
                    jn["modifiers"][i][j] = modifiers[i][j].ToJSON();

            return jn;
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
