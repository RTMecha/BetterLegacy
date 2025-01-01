using UnityEngine;

namespace BetterLegacy.Core.Components
{
    public class ReactiveAudio : MonoBehaviour
    {
        float[] samples = new float[256];

        public int[] channels = new int[2];
        public float[] intensity = new float[2];

        private Vector3 ogScale;

        void Awake() => ogScale = transform.localScale;

        void Update()
        {
            AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

            float x = samples[channels[0]] * intensity[0];
            float y = samples[channels[1]] * intensity[1];

            gameObject.transform.localScale = ogScale + new Vector3(x, y, 1f);
        }
    }
}
