namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Used for simplifying null checks.
    /// </summary>
    public class Exists { public static implicit operator bool(Exists exists) => exists != null; }
}
