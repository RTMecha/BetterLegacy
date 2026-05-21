using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

using UnityEngine;
using UnityEngine.Networking;

using SimpleJSON;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Menus.UI.Interfaces;

namespace BetterLegacy.Core
{
    /// <summary>
    /// General class for handling online data.
    /// </summary>
    public static class AlephNetwork
    {
        /// <summary>
        /// AlephNetwork logger name.
        /// </summary>
        public static string className = "[<color=#FC5F58>AlephNetwork</color>]\n";

        #region Links

        /// <summary>
        /// Link to the Arcade server.
        /// </summary>
        public static string ArcadeServerURL => !string.IsNullOrEmpty(Configs.CoreConfig.Instance.ArcadeServerURL.Value) ? RTFile.AppendEndSlash(Configs.CoreConfig.Instance.ArcadeServerURL.Value) : ARCADE_SERVER_URL;

        #region User

        public static string UserURL => $"{ArcadeServerURL}api/user/";

        /// <summary>
        /// User search endpoint.
        /// </summary>
        public static string UserSearchURL => $"{UserURL}search";

        #endregion

        #region Level

        /// <summary>
        /// Level URL.
        /// </summary>
        public static string LevelURL => $"{ArcadeServerURL}api/level/";

        /// <summary>
        /// Level search endpoint.
        /// </summary>
        public static string LevelSearchURL => $"{LevelURL}search";

        /// <summary>
        /// Level cover image endpoint.
        /// </summary>
        public static string LevelCoverURL => $"{LevelURL}cover/";

        /// <summary>
        /// Level zip download endpoint.
        /// </summary>
        public static string LevelDownloadURL => $"{LevelURL}zip/";

        #endregion

        #region Level Collection

        /// <summary>
        /// Level Collection URL.
        /// </summary>
        public static string LevelCollectionURL => $"{ArcadeServerURL}api/levelcollection/";

        /// <summary>
        /// Level Collection search endpoint.
        /// </summary>
        public static string LevelCollectionSearchURL => $"{LevelCollectionURL}search";

        /// <summary>
        /// Level Collection cover image endpoint.
        /// </summary>
        public static string LevelCollectionCoverURL => $"{LevelCollectionURL}cover/";

        /// <summary>
        /// Level Collection banner image endpoint.
        /// </summary>
        public static string LevelCollectionBannerURL => $"{LevelCollectionURL}banner/";

        /// <summary>
        /// Level Collection zip download endpoint.
        /// </summary>
        public static string LevelCollectionDownloadURL => $"{LevelCollectionURL}zip/";

        #endregion

        #region Prefab

        /// <summary>
        /// Prefab URL.
        /// </summary>
        public static string PrefabURL => $"{ArcadeServerURL}api/prefab/";

        /// <summary>
        /// Prefab search endpoint.
        /// </summary>
        public static string PrefabSearchURL => $"{PrefabURL}search";

        /// <summary>
        /// Prefab cover image endpoint.
        /// </summary>
        public static string PrefabCoverURL => $"{PrefabURL}cover/";

        /// <summary>
        /// Prefab download endpoint.
        /// </summary>
        public static string PrefabDownloadURL => $"{PrefabURL}download/";

        #endregion

        #region Constants

        /// <summary>
        /// Link to the Arcade server.
        /// </summary>
        public const string ARCADE_SERVER_URL = "https://betterlegacy.net/";

        /// <summary>
        /// Link to the System Error Discord server.
        /// </summary>
        public const string MOD_DISCORD_URL = "https://discord.gg/nB27X2JZcY";

        /// <summary>
        /// Link to the Project Arrhythmia mod showcases playlist on YouTube.
        /// </summary>
        public const string PA_MOD_SHOWCASES_URL = "https://www.youtube.com/playlist?list=PLMHuUok_ojlX89xw2z6hUFF3meXFXz9DL";

        /// <summary>
        /// Link to the BetterLegacy github.
        /// </summary>
        public const string OPEN_SOURCE_URL = "https://github.com/RTMecha/BetterLegacy";

        #endregion

        #endregion

        #region Link Formatting

        /// <summary>
        /// Ensures that spaces are replaced with '+' for URL compatibility.
        /// </summary>
        /// <param name="search">The query to replace.</param>
        /// <returns>Returns a proper query.</returns>
        public static string ReplaceSpace(string search) => search.ToLower().Replace(" ", "+");

        /// <summary>
        /// Formats a URL based on source and site.
        /// </summary>
        /// <param name="source">The source of the URL.</param>
        /// <param name="site">Site of the URL.</param>
        /// <param name="link">Link to format.</param>
        /// <returns>Returns a formatted URL.</returns>
        public static string GetURL(URLSource source, int site, string link)
        {
            if (string.IsNullOrEmpty(link))
                return link;

            // no sussy
            if (link.Contains("http://") || link.Contains("https://") || link.Contains("."))
                return null;

            var links = source switch
            {
                URLSource.Artist => ArtistLinks,
                URLSource.Song => SongLinks,
                URLSource.Creator => CreatorLinks,
                URLSource.Video => VideoLinks,
                _ => null,
            };

            if (links == null || !links.InRange(site))
                return null;

            var linkType = links[site];
            if (source == URLSource.Song && linkType.linkFormat.Contains("{1}"))
            {
                var split = link.Split(',');
                return string.Format(linkType.linkFormat, split[0], split[1]);
            }
            else
                return string.Format(linkType.linkFormat, link);
        }

        /// <summary>
        /// <see cref="URLSource.Song"/> list.
        /// </summary>
        public static List<DataManager.LinkType> SongLinks { get; set; }

        /// <summary>
        /// <see cref="URLSource.Artist"/> list.
        /// </summary>
        public static List<DataManager.LinkType> ArtistLinks { get; set; }

        /// <summary>
        /// <see cref="URLSource.Creator"/> list.
        /// </summary>
        public static List<DataManager.LinkType> CreatorLinks { get; set; }

        /// <summary>
        /// <see cref="URLSource.Video"/> list.
        /// </summary>
        public static List<DataManager.LinkType> VideoLinks { get; set; }

        /// <summary>
        /// Loads all link types.
        /// </summary>
        public static void LoadLinkTypes()
        {
            SongLinks = LoadLinkTypes("core/data/links/song_links.json");
            ArtistLinks = LoadLinkTypes("core/data/links/artist_links.json");
            CreatorLinks = LoadLinkTypes("core/data/links/creator_links.json");
            VideoLinks = LoadLinkTypes("core/data/links/video_links.json");
        }

        /// <summary>
        /// Loads a list of link types from an asset file.
        /// </summary>
        /// <param name="path">Asset path to the file.</param>
        /// <returns>Returns a list of the link types from the file.</returns>
        public static List<DataManager.LinkType> LoadLinkTypes(string path)
        {
            if (!AssetPack.TryReadFromFile(path, out string file))
                return new List<DataManager.LinkType>();

            var linkTypes = new List<DataManager.LinkType>();
            var jn = JSON.Parse(file);
            for (int i = 0; i < jn.Count; i++)
                linkTypes.Add(new DataManager.LinkType(jn[i]["name"], jn[i]["link_format"]));
            return linkTypes;
        }

        #endregion

        #region Functions

        /// <summary>
        /// Downloads byte data.
        /// </summary>
        /// <param name="uri">URI path.</param>
        /// <param name="callback">Callback function returns a byte array.</param>
        /// <param name="percentage">Function to run when progress is desired.</param>
        /// <param name="onError">Function to run if there were any errors.<br/>
        /// Parameters:<br/>
        /// onError<br/>
        /// responseCode<br/>
        /// errorMsg</param>
        /// <param name="headers">Headers to provide to the request.</param>
        public static IEnumerator DownloadBytes(string uri, Action<byte[]> callback, Action<float> percentage = null, Action<string, long, string> onError = null, Dictionary<string, string> headers = null)
        {
            using var www = UnityWebRequest.Get(uri);
            www.certificateHandler = new ForceAcceptAll();
            SetHeaders(www, headers);

            if (percentage == null)
                yield return www.SendWebRequest();
            else
            {
                var webRequest = www.SendWebRequest();
                while (!webRequest.isDone)
                {
                    percentage?.Invoke(webRequest.progress);
                    yield return null;
                }
            }

            if (www.isNetworkError || www.isHttpError)
                onError?.Invoke(www.error, www.responseCode, www.downloadHandler?.text);
            else
                callback?.Invoke(www.downloadHandler.data);

            yield break;
        }

        /// <summary>
        /// Uploads byte data.
        /// </summary>
        /// <param name="uri">URI path.</param>
        /// <param name="bytes">Byte data to upload.</param>
        /// <param name="percentage">Function to run when progress is desired.</param>
        /// <param name="onComplete">Function to run on upload complete.</param>
        /// <param name="onError">Function to run if there were any errors.<br/>
        /// Parameters:<br/>
        /// onError<br/>
        /// responseCode<br/>
        /// errorMsg</param>
        /// <param name="headers">Headers to provide to the request.</param>
        public static IEnumerator UploadBytes(string uri, byte[] bytes, Action<float> percentage, Action<string> onComplete, Action<string, long, string> onError, Dictionary<string, string> headers = null)
        {
            var form = new WWWForm();
            form.AddBinaryData("file", bytes);

            using var www = UnityWebRequest.Post(uri, form);
            www.certificateHandler = new ForceAcceptAll();
            SetHeaders(www, headers);

            if (percentage == null)
                yield return www.SendWebRequest();
            else
            {
                var webRequest = www.SendWebRequest();
                while (!webRequest.isDone)
                {
                    percentage?.Invoke(webRequest.progress);
                    yield return null;
                }
            }

            if (www.isNetworkError || www.isHttpError)
                onError?.Invoke(www.error, www.responseCode, www.downloadHandler?.text);
            else
                onComplete?.Invoke(www.downloadHandler.text);
        }

        /// <summary>
        /// Uploads string data.
        /// </summary>
        /// <param name="uri">URI path.</param>
        /// <param name="str">String data to upload.</param>
        /// <param name="percentage">Function to run when progress is desired.</param>
        /// <param name="onComplete">Function to run on upload complete.</param>
        /// <param name="onError">Function to run if there were any errors.<br/>
        /// Parameters:<br/>
        /// onError<br/>
        /// responseCode<br/>
        /// errorMsg</param>
        /// <param name="headers">Headers to provide to the request.</param>
        public static IEnumerator UploadString(string uri, string str, Action<float> percentage, Action<string> onComplete, Action<string, long, string> onError, Dictionary<string, string> headers = null)
        {
            using var www = UnityWebRequest.Post(uri, str);
            www.certificateHandler = new ForceAcceptAll();
            SetHeaders(www, headers);

            if (percentage == null)
                yield return www.SendWebRequest();
            else
            {
                var webRequest = www.SendWebRequest();
                while (!webRequest.isDone)
                {
                    percentage?.Invoke(webRequest.progress);
                    yield return null;
                }
            }

            if (www.isNetworkError || www.isHttpError)
                onError?.Invoke(www.error, www.responseCode, www.downloadHandler?.text);
            else
                onComplete?.Invoke(www.downloadHandler.text);
        }

        /// <summary>
        /// Downloads string data.
        /// </summary>
        /// <param name="uri">URI path.</param>
        /// <param name="callback">Callback function returns a string.</param>
        /// <param name="percentage">Function to run when progress is desired.</param>
        /// <param name="onError">Function to run if there were any errors.<br/>
        /// Parameters:<br/>
        /// onError<br/>
        /// responseCode<br/>
        /// errorMsg</param>
        /// <param name="headers">Headers to provide to the request.</param>
        public static IEnumerator DownloadJSONFile(string uri, Action<string> callback, Action<float> percentage = null, Action<string, long, string> onError = null, Dictionary<string, string> headers = null)
        {
            using var www = UnityWebRequest.Get(uri);
            www.certificateHandler = new ForceAcceptAll();
            SetHeaders(www, headers);

            if (percentage == null)
                yield return www.SendWebRequest();
            else
            {
                var webRequest = www.SendWebRequest();
                while (!webRequest.isDone)
                {
                    percentage?.Invoke(webRequest.progress);
                    yield return null;
                }
            }

            if (www.isNetworkError || www.isHttpError)
                onError?.Invoke(www.error, www.responseCode, www.downloadHandler?.text);
            else
                callback?.Invoke(www.downloadHandler?.text);
        }

        /// <summary>
        /// Downloads <see cref="Texture2D"/> data.
        /// </summary>
        /// <param name="uri">URI path.</param>
        /// <param name="callback">Callback function returns <see cref="Texture2D"/>.</param>
        /// <param name="percentage">Function to run when progress is desired.</param>
        /// <param name="onError">Function to run if there were any errors.<br/>
        /// Parameters:<br/>
        /// onError<br/>
        /// responseCode<br/>
        /// errorMsg</param>
        /// <param name="headers">Headers to provide to the request.</param>
        public static IEnumerator DownloadImageTexture(string uri, Action<Texture2D> callback, Action<float> percentage = null, Action<string, long, string> onError = null, Dictionary<string, string> headers = null)
        {
            using var www = UnityWebRequestTexture.GetTexture(uri);
            www.certificateHandler = new ForceAcceptAll();
            SetHeaders(www, headers);

            if (percentage == null)
                yield return www.SendWebRequest();
            else
            {
                var webRequest = www.SendWebRequest();
                while (!webRequest.isDone)
                {
                    percentage?.Invoke(webRequest.progress);
                    yield return null;
                }
            }

            if (www.isNetworkError || www.isHttpError)
                onError?.Invoke(www.error, www.responseCode, www.downloadHandler?.text);
            else
                callback?.Invoke(((DownloadHandlerTexture)www.downloadHandler).texture);
        }

        /// <summary>
        /// Downloads <see cref="AudioClip"/> data.
        /// </summary>
        /// <param name="uri">URI path.</param>
        /// <param name="callback">Callback function returns <see cref="AudioClip"/>.</param>
        /// <param name="percentage">Function to run when progress is desired.</param>
        /// <param name="onError">Function to run if there were any errors.<br/>
        /// Parameters:<br/>
        /// onError<br/>
        /// responseCode<br/>
        /// errorMsg</param>
        /// <param name="headers">Headers to provide to the request.</param>
        public static IEnumerator DownloadAudioClip(string uri, AudioType audioType, Action<AudioClip> callback, Action<float> percentage = null, Action<string, long, string> onError = null, Dictionary<string, string> headers = null)
        {
            using var www = UnityWebRequestMultimedia.GetAudioClip(uri, audioType);
            www.certificateHandler = new ForceAcceptAll();
            SetHeaders(www, headers);

            if (percentage == null)
                yield return www.SendWebRequest();
            else
            {
                var webRequest = www.SendWebRequest();
                while (!webRequest.isDone)
                {
                    percentage?.Invoke(webRequest.progress);
                    yield return null;
                }
            }

            if (www.isNetworkError || www.isHttpError)
                onError?.Invoke(www.error, www.responseCode, www.downloadHandler?.text);
            else
                callback?.Invoke(((DownloadHandlerAudioClip)www.downloadHandler).audioClip);
        }

        /// <summary>
        /// Downloads <see cref="AssetBundle"/> data.
        /// </summary>
        /// <param name="uri">URI path.</param>
        /// <param name="callback">Callback function returns <see cref="AssetBundle"/>.</param>
        /// <param name="percentage">Function to run when progress is desired.</param>
        /// <param name="onError">Function to run if there were any errors.<br/>
        /// Parameters:<br/>
        /// onError<br/>
        /// responseCode<br/>
        /// errorMsg</param>
        /// <param name="headers">Headers to provide to the request.</param>
        public static IEnumerator DownloadAssetBundle(string uri, Action<AssetBundle> callback, Action<float> percentage = null, Action<string, long, string> onError = null, Dictionary<string, string> headers = null)
        {
            using var www = UnityWebRequestAssetBundle.GetAssetBundle(uri);
            www.certificateHandler = new ForceAcceptAll();
            SetHeaders(www, headers);

            if (percentage == null)
                yield return www.SendWebRequest();
            else
            {
                var webRequest = www.SendWebRequest();
                while (!webRequest.isDone)
                {
                    percentage?.Invoke(webRequest.progress);
                    yield return null;
                }
            }

            if (www.isNetworkError || www.isHttpError)
                onError?.Invoke(www.error, www.responseCode, www.downloadHandler?.text);
            else
                callback?.Invoke(((DownloadHandlerAssetBundle)www.downloadHandler).assetBundle);
        }

        /// <summary>
        /// Sends a delete request.
        /// </summary>
        /// <param name="uri">URI path.</param>
        /// <param name="percentage">Function to run when progress is desired.</param>
        /// <param name="onComplete">Function to run on delete complete.</param>
        /// <param name="onError">Function to run if there were any errors.<br/>
        /// Parameters:<br/>
        /// onError<br/>
        /// responseCode<br/>
        /// errorMsg</param>
        /// <param name="headers">Headers to provide to the request.</param>
        public static IEnumerator Delete(string uri, Action<float> percentage, Action<string> onComplete, Action<string, long, string> onError = null, Dictionary<string, string> headers = null)
        {
            using var www = UnityWebRequest.Delete(uri);
            www.certificateHandler = new ForceAcceptAll();
            SetHeaders(www, headers);

            if (percentage == null)
                yield return www.SendWebRequest();
            else
            {
                var webRequest = www.SendWebRequest();
                while (!webRequest.isDone)
                {
                    percentage?.Invoke(webRequest.progress);
                    yield return null;
                }
            }

            if (www.isNetworkError || www.isHttpError)
                onError?.Invoke(www.error, www.responseCode, www.downloadHandler?.text);
            else
                onComplete?.Invoke(www.downloadHandler?.text);

            yield break;
        }

        /// <summary>
        /// Gets a response code.
        /// </summary>
        /// <param name="uri">URI path.</param>
        /// <param name="callback">Callback function returns the response code.</param>
        /// <param name="percentage">Function to run when progress is desired.</param>
        /// <param name="headers">Headers to provide to the request.</param>
        public static IEnumerator GetResponseCode(string uri, Action<long> callback, Action<float> percentage = null, Dictionary<string, string> headers = null)
        {
            using var www = UnityWebRequest.Get(uri);
            www.certificateHandler = new ForceAcceptAll();
            SetHeaders(www, headers);

            if (percentage == null)
                yield return www.SendWebRequest();
            else
            {
                var webRequest = www.SendWebRequest();
                while (!webRequest.isDone)
                {
                    percentage?.Invoke(webRequest.progress);
                    yield return null;
                }
            }

            callback?.Invoke(www.responseCode);
        }

        /// <summary>
        /// Checks if a URI exists.
        /// </summary>
        /// <param name="uri">URI path.</param>
        /// <param name="callback">Callback function returns true if the URI exists, otherwise returns false.</param>s
        /// <param name="percentage">Function to run when progress is desired.</param>
        /// <param name="headers">Headers to provide to the request.</param>
        public static IEnumerator URIExists(string uri, Action<bool> callback, Action<float> percentage = null, Dictionary<string, string> headers = null)
        {
            if (callback == null)
                yield break;

            using var www = UnityWebRequest.Get(uri);
            www.certificateHandler = new CertificateHandler();
            SetHeaders(www, headers);

            if (percentage == null)
                yield return www.SendWebRequest();
            else
            {
                var webRequest = www.SendWebRequest();
                while (!webRequest.isDone)
                {
                    percentage?.Invoke(webRequest.progress);
                    yield return null;
                }
            }

            callback.Invoke(!www.isNetworkError && !www.isHttpError);
        }

        /// <summary>
        /// Downloads a level.
        /// </summary>
        /// <param name="jn">JSON object reference.</param>
        /// <param name="onDownload">Function to run on download.</param>
        /// <param name="onError">Function to run if there were any errors.<br/>
        /// Parameters:<br/>
        /// onError<br/>
        /// responseCode<br/>
        /// errorMsg</param>
        public static void DownloadLevel(JSONObject jn, Action<Level> onDownload, Action<string, long, string> onError)
        {
            var name = jn["name"].Value;
            string id = jn["id"];
            name = RTString.ReplaceFormatting(name); // for cases where a user has used symbols not allowed.
            name = RTFile.ValidateDirectory(name);
            var directory = RTFile.CombinePaths(RTFile.ApplicationDirectory, LevelManager.ListSlash, $"{name} [{id}]");

            ProgressInterface.Init($"Downloading Arcade server level: {id} - {name}<br>Please wait...");

            CoroutineHelper.StartCoroutine(DownloadBytes($"{LevelDownloadURL}{id}{FileFormat.ZIP.Dot()}?r" + UnityRandom.Range(0, int.MaxValue),
                callback: bytes =>
                {
                    if (LevelManager.Levels.TryFindIndex(x => x.metadata.serverID == id, out int existingLevelIndex)) // prevent multiple of the same level ID
                    {
                        var existingLevel = LevelManager.Levels[existingLevelIndex];
                        RTFile.DeleteDirectory(existingLevel.path);
                        LevelManager.Levels.RemoveAt(existingLevelIndex);
                    }

                    RTFile.DeleteDirectory(directory);
                    RTFile.CreateDirectory(directory);

                    var zipFile = $"{directory}{FileFormat.ZIP.Dot()}";
                    File.WriteAllBytes(zipFile, bytes);
                    ZipFile.ExtractToDirectory(zipFile, directory);
                    RTFile.DeleteFile(zipFile);

                    var level = new Level(directory);

                    LevelManager.Levels.Add(level);

                    onDownload?.Invoke(level);
                },
                percentage: ProgressInterface.Current.UpdateProgress,
                onError: onError));
        }

        /// <summary>
        /// Downloads a level collection.
        /// </summary>
        /// <param name="jn">JSON object reference.</param>
        /// <param name="onDownload">Function to run on download.</param>
        /// <param name="onError">Function to run if there were any errors.<br/>
        /// Parameters:<br/>
        /// onError<br/>
        /// responseCode<br/>
        /// errorMsg</param>
        public static void DownloadLevelCollection(JSONObject jn, Action<LevelCollection> onDownload, Action<string, long, string> onError)
        {
            var name = jn["name"].Value;
            string id = jn["id"];
            name = RTString.ReplaceFormatting(name); // for cases where a user has used symbols not allowed.
            name = RTFile.ValidateDirectory(name);
            var directory = RTFile.CombinePaths(RTFile.ApplicationDirectory, LevelManager.ListSlash, $"{name} [{id}]");

            ProgressInterface.Init($"Downloading Arcade server level collection: {id} - {name}<br>Please wait...");

            CoroutineHelper.StartCoroutine(DownloadBytes($"{LevelCollectionDownloadURL}{id}{FileFormat.ZIP.Dot()}?r" + UnityRandom.Range(0, int.MaxValue),
                callback: bytes =>
                {
                    if (LevelManager.LevelCollections.TryFindIndex(x => x.serverID == id, out int existingLevelIndex)) // prevent multiple of the same level ID
                    {
                        var existingLevel = LevelManager.LevelCollections[existingLevelIndex];
                        RTFile.DeleteDirectory(existingLevel.path);
                        LevelManager.LevelCollections.RemoveAt(existingLevelIndex);
                    }

                    RTFile.DeleteDirectory(directory);
                    RTFile.CreateDirectory(directory);

                    var zipFile = $"{directory}{FileFormat.ZIP.Dot()}";
                    File.WriteAllBytes(zipFile, bytes);
                    ZipFile.ExtractToDirectory(zipFile, directory);
                    RTFile.DeleteFile(zipFile);

                    var name = Path.GetFileName(directory);

                    var levelCollection = LevelCollection.Parse(directory, JSON.Parse(RTFile.ReadFromFile(RTFile.CombinePaths(directory, LevelCollection.COLLECTION_LSCO))));
                    LevelManager.LevelCollections.Add(levelCollection);

                    onDownload?.Invoke(levelCollection);
                },
                percentage: ProgressInterface.Current.UpdateProgress,
                onError: onError));
        }

        /// <summary>
        /// Gets headers based on the current user.
        /// </summary>
        /// <returns>Returns a dictionary of headers.</returns>
        public static Dictionary<string, string> GetUserHeaders()
        {
            var headers = new Dictionary<string, string>();
            if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null)
                headers["Authorization"] = $"Bearer {LegacyPlugin.authData["access_token"].Value}";
            return headers;
        }

        /// <summary>
        /// Applies the headers from <paramref name="headers"/> to <paramref name="www"/>.
        /// </summary>
        /// <param name="www"><see cref="UnityWebRequest"/> to apply the headers to.</param>
        /// <param name="headers">Headers dictionary.</param>
        public static void SetHeaders(UnityWebRequest www, Dictionary<string, string> headers)
        {
            if (headers != null)
                foreach (var header in headers)
                    www.SetRequestHeader(header.Key, header.Value);
        }

        #endregion

        public class ForceAcceptAll : CertificateHandler
        {
            public override bool ValidateCertificate(byte[] certificateData) => true;
        }
    }
}
