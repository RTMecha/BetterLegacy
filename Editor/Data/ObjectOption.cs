using System;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Timeline;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Represents an object that can be created.
    /// </summary>
    public class ObjectOption : PAObject<ObjectOption>
    {
        public ObjectOption() { }

        public ObjectOption(string name, string hint, Action<TimelineObject> action)
        {
            this.name = name;
            this.hint = hint;
            this.action = action;
        }

        /// <summary>
        /// Name of the option.
        /// </summary>
        public string name;

        /// <summary>
        /// Hint description of what the option creates.
        /// </summary>
        public string hint;

        /// <summary>
        /// Function to run when the object is created.
        /// </summary>
        public Action<TimelineObject> action;

        /// <summary>
        /// Object to parse.
        /// </summary>
        public JSONNode obj;

        /// <summary>
        /// Icon to display.
        /// </summary>
        public Sprite icon;

        /// <summary>
        /// Icon reference.
        /// </summary>
        public string iconReference;

        public override void CopyData(ObjectOption orig, bool newID = true)
        {
            name = orig.name;
            hint = orig.hint;
            action = orig.action;
            icon = orig.icon;
        }

        public override void ReadJSON(JSONNode jn)
        {
            if (!string.IsNullOrEmpty(jn["name"]))
                name = jn["name"];
            if (!string.IsNullOrEmpty(jn["hint"]))
                hint = jn["hint"];
            if (jn["obj"] != null)
                obj = jn["obj"];
            if (jn["icon"] != null)
                icon = SpriteHelper.StringToSprite(jn["icon"]);
            if (!string.IsNullOrEmpty(jn["icon_ref"]))
                iconReference = jn["icon_ref"];
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            if (!string.IsNullOrEmpty(name))
                jn["name"] = name;
            if (!string.IsNullOrEmpty(hint))
                jn["hint"] = hint;
            if (obj != null)
                jn["obj"] = obj;
            if (!string.IsNullOrEmpty(iconReference))
                jn["icon_ref"] = iconReference;
            if (icon)
                jn["icon"] = SpriteHelper.SpriteToString(icon);

            return jn;
        }

        /// <summary>
        /// Creates the associated object.
        /// </summary>
        public void Create()
        {
            try
            {
                if (obj != null)
                    ObjectEditor.inst.CreateNewObject(timelineObject =>
                    {
                        var beatmapObject = timelineObject.GetData<BeatmapObject>();
                        beatmapObject.ReadJSON(obj);
                        beatmapObject.id = GetStringID(); // refresh ID so it's always random

                        var time = AudioManager.inst.CurrentAudioSource.time;
                        if (RTEditor.inst.editorInfo.bpmSnapActive && EditorConfig.Instance.BPMSnapsCreated.Value && EditorConfig.Instance.BPMSnapsObjects.Value)
                            time = RTEditor.SnapToBPM(time);
                        beatmapObject.StartTime = time;
                        beatmapObject.editorData.Layer = EditorTimeline.inst.Layer;

                        ObjectEditor.inst.ApplyObjectCreationSettings(beatmapObject);
                        EditorTimeline.inst.SelectObject(timelineObject);
                    }, false);
                else
                    ObjectEditor.inst.CreateNewObject(action);
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        /// <summary>
        /// Gets the icon of the object creation option.
        /// </summary>
        /// <returns>Returns the associated icon.</returns>
        public Sprite GetIcon()
        {
            if (!string.IsNullOrEmpty(iconReference))
            {
                if (AssetPack.TryGetFile(iconReference, out string filePath))
                {
                    try
                    {
                        return SpriteHelper.LoadSprite(filePath);
                    }
                    catch
                    {

                    }
                }

                var split = iconReference.Split('/');
                switch (split[0])
                {
                    case "shape": {
                            if (split.Length <= 1)
                                break;

                            var shape = Parser.TryParse(split[1].ToString(), 0);
                            shape = RTMath.Clamp(shape, 0, ShapeManager.inst.Shapes2D.Count - 1);

                            if (split.Length > 2)
                            {
                                var shapeOption = Parser.TryParse(split[2].ToString(), 0);
                                shapeOption = RTMath.Clamp(shapeOption, 0, ShapeManager.inst.Shapes2D[shape].Count - 1);

                                return ShapeManager.inst.Shapes2D[shape][shapeOption].icon;
                            }

                            return ShapeManager.inst.Shapes2D[shape].icon;
                        }
                }
            }

            return icon;
        }
    }
}
