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

        /// <summary>
        /// How the texture should wrap.
        /// </summary>
        public TextureWrapMode wrapMode = TextureWrapMode.Clamp;

        public override void CopyData(SpriteAsset orig, bool newID = true)
        {
            id = newID ? GetStringID() : orig.id;
            name = orig.name ?? string.Empty;
            sprite = orig.sprite;
            wrapMode = orig.wrapMode;
        }

        public override void ReadJSON(JSONNode jn)
        {
            id = jn["id"] ?? GetStringID();
            name = jn["n"] ?? string.Empty;
            if (jn["w"] != null)
                wrapMode = (TextureWrapMode)jn["w"].AsInt;
            sprite = SpriteHelper.StringToSprite(jn["i"], textureWrapMode: wrapMode);
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["id"] = id ?? GetStringID();
            jn["n"] = name ?? string.Empty;
            if (sprite)
                jn["i"] = SpriteHelper.SpriteToString(sprite);
            if (wrapMode != TextureWrapMode.Clamp)
                jn["w"] = (int)wrapMode;

            return jn;
        }

        /// <summary>
        /// Unloads the sprite.
        /// </summary>
        public void UnloadSprite()
        {
            if (sprite)
                CoreHelper.Destroy(sprite);
            sprite = null;
        }

        /// <summary>
        /// Sets the wrap mode of the sprite texture.
        /// </summary>
        /// <param name="wrapMode">Wrap mode to set.</param>
        public void SetWrapMode(TextureWrapMode wrapMode)
        {
            this.wrapMode = wrapMode;
            if (sprite && sprite.texture)
                sprite.texture.wrapMode = wrapMode;
        }

        public override string ToString() => name;
    }
}
