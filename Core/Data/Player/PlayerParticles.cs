using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core.Data.Beatmap;

namespace BetterLegacy.Core.Data.Player
{
    public class PlayerParticles : PAObject<PlayerParticles>, IShapeable
    {
        public PlayerParticles() { }

        #region Values

        public bool ShouldSerialize =>
            emitting ||
            this.ShouldSerializeShape() ||
            color != 23 ||
            !string.IsNullOrEmpty(customColor) && customColor != RTColors.WHITE_HEX_CODE ||
            startOpacity != 1f ||
            endOpacity != 0f ||
            startScale != 1f ||
            endScale != 0f ||
            rotation != 0f ||
            lifeTime != 5f ||
            speed != 5f ||
            amount != 5f ||
            force != Vector2.zero ||
            trailEmitting;

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

        public bool emitting = false;

        public int color = 23;

        public string customColor = RTColors.WHITE_HEX_CODE;

        public float startOpacity = 1f;

        public float endOpacity = 0f;

        public float startScale = 1f;

        public float endScale = 0f;

        public float rotation = 0f;

        public float lifeTime = 5f;

        public float speed = 5f;

        public float amount = 5f;

        public Vector2 force = Vector2.zero;

        public bool trailEmitting = false;

        #endregion

        #endregion

        #region Methods

        public override void CopyData(PlayerParticles orig, bool newID = true)
        {
            emitting = orig.emitting;
            this.CopyShapeableData(orig);
            color = orig.color;
            customColor = orig.customColor;
            startOpacity = orig.startOpacity;
            endOpacity = orig.endOpacity;
            startScale = orig.startScale;
            endScale = orig.endScale;
            rotation = orig.rotation;
            lifeTime = orig.lifeTime;
            speed = orig.speed;
            force = orig.force;
            trailEmitting = orig.trailEmitting;
            amount = orig.amount;
        }

        public override void ReadJSON(JSONNode jn)
        {
            if (jn == null)
                return;

            emitting = jn["em"].AsBool;

            this.ReadShapeJSON(jn);

            if (jn["col"] != null)
                color = jn["col"].AsInt;

            if (jn["colhex"] != null)
                customColor = jn["colhex"];

            if (jn["opa"]["start"] != null)
                startOpacity = jn["opa"]["start"].AsFloat;

            if (jn["opa"]["end"] != null)
                endOpacity = jn["opa"]["end"].AsFloat;

            if (jn["sca"]["start"] != null)
                startScale = jn["sca"]["start"].AsFloat;

            if (jn["sca"]["end"] != null)
                endScale = jn["sca"]["end"].AsFloat;

            if (jn["rot"] != null)
                rotation = jn["rot"].AsFloat;

            if (jn["lt"] != null)
                lifeTime = jn["lt"].AsFloat;

            if (jn["sp"] != null)
                speed = jn["sp"].AsFloat;

            if (jn["am"] != null)
                amount = jn["am"].AsFloat;

            if (jn["frc"]["x"] != null)
                force.x = jn["frc"]["x"].AsFloat;
            if (jn["frc"]["y"] != null)
                force.y = jn["frc"]["y"].AsFloat;

            if (jn["trem"] != null)
                trailEmitting = jn["trem"].AsBool;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            if (emitting)
                jn["em"] = emitting;

            this.WriteShapeJSON(jn);

            if (color != 23)
                jn["col"] = color;
            if (!string.IsNullOrEmpty(customColor) && customColor != RTColors.WHITE_HEX_CODE)
                jn["colhex"] = customColor;

            if (startOpacity != 1f)
                jn["opa"]["start"] = startOpacity;
            if (endOpacity != 0f)
                jn["opa"]["end"] = endOpacity;

            if (startScale != 1f)
                jn["sca"]["start"] = startScale;
            if (endScale != 0f)
                jn["sca"]["end"] = endScale;

            if (rotation != 0f)
                jn["rot"] = rotation;
            if (lifeTime != 5f)
                jn["lt"] = lifeTime;
            if (speed != 5f)
                jn["sp"] = speed;
            if (amount != 5f)
                jn["am"] = amount;

            if (force.x != 0f || force.y != 0f)
            {
                jn["frc"]["x"] = force.x;
                jn["frc"]["y"] = force.y;
            }

            if (trailEmitting)
                jn["trem"] = trailEmitting;

            return jn;
        }

        public void SetCustomShape(int shape, int shapeOption) { }

        #endregion
    }
}
