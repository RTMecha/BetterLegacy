using BetterLegacy.Core.Helpers;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BetterLegacy.Core
{
    /// <summary>
    /// File system wrapper class based on <see cref="LSFunctions.LSFile"/>.
    /// </summary>
    public static class RTFile
    {
        #region Properties

        /// <summary>
        /// The full path to the Project Arrhythmia folder.
        /// </summary>
        public static string ApplicationDirectory => Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("/")) + "/";

        /// <summary>
        /// Full path to Project Arrhythmia's global data folder.
        /// </summary>
        public static string PersistentApplicationDirectory => Application.persistentDataPath;

        /// <summary>
        /// The main level path.
        /// </summary>
        public static string BasePath
        {
            get => GameManager.inst?.basePath;
            set
            {
                if (CoreHelper.InGame)
                    GameManager.inst.basePath = value;
            }
        }

        /// <summary>
        /// Path where all the plugins are stored.
        /// </summary>
        public static string BepInExPluginsPath => "BepInEx/plugins/";

        /// <summary>
        /// Path where all the mod-specific assets are stored.
        /// </summary>
        public static string BepInExAssetsPath => $"{BepInExPluginsPath}Assets/";

        /// <summary>
        /// If the user is using the Mac Operating System.
        /// </summary>
        public static bool IsInMacOS => SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX;

        /// <summary>
        /// If the user is using the Windows Operating System.
        /// </summary>
        public static bool IsInWinOS => SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows;

        #endregion

        #region Methods

        /// <summary>
        /// Checks if the file path is not null or empty and if it exists.
        /// </summary>
        /// <param name="path">File to check.</param>
        /// <returns>Returns true if the file exists, otherwise returns false.</returns>
        public static bool FileExists(string path) => !string.IsNullOrEmpty(path) && File.Exists(path);

        /// <summary>
        /// Checks if the directory path is not null or empty and if it exists.
        /// </summary>
        /// <param name="path">Directory to check.</param>
        /// <returns>Returns true if the directory exists, otherwise returns false.</returns>
        public static bool DirectoryExists(string path) => !string.IsNullOrEmpty(path) && Directory.Exists(path);

        /// <summary>
        /// Gets an asset file from the BepInEx plugins folder.
        /// </summary>
        /// <param name="path">File to get.</param>
        /// <returns>Returns a combined path of the app directory and BepInEx assets path.</returns>
        public static string GetAsset(string path) => CombinePaths(ApplicationDirectory, BepInExAssetsPath, path);

        #region Copying / Moving

        /// <summary>
        /// Copies a file to a new destination.
        /// </summary>
        /// <param name="path">File to copy.</param>
        /// <param name="destination">Copy destination.</param>
        /// <returns>Returns true if the file was successfully copied, otherwise returns false.</returns>
        public static bool CopyFile(string path, string destination)
        {
            if (FileExists(path) && !string.IsNullOrEmpty(destination) && path.ToLower() != destination.ToLower())
            {
                try
                {
                    File.Copy(path, destination, true);
                    return true;
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
            }
            return false;
        }

        /// <summary>
        /// Copies a directory and all of its contents to a new destination.
        /// </summary>
        /// <param name="path">Directory to copy.</param>
        /// <param name="destination">Copy destination.</param>
        /// <returns>Returns true if the directory was successfully copied, otherwise returns false.</returns>
        public static bool CopyDirectory(string path, string destination)
        {
            if (DirectoryExists(path) && !string.IsNullOrEmpty(destination) && path.ToLower() != destination.ToLower())
            {
                try
                {
                    path = ReplaceSlash(path);
                    destination = ReplaceSlash(destination);
                    var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                    var result = false;
                    for (int i = 0; i < files.Length; i++)
                    {
                        var file = ReplaceSlash(files[i]);
                        var fileDestination = file.Replace(path, destination);
                        CreateDirectory(GetDirectory(fileDestination));
                        if (CopyFile(file, fileDestination))
                            result = true;
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
            }
            return false;
        }

        /// <summary>
        /// Moves a file to a new destination.
        /// </summary>
        /// <param name="path">File to move.</param>
        /// <param name="destination">Move destination.</param>
        /// <returns>Returns true if the file was successfully moved, otherwise returns false.</returns>
        public static bool MoveFile(string path, string destination)
        {
            if (FileExists(path) && !string.IsNullOrEmpty(destination) && path.ToLower() != destination.ToLower())
            {
                try
                {
                    DeleteFile(destination);
                    File.Move(path, destination);
                    return true;
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
            }
            return false;
        }

        /// <summary>
        /// Moves a directory to a new destination.
        /// </summary>
        /// <param name="path">Directory to move.</param>
        /// <param name="destination">Move destination.</param>
        /// <returns>Returns true if the directory was successfully moved.</returns>
        public static bool MoveDirectory(string path, string destination)
        {
            if (DirectoryExists(path) && !string.IsNullOrEmpty(destination) && path.ToLower() != destination.ToLower())
            {
                try
                {
                    DeleteDirectory(destination);
                    Directory.Move(path, destination);
                    return true;
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
            }
            return false;
        }

        #endregion

        #region Path Validation / Parsing

        /// <summary>
        /// Ensures the file name is acceptible.
        /// </summary>
        /// <param name="name">File name to validate.</param>
        /// <returns>Returns a validated file name.</returns>
        public static string ValidateFileName(string name)
            => Regex.Replace(name, string.Format("([{0}]*\\.+$)|([{0}]+)", Regex.Escape(new string(Path.GetInvalidFileNameChars())) + "+?#!"), string.Empty);

        /// <summary>
        /// Ensures a directory path if acceptible.
        /// </summary>
        /// <param name="name">Directory path to validate.</param>
        /// <returns>Returns a validated directory path.</returns>
        public static string ValidateDirectory(string name)
            => Regex.Replace(name, string.Format("([{0}]*\\.+$)|([{0}]+)", Regex.Escape(new string(Path.GetInvalidPathChars())) + "+?#!"), string.Empty);

        /// <summary>
        /// Replaces the "\" with "/".
        /// </summary>
        /// <param name="path">Path to format.</param>
        /// <returns>Returns a replaced path. Example: Path\To\File.txt > Path/To/File.txt</returns>
        public static string ReplaceSlash(string path) => path.Replace("\\", "/");

        /// <summary>
        /// Used for setting the file name of a Legacy file. E.G. "Para Template" > "para_template".
        /// </summary>
        /// <param name="name">Name to format.</param>
        /// <returns>Returns a Legacy file name.</returns>
        public static string FormatLegacyFileName(string name) => ValidateFileName(RTString.ReplaceSpace(name.ToLower()));

        /// <summary>
        /// Used for setting the file name of a Alpha file. E.G. "Para Template" > "para template".
        /// </summary>
        /// <param name="name">Name to format.</param>
        /// <returns>Returns a Alpha file name.</returns>
        public static string FormatAlphaFileName(string name) => ValidateFileName(name.ToLower());

        /// <summary>
        /// Gets the parent directory of the path.
        /// </summary>
        /// <param name="path">Path to get the parent directory of.</param>
        /// <returns>Returns the paths' parent.</returns>
        public static string GetDirectory(string path) => ReplaceSlash(Path.GetDirectoryName(path));

        /// <summary>
        /// Parses specific properties with the proper paths.
        /// </summary>
        /// <param name="str">String to parse.</param>
        /// <returns>Returns a file parsed string.</returns>
        public static string ParsePaths(string str) => string.IsNullOrEmpty(str) ? str : str
            .Replace("{{AppDirectory}}", ApplicationDirectory)
            .Replace("{{BepInExAssetsDirectory}}", BepInExAssetsPath)
            .Replace("{{LevelPath}}", GameManager.inst ? BasePath : ApplicationDirectory);

        /// <summary>
        /// Combines two paths together and ensures a correct set of slashes.
        /// </summary>
        /// <param name="path1">First path to combine.</param>
        /// <param name="path2">Second path to combine.</param>
        /// <returns>Returns a proper path.<br></br>Example:<br></br>path1: beatmaps/editor<br></br>path2: Node<br>Result: beatmaps/editor/Node</br></returns>
        public static string CombinePaths(string path1, string path2) => string.IsNullOrEmpty(path1) || string.IsNullOrEmpty(path2) ? string.Empty : ReplaceSlash(Path.Combine(path1, path2));

        /// <summary>
        /// Combines multiple paths together and ensures a correct set of slashes.
        /// </summary>
        /// <param name="paths">The paths to combine.</param>
        /// <returns>Returns a proper path.<br></br>Example:<br></br>path1: beatmaps/editor<br></br>path2: Node<br>Result: beatmaps/editor/Node</br></returns>
        public static string CombinePaths(params string[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
                if (string.IsNullOrEmpty(paths[i]))
                    return string.Empty;

            return ReplaceSlash(Path.Combine(paths));
        }

        /// <summary>
        /// Ensures the path ends with a /.
        /// </summary>
        /// <param name="path">Path to append.</param>
        /// <returns>If the path doesn't end with a /, returns the path with a / appeneded, otherwise returns the path.</returns>
        public static string AppendEndSlash(string path) => string.IsNullOrEmpty(path) ? path : path[path.Length - 1] != '/' ? path + "/" : path;

        /// <summary>
        /// Ensures the path doesn't end with a /.
        /// </summary>
        /// <param name="path">Path to remove a / from.</param>
        /// <returns>If the path ends with a /, returns the path without the / at the end, otherwise returns the path.</returns>
        public static string RemoveEndSlash(string path) => string.IsNullOrEmpty(path) ? path : path[path.Length - 1] == '/' ? path.Substring(0, path.Length - 1) : path;

        #endregion

        #region Reading / Writing

        /// <summary>
        /// Deletes a file, if it exists.
        /// </summary>
        /// <param name="path">File to delete.</param>
        /// <returns>Returns true if the file was successfully deleted, otherwise returns false.</returns>
        public static bool DeleteFile(string path)
        {
            var delete = FileExists(path);
            try
            {
                if (delete)
                    File.Delete(path);
            }
            catch
            {
                delete = false;
            }
            return delete;
        }

        /// <summary>
        /// Deletes a directory recursively, if it exists.
        /// </summary>
        /// <param name="path">Directory to delete.</param>
        /// <returns>Returns true if the directory was successfully deleted, otherwise returns false.</returns>
        public static bool DeleteDirectory(string path)
        {
            var delete = DirectoryExists(path);
            try
            {
                if (delete)
                    Directory.Delete(path, true);
            }
            catch
            {
                delete = false;
            }
            return delete;
        }

        /// <summary>
        /// Checks if a directory doesn't exist and if it doesn't, creates a directory.
        /// </summary>
        /// <param name="path">Directory to create.</param>
        public static bool CreateDirectory(string path)
        {
            var create = !DirectoryExists(path);
            if (create)
                Directory.CreateDirectory(path);
            return create;
        }

        /// <summary>
        /// Writes text to a file.
        /// </summary>
        /// <param name="path">The file to write to.</param>
        /// <param name="json">The text to write.</param>
        public static void WriteToFile(string path, string json)
        {
            using var streamWriter = new StreamWriter(path);
            streamWriter.Write(json);
        }

        /// <summary>
        /// Serializes an object and writes it to a file.
        /// </summary>
        /// <typeparam name="T">Type of <typeparamref name="T"/>. Must be serializable.</typeparam>
        /// <param name="path">The file to write to.</param>
        /// <param name="obj">The object to serialize and write.</param>
        /// 
        /// <returns>Returns true if the object was successfully serialized and the file was properly written to.</returns>
        public static bool WriteToFile<T>(string path, T obj)
        {
            try
            {
                var binaryFormatter = new BinaryFormatter();
                using var fileStream = File.Create(path);
                binaryFormatter.Serialize(fileStream, obj);
                return true;
            }
            catch
            {
                DeleteFile(path);
                return false;
            }
        }

        /// <summary>
        /// Reads text from a file.
        /// </summary>
        /// <param name="path">The file to read from.</param>
        /// <returns>If the file exists, returns the text from the file, otherwise returns null.</returns>
        public static string ReadFromFile(string path) => ReadFromFile(path, true);

        /// <summary>
        /// Reads text from a file.
        /// </summary>
        /// <param name="path">The file to read from.</param>
        /// <returns>If the file exists, returns the text from the file, otherwise returns null.</returns>
        public static string ReadFromFile(string path, bool log)
        {
            if (!FileExists(path))
            {
                CoreHelper.Log($"Could not load JSON file [{path}]");
                return null;
            }

            using var streamReader = new StreamReader(path);
            var result = streamReader.ReadToEnd().ToString();
            return result;
        }

        /// <summary>
        /// Deserializes a file into an object.
        /// </summary>
        /// <typeparam name="T">Type of <typeparamref name="T"/>. Must be serializable.</typeparam>
        /// <param name="path">The file to deserialize.</param>
        /// <returns>If the object is serializable, returns the object deserialized from the file, otherwise returns the default.</returns>
        public static T ReadFromFile<T>(string path)
        {
            if (!FileExists(path))
            {
                CoreHelper.Log($"Could not load file [{path}]");
                return default;
            }

            try
            {
                var binaryFormatter = new BinaryFormatter();
                using var fileStream = File.Open(path, FileMode.Open);
                return (T)binaryFormatter.Deserialize(fileStream);
            }
            catch
            {
                return default;
            }
        }

        public static bool TryReadFromFile(string path, out string file)
        {
            file = ReadFromFile(path, false);
            return !string.IsNullOrEmpty(file);
        }

        /// <summary>
        /// Reads bytes from a Stream.
        /// </summary>
        /// <param name="input">The input stream.</param>
        /// <returns>Returns an array of bytes from the <see cref="Stream"/>.</returns>
        public static byte[] ReadBytes(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using var ms = new MemoryStream();

            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                ms.Write(buffer, 0, read);

            return ms.ToArray();
        }

        public static string BytesToString(byte[] bytes) => Encoding.UTF8.GetString(bytes);

        #endregion

        #endregion

        #region File Formats

        /// <summary>
        /// Checks if the file is a specific file format.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <param name="fileFormats">The file formats to compare.</param>
        /// <returns>Returns true if the files' format is compared to the <paramref name="fileFormats"/>, otherwise returns false.</returns>
        public static bool FileIsFormat(string path, params FileFormat[] fileFormats)
        {
            var result = false;
            for (int i = 0; i < fileFormats.Length; i++)
                if (FileIsFormat(path, fileFormats[i]))
                    result = true;
            return result;
        }

        /// <summary>
        /// Checks if the file is a specific file format.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <param name="fileFormat">The file format to compare.</param>
        /// <returns>Returns true if the files' format is compared to the <paramref name="fileFormat"/>, otherwise returns false.</returns>
        public static bool FileIsFormat(string path, FileFormat fileFormat) => GetFileFormat(path) == fileFormat;

        /// <summary>
        /// Gets a files' <see cref="AudioType"/>.
        /// </summary>
        /// <param name="path">Path to get the audio type from.</param>
        /// <returns>Returns a <see cref="AudioType"/> from the file.</returns>
        public static AudioType GetAudioType(string path) => GetFileFormat(path).ToAudioType();

        /// <summary>
        /// Checks if a <see cref="FileFormat"/> is a valid <see cref="AudioType"/>.
        /// </summary>
        /// <param name="fileFormat">File format to check.</param>
        /// <returns>Returns true if the file format is a valid <see cref="AudioType"/>, otherwise returns false.</returns>
        public static bool ValidAudio(FileFormat fileFormat) => ValidAudioType(fileFormat.ToAudioType());

        /// <summary>
        /// Checks if an <see cref="AudioType"/> is a valid type to be used by PA.
        /// </summary>
        /// <param name="audioType">Audio type to check.</param>
        /// <returns>Returns true if the audio type is usable in PA.</returns>
        public static bool ValidAudioType(AudioType audioType) => audioType == AudioType.OGGVORBIS || audioType == AudioType.WAV || audioType == AudioType.MPEG;

        public static FileFormat[] AudioFormats => new FileFormat[] { FileFormat.OGG, FileFormat.WAV, FileFormat.MP3 };

        public static string[] AudioDotFormats => DotFormats(FileFormat.OGG, FileFormat.WAV, FileFormat.MP3);

        /// <summary>
        /// Adds a . to the beginning of all specified file formats.
        /// </summary>
        /// <param name="fileFormats">File formats to dot.</param>
        /// <returns>Returns an array of dot formats. Example: .json, .txt, .lsb</returns>
        public static string[] DotFormats(params FileFormat[] fileFormats)
        {
            var array = new string[fileFormats.Length];
            for (int i = 0; i < array.Length; i++)
                array[i] = fileFormats[i].Dot();
            return array;
        }

        /// <summary>
        /// Gets a <see cref="FileFormat"/> from a path.
        /// </summary>
        /// <param name="path">Path to get a file format from.</param>
        /// <returns>Returns a parsed <see cref="FileFormat"/>.</returns>
        public static FileFormat GetFileFormat(string path) => string.IsNullOrEmpty(path) || !path.Contains(".") ? FileFormat.NULL : Parser.TryParse(Path.GetExtension(path).Remove("."), true, FileFormat.NULL);

        /// <summary>
        /// Checks if the file is of an audio type.
        /// </summary>
        /// <param name="path">File to check.</param>
        /// <returns>Returns true if the file is an audio (.ogg, .wav, .mp3), otherwise returns false.</returns>
        public static bool FileIsAudio(string path) => ValidAudioType(GetAudioType(path));

        #endregion

        /// <summary>
        /// Wrapper for opening a file in the file explorer.
        /// </summary>
        public static class OpenInFileBrowser
        {
            static void OpenInMac(string path)
            {
                string text = ReplaceSlash(path);
                bool exists = DirectoryExists(text);

                if (!text.StartsWith("\""))
                    text = "\"" + text;
                if (!text.EndsWith("\""))
                    text += "\"";

                string arguments = (exists ? "" : "-R ") + text;

                try
                {
                    Process.Start("open", arguments);
                }
                catch (Win32Exception ex)
                {
                    ex.HelpLink = "";
                }
            }

            static void OpenInWin(string path)
            {
                string text = path.Replace("/", "\\");
                bool exists = DirectoryExists(text);

                try
                {
                    Process.Start("explorer.exe", (exists ? "/root," : "/select,") + text);
                }
                catch (Win32Exception ex)
                {
                    ex.HelpLink = "";
                }
            }

            /// <summary>
            /// Opens a directory in the OS' local file explorer.
            /// </summary>
            /// <param name="path">Directory to open.</param>
            public static void Open(string path)
            {
                if (IsInWinOS)
                {
                    OpenInWin(path);
                    return;
                }

                if (IsInMacOS)
                {
                    OpenInMac(path);
                    return;
                }

                OpenInWin(path);
                OpenInMac(path);
            }

            /// <summary>
            /// Opens a file with the associated file format.
            /// </summary>
            /// <param name="path">File to open.</param>
            public static void OpenFile(string path) => Process.Start(path);
        }
    }
}
