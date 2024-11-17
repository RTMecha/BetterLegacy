using BetterLegacy.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using VersionComparison = DataManager.VersionComparison;

namespace BetterLegacy.Core
{
    public struct Version
    {
        public Version(int major, int minor, int patch, string iteration)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Iteration = iteration;

            BuildDate = DateTime.Now.ToString("G");
        }

        public Version(int major, int minor, int patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Iteration = "";

            BuildDate = DateTime.Now.ToString("G");
        }

        public Version(string ver)
        {
            this = Parse(ver);

            BuildDate = DateTime.Now.ToString("G");
        }

        public int Major { get; set; }
        public int Minor { get; set; }
        public int Patch { get; set; }

        public string Iteration { get; set; }
        public int IterationIndex
        {
            get => string.IsNullOrEmpty(Iteration) ? 0 : Alphabet.IndexOf(Iteration.ToLower());
            set
            {
                if (value >= 0 && value < Alphabet.Count)
                    Iteration = Alphabet[value];
            }
        }

        public string Full => $"{Major}.{Minor}.{Patch}{Iteration}";

        public string BuildDate { get; set; }

        public int this[int index]
        {
            get => index switch
            {
                0 => Major,
                1 => Minor,
                2 => Patch,
                3 => IterationIndex,
                _ => throw new IndexOutOfRangeException("Invalid Version index!"),
            };
            set
            {
                switch (index)
                {
                    case 0: Major = value; break;
                    case 1: Minor = value; break;
                    case 2: Patch = value; break;
                    case 3: IterationIndex = value; break;
                }
            }
        }

        public override string ToString() => Full;

        public override bool Equals(object obj) => obj is Version && (Version)obj == this;

        public override int GetHashCode() => base.GetHashCode();

        public VersionComparison CompareVersions(Version other)
            => other > this ? VersionComparison.GreaterThan : other< this ? VersionComparison.LessThan : VersionComparison.EqualTo;

        public static Version DeepCopy(Version other) => new Version
        {
            Major = other.Major,
            Minor = other.Minor,
            Patch = other.Patch,
            Iteration = other.Iteration
        };

        public static bool TryParse(string ver, out Version result)
        {
            if (RTString.RegexMatch(ver, new Regex(@"([0-9]+).([0-9]+).([0-9]+)([a-z]+)"), out Match match))
            {
                var major = int.Parse(match.Groups[1].ToString());
                var minor = int.Parse(match.Groups[2].ToString());
                var patch = int.Parse(match.Groups[3].ToString());

                string iteration = match.Groups[4].ToString();

                result = new Version(major, minor, patch, iteration);
                return true;
            }

            if (RTString.RegexMatch(ver, new Regex(@"([0-9]+).([0-9]+).([0-9]+)"), out Match match2))
            {
                var major = int.Parse(match2.Groups[1].ToString());
                var minor = int.Parse(match2.Groups[2].ToString());
                var patch = int.Parse(match2.Groups[3].ToString());

                result = new Version(major, minor, patch);
                return true;
            }

            result = default;
            return false;
        }

        public static Version Parse(string ver)
        {
            var list = ver.Split('.').ToList();

            if (list.Count < 0)
                throw new Exception("String must be correct format!");

            var major = int.Parse(list[0]);
            var minor = int.Parse(list[1]);
            var patch = int.Parse(list[2][0].ToString());

            string iteration;
            if (list[2].Length > 1)
                iteration = list[2][1].ToString();
            else
                iteration = "";

            return new Version(major, minor, patch, iteration);
        }

        #region Operators

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(Version a, Version b)
        {
            if (a.Major > b.Major)
                return true;
            if (a.Minor > b.Minor)
                return true;
            if (a.Patch > b.Patch)
                return true;
            if (a.IterationIndex > b.IterationIndex)
                return true;

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(Version a, Version b)
        {
            if (a.Major < b.Major)
                return true;
            if (a.Minor < b.Minor && a.Major <= b.Major)
                return true;
            if (a.Patch < b.Patch && a.Minor <= b.Minor && a.Major <= b.Major)
                return true;
            if (!string.IsNullOrEmpty(a.Iteration) && !string.IsNullOrEmpty(b.Iteration) && a.IterationIndex < b.IterationIndex && a.Patch <= b.Patch && a.Minor <= b.Minor && a.Major <= b.Major)
                return true;

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Version a, Version b)
        {
            if (a.Major == b.Major)
                return true;
            if (a.Minor == b.Minor)
                return true;
            if (a.Patch == b.Patch)
                return true;
            if (!string.IsNullOrEmpty(a.Iteration) && !string.IsNullOrEmpty(b.Iteration) && a.IterationIndex == b.IterationIndex)
                return true;

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Version a, Version b)
        {
            if (a.Major != b.Major)
                return true;
            if (a.Minor != b.Minor)
                return true;
            if (a.Patch != b.Patch)
                return true;
            if (a.IterationIndex != b.IterationIndex)
                return true;

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(Version a, Version b)
        {
            if (a.Major >= b.Major)
                return true;
            if (a.Minor >= b.Minor)
                return true;
            if (a.Patch >= b.Patch)
                return true;
            if (a.IterationIndex >= b.IterationIndex)
                return true;

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(Version a, Version b)
        {
            if (a.Major <= b.Major)
                return true;
            if (a.Minor <= b.Minor && a.Major <= b.Major)
                return true;
            if (a.Patch <= b.Patch && a.Minor <= b.Minor && a.Major <= b.Major)
                return true;
            if (!string.IsNullOrEmpty(a.Iteration) && !string.IsNullOrEmpty(b.Iteration) && a.IterationIndex <= b.IterationIndex && a.Patch <= b.Patch && a.Minor <= b.Minor && a.Major <= b.Major)
                return true;

            return false;
        }

        #endregion

        static List<string> Alphabet => new List<string>
        {
                "a",
                "b",
                "c",
                "d",
                "e",
                "f",
                "g",
                "h",
                "i",
                "j",
                "k",
                "l",
                "m",
                "n",
                "o",
                "p",
                "q",
                "r",
                "s",
                "t",
                "u",
                "v",
                "w",
                "x",
                "y",
                "z"
        };

        //readonly static List<string> alphabet = List<string>
        //{
        //    "a",
        //    "b",
        //    "c",
        //    "d",
        //    "e",
        //    "f",
        //    "g",
        //    "h",
        //    "i",
        //    "j",
        //    "k",
        //    "l",
        //    "m",
        //    "n",
        //    "o",
        //    "p",
        //    "q",
        //    "r",
        //    "s",
        //    "t",
        //    "u",
        //    "v",
        //    "w",
        //    "x",
        //    "y",
        //    "z"
        //};
    }
}
