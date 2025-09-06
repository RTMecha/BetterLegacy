using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;

namespace BetterLegacy.Editor.Data
{
    public class CaptureSettings : PAObject<CaptureSettings>
    {
        public CaptureSettings() : base() { }

        public CaptureSettings(Vector2Int resolution, Vector2 pos, float rot) : this()
        {
            this.resolution = resolution;
            this.pos = pos;
            this.rot = rot;
        }

        #region Values

        public Vector2Int resolution = new Vector2Int(512, 512);
        public bool move = true;
        public Vector2 pos;
        public float rot;

        #endregion

        #region Methods

        public override void CopyData(CaptureSettings orig, bool newID = true)
        {
            resolution = orig.resolution;
            move = orig.move;
            pos = orig.pos;
            rot = orig.rot;
        }

        public override void ReadJSON(JSONNode jn)
        {
            if (jn == null)
                return;

            resolution = Parser.TryParse(jn["resolution"], new Vector2Int(512, 512));
            if (jn["move"] != null)
                move = jn["move"].AsBool;
            pos = Parser.TryParse(jn["pos"], Vector2.zero);
            rot = jn["rot"].AsFloat;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["resolution"] = resolution.ToJSON();
            if (!move)
                jn["move"] = move;
            if (pos.x != 0f || pos.y != 0f)
                jn["pos"] = pos.ToJSON();
            if (rot != 0f)
                jn["rot"] = rot;

            return jn;
        }

        #endregion
    }
}
