namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Represents a group of music to play in menus.
    /// </summary>
    public class MusicGroup : SoundGroup
    {
        public MusicGroup() { }

        public MusicGroup(string id) => this.id = id;

        public MusicGroup(SoundLibrary.MusicGroup musicGroup)
        {
            id = musicGroup.musicID;
            for (int i = 0; i < musicGroup.music.Length; i++)
                group.Add(musicGroup.music[i]);
            randomIndex = musicGroup.randomIndex;
            alwaysRandom = !musicGroup.alwaysRandom; // inverses always random due to incorrect usage
            orig = musicGroup;
        }

        /// <summary>
        /// Selected audio clip to play.
        /// </summary>
        public int randomIndex = -1;

        /// <summary>
        /// If the music group is always random.
        /// </summary>
        public bool alwaysRandom = true;

        /// <summary>
        /// The vanilla music group.
        /// </summary>
        public new SoundLibrary.MusicGroup orig;

        public override AudioClipWrapper GetClip() => group.IsEmpty() ? null : group.TryGetAt(randomIndex, out AudioClipWrapper clip) ? clip : base.GetClip();

        /// <summary>
        /// Shuffles the music grouo.
        /// </summary>
        public void Shuffle()
        {
            if (!alwaysRandom || group.IsEmpty())
                return;

            if (group.Count == 1)
                randomIndex = 0;
            else
                randomIndex = UnityRandom.Range(0, Count);
        }

        public static implicit operator MusicGroup(SoundLibrary.MusicGroup soundGroup) => new MusicGroup(soundGroup);

        public override string ToString() => id;
    }
}
