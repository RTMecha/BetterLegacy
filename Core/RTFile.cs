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

        public static bool FileExists(string _filePath) => !string.IsNullOrEmpty(_filePath) && File.Exists(_filePath);

        public static bool DirectoryExists(string _directoryPath) => !string.IsNullOrEmpty(_directoryPath) && Directory.Exists(_directoryPath);

        public static string ValidateFileName(string name)
            => Regex.Replace(name, string.Format("([{0}]*\\.+$)|([{0}]+)", Regex.Escape(new string(Path.GetInvalidFileNameChars())) + "+?#!"), string.Empty);

        public static string ValidateDirectory(string name)
            => Regex.Replace(name, string.Format("([{0}]*\\.+$)|([{0}]+)", Regex.Escape(new string(Path.GetInvalidPathChars())) + "+?#!"), string.Empty);

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

        public static string CombinePath(string path1, string path2) => Path.Combine(path1, path2).Replace("\\", "/");
        
        public static string CombinePath(params string[] paths) => Path.Combine(paths).Replace("\\", "/");

        public static AudioType GetAudioType(string str)
        {
            var l = str.LastIndexOf('.');

            var fileType = str.Substring(l, -(l - str.Length)).ToLower();

            switch (fileType)
            {
                case ".wav":
                    {
                        return AudioType.WAV;
                    }
                case ".ogg":
                    {
                        return AudioType.OGGVORBIS;
                    }
                case ".mp3":
                    {
                        return AudioType.MPEG;
                    }
            }

            return AudioType.UNKNOWN;
        }

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
