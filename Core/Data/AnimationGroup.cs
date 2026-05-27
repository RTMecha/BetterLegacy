using System.Collections.Generic;
using System.Linq;

using SimpleJSON;

namespace BetterLegacy.Core.Data
{
    /// <summary>
    /// Represents a collection of animations that can be applied to a set of objects.
    /// </summary>
    public class AnimationGroup : PAObject<AnimationGroup>
    {
        #region Constructors

        public AnimationGroup() { }
        
        public AnimationGroup(string name, string description)
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

        #endregion

        #region Functions

        public override void CopyData(AnimationGroup orig, bool newID = true)
        {
            name = orig.name;
            description = orig.description;
            animations = new List<PAAnimation>(orig.animations.Select(x => x.Copy(false)));
            collapse = orig.collapse;
        }

        public override void ReadJSON(JSONNode jn)
        {
            if (jn == null)
                return;

            name = jn["name"];
            description = jn["desc"];
            if (jn["anims"] != null)
                animations = Parser.ParseObjectList<PAAnimation>(jn["anims"]);
            if (jn["collapse"] != null)
                collapse = jn["collapse"].AsBool;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            if (!string.IsNullOrEmpty(name))
                jn["name"] = name;
            if (!string.IsNullOrEmpty(description))
                jn["desc"] = description;
            if (animations != null && !animations.IsEmpty())
                jn["anims"] = Parser.ObjectListToJSON(animations);
            if (collapse)
                jn["collapse"] = collapse;

            return jn;
        }

        #endregion
    }
}
