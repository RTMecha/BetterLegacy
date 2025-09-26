namespace BetterLegacy.Core.Data
{
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

        public int randomIndex = -1;
        public bool alwaysRandom = true;

        public new SoundLibrary.MusicGroup orig;

        public override AudioClipWrapper GetClip() => group.IsEmpty() ? null : group.TryGetAt(randomIndex, out AudioClipWrapper clip) ? clip : base.GetClip();

        public static implicit operator MusicGroup(SoundLibrary.MusicGroup soundGroup) => new MusicGroup(soundGroup);

        public override string ToString() => id;
    }
}
