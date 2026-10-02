using System;
namespace BetterLegacy.Core.Data
{
    public class ChatMessage
    {
        public string name;
        public string colorHex;
        public string text;
        public DateTime timeUtc;
        public ChatMessageKind kind;
        public string referenceLevelPath;
        public ReferenceKind referenceKind;
        public string referenceIds;
        public Guid id = Guid.NewGuid();
        public bool referenceWasValid;
        public bool referenceDead;
    }
    public enum ChatMessageKind
    {
        Player,
        System,
    }
    public enum ReferenceKind
    {
        None,
        Objects,
        Marker,
        Layer,
        Keyframes,
    }
}
