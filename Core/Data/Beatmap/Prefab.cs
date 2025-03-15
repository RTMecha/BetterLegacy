using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Managers;
using LSFunctions;
using SimpleJSON;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class Prefab : Exists
    {
        public Prefab() { }

        public Prefab(string name, int type, float offset, List<BeatmapObject> beatmapObjects, List<PrefabObject> prefabObjects)
        {
            this.name = name;
            this.type = type;
            typeID = PrefabType.prefabTypeLSIndexToID.TryGetValue(type, out string prefabTypeID) ? prefabTypeID : null;
            this.offset = offset;

            this.beatmapObjects.AddRange(beatmapObjects.Select(x => BeatmapObject.DeepCopy(x, false)));
            this.prefabObjects.AddRange(prefabObjects.Select(x => PrefabObject.DeepCopy(x, false)));

            float num = prefabObjects.Select(x => x.StartTime).Union(beatmapObjects.Select(x => x.StartTime)).Min(x => x);
            for (int i = 0; i < this.beatmapObjects.Count; i++)
                this.beatmapObjects[i].StartTime -= num;
            for (int i = 0; i < prefabObjects.Count; i++)
                this.prefabObjects[i].StartTime -= num;
        }

        #region Values

        public string name;

        public string id;

        public float offset;

        public string filePath;

        public string description;

        /// <summary>
        /// Only used for vanilla compatibility.
        /// </summary>
        public int type;

        public string typeID;

        #region Contents

        public List<BeatmapObject> beatmapObjects = new List<BeatmapObject>();

        public List<PrefabObject> prefabObjects = new List<PrefabObject>();

        public Dictionary<string, Sprite> SpriteAssets { get; set; } = new Dictionary<string, Sprite>();

        #endregion

        #endregion

        #region Methods

        public static Prefab DeepCopy(Prefab og, bool newID = true)
        {
            var prefab = new Prefab()
            {
                description = og.description,
                id = newID ? LSText.randomString(16) : og.id,
                name = og.name,
                offset = og.offset,
                prefabObjects = og.prefabObjects.Clone(),
                type = og.type,
                typeID = og.typeID,
            };

            prefab.beatmapObjects = new List<BeatmapObject>();
            prefab.beatmapObjects.AddRange(og.beatmapObjects.Select(x => BeatmapObject.DeepCopy(x, false)).ToList());

            foreach (var beatmapObject in prefab.beatmapObjects)
                if (beatmapObject.shape == 6 && !string.IsNullOrEmpty(beatmapObject.text) && og.SpriteAssets.TryGetValue(beatmapObject.text, out Sprite sprite))
                    prefab.SpriteAssets[beatmapObject.text] = sprite;

            return prefab;
        }

        public static Prefab ParseVG(JSONNode jn, Version version = default)
        {
            var beatmapObjects = new List<BeatmapObject>();
            for (int i = 0; i < jn["objs"].Count; i++)
                beatmapObjects.Add(BeatmapObject.ParseVG(jn["objs"][i], version));

            var prefab = new Prefab
            {
                id = jn["id"] ?? LSText.randomString(16),
                name = jn["n"],
                type = jn["type"].AsInt,
                offset = -jn["o"].AsFloat,
                beatmapObjects = beatmapObjects,
                prefabObjects = new List<PrefabObject>(),
                description = jn["description"],
            };
            prefab.typeID = PrefabType.prefabTypeVGIndexToID.TryGetValue(prefab.type, out string prefabTypeID) ? prefabTypeID : "";

            return prefab;
        }

        public static Prefab Parse(JSONNode jn)
        {
            var beatmapObjects = new List<BeatmapObject>();
            for (int j = 0; j < jn["objects"].Count; j++)
                beatmapObjects.Add(BeatmapObject.Parse(jn["objects"][j]));

            var prefabObjects = new List<PrefabObject>();
            for (int k = 0; k < jn["prefab_objects"].Count; k++)
                prefabObjects.Add(PrefabObject.Parse(jn["prefab_objects"][k]));

            var prefab = new Prefab
            {
                id = jn["id"],
                name = jn["name"],
                type = jn["type"].AsInt,
                offset = jn["offset"].AsFloat,
                beatmapObjects = beatmapObjects,
                prefabObjects = prefabObjects,
                description = jn["desc"] == null ? "" : jn["desc"],
                typeID = jn["type_id"],
            };

            if (string.IsNullOrEmpty(prefab.typeID))
                prefab.typeID = PrefabType.prefabTypeLSIndexToID.TryGetValue(prefab.type, out string prefabTypeID) ? prefabTypeID : "";

            if (jn["assets"] != null && jn["assets"]["spr"] != null)
            {
                for (int i = 0; i < jn["assets"]["spr"].Count; i++)
                {
                    var name = jn["assets"]["spr"][i]["n"];
                    var data = jn["assets"]["spr"][i]["d"];

                    if (!prefab.SpriteAssets.ContainsKey(name))
                    {
                        if (jn["assets"]["spr"][i]["i"] != null)
                        {
                            prefab.SpriteAssets.Add(name, SpriteHelper.StringToSprite(jn["assets"]["spr"][i]["i"]));
                            continue;
                        }

                        byte[] imageData = new byte[data.Count];
                        for (int j = 0; j < data.Count; j++)
                        {
                            imageData[j] = (byte)data[j].AsInt;
                        }

                        var texture2d = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                        texture2d.LoadImage(imageData);

                        texture2d.wrapMode = TextureWrapMode.Clamp;
                        texture2d.filterMode = FilterMode.Point;
                        texture2d.Apply();

                        prefab.SpriteAssets.Add(name, SpriteHelper.CreateSprite(texture2d));
                    }
                }
            }

            return prefab;
        }

        public JSONNode ToJSONVG()
        {
            var jn = JSON.Parse("{}");
            jn["n"] = name;
            if (id != null)
                jn["id"] = id;
            jn["type"] = PrefabType.prefabTypeVGIDToIndex.TryGetValue(typeID, out int prefabType) ? prefabType : 0;

            jn["o"] = -offset;

            jn["description"] = description ?? string.Empty;

            for (int i = 0; i < beatmapObjects.Count; i++)
                if (beatmapObjects[i] != null)
                    jn["objs"][i] = beatmapObjects[i].ToJSONVG();

            return jn;
        }

        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");
            jn["name"] = name;
            jn["type"] = (PrefabType.prefabTypeLSIDToIndex.TryGetValue(typeID, out int prefabType) ? prefabType : 0).ToString();
            jn["offset"] = offset;

            if (id != null)
                jn["id"] = id;

            if (typeID != null)
                jn["type_id"] = typeID;

            jn["desc"] = description ?? string.Empty;

            for (int i = 0; i < beatmapObjects.Count; i++)
                jn["objects"][i] = beatmapObjects[i].ToJSON();

            if (prefabObjects != null && !prefabObjects.IsEmpty())
                for (int i = 0; i < prefabObjects.Count; i++)
                    jn["prefab_objects"][i] = prefabObjects[i].ToJSON();

            var spriteAssets = new Dictionary<string, Sprite>();

            foreach (var obj in beatmapObjects)
                if (AssetManager.SpriteAssets.TryGetValue(obj.text, out Sprite sprite) && !spriteAssets.ContainsKey(obj.text))
                    spriteAssets.Add(obj.text, sprite);

            for (int i = 0; i < spriteAssets.Count; i++)
            {
                jn["assets"]["spr"][i]["n"] = spriteAssets.ElementAt(i).Key;
                jn["assets"]["spr"][i]["i"] = SpriteHelper.SpriteToString(spriteAssets.ElementAt(i).Value);
            }

            return jn;
        }

        /// <summary>
        /// Gets the type group of the prefab.
        /// </summary>
        /// <returns>Returns the prefab type.</returns>
        public PrefabType GetPrefabType() => RTPrefabEditor.inst && RTPrefabEditor.inst.prefabTypes.TryFind(x => x.id == typeID, out PrefabType prefabType) ? prefabType : PrefabType.InvalidType;

        #endregion

        #region Operators

        public static implicit operator bool(Prefab exists) => exists != null;

        public override bool Equals(object obj) => obj is Prefab && id == (obj as Prefab).id;

        public override int GetHashCode() => base.GetHashCode();

        public override string ToString() => id;

        #endregion
    }
}
