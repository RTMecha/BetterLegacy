using SimpleJSON;
using System.Collections;

using UnityEngine;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class SoundAsset : PAObject<SoundAsset>
    {
        public SoundAsset() : base() { }

        public SoundAsset(string name, AudioClip audio) : this()
        {
            this.name = name;
            this.audio = audio;
        }

        /// <summary>
        /// Name of the audio.
        /// </summary>
        public string name = string.Empty;

        /// <summary>
        /// Contained audio.
        /// </summary>
        public AudioClip audio;

        public override void CopyData(SoundAsset orig, bool newID = true)
        {
            id = newID ? GetStringID() : orig.id;
            name = orig.name ?? string.Empty;
            audio = orig.audio;
        }

        public override void ReadJSON(JSONNode jn)
        {
            id = jn["id"] ?? GetStringID();
            name = jn["n"] ?? string.Empty;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["id"] = id ?? GetStringID();
            jn["n"] = name ?? string.Empty;

            return jn;
        }

        public IEnumerator LoadAudioClip()
        {
            if (audio)
                yield break;

            var path = RTFile.CombinePaths(RTFile.BasePath, name);
            yield return CoroutineHelper.StartCoroutine(AlephNetwork.DownloadAudioClip("file://" + path, RTFile.GetAudioType(name), audioClip =>
            {
                audio = audioClip;
            }));
        }
    }
}
