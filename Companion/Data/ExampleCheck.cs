using System;

using BetterLegacy.Core.Data;

namespace BetterLegacy.Companion.Data
{
    /// <summary>
    /// Represents how Example checks a specific thing is occuring.
    /// </summary>
    public class ExampleCheck : Exists
    {
        #region Constructors

        public ExampleCheck() { }

        public ExampleCheck(string key, Func<bool> check)
        {
            this.key = key;
            this.check = check;
        }

        #endregion

        #region Values

        /// <summary>
        /// Key of the check.
        /// </summary>
        public string key;

        /// <summary>
        /// The check function.
        /// </summary>
        public Func<bool> check;

        static ExampleCheck defaultCheck;
        /// <summary>
        /// The default check.
        /// </summary>
        public static ExampleCheck Default
        {
            get
            {
                if (!defaultCheck)
                    defaultCheck = new ExampleCheck(string.Empty, () => false);
                return defaultCheck;
            }
        }

        #endregion

        #region Functions

        /// <summary>
        /// Checks if the function is true.
        /// </summary>
        /// <returns>Returns the result of the check function.</returns>
        public bool Check()
        {
            try
            {
                return check?.Invoke() ?? false;
            }
            catch (Exception ex)
            {
                CompanionManager.LogError($"{nameof(ExampleCheck)} [{key}] encountered an exception while checking: {ex}");
                return false;
            }
        }

        public override string ToString() => key;

        #endregion
    }
}
