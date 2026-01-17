using UnityEngine;

using SimpleJSON;

using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Stores sprite data that can be reused.
    /// </summary>
    public class SpriteAsset : PAObject<SpriteAsset>, IPacket
    {
        #region Constructors

        public SpriteAsset() : base() { }

        public SpriteAsset(string name) : this() => this.name = name;

        public SpriteAsset(string name, Sprite sprite) : this(name) => this.sprite = sprite;

        #endregion

        #region Values

        /// <summary>
        /// Name of the sprite.
        /// </summary>
        public string name = string.Empty;

        /// <summary>
        /// How the texture should wrap.
        /// </summary>
        public TextureWrapMode wrapMode = TextureWrapMode.Clamp;

        /// <summary>
        /// Contained sprite.
        /// </summary>
        public Sprite sprite;

        #endregion

        #region Functions

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
            if (wrapMode != TextureWrapMode.Clamp)
                jn["w"] = (int)wrapMode;
            if (sprite)
                jn["i"] = SpriteHelper.SpriteToString(sprite);

            return jn;
        }

        public void ReadPacket(NetworkReader reader)
        {
            id = reader.ReadString();
            name = reader.ReadString();
            wrapMode = (TextureWrapMode)reader.ReadByte();
            sprite = reader.ReadSprite();
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(id);
            writer.Write(name);
            writer.Write((byte)wrapMode);
            writer.Write(sprite);
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

        #endregion
    }
}
