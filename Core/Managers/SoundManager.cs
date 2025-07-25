﻿using System.Linq;

using UnityEngine;

using HarmonyLib;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;

using SoundGroup = SoundLibrary.SoundGroup;
using MusicGroup = SoundLibrary.MusicGroup;

namespace BetterLegacy.Core.Managers
{
    /// <summary>
    /// <see cref="AudioManager"/> and <see cref="SoundLibrary"/> wrapper.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        #region Init

        /// <summary>
        /// The <see cref="SoundManager"/> global instance reference.
        /// </summary>
        public static SoundManager inst;

        AudioManager BaseManager => AudioManager.inst;
        SoundLibrary Library => AudioManager.inst.library;

        /// <summary>
        /// Music volume reference for the no_volume achievement.
        /// </summary>
        public static float musicVolume = 1f;

        void Awake() => inst = this;

        #endregion

        #region Playing State

        /// <summary>
        /// If the current audio source is playing.
        /// </summary>
        public bool Playing => BaseManager.CurrentAudioSource.isPlaying;

        /// <summary>
        /// Sets the playing state of the current audio source.
        /// </summary>
        /// <param name="playing">If the audio should play.</param>
        public void SetPlaying(bool playing) => (playing ? (System.Action)BaseManager.CurrentAudioSource.Play : BaseManager.CurrentAudioSource.Pause).Invoke();

        /// <summary>
        /// Toggles the playing state of the current audio source.
        /// </summary>
        public void TogglePlaying() => SetPlaying(!Playing);

        #endregion

        #region Sound

        public AudioSource PlaySound(AudioClip clip, AudioSourceSettings settings) => PlaySound(Camera.main.gameObject, clip, settings);

        public AudioSource PlaySound(GameObject gameObject, AudioClip clip, AudioSourceSettings settings) => PlaySound(gameObject, clip, settings.volume, settings.pitch, settings.loop, settings.panStereo);

        public AudioSource PlaySound(DefaultSounds defaultSound, float volume = 1, float pitch = 1, bool loop = false, float panStereo = 0f, System.Action onSoundComplete = null) => PlaySound(defaultSound.ToString(), volume, pitch, loop, panStereo, onSoundComplete);

        public AudioSource PlaySound(GameObject gameObject, DefaultSounds defaultSound, float volume = 1, float pitch = 1, bool loop = false, float panStereo = 0f, System.Action onSoundComplete = null) => PlaySound(gameObject, defaultSound.ToString(), volume, pitch, loop, panStereo, onSoundComplete);

        public AudioSource PlaySound(string soundName, float volume = 1f, float pitch = 1f, bool loop = false, float panStereo = 0f, System.Action onSoundComplete = null) => PlaySound(Library.GetClipFromName(soundName), volume, pitch, loop, panStereo, onSoundComplete);

        public AudioSource PlaySound(GameObject gameObject, string soundName, float volume = 1f, float pitch = 1f, bool loop = false, float panStereo = 0f, System.Action onSoundComplete = null) => PlaySound(gameObject, Library.GetClipFromName(soundName), volume, pitch, loop, panStereo, onSoundComplete);

        public AudioSource PlaySound(AudioClip clip, float volume = 1f, float pitch = 1f, bool loop = false, float panStereo = 0f, System.Action onSoundComplete = null) => PlaySound(Camera.main.gameObject, clip, volume, pitch, loop, panStereo, onSoundComplete);

        public AudioSource PlaySound(GameObject gameObject, AudioClip clip, float volume = 1f, float pitch = 1f, bool loop = false, float panStereo = 0f, System.Action onSoundComplete = null)
        {
            if (!clip)
                return null;

            pitch = pitch < 0f ? -pitch : pitch == 0f ? 0.001f : pitch;
            var audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.playOnAwake = true;
            audioSource.loop = loop;
            audioSource.volume = BaseManager.sfxVol * volume;
            audioSource.pitch = pitch;
            audioSource.panStereo = panStereo;
            audioSource.Play();
            float length = clip.length / pitch;
            //BaseManager.StartCoroutine(BaseManager.DestroyWithDelay(audioSource, length));

            if (!loop)
                CoroutineHelper.PerformActionAfterSeconds(length, () =>
                {
                    CoreHelper.Destroy(audioSource);
                    onSoundComplete?.Invoke();
                });

            return audioSource;
        }

        public AudioClip GetSound(DefaultSounds defaultSound) => GetSound(defaultSound.ToString());

        public AudioClip GetSound(string name)
        {
            var soundClips = Library.soundClips[name];
            return soundClips[UnityEngine.Random.Range(0, soundClips.Length)];
        }

        public bool TryGetSound(string name, out AudioClip audioClip)
        {
            if (Library.soundClips.TryGetValue(name, out AudioClip[] soundClips))
            {
                audioClip = soundClips[Random.Range(0, soundClips.Length)];
                return true;
            }

            audioClip = null;
            return false;
        }

        public void AddSound(string id, AudioClip[] audioClips)
        {
            if (Library == null)
                return;

            var soundGroup = new SoundGroup
            {
                soundID = id,
                group = audioClips,
            };

            Library.soundGroups = Library.soundGroups.AddItem(soundGroup).ToArray();
            Library.soundClips[id] = audioClips;
        }

        #endregion

        #region Music

        /// <summary>
        /// Length of the current audio clip.
        /// </summary>
        public float MusicLength => BaseManager && BaseManager.CurrentAudioSource && BaseManager.CurrentAudioSource.clip ? BaseManager.CurrentAudioSource.clip.length : 0f;

        public void PlayMusic(DefaultMusic defaultMusic, float volume = 1f, float pitch = 1f, float fadeDuration = 0.5f, bool loop = true, bool allowSame = false) => PlayMusic(defaultMusic.ToString(), volume, pitch, fadeDuration, loop, allowSame);

        public void PlayMusic(string musicName, float volume = 1f, float pitch = 1f, float fadeDuration = 0.5f, bool loop = true, bool allowSame = false) => PlayMusic(Library.GetMusicFromName(musicName), volume, pitch, fadeDuration, loop, allowSame);

        public void PlayMusic(AudioClip clip, float volume = 1, float pitch = 1, float fadeDuration = 0.5f, bool loop = true, bool allowSame = false)
        {
            AudioManager.inst.SetPitch(pitch);
            musicVolume = volume;
            AudioManager.inst.PlayMusic(clip.name, clip, allowSame, fadeDuration, loop);
        }

        public bool TryGetMusic(string name, out AudioClip audioClip)
        {
            if (!Library.musicClips.TryGetValue(name, out AudioClip[] audioClips))
            {
                audioClip = null;
                return false;
            }
            if (Library.musicClipsRandomIndex.TryGetValue(name, out int randomIndex))
            {
                audioClip = audioClips[randomIndex];
                return true;
            }

            audioClip = audioClips.Length == 1 ? audioClips[0] : audioClips[Random.Range(0, audioClips.Length)];
            return true;
        }

        public void AddMusic(string id, AudioClip[] audioClips)
        {
            if (Library == null)
                return;

            var musicGroup = new MusicGroup
            {
                musicID = id,
                music = audioClips,
            };

            if (musicGroup.music.Length > 1 && !musicGroup.alwaysRandom) // not alwaysRandom is apparently ACTUALLY RANDOM???
                Library.musicClipsRandomIndex[musicGroup.musicID] = Random.Range(0, musicGroup.music.Length);
            Library.musicClips[musicGroup.musicID] = musicGroup.music;
        }

        #endregion
    }
}
