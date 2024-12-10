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
    public static class RTFile
    {
        public static string ApplicationDirectory => Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("/")) + "/";

        public static string PersistentApplicationDirectory => Application.persistentDataPath;

        public static string BasePath => GameManager.inst.basePath;

        /// <summary>
        /// Path where all the plugins are stored.
        /// </summary>
        public static string BepInExPluginsPath => "BepInEx/plugins/";

        /// <summary>
        /// Path where all the mod-specific assets are stored.
        /// </summary>
        public static string BepInExAssetsPath => $"{BepInExPluginsPath}Assets/";

        public static bool CopyFile(string filePath, string destination)
        {
            if (!string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(destination) && filePath.ToLower() != destination.ToLower())
            {
                try
                {
                    File.Copy(filePath, destination, true);
                    return true;
                }
                catch (Exception ex)
                {
                    CoreHelper.LogException(ex);
                }
            }
            return false;
        }

        public static bool FileExists(string _filePath) => !string.IsNullOrEmpty(_filePath) && File.Exists(_filePath);

        public static bool DirectoryExists(string _directoryPath) => !string.IsNullOrEmpty(_directoryPath) && Directory.Exists(_directoryPath);

        public static void DeleteFile(string path)
        {
            if (FileExists(path))
                File.Delete(path);
        }

        public static string ValidateFileName(string name)
            => Regex.Replace(name, string.Format("([{0}]*\\.+$)|([{0}]+)", Regex.Escape(new string(Path.GetInvalidFileNameChars())) + "+?#!"), string.Empty);

        public static string ValidateDirectory(string name)
            => Regex.Replace(name, string.Format("([{0}]*\\.+$)|([{0}]+)", Regex.Escape(new string(Path.GetInvalidPathChars())) + "+?#!"), string.Empty);

        /// <summary>
        /// Used for setting the file name of a Legacy file. E.G. "Para Template" > "para_template".
        /// </summary>
        /// <param name="name">Name to format.</param>
        /// <returns>Returns a Legacy file name.</returns>
        public static string FormatLegacyFileName(string name) => ValidateFileName(RTString.ReplaceSpace(name.ToLower()));

        public static string ParsePaths(string str) => string.IsNullOrEmpty(str) ? str : str
            .Replace("{{AppDirectory}}", ApplicationDirectory)
            .Replace("{{BepInExAssetsDirectory}}", BepInExAssetsPath)
            .Replace("{{LevelPath}}", GameManager.inst ? BasePath : ApplicationDirectory);

        /// <summary>
        /// Checks if a directory doesn't exist and if it doesn't, creates a directory.
        /// </summary>
        /// <param name="path">Directory to create.</param>
        public static void CreateDirectory(string path)
        {
            if (!DirectoryExists(path))
                Directory.CreateDirectory(path);
        }

        public static void WriteToFile(string path, string json)
        {
            using var streamWriter = new StreamWriter(path);
            streamWriter.Write(json);
        }

        public static void WriteToFile<T>(string path, T obj)
        {
            var binaryFormatter = new BinaryFormatter();
            using var fileStream = File.Create(path);
            binaryFormatter.Serialize(fileStream, obj);
        }

        public static string ReadFromFile(string path)
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

        public static T ReadFromFile<T>(string path)
        {
            if (!FileExists(path))
            {
                CoreHelper.Log($"Could not load file [{path}]");
                return default;
            }

            var binaryFormatter = new BinaryFormatter();
            using var fileStream = File.Open(path, FileMode.Open);
            return (T)binaryFormatter.Deserialize(fileStream);
        }

        public static string CombinePaths(string path1, string path2) => Path.Combine(path1, path2).Replace("\\", "/");
        public static string CombinePaths(params string[] paths) => Path.Combine(paths).Replace("\\", "/");

        public static string AppendEndSlash(string path) => string.IsNullOrEmpty(path) ? path : path[path.Length - 1] != '/' ? path + "/" : path;

        public static string RemoveEndSlash(string path) => path == null || path.Length == 0 ? path : path[path.Length - 1] == '/' ? path.Substring(0, path.Length - 1) : path;

        public static AudioType GetAudioType(string path) => GetFileFormat(path) switch
        {
            FileFormat.WAV => AudioType.WAV,
            FileFormat.OGG => AudioType.OGGVORBIS,
            FileFormat.MP3 => AudioType.MPEG,
            _ => AudioType.UNKNOWN,
        };

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

        #region File Formats

        public static string[] AudioDotFormats => DotFormats(FileFormat.OGG, FileFormat.WAV, FileFormat.MP3);

        public static string[] DotFormats(params FileFormat[] fileFormats)
        {
            var array = new string[fileFormats.Length];
            for (int i = 0; i < array.Length; i++)
                array[i] = fileFormats[i].Dot();
            return array;
        }

        /// <summary>
        /// Ensures a path ends with the specified file format.
        /// </summary>
        /// <param name="path">Path to append a file format to.</param>
        /// <param name="fileFormats">The file format.</param>
        /// <returns>Returns a path with an appended file format.</returns>
        public static string AppendFormat(string path, FileFormat fileFormats) => string.IsNullOrEmpty(path) ? path : AppendDotFormat(path, fileFormats.ToString().ToLower());

        public static string AppendDotFormat(string path, string format) => path.Contains("." + format) ? path : path.EndsWith(".") ? path + format : path + "." + format;

        /// <summary>
        /// Gets a <see cref="FileFormat"/> from a path.
        /// </summary>
        /// <param name="path">Path to get a file format from.</param>
        /// <returns>Returns a parsed <see cref="FileFormat"/>.</returns>
        public static FileFormat GetFileFormat(string path) => string.IsNullOrEmpty(path) || !path.Contains(".") ? FileFormat.NULL : Parser.TryParse(Path.GetExtension(path).Remove("."), true, FileFormat.NULL);

        public static bool FileIsAudio(string path) => GetAudioType(path) != AudioType.UNKNOWN;

        #endregion

        public static class OpenInFileBrowser
        {
            public static bool IsInMacOS => SystemInfo.operatingSystem.IndexOf("Mac OS") != -1;

            public static bool IsInWinOS => SystemInfo.operatingSystem.IndexOf("Windows") != -1;

            public static void OpenInMac(string path)
            {
                string text = path.Replace("\\", "/");
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

            public static void OpenInWin(string path)
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

            public static void OpenFile(string path) => Process.Start(path);
        }
    }
}
