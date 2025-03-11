using BetterLegacy.Configs;
using BetterLegacy.Core.Data.Beatmap;
using System;

namespace BetterLegacy.Core.Helpers
{
    /// <summary>
    /// Helper class for randomization.
    /// </summary>
    public static class RandomHelper
    {
        /// <summary>
        /// The current seed a Project Arrhythmia level uses.
        /// </summary>
        public static string CurrentSeed { get; set; }

        /// <summary>
        /// Updates the seed to the current seed setting.
        /// </summary>
        public static void UpdateSeed()
        {
            try
            {
                SetSeed(CoreConfig.Instance.Seed.Value);
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        /// <summary>
        /// Sets the current seed.
        /// </summary>
        /// <param name="seedSetting">The seed setting to apply.</param>
        public static void SetSeed(string seedSetting) => CurrentSeed = !string.IsNullOrEmpty(seedSetting) ? seedSetting : LSFunctions.LSText.randomString(16);

        /// <summary>
        /// Randomizes the current seed.
        /// </summary>
        public static void RandomizeSeed() => CurrentSeed = LSFunctions.LSText.randomString(16);

        #region Main Seed

        #region ID

        /// <summary>
        /// Gets a "random" value from a specific ID and seed between 0 and 1.
        /// </summary>
        /// <param name="id">Object ID to calculate.</param>
        /// <returns>Returns a value based on the object ID and seed.</returns>
        public static float SingleFromID(string id)
        {
            var hash = id.GetHashCode() ^ CurrentSeed.GetHashCode();
            return (float)(hash / (double)int.MaxValue) * 0.5f + 0.5f;
        }

        /// <summary>
        /// Gets a "random" value within a range from a specific ID and seed.
        /// </summary>
        /// <param name="id">Object ID to calculate.</param>
        /// <param name="min">Minimum range.</param>
        /// <param name="max">Maximum range.</param>
        /// <returns>Returns a value based on the object ID and seed.</returns>
        public static float SingleFromIDRange(string id, float min, float max) => RTMath.Lerp(min, max, SingleFromID(id));

        /// <summary>
        /// Gets a "random" value from a specific ID and seed.
        /// </summary>
        /// <param name="id">Object ID to calculate.</param>
        /// <returns>Returns a value based on the object ID and seed.</returns>
        public static int FromID(string id) => id.GetHashCode() ^ CurrentSeed.GetHashCode();

        /// <summary>
        /// Gets a "random" value within a range from a specific ID and seed.
        /// </summary>
        /// <param name="id">Object ID to calculate.</param>
        /// <param name="min">Minimum range.</param>
        /// <param name="max">Maximum range.</param>
        /// <returns>Returns a value based on the object ID and seed.</returns>
        public static int FromIDRange(string id, int min, int max) => UnityEngine.Mathf.RoundToInt(RTMath.Lerp(min, max, SingleFromID(id)));

        /// <summary>
        /// Gets a "random" value from a specific ID and seed between 0 and 1.
        /// </summary>
        /// <param name="index">Object ID to calculate.</param>
        /// <returns>Returns a value based on the object ID and seed.</returns>
        public static float SingleFromIndex(int index)
        {
            var hash = index.GetHashCode() ^ CurrentSeed.GetHashCode();
            return (float)(hash / (double)int.MaxValue) * 0.5f + 0.5f;
        }

        /// <summary>
        /// Gets a "random" value within a range from a specific ID and seed.
        /// </summary>
        /// <param name="index">Object ID to calculate.</param>
        /// <param name="min">Minimum range.</param>
        /// <param name="max">Maximum range.</param>
        /// <returns>Returns a value based on the object ID and seed.</returns>
        public static float SingleFromIndexRange(int index, float min, float max) => RTMath.Lerp(min, max, SingleFromIndex(index));

        /// <summary>
        /// Gets a "random" value from a specific ID and seed.
        /// </summary>
        /// <param name="index">Object ID to calculate.</param>
        /// <returns>Returns a value based on the object ID and seed.</returns>
        public static int FromIndex(int index) => index.GetHashCode() ^ CurrentSeed.GetHashCode();

        /// <summary>
        /// Gets a "random" value within a range from a specific ID and seed.
        /// </summary>
        /// <param name="index">Object ID to calculate.</param>
        /// <param name="min">Minimum range.</param>
        /// <param name="max">Maximum range.</param>
        /// <returns>Returns a value based on the object ID and seed.</returns>
        public static int FromIndexRange(int index, int min, int max) => UnityEngine.Mathf.RoundToInt(RTMath.Lerp(min, max, SingleFromIndex(index)));

        #endregion

        #region Bool

        /// <summary>
        /// Initializes a new Random class and checks for a true or false value.
        /// </summary>
        /// <returns>Returns a random true or false value.</returns>
        public static bool IsTrueSeed() => FromRange(0, 1) == 1;

        /// <summary>
        /// Determines a random true or false value depending on the seed and index.
        /// </summary>
        /// <param name="index">The index of Random.Next(0, 2)</param>
        /// <returns>Returns a random true or false value based on the seed and index.</returns>
        public static bool IsTrue(int index) => FromIndexRange(index, 0, 2) == 1;

        /// <summary>
        /// Calculates a chance value.
        /// </summary>
        /// <param name="p">Percentage chance to happen.</param>
        /// <returns>Returns true if a random value was higher than the percentage.</returns>
        public static bool PercentChanceSeed(int p) => FromRange(0, 100) <= p;

        /// <summary>
        /// Calculates a chance value.
        /// </summary>
        /// <param name="index">Index to get.</param>
        /// <param name="p">Percentage chance to happen.</param>
        /// <returns>Returns true if a random value was higher than the percentage.</returns>
        public static bool PercentChance(int index, int p) => FromIndexRange(index, 0, 100) <= p;

        /// <summary>
        /// Calculates a chance value.
        /// </summary>
        /// <param name="p">Percentage chance to happen.</param>
        /// <returns>Returns true if a random value was higher than the percentage.</returns>
        public static bool PercentChanceSeedSingle(float p) => SingleFromRange(0f, 100f) <= p;

        /// <summary>
        /// Calculates a chance value.
        /// </summary>
        /// <param name="index">Index to get.</param>
        /// <param name="p">Percentage chance to happen.</param>
        /// <returns>Returns true if a random value was higher than the percentage.</returns>
        public static bool PercentChanceSeedSingle(int index, float p) => SingleFromIndexRange(index, 0f, 100f) <= p;

        #endregion

        public static float SingleFromRange(float min, float max) => RTMath.Lerp(min, max, (float)(Convert.ToUInt32(CurrentSeed.GetHashCode()) * 2U / (double)uint.MaxValue));
        public static int FromRange(int min, int max) => UnityEngine.Mathf.RoundToInt(RTMath.Lerp(min, max, (float)(Convert.ToUInt32(CurrentSeed.GetHashCode()) * 2U / (double)uint.MaxValue)));

        #endregion

        #region Specific Seed

        #region ID

        /// <summary>
        /// Gets a "random" value from a specific ID and seed between 0 and 1.
        /// </summary>
        /// <param name="id">Object ID to calculate.</param>
        /// <param name="seed">Seed to calculate.</param>
        /// <returns>Returns a value based on the object ID and seed.</returns>
        public static float SingleFromID(string seed, string id)
        {
            var hash = id.GetHashCode() ^ seed.GetHashCode();
            return (float)(hash / (double)int.MaxValue) * 0.5f + 0.5f;
        }

        /// <summary>
        /// Gets a "random" value within a range from a specific ID and seed.
        /// </summary>
        /// <param name="id">Object ID to calculate.</param>
        /// <param name="seed">Seed to calculate.</param>
        /// <param name="min">Minimum range.</param>
        /// <param name="max">Maximum range.</param>
        /// <returns>Returns a value based on the object ID and seed.</returns>
        public static float SingleFromIDRange(string seed, string id, float min, float max) => RTMath.Lerp(min, max, SingleFromID(seed, id));

        /// <summary>
        /// Gets a "random" value from a specific ID and seed.
        /// </summary>
        /// <param name="id">Object ID to calculate.</param>
        /// <param name="seed">Seed to calculate.</param>
        /// <returns>Returns a value based on the object ID and seed.</returns>
        public static int FromID(string seed, string id) => id.GetHashCode() ^ seed.GetHashCode();

        /// <summary>
        /// Gets a "random" value within a range from a specific ID and seed.
        /// </summary>
        /// <param name="id">Object ID to calculate.</param>
        /// <param name="seed">Seed to calculate.</param>
        /// <param name="min">Minimum range.</param>
        /// <param name="max">Maximum range.</param>
        /// <returns>Returns a value based on the object ID and seed.</returns>
        public static int FromIDRange(string seed, string id, int min, int max) => UnityEngine.Mathf.RoundToInt(RTMath.Lerp(min, max, SingleFromID(seed, id)));

        /// <summary>
        /// Gets a "random" value from a specific ID and seed between 0 and 1.
        /// </summary>
        /// <param name="id">Object ID to calculate.</param>
        /// <param name="seed">Seed to calculate.</param>
        /// <returns>Returns a value based on the object ID and seed.</returns>
        public static float SingleFromIndex(string seed, int index)
        {
            var hash = index.GetHashCode() ^ seed.GetHashCode();
            return (float)(hash / (double)int.MaxValue) * 0.5f + 0.5f;
        }

        /// <summary>
        /// Gets a "random" value within a range from a specific ID and seed.
        /// </summary>
        /// <param name="id">Object ID to calculate.</param>
        /// <param name="seed">Seed to calculate.</param>
        /// <param name="min">Minimum range.</param>
        /// <param name="max">Maximum range.</param>
        /// <returns>Returns a value based on the object ID and seed.</returns>
        public static float SingleFromIndexRange(string seed, int index, float min, float max) => RTMath.Lerp(min, max, SingleFromIndex(seed, index));

        /// <summary>
        /// Gets a "random" value from a specific ID and seed.
        /// </summary>
        /// <param name="id">Object ID to calculate.</param>
        /// <param name="seed">Seed to calculate.</param>
        /// <returns>Returns a value based on the object ID and seed.</returns>
        public static int FromIndex(string seed, int index) => index.GetHashCode() ^ seed.GetHashCode();

        /// <summary>
        /// Gets a "random" value within a range from a specific ID and seed.
        /// </summary>
        /// <param name="id">Object ID to calculate.</param>
        /// <param name="seed">Seed to calculate.</param>
        /// <param name="min">Minimum range.</param>
        /// <param name="max">Maximum range.</param>
        /// <returns>Returns a value based on the object ID and seed.</returns>
        public static int FromIndexRange(string seed, int index, int min, int max) => UnityEngine.Mathf.RoundToInt(RTMath.Lerp(min, max, SingleFromIndex(seed, index)));

        #endregion

        #region Bool

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
        public static bool IsTrue(string seed) => new Random(seed.GetHashCode()).Next(0, 2) == 1;

        /// <summary>
        /// Determines a random true or false value depending on the seed and index.
        /// </summary>
        /// <param name="seed">The seed to determine the random value.</param>
        /// <param name="index">The index of Random.Next(0, 2)</param>
        /// <returns>Returns a random true or false value based on the seed and index.</returns>
        public static bool IsTrue(string seed, int index) => FromIndexRange(seed, index, 0, 2) == 1;

        /// <summary>
        /// Calculates a chance value.
        /// </summary>
        /// <param name="p">Percentage chance to happen.</param>
        /// <returns>Returns true if a random value was higher than the percentage.</returns>
        public static bool PercentChance(int p) => new Random().Next(0, 101) <= p;

        /// <summary>
        /// Calculates a chance value.
        /// </summary>
        /// <param name="seed">Seed to calculate.</param>
        /// <param name="p">Percentage chance to happen.</param>
        /// <returns>Returns true if a random value was higher than the percentage.</returns>
        public static bool PercentChance(string seed, int p) => new Random(seed.GetHashCode()).Next(0, 101) <= p;

        /// <summary>
        /// Calculates a chance value.
        /// </summary>
        /// <param name="seed">Seed to calculate.</param>
        /// <param name="index">Index to get.</param>
        /// <param name="p">Percentage chance to happen.</param>
        /// <returns>Returns true if a random value was higher than the percentage.</returns>
        public static bool PercentChance(string seed, int index, int p) => FromIndexRange(seed, index, 0, 101) <= p;

        /// <summary>
        /// Calculates a chance value.
        /// </summary>
        /// <param name="p">Percentage chance to happen.</param>
        /// <returns>Returns true if a random value was higher than the percentage.</returns>
        public static bool PercentChanceSingle(float p) => UnityEngine.Random.Range(0f, 100f) <= p;

        /// <summary>
        /// Calculates a chance value.
        /// </summary>
        /// <param name="seed">Seed to calculate.</param>
        /// <param name="p">Percentage chance to happen.</param>
        /// <returns>Returns true if a random value was higher than the percentage.</returns>
        public static bool PercentChanceSingle(string seed, float p) => SingleFromRange(seed, 0f, 100f) <= p;

        /// <summary>
        /// Calculates a chance value.
        /// </summary>
        /// <param name="seed">Seed to calculate.</param>
        /// <param name="index">Index to get.</param>
        /// <param name="p">Percentage chance to happen.</param>
        /// <returns>Returns true if a random value was higher than the percentage.</returns>
        public static bool PercentChanceSingle(string seed, int index, float p) => SingleFromIndexRange(seed, index, 0f, 100f) <= p;

        #endregion

        public static float SingleFromRange(string seed, float min, float max) => RTMath.Lerp(min, max, (float)(Convert.ToUInt32(seed.GetHashCode()) * 2U / (double)uint.MaxValue));
        public static int FromRange(string seed, int min, int max) => UnityEngine.Mathf.RoundToInt(RTMath.Lerp(min, max, (float)(Convert.ToUInt32(seed.GetHashCode()) * 2U / (double)uint.MaxValue)));

        #endregion

        public static class KeyframeRandomizer
        {
            public static float RandomizeFloatKeyframe(string id, EventKeyframe eventKeyframe, int index = 0)
            {
                var round = eventKeyframe.randomValues.Length > 2 && eventKeyframe.randomValues[2] != 0f;
                return eventKeyframe.random switch
                {
                    1 => round ?
                            RTMath.RoundToNearestNumber(SingleFromIDRange(id, CurrentSeed, eventKeyframe.values[index], eventKeyframe.randomValues[0]), eventKeyframe.randomValues[2]) :
                            SingleFromIDRange(id, CurrentSeed, eventKeyframe.values[index], eventKeyframe.randomValues[0]),
                    2 => UnityEngine.Mathf.Round(SingleFromIDRange(id, CurrentSeed, eventKeyframe.values[index], eventKeyframe.randomValues[0])),
                    3 => (SingleFromID(id, CurrentSeed) > 0.5f) ? eventKeyframe.values[index] : eventKeyframe.randomValues[0],
                    4 => eventKeyframe.values[index] * (round ?
                                RTMath.RoundToNearestNumber(SingleFromIDRange(id, CurrentSeed, eventKeyframe.randomValues[0], eventKeyframe.randomValues[1]), eventKeyframe.randomValues[2]) :
                                SingleFromIDRange(id, CurrentSeed, eventKeyframe.randomValues[0], eventKeyframe.randomValues[1])),
                    _ => 0f
                };
            }

            public static UnityEngine.Vector2 RandomizeVector2Keyframe(string id, EventKeyframe eventKeyframe)
            {
                float x = 0f;
                float y = 0f;
                var round = eventKeyframe.randomValues.Length > 2 && eventKeyframe.randomValues[2] != 0f;
                switch (eventKeyframe.random)
                {
                    case 1: // Regular
                        {
                            if (round)
                            {
                                x = (eventKeyframe.values[0] == eventKeyframe.randomValues[0]) ?
                                        eventKeyframe.values[0] :
                                        RTMath.RoundToNearestNumber(SingleFromIDRange(CurrentSeed, id + "X", eventKeyframe.values[0], eventKeyframe.randomValues[0]), eventKeyframe.randomValues[2]);
                                y = (eventKeyframe.values[1] == eventKeyframe.randomValues[1]) ?
                                        eventKeyframe.values[1] :
                                        RTMath.RoundToNearestNumber(SingleFromIDRange(CurrentSeed, id + "Y", eventKeyframe.values[1], eventKeyframe.randomValues[1]), eventKeyframe.randomValues[2]);
                            }
                            else
                            {
                                x = SingleFromIDRange(CurrentSeed, id + "X", eventKeyframe.values[0], eventKeyframe.randomValues[0]);
                                y = SingleFromIDRange(CurrentSeed, id + "Y", eventKeyframe.values[1], eventKeyframe.randomValues[1]);
                            }
                            break;
                        }
                    case 2: // Support for really old levels (for some reason)
                        {
                            x = UnityEngine.Mathf.Round(SingleFromIDRange(CurrentSeed, id + "X", eventKeyframe.values[0], eventKeyframe.randomValues[0]));
                            y = UnityEngine.Mathf.Round(SingleFromIDRange(CurrentSeed, id + "Y", eventKeyframe.values[1], eventKeyframe.randomValues[1]));
                            break;
                        }
                    case 3: // Toggle
                        {
                            bool toggle = SingleFromID(CurrentSeed, id) > 0.5f;
                            x = toggle ? eventKeyframe.values[0] : eventKeyframe.randomValues[0];
                            y = toggle ? eventKeyframe.values[1] : eventKeyframe.randomValues[1];
                            break;
                        }
                    case 4: // Scale
                        {
                            float multiply = SingleFromIDRange(CurrentSeed, id, eventKeyframe.randomValues[0], eventKeyframe.randomValues[1]);

                            if (round)
                                multiply = RTMath.RoundToNearestNumber(multiply, eventKeyframe.randomValues[2]);

                            x = eventKeyframe.values[0] * multiply;
                            y = eventKeyframe.values[1] * multiply;
                            break;
                        }
                }
                return new UnityEngine.Vector2(x, y);
            }

            public static float RandomizeFloatKeyframe(EventKeyframe eventKeyframe, int index = 0) => eventKeyframe.random switch
            {
                1 => eventKeyframe.randomValues.Length > 2 && eventKeyframe.randomValues[2] != 0f ?
                        RTMath.RoundToNearestNumber(UnityEngine.Random.Range(eventKeyframe.values[index], eventKeyframe.randomValues[0]), eventKeyframe.randomValues[2]) :
                        UnityEngine.Random.Range(eventKeyframe.values[index], eventKeyframe.randomValues[0]),
                2 => UnityEngine.Mathf.Round(UnityEngine.Random.Range(eventKeyframe.values[index], eventKeyframe.randomValues[0])),
                3 => (UnityEngine.Random.value > 0.5f) ? eventKeyframe.values[index] : eventKeyframe.randomValues[0],
                4 => eventKeyframe.values[index] * eventKeyframe.randomValues.Length > 2 && eventKeyframe.randomValues[2] != 0f ?
                            RTMath.RoundToNearestNumber(UnityEngine.Random.Range(eventKeyframe.randomValues[0], eventKeyframe.randomValues[1]), eventKeyframe.randomValues[2]) :
                            UnityEngine.Random.Range(eventKeyframe.randomValues[0], eventKeyframe.randomValues[1]),
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
                            if (eventKeyframe.randomValues.Length > 2 && eventKeyframe.randomValues[2] != 0f)
                            {
                                x = ((eventKeyframe.values[xIndex] == eventKeyframe.randomValues[0]) ? eventKeyframe.values[xIndex] : RTMath.RoundToNearestNumber(UnityEngine.Random.Range(eventKeyframe.values[xIndex], eventKeyframe.randomValues[0]), eventKeyframe.randomValues[2]));
                                y = ((eventKeyframe.values[yIndex] == eventKeyframe.randomValues[1]) ? eventKeyframe.values[yIndex] : RTMath.RoundToNearestNumber(UnityEngine.Random.Range(eventKeyframe.values[yIndex], eventKeyframe.randomValues[1]), eventKeyframe.randomValues[2]));
                            }
                            else
                            {
                                x = UnityEngine.Random.Range(eventKeyframe.values[xIndex], eventKeyframe.randomValues[0]);
                                y = UnityEngine.Random.Range(eventKeyframe.values[yIndex], eventKeyframe.randomValues[1]);
                            }
                            break;
                        }
                    case 2: // Support for really old levels
                        {
                            x = UnityEngine.Mathf.Round(UnityEngine.Random.Range(eventKeyframe.values[xIndex], eventKeyframe.randomValues[0]));
                            y = UnityEngine.Mathf.Round(UnityEngine.Random.Range(eventKeyframe.values[yIndex], eventKeyframe.randomValues[1]));
                            break;
                        }
                    case 3: // Toggle
                        {
                            bool toggle = UnityEngine.Random.value > 0.5f;
                            x = toggle ? eventKeyframe.values[xIndex] : eventKeyframe.randomValues[0];
                            y = toggle ? eventKeyframe.values[yIndex] : eventKeyframe.randomValues[1];
                            break;
                        }
                    case 4: // Scale
                        {
                            float multiply = eventKeyframe.randomValues.Length > 2 && eventKeyframe.randomValues[2] != 0f ? RTMath.RoundToNearestNumber(UnityEngine.Random.Range(eventKeyframe.randomValues[0], eventKeyframe.randomValues[1]), eventKeyframe.randomValues[2]) : UnityEngine.Random.Range(eventKeyframe.randomValues[0], eventKeyframe.randomValues[1]);
                            x = eventKeyframe.values[xIndex] * multiply;
                            y = eventKeyframe.values[yIndex] * multiply;
                            break;
                        }
                }
                return new UnityEngine.Vector2(x, y);
            }
        }
    }
}
