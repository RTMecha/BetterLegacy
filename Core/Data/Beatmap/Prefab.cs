using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class Prefab : PAObject<Prefab>
    {
        public Prefab() : base() { }

        public Prefab(string name, int type, float offset, List<BeatmapObject> beatmapObjects, List<PrefabObject> prefabObjects) : this()
        {
            this.name = name;
            this.type = type;
            typeID = PrefabType.prefabTypeLSIndexToID.TryGetValue(type, out string prefabTypeID) ? prefabTypeID : null;
            this.offset = offset;

            this.beatmapObjects.AddRange(beatmapObjects.Select(x => x.Copy(false)));
            this.prefabObjects.AddRange(prefabObjects.Select(x => x.Copy(false)));

            float num = prefabObjects.Select(x => x.StartTime).Union(beatmapObjects.Select(x => x.StartTime)).Min(x => x);
            for (int i = 0; i < this.beatmapObjects.Count; i++)
                this.beatmapObjects[i].StartTime -= num;
            for (int i = 0; i < prefabObjects.Count; i++)
                this.prefabObjects[i].StartTime -= num;
        }

        #region Values

        public string name;

        public float offset;

        public string filePath;

        public string description;

        /// <summary>
        /// Only used for vanilla compatibility.
        /// </summary>
        public int type;

        public string typeID;

        #region Contents

        public Assets assets = new Assets();

        public List<BeatmapObject> beatmapObjects = new List<BeatmapObject>();

        public List<PrefabObject> prefabObjects = new List<PrefabObject>();
        
        #endregion

        #endregion

        #region Methods

        public override void CopyData(Prefab orig, bool newID = true)
        {
            description = orig.description;
            id = newID ? LSText.randomString(16) : orig.id;
            name = orig.name;
            offset = orig.offset;
            prefabObjects = orig.prefabObjects.Clone();
            type = orig.type;
            typeID = orig.typeID;

            beatmapObjects = new List<BeatmapObject>();
            if (!orig.beatmapObjects.IsEmpty())
                beatmapObjects.AddRange(orig.beatmapObjects.Select(x => x.Copy(false)).ToList());

            prefabObjects = new List<PrefabObject>();
            if (!orig.prefabObjects.IsEmpty())
                prefabObjects.AddRange(orig.prefabObjects.Select(x => x.Copy(false)).ToList());

            foreach (var beatmapObject in beatmapObjects)
            {
                if (beatmapObject.shape == 6 && !string.IsNullOrEmpty(beatmapObject.text) && orig.assets.sprites.TryFind(x => x.name == beatmapObject.text, out SpriteAsset spriteAsset))
                    assets.sprites.Add(spriteAsset.Copy());
            }
        }

        public override void ReadJSONVG(JSONNode jn, Version version = default)
        {
            beatmapObjects.Clear();
            for (int i = 0; i < jn["objs"].Count; i++)
                beatmapObjects.Add(BeatmapObject.ParseVG(jn["objs"][i], version));

            id = jn["id"] ?? LSText.randomString(16);
            name = jn["n"];
            type = jn["type"].AsInt;
            offset = -jn["o"].AsFloat;
            prefabObjects = new List<PrefabObject>();
            description = jn["description"];
            typeID = PrefabType.prefabTypeVGIndexToID.TryGetValue(type, out string prefabTypeID) ? prefabTypeID : "";
        }

        public override void ReadJSON(JSONNode jn)
        {
            beatmapObjects.Clear();
            for (int j = 0; j < jn["objects"].Count; j++)
                beatmapObjects.Add(BeatmapObject.Parse(jn["objects"][j]));

            prefabObjects.Clear();
            for (int k = 0; k < jn["prefab_objects"].Count; k++)
                prefabObjects.Add(PrefabObject.Parse(jn["prefab_objects"][k]));

            id = jn["id"];
            name = jn["name"];
            type = jn["type"].AsInt;
            typeID = jn["type_id"];
            offset = jn["offset"].AsFloat;
            description = jn["desc"] ?? string.Empty;

            if (string.IsNullOrEmpty(typeID))
                typeID = PrefabType.prefabTypeLSIndexToID.TryGetValue(type, out string prefabTypeID) ? prefabTypeID : "";

            assets.Clear();
            if (jn["assets"] != null)
                assets.ReadJSON(jn["assets"]);
        }

        public override JSONNode ToJSONVG()
        {
            var jn = Parser.NewJSONObject();
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

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();
            jn["name"] = name;
            jn["type"] = (PrefabType.prefabTypeLSIDToIndex.TryGetValue(typeID, out int prefabType) ? prefabType : 0).ToString();
            jn["offset"] = offset;

            if (id != null)
                jn["id"] = id;

            if (typeID != null)
                jn["type_id"] = typeID;

            if (!string.IsNullOrEmpty(description))
                jn["desc"] = description;

            for (int i = 0; i < beatmapObjects.Count; i++)
                jn["objects"][i] = beatmapObjects[i].ToJSON();

            if (prefabObjects != null && !prefabObjects.IsEmpty())
                for (int i = 0; i < prefabObjects.Count; i++)
                    jn["prefab_objects"][i] = prefabObjects[i].ToJSON();

            if (GameData.Current)
                foreach (var obj in beatmapObjects)
                    if (GameData.Current.assets.sprites.TryFind(x => x.name == obj.text, out SpriteAsset sprite) && !assets.sprites.Has(x => x.name == obj.text))
                        assets.sprites.Add(sprite);

            if (assets && !assets.IsEmpty())
                jn["assets"] = assets.ToJSON();

            return jn;
        }

        /// <summary>
        /// Gets the type group of the prefab.
        /// </summary>
        /// <returns>Returns the prefab type.</returns>
        public PrefabType GetPrefabType() => RTPrefabEditor.inst && RTPrefabEditor.inst.prefabTypes.TryFind(x => x.id == typeID, out PrefabType prefabType) ? prefabType : PrefabType.InvalidType;

        #endregion

        #region Operators

        public override bool Equals(object obj) => obj is Prefab paObj && id == paObj.id;

        public override int GetHashCode() => base.GetHashCode();

        public override string ToString() => $"{id} - {name}";

        #endregion
    }
}
