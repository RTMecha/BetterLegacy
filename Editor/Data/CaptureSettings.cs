using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Network;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Represents settings for the Capture Area.
    /// </summary>
    public class CaptureSettings : PAObject<CaptureSettings>, IPacket
    {
        #region Constructors

        public CaptureSettings() : base() { }

        public CaptureSettings(Vector2Int resolution, Vector2 pos, float rot) : this()
        {
            this.resolution = resolution;
            this.pos = pos;
            this.rot = rot;
        }

        #endregion

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

        /// <summary>
        /// If all render layers should be captured.
        /// </summary>
        public bool captureAllLayers;

        /// <summary>
        /// If the precise editor display should show.
        /// </summary>
        public bool showEditor;

        /// <summary>
        /// If a custom background color should be used.
        /// </summary>
        public bool useCustomBGColor;

        /// <summary>
        /// The custom background color.
        /// </summary>
        public Color customBGColor;

        /// <summary>
        /// Specific value that should be locked when dragging.
        /// </summary>
        public LockDragMode lockDragMode;

        /// <summary>
        /// Specific value that should be locked when dragging.
        /// </summary>
        public enum LockDragMode
        {
            /// <summary>
            /// No lock.
            /// </summary>
            None,
            /// <summary>
            /// Locks position X.
            /// </summary>
            PositionX,
            /// <summary>
            /// Locks position Y.
            /// </summary>
            PositionY,
        }

        #endregion

        #region Functions

        public override void CopyData(CaptureSettings orig, bool newID = true)
        {
            resolution = orig.resolution;
            move = orig.move;
            pos = orig.pos;
            zoom = orig.zoom;
            rot = orig.rot;
            matchSize = orig.matchSize;

            hidePlayers = orig.hidePlayers;
            showEditor = orig.showEditor;
            captureAllLayers = orig.captureAllLayers;

            useCustomBGColor = orig.useCustomBGColor;
            customBGColor = orig.customBGColor;

            lockDragMode = orig.lockDragMode;
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

            if (jn["hide_players"] != null)
                hidePlayers = jn["hide_players"].AsBool;
            showEditor = jn["show_editor"].AsBool;
            captureAllLayers = jn["capture_all_layers"].AsBool;

            useCustomBGColor = jn["use_custom_bg_color"].AsBool;
            if (jn["custom_bg_color"] != null)
                customBGColor = RTColors.HexToColor(jn["custom_bg_color"]);

            if (jn["lock_drag_mode"] != null)
                lockDragMode = (LockDragMode)jn["lock_drag_mode"].AsInt;
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
            if (showEditor)
                jn["show_editor"] = showEditor;
            if (captureAllLayers)
                jn["capture_all_layers"] = captureAllLayers;

            if (useCustomBGColor)
                jn["use_custom_bg_color"] = useCustomBGColor;
            if (customBGColor != Color.white)
                jn["custom_bg_color"] = RTColors.ColorToHex(customBGColor);

            if (lockDragMode != LockDragMode.None)
                jn["lock_drag_mode"] = (int)lockDragMode;

            return jn;
        }

        public void ReadPacket(NetworkReader reader)
        {
            resolution = reader.ReadVector2Int();
            move = reader.ReadBoolean();
            pos = reader.ReadVector2();
            zoom = reader.ReadSingle();
            rot = reader.ReadSingle();
            matchSize = reader.ReadBoolean();

            hidePlayers = reader.ReadBoolean();
            showEditor = reader.ReadBoolean();
            captureAllLayers = reader.ReadBoolean();

            useCustomBGColor = reader.ReadBoolean();
            customBGColor = reader.ReadColor();

            lockDragMode = (LockDragMode)reader.ReadByte();
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(resolution);
            writer.Write(move);
            writer.Write(pos);
            writer.Write(zoom);
            writer.Write(rot);
            writer.Write(matchSize);

            writer.Write(hidePlayers);
            writer.Write(showEditor);
            writer.Write(captureAllLayers);

            writer.Write(useCustomBGColor);
            writer.Write(customBGColor);

            writer.Write((byte)lockDragMode);
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
            matchSize = false;
            hidePlayers = true;
            captureAllLayers = false;
            useCustomBGColor = false;
            customBGColor = Color.white;
            lockDragMode = LockDragMode.None;
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
