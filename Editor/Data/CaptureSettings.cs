using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Represents settings for the Capture Area.
    /// </summary>
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

        Vector2Int resolution = new Vector2Int(512, 512);
        /// <summary>
        /// Resolution of the capture.
        /// </summary>
        public Vector2Int Resolution
        {
            get => resolution;
            set
            {
                SetResolutionWidth(value.x);
                SetResolutionHeight(value.y);
            }
        }

        /// <summary>
        /// If the camera should be moved when the capture is created.
        /// </summary>
        public bool move = true;

        /// <summary>
        /// Position of the capture.
        /// </summary>
        public Vector2 pos;

        float zoom = 1f;
        /// <summary>
        /// Zoom of the capture.
        /// </summary>
        public float Zoom
        {
            get => zoom;
            set => zoom = Mathf.Clamp(value, 0.1f, float.MaxValue);
        }

        /// <summary>
        /// Rotation of the capture.
        /// </summary>
        public float rot;

        /// <summary>
        /// If resolution height and weight should match.
        /// </summary>
        public bool matchSize;

        /// <summary>
        /// If players should be hidden from the capture.
        /// </summary>
        public bool hidePlayers = true;

        #endregion

        #region Methods

        public override void CopyData(CaptureSettings orig, bool newID = true)
        {
            resolution = orig.resolution;
            move = orig.move;
            pos = orig.pos;
            zoom = orig.zoom;
            rot = orig.rot;
            matchSize = orig.matchSize;
            hidePlayers = orig.hidePlayers;
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
            hidePlayers = jn["hide_players"].AsBool;
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
            if (hidePlayers)
                jn["hide_players"] = hidePlayers;

            return jn;
        }

        /// <summary>
        /// Resets the capture settings.
        /// </summary>
        public void Reset()
        {
            resolution = new Vector2Int(512, 512);
            move = true;
            pos = Vector2.zero;
            zoom = 1f;
            rot = 0f;
        }

        /// <summary>
        /// Sets the resolution's width.
        /// </summary>
        /// <param name="x">Width value.</param>
        public void SetResolutionWidth(int width) => resolution.x = RTMath.Clamp(width, 32, int.MaxValue);

        /// <summary>
        /// Sets the resolution's height.
        /// </summary>
        /// <param name="y">Height value.</param>
        public void SetResolutionHeight(int height) => resolution.y = RTMath.Clamp(height, 32, int.MaxValue);

        #endregion
    }
}
