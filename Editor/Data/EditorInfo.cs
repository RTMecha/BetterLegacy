using BetterLegacy.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SimpleJSON;
using BetterLegacy.Editor.Managers;
using BetterLegacy.Configs;

namespace BetterLegacy.Editor.Data
{
    public class EditorInfo : Exists
    {
        #region Usage

        /// <summary>
        /// The total time the user has been editing a level.
        /// </summary>
        public RTTimer timer;

        /// <summary>
        /// The time that was saved.
        /// </summary>
        public float savedTimeEditng;

        /// <summary>
        /// The amount of times the user has opened the level.
        /// </summary>
        public int openAmount;

        #endregion

        #region Positions

        /// <summary>
        /// Zoom of the main timeline.
        /// </summary>
        public float mainZoom = 0.05f;

        /// <summary>
        /// Position of the main timeline;
        /// </summary>
        public float mainPosition;

        /// <summary>
        /// Total bin count.
        /// </summary>
        public int binCount = EditorTimeline.DEFAULT_BIN_COUNT;

        /// <summary>
        /// Bin scroll position.
        /// </summary>
        public float binPosition;

        #endregion

        #region Layers

        /// <summary>
        /// Layer of the main timeline.
        /// </summary>
        public int layer;

        /// <summary>
        /// Layer type of the main timeline.
        /// </summary>
        public EditorTimeline.LayerType layerType;

        /// <summary>
        /// Pinned editor layers.
        /// </summary>
        public List<PinnedEditorLayer> pinnedEditorLayers = new List<PinnedEditorLayer>();

        #endregion

        #region BPM

        public bool bpmSnapActive;

        public float bpm = 140f;

        /// <summary>
        /// Offset to the song BPM. Good for cases where the song starts at an offset.
        /// </summary>
        public float bpmOffset = 0f;

        #endregion

        /// <summary>
        /// Audio time.
        /// </summary>
        public float time;

        /// <summary>
        /// Path to load prefabs from.
        /// </summary>
        public string prefabPath;

        /// <summary>
        /// Path to load themes from.
        /// </summary>
        public string themePath;

        /// <summary>
        /// Parses an <see cref="EditorInfo"/> from JSON.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns a parsed <see cref="EditorInfo"/>.</returns>
        public static EditorInfo Parse(JSONNode jn)
        {
            var editorInfo = new EditorInfo();

            if (jn["paths"] != null)
            {
                editorInfo.prefabPath = jn["paths"]["prefab"];
                editorInfo.themePath = jn["paths"]["theme"];
            }

            if (jn["timeline"] != null)
            {
                var layer = 0;
                var layerType = EditorTimeline.LayerType.Objects;

                float zoom = 0.05f;
                float position = 0f;

                if (jn["timeline"]["z"] != null)
                    zoom = jn["timeline"]["z"].AsFloat;

                if (jn["timeline"]["tsc"] != null)
                    position = jn["timeline"]["tsc"].AsFloat;

                if (jn["timeline"]["zoom"] != null)
                    zoom = jn["timeline"]["zoom"].AsFloat;

                if (jn["timeline"]["position"] != null)
                    position = jn["timeline"]["position"].AsFloat;

                if (jn["timeline"]["layer_type"] != null)
                    layerType = (EditorTimeline.LayerType)jn["timeline"]["layer_type"].AsInt;

                if (jn["timeline"]["l"] != null)
                    layer = jn["timeline"]["l"].AsInt;

                if (jn["timeline"]["layer"] != null)
                    layer = jn["timeline"]["layer"].AsInt;

                editorInfo.mainZoom = zoom;
                editorInfo.mainPosition = position;

                editorInfo.layer = layer;
                editorInfo.layerType = layerType;

                editorInfo.binCount = jn["timeline"]["bin_count"] != null ? jn["timeline"]["bin_count"].AsInt : EditorTimeline.DEFAULT_BIN_COUNT;
                editorInfo.binPosition = jn["timeline"]["bin_position"].AsFloat;

                if (jn["timeline"]["pinned_layers"] != null)
                    for (int i = 0; i < jn["timeline"]["pinned_layers"].Count; i++)
                        editorInfo.pinnedEditorLayers.Add(PinnedEditorLayer.Parse(jn["timeline"]["pinned_layers"][i]));
            }

            if (jn["editor"] != null)
            {
                if (jn["editor"]["t"] != null)
                    editorInfo.timer.offset = jn["editor"]["t"].AsFloat;
                if (jn["editor"]["editing_time"] != null)
                    editorInfo.timer.offset = jn["editor"]["editing_time"].AsFloat;

                if (jn["editor"]["a"] != null)
                    editorInfo.openAmount = jn["editor"]["a"].AsInt + 1;
                if (jn["editor"]["open_amount"] != null)
                    editorInfo.openAmount = jn["editor"]["open_amount"].AsInt + 1;
            }

            if (jn["misc"] != null)
            {
                if (jn["misc"]["sn"] != null)
                    editorInfo.bpmSnapActive = jn["misc"]["sn"].AsBool;
                if (jn["misc"]["bpm_snap_active"] != null)
                    editorInfo.bpmSnapActive = jn["misc"]["bpm_snap_active"].AsBool;

                var bpm = 140f;
                if (jn["misc"]["bpm"] != null)
                    bpm = jn["misc"]["bpm"].AsFloat;
                editorInfo.bpm = bpm;
                
                var bpmOffset = 0f;
                if (jn["misc"]["so"] != null)
                    bpmOffset = jn["misc"]["so"].AsFloat;
                if (jn["misc"]["bpm_offset"] != null)
                    bpmOffset = jn["misc"]["bpm_offset"].AsFloat;
                editorInfo.bpmOffset = bpmOffset;

                float time = -1f;
                if (jn["misc"]["t"] != null)
                    time = jn["misc"]["t"].AsFloat;
                if (jn["misc"]["time"] != null)
                    time = jn["misc"]["time"].AsFloat;

                editorInfo.time = time;
            }

            return editorInfo;
        }

        /// <summary>
        /// Converts the editor info into a JSON object.
        /// </summary>
        /// <returns>Returns a JSON object representing the editor info.</returns>
        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");

            if (!string.IsNullOrEmpty(prefabPath))
                jn["paths"]["prefab"] = prefabPath;
            if (!string.IsNullOrEmpty(themePath))
                jn["paths"]["theme"] = themePath;

            jn["timeline"]["zoom"] = EditorManager.inst.zoomFloat.ToString("f3");
            jn["timeline"]["position"] = EditorManager.inst.timelineScrollRectBar.value.ToString("f2");
            jn["timeline"]["layer_type"] = ((int)EditorTimeline.inst.layerType).ToString();
            jn["timeline"]["layer"] = EditorTimeline.inst.Layer.ToString();
            jn["timeline"]["bin_count"] = EditorTimeline.inst.BinCount.ToString();
            jn["timeline"]["bin_position"] = EditorTimeline.inst.binSlider.value.ToString();

            for (int i = 0; i < EditorTimeline.inst.pinnedEditorLayers.Count; i++)
                jn["timeline"]["pinned_layers"][i] = EditorTimeline.inst.pinnedEditorLayers[i].ToJSON();

            jn["editor"]["editing_time"] = timer.time.ToString();
            jn["editor"]["open_amount"] = openAmount.ToString();
            jn["misc"]["bpm_snap_active"] = bpmSnapActive.ToString();
            jn["misc"]["bpm"] = bpm.ToString();
            jn["misc"]["bpm_offset"] = bpmOffset.ToString();
            jn["misc"]["time"] = AudioManager.inst.CurrentAudioSource.time.ToString();

            return jn;
        }

        /// <summary>
        /// Applies the editor info to the editor.
        /// </summary>
        public void ApplyTo()
        {
            EditorTimeline.inst.BinCount = binCount;
            EditorTimeline.inst.binSlider.value = binPosition;

            EditorTimeline.inst.SetLayer(layer, layerType, false);
            EditorTimeline.inst.SetTimeline(mainZoom, mainPosition);

            EditorTimeline.inst.pinnedEditorLayers.Clear();
            EditorTimeline.inst.pinnedEditorLayers.AddRange(pinnedEditorLayers.Select(x => PinnedEditorLayer.DeepCopy(x)));

            SettingEditor.inst.SnapActive = bpmSnapActive;
            SettingEditor.inst.SnapBPM = bpm;

            if (time >= 0f && time < AudioManager.inst.CurrentAudioSource.clip.length && EditorConfig.Instance.LevelLoadsLastTime.Value)
                AudioManager.inst.SetMusicTime(time);

            if (!string.IsNullOrEmpty(prefabPath) && RTEditor.PrefabPath != prefabPath)
            {
                RTEditor.inst.prefabPathField.text = prefabPath;
                RTEditor.inst.UpdatePrefabPath(false);
            }

            if (!string.IsNullOrEmpty(themePath) && RTEditor.ThemePath != themePath)
            {
                RTEditor.inst.themePathField.text = themePath;
                RTEditor.inst.UpdateThemePath(false);
            }
        }

        /// <summary>
        /// Takes info from the editor and applies it to itself.
        /// </summary>
        public void ApplyFrom()
        {
            binCount = EditorTimeline.inst.BinCount;
            binPosition = EditorTimeline.inst.binSlider.value;

            layer = EditorTimeline.inst.Layer;
            layerType = EditorTimeline.inst.layerType;

            pinnedEditorLayers.Clear();
            pinnedEditorLayers.AddRange(EditorTimeline.inst.pinnedEditorLayers.Select(x => PinnedEditorLayer.DeepCopy(x)));

            bpmSnapActive = SettingEditor.inst.SnapActive;
            bpm = MetaData.Current.song.BPM;

            time = AudioManager.inst.CurrentAudioSource.time;

            // don't apply paths since it's just an override.
        }
    }
}
