using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core.Data.Network;

namespace BetterLegacy.Core.Data.Player
{
    public class PlayerTrail : PAObject<PlayerTrail>, IPacket
    {
        public PlayerTrail() { }

        #region Values

        public override bool ShouldSerialize =>
            emitting ||
            time != 1f ||
            startWidth != 1f ||
            endWidth != 1f ||
            startColor != 23 ||
            !string.IsNullOrEmpty(startCustomColor) && startCustomColor != RTColors.WHITE_HEX_CODE ||
            startOpacity != 1f ||
            endColor != 23 ||
            !string.IsNullOrEmpty(endCustomColor) && endCustomColor != RTColors.WHITE_HEX_CODE ||
            endOpacity != 0f ||
            positionOffset != Vector2.zero;

        /// <summary>
        /// If the trail is emitting.
        /// </summary>
        public bool emitting;

        /// <summary>
        /// Delay time of the trail.
        /// </summary>
        public float time = 1f;

        /// <summary>
        /// Position offset of the trail.
        /// </summary>
        public Vector2 positionOffset = Vector2.zero;

        #region Start

        /// <summary>
        /// Width the trail starts at.
        /// </summary>
        public float startWidth = 1f;

        /// <summary>
        /// Color the trail starts at.
        /// </summary>
        public int startColor = 23;

        /// <summary>
        /// Custom color the trail starts at.
        /// </summary>
        public string startCustomColor = RTColors.WHITE_HEX_CODE;

        /// <summary>
        /// Opacity the trail starts at.
        /// </summary>
        public float startOpacity = 1f;

        #endregion

        #region End

        /// <summary>
        /// Width the trail ends at.
        /// </summary>
        public float endWidth = 1f;

        /// <summary>
        /// Color the trail ends at.
        /// </summary>
        public int endColor = 23;

        /// <summary>
        /// Custom color the trail ends at.
        /// </summary>
        public string endCustomColor = RTColors.WHITE_HEX_CODE;

        /// <summary>
        /// Opacity the trail ends at.
        /// </summary>
        public float endOpacity = 0f;

        #endregion

        #endregion

        #region Functions

        public override void CopyData(PlayerTrail orig, bool newID = true)
        {
            emitting = orig.emitting;
            time = orig.time;
            positionOffset = orig.positionOffset;

            // start
            startWidth = orig.startWidth;
            startColor = orig.startColor;
            startCustomColor = orig.startCustomColor;
            startOpacity = orig.startOpacity;

            // end
            endWidth = orig.endWidth;
            endColor = orig.endColor;
            endCustomColor = orig.endCustomColor;
            endOpacity = orig.endOpacity;
        }

        public override void ReadJSON(JSONNode jn)
        {
            if (jn == null)
                return;

            if (jn["em"] != null)
                emitting = jn["em"].AsBool;

            if (jn["t"] != null)
                time = jn["t"].AsFloat;

            if (jn["w"]["start"] != null)
                startWidth = jn["w"]["start"].AsFloat;

            if (jn["w"]["end"] != null)
                endWidth = jn["w"]["end"].AsFloat;

            if (jn["c"]["start"] != null)
                startColor = jn["c"]["start"].AsInt;

            if (jn["c"]["end"] != null)
                endColor = jn["c"]["end"].AsInt;

            if (jn["c"]["starthex"] != null)
                startCustomColor = jn["c"]["starthex"];

            if (jn["c"]["endhex"] != null)
                endCustomColor = jn["c"]["starthex"];

            if (jn["o"]["start"] != null)
                startOpacity = jn["o"]["start"].AsFloat;

            if (jn["o"]["end"] != null)
                endOpacity = jn["o"]["end"].AsFloat;

            if (jn["pos"]["x"] != null)
                positionOffset.x = jn["pos"]["x"].AsFloat;
            if (jn["pos"]["y"] != null)
                positionOffset.y = jn["pos"]["y"].AsFloat;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            if (emitting)
                jn["em"] = emitting;
            if (time != 1f)
                jn["t"] = time;
            if (startWidth != 1f)
                jn["w"]["start"] = startWidth;
            if (endWidth != 1f)
                jn["w"]["end"] = endWidth;
            if (startColor != 23)
                jn["c"]["start"] = startColor;
            if (endColor != 23)
                jn["c"]["end"] = endColor;
            if (!string.IsNullOrEmpty(startCustomColor) && startCustomColor != RTColors.WHITE_HEX_CODE)
                jn["c"]["starthex"] = startCustomColor;
            if (!string.IsNullOrEmpty(endCustomColor) && endCustomColor != RTColors.WHITE_HEX_CODE)
                jn["c"]["endhex"] = endCustomColor;
            if (startOpacity != 1f)
                jn["o"]["start"] = startOpacity;
            if (endOpacity != 0f)
                jn["o"]["end"] = endOpacity;
            if (positionOffset.x != 0f || positionOffset.y != 0f)
            {
                jn["pos"]["x"] = positionOffset.x;
                jn["pos"]["y"] = positionOffset.y;
            }

            return jn;
        }

        public void ReadPacket(NetworkReader reader)
        {
            emitting = reader.ReadBoolean();
            time = reader.ReadSingle();
            positionOffset = reader.ReadVector2();

            // start
            startWidth = reader.ReadSingle();
            startColor = reader.ReadInt32();
            startCustomColor = reader.ReadString();
            startOpacity = reader.ReadSingle();

            // end
            endWidth = reader.ReadSingle();
            endColor = reader.ReadInt32();
            endCustomColor = reader.ReadString();
            endOpacity = reader.ReadSingle();
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(emitting);
            writer.Write(time);
            writer.Write(positionOffset);

            // start
            writer.Write(startWidth);
            writer.Write(startColor);
            writer.Write(startCustomColor);
            writer.Write(startOpacity);

            // end
            writer.Write(endWidth);
            writer.Write(endColor);
            writer.Write(endCustomColor);
            writer.Write(endOpacity);
        }

        #endregion
    }

}
