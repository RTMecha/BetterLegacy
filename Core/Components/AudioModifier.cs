using UnityEngine;

using BetterLegacy.Core.Data.Beatmap;

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
            AudioSource = gameObject.GetOrAddComponent<AudioSource>();
            AudioSource.loop = true;
            AudioSource.clip = AudioClip;

            gameObject.AddComponent<DestroyModifierResult>().Modifier = modifier;
        }

        void Update()
        {
            if (!AudioSource || !BeatmapObject)
                return;

            var time = timeOffset - BeatmapObject.StartTime;

            AudioSource.pitch = pitch * CurrentAudioSource.pitch;
            AudioSource.volume = volume * AudioManager.inst.sfxVol;

            var isPlaying = CurrentAudioSource.isPlaying && playing;
            if (!isPlaying && AudioSource.isPlaying)
                AudioSource.Pause();
            else if (isPlaying && !AudioSource.isPlaying)
                AudioSource.Play();

            var length = AudioSource.clip.length - lengthOffset;
            if (AudioSource.time != Mathf.Clamp(time * pitch % length, 0f, length))
                AudioSource.time = Mathf.Clamp(time * pitch % length, 0f, length);
        }

        public float timeOffset;

        public float pitch = 1f;

        public float volume = 1f;

        public bool loop = false;

        public float lengthOffset;

        public bool playing = true;

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
