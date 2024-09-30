using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Networking;

namespace BetterLegacy.Core.Managers.Networking
{
    /// <summary>
    /// General class for handling online data.
    /// </summary>
    public class AlephNetworkManager : MonoBehaviour
    {
        public static AlephNetworkManager inst;
        public static string className = "[<color=#FC5F58>AlephNetworkManager</color>]\n";

        public static void Init()
        {
            var gameObject = new GameObject(nameof(AlephNetworkManager));
            gameObject.transform.SetParent(SystemManager.inst.transform);
            gameObject.AddComponent<AlephNetworkManager>();
            gameObject.AddComponent<AlephNetworkEditorManager>();
        }

        void Awake() => inst = this;

        public static bool ServerFinished => true;

        public static string ArcadeServerURL => "https://betterlegacy.net/";

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

        /// <summary>
        /// Ensures that spaces are replaced with '+' for URL compatibility.
        /// </summary>
        /// <param name="search">The query to replace.</param>
        /// <returns>Returns a proper query.</returns>
        public static string ReplaceSpace(string search) => search.ToLower().Replace(" ", "+");

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

        public static IEnumerator DownloadJSONFile(string path, Action<float> percentage, Action<string> callback, Action<string> onError)
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
                Debug.LogError($"{className}Error: {www.error}\nMessage: {www.downloadHandler.text}");
                onError?.Invoke(www.error);
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
            {
                Debug.LogError($"{className}Error: {www.error}\nMessage: {www.downloadHandler.text}");
                onError?.Invoke(www.error);
            }
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

        public class ForceAcceptAll : CertificateHandler
        {
            public override bool ValidateCertificate(byte[] certificateData)
            {
                return true;
            }
        }
    }
}
