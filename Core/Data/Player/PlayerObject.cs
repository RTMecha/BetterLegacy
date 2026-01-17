using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Network;

namespace BetterLegacy.Core.Data.Player
{
    public class PlayerObject : PAObject<PlayerObject>, IPacket, IPlayerObject, IShapeable
    {
        public PlayerObject()
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

        #endregion

        #region Functions

        public override void CopyData(PlayerObject orig, bool newID = true)
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
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["active"] = active;

            this.WriteShapeJSON(jn);

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

            return jn;
        }

        public void ReadPacket(NetworkReader reader)
        {
            #region Interface

            this.ReadShapePacket(reader);

            #endregion

            active = reader.ReadBoolean();
            position = reader.ReadVector2();
            scale = reader.ReadVector2();
            rotation = reader.ReadSingle();
            depth = reader.ReadSingle();

            color = reader.ReadInt32();
            customColor = reader.ReadString();
            opacity = reader.ReadSingle();

            var hasTrail = reader.ReadBoolean();
            if (hasTrail)
                Trail = Packet.CreateFromPacket<PlayerTrail>(reader);
            var hasParticles = reader.ReadBoolean();
            if (hasParticles)
                Particles = Packet.CreateFromPacket<PlayerParticles>(reader);
        }

        public void WritePacket(NetworkWriter writer)
        {
            #region Interface

            this.WriteShapePacket(writer);

            #endregion

            writer.Write(active);
            writer.Write(position);
            writer.Write(scale);
            writer.Write(rotation);
            writer.Write(depth);

            writer.Write(color);
            writer.Write(customColor);
            writer.Write(opacity);

            bool hasTrail = Trail;
            writer.Write(hasTrail);
            if (hasTrail)
                Trail.WritePacket(writer);
            bool hasParticles = Particles;
            writer.Write(hasParticles);
            if (hasParticles)
                Particles.WritePacket(writer);
        }

        public void SetCustomShape(int shape, int shapeOption) { }

        #endregion
    }
}
