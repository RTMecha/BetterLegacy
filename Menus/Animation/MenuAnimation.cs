namespace BetterLegacy.Menus.Animation
{
    public class MenuAnimation
    {
        public MenuFrameBase[] menuFrames;

        public float time;
        public float timeOffset;
        public bool isPlaying;

        public void Play()
        {
            timeOffset = UnityEngine.Time.time;
        }

        public void Update()
        {
            time = UnityEngine.Time.time - timeOffset;

            if (!isPlaying)
                return;


        }
    }
}
