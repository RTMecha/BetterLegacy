using System.Collections.Generic;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Indicates an object can be uploaded to / downloaded from the Arcade server.
    /// </summary>
    public interface IUploadable
    {
        /// <summary>
        /// ID of the item on the server.
        /// </summary>
        public string ServerID { get; set; }

        /// <summary>
        /// Name of the owner.
        /// </summary>
        public string UploaderName { get; set; }

        /// <summary>
        /// User ID of the owner.
        /// </summary>
        public string UploaderID { get; set; }

        /// <summary>
        /// List of item collaborators that can post to the item on the server.
        /// </summary>
        public List<ServerUser> Uploaders { get; set; }

        /// <summary>
        /// Visibility of the item on the server.
        /// </summary>
        public ServerVisibility Visibility { get; set; }

        /// <summary>
        /// Changelog to display when updating the item on the server.
        /// </summary>
        public string Changelog { get; set; }

        /// <summary>
        /// Tag list that best represents what the item is.
        /// </summary>
        public List<string> ArcadeTags { get; set; }

        /// <summary>
        /// Specified update version of the item.
        /// </summary>
        public string ObjectVersion { get; set; }

        /// <summary>
        /// The date time the item was uploaded.
        /// </summary>
        public string DatePublished { get; set; }

        /// <summary>
        /// Incremental version number of the file on the server.
        /// </summary>
        public int VersionNumber { get; set; }
    }
}
