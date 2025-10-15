using System.Collections.Generic;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Editor.Managers;

using Setting = BetterLegacy.Editor.Data.Keybind.Setting;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Represents a set of custom keybinds.
    /// </summary>
    public class KeybindProfile : PAObject<KeybindProfile>, IFile
    {
        public KeybindProfile() => id = GetNumberID();

        public KeybindProfile(string name) : this(CURRENT_VERSION, name) { }

        public KeybindProfile(int version, string name) : this()
        {
            this.version = version;
            this.name = name;
        }

        public KeybindProfile(string name, List<Keybind> keybinds) : this(name) => this.keybinds = keybinds;

        public KeybindProfile(int version, string name, List<Keybind> keybinds) : this(version, name) => this.keybinds = keybinds;

        #region Values

        /// <summary>
        /// The default keybinds profile.
        /// </summary>
        public static KeybindProfile DefaultProfile => new KeybindProfile("Default", GetDefaultKeybinds());

        /// <summary>
        /// The current version. Indicates changes to the keybinds format.
        /// </summary>
        public const int CURRENT_VERSION = 0;

        /// <summary>
        /// The keybind profiles' local version.
        /// </summary>
        public int version = 0;

        /// <summary>
        /// Name of the keybind profile.
        /// </summary>
        public string name = string.Empty;

        /// <summary>
        /// List of keybinds in the profile.
        /// </summary>
        public List<Keybind> keybinds = new List<Keybind>();

        public FileFormat FileFormat => FileFormat.LSS;

        #endregion

        #region Methods

        public string GetFileName() => RTFile.FormatLegacyFileName(name) + FileFormat.Dot();

        public void ReadFromFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            if (!path.EndsWith(FileFormat.Dot()))
                path = path += FileFormat.Dot();

            var file = RTFile.ReadFromFile(path);
            if (string.IsNullOrEmpty(file))
                return;

            ReadJSON(JSON.Parse(file));
        }

        public void WriteToFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            var jn = ToJSON();
            RTFile.WriteToFile(path, jn.ToString());
        }

        public override void CopyData(KeybindProfile orig, bool newID = true)
        {
            id = newID ? GetNumberID() : orig.id;
            version = orig.version;
            name = orig.name;
            keybinds = orig.keybinds;
        }

        public override void ReadJSON(JSONNode jn)
        {
            version = jn["ver"].AsInt;
            id = jn["id"];
            name = jn["name"] ?? string.Empty;
            keybinds.Clear();
            for (int i = 0; i < jn["keybinds"].Count; i++)
                keybinds.Add(Keybind.Parse(jn["keybinds"][i]));
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["ver"] = version;
            jn["id"] = id ?? GetNumberID();
            jn["name"] = name ?? string.Empty;
            for (int i = 0; i < keybinds.Count; i++)
                jn["keybinds"][i] = keybinds[i].ToJSON();

            return jn;
        }

        /// <summary>
        /// Gets the default list of keybinds.
        /// </summary>
        /// <returns>Returns the default list of keybinds.</returns>
        public static List<Keybind> GetDefaultKeybinds() => new List<Keybind>()
        {
            new Keybind(nameof(KeybindEditor.TogglePlayingSong), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Space)),

            new Keybind(nameof(KeybindEditor.SaveLevel), new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.S)),
            new Keybind(nameof(KeybindEditor.OpenLevelPopup), new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.O)),

            new Keybind(nameof(KeybindEditor.SetLayer), new List<Setting> { new Setting("Layer", "0") },
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Alpha1)),
            new Keybind(nameof(KeybindEditor.SetLayer), new List<Setting> { new Setting("Layer", "1") },
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Alpha2)),
            new Keybind(nameof(KeybindEditor.SetLayer), new List<Setting> { new Setting("Layer", "2") },
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Alpha3)),
            new Keybind(nameof(KeybindEditor.SetLayer), new List<Setting> { new Setting("Layer", "3") },
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Alpha4)),
            new Keybind(nameof(KeybindEditor.SetLayer), new List<Setting> { new Setting("Layer", "4") },
                new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Alpha5)),

            new Keybind(nameof(KeybindEditor.ToggleEventLayer), new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftShift), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.E)),
            new Keybind(nameof(KeybindEditor.Undo), new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl), new Keybind.Key(Keybind.Key.Type.NotPressed, KeyCode.LeftShift), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Z)),
            new Keybind(nameof(KeybindEditor.Redo), new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl), new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftShift), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Z)),
            new Keybind(nameof(KeybindEditor.SwapLockSelection), new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.L)),
            new Keybind(nameof(KeybindEditor.UpdateObject), new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.R)),
            new Keybind(nameof(KeybindEditor.UpdateEverything), new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.T)),
            new Keybind(nameof(KeybindEditor.SetFirstKeyframeInType), new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Comma)),
            new Keybind(nameof(KeybindEditor.SetLastKeyframeInType), new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Period)),
            new Keybind(nameof(KeybindEditor.SetPreviousKeyframeInType), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Comma)),
            new Keybind(nameof(KeybindEditor.SetNextKeyframeInType), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Period)),
            new Keybind(nameof(KeybindEditor.JumpToPreviousMarker), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.LeftBracket)),
            new Keybind(nameof(KeybindEditor.JumpToNextMarker), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.RightBracket)),
            new Keybind(nameof(KeybindEditor.AddPitch), new List<Setting> { new Setting("Pitch", "0.1") }, new Keybind.Key(Keybind.Key.Type.Down, KeyCode.UpArrow)),
            new Keybind(nameof(KeybindEditor.AddPitch), new List<Setting> { new Setting("Pitch", "-0.1") }, new Keybind.Key(Keybind.Key.Type.Down, KeyCode.DownArrow)),
            new Keybind(nameof(KeybindEditor.ToggleShowHelp), new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.H)),
            new Keybind(nameof(KeybindEditor.GoToCurrentTime), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Insert)),
            new Keybind(nameof(KeybindEditor.GoToStart), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Home)),
            new Keybind(nameof(KeybindEditor.GoToEnd), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.End)),
            new Keybind(nameof(KeybindEditor.CreateNewMarker), new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.M)),
            new Keybind(nameof(KeybindEditor.SpawnSelectedQuickPrefab), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Slash)),
            new Keybind(nameof(KeybindEditor.Cut), new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.X)),
            new Keybind(nameof(KeybindEditor.Copy), new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.C)),
            new Keybind(nameof(KeybindEditor.Paste), new List<Setting> { new Setting("Remove Prefab Instance ID", "False") }, new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.V)),
            new Keybind(nameof(KeybindEditor.Duplicate), new List<Setting> { new Setting("Remove Prefab Instance ID", "False") }, new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.D)),
            new Keybind(nameof(KeybindEditor.Delete), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Backspace)),
            new Keybind(nameof(KeybindEditor.Delete), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Delete)),
            new Keybind(nameof(KeybindEditor.ToggleZenMode), new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftAlt), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Z)),
            new Keybind(nameof(KeybindEditor.TransformPosition),
                new List<Setting> { new Setting("Create Keyframe", "True"), new Setting("Use Nearest", "True"), new Setting("Use Previous", "False") },
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.G)),
            new Keybind(nameof(KeybindEditor.TransformScale),
                new List<Setting> { new Setting("Create Keyframe", "True"), new Setting("Use Nearest", "True"), new Setting("Use Previous", "False") },
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Y)),
            new Keybind(nameof(KeybindEditor.TransformRotation),
                new List<Setting> { new Setting("Create Keyframe", "True"), new Setting("Use Nearest", "True"), new Setting("Use Previous", "False") },
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.R), new Keybind.Key(Keybind.Key.Type.NotPressed, KeyCode.LeftControl)),
            new Keybind(nameof(KeybindEditor.FinishTransform),
                new List<Setting> { new Setting("Cancel", "False") },
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Return)),
            new Keybind(nameof(KeybindEditor.FinishTransform),
                new List<Setting> { new Setting("Cancel", "True") },
                new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Escape)),
            new Keybind(nameof(KeybindEditor.ToggleProjectPlanner), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.F10)),
            new Keybind(nameof(KeybindEditor.ParentPicker), new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftControl), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.P)),

            new Keybind(nameof(KeybindEditor.AddTimelineBin), new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.Tab), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.KeypadPlus)),
            new Keybind(nameof(KeybindEditor.AddTimelineBin), new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.Tab), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Equals)),
            new Keybind(nameof(KeybindEditor.RemoveTimelineBin), new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.Tab), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.KeypadMinus)),
            new Keybind(nameof(KeybindEditor.RemoveTimelineBin), new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.Tab), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.Minus)),

            new Keybind(nameof(KeybindEditor.AddLayer), new List<Setting> { new Setting("Layer", "1") }, new Keybind.Key(Keybind.Key.Type.Down, KeyCode.PageUp)),
            new Keybind(nameof(KeybindEditor.AddLayer), new List<Setting> { new Setting("Layer", "-1") }, new Keybind.Key(Keybind.Key.Type.Down, KeyCode.PageDown)),

            new Keybind(nameof(KeybindEditor.SetSongTimeAutokill), new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftAlt), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.K)),
            new Keybind(nameof(KeybindEditor.HideSelection), new Keybind.Key(Keybind.Key.Type.NotPressed, KeyCode.LeftControl), new Keybind.Key(Keybind.Key.Type.NotPressed, KeyCode.LeftAlt), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.H)),
            new Keybind(nameof(KeybindEditor.UnhideHiddenObjects), new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftAlt), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.H)),

            new Keybind(nameof(KeybindEditor.ForceSnapBPM), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.J)),
            new Keybind(nameof(KeybindEditor.ToggleBPMSnap), new Keybind.Key(Keybind.Key.Type.Pressed, KeyCode.LeftAlt), new Keybind.Key(Keybind.Key.Type.Down, KeyCode.S)),
        };

        #endregion
    }
}
