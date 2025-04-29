using UnityEngine;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Runtime;

namespace BetterLegacy.Core.Components
{
    /// <summary>
    /// Component used for audioSource modifier.
    /// </summary>
    public class AudioModifier : MonoBehaviour
    {
        /// <summary>
        /// Assigns audio, a BeatmapObject and modifier.
        /// </summary>
        public void Init(AudioClip audioClip, BeatmapObject beatmapObject, Modifier<BeatmapObject> modifier)
        {
            AudioClip = audioClip;
            BeatmapObject = beatmapObject;
            Modifier = modifier;
            AudioSource = gameObject.GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            AudioSource.loop = true;
            AudioSource.clip = AudioClip;

            (gameObject.GetComponent<DestroyModifierResult>() ?? gameObject.AddComponent<DestroyModifierResult>()).Modifier = modifier;
        }

        void Update()
        {
            if (!AudioSource || !BeatmapObject || BeatmapObject.cachedSequences)
                return;

            var time = CurrentAudioSource.time - BeatmapObject.StartTime;

            var sequence = BeatmapObject.cachedSequences.ScaleSequence.Interpolate(time);
            var pitch = sequence.x * CurrentAudioSource.pitch;

            AudioSource.pitch = pitch;
            AudioSource.volume = sequence.y * AudioManager.inst.sfxVol;

            var isPlaying = CurrentAudioSource.isPlaying;
            if (!isPlaying && AudioSource.isPlaying)
                AudioSource.Pause();
            else if (isPlaying && !AudioSource.isPlaying)
                AudioSource.Play();

            var length = AudioSource.clip.length;
            if (AudioSource.time != Mathf.Clamp(time * pitch % length, 0f, length))
                AudioSource.time = Mathf.Clamp(time * pitch % length, 0f, length);
        }

        /// <summary>
        /// The main audio source to base the time off of.
        /// </summary>
        public AudioSource CurrentAudioSource => AudioManager.inst.CurrentAudioSource;

        /// <summary>
        /// The current audio source.
        /// </summary>
        public AudioSource AudioSource { get; set; }

        /// <summary>
        /// The current audio clip.
        /// </summary>
        public AudioClip AudioClip { get; set; }

        public BeatmapObject BeatmapObject { get; set; }

        public Modifier<BeatmapObject> Modifier { get; set; }
    }
}
