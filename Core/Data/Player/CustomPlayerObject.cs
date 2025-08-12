using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core.Data.Beatmap;

namespace BetterLegacy.Core.Data.Player
{
    public class CustomPlayerObject : PAObject<CustomPlayerObject>, IPlayerObject, IShapeable, IModifyable
    {
        public CustomPlayerObject()
        {
            Trail = new PlayerTrail();
            Particles = new PlayerParticles();
        }

        #region Values

        #region Shape

        public int shape;

        public int shapeOption;

        public string text = string.Empty;

        public PolygonShape polygonShape = new PolygonShape();

        public int Shape { get => shape; set => shape = value; }

        public int ShapeOption { get => shapeOption; set => shapeOption = value; }

        public string Text { get => text; set => text = value; }

        public bool AutoTextAlign { get; set; }

        public PolygonShape Polygon { get => polygonShape; set => polygonShape = value; }

        public ShapeType ShapeType
        {
            get => (ShapeType)shape;
            set => shape = (int)value;
        }

        public bool IsSpecialShape => ShapeType == ShapeType.Text || ShapeType == ShapeType.Image || ShapeType == ShapeType.Polygon;

        #endregion

        #region Main

        public bool active = true;
        public bool Active { get => active; set => active = value; }

        public Vector2 position = Vector2.zero;
        public Vector2 Position { get => position; set => position = value; }

        public Vector2 scale = Vector2.one;
        public Vector2 Scale { get => scale; set => scale = value; }

        public float rotation;
        public float Rotation { get => rotation; set => rotation = value; }

        public int color = 23;
        public int Color { get => color; set => color = value; }

        public string customColor = RTColors.WHITE_HEX_CODE;
        public string CustomColor { get => customColor; set => customColor = value; }

        public float opacity = 1f;
        public float Opacity { get => opacity; set => opacity = value; }

        public float depth = 0.1f;
        public float Depth { get => depth; set => depth = value; }

        public PlayerTrail Trail { get; set; }
        public PlayerParticles Particles { get; set; }

        #endregion

        #region Custom

        public string name = string.Empty;

        public string customParent = string.Empty;
        public int parent;

        public float positionOffset = 1f;

        public float scaleOffset = 1f;

        public float rotationOffset = 1f;

        public bool scaleParent = true;

        public bool rotationParent = true;

        public bool requireAll;

        public List<Visibility> visibilitySettings = new List<Visibility>();

        public class Visibility : PAObject<Visibility>
        {
            public Visibility() { }

            #region Values

            public bool not;
            public string command = string.Empty;
            public float value;

            #endregion

            #region Methods

            public override void CopyData(Visibility orig, bool newID = true)
            {
                not = orig.not;
                command = orig.command;
                value = orig.value;
            }

            public override void ReadJSON(JSONNode jn)
            {
                command = jn["cmd"] ?? string.Empty;
                not = jn["not"].AsBool;
                value = jn["val"].AsFloat;
            }

            public override JSONNode ToJSON()
            {
                var jn = Parser.NewJSONObject();

                if (!string.IsNullOrEmpty(command))
                    jn["cmd"] = command;
                if (not)
                    jn["not"] = not.ToString();
                if (value != 0f)
                    jn["val"] = value.ToString();

                return jn;
            }

            #endregion
        }

        public List<PAAnimation> animations = new List<PAAnimation>();

        public ModifierReferenceType ReferenceType => ModifierReferenceType.PlayerObject;

        public List<string> Tags { get; set; } = new List<string>();

        public List<Modifier> Modifiers { get; set; } = new List<Modifier>();

        public bool IgnoreLifespan { get; set; }

        public bool OrderModifiers { get; set; } = true;

        public int IntVariable { get; set; }

        public bool ModifiersActive => false;

        #endregion

        #endregion

        #region Methods

        public override void CopyData(CustomPlayerObject orig, bool newID = true)
        {
            active = orig.active;
            this.CopyShapeableData(orig);
            position = orig.position;
            scale = orig.scale;
            rotation = orig.rotation;
            color = orig.color;
            customColor = orig.customColor;
            opacity = orig.opacity;
            depth = orig.depth;
            Trail = orig.Trail.Copy();
            Particles = orig.Particles.Copy();

            id = newID ? LSText.randomNumString(16) : orig.id;
            name = orig.name;
            parent = orig.parent;
            customParent = orig.customParent;
            positionOffset = orig.positionOffset;
            scaleOffset = orig.scaleOffset;
            rotationOffset = orig.rotationOffset;
            rotationParent = orig.rotationParent;
            scaleParent = orig.scaleParent;
            requireAll = orig.requireAll;
            visibilitySettings = orig.visibilitySettings.Select(x => x.Copy()).ToList();
            animations = orig.animations.Select(x => x.Copy()).ToList();

            this.CopyModifyableData(orig);
        }

        public override void ReadJSON(JSONNode jn)
        {
            if (jn == null)
                return;

            if (jn["active"] != null)
                active = jn["active"].AsBool;

            this.ReadShapeJSON(jn);

            if (jn["pos"]["x"] != null)
                position.x = jn["pos"]["x"].AsFloat;
            if (jn["pos"]["y"] != null)
                position.y = jn["pos"]["y"].AsFloat;

            if (jn["sca"]["x"] != null)
                scale.x = jn["sca"]["x"].AsFloat;
            if (jn["sca"]["y"] != null)
                scale.y = jn["sca"]["y"].AsFloat;

            if (jn["rot"]["x"] != null)
                rotation = jn["rot"]["x"].AsFloat;

            if (jn["col"]["x"] != null)
                color = jn["col"]["x"].AsInt;

            if (jn["col"]["hex"] != null)
                customColor = jn["col"]["hex"];

            if (jn["opa"]["x"] != null)
                opacity = jn["opa"]["x"].AsFloat;

            if (jn["d"] != null)
                depth = jn["d"].AsFloat;

            Trail.ReadJSON(jn["trail"]);
            Particles.ReadJSON(jn["particles"]);

            if (!string.IsNullOrEmpty(jn["id"]))
                id = jn["id"];

            if (!string.IsNullOrEmpty(jn["n"]))
                name = jn["n"];

            parent = jn["p"].AsInt;
            customParent = jn["idp"] ?? string.Empty;

            if (jn["ppo"] != null)
                positionOffset = jn["ppo"].AsFloat;

            if (jn["pso"] != null)
                scaleOffset = jn["pso"].AsFloat;

            if (jn["pro"] != null)
                rotationOffset = jn["pro"].AsFloat;

            if (jn["psa"] != null)
                scaleParent = jn["psa"].AsBool;

            if (jn["pra"] != null)
                rotationParent = jn["pra"].AsBool;

            int visible = -1;
            if (jn["v"] != null)
                visible = jn["v"].AsInt;

            bool not = false;
            if (jn["vn"] != null)
                not = jn["vn"].AsBool;

            float visibleValue = 0f;
            if (jn["vhp"] != null)
                visibleValue = jn["vhp"].AsFloat;

            switch (visible)
            {
                case 0: {
                        active = true;
                        break;
                    } // always
                case 1: {
                        visibilitySettings.Add(new Visibility
                        {
                            command = "isBoosting",
                            not = not,
                            value = visibleValue
                        });
                        break;
                    } // isBoosting
                case 2: {
                        visibilitySettings.Add(new Visibility
                        {
                            command = "isTakingHit",
                            not = not,
                            value = visibleValue
                        });
                        break;
                    } // isTakingHit
                case 3: {
                        visibilitySettings.Add(new Visibility
                        {
                            command = "isZenMode",
                            not = not,
                            value = visibleValue
                        });
                        break;
                    } // isZenMode
                case 4: {
                        visibilitySettings.Add(new Visibility
                        {
                            command = "isHealthPercentageGreaterEquals",
                            not = not,
                            value = visibleValue
                        });
                        break;
                    } // isHealthPercentageGreater
                case 5: {
                        visibilitySettings.Add(new Visibility
                        {
                            command = "isHealthGreaterEquals",
                            not = not,
                            value = visibleValue
                        });
                        break;
                    } // isHealthGreaterEquals
                case 6: {
                        visibilitySettings.Add(new Visibility
                        {
                            command = "isHealthEquals",
                            not = not,
                            value = visibleValue
                        });
                        break;
                    } // isHealthEquals
                case 7: {
                        visibilitySettings.Add(new Visibility
                        {
                            command = "isHealthGreater",
                            not = not,
                            value = visibleValue
                        });
                        break;
                    } // isHealthGreater
                case 8: {
                        visibilitySettings.Add(new Visibility
                        {
                            command = "isPressingKey",
                            not = not,
                            value = visibleValue
                        });
                        break;
                    } // isPressingKey
            }

            if (jn["req_all"] != null)
                requireAll = jn["req_all"].AsBool;

            visibilitySettings.Clear();
            for (int i = 0; i < jn["visible"].Count; i++)
                visibilitySettings.Add(Visibility.Parse(jn["visible"][i]));

            if (jn["anims"] != null)
            {
                for (int i = 0; i < jn["anims"].Count; i++)
                    animations.Add(PAAnimation.Parse(jn["anims"][i]));
            }

            this.ReadModifiersJSON(jn);
            if (!Modifiers.IsEmpty())
                this.UpdateFunctions();
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            this.WriteShapeJSON(jn);

            jn["active"] = active;

            if (position.x != 0f || position.y != 0f)
            {
                jn["pos"]["x"] = position.x;
                jn["pos"]["y"] = position.y;
            }

            if (scale.x != 1f || scale.y != 1f)
            {
                jn["sca"]["x"] = scale.x;
                jn["sca"]["y"] = scale.y;
            }

            if (rotation != 0f)
                jn["rot"]["x"] = rotation;

            if (color != 23)
                jn["col"]["x"] = color;

            if (color == 24 && customColor != "FFFFFF" && !string.IsNullOrEmpty(customColor))
                jn["col"]["hex"] = customColor;

            if (opacity != 1f)
                jn["opa"]["x"] = opacity;

            if (depth != 0.1f)
                jn["d"] = depth;

            if (Trail && Trail.ShouldSerialize)
                jn["trail"] = Trail.ToJSON();
            if (Particles && Particles.ShouldSerialize)
                jn["particles"] = Particles.ToJSON();

            jn["id"] = id;
            if (!string.IsNullOrEmpty(name))
                jn["n"] = name;
            if (parent != 0)
                jn["p"] = parent;
            if (!string.IsNullOrEmpty(customParent))
                jn["idp"] = customParent;
            if (positionOffset != 1f)
                jn["ppo"] = positionOffset;
            if (scaleOffset != 1f)
                jn["pso"] = scaleOffset;
            if (rotationOffset != 1f)
                jn["pro"] = rotationOffset;
            if (!scaleParent)
                jn["psa"] = scaleParent;
            if (!rotationParent)
                jn["pra"] = rotationParent;

            if (requireAll)
                jn["req_all"] = requireAll;

            for (int i = 0; i < visibilitySettings.Count; i++)
                jn["visible"][i] = visibilitySettings[i].ToJSON();

            for (int i = 0; i < animations.Count; i++)
                jn["anims"][i] = animations[i].ToJSON();

            this.WriteModifiersJSON(jn);

            return jn;
        }

        public void SetCustomShape(int shape, int shapeOption) { }

        #endregion
    }

}
