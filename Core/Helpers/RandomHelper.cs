using System;

using BetterLegacy.Configs;
using BetterLegacy.Core.Data.Beatmap;

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

        public static float Single(int hash) => (float)(hash / (double)int.MaxValue) * 0.5f + 0.5f;
        public static float SingleFromRange(int hash, float min, float max) => RTMath.Lerp(min, max, Single(hash));
        public static int IntFromRange(int hash, int min, int max) => UnityEngine.Mathf.RoundToInt(SingleFromRange(hash, min, max));

        /// <summary>
        /// Gets a "random" value from a specific ID and seed between 0 and 1.
        /// </summary>
        /// <param name="seed">Seed to calculate.</param>
        /// <param name="index">Object index to calculate.</param>
        /// <returns>Returns a value based on the object index and seed.</returns>
        public static float SingleFromIndex(string seed, int index) => Single(GetHash(index, seed));
        public static float Single(string seed) => Single(seed.GetHashCode());
        public static float SingleFromRange(string seed, float min, float max) => SingleFromRange(seed.GetHashCode(), min, max);
        public static int IntFromRange(string seed, int min, int max) => IntFromRange(seed.GetHashCode(), min, max);

        /// <summary>
        /// Gets a "random" string from a specific ID and seed.
        /// </summary>
        /// <param name="seed">Seed to calculate.</param>
        /// <param name="id">Object ID to calculate.</param>
        /// <param name="length">Length of the string.</param>
        /// <returns>Returns a string based on the object ID and seed.</returns>
        public static string RandomString(int hash, int length)
        {
            string text = string.Empty;
            char[] array = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789~!@#$%^&*_+{}|:<>?,./;'[]▓▒░▐▆▉☰☱☲☳☴☵☶☷►▼◄▬▩▨▧▦▥▤▣▢□■¤ÿòèµ¶™ßÃ®¾ð¥œ⁕(◠‿◠✿)".ToCharArray();
            int num = 0;
            while (text.Length < length)
            {
                var index = UnityEngine.Mathf.RoundToInt(RTMath.Lerp(0, array.Length - 1, Single(hash * num)));
                text += array[index].ToString();
                num++;
            }
            return text;
        }

        /// <summary>
        /// Gets a hash code from a set of objects.
        /// </summary>
        /// <param name="obj1">Object 1 to get hash code from.</param>
        /// <param name="obj2">Object 2 to get hash code from.</param>
        /// <returns>Returns a singlular hash code.</returns>
        public static int GetHash(object obj1, object obj2) => obj1.GetHashCode() ^ obj2.GetHashCode();

        /// <summary>
        /// Gets a hash code from a set of objects.
        /// </summary>
        /// <param name="obj1">Object 1 to get hash code from.</param>
        /// <param name="obj2">Object 2 to get hash code from.</param>
        /// <param name="obj3">Object 3 to get hash code from.</param>
        /// <returns>Returns a singlular hash code.</returns>
        public static int GetHash(object obj1, object obj2, object obj3) => obj1.GetHashCode() ^ obj2.GetHashCode() ^ obj3.GetHashCode();

        /// <summary>
        /// Gets a hash code from an array of objects.
        /// </summary>
        /// <param name="objs">Objects to get their hash codes from.</param>
        /// <returns>Returns a singlular hash code.</returns>
        public static int GetHash(params object[] objs)
        {
            int hash = objs.Length > 0 ? objs[0].GetHashCode() : 0;
            for (int i = 1; i < objs.Length; i++)
                hash ^= objs[i].GetHashCode();
            return hash;
        }

        #region ID

        /// <summary>
        /// Gets a "random" value within a range from a specific ID and seed.
        /// </summary>
        /// <param name="seed">Seed to calculate.</param>
        /// <param name="id">Object ID to calculate.</param>
        /// <param name="min">Minimum range.</param>
        /// <param name="max">Maximum range.</param>
        /// <returns>Returns a value based on the object ID and seed.</returns>
        public static float SingleFromIDRange(string seed, string id, float min, float max) => RTMath.Lerp(min, max, Single(GetHash(id, seed)));

        /// <summary>
        /// Gets a "random" value from a specific ID and seed.
        /// </summary>
        /// <param name="seed">Seed to calculate.</param>
        /// <param name="id">Object ID to calculate.</param>
        /// <returns>Returns a value based on the object ID and seed.</returns>
        public static int FromID(string seed, string id) => id.GetHashCode() ^ seed.GetHashCode();

        /// <summary>
        /// Gets a "random" value within a range from a specific ID and seed.
        /// </summary>
        /// <param name="seed">Seed to calculate.</param>
        /// <param name="id">Object ID to calculate.</param>
        /// <param name="min">Minimum range.</param>
        /// <param name="max">Maximum range.</param>
        /// <returns>Returns a value based on the object ID and seed.</returns>
        public static int FromIDRange(string seed, string id, int min, int max) => UnityEngine.Mathf.RoundToInt(RTMath.Lerp(min, max, Single(GetHash(id, seed))));

        /// <summary>
        /// Gets a "random" value within a range from a specific ID and seed.
        /// </summary>
        /// <param name="seed">Seed to calculate.</param>
        /// <param name="index">Object index to calculate.</param>
        /// <param name="min">Minimum range.</param>
        /// <param name="max">Maximum range.</param>
        /// <returns>Returns a value based on the object index and seed.</returns>
        public static float SingleFromIndexRange(string seed, int index, float min, float max) => RTMath.Lerp(min, max, Single(GetHash(index, seed)));

        /// <summary>
        /// Gets a "random" value from a specific ID and seed.
        /// </summary>
        /// <param name="seed">Seed to calculate.</param>
        /// <param name="index">Object index to calculate.</param>
        /// <returns>Returns a value based on the object index and seed.</returns>
        public static int FromIndex(string seed, int index) => index.GetHashCode() ^ seed.GetHashCode();

        /// <summary>
        /// Gets a "random" value within a range from a specific ID and seed.
        /// </summary>
        /// <param name="seed">Seed to calculate.</param>
        /// <param name="index">Object index to calculate.</param>
        /// <param name="min">Minimum range.</param>
        /// <param name="max">Maximum range.</param>
        /// <returns>Returns a value based on the object index and seed.</returns>
        public static int FromIndexRange(string seed, int index, int min, int max) => UnityEngine.Mathf.RoundToInt(RTMath.Lerp(min, max, Single(GetHash(index, seed))));

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
        /// Initializes a new Random class based on a provided seed and checks for a true or false value.
        /// </summary>
        /// <param name="hash">Object hash code.</param>
        /// <returns>Returns a random true or false value based on the seed.</returns>
        public static bool IsTrue(int hash) => IntFromRange(hash, 0, 1) == 1;

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

        public static class KeyframeRandomizer
        {
            public static float RandomizeFloatKeyframe(string id, EventKeyframe eventKeyframe, int index = 0, int kfIndex = 0)
            {
                if (!CoreConfig.Instance.UseSeedBasedRandom.Value)
                    return RandomizeFloatKeyframe(eventKeyframe, index);

                var round = eventKeyframe.randomValues.Length > 2 && eventKeyframe.randomValues[2] != 0f;
                var hash = GetHash(id, kfIndex, CurrentSeed);
                return eventKeyframe.RandomType switch
                {
                    RandomType.Normal => round ?
                            RTMath.RoundToNearestNumber(SingleFromRange(hash, eventKeyframe.values[index], eventKeyframe.randomValues[0]), eventKeyframe.randomValues[2]) :
                            SingleFromRange(hash, eventKeyframe.values[index], eventKeyframe.randomValues[0]),
                    RandomType.BETA_SUPPORT => UnityEngine.Mathf.Round(SingleFromRange(hash, eventKeyframe.values[index], eventKeyframe.randomValues[0])),
                    RandomType.Toggle => (Single(hash) > 0.5f) ? eventKeyframe.values[index] : eventKeyframe.randomValues[0],
                    RandomType.Scale => eventKeyframe.values[index] * (round ?
                                RTMath.RoundToNearestNumber(SingleFromRange(hash, eventKeyframe.randomValues[0], eventKeyframe.randomValues[1]), eventKeyframe.randomValues[2]) :
                                SingleFromRange(hash, eventKeyframe.randomValues[0], eventKeyframe.randomValues[1])),
                    _ => 0f
                };
            }

            public static UnityEngine.Vector2 RandomizeVector2Keyframe(string id, EventKeyframe eventKeyframe, int kfIndex = 0)
            {
                if (!CoreConfig.Instance.UseSeedBasedRandom.Value)
                    return RandomizeVector2Keyframe(eventKeyframe);

                float x = 0f;
                float y = 0f;
                var round = eventKeyframe.randomValues.Length > 2 && eventKeyframe.randomValues[2] != 0f;
                switch ((RandomType)eventKeyframe.random)
                {
                    case RandomType.Normal: {
                            var xHash = GetHash(id, kfIndex, "X", CurrentSeed);
                            var yHash = GetHash(id, kfIndex, "Y", CurrentSeed);

                            if (round)
                            {
                                x = (eventKeyframe.values[0] == eventKeyframe.randomValues[0]) ?
                                        eventKeyframe.values[0] :
                                        RTMath.RoundToNearestNumber(SingleFromRange(xHash, eventKeyframe.values[0], eventKeyframe.randomValues[0]), eventKeyframe.randomValues[2]);
                                y = (eventKeyframe.values[1] == eventKeyframe.randomValues[1]) ?
                                        eventKeyframe.values[1] :
                                        RTMath.RoundToNearestNumber(SingleFromRange(yHash, eventKeyframe.values[1], eventKeyframe.randomValues[1]), eventKeyframe.randomValues[2]);
                            }
                            else
                            {
                                x = SingleFromRange(xHash, eventKeyframe.values[0], eventKeyframe.randomValues[0]);
                                y = SingleFromRange(yHash, eventKeyframe.values[1], eventKeyframe.randomValues[1]);
                            }
                            break;
                        }
                    case RandomType.BETA_SUPPORT: {
                            var xHash = GetHash(id, kfIndex, "X", CurrentSeed);
                            var yHash = GetHash(id, kfIndex, "Y", CurrentSeed);

                            x = UnityEngine.Mathf.Round(SingleFromRange(xHash, eventKeyframe.values[0], eventKeyframe.randomValues[0]));
                            y = UnityEngine.Mathf.Round(SingleFromRange(yHash, eventKeyframe.values[1], eventKeyframe.randomValues[1]));
                            break;
                        }
                    case RandomType.Toggle: {
                            var hash = GetHash(id, kfIndex, CurrentSeed);
                            bool toggle = Single(hash) > 0.5f;
                            x = toggle ? eventKeyframe.values[0] : eventKeyframe.randomValues[0];
                            y = toggle ? eventKeyframe.values[1] : eventKeyframe.randomValues[1];
                            break;
                        }
                    case RandomType.Scale: {
                            var hash = GetHash(id, kfIndex, CurrentSeed);
                            float multiply = SingleFromRange(hash, eventKeyframe.randomValues[0], eventKeyframe.randomValues[1]);

                            if (round)
                                multiply = RTMath.RoundToNearestNumber(multiply, eventKeyframe.randomValues[2]);

                            x = eventKeyframe.values[0] * multiply;
                            y = eventKeyframe.values[1] * multiply;
                            break;
                        }
                }
                return new UnityEngine.Vector2(x, y);
            }

            public static float RandomizeFloatKeyframe(EventKeyframe eventKeyframe, int index = 0) => eventKeyframe.RandomType switch
            {
                RandomType.Normal => eventKeyframe.randomValues.Length > 2 && eventKeyframe.randomValues[2] != 0f ?
                        RTMath.RoundToNearestNumber(UnityEngine.Random.Range(eventKeyframe.values[index], eventKeyframe.randomValues[0]), eventKeyframe.randomValues[2]) :
                        UnityEngine.Random.Range(eventKeyframe.values[index], eventKeyframe.randomValues[0]),
                RandomType.BETA_SUPPORT => UnityEngine.Mathf.Round(UnityEngine.Random.Range(eventKeyframe.values[index], eventKeyframe.randomValues[0])),
                RandomType.Toggle => (UnityEngine.Random.value > 0.5f) ? eventKeyframe.values[index] : eventKeyframe.randomValues[0],
                RandomType.Scale => eventKeyframe.values[index] * eventKeyframe.randomValues.Length > 2 && eventKeyframe.randomValues[2] != 0f ?
                            RTMath.RoundToNearestNumber(UnityEngine.Random.Range(eventKeyframe.randomValues[0], eventKeyframe.randomValues[1]), eventKeyframe.randomValues[2]) :
                            UnityEngine.Random.Range(eventKeyframe.randomValues[0], eventKeyframe.randomValues[1]),
                _ => 0f
            };

            public static UnityEngine.Vector2 RandomizeVector2Keyframe(EventKeyframe eventKeyframe, int xIndex = 0, int yIndex = 1)
            {
                float x = 0f;
                float y = 0f;
                switch (eventKeyframe.RandomType)
                {
                    case RandomType.Normal: {
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
                    case RandomType.BETA_SUPPORT: {
                            x = UnityEngine.Mathf.Round(UnityEngine.Random.Range(eventKeyframe.values[xIndex], eventKeyframe.randomValues[0]));
                            y = UnityEngine.Mathf.Round(UnityEngine.Random.Range(eventKeyframe.values[yIndex], eventKeyframe.randomValues[1]));
                            break;
                        }
                    case RandomType.Toggle: {
                            bool toggle = UnityEngine.Random.value > 0.5f;
                            x = toggle ? eventKeyframe.values[xIndex] : eventKeyframe.randomValues[0];
                            y = toggle ? eventKeyframe.values[yIndex] : eventKeyframe.randomValues[1];
                            break;
                        }
                    case RandomType.Scale: {
                            float multiply = eventKeyframe.randomValues.Length > 2 && eventKeyframe.randomValues[2] != 0f ?
                                RTMath.RoundToNearestNumber(UnityEngine.Random.Range(eventKeyframe.randomValues[0], eventKeyframe.randomValues[1]), eventKeyframe.randomValues[2]) :
                                UnityEngine.Random.Range(eventKeyframe.randomValues[0], eventKeyframe.randomValues[1]);

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
