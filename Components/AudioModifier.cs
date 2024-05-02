using BetterLegacy.Core.Data;
using BetterLegacy.Core.Optimization;
using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace BetterLegacy.Components
{
    public class AudioModifier : MonoBehaviour
    {
        void Awake()
        {
            AudioSource = gameObject.AddComponent<AudioSource>();
            AudioSource.loop = true;
        }

        public void Init(AudioClip audioClip, BeatmapObject beatmapObject, Modifier<BeatmapObject> modifier)
        {
            AudioClip = audioClip;
            BeatmapObject = beatmapObject;
            Modifier = modifier;
            AudioSource.clip = AudioClip;

            var onDestroy = gameObject.AddComponent<DestroyModifierResult>();
            onDestroy.Modifier = modifier;
        }

        void Update()
        {
            if (AudioSource == null || BeatmapObject == null ||
                !Updater.levelProcessor || Updater.levelProcessor.converter == null ||
                Updater.levelProcessor.converter.cachedSequences == null || !Updater.levelProcessor.converter.cachedSequences.ContainsKey(BeatmapObject.id))
                return;

            var time = CurrentAudioSource.time - BeatmapObject.StartTime;

            var sequence = Updater.levelProcessor.converter.cachedSequences[BeatmapObject.id].ScaleSequence.Interpolate(time);
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

        public AudioSource CurrentAudioSource => AudioManager.inst.CurrentAudioSource;
        public AudioSource AudioSource { get; set; }
        public AudioClip AudioClip { get; set; }

        public BeatmapObject BeatmapObject { get; set; }
        public Modifier<BeatmapObject> Modifier { get; set; }
    }
}
