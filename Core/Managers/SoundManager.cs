using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers.Settings;

namespace BetterLegacy.Core.Managers
{
    /// <summary>
    /// <see cref="AudioManager"/> and <see cref="SoundLibrary"/> wrapper.
    /// </summary>
    public class SoundManager : BaseManager<SoundManager, SoundManagerSettings>
    {
        #region Values

        AudioManager BaseManager => AudioManager.inst;
        SoundLibrary Library => AudioManager.inst.library;

        /// <summary>
        /// Music volume reference for the no_volume achievement.
        /// </summary>
        public static float musicVolume = 1f;

        /// <summary>
        /// If the current audio source is playing.
        /// </summary>
        public bool Playing => BaseManager.CurrentAudioSource.isPlaying;

        /// <summary>
        /// Total volume.
        /// </summary>
        public float MasterVolume => CoreConfig.Instance.MasterVol.Value / 9f;

        /// <summary>
        /// Sound effect volume.
        /// </summary>
        public float SFXVolume => CoreConfig.Instance.SFXVol.Value / 9f;

        /// <summary>
        /// Music volume.
        /// </summary>
        public float MusicVolume => CoreConfig.Instance.MusicVol.Value / 9f;

        /// <summary>
        /// Length of the current audio clip.
        /// </summary>
        public float MusicLength => BaseManager && BaseManager.CurrentAudioSource && BaseManager.CurrentAudioSource.clip ? BaseManager.CurrentAudioSource.clip.length : 0f;

        #endregion

        #region Functions

        #region Playing State

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

        public AudioClip GetSound(string name) => LegacyResources.soundClips.TryFind(x => x.id == name, out SoundGroup soundGroup) ? soundGroup.GetClip() : null;

        public bool TryGetSound(string name, out AudioClip audioClip)
        {
            if (LegacyResources.soundClips.TryFind(x => x.id == name, out SoundGroup soundGroup))
            {
                audioClip = soundGroup.GetClip();
                return true;
            }

            audioClip = null;
            return false;
        }

        public void AddSound(string id, List<SoundGroup.AudioClipWrapper> audioClips)
        {
            LegacyResources.soundClips.Add(new SoundGroup
            {
                id = id,
                group = audioClips,
            });
        }

        #endregion

        #region Music

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
            if (!LegacyResources.musicClips.TryFind(x => x.id == name, out MusicGroup musicGroup))
            {
                audioClip = null;
                return false;
            }

            audioClip = musicGroup.GetClip();
            return true;
        }

        public void AddMusic(string id, List<SoundGroup.AudioClipWrapper> audioClips)
        {
            if (!Library)
                return;

            var musicGroup = new MusicGroup
            {
                id = id,
                group = audioClips,
            };

            if (musicGroup.group.Count > 1 && musicGroup.alwaysRandom)
                musicGroup.randomIndex = Random.Range(0, musicGroup.Count);
            LegacyResources.musicClips.Add(musicGroup);
        }

        #endregion

        /// <summary>
        /// Fade transitions between two audio sources.
        /// </summary>
        /// <param name="a">Audio source to fade from.</param>
        /// <param name="b">Audio source to fade to.</param>
        /// <param name="duration">Amount of time to fade.</param>
        /// <param name="volume">The current volume.</param>
        public void FadeTransition(AudioSource a, AudioSource b, float duration, float volume) => CoroutineHelper.StartCoroutine(IFadeTransition(a, b, duration, volume));

        /// <summary>
        /// Fade transitions between two audio sources.
        /// </summary>
        /// <param name="a">Audio source to fade from.</param>
        /// <param name="b">Audio source to fade to.</param>
        /// <param name="duration">Amount of time to fade.</param>
        /// <param name="volume">The current volume.</param>
        public IEnumerator IFadeTransition(AudioSource a, AudioSource b, float duration, float volume)
        {
            float percent = 0f;
            while (percent < 1f)
            {
                percent += Time.deltaTime * 1f / duration;
                a.volume = Mathf.Lerp(0f, volume, percent);
                b.volume = Mathf.Lerp(volume, 0f, percent);
                yield return null;
            }
        }

        #endregion
    }
}
