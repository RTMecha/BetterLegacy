using System;
namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Represents a chat message in the Lobby Manager chat sub tab.
    /// </summary>
    public class ChatMessage
    {
        /// <summary>
        /// Name of the user that sent the message.
        /// </summary>
        public string name;

        /// <summary>
        /// Color of the user.
        /// </summary>
        public string colorHex;

        /// <summary>
        /// Text of the message.
        /// </summary>
        public string text;

        /// <summary>
        /// Time the message was sent.
        /// </summary>
        public DateTime timeUtc;

        /// <summary>
        /// Type of the message.
        /// </summary>
        public ChatMessageKind kind;

        /// <summary>
        /// Path to a referenced level.
        /// </summary>
        public string referenceLevelPath;

        /// <summary>
        /// Type of reference if the message is a reference type.
        /// </summary>
        public ReferenceKind referenceKind;

        /// <summary>
        /// Reference message used for selecting when the message is clicked.
        /// </summary>
        public string referenceIds;

        /// <summary>
        /// ID of the message.
        /// </summary>
        public Guid id = Guid.NewGuid();

        /// <summary>
        /// If the reference was a valid one.
        /// </summary>
        public bool referenceWasValid;

        /// <summary>
        /// If the reference was unloaded.
        /// </summary>
        public bool referenceDead;
    }

    /// <summary>
    /// The type of a chat message.
    /// </summary>
    public enum ChatMessageKind
    {
        /// <summary>
        /// The message was sent by a player.
        /// </summary>
        Player,
        /// <summary>
        /// The message was sent by the system.
        /// </summary>
        System,
    }

    /// <summary>
    /// The type of reference if a chat message was a reference type.
    /// </summary>
    public enum ReferenceKind
    {
        /// <summary>
        /// The chat message is a normal message.
        /// </summary>
        None,
        /// <summary>
        /// The chat message references a list of timeline objects that can be selected when clicking the message.
        /// </summary>
        Objects,
        /// <summary>
        /// The chat message references a marker that can be selected when clicking the message.
        /// </summary>
        Marker,
        /// <summary>
        /// The chat message references a layer that the user can be sent to when clicking the message.
        /// </summary>
        Layer,
        /// <summary>
        /// The chat message references a list of event keyframes that can be selected when clicking the message.
        /// </summary>
        Keyframes,
    }
}
