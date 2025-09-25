using System.Collections.Generic;

namespace BetterLegacy.Core.Data
{
    /* TODO:
    - Allow editor layout, editor themes and editor complexity to be customized via the asset pack.
      For example, you could make the object origin toggles display in Advanced editor complexity.
    - Move Story folder to beatmaps? Or should it be considered an asset?
    - Custom interfaces (main menu, pause menu, etc) can be loaded.
     */

    /// <summary>
    /// Represents a package of mod assets and resources. Assets can be customized via adding a new asset pack to <see cref="AssetPacks"/>.<br></br>
    /// If you want to get an asset, use <see cref="GetFile(string)"/>.
    /// </summary>
    public class AssetPack : Exists
    {
        public AssetPack() { }

        public AssetPack(string path) => this.path = path;

        /// <summary>
        /// The default asset pack.
        /// </summary>
        public static AssetPack BuiltIn { get; } = new AssetPack(RTFile.CombinePaths(RTFile.ApplicationDirectory, RTFile.BepInExAssetsPath));

        /// <summary>
        /// List of loaded asset packs.
        /// </summary>
        public static List<AssetPack> AssetPacks { get; set; } = new List<AssetPack>();

        /// <summary>
        /// File path to the asset pack.
        /// </summary>
        public string path;

        /// <summary>
        /// Gets an asset file.
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns>Returns a combined file path of the found asset.
        /// <br></br>Searches through the loaded asset packs for the specified file. If any of the loaded asset packs contains the file, then returns the asset pack file path and the <paramref name="assetPath"/>.
        /// <br></br>Otherwise if none of the loaded asset packs contains the file, returns the built-in asset pack file path and the <paramref name="assetPath"/>.</returns>
        public static string GetFile(string assetPath)
        {
            for (int i = AssetPacks.Count - 1; i >= 0; i--)
            {
                var assetPack = AssetPacks[i];
                if (assetPack.HasFile(assetPath))
                    return RTFile.CombinePaths(assetPack.path, assetPath);
            }

            return RTFile.CombinePaths(BuiltIn.path, assetPath);
        }

        /// <summary>
        /// If the asset pack has a file.
        /// </summary>
        /// <param name="assetPath">Asset file path.</param>
        /// <returns>Returns true if the asset pack contains the file, otherwise returns false.</returns>
        public bool HasFile(string assetPath) => RTFile.FileExists(RTFile.CombinePaths(path, assetPath));

        /// <summary>
        /// If the asset pack has a directory.
        /// </summary>
        /// <param name="assetPath">Asset directory path.</param>
        /// <returns>Returns true if the asset pack contains the directory, otherwise returns false.</returns>
        public bool HasDirectory(string assetPath) => RTFile.DirectoryExists(RTFile.CombinePaths(path, assetPath));
    }
}
