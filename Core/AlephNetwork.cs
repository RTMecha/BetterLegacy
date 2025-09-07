using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Networking;

using SimpleJSON;

using BetterLegacy.Arcade.Interfaces;
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

        /// <summary>
        /// Link to the Arcade server.
        /// </summary>
        public static string ArcadeServerURL => !string.IsNullOrEmpty(Configs.CoreConfig.Instance.ArcadeServerURL.Value) ? RTFile.AppendEndSlash(Configs.CoreConfig.Instance.ArcadeServerURL.Value) : ARCADE_SERVER_URL;

        public static string LevelURL => $"{ArcadeServerURL}api/level/";
        public static string LevelSearchURL => $"{LevelURL}search";
        public static string LevelCoverURL => $"{LevelURL}cover/";
        public static string LevelDownloadURL => $"{LevelURL}zip/";
        
        public static string LevelCollectionURL => $"{ArcadeServerURL}api/levelcollection/";
        public static string LevelCollectionSearchURL => $"{LevelCollectionURL}search";
        public static string LevelCollectionCoverURL => $"{LevelCollectionURL}cover/";
        public static string LevelCollectionBannerURL => $"{LevelCollectionURL}banner/";
        public static string LevelCollectionDownloadURL => $"{LevelCollectionURL}zip/";

        public static string PrefabURL => $"{ArcadeServerURL}api/prefab/";
        public static string PrefabSearchURL => $"{PrefabURL}search";
        public static string PrefabCoverURL => $"{PrefabURL}cover/";
        public static string PrefabDownloadURL => $"{PrefabURL}download/";

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

            var linkFormat = links[site];
            if (source == URLSource.Song && linkFormat.linkFormat.Contains("{1}"))
            {
                var split = link.Split(',');
                return string.Format(linkFormat.linkFormat, split[0], split[1]);
            }
            else
                return string.Format(linkFormat.linkFormat, link);
        }

        /// <summary>
        /// <see cref="URLSource.Song"/> list.
        /// </summary>
        public static List<DataManager.LinkType> SongLinks { get; } = new List<DataManager.LinkType>
        {
            new DataManager.LinkType("Spotify", "https://open.spotify.com/{0}"),
            new DataManager.LinkType("SoundCloud", "https://soundcloud.com/{0}"),
            new DataManager.LinkType("Bandcamp", "https://{0}.bandcamp.com/{1}"),
            new DataManager.LinkType("YouTube", "https://youtube.com/watch?v={0}"),
            new DataManager.LinkType("Newgrounds", "https://newgrounds.com/audio/listen/{0}"),
        };

        /// <summary>
        /// <see cref="URLSource.Artist"/> list.
        /// </summary>
        public static List<DataManager.LinkType> ArtistLinks { get; } = new List<DataManager.LinkType>
        {
            new DataManager.LinkType("Spotify", "https://open.spotify.com/artist/{0}"),
            new DataManager.LinkType("SoundCloud", "https://soundcloud.com/{0}"),
            new DataManager.LinkType("Bandcamp", "https://{0}.bandcamp.com"),
            new DataManager.LinkType("YouTube", "https://youtube.com/c/{0}"),
            new DataManager.LinkType("Newgrounds", "https://{0}.newgrounds.com/"),
        };

        /// <summary>
        /// <see cref="URLSource.Creator"/> list.
        /// </summary>
        public static List<DataManager.LinkType> CreatorLinks { get; } = new List<DataManager.LinkType>
        {
            new DataManager.LinkType("YouTube", "https://youtube.com/c/{0}"),
            new DataManager.LinkType("Newgrounds", "https://{0}.newgrounds.com/"),
            new DataManager.LinkType("Discord", "https://discord.gg/{0}"),
            new DataManager.LinkType("Patreon", "https://patreon.com/{0}"),
            new DataManager.LinkType("Twitter", "https://twitter.com/{0}"),
        };

        /// <summary>
        /// <see cref="URLSource.Video"/> list.
        /// </summary>
        public static List<DataManager.LinkType> VideoLinks { get; } = new List<DataManager.LinkType>
        {
            new DataManager.LinkType("YouTube", "https://www.youtube.com/watch?v={0}"),
            new DataManager.LinkType("Streamable", "https://streamable.com/{0}"),
        };

        #endregion

        #region Client

        public static IEnumerator DownloadClient(string path, Action<byte[]> callback)
        {
            using var client = new WebClient();
            var bytes = client.DownloadData(path);
            while (client.IsBusy)
                yield return null;

            callback?.Invoke(bytes);
            yield break;
        }

        public static IEnumerator DownloadClient(string path, Action<string> callback)
        {
            using var client = new WebClient();
            var bytes = client.DownloadString(path);
            while (client.IsBusy)
                yield return null;

            callback?.Invoke(bytes);
            yield break;
        }

        #endregion

        #region Bytes

        public static IEnumerator DownloadBytes(string path, Action<byte[]> callback, Action<float> percentage, Action<string> onError)
        {
            using var www = UnityWebRequest.Get(path);
            www.certificateHandler = new ForceAcceptAll();
            var webRequest = www.SendWebRequest();

            while (!webRequest.isDone)
            {
                percentage?.Invoke(webRequest.progress);
                yield return null;
            }

            if (www.isNetworkError || www.isHttpError)
                onError?.Invoke(www.error);
            else
                callback?.Invoke(www.downloadHandler.data);

            yield break;
        }

        public static IEnumerator DownloadBytes(string path, Action<byte[]> callback, Action<string> onError)
        {
            using var www = UnityWebRequest.Get(path);
            www.certificateHandler = new ForceAcceptAll();
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
                onError?.Invoke(www.error);
            else
                callback?.Invoke(www.downloadHandler.data);

            yield break;
        }

        public static IEnumerator DownloadBytes(string path, Action<byte[]> callback)
        {
            using var www = UnityWebRequest.Get(path);
            www.certificateHandler = new ForceAcceptAll();
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
                Debug.LogError($"{className}Error: {www.error}\nMessage: {www.downloadHandler.text}");
            else
                callback?.Invoke(www.downloadHandler.data);
        }

        public static IEnumerator UploadBytes(string url, byte[] bytes)
        {
            var form = new WWWForm();
            form.AddBinaryData("file", bytes);

            using var www = UnityWebRequest.Post(url, form);

            www.certificateHandler = new ForceAcceptAll();
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
                Debug.LogError($"{className}Error: {www.error}\nMessage: {www.downloadHandler.text}");
            else
                Debug.Log($"{className}Form upload complete! {www.downloadHandler.text}");
        }

        public static IEnumerator UploadBytes(string url, byte[] bytes, Action<string> onComplete)
        {
            var form = new WWWForm();
            form.AddBinaryData("file", bytes);

            using var www = UnityWebRequest.Post(url, form);

            www.certificateHandler = new ForceAcceptAll();
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
                Debug.LogError($"{className}Error: {www.error}\nMessage: {www.downloadHandler.text}");
            else
                onComplete?.Invoke(www.downloadHandler.text);
        }

        public static IEnumerator UploadBytes(string url, byte[] bytes, Action<string> onComplete, Action<string, long, string> onError, Dictionary<string, string> headers = null)
        {
            var form = new WWWForm();
            form.AddBinaryData("file", bytes);

            using var www = UnityWebRequest.Post(url, form);

            www.certificateHandler = new ForceAcceptAll();
            
            if (headers != null)
                foreach (var header in headers)
                    www.SetRequestHeader(header.Key, header.Value);
            
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
                onError?.Invoke(www.error, www.responseCode, www.downloadHandler?.text);
            else
                onComplete?.Invoke(www.downloadHandler.text);
        }

        public static IEnumerator UploadBytes(string url, byte[] bytes, Action<float> percentage, Action<string> onComplete, Action<string, long> onError)
        {
            var form = new WWWForm();
            form.AddBinaryData("file", bytes);

            using var www = UnityWebRequest.Post(url, form);

            www.certificateHandler = new ForceAcceptAll();

            var webRequest = www.SendWebRequest();

            while (!webRequest.isDone)
            {
                percentage?.Invoke(webRequest.progress);
                yield return null;
            }

            if (www.isNetworkError || www.isHttpError)
                onError?.Invoke(www.error, www.responseCode);
            else
                onComplete?.Invoke(www.downloadHandler.text);
        }

        public static IEnumerator UploadString(string url, string str)
        {
            using var www = UnityWebRequest.Post(url, str);

            www.certificateHandler = new ForceAcceptAll();
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
                Debug.LogError($"{className}Error: {www.error}\nMessage: {www.downloadHandler.text}");
            else
                Debug.Log($"{className}Form upload complete! {www.downloadHandler.text}");
        }

        public static IEnumerator DownloadBytes(string path, Action<float> percentage, Action<byte[]> callback, Action<string> onError)
        {
            using var www = UnityWebRequest.Get(path);

            www.certificateHandler = new ForceAcceptAll();
            var webRequest = www.SendWebRequest();

            while (!webRequest.isDone)
            {
                percentage?.Invoke(webRequest.progress);
                yield return null;
            }

            if (www.isNetworkError || www.isHttpError)
                onError?.Invoke(www.error);
            else
                callback?.Invoke(www.downloadHandler.data);

            yield break;
        }

        #endregion

        #region JSON

        public static IEnumerator DownloadJSONFile(string path, Action<string> callback, Action<string> onError)
        {
            using var www = UnityWebRequest.Get(path);
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError($"{className}Error: {www.error}\nMessage: {www.downloadHandler.text}");
                onError?.Invoke(www.error);
            }
            else
                callback?.Invoke(www.downloadHandler.text);

        }

        public static IEnumerator DownloadJSONFile(string path, Action<string> callback, Dictionary<string, string> headers = null)
        {
            using var www = UnityWebRequest.Get(path);
            www.certificateHandler = new ForceAcceptAll();

            if (headers != null)
                foreach (var header in headers)
                    www.SetRequestHeader(header.Key, header.Value);

            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
                Debug.LogError($"{className}Error: {www.error}\nMessage: {www.downloadHandler.text}");
            else
                callback?.Invoke(www.downloadHandler.text);

            yield break;
        }

        public static IEnumerator DownloadJSONFile(string path, Action<string> callback, Action<string, long, string> onError, Dictionary<string, string> headers = null)
        {
            using var www = UnityWebRequest.Get(path);
            www.certificateHandler = new ForceAcceptAll();

            if (headers != null)
                foreach (var header in headers)
                    www.SetRequestHeader(header.Key, header.Value);

            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
            {
                if (www.downloadHandler != null)
                    onError?.Invoke(www.error, www.responseCode, www.downloadHandler.text);
                else
                    onError?.Invoke(www.error, www.responseCode, null);
            }
            else
                callback?.Invoke(www.downloadHandler.text);

            yield break;
        }
        
        public static IEnumerator DownloadJSONFile(string path, Action<float> percentage, Action<string> callback, Action<string, string> onError, Dictionary<string, string> headers = null)
        {
            using var www = UnityWebRequest.Get(path);
            www.certificateHandler = new ForceAcceptAll();

            if (headers != null)
                foreach (var header in headers)
                    www.SetRequestHeader(header.Key, header.Value);

            var webRequest = www.SendWebRequest();

            while (!webRequest.isDone)
            {
                percentage?.Invoke(webRequest.progress);
                yield return null;
            }

            if (www.isNetworkError || www.isHttpError)
            {
                if (www.downloadHandler != null)
                    onError?.Invoke(www.error, www.downloadHandler.text);
                else
                    onError?.Invoke(www.error, null);
            }
            else
                callback?.Invoke(www.downloadHandler.text);

            yield break;
        }

        public static IEnumerator DownloadJSONFile(string path, Action<float> percentage, Action<string> callback, Action<string, string> onError)
        {
            using var www = UnityWebRequest.Get(path);
            var webRequest = www.SendWebRequest();

            while (!webRequest.isDone)
            {
                percentage?.Invoke(webRequest.progress);
                yield return null;
            }

            if (www.isNetworkError || www.isHttpError)
            {
                if (www.downloadHandler != null)
                    onError?.Invoke(www.error, www.downloadHandler.text);
                else
                    onError?.Invoke(www.error, null);
            }
            else
                callback?.Invoke(www.downloadHandler.text);

            yield break;
        }

        #endregion

        #region Image

        public static IEnumerator DownloadImageTexture(string path, Action<Texture2D> callback, Action<string, string> onError)
        {
            using var www = UnityWebRequestTexture.GetTexture(path);
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
                onError?.Invoke(www.error, www.downloadHandler?.text);
            else
                callback?.Invoke(((DownloadHandlerTexture)www.downloadHandler).texture);
        }
        
        public static IEnumerator DownloadImageTexture(string path, Action<Texture2D> callback, Action<string> onError)
        {
            using var www = UnityWebRequestTexture.GetTexture(path);
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
                onError?.Invoke(www.error);
            else
                callback?.Invoke(((DownloadHandlerTexture)www.downloadHandler).texture);
        }

        public static IEnumerator DownloadImageTexture(string path, Action<Texture2D> callback)
        {
            using var www = UnityWebRequestTexture.GetTexture(path);
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
                Debug.LogError($"{className}Error: {www.error}\nMessage: {www.downloadHandler.text}");
            else
                callback?.Invoke(((DownloadHandlerTexture)www.downloadHandler).texture);
        }

        public static IEnumerator DownloadImageTexture(string path, Action<float> percentage, Action<Texture2D> callback, Action<string> onError)
        {
            using var www = UnityWebRequestTexture.GetTexture(path);
            var webRequest = www.SendWebRequest();

            while (!webRequest.isDone)
            {
                percentage?.Invoke(webRequest.progress);
                yield return null;
            }

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError($"{className}Error: {www.error}\nMessage: {www.downloadHandler.text}");
                onError?.Invoke(www.error);
            }
            else
                callback?.Invoke(((DownloadHandlerTexture)www.downloadHandler).texture);
        }

        #endregion

        #region AudioClip

        public static IEnumerator DownloadAudioClip(string path, AudioType audioType, Action<AudioClip> callback, Action<string> onError)
        {
            using var www = UnityWebRequestMultimedia.GetAudioClip(path, audioType);
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError($"{className}Error: {www.error}");
                onError?.Invoke(www.error);
            }
            else
                callback?.Invoke(((DownloadHandlerAudioClip)www.downloadHandler).audioClip);
        }

        public static IEnumerator DownloadAudioClip(string path, AudioType audioType, Action<AudioClip> callback)
        {
            using var www = UnityWebRequestMultimedia.GetAudioClip(path, audioType);
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
                Debug.LogError($"{className}Error: {www.error}");
            else
                callback?.Invoke(((DownloadHandlerAudioClip)www.downloadHandler).audioClip);
        }

        public static IEnumerator DownloadAudioClip(string path, Action<float> percentage, AudioType audioType, Action<AudioClip> callback, Action<string> onError)
        {
            using var www = UnityWebRequestMultimedia.GetAudioClip(path, audioType);
            var webRequest = www.SendWebRequest();

            while (!webRequest.isDone)
            {
                percentage?.Invoke(webRequest.progress);
                yield return null;
            }

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError($"{className}Error: {www.error}");
                onError?.Invoke(www.error);
            }
            else
                callback?.Invoke(((DownloadHandlerAudioClip)www.downloadHandler).audioClip);
        }

        #endregion

        #region AssetBundle

        public static IEnumerator DownloadAssetBundle(string path, Action<AssetBundle> callback, Action<string> onError)
        {
            using var www = UnityWebRequestAssetBundle.GetAssetBundle(path);
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError($"{className}Error: {www.error}\nMessage: {www.downloadHandler.text}");
                onError?.Invoke(www.error);
            }
            else
                callback?.Invoke(((DownloadHandlerAssetBundle)www.downloadHandler).assetBundle);
        }

        public static IEnumerator DownloadAssetBundle(string path, Action<AssetBundle> callback)
        {
            using var www = UnityWebRequestAssetBundle.GetAssetBundle(path);
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
                Debug.LogError($"{className}Error: {www.error}\nMessage: {www.downloadHandler.text}");
            else
                callback?.Invoke(((DownloadHandlerAssetBundle)www.downloadHandler).assetBundle);
        }

        public static IEnumerator DownloadAssetBundle(string path, Action<float> percentage, Action<AssetBundle> callback, Action<string> onError)
        {
            using var www = UnityWebRequestAssetBundle.GetAssetBundle(path);
            var webRequest = www.SendWebRequest();

            while (!webRequest.isDone)
            {
                percentage?.Invoke(webRequest.progress);
                yield return null;
            }

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError($"{className}Error: {www.error}\nMessage: {www.downloadHandler.text}");
                onError?.Invoke(www.error);
            }
            else
                callback?.Invoke(((DownloadHandlerAssetBundle)www.downloadHandler).assetBundle);
        }

        #endregion

        #region Misc

        public static IEnumerator Delete(string path, Action onComplete, Action<string, long> onError, Dictionary<string, string> headers = null)
        {
            using var www = UnityWebRequest.Delete(path);
            www.certificateHandler = new ForceAcceptAll();

            if (headers != null)
                foreach (var header in headers)
                    www.SetRequestHeader(header.Key, header.Value);

            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
                onError?.Invoke(www.error, www.responseCode);
            else
                onComplete?.Invoke();

            yield break;
        }

        public static void DownloadLevel(JSONObject jn, Action<Level> onDownload, Action<string> onError)
        {
            var name = jn["name"].Value;
            string id = jn["id"];
            name = RTString.ReplaceFormatting(name); // for cases where a user has used symbols not allowed.
            name = RTFile.ValidateDirectory(name);
            var directory = RTFile.CombinePaths(RTFile.ApplicationDirectory, LevelManager.ListSlash, $"{name} [{id}]");

            ProgressMenu.Init($"Downloading Arcade server level: {id} - {name}<br>Please wait...");

            CoroutineHelper.StartCoroutine(DownloadBytes($"{LevelDownloadURL}{id}{FileFormat.ZIP.Dot()}", ProgressMenu.Current.UpdateProgress, bytes =>
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
            }, onError));
        }

        public static void DownloadLevelCollection(JSONObject jn, Action<LevelCollection> onDownload, Action<string> onError)
        {
            var name = jn["name"].Value;
            string id = jn["id"];
            name = RTString.ReplaceFormatting(name); // for cases where a user has used symbols not allowed.
            name = RTFile.ValidateDirectory(name);
            var directory = RTFile.CombinePaths(RTFile.ApplicationDirectory, LevelManager.ListSlash, $"{name} [{id}]");

            ProgressMenu.Init($"Downloading Arcade server level collection: {id} - {name}<br>Please wait...");

            CoroutineHelper.StartCoroutine(DownloadBytes($"{LevelCollectionDownloadURL}{id}{FileFormat.ZIP.Dot()}", ProgressMenu.Current.UpdateProgress, bytes =>
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
            }, onError));
        }

        public static IEnumerator GetResponseCode(string url, Action<long> result)
        {
            using var www = UnityWebRequest.Get(url);
            www.certificateHandler = new ForceAcceptAll();
            yield return www.SendWebRequest();
            result?.Invoke(www.responseCode);
        }

        public static async Task<bool> URLExistsAsync(string url) => await Task.Run(() =>
        {
            using var www = UnityWebRequest.Get(url);
            www.certificateHandler = new ForceAcceptAll();

            var webRequest = www.SendWebRequest();

            while (!webRequest.isDone)
                Thread.Sleep(1);

            return !www.isNetworkError && !www.isHttpError;
        });

        /// <summary>
        /// Builds a search query.
        /// </summary>
        /// <param name="searchURL">URL to append.</param>
        /// <param name="queries">Array of queries.</param>
        /// <returns>Returns the built query.</returns>
        public static string BuildQuery(string searchURL, List<Query> queries)
        {
            if (!queries.IsEmpty())
                searchURL += "?";

            for (int i = 0; i < queries.Count; i++)
            {
                searchURL += queries[i].ToString();
                if (i < queries.Count - 1)
                    searchURL += "&";
            }

            return searchURL;
        }

        /// <summary>
        /// Builds a search query.
        /// </summary>
        /// <param name="searchURL">URL to append.</param>
        /// <param name="queries">Array of queries.</param>
        /// <returns>Returns the built query.</returns>
        public static string BuildQuery(string searchURL, params Query[] queries)
        {
            if (!queries.IsEmpty())
                searchURL += "?";

            for (int i = 0; i < queries.Length; i++)
            {
                searchURL += queries[i].ToString();
                if (i < queries.Length - 1)
                    searchURL += "&";
            }

            return searchURL;
        }

        public static string BuildQuery(string searchURL, string search, int page = 0, int sort = 0, bool ascend = false)
        {
            var queries = new List<Query>();
            if (!string.IsNullOrEmpty(search))
                queries.Add(new Query("query", search));
            if (page != 0)
                queries.Add(new Query("page", page.ToString()));

            // todo: implement sorting
            if (sort != 0)
                queries.Add(new Query("sort", sort.ToString()));
            if (ascend)
                queries.Add(new Query("ascend", ascend.ToString()));

            return BuildQuery(searchURL, queries);
        }

        #endregion

        public class ForceAcceptAll : CertificateHandler
        {
            public override bool ValidateCertificate(byte[] certificateData) => true;
        }

        /// <summary>
        /// Query used for search URLs.
        /// </summary>
        public class Query
        {
            public Query(string name, string variable)
            {
                this.name = name;
                this.variable = variable;
            }

            /// <summary>
            /// Name of the query.
            /// </summary>
            public string name;

            /// <summary>
            /// Variable of the query.
            /// </summary>
            public string variable;

            public override string ToString() => $"{name}={variable}";
        }
    }
}
