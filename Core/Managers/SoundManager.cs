
using HarmonyLib;

using UnityEngine;

using SoundGroup = SoundLibrary.SoundGroup;

namespace BetterLegacy.Core.Managers
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager inst;

        public AudioManager BaseManager => AudioManager.inst;
        public SoundLibrary Library => AudioManager.inst.library;

        void Awake() => inst = this;

        public void PlaySound(string soundName, float volume = 1f, float pitch = 1f)
        {
            PlaySound(Library.GetClipFromName(soundName), volume, pitch);
        }

        public void PlaySound(AudioClip clip, float volume = 1f, float pitch = 1f) => PlaySound(Camera.main.gameObject, clip, volume, pitch);

        public void PlaySound(GameObject gameObject, AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip != null)
            {
                var audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.clip = clip;
                audioSource.playOnAwake = true;
                audioSource.loop = false;
                audioSource.volume = BaseManager.sfxVol * volume;
                audioSource.pitch = pitch < 0f ? -pitch : pitch == 0f ? 0.001f : pitch;
                audioSource.Play();
                BaseManager.StartCoroutine(BaseManager.DestroyWithDelay(audioSource, clip.length * (pitch < 0f ? -pitch : pitch == 0f ? 0.001f : pitch)));
            }
        }

        public void AddSound(string id, AudioClip[] audioClips)
        {
            if (Library == null || Library.soundClips.ContainsKey(id))
                return;

            var soundGroup = new SoundGroup
            {
                soundID = id,
                group = audioClips,
            };

            Library.soundGroups.AddItem(soundGroup);
            Library.soundClips.Add(id, audioClips);
        }
    }
}
