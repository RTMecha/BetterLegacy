using System;
using System.Collections;

using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Stores sound data that can be reused.
    /// </summary>
    public class SoundAsset : PAObject<SoundAsset>
    {
        public SoundAsset() : base() { }

        public SoundAsset(string name) : this() => this.name = name;

        public SoundAsset(string name, AudioClip audio) : this(name) => this.audio = audio;

        /// <summary>
        /// Name of the audio.
        /// </summary>
        public string name = string.Empty;

        /// <summary>
        /// Contained audio.
        /// </summary>
        public AudioClip audio;

        /// <summary>
        /// If the audio should automatically load on start.
        /// </summary>
        public bool autoLoad = true;

        public override void CopyData(SoundAsset orig, bool newID = true)
        {
            id = newID ? GetStringID() : orig.id;
            name = orig.name ?? string.Empty;
            audio = orig.audio;
            autoLoad = orig.autoLoad;
        }

        public override void ReadJSON(JSONNode jn)
        {
            id = jn["id"] ?? GetStringID();
            name = jn["n"] ?? string.Empty;
            if (jn["al"] != null)
                autoLoad = jn["al"].AsBool;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["id"] = id ?? GetStringID();
            jn["n"] = name ?? string.Empty;
            if (!autoLoad)
                jn["al"] = autoLoad;

            return jn;
        }

        /// <summary>
        /// Loads the associated sound asset.
        /// </summary>
        public IEnumerator LoadAudioClip(Action onComplete = null)
        {
            if (audio)
                yield break;

            var path = RTFile.CombinePaths(RTFile.BasePath, name);
            if (RTFile.FileExists(path + FileFormat.OGG.Dot()))
                path += FileFormat.OGG.Dot();
            if (RTFile.FileExists(path + FileFormat.WAV.Dot()))
                path += FileFormat.WAV.Dot();
            if (RTFile.FileExists(path + FileFormat.MP3.Dot()))
                path += FileFormat.MP3.Dot();

            if (!RTFile.FileExists(path))
            {
                onComplete?.Invoke();
                yield break;
            }

            yield return CoroutineHelper.StartCoroutine(AlephNetwork.DownloadAudioClip("file://" + path, RTFile.GetAudioType(path), audioClip =>
            {
                audio = audioClip;
                onComplete?.Invoke();
            }, error =>
            {
                onComplete?.Invoke();
            }));
        }

        /// <summary>
        /// Unloads the associated sound asset.
        /// </summary>
        public void UnloadAudioClip()
        {
            if (audio)
                CoreHelper.Destroy(audio);
            audio = null;
        }

        public override string ToString() => name;
    }
}
