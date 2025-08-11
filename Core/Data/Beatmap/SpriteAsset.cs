using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Stores sprite data that can be reused.
    /// </summary>
    public class SpriteAsset : PAObject<SpriteAsset>
    {
        public SpriteAsset() : base() { }

        public SpriteAsset(string name) : this() => this.name = name;

        public SpriteAsset(string name, Sprite sprite) : this(name) => this.sprite = sprite;

        /// <summary>
        /// Name of the sprite.
        /// </summary>
        public string name = string.Empty;

        /// <summary>
        /// Contained sprite.
        /// </summary>
        public Sprite sprite;

        public override void CopyData(SpriteAsset orig, bool newID = true)
        {
            id = newID ? GetStringID() : orig.id;
            name = orig.name ?? string.Empty;
            sprite = orig.sprite;
        }

        public override void ReadJSON(JSONNode jn)
        {
            id = jn["id"] ?? GetStringID();
            name = jn["n"] ?? string.Empty;
            sprite = SpriteHelper.StringToSprite(jn["i"]);
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["id"] = id ?? GetStringID();
            jn["n"] = name ?? string.Empty;
            if (sprite)
                jn["i"] = SpriteHelper.SpriteToString(sprite);

            return jn;
        }

        public void UnloadSprite()
        {
            if (sprite)
                CoreHelper.Destroy(sprite);
            sprite = null;
        }

        public override string ToString() => name;
    }
}
