using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core.Data.Modifiers;
using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime.Objects;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Contains a package of <see cref="IPrefabable"/> objects.
    /// </summary>
    public class Prefab : PAObject<Prefab>, IPacket, IBeatmap, IPrefabable, IUploadable, IFile
    {
        #region Constructors

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

        #endregion

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
        /// Creator of the prefab.
        /// </summary>
        public string creator;

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

        /// <summary>
        /// Data of the icon.
        /// </summary>
        public string iconData;
        /// <summary>
        /// Icon of the Prefab.
        /// </summary>
        public Sprite icon;

        public FileFormat FileFormat => RTFile.GetFileFormat(filePath);

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
        public PrefabObject CachedPrefabObject { get; set; }

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

        public List<BeatmapTheme> BeatmapThemes { get => beatmapThemes; set => beatmapThemes = value; }
        /// <summary>
        /// Contained themes.
        /// </summary>
        public List<BeatmapTheme> beatmapThemes = new List<BeatmapTheme>();

        public List<ModifierBlock> modifierBlocks = new List<ModifierBlock>();

        #endregion

        #region Server

        public string ServerID { get; set; }

        public string UploaderName { get; set; }

        public string UploaderID { get; set; }

        public List<ServerUser> Uploaders { get; set; } = new List<ServerUser>();

        public ServerVisibility Visibility { get; set; }

        public string Changelog { get; set; }

        public List<string> ArcadeTags { get; set; } = new List<string>();

        public string ObjectVersion { get; set; }

        public string dateCreated = string.Empty;
        public string dateEdited = DateTime.Now.ToString(LegacyPlugin.DATE_TIME_FORMAT);
        public string datePublished = string.Empty;
        public int versionNumber;

        public string DatePublished { get => datePublished; set => datePublished = value; }

        public int VersionNumber { get => versionNumber; set => versionNumber = value; }

        #endregion

        #region Editor

        public PrefabPanel prefabPanel;

        #endregion

        #endregion

        #region Functions

        public override void CopyData(Prefab orig, bool newID = true)
        {
            id = newID ? LSText.randomString(16) : orig.id;
            name = orig.name;
            description = orig.description;
            creator = orig.creator;

            dateCreated = orig.dateCreated;
            dateEdited = orig.dateEdited;
            datePublished = orig.datePublished;
            versionNumber = orig.versionNumber;

            offset = orig.offset;
            prefabObjects = orig.prefabObjects.Clone();
            type = orig.type;
            typeID = orig.typeID;
            iconData = orig.iconData;
            if (string.IsNullOrEmpty(iconData) && orig.icon)
                iconData = SpriteHelper.SpriteToString(orig.icon);

            beatmapThemes = new List<BeatmapTheme>();
            if (!orig.beatmapThemes.IsEmpty())
                beatmapThemes.AddRange(orig.beatmapThemes.Select(x => x.Copy(false)));
            
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

            modifierBlocks = new List<ModifierBlock>();
            if (!orig.modifierBlocks.IsEmpty())
                modifierBlocks.AddRange(orig.modifierBlocks.Select(x => x.Copy(false)));

            if (orig.defaultInstanceData)
                defaultInstanceData = orig.defaultInstanceData.Copy();

            assets = orig.assets?.Copy();

            this.CopyUploadableData(orig);
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
            iconData = jn["preview"];
            dateCreated = DateTime.Now.ToString(LegacyPlugin.DATE_TIME_FORMAT);
        }

        public override void ReadJSON(JSONNode jn)
        {
            id = jn["id"];
            name = jn["name"];
            type = jn["type"].AsInt;
            typeID = jn["type_id"];
            offset = jn["offset"].AsFloat;
            creator = jn["creator"];
            description = jn["desc"] ?? string.Empty;
            if (string.IsNullOrEmpty(typeID))
                typeID = PrefabType.LSIndexToID.TryGetValue(type, out string prefabTypeID) ? prefabTypeID : string.Empty;

            iconData = jn["icon"];

            if (!string.IsNullOrEmpty(jn["date_edited"]))
                dateEdited = jn["date_edited"];
            if (!string.IsNullOrEmpty(jn["date_created"]))
                dateCreated = jn["date_created"];
            else
                dateCreated = DateTime.Now.ToString(LegacyPlugin.DATE_TIME_FORMAT);
            if (!string.IsNullOrEmpty(jn["date_published"]))
                datePublished = jn["date_published"];
            if (jn["version_number"] != null)
                versionNumber = jn["version_number"].AsInt;

            this.ReadUploadableJSON(jn);
            this.ReadPrefabJSON(jn);

            #region Read Contents

            beatmapThemes.Clear();
            for (int i = 0; i < jn["themes"].Count; i++)
            {
                if (string.IsNullOrEmpty(jn["themes"][i]["id"]))
                    continue;

                beatmapThemes.Add(BeatmapTheme.Parse(jn["themes"][i]));
            }

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

            modifierBlocks = Parser.ParseModifierBlocks(jn["modifier_blocks"], ModifierReferenceType.GameData);

            if (jn["default"] != null)
                defaultInstanceData = PrefabObject.Parse(jn["default"]);

            assets.Clear();
            if (jn["assets"] != null)
                assets.ReadJSON(jn["assets"]);

            #endregion
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

            if (icon)
                jn["preview"] = SpriteHelper.SpriteToString(icon);
            else if (!string.IsNullOrEmpty(iconData))
                jn["preview"] = iconData;

            return jn;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();
            if (id != null)
                jn["id"] = id;
            jn["name"] = name;
            jn["type"] = (PrefabType.LSIDToIndex.TryGetValue(typeID, out int prefabType) ? prefabType : 0).ToString();
            if (typeID != null)
                jn["type_id"] = typeID;
            jn["offset"] = offset;

            this.WritePrefabJSON(jn);

            if (!string.IsNullOrEmpty(creator))
                jn["creator"] = creator;
            if (!string.IsNullOrEmpty(description))
                jn["desc"] = description;

            jn["date_created"] = dateCreated;
            jn["date_edited"] = DateTime.Now.ToString(LegacyPlugin.DATE_TIME_FORMAT);
            if (!string.IsNullOrEmpty(datePublished))
                jn["date_published"] = datePublished;
            if (versionNumber != 0)
                jn["version_number"] = versionNumber;

            this.WriteUploadableJSON(jn);

            #region Write Contents

            if (!modifierBlocks.IsEmpty())
                jn["modifier_blocks"] = Parser.ModifierBlocksToJSON(modifierBlocks);

            for (int i = 0; i < beatmapThemes.Count; i++)
                jn["themes"][i] = beatmapThemes[i].ToJSON();

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

            #endregion

            if (icon)
                jn["icon"] = SpriteHelper.SpriteToString(icon);
            else if (!string.IsNullOrEmpty(iconData))
                jn["icon"] = iconData;

            return jn;
        }

        public void ReadPacket(NetworkReader reader)
        {
            id = reader.ReadString();

            #region Interface

            this.ReadPrefabPacket(reader);
            this.ReadUploadablePacket(reader);

            #endregion

            #region Base

            name = reader.ReadString();
            typeID = reader.ReadString();
            offset = reader.ReadSingle();
            creator = reader.ReadString();
            description = reader.ReadString();
            dateCreated = reader.ReadString();
            dateEdited = reader.ReadString();
            icon = reader.ReadSprite();

            #endregion

            #region Contents

            Packet.ReadPacketList(modifierBlocks, reader);
            Packet.ReadPacketList(beatmapThemes, reader);
            Packet.ReadPacketList(prefabs, reader);
            Packet.ReadPacketList(beatmapObjects, reader);
            Packet.ReadPacketList(prefabObjects, reader);
            Packet.ReadPacketList(backgroundLayers, reader);
            Packet.ReadPacketList(backgroundObjects, reader);
            var hasDefaultInstanceData = reader.ReadBoolean();
            if (hasDefaultInstanceData)
                defaultInstanceData = Packet.CreateFromPacket<PrefabObject>(reader);
            assets = Packet.CreateFromPacket<Assets>(reader);

            #endregion
        }

        public void WritePacket(NetworkWriter writer)
        {
            writer.Write(id);

            #region Interface

            this.WritePrefabPacket(writer);
            this.WriteUploadablePacket(writer);

            #endregion

            #region Base

            writer.Write(name);
            writer.Write(typeID);
            writer.Write(offset);
            writer.Write(creator);
            writer.Write(description);
            writer.Write(dateCreated);
            writer.Write(dateEdited);
            writer.Write(icon);

            #endregion

            #region Contents

            Packet.WritePacketList(modifierBlocks, writer);
            Packet.WritePacketList(beatmapThemes, writer);
            Packet.WritePacketList(prefabs, writer);
            Packet.WritePacketList(beatmapObjects, writer);
            Packet.WritePacketList(prefabObjects, writer);
            Packet.WritePacketList(backgroundLayers, writer);
            Packet.WritePacketList(backgroundObjects, writer);
            bool hasDefaultInstanceData = defaultInstanceData;
            writer.Write(hasDefaultInstanceData);
            if (hasDefaultInstanceData)
                defaultInstanceData.WritePacket(writer);
            assets.WritePacket(writer);

            #endregion
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
            if (beatmapObjects != null)
                this.beatmapObjects.AddRange(beatmapObjects.Select(x => x?.Copy(false)));

            this.prefabObjects.Clear();
            if (prefabObjects != null)
                this.prefabObjects.AddRange(prefabObjects.Select(x => x?.Copy(false)));

            this.backgroundLayers.Clear();
            if (backgroundLayers != null)
                this.backgroundLayers.AddRange(backgroundLayers.Select(x => x?.Copy(false)));

            this.backgroundObjects.Clear();
            if (backgroundObjects != null)
                this.backgroundObjects.AddRange(backgroundObjects.Select(x => x?.Copy(false)));

            this.prefabs.Clear();
            if (prefabs != null)
                this.prefabs.AddRange(prefabs.Select(x => x?.Copy(false)));

            float num = GetMinimumTime(beatmapObjects, prefabObjects, backgroundObjects, prefabs);
            if (num == 0f)
                return;

            for (int i = 0; i < this.beatmapObjects.Count; i++)
                this.beatmapObjects[i].StartTime -= num;
            for (int i = 0; i < this.prefabObjects.Count; i++)
                this.prefabObjects[i].StartTime -= num;
            for (int i = 0; i < this.backgroundObjects.Count; i++)
                this.backgroundObjects[i].StartTime -= num;
        }

        /// <summary>
        /// Gets the minimum start time of a package of objects.
        /// </summary>
        /// <returns>Returns the minimum start time.</returns>
        public static float GetMinimumTime(List<BeatmapObject> beatmapObjects, List<PrefabObject> prefabObjects, List<BackgroundObject> backgroundObjects = null, List<Prefab> prefabs = null)
        {
            IEnumerable<float> collection = null;
            if (beatmapObjects != null && !beatmapObjects.IsEmpty())
                collection = beatmapObjects.SelectWhere(x => x, x => x.StartTime);
            if (prefabObjects != null && !prefabObjects.IsEmpty())
                collection = collection != null ? collection.Union(prefabObjects.SelectWhere(x => x, x => x.StartTime)) : prefabObjects.SelectWhere(x => x, x => x.StartTime);
            if (backgroundObjects != null && !backgroundObjects.IsEmpty())
                collection = collection != null ? collection.Union(backgroundObjects.SelectWhere(x => x, x => x.StartTime)) : backgroundObjects.SelectWhere(x => x, x => x.StartTime);

            return collection?.Min(x => x) ?? 0f;
        }

        public Assets GetAssets() => assets;

        public IRTObject GetRuntimeObject() => null;

        public float GetObjectLifeLength(float offset = 0f, bool noAutokill = false, bool collapse = false) => 0f;

        public string GetFileName() => $"{RTFile.FormatLegacyFileName(name)}{FileFormat.Dot()}";

        public void ReadFromFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            if (!path.EndsWith(FileFormat.Dot()))
                path = path += FileFormat.Dot();

            var file = RTFile.ReadFromFile(path);
            if (string.IsNullOrEmpty(file))
                return;

            switch (RTFile.GetFileFormat(path))
            {
                case FileFormat.LSP: ReadJSON(JSON.Parse(file));
                    break;
                case FileFormat.VGP: ReadJSONVG(JSON.Parse(file));
                    break;
            }
        }

        public void WriteToFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            var jn = RTFile.GetFileFormat(path) switch
            {
                FileFormat.LSP => ToJSON(),
                FileFormat.VGP => ToJSONVG(),
                _ => null,
            };
            if (jn != null)
                RTFile.WriteToFile(path, jn.ToString());
        }

        /// <summary>
        /// Loads and gets the Prefabs' icon.
        /// </summary>
        public Sprite GetIcon()
        {
            if (icon)
                return icon;

            if (string.IsNullOrEmpty(iconData))
                return null;

            icon = SpriteHelper.StringToSprite(iconData);
            return icon;
        }

        public override string ToString() => $"{id} - {name}";

        #endregion
    }
}
