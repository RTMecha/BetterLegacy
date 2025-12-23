using System.Collections.Generic;

using UnityEngine;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Represents a group of sounds that can be played.
    /// </summary>
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

        /// <summary>
        /// Identification of the group.
        /// </summary>
        public string id;

        /// <summary>
        /// List of audio clips.
        /// </summary>
        public List<AudioClipWrapper> group = new List<AudioClipWrapper>();

        /// <summary>
        /// The vanilla music group.
        /// </summary>
        public SoundLibrary.SoundGroup orig;

        /// <summary>
        /// Gets an audio clip from the group.
        /// </summary>
        /// <returns>Returns an audio clip from the group.</returns>
        public virtual AudioClipWrapper GetClip() => group.IsEmpty() ? null : group[UnityRandom.Range(0, group.Count)];

        public AudioClipWrapper this[int index]
        {
            get => group[index];
            set => group[index] = value;
        }

        /// <summary>
        /// Amount of audio clips in the group.
        /// </summary>
        public int Count => group.Count;

        public static implicit operator SoundGroup(SoundLibrary.SoundGroup soundGroup) => new SoundGroup(soundGroup);

        public override string ToString() => id;

        /// <summary>
        /// Wraps <see cref="AudioClip"/>.
        /// </summary>
        public class AudioClipWrapper
        {
            public AudioClipWrapper(AudioClip clip) => this.clip = clip;

            public AudioClipWrapper(AudioClip clip, bool custom) : this(clip) => this.custom = custom;

            /// <summary>
            /// <see cref="AudioClip"/> reference.
            /// </summary>
            public AudioClip clip;

            /// <summary>
            /// If the audio clip is a custom loaded clip.
            /// </summary>
            public bool custom;

            public override string ToString() => !clip ? "null" : clip.ToString();

            public static implicit operator AudioClipWrapper(AudioClip clip) => new AudioClipWrapper(clip);
            public static implicit operator AudioClip(AudioClipWrapper wrapper) => wrapper.clip;
        }
    }
}
