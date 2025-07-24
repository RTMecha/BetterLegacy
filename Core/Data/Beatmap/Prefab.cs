using System.Collections.Generic;
using System.Linq;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core.Runtime.Objects;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Contains a package of <see cref="IPrefabable"/> objects.
    /// </summary>
    public class Prefab : PAObject<Prefab>, IBeatmap, IPrefabable
    {
        public Prefab() : base() { }

        public Prefab(string name, int type, float offset, List<BeatmapObject> beatmapObjects, List<PrefabObject> prefabObjects, List<BackgroundLayer> backgroundLayers = null, List<BackgroundObject> backgroundObjects = null, List<Prefab> prefabs = null) : this()
        {
            this.name = name;
            this.type = type;
            typeID = PrefabType.LSIndexToID.TryGetValue(type, out string prefabTypeID) ? prefabTypeID : string.Empty;
            this.offset = offset;

            CopyObjects(beatmapObjects, prefabObjects, backgroundLayers, backgroundObjects, prefabs);
        }

        public Prefab(string name, int type, float offset, IBeatmap beatmap) : this()
        {
            this.name = name;
            this.type = type;
            typeID = PrefabType.LSIndexToID.TryGetValue(type, out string prefabTypeID) ? prefabTypeID : string.Empty;
            this.offset = offset;

            CopyObjects(beatmap);
        }

        #region Values

        /// <summary>
        /// Name of the Prefab.
        /// </summary>
        public string name;

        /// <summary>
        /// Offset added to spawn / despawn times of all spawned objects.
        /// </summary>
        public float offset;

        /// <summary>
        /// File path to the external Prefab.
        /// </summary>
        public string filePath;

        /// <summary>
        /// Description of the Prefab.
        /// </summary>
        public string description;

        /// <summary>
        /// Vanilla Prefab Type index.
        /// </summary>
        public int type;

        /// <summary>
        /// ID of the Prefab Type.
        /// </summary>
        public string typeID;

        #region Prefab

        public float StartTime { get; set; }

        /// <summary>
        /// Used for objects spawned from a Prefab Object.
        /// </summary>
        public string originalID;

        /// <summary>
        /// If the object is spawned from a prefab and has no parent.
        /// </summary>
        public bool fromPrefabBase;

        /// <summary>
        /// If the object is spawned from a prefab.
        /// </summary>
        public bool fromPrefab;

        /// <summary>
        /// Prefab Object reference ID.
        /// </summary>
        public string prefabInstanceID = string.Empty;

        public string OriginalID { get => originalID; set => originalID = value; }

        public string PrefabID { get; set; }

        public string PrefabInstanceID { get => prefabInstanceID; set => prefabInstanceID = value; }

        public bool FromPrefab { get => fromPrefab; set => fromPrefab = value; }

        public Prefab CachedPrefab { get; set; }

        public PrefabObject defaultInstanceData;

        #endregion

        #region Contents

        /// <summary>
        /// Assets of the Prefab.
        /// </summary>
        public Assets assets = new Assets();

        public List<BeatmapObject> BeatmapObjects { get => beatmapObjects; set => beatmapObjects = value; }
        /// <summary>
        /// Contained Beatmap Objects.
        /// </summary>
        public List<BeatmapObject> beatmapObjects = new List<BeatmapObject>();

        public List<PrefabObject> PrefabObjects { get => prefabObjects; set => prefabObjects = value; }
        /// <summary>
        /// Contained Prefab Objects.
        /// </summary>
        public List<PrefabObject> prefabObjects = new List<PrefabObject>();

        public List<Prefab> Prefabs { get => prefabs; set => prefabs = value; }
        /// <summary>
        /// Contained Prefabs (recursive).
        /// </summary>
        public List<Prefab> prefabs = new List<Prefab>();

        public List<BackgroundLayer> BackgroundLayers { get => backgroundLayers; set => backgroundLayers = value; }
        /// <summary>
        /// Contained Background Layers.
        /// </summary>
        public List<BackgroundLayer> backgroundLayers = new List<BackgroundLayer>();

        public List<BackgroundObject> BackgroundObjects { get => backgroundObjects; set => backgroundObjects = value; }
        /// <summary>
        /// Contained Background Objects.
        /// </summary>
        public List<BackgroundObject> backgroundObjects = new List<BackgroundObject>();

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

            prefabs = new List<Prefab>();
            if (!orig.prefabs.IsEmpty())
                prefabs.AddRange(orig.prefabs.Select(x => x.Copy(false)));

            beatmapObjects = new List<BeatmapObject>();
            if (!orig.beatmapObjects.IsEmpty())
                beatmapObjects.AddRange(orig.beatmapObjects.Select(x => x.Copy(false)));

            prefabObjects = new List<PrefabObject>();
            if (!orig.prefabObjects.IsEmpty())
                prefabObjects.AddRange(orig.prefabObjects.Select(x => x.Copy(false)));

            backgroundLayers = new List<BackgroundLayer>();
            if (!orig.backgroundLayers.IsEmpty())
                backgroundLayers.AddRange(orig.backgroundLayers.Select(x => x.Copy(false)));

            backgroundObjects = new List<BackgroundObject>();
            if (!orig.backgroundObjects.IsEmpty())
                backgroundObjects.AddRange(orig.backgroundObjects.Select(x => x.Copy(false)));

            if (orig.defaultInstanceData)
                defaultInstanceData = orig.defaultInstanceData.Copy();

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
            typeID = PrefabType.VGIndexToID.TryGetValue(type, out string prefabTypeID) ? prefabTypeID : string.Empty;
        }

        public override void ReadJSON(JSONNode jn)
        {
            prefabs.Clear();
            if (jn["prefabs"] != null)
                for (int i = 0; i < jn["prefabs"].Count; i++)
                    prefabs.Add(Parse(jn["prefabs"][i]));

            beatmapObjects.Clear();
            for (int i = 0; i < jn["objects"].Count; i++)
                beatmapObjects.Add(BeatmapObject.Parse(jn["objects"][i]));

            prefabObjects.Clear();
            for (int i = 0; i < jn["prefab_objects"].Count; i++)
                prefabObjects.Add(PrefabObject.Parse(jn["prefab_objects"][i]));

            backgroundLayers.Clear();
            if (jn["bg_layers"] != null)
                for (int i = 0; i < jn["bg_layers"].Count; i++)
                    backgroundLayers.Add(BackgroundLayer.Parse(jn["bg_layers"][i]));

            backgroundObjects.Clear();
            if (jn["bg_objects"] != null)
                for (int i = 0; i < jn["bg_objects"].Count; i++)
                    backgroundObjects.Add(BackgroundObject.Parse(jn["bg_objects"][i]));

            id = jn["id"];
            name = jn["name"];
            type = jn["type"].AsInt;
            typeID = jn["type_id"];
            offset = jn["offset"].AsFloat;
            description = jn["desc"] ?? string.Empty;

            this.ReadPrefabJSON(jn);

            if (string.IsNullOrEmpty(typeID))
                typeID = PrefabType.LSIndexToID.TryGetValue(type, out string prefabTypeID) ? prefabTypeID : string.Empty;

            if (jn["default"] != null)
                defaultInstanceData = PrefabObject.Parse(jn["default"]);

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
            jn["type"] = PrefabType.VGIDToIndex.TryGetValue(typeID, out int prefabType) ? prefabType : 0;

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
            jn["type"] = (PrefabType.LSIDToIndex.TryGetValue(typeID, out int prefabType) ? prefabType : 0).ToString();
            jn["offset"] = offset;

            if (id != null)
                jn["id"] = id;

            this.WritePrefabJSON(jn);

            if (typeID != null)
                jn["type_id"] = typeID;

            if (!string.IsNullOrEmpty(description))
                jn["desc"] = description;

            for (int i = 0; i < prefabs.Count; i++)
                jn["prefabs"][i] = prefabs[i].ToJSON();

            for (int i = 0; i < beatmapObjects.Count; i++)
                jn["objects"][i] = beatmapObjects[i].ToJSON();

            if (prefabObjects != null && !prefabObjects.IsEmpty())
                for (int i = 0; i < prefabObjects.Count; i++)
                    jn["prefab_objects"][i] = prefabObjects[i].ToJSON();

            if (backgroundLayers != null && !backgroundLayers.IsEmpty())
                for (int i = 0; i < backgroundLayers.Count; i++)
                    jn["bg_layers"][i] = backgroundLayers[i].ToJSON();

            if (backgroundObjects != null && !backgroundObjects.IsEmpty())
                for (int i = 0; i < backgroundObjects.Count; i++)
                    jn["bg_objects"][i] = backgroundObjects[i].ToJSON();

            if (GameData.Current)
                foreach (var obj in beatmapObjects)
                    if (GameData.Current.assets.sprites.TryFind(x => x.name == obj.text, out SpriteAsset sprite) && !assets.sprites.Has(x => x.name == obj.text))
                        assets.sprites.Add(sprite);

            if (defaultInstanceData)
                jn["default"] = defaultInstanceData.ToJSON();

            if (assets && !assets.IsEmpty())
                jn["assets"] = assets.ToJSON();

            return jn;
        }

        /// <summary>
        /// Gets the type group of the prefab.
        /// </summary>
        /// <returns>Returns the prefab type.</returns>
        public PrefabType GetPrefabType() => RTPrefabEditor.inst && RTPrefabEditor.inst.prefabTypes.TryFind(x => x.id == typeID, out PrefabType prefabType) ? prefabType : PrefabType.InvalidType;

        /// <summary>
        /// Copies an <see cref="IBeatmap"/>'s objects to this prefab.
        /// </summary>
        /// <param name="beatmap">Package reference.</param>
        public void CopyObjects(IBeatmap beatmap) => CopyObjects(beatmap.BeatmapObjects, beatmap.PrefabObjects, beatmap.BackgroundLayers, beatmap.BackgroundObjects, beatmap.Prefabs);

        /// <summary>
        /// Copies objects to this prefab.
        /// </summary>
        /// <param name="beatmapObjects">List of Beatmap Objects.</param>
        /// <param name="prefabObjects">List of Prefab Objects.</param>
        /// <param name="backgroundLayers">List of Background Layers.</param>
        /// <param name="backgroundObjects">List of Background Objects.</param>
        /// <param name="prefabs">List of Prefabs.</param>
        public void CopyObjects(List<BeatmapObject> beatmapObjects, List<PrefabObject> prefabObjects, List<BackgroundLayer> backgroundLayers = null, List<BackgroundObject> backgroundObjects = null, List<Prefab> prefabs = null)
        {
            this.beatmapObjects.Clear();
            this.beatmapObjects.AddRange(beatmapObjects.Select(x => x.Copy(false)));
            this.prefabObjects.Clear();
            this.prefabObjects.AddRange(prefabObjects.Select(x => x.Copy(false)));

            this.backgroundLayers.Clear();
            if (backgroundLayers != null)
                this.backgroundLayers.AddRange(backgroundLayers.Select(x => x.Copy(false)));

            this.backgroundObjects.Clear();
            if (backgroundObjects != null)
                this.backgroundObjects.AddRange(backgroundObjects.Select(x => x.Copy(false)));

            this.prefabs.Clear();
            if (prefabs != null)
                this.prefabs.AddRange(prefabs.Select(x => x.Copy(false)));

            var collection = prefabObjects.Select(x => x.StartTime).Union(beatmapObjects.Select(x => x.StartTime));
            if (backgroundObjects != null)
                collection = collection.Union(backgroundObjects.Select(x => x.StartTime));

            float num = collection.Min(x => x);
            for (int i = 0; i < this.beatmapObjects.Count; i++)
                this.beatmapObjects[i].StartTime -= num;
            for (int i = 0; i < prefabObjects.Count; i++)
                this.prefabObjects[i].StartTime -= num;
            for (int i = 0; i < this.backgroundObjects.Count; i++)
                this.backgroundObjects[i].StartTime -= num;
        }

        public Assets GetAssets() => assets;

        public IRTObject GetRuntimeObject() => null;

        public float GetObjectLifeLength(float offset = 0f, bool noAutokill = false, bool collapse = false) => 0f;

        public override string ToString() => $"{id} - {name}";

        #endregion
    }
}
