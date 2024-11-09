using BetterLegacy.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Core.Helpers
{
    public static class RandomHelper
    {
        // temporary, replace with proper setting later
        public static string seedSettingTemp;

        public static int CurrentSeed { get; set; }

        public static void UpdateSeed() => SetSeed(seedSettingTemp);

        public static void SetSeed(string seedSetting)
        {
            if (string.IsNullOrEmpty(seedSetting))
                RandomizeSeed();
            else if (int.TryParse(seedSetting, out int seed))
                CurrentSeed = seed;
        }

        public static void RandomizeSeed() => CurrentSeed = new Random().Next();

        public static float SingleFromID(string id, int seed)
        {
            var hash = id.GetHashCode() ^ seed.GetHashCode();
            return (float)(hash / (double)int.MaxValue) * 0.5f + 0.5f;
        }

        public static float SingleFromIDRange(string id, int seed, float min, float max) => RTMath.Lerp(min, max, SingleFromID(id, seed));

        public static int FromID(string id, int seed) => id.GetHashCode() ^ seed.GetHashCode();

        public static int FromIDRange(string id, int seed, int min, int max) => FromID(id, seed) * (min + max) - min;

        /// <summary>
        /// Initializes a new Random class and checks for a true or false value.
        /// </summary>
        /// <returns>Returns a random true or false value.</returns>
        public static bool IsTrue() => new Random().Next(0, 2) == 1;

        /// <summary>
        /// Initializes a new Random class based on a provided seed and checks for a true or false value.
        /// </summary>
        /// <param name="seed">The seed to determine the random value.</param>
        /// <returns>Returns a random true or false value based on the seed.</returns>
        public static bool IsTrue(int seed) => new Random(seed).Next(0, 2) == 1;

        /// <summary>
        /// Determines a random true or false value depending on the seed and index.
        /// </summary>
        /// <param name="seed">The seed to determine the random value.</param>
        /// <param name="index">The index of Random.Next(0, 2)</param>
        /// <returns>Returns a random true or false value based on the seed and index.</returns>
        public static bool IsTrue(int seed, int index) => RandomInstanceRange(seed, 0, 2, index) == 1;

        public static bool PercentChance(int p) => new Random().Next(0, 101) <= p;

        public static bool PercentChance(int seed, int p) => new Random(seed).Next(0, 101) <= p;

        public static bool PercentChance(int seed, int index, int p) => RandomInstanceRange(seed, 0, 101, index) <= p;

        public static bool PercentChanceSingle(float p) => RandomInstanceSingleRange(new Random().Next(), 0f, 100f, 1) <= p;
        public static bool PercentChanceSingle(int seed, float p) => RandomInstanceSingleRange(seed, 0f, 100f, 1) <= p;
        public static bool PercentChanceSingle(int seed, int index, float p) => RandomInstanceSingleRange(seed, 0f, 100f, index) <= p;

        public static int RandomInstance(int seed, int index)
        {
            var rand = new Random(seed);
            int num = 0;
            int result = 0;
            if (index < 1)
                index = 1;

            while (num < index)
            {
                result = rand.Next();
                num++;
            }
            return result;
        }

        public static int RandomInstanceRange(int seed, int min, int max, int index)
        {
            var rand = new Random(seed);
            int num = 0;
            int result = 0;
            if (index < 1)
                index = 1;

            while (num < index)
            {
                result = rand.Next(min, max);
                num++;
            }
            return result;
        }

        public static float RandomInstanceSingle(int seed, int index)
        {
            var rand = new Random(seed);
            int num = 0;
            double result = 0;
            if (index < 1)
                index = 1;

            while (num < index)
            {
                result = rand.NextDouble();
                num++;
            }
            return (float)result;
        }

        public static float RandomInstanceSingleRange(int seed, float min, float max, int index)
        {
            var rand = new Random(seed);
            int num = 0;
            float result = 0f;
            float total = min + max;
            if (index < 1)
                index = 1;

            while (num < index)
            {
                result = (float)rand.NextDouble() * total - min;
                num++;
            }
            return result;
        }

        public static class KeyframeRandomizer
        {
            public static float RandomizeFloatKeyframe(string id, EventKeyframe eventKeyframe, int index = 0)
            {
                var round = eventKeyframe.eventRandomValues.Length > 2 && eventKeyframe.eventRandomValues[2] != 0f;
                return eventKeyframe.random switch
                {
                    1 => round ?
                            RTMath.RoundToNearestNumber(SingleFromIDRange(id, CurrentSeed, eventKeyframe.eventValues[index], eventKeyframe.eventRandomValues[0]), eventKeyframe.eventRandomValues[2]) :
                            SingleFromIDRange(id, CurrentSeed, eventKeyframe.eventValues[index], eventKeyframe.eventRandomValues[0]),
                    2 => UnityEngine.Mathf.Round(SingleFromIDRange(id, CurrentSeed, eventKeyframe.eventValues[index], eventKeyframe.eventRandomValues[0])),
                    3 => (SingleFromID(id, CurrentSeed) > 0.5f) ? eventKeyframe.eventValues[index] : eventKeyframe.eventRandomValues[0],
                    4 => eventKeyframe.eventValues[index] * (round ?
                                RTMath.RoundToNearestNumber(SingleFromIDRange(id, CurrentSeed, eventKeyframe.eventRandomValues[0], eventKeyframe.eventRandomValues[1]), eventKeyframe.eventRandomValues[2]) :
                                SingleFromIDRange(id, CurrentSeed, eventKeyframe.eventRandomValues[0], eventKeyframe.eventRandomValues[1])),
                    _ => 0f
                };
            }

            public static UnityEngine.Vector2 RandomizeVector2Keyframe(string id, EventKeyframe eventKeyframe)
            {
                float x = 0f;
                float y = 0f;
                var round = eventKeyframe.eventRandomValues.Length > 2 && eventKeyframe.eventRandomValues[2] != 0f;
                switch (eventKeyframe.random)
                {
                    case 1: // Regular
                        {
                            if (round)
                            {
                                x = (eventKeyframe.eventValues[0] == eventKeyframe.eventRandomValues[0]) ?
                                        eventKeyframe.eventValues[0] :
                                        RTMath.RoundToNearestNumber(SingleFromIDRange(id + "X", CurrentSeed, eventKeyframe.eventValues[0], eventKeyframe.eventRandomValues[0]), eventKeyframe.eventRandomValues[2]);
                                y = (eventKeyframe.eventValues[1] == eventKeyframe.eventRandomValues[1]) ?
                                        eventKeyframe.eventValues[1] :
                                        RTMath.RoundToNearestNumber(SingleFromIDRange(id + "Y", CurrentSeed, eventKeyframe.eventValues[1], eventKeyframe.eventRandomValues[1]), eventKeyframe.eventRandomValues[2]);
                            }
                            else
                            {
                                x = SingleFromIDRange(id + "X", CurrentSeed, eventKeyframe.eventValues[0], eventKeyframe.eventRandomValues[0]);
                                y = SingleFromIDRange(id + "Y", CurrentSeed, eventKeyframe.eventValues[1], eventKeyframe.eventRandomValues[1]);
                            }
                            break;
                        }
                    case 2: // Support for really old levels
                        {
                            x = UnityEngine.Mathf.Round(SingleFromIDRange(id + "X", CurrentSeed, eventKeyframe.eventValues[0], eventKeyframe.eventRandomValues[0]));
                            y = UnityEngine.Mathf.Round(SingleFromIDRange(id + "Y", CurrentSeed, eventKeyframe.eventValues[1], eventKeyframe.eventRandomValues[1]));
                            break;
                        }
                    case 3: // Toggle
                        {
                            bool toggle = SingleFromID(id, CurrentSeed) > 0.5f;
                            x = toggle ? eventKeyframe.eventValues[0] : eventKeyframe.eventRandomValues[0];
                            y = toggle ? eventKeyframe.eventValues[1] : eventKeyframe.eventRandomValues[1];
                            break;
                        }
                    case 4: // Scale
                        {
                            float multiply = SingleFromIDRange(id, CurrentSeed, eventKeyframe.eventRandomValues[0], eventKeyframe.eventRandomValues[1]);

                            if (round)
                                multiply = RTMath.RoundToNearestNumber(multiply, eventKeyframe.eventRandomValues[2]);

                            x = eventKeyframe.eventValues[0] * multiply;
                            y = eventKeyframe.eventValues[1] * multiply;
                            break;
                        }
                }
                return new UnityEngine.Vector2(x, y);
            }

            public static float RandomizeFloatKeyframe(EventKeyframe eventKeyframe, int index = 0) => eventKeyframe.random switch
            {
                1 => eventKeyframe.eventRandomValues.Length > 2 && eventKeyframe.eventRandomValues[2] != 0f ?
                        RTMath.RoundToNearestNumber(UnityEngine.Random.Range(eventKeyframe.eventValues[index], eventKeyframe.eventRandomValues[0]), eventKeyframe.eventRandomValues[2]) :
                        UnityEngine.Random.Range(eventKeyframe.eventValues[index], eventKeyframe.eventRandomValues[0]),
                2 => UnityEngine.Mathf.Round(UnityEngine.Random.Range(eventKeyframe.eventValues[index], eventKeyframe.eventRandomValues[0])),
                3 => (UnityEngine.Random.value > 0.5f) ? eventKeyframe.eventValues[index] : eventKeyframe.eventRandomValues[0],
                4 => eventKeyframe.eventValues[index] * eventKeyframe.eventRandomValues.Length > 2 && eventKeyframe.eventRandomValues[2] != 0f ?
                            RTMath.RoundToNearestNumber(UnityEngine.Random.Range(eventKeyframe.eventRandomValues[0], eventKeyframe.eventRandomValues[1]), eventKeyframe.eventRandomValues[2]) :
                            UnityEngine.Random.Range(eventKeyframe.eventRandomValues[0], eventKeyframe.eventRandomValues[1]),
                _ => 0f
            };

            public static UnityEngine.Vector2 RandomizeVector2Keyframe(EventKeyframe eventKeyframe, int xIndex = 0, int yIndex = 1)
            {
                float x = 0f;
                float y = 0f;
                switch (eventKeyframe.random)
                {
                    case 1: // Regular
                        {
                            if (eventKeyframe.eventRandomValues.Length > 2 && eventKeyframe.eventRandomValues[2] != 0f)
                            {
                                x = ((eventKeyframe.eventValues[xIndex] == eventKeyframe.eventRandomValues[0]) ? eventKeyframe.eventValues[xIndex] : RTMath.RoundToNearestNumber(UnityEngine.Random.Range(eventKeyframe.eventValues[xIndex], eventKeyframe.eventRandomValues[0]), eventKeyframe.eventRandomValues[2]));
                                y = ((eventKeyframe.eventValues[yIndex] == eventKeyframe.eventRandomValues[1]) ? eventKeyframe.eventValues[yIndex] : RTMath.RoundToNearestNumber(UnityEngine.Random.Range(eventKeyframe.eventValues[yIndex], eventKeyframe.eventRandomValues[1]), eventKeyframe.eventRandomValues[2]));
                            }
                            else
                            {
                                x = UnityEngine.Random.Range(eventKeyframe.eventValues[xIndex], eventKeyframe.eventRandomValues[0]);
                                y = UnityEngine.Random.Range(eventKeyframe.eventValues[yIndex], eventKeyframe.eventRandomValues[1]);
                            }
                            break;
                        }
                    case 2: // Support for really old levels
                        {
                            x = UnityEngine.Mathf.Round(UnityEngine.Random.Range(eventKeyframe.eventValues[xIndex], eventKeyframe.eventRandomValues[0]));
                            y = UnityEngine.Mathf.Round(UnityEngine.Random.Range(eventKeyframe.eventValues[yIndex], eventKeyframe.eventRandomValues[1]));
                            break;
                        }
                    case 3: // Toggle
                        {
                            bool toggle = UnityEngine.Random.value > 0.5f;
                            x = toggle ? eventKeyframe.eventValues[xIndex] : eventKeyframe.eventRandomValues[0];
                            y = toggle ? eventKeyframe.eventValues[yIndex] : eventKeyframe.eventRandomValues[1];
                            break;
                        }
                    case 4: // Scale
                        {
                            float multiply = eventKeyframe.eventRandomValues.Length > 2 && eventKeyframe.eventRandomValues[2] != 0f ? RTMath.RoundToNearestNumber(UnityEngine.Random.Range(eventKeyframe.eventRandomValues[0], eventKeyframe.eventRandomValues[1]), eventKeyframe.eventRandomValues[2]) : UnityEngine.Random.Range(eventKeyframe.eventRandomValues[0], eventKeyframe.eventRandomValues[1]);
                            x = eventKeyframe.eventValues[xIndex] * multiply;
                            y = eventKeyframe.eventValues[yIndex] * multiply;
                            break;
                        }
                }
                return new UnityEngine.Vector2(x, y);
            }
        }
    }
}
