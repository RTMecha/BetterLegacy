namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Indicates an object can be read from and written to a file.
    /// </summary>
    public interface IFile
    {
        /// <summary>
        /// Format of the file.
        /// </summary>
        public FileFormat FileFormat { get; }
        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <returns>Returns the file name with the file extension. (e.g. "para_boss.lsp")</returns>
        public string GetFileName();
        /// <summary>
        /// Reads the objects' data from a file.
        /// </summary>
        /// <param name="path">File path to read from.</param>
        public void ReadFromFile(string path);
        /// <summary>
        /// Writes the objects' data to a file.
        /// </summary>
        /// <param name="path">File path to write to.</param>
        public void WriteToFile(string path);
    }
}
