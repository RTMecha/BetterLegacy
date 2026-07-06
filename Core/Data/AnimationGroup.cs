using System.Collections.Generic;
using System.Linq;

using SimpleJSON;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Runtime.Objects;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Represents a collection of animations that can be applied to a set of objects.
    /// </summary>
    public class AnimationGroup : PAObject<AnimationGroup>, IPacket, IPrefabable
    {
        #region Constructors

        public AnimationGroup() : base() { }
        
        public AnimationGroup(string name, string description) : base()
        {
            this.name = name;
            this.description = description;
        }

        #endregion

        #region Values

        /// <summary>
        /// Name of the animation group.
        /// </summary>
        public string name;

        /// <summary>
        /// Description of the animation group.
        /// </summary>
        public string description;

        /// <summary>
        /// List of animations the group contains.
        /// </summary>
        public List<PAAnimation> animations = new List<PAAnimation>();

        public PAAnimation this[int index]
        {
            get => animations[index];
            set => animations[index] = value;
        }

        /// <summary>
        /// Amount of animations.
        /// </summary>
        public int Count => animations.Count;

        /// <summary>
        /// If the animation group should be collapsed in the editor.
        /// </summary>
        public bool collapse;

        /// <summary>
        /// Total length of the animation group.
        /// </summary>
        public float AnimLength => animations.IsEmpty() ? 0f : animations.Max(x => x.AnimLength);

        #region Prefab

        public string OriginalID { get; set; }

        public string PrefabID { get; set; }

        public string PrefabInstanceID { get; set; }

        public bool FromPrefab { get; set; }

        public Prefab CachedPrefab { get; set; }

        public PrefabObject CachedPrefabObject { get; set; }

        public float StartTime { get; set; }

        #endregion

        #endregion

        #region Functions

        public override void CopyData(AnimationGroup orig, bool newID = true)
        {
            id = newID ? GetStringID() : orig.id;
            name = orig.name;
            description = orig.description;
            animations = new List<PAAnimation>(orig.animations.Select(x => x.Copy(false)));
            collapse = orig.collapse;

            this.SetPrefabReference(orig);
        }

        public override void ReadJSON(JSONNode jn)
        {
            if (jn == null)
                return;

            id = jn["id"];
            if (string.IsNullOrEmpty(id))
                id = GetStringID();
            name = jn["name"];
            description = jn["desc"];
            if (jn["anims"] != null)
                animations = Parser.ParseObjectList<PAAnimation>(jn["anims"]);
            if (jn["collapse"] != null)
                collapse = jn["collapse"].AsBool;

            this.ReadPrefabJSON(jn);
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["id"] = !string.IsNullOrEmpty(id) ? id : GetStringID();
            if (!string.IsNullOrEmpty(name))
                jn["name"] = name;
            if (!string.IsNullOrEmpty(description))
                jn["desc"] = description;
            if (animations != null && !animations.IsEmpty())
                jn["anims"] = Parser.ObjectListToJSON(animations);
            if (collapse)
                jn["collapse"] = collapse;

            this.WritePrefabJSON(jn);

            return jn;
        }

        public void ReadPacket(NetworkReader reader)
        {
            id = reader.ReadString();

            #region Interface

            this.ReadPrefabPacket(reader);

            #endregion

            name = reader.ReadString();
            description = reader.ReadString();
            Packet.ReadPacketList(animations, reader);
            collapse = reader.ReadBoolean();
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(id);

            #region Interface

            this.WritePrefabPacket(writer);

            #endregion

            writer.Write(name);
            writer.Write(description);
            Packet.WritePacketList(animations, writer);
            writer.Write(collapse);
        }

        public float GetObjectLifeLength(float offset = 0f, bool noAutokill = false, bool collapse = false) => AnimLength + offset;

        public IRTObject GetRuntimeObject() => default;

        #endregion
    }
}
