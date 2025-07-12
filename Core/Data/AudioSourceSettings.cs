namespace BetterLegacy.Core.Data
{
    public struct AudioSourceSettings
    {
        public AudioSourceSettings(float pitch = 1f, float volume = 1f, bool loop = false, float panStereo = 0f)
        {
            this.pitch = pitch;
            this.volume = volume;
            this.loop = loop;
            this.panStereo = panStereo;
        }

        public static AudioSourceSettings Default => new AudioSourceSettings(1f, 1f, false, 0f);

        public float pitch;
        public float volume;
        public bool loop;
        public float panStereo;
    }
}
