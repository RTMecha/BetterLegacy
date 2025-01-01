namespace BetterLegacy.Core.Components
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// FPS Counter component from https://forum.unity.com/threads/fps-counter.505495/
    /// </summary>
    public class FPSCounter : MonoBehaviour
    {
        public string Text { get; set; }
        public int FPS { get; set; }

        List<string> cachedNumberStrings = new List<string>();
        int[] frameRateSamples;
        int cacheNumbersAmount = 300;
        int averageFromAmount = 30;
        int averageCounter = 0;
        int currentAveraged;

        void Awake()
        {
            // Cache strings and create array
            {
                for (int i = 0; i < cacheNumbersAmount; i++)
                    cachedNumberStrings.Add(i.ToString());
                frameRateSamples = new int[averageFromAmount];
            }
        }
        void Update()
        {
            // Sample
            {
                var currentFrame = (int)Math.Round(1f / Time.smoothDeltaTime); // If your game modifies Time.timeScale, use unscaledDeltaTime and smooth manually (or not).
                frameRateSamples[averageCounter] = currentFrame;
            }

            // Average
            {
                var average = 0f;

                foreach (var frameRate in frameRateSamples)
                    average += frameRate;

                currentAveraged = (int)Math.Round(average / averageFromAmount);
                averageCounter = (averageCounter + 1) % averageFromAmount;
            }

            // Assign to UI
            {
                Text = currentAveraged switch
                {
                    var x when x >= 0 && x < cacheNumbersAmount => cachedNumberStrings[x],
                    var x when x >= cacheNumbersAmount => $"> {cacheNumbersAmount}",
                    var x when x < 0 => "< 0",
                    _ => "?"
                };

                FPS = Mathf.Clamp(currentAveraged, 0, cacheNumbersAmount);
            }
        }
    }
}
