using UnityEngine;

using SimpleJSON;

namespace BetterLegacy.Core.Data.Player
{
    public class PlayerTrail : PAObject<PlayerTrail>
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

        public bool emitting;

        public float time = 1f;

        public float startWidth = 1f;

        public float endWidth = 1f;

        public int startColor = 23;

        public string startCustomColor = RTColors.WHITE_HEX_CODE;

        public float startOpacity = 1f;

        public int endColor = 23;

        public string endCustomColor = RTColors.WHITE_HEX_CODE;

        public float endOpacity = 0f;

        public Vector2 positionOffset = Vector2.zero;

        #endregion

        #region Methods

        public override void CopyData(PlayerTrail orig, bool newID = true)
        {
            emitting = orig.emitting;
            time = orig.time;
            startWidth = orig.startWidth;
            endWidth = orig.endWidth;
            startColor = orig.startColor;
            endColor = orig.endColor;
            startCustomColor = orig.startCustomColor;
            endCustomColor = orig.endCustomColor;
            startOpacity = orig.startOpacity;
            endOpacity = orig.endOpacity;
            positionOffset = orig.positionOffset;
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

        #endregion
    }

}
