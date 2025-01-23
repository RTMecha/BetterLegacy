namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Used for simplifying null checks.
    /// </summary>
    public abstract class Exists { public static implicit operator bool(Exists exists) => exists != null; }
}
