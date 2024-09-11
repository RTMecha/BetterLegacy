using BetterLegacy.Core;
using BetterLegacy.Core.Managers;
using LSFunctions;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BaseBeatmapObject = DataManager.GameData.BeatmapObject;
using BasePrefab = DataManager.GameData.Prefab;
using BasePrefabObject = DataManager.GameData.PrefabObject;

namespace BetterLegacy.Core.Data
{
    public class Prefab : BasePrefab
    {
        public Prefab()
        {

        }

        public Prefab(string name, int type, float offset, List<BeatmapObject> beatmapObjects, List<PrefabObject> prefabObjects)
        {
            Name = name;
            Type = type;
            typeID = PrefabType.prefabTypeLSIndexToID.ContainsKey(type) ? PrefabType.prefabTypeLSIndexToID[type] : null;
            Offset = offset;

            objects.AddRange(beatmapObjects.Select(x => BeatmapObject.DeepCopy(x, false)));
            this.prefabObjects.AddRange(prefabObjects.Select(x => PrefabObject.DeepCopy(x, false)));

            float num = prefabObjects.Select(x => x.StartTime).Union(objects.Select(x => x.StartTime)).Min(x => x);
            for (int i = 0; i < objects.Count; i++)
                objects[i].StartTime -= num;
            for (int i = 0; i < prefabObjects.Count; i++)
                this.prefabObjects[i].StartTime -= num;
        }

        public string filePath;

        public string description;

        /// <summary>
        /// Only used for vanilla compatibility.
        /// </summary>
        public new int Type;

        public Dictionary<string, Sprite> SpriteAssets { get; set; } = new Dictionary<string, Sprite>();
        public PrefabType PrefabType => DataManager.inst.PrefabTypes.TryFind(x => x is PrefabType prefabType && prefabType.id == typeID, out DataManager.PrefabType prefabType) ? (PrefabType)prefabType : PrefabType.InvalidType;
        public string typeID;

        #region Methods

        public static Prefab DeepCopy(Prefab og, bool newID = true)
        {
            var prefab = new Prefab()
            {
                description = og.description,
                ID = newID ? LSText.randomString(16) : og.ID,
                MainObjectID = og.MainObjectID,
                Name = og.Name,
                Offset = og.Offset,
                prefabObjects = og.prefabObjects.Clone(),
                Type = og.Type,
                typeID = og.typeID,
            };

            prefab.objects = new List<BaseBeatmapObject>();
            prefab.objects.AddRange(og.objects.Select(x => BeatmapObject.DeepCopy((BeatmapObject)x, false)).ToList());

            foreach (var beatmapObject in prefab.objects)
            {
                if (!prefab.SpriteAssets.ContainsKey(beatmapObject.text) && og.SpriteAssets.ContainsKey(beatmapObject.text))
                {
                    prefab.SpriteAssets.Add(beatmapObject.text, og.SpriteAssets[beatmapObject.text]);
                }
            }

            return prefab;
        }

        public static Prefab ParseVG(JSONNode jn)
        {
            var beatmapObjects = new List<BaseBeatmapObject>();
            for (int i = 0; i < jn["objs"].Count; i++)
                beatmapObjects.Add(BeatmapObject.ParseVG(jn["objs"][i]));

            var prefab = new Prefab
            {
                ID = jn["id"] == null ? LSText.randomString(16) : jn["id"],
                MainObjectID = LSText.randomString(16),
                Name = jn["n"],
                Type = jn["type"].AsInt,
                Offset = -jn["o"].AsFloat,
                objects = beatmapObjects,
                prefabObjects = new List<BasePrefabObject>(),
                description = jn["description"],
            };
            prefab.typeID = PrefabType.prefabTypeVGIndexToID.ContainsKey(prefab.Type) ? PrefabType.prefabTypeVGIndexToID[prefab.Type] : "";

            return prefab;
        }

        public static Prefab Parse(JSONNode jn)
        {
            var beatmapObjects = new List<BaseBeatmapObject>();
            for (int j = 0; j < jn["objects"].Count; j++)
                beatmapObjects.Add(BeatmapObject.Parse(jn["objects"][j]));

            var prefabObjects = new List<BasePrefabObject>();
            for (int k = 0; k < jn["prefab_objects"].Count; k++)
                prefabObjects.Add(PrefabObject.Parse(jn["prefab_objects"][k]));

            var prefab = new Prefab
            {
                ID = jn["id"],
                MainObjectID = jn["main_obj_id"] == null ? LSText.randomString(16) : jn["main_obj_id"],
                Name = jn["name"],
                Type = jn["type"].AsInt,
                Offset = jn["offset"].AsFloat,
                objects = beatmapObjects,
                prefabObjects = prefabObjects,
                description = jn["desc"] == null ? "" : jn["desc"],
                typeID = jn["type_id"],
            };

            if (string.IsNullOrEmpty(prefab.typeID))
                prefab.typeID = PrefabType.prefabTypeLSIndexToID.ContainsKey(prefab.Type) ? PrefabType.prefabTypeLSIndexToID[prefab.Type] : "";

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
            jn["n"] = Name;
            if (ID != null)
                jn["id"] = ID;
            jn["type"] = PrefabType.prefabTypeVGIDToIndex.ContainsKey(typeID) ? PrefabType.prefabTypeVGIDToIndex[typeID] : 0;

            jn["o"] = -Offset;

            jn["description"] = description;

            for (int i = 0; i < objects.Count; i++)
                if (objects[i] != null)
                    jn["objs"][i] = ((BeatmapObject)objects[i]).ToJSONVG();

            return jn;
        }

        public JSONNode ToJSON()
        {
            var jn = JSON.Parse("{}");
            jn["name"] = Name;
            jn["type"] = (PrefabType.prefabTypeLSIDToIndex.ContainsKey(typeID) ? PrefabType.prefabTypeLSIDToIndex[typeID] : 0).ToString();
            jn["offset"] = Offset.ToString();

            if (ID != null)
                jn["id"] = ID.ToString();

            if (typeID != null)
                jn["type_id"] = typeID;

            if (MainObjectID != null)
                jn["main_obj_id"] = MainObjectID.ToString();

            jn["desc"] = description == null ? "" : description;

            for (int i = 0; i < objects.Count; i++)
                jn["objects"][i] = ((BeatmapObject)objects[i]).ToJSON();

            if (prefabObjects != null && prefabObjects.Count > 0)
                for (int i = 0; i < prefabObjects.Count; i++)
                    jn["prefab_objects"][i] = ((PrefabObject)prefabObjects[i]).ToJSON();

            var spriteAssets = new Dictionary<string, Sprite>();

            foreach (var obj in objects)
            {
                if (AssetManager.SpriteAssets.ContainsKey(obj.text) && !spriteAssets.ContainsKey(obj.text))
                {
                    spriteAssets.Add(obj.text, AssetManager.SpriteAssets[obj.text]);
                }
            }

            for (int i = 0; i < spriteAssets.Count; i++)
            {
                jn["assets"]["spr"][i]["n"] = spriteAssets.ElementAt(i).Key;
                jn["assets"]["spr"][i]["i"] = SpriteHelper.SpriteToString(spriteAssets.ElementAt(i).Value);
            }

            return jn;
        }

        #endregion

        #region Operators

        public static implicit operator bool(Prefab exists) => exists != null;

        public override bool Equals(object obj) => obj is Prefab && ID == (obj as Prefab).ID;

        public override string ToString() => ID;

        #endregion
    }
}
