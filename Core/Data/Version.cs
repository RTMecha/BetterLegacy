using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using VersionComparison = DataManager.VersionComparison;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Represents a semantic version.
    /// </summary>
    public struct Version
    {
        public Version(string prefix, int major, int minor, int patch, string iteration)
        {
            Prefix = prefix;
            Major = major;
            Minor = minor;
            Patch = patch;
            Iteration = iteration;

            BuildDate = DateTime.Now.ToString("G");
        }

        public Version(string prefix, int major, int minor, int patch) : this(prefix, major, minor, patch, null) { }

        public Version(int major, int minor, int patch, string iteration) : this(null, major, minor, patch, iteration) { }

        public Version(int major, int minor, int patch) : this(major, minor, patch, null) { }

        public Version(string ver)
        {
            this = Parse(ver);

            BuildDate = DateTime.Now.ToString("G");
        }

        /// <summary>
        /// Prefix build. What this version is used for. (e.g. snapshot, etc)
        /// </summary>
        public string Prefix { get; set; }
        /// <summary>
        /// Major build number. Represents a big milestone or huge backwards compatibility breaking changes.
        /// </summary>
        public int Major { get; set; }
        /// <summary>
        /// Minor build number. Represents a few additions and fixes.
        /// </summary>
        public int Minor { get; set; }
        /// <summary>
        /// Patch build number. Represents a few fixes.
        /// </summary>
        public int Patch { get; set; }

        /// <summary>
        /// Iteration letter. Represents a very small change from the last build.
        /// </summary>
        public string Iteration { get; set; }
        public int IterationIndex
        {
            get => string.IsNullOrEmpty(Iteration) ? 0 : RTString.alphabetLower.Select(x => x.ToString()).ToList().IndexOf(Iteration.ToLower());
            set
            {
                if (value >= 0 && value < RTString.alphabetLower.Length)
                    Iteration = RTString.alphabetLower[value].ToString();
            }
        }

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

        public override string ToString()
        {
            var result = $"{Major}.{Minor}.{Patch}{Iteration}";
            if (!string.IsNullOrEmpty(Prefix))
                result = $"{Prefix}-{result}";
            return result;
        }

        public override bool Equals(object obj) => obj is Version && (Version)obj == this;

        public override int GetHashCode()
        {
            var hash = Major.GetHashCode() ^ Minor.GetHashCode() ^ Patch.GetHashCode();
            if (!string.IsNullOrEmpty(Iteration))
                hash ^= Iteration.GetHashCode();
            return hash;
        }

        public VersionComparison CompareVersions(Version other) => other > this ? VersionComparison.GreaterThan : other< this ? VersionComparison.LessThan : VersionComparison.EqualTo;

        /// <summary>
        /// Copies a versions' values.
        /// </summary>
        /// <param name="other">Other to copy.</param>
        /// <returns>Returns a copied version.</returns>
        public static Version DeepCopy(Version other) => new Version(other.Major, other.Minor, other.Patch);

        /// <summary>
        /// Tries to parse a version from an input string.
        /// </summary>
        /// <param name="ver">Input string to parse.</param>
        /// <param name="result">Output version.</param>
        /// <returns>Returns true if parse was successful, otherwise returns false.</returns>
        public static bool TryParse(string ver, out Version result)
        {
            Match match;
            if (RTString.RegexMatch(ver, new Regex(@"(.*?)-([0-9]+).([0-9]+).([0-9]+)([a-z]+)"), out match))
            {
                var prefix = match.Groups[1].ToString();
                var major = int.Parse(match.Groups[2].ToString());
                var minor = int.Parse(match.Groups[3].ToString());
                var patch = int.Parse(match.Groups[4].ToString());

                string iteration = match.Groups[5].ToString();

                result = new Version(prefix, major, minor, patch, iteration);
                return true;
            }

            if (RTString.RegexMatch(ver, new Regex(@"(.*?)-([0-9]+).([0-9]+).([0-9]+)"), out match))
            {
                var prefix = match.Groups[1].ToString();
                var major = int.Parse(match.Groups[2].ToString());
                var minor = int.Parse(match.Groups[3].ToString());
                var patch = int.Parse(match.Groups[4].ToString());

                result = new Version(prefix, major, minor, patch);
                return true;
            }
            
            if (RTString.RegexMatch(ver, new Regex(@"([0-9]+).([0-9]+).([0-9]+)([a-z]+)"), out match))
            {
                var major = int.Parse(match.Groups[1].ToString());
                var minor = int.Parse(match.Groups[2].ToString());
                var patch = int.Parse(match.Groups[3].ToString());

                string iteration = match.Groups[4].ToString();

                result = new Version(major, minor, patch, iteration);
                return true;
            }

            if (RTString.RegexMatch(ver, new Regex(@"([0-9]+).([0-9]+).([0-9]+)"), out match))
            {
                var major = int.Parse(match.Groups[1].ToString());
                var minor = int.Parse(match.Groups[2].ToString());
                var patch = int.Parse(match.Groups[3].ToString());

                result = new Version(major, minor, patch);
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Parses a version from an input string/
        /// </summary>
        /// <param name="ver">Input string to parse.</param>
        /// <returns>Returns a parsed build version.</returns>
        public static Version Parse(string ver)
        {
            var list = ver.Split('.');

            if (list.Length < 0)
                throw new Exception("String must be correct format!");

            string prefix = null;
            if (ver.Contains('-'))
                prefix = ver.Split('-')[0];

            var major = int.Parse(list[0]);
            var minor = int.Parse(list[1]);
            var patch = int.Parse(list[2][0].ToString());

            string iteration = null;
            if (list[2].Length > 1)
                iteration = list[2][1].ToString();

            return new Version(prefix, major, minor, patch, iteration);
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
        public static bool operator !=(Version a, Version b) => !(a == b);

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
    }
}
