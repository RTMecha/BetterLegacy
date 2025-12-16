using System.Collections.Generic;

using UnityEngine;

namespace BetterLegacy.Core.Data
{
    public class SoundGroup : Exists
    {
        public SoundGroup() { }

        public SoundGroup(string id) => this.id = id;

        public SoundGroup(SoundLibrary.SoundGroup soundGroup)
        {
            id = soundGroup.soundID;
            for (int i = 0; i < soundGroup.group.Length; i++)
                group.Add(soundGroup.group[i]);
            orig = soundGroup;
        }

        public string id;
        public List<AudioClipWrapper> group = new List<AudioClipWrapper>();

        public SoundLibrary.SoundGroup orig;

        public virtual AudioClipWrapper GetClip() => group.IsEmpty() ? null : group[UnityRandom.Range(0, group.Count)];

        public AudioClipWrapper this[int index]
        {
            get => group[index];
            set => group[index] = value;
        }

        public int Count => group.Count;

        public static implicit operator SoundGroup(SoundLibrary.SoundGroup soundGroup) => new SoundGroup(soundGroup);

        public override string ToString() => id;

        public class AudioClipWrapper
        {
            public AudioClipWrapper(AudioClip clip) => this.clip = clip;

            public AudioClipWrapper(AudioClip clip, bool custom) : this(clip) => this.custom = custom;

            public AudioClip clip;
            public bool custom;

            public override string ToString() => !clip ? "null" : clip.ToString();

            public static implicit operator AudioClipWrapper(AudioClip clip) => new AudioClipWrapper(clip);
            public static implicit operator AudioClip(AudioClipWrapper wrapper) => wrapper.clip;
        }
    }
}
