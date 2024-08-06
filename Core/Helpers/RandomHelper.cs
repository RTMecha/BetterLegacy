using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Core.Helpers
{
    public static class RandomHelper
    {
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
    }
}
