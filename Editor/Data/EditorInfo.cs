using System.Collections.Generic;

using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data
{
    public class EditorInfo : PAObject<EditorInfo>, IFile
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

        public bool analyzedBPM;

        public bool bpmSnapActive;

        public float bpm = 140f;

        /// <summary>
        /// Offset to the song BPM. Good for cases where the song starts at an offset.
        /// </summary>
        public float bpmOffset = 0f;

        public float timeSignature = 4f;

        #endregion

        #region Story

        public bool isStory;

        public int storyChapter;

        public int storyLevel;

        public int cutscene = -1;

        public Story.CutsceneDestination cutsceneDestination = Story.CutsceneDestination.Level;

        #endregion

        public FileFormat FileFormat => FileFormat.LSE;

        public string GetFileName() => $"editor{FileFormat.Dot()}";

        public void ReadFromFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            if (!path.EndsWith(FileFormat.Dot()))
                path = path += FileFormat.Dot();

            var file = RTFile.ReadFromFile(path);
            if (!string.IsNullOrEmpty(file))
                ReadJSON(JSON.Parse(file));
        }

        public void WriteToFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            var jn = ToJSON();
            if (jn != null)
                RTFile.WriteToFile(path, jn.ToString(3));
        }

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

        public CaptureSettings captureSettings = new CaptureSettings();

        public override void CopyData(EditorInfo orig, bool newID = true)
        {

        }

        public override void ReadJSON(JSONNode jn)
        {
            if (jn["paths"] != null)
            {
                prefabPath = jn["paths"]["prefab"];
                themePath = jn["paths"]["theme"];
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

                mainZoom = zoom;
                mainPosition = position;

                this.layer = layer;
                this.layerType = layerType;

                binCount = jn["timeline"]["bin_count"] != null ? jn["timeline"]["bin_count"].AsInt : EditorTimeline.DEFAULT_BIN_COUNT;
                binPosition = jn["timeline"]["bin_position"].AsFloat;

                if (jn["timeline"]["pinned_layers"] != null)
                    for (int i = 0; i < jn["timeline"]["pinned_layers"].Count; i++)
                        pinnedEditorLayers.Add(PinnedEditorLayer.Parse(jn["timeline"]["pinned_layers"][i]));
            }

            if (jn["editor"] != null)
            {
                if (jn["editor"]["t"] != null)
                    timer.offset = jn["editor"]["t"].AsFloat;
                if (jn["editor"]["editing_time"] != null)
                    timer.offset = jn["editor"]["editing_time"].AsFloat;

                if (jn["editor"]["a"] != null)
                    openAmount = jn["editor"]["a"].AsInt + 1;
                if (jn["editor"]["open_amount"] != null)
                    openAmount = jn["editor"]["open_amount"].AsInt + 1;
            }

            try
            {
                if (jn["story"] != null)
                {
                    isStory = true;
                    if (jn["story"]["chapter"] != null)
                        storyChapter = jn["story"]["chapter"].AsInt;
                    if (jn["story"]["level"] != null)
                        storyLevel = jn["story"]["level"].AsInt;
                    if (jn["story"]["cutscene"] != null)
                        cutscene = jn["story"]["cutscene"].AsInt;
                    if (jn["story"]["cutscene_destination"] != null && System.Enum.TryParse(jn["story"]["cutscene_destination"], true, out Story.CutsceneDestination cutsceneDestination))
                        cutsceneDestination = cutsceneDestination;
                }
            }
            catch (System.Exception ex)
            {
                CoreHelper.LogException(ex);
            }

            if (jn["misc"] != null)
            {
                captureSettings?.ReadJSON(jn["misc"]["capture_settings"]);

                if (jn["misc"]["bpm_analyzed"] != null)
                    analyzedBPM = jn["misc"]["bpm_analyzed"].AsBool;

                if (jn["misc"]["sn"] != null)
                    bpmSnapActive = jn["misc"]["sn"].AsBool;
                if (jn["misc"]["bpm_snap_active"] != null)
                    bpmSnapActive = jn["misc"]["bpm_snap_active"].AsBool;

                if (jn["misc"]["bpm"] != null)
                    bpm = jn["misc"]["bpm"].AsFloat;

                if (jn["misc"]["so"] != null)
                    bpmOffset = jn["misc"]["so"].AsFloat;
                if (jn["misc"]["bpm_offset"] != null)
                    bpmOffset = jn["misc"]["bpm_offset"].AsFloat;

                if (jn["misc"]["bpm_signature"] != null)
                    timeSignature = jn["misc"]["bpm_signature"].AsFloat;

                if (jn["misc"]["t"] != null)
                    time = jn["misc"]["t"].AsFloat;
                if (jn["misc"]["time"] != null)
                    time = jn["misc"]["time"].AsFloat;

                if (jn["misc"]["prefab_object_data"] != null && RTPrefabEditor.inst)
                    RTPrefabEditor.inst.copiedInstanceData = PrefabObject.Parse(jn["misc"]["prefab_object_data"]);
            }
        }

        /// <summary>
        /// Converts the editor info into a JSON object.
        /// </summary>
        /// <returns>Returns a JSON object representing the editor info.</returns>
        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            if (!string.IsNullOrEmpty(prefabPath))
                jn["paths"]["prefab"] = prefabPath;
            if (!string.IsNullOrEmpty(themePath))
                jn["paths"]["theme"] = themePath;

            jn["timeline"]["zoom"] = EditorManager.inst.zoomFloat.ToString("f3");
            jn["timeline"]["position"] = EditorManager.inst.timelineScrollRectBar.value.ToString("f2");

            jn["timeline"]["layer_type"] = (int)EditorTimeline.inst.layerType;
            jn["timeline"]["layer"] = EditorTimeline.inst.Layer;
            jn["timeline"]["bin_count"] = EditorTimeline.inst.BinCount;
            jn["timeline"]["bin_position"] = EditorTimeline.inst.binSlider.value;

            for (int i = 0; i < pinnedEditorLayers.Count; i++)
                jn["timeline"]["pinned_layers"][i] = pinnedEditorLayers[i].ToJSON();

            jn["editor"]["editing_time"] = timer.time;
            jn["editor"]["open_amount"] = openAmount;

            if (isStory)
            {
                jn["story"]["chapter"] = storyChapter;
                jn["story"]["level"] = storyLevel;
                if (cutscene >= 0)
                {
                    jn["story"]["cutscene"] = cutscene;
                    if (cutsceneDestination != Story.CutsceneDestination.Level)
                        jn["story"]["cutscene_destination"] = cutsceneDestination.ToString().ToLower();
                }
            }

            if (captureSettings)
                jn["misc"]["capture_settings"] = captureSettings.ToJSON();

            if (analyzedBPM)
                jn["misc"]["bpm_analyzed"] = analyzedBPM;
            jn["misc"]["bpm_snap_active"] = bpmSnapActive;
            jn["misc"]["bpm"] = bpm;
            jn["misc"]["bpm_offset"] = bpmOffset;
            jn["misc"]["bpm_signature"] = timeSignature;
            jn["misc"]["time"] = AudioManager.inst.CurrentAudioSource.time;

            if (RTPrefabEditor.inst && RTPrefabEditor.inst.copiedInstanceData)
                jn["misc"]["prefab_object_data"] = RTPrefabEditor.inst.copiedInstanceData.ToJSON();

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

            SettingEditor.inst.SnapBPM = bpm;

            if (time >= 0f && time < AudioManager.inst.CurrentAudioSource.clip.length && EditorConfig.Instance.LevelLoadsLastTime.Value)
                AudioManager.inst.SetMusicTime(time);

            if (!string.IsNullOrEmpty(prefabPath) && RTEditor.inst.PrefabPath != prefabPath)
            {
                RTEditor.inst.prefabPathField.text = prefabPath;
                RTEditor.inst.UpdatePrefabPath(false);
            }

            if (!string.IsNullOrEmpty(themePath) && RTEditor.inst.ThemePath != themePath)
            {
                RTThemeEditor.inst.Popup.PathField.text = themePath;
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

            bpm = MetaData.Current.song.bpm;

            time = AudioManager.inst.CurrentAudioSource.time;

            // don't apply paths since it's just an override.
        }
    }
}
