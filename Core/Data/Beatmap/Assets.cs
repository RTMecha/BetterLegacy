using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Represents stored assets in a level.
    /// </summary>
    public class Assets : PAObject<Assets>
    {
        public Assets() { }

        /// <summary>
        /// Stored sprite assets.
        /// </summary>
        public List<SpriteAsset> sprites = new List<SpriteAsset>();

        /// <summary>
        /// Stored sound assets.
        /// </summary>
        public List<SoundAsset> sounds = new List<SoundAsset>();

        /// <summary>
        /// Checks if the assets are empty.
        /// </summary>
        /// <returns>Returns true if <see cref="sprites"/> and <see cref="sounds"/> are empty, otherwise returns false.</returns>
        public bool IsEmpty() => sprites.IsEmpty() && sounds.IsEmpty();

        public override void CopyData(Assets orig, bool newID = true)
        {
            sprites = orig.sprites.Select(x => x.Copy(newID)).ToList();
            sounds = orig.sounds.Select(x => x.Copy(newID)).ToList();
        }

        public override void ReadJSON(JSONNode jn)
        {
            Clear();

            if (jn["spr"] != null)
                for (int i = 0; i < jn["spr"].Count; i++)
                {
                    if (jn["spr"][i]["i"] != null)
                    {
                        sprites.Add(SpriteAsset.Parse(jn["spr"][i]));
                        continue;
                    }

                    var data = jn["spr"][i]["d"];
                    byte[] imageData = new byte[data.Count];
                    for (int j = 0; j < data.Count; j++)
                        imageData[j] = (byte)data[j].AsInt;

                    var texture2d = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                    texture2d.LoadImage(imageData);

                    texture2d.wrapMode = TextureWrapMode.Clamp;
                    texture2d.filterMode = FilterMode.Point;
                    texture2d.Apply();

                    sprites.Add(new SpriteAsset(jn["spr"][i]["n"], SpriteHelper.CreateSprite(texture2d)));
                }

            if (jn["snd"] != null)
                for (int i = 0; i < jn["snd"].Count; i++)
                    sounds.Add(SoundAsset.Parse(jn["snd"][i]));
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            for (int i = 0; i < sprites.Count; i++)
                jn["spr"][i] = sprites[i].ToJSON();

            for (int i = 0; i < sounds.Count; i++)
                jn["snd"][i] = sounds[i].ToJSON();

            return jn;
        }

        /// <summary>
        /// Clears all assets.
        /// </summary>
        public void Clear()
        {
            sprites.Clear();
            sounds.Clear();
        }

        /// <summary>
        /// Gets a sprite from the asset list.
        /// </summary>
        /// <param name="name">Name of the sprite to get.</param>
        /// <returns>Returns a stored sprite.</returns>
        public Sprite GetSprite(string name) => sprites.TryFind(x => x.name == name, out SpriteAsset spriteAsset) ? spriteAsset.sprite : null;

        /// <summary>
        /// Registers a sprite to the sprite asset list.
        /// </summary>
        /// <param name="name">Name of the sprite.</param>
        /// <param name="sprite">Sprite to store.</param>
        public void AddSprite(string name, Sprite sprite)
        {
            if (sprites.TryFind(x => x.name == name, out SpriteAsset spriteAsset))
                spriteAsset.sprite = sprite;
            else
                sprites.Add(new SpriteAsset(name, sprite));
        }

        /// <summary>
        /// Removes the sprite from the sprite asset list.
        /// </summary>
        /// <param name="name">Name of the sprite to remove.</param>
        /// <returns>Returns true if a sprite asset was successfully removed, otherwise returns false.</returns>
        public bool RemoveSprite(string name) => sprites.Remove(x => x.name == name);

        /// <summary>
        /// Adds and loads a sound to the sound assets list.
        /// </summary>
        /// <param name="name">Name of the sound asset to add.</param>
        public void AddAndLoadSound(string name)
        {
            if (sounds.Has(x => x.name == name))
                return;

            var soundAsset = new SoundAsset(name, null);
            CoroutineHelper.StartCoroutine(soundAsset.LoadAudioClip());
            sounds.Add(soundAsset);
        }
    }
}
