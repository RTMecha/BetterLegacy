using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

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
            get
            {
                switch (index)
                {
                    case 0: return Major;
                    case 1: return Minor;
                    case 2: return Patch;
                    case 3: return IterationIndex;
                    default: throw new IndexOutOfRangeException("Invalid Version index!");
                }
            }
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
        {
            if (other > this)
                return VersionComparison.GreaterThan;
            if (other < this)
                return VersionComparison.LessThan;

            return VersionComparison.GreaterThan;
        }

        public static Version DeepCopy(Version other) => new Version
        {
            Major = other.Major,
            Minor = other.Minor,
            Patch = other.Patch,
            Iteration = other.Iteration
        };

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
            if (a.Minor < b.Minor)
                return true;
            if (a.Patch < b.Patch)
                return true;
            if (a.IterationIndex < b.IterationIndex)
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
            if (a.IterationIndex == b.IterationIndex)
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
            if (a.Minor <= b.Minor)
                return true;
            if (a.Patch <= b.Patch)
                return true;
            if (a.IterationIndex <= b.IterationIndex)
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
