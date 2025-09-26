using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy
{
    /* TODO:
    - Allow editor layout, editor themes and editor complexity to be customized via the asset pack.
      For example, you could make the object origin toggles display in Advanced editor complexity.
    - Move Story folder to beatmaps? Or should it be considered an asset?
    - Change Example to use this system, so he can be fully customized without modding.
    - Make uploadable
    - Levels & level collections can store asset packs, or reference specific ones online. (change "soundlibrary" folder to this system)
     */

    /// <summary>
    /// Represents a package of mod assets and resources. Assets can be customized via adding a new asset pack to <see cref="AssetPacks"/>.<br></br>
    /// If you want to get an asset, use <see cref="GetFile(string)"/>.
    /// </summary>
    public class AssetPack : Exists, IFile, IUploadable
    {
        public AssetPack() { }

        public AssetPack(string path) => this.path = path;

        public AssetPack(string path, Sprite icon) : this(path) => this.icon = icon;

        public AssetPack(string path, Sprite icon, string name) : this(path, icon) => this.name = name;

        public AssetPack(string path, Sprite icon, string name, string description) : this(path, icon, name) => this.description = description;

        #region Global

        #region Values

        /// <summary>
        /// The default asset pack.
        /// </summary>
        public static AssetPack BuiltIn { get; } = new AssetPack(
            path: RTFile.CombinePaths(RTFile.ApplicationDirectory, RTFile.BepInExAssetsPath),
            icon: SpriteHelper.LoadSprite(RTFile.GetAsset(ICON_JPG)),
            name: "Built-In Assets",
            description: "The default BetterLegacy assets.");

        /// <summary>
        /// List of loaded asset packs.
        /// </summary>
        public static List<AssetPack> AssetPacks { get; set; } = new List<AssetPack>();

        /// <summary>
        /// List of asset pack settings.
        /// </summary>
        public static List<UserSettings> Settings { get; set; } = new List<UserSettings>();

        public const string DATA_LSAP = "data.lsap";

        public const string ICON_JPG = "icon.jpg";

        public const string ASSETPACKS_FOLDER = "assetpacks";

        #endregion

        #region Methods

        /// <summary>
        /// Gets an asset file.
        /// </summary>
        /// <param name="assetPath">Path to the asset.</param>
        /// <returns>Returns a combined path of the found asset.
        /// <br></br>Searches through the loaded asset packs for the specified file. If any of the loaded asset packs contains the file, then returns the asset pack file path and the <paramref name="assetPath"/>.
        /// <br></br>Otherwise if none of the loaded asset packs contains the file, returns the built-in asset pack file path and the <paramref name="assetPath"/>.</returns>
        public static string GetFile(string assetPath)
        {
            for (int i = AssetPacks.Count - 1; i >= 0; i--)
            {
                var assetPack = AssetPacks[i];
                var settings = assetPack.GetSettings();
                if (settings && !settings.enabled)
                    continue;

                if (assetPack.HasFile(assetPath))
                    return RTFile.CombinePaths(assetPack.path, assetPath);
            }

            return RTFile.CombinePaths(BuiltIn.path, assetPath);
        }

        /// <summary>
        /// Tries to get an asset file.
        /// </summary>
        /// <param name="assetPath">Path to the asset.</param>
        /// <param name="filePath">File path result.</param>
        /// <returns>Returns true if an asset file exists, otherwise returns false.</returns>
        public static bool TryGetFile(string assetPath, out string filePath)
        {
            filePath = GetFile(assetPath);
            return RTFile.FileExists(filePath);
        }

        /// <summary>
        /// Tries to get and read an asset file.
        /// </summary>
        /// <param name="assetPath">Path to the asset.</param>
        /// <param name="file">File result.</param>
        /// <returns>Returns true if an asset file exists and has been read successfully, otherwise returns false.</returns>
        public static bool TryReadFromFile(string assetPath, out string file)
        {
            var filePath = GetFile(assetPath);
            return RTFile.TryReadFromFile(filePath, out file);
        }

        /// <summary>
        /// Gets an asset directory.
        /// </summary>
        /// <param name="assetPath">Path to the asset.</param>
        /// <returns>Returns a combined path of the found asset directory.
        /// <br></br>Searches through the loaded asset packs for the specified directory. If any of the loaded asset packs contains the directory, then returns the asset pack file path and the <paramref name="assetPath"/>.
        /// <br></br>Otherwise if none of the loaded asset packs contains the directory, returns the built-in asset pack file path and the <paramref name="assetPath"/>.</returns>
        public static string GetDirectory(string assetPath)
        {
            for (int i = AssetPacks.Count - 1; i >= 0; i--)
            {
                var assetPack = AssetPacks[i];
                var settings = assetPack.GetSettings();
                if (settings && !settings.enabled)
                    continue;

                if (assetPack.HasDirectory(assetPath))
                    return RTFile.CombinePaths(assetPack.path, assetPath);
            }

            return RTFile.CombinePaths(BuiltIn.path, assetPath);
        }

        /// <summary>
        /// Tries to get an asset directory.
        /// </summary>
        /// <param name="assetPath">Path to the asset directory.</param>
        /// <param name="directory">Directory path result.</param>
        /// <returns>Returns true if an asset directory exists, otherwise returns false.</returns>
        public static bool TryGetDirectory(string assetPath, out string directory)
        {
            directory = GetFile(assetPath);
            return RTFile.DirectoryExists(directory);
        }

        /// <summary>
        /// Gets an array of items from an asset file, or multiple.
        /// </summary>
        /// <param name="assetPath">Path to the asset.</param>
        /// <param name="forceAdd">If multiple entries are prioritized.</param>
        /// <returns>Returns a JSON array of the items.</returns>
        public static JSONArray GetArray(string assetPath, bool forceAdd = false)
        {
            var jsonArray = Parser.NewJSONArray().AsArray;

            jsonArray = AddArray(jsonArray, RTFile.ReadFromFile(RTFile.CombinePaths(BuiltIn.path, assetPath)), false);

            for (int i = AssetPacks.Count - 1; i >= 0; i--)
            {
                var assetPack = AssetPacks[i];
                var settings = assetPack.GetSettings();
                if (settings && !settings.enabled)
                    continue;

                if (!assetPack.HasFile(assetPath) || RTFile.TryReadFromFile(RTFile.CombinePaths(assetPack.path, assetPath), out string file))
                    continue;

                jsonArray = AddArray(jsonArray, file, forceAdd);
            }

            return jsonArray;
        }

        static JSONArray AddArray(JSONArray jsonArray, string file, bool forceAdd)
        {
            var jn = JSON.Parse(file);

            if (!jn.IsArray)
            {
                var overwrite = jn["overwrite"].AsBool;
                if (forceAdd || !overwrite)
                    ReadArray(jsonArray, jn["items"]);
                else
                    jsonArray = jn["items"].AsArray;
            }
            else
            {
                if (forceAdd)
                    ReadArray(jsonArray, jn["items"]);
                else
                    jsonArray = jn["items"].AsArray;
            }

            return jsonArray;
        }

        static void ReadArray(JSONArray jsonArray, JSONNode jn)
        {
            for (int i = 0; i < jn.Count; i++)
            {
                var item = jn[i];
                if (item.IsArray)
                {
                    ReadArray(jsonArray, item);
                    continue;
                }

                jsonArray.Add(item);
            }
        }

        /// <summary>
        /// Loads all asset packs.
        /// </summary>
        public static void LoadAssetPacks() => LoadAssetPacks(null, null);

        /// <summary>
        /// Loads all asset packs.
        /// </summary>
        /// <param name="progress">Function to run on progress update.</param>
        /// <param name="onLoaded">Function to run on asset packs loaded.</param>
        public static void LoadAssetPacks(Action<float, AssetPack> progress, Action onLoaded)
        {
            AssetPacks.Clear();

            if (!RTFile.DirectoryExists(RTFile.CombinePaths(RTFile.ApplicationDirectory, ASSETPACKS_FOLDER)))
                return;

            var directories = Directory.GetDirectories(RTFile.CombinePaths(RTFile.ApplicationDirectory, ASSETPACKS_FOLDER));
            for (int i = 0; i < directories.Length; i++)
            {
                var directory = directories[i];
                if (!RTFile.FileExists(RTFile.CombinePaths(directory, DATA_LSAP)))
                    continue;

                var assetPack = RTFile.CreateFromFile<AssetPack>(directory);
                AssetPacks.Add(assetPack);

                try
                {
                    progress?.Invoke(i / directories.Length, assetPack);
                }
                catch
                {

                }
            }

            ValidateSettings();
            Sort();

            onLoaded?.Invoke();
        }

        /// <summary>
        /// Validates that all asset packs have settings.
        /// </summary>
        public static void ValidateSettings()
        {
            for (int i = 0; i < AssetPacks.Count; i++)
            {
                var assetPack = AssetPacks[i];
                if (!assetPack.GetSettings())
                    Settings.Add(new UserSettings(assetPack.id, true));
            }
        }

        /// <summary>
        /// Sorts the loaded asset packs by their related settings.<br></br>
        /// Lowest asset packs in the list are prioritized.
        /// </summary>
        public static void Sort()
        {
            var count = AssetPacks.Count;
            AssetPacks = AssetPacks.OrderBy(a => Settings.TryFindIndex(x => x.id == a.id, out int index) ? index : count).ToList();
        }

        #endregion

        #endregion

        #region Values

        /// <summary>
        /// File path to the asset pack.
        /// </summary>
        public string path;

        /// <summary>
        /// Icon of the asset pack.
        /// </summary>
        public Sprite icon;

        /// <summary>
        /// Identification of the asset pack.
        /// </summary>
        public string id;

        /// <summary>
        /// Name of the asset pack.
        /// </summary>
        public string name;

        /// <summary>
        /// Description of the asset pack.
        /// </summary>
        public string description;

        #region Server

        public string ServerID { get; set; }

        public string UploaderName { get; set; }

        public string UploaderID { get; set; }

        public List<ServerUser> Uploaders { get; set; }

        public ServerVisibility Visibility { get; set; }

        public string Changelog { get; set; }

        public List<string> ArcadeTags { get; set; }

        public string ObjectVersion { get; set; }

        public string DatePublished { get; set; }

        public int VersionNumber { get; set; }

        #endregion

        public FileFormat FileFormat => FileFormat.LSAP;

        #endregion

        #region Methods

        /// <summary>
        /// Gets the associated user settings for the asset pack.
        /// </summary>
        /// <returns>Returns the found user settings.</returns>
        public UserSettings GetSettings() => Settings.Find(x => x.id == id);

        /// <summary>
        /// Enables the asset pack.
        /// </summary>
        public void Enable()
        {
            var settings = GetSettings();
            if (settings)
                settings.enabled = true;
            else
                Settings.Add(new UserSettings(id, true));
            LegacyPlugin.SaveProfile();
        }

        /// <summary>
        /// Disables the asset pack.
        /// </summary>
        public void Disable()
        {
            var settings = GetSettings();
            if (settings)
                settings.enabled = false;
            else
                Settings.Add(new UserSettings(id, false));
            LegacyPlugin.SaveProfile();
        }

        /// <summary>
        /// Moves the asset pack priority up.
        /// </summary>
        public void MoveUp()
        {
            var settingsIndex = Settings.FindIndex(x => x.id == id);
            if (settingsIndex < 0)
            {
                var index = AssetPacks.IndexOf(this) - 1;
                index = RTMath.Clamp(index, 0, Settings.Count - 1);
                Settings.Insert(index, new UserSettings(id, true));
            }
            else if (settingsIndex > 1)
                Settings.Move(settingsIndex, settingsIndex - 1);

            Sort();
        }

        /// <summary>
        /// Moves the asset pack priority down.
        /// </summary>
        public void MoveDown()
        {
            var settingsIndex = Settings.FindIndex(x => x.id == id);
            if (settingsIndex < 0)
            {
                var index = AssetPacks.IndexOf(this) + 1;
                index = RTMath.Clamp(index, 0, Settings.Count - 1);
                Settings.Insert(index, new UserSettings(id, true));
            }
            else if (settingsIndex < Settings.Count - 1)
                Settings.Move(settingsIndex, settingsIndex + 1);

            Sort();
        }

        /// <summary>
        /// If the asset pack has a file.
        /// </summary>
        /// <param name="assetPath">Asset file path.</param>
        /// <returns>Returns true if the asset pack contains the file, otherwise returns false.</returns>
        public bool HasFile(string assetPath) => RTFile.FileExists(RTFile.CombinePaths(path, assetPath));

        /// <summary>
        /// If the asset pack has a directory.
        /// </summary>
        /// <param name="assetPath">Asset directory path.</param>
        /// <returns>Returns true if the asset pack contains the directory, otherwise returns false.</returns>
        public bool HasDirectory(string assetPath) => RTFile.DirectoryExists(RTFile.CombinePaths(path, assetPath));

        public string GetFileName() => Path.GetFileName(path);

        public void ReadFromFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            if (path.EndsWith(DATA_LSAP))
                path = path.Remove(DATA_LSAP);

            if (!RTFile.DirectoryExists(path))
                return;

            this.path = path;

            if (RTFile.FileExists(RTFile.CombinePaths(path, ICON_JPG)))
                icon = SpriteHelper.LoadSprite(RTFile.CombinePaths(path, ICON_JPG));

            var dataPath = RTFile.CombinePaths(path, DATA_LSAP);
            if (!RTFile.FileExists(dataPath))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(dataPath));
            if (jn["name"] != null)
                name = jn["name"];
            if (jn["id"] != null)
                id = jn["id"];
            else
                id = PAObjectBase.GetNumberID();
            if (jn["desc"] != null)
                description = jn["desc"];

            this.ReadUploadableJSON(jn);
        }

        public void WriteToFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            if (!path.Contains(DATA_LSAP))
                path += DATA_LSAP;

            var jn = Parser.NewJSONObject();

            if (!string.IsNullOrEmpty(id))
                jn["id"] = id;
            else
                jn["id"] = PAObjectBase.GetNumberID();
            if (!string.IsNullOrEmpty(name))
                jn["name"] = name;
            if (!string.IsNullOrEmpty(description))
                jn["desc"] = description;

            this.WriteUploadableJSON(jn);

            RTFile.WriteToFile(path, jn.ToString(3));
        }

        public override string ToString() => name;

        #endregion

        /// <summary>
        /// Represents the settings for an asset pack.
        /// </summary>
        public class UserSettings : Exists
        {
            public UserSettings(string id, bool enabled)
            {
                this.id = id;
                this.enabled = enabled;
            }

            /// <summary>
            /// Identification of the asset pack.
            /// </summary>
            public string id;

            /// <summary>
            /// If the asset pack is enabled.
            /// </summary>
            public bool enabled;
        }
    }
}
