using HarmonyLib;
using System.Linq;
using UnityEngine;
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
        public static SoundManager inst;

        public AudioManager BaseManager => AudioManager.inst;
        public SoundLibrary Library => AudioManager.inst.library;

        public static float musicVolume = 1f;

        void Awake() => inst = this;

        public void SetPlaying(bool playing) => (playing ? (System.Action)BaseManager.CurrentAudioSource.Play : BaseManager.CurrentAudioSource.Pause).Invoke();

        public void TogglePlaying() => SetPlaying(!BaseManager.CurrentAudioSource.isPlaying);

        public void PlaySound(DefaultSounds defaultSound, float volume = 1, float pitch = 1, bool loop = false, System.Action onSoundComplete = null) => PlaySound(defaultSound.ToString(), volume, pitch, loop, onSoundComplete);
        public void PlaySound(GameObject gameObject, DefaultSounds defaultSound, float volume = 1, float pitch = 1, bool loop = false, System.Action onSoundComplete = null) => PlaySound(gameObject, defaultSound.ToString(), volume, pitch, loop, onSoundComplete);

        public void PlaySound(string soundName, float volume = 1f, float pitch = 1f, bool loop = false, System.Action onSoundComplete = null) => PlaySound(Library.GetClipFromName(soundName), volume, pitch, loop, onSoundComplete);
        public void PlaySound(GameObject gameObject, string soundName, float volume = 1f, float pitch = 1f, bool loop = false, System.Action onSoundComplete = null) => PlaySound(gameObject, Library.GetClipFromName(soundName), volume, pitch, loop, onSoundComplete);

        public void PlaySound(AudioClip clip, float volume = 1f, float pitch = 1f, bool loop = false, System.Action onSoundComplete = null) => PlaySound(Camera.main.gameObject, clip, volume, pitch, loop, onSoundComplete);

        public void PlaySound(GameObject gameObject, AudioClip clip, float volume = 1f, float pitch = 1f, bool loop = false, System.Action onSoundComplete = null)
        {
            if (!clip)
                return;

            pitch = pitch < 0f ? -pitch : pitch == 0f ? 0.001f : pitch;
            var audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.playOnAwake = true;
            audioSource.loop = loop;
            audioSource.volume = BaseManager.sfxVol * volume;
            audioSource.pitch = pitch;
            audioSource.Play();
            float length = clip.length / pitch;
            //BaseManager.StartCoroutine(BaseManager.DestroyWithDelay(audioSource, length));

            CoreHelper.PerformActionAfterSeconds(length, () =>
            {
                CoreHelper.Destroy(audioSource);
                onSoundComplete?.Invoke();
            });
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
            Library.soundClips.Add(id, audioClips);
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
    }
}
