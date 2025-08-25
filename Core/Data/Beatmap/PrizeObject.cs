using System.Collections.Generic;
using System.Linq;

using SimpleJSON;

using BetterLegacy.Core.Data.Modifiers;
using BetterLegacy.Core.Data.Player;

namespace BetterLegacy.Core.Data.Beatmap
{
    /*
     Notes:
    - Prize objects will be in a separate "prizes" folder. You can import objects from this folder directly into the level.
    - Prize objects that are awarded to the user are stored in a "prizes.lspo" file.
     */

    /// <summary>
    /// Represents a set of objects that is awarded to the user.
    /// </summary>
    public class PrizeObject : PAObject<PrizeObject>, IFile
    {
        public PrizeObject() => id = GetNumberID();

        #region Values

        /// <summary>
        /// Name of the prize.
        /// </summary>
        public string name = string.Empty;

        /// <summary>
        /// The objects to be rewarded.
        /// </summary>
        public List<PAObjectBase> objects;

        public FileFormat FileFormat => FileFormat.LSPO;

        #endregion

        #region Methods

        public override void CopyData(PrizeObject orig, bool newID = true)
        {
            name = orig.name;
            objects = new List<PAObjectBase>(orig.objects.Select(x => CopyObj(x)));
        }

        PAObjectBase CopyObj(PAObjectBase obj)
        {
            if (obj is BeatmapObject beatmapObject)
                return beatmapObject.Copy();
            if (obj is BackgroundLayer backgroundLayer)
                return backgroundLayer.Copy();
            if (obj is BackgroundObject backgroundObject)
                return backgroundObject.Copy();
            if (obj is PrefabObject prefabObject)
                return prefabObject.Copy();
            if (obj is Prefab prefab)
                return prefab.Copy();
            if (obj is BeatmapTheme beatmapTheme)
                return beatmapTheme.Copy();
            if (obj is Modifier modifier)
                return modifier.Copy();
            if (obj is ModifierBlock modifierBlock)
                return modifierBlock.Copy();
            if (obj is PlayerModel playerModel)
                return playerModel.Copy();
            if (obj is PlayerItem playerItem)
                return playerItem.Copy();
            if (obj is PAAnimation animation)
                return animation.Copy();
            return null;
        }

        public override void ReadJSON(JSONNode jn)
        {
            name = jn["name"] ?? string.Empty;

            if (jn["objs"] == null)
                return;

            objects = new List<PAObjectBase>();

            for (int i = 0; i < jn["objs"].Count; i++)
            {
                var itemJN = jn["objs"][i];
                var obj = ParseObj(itemJN);
                if (obj)
                    objects.Add(ParseObj(itemJN));
            }
        }

        PAObjectBase ParseObj(JSONNode itemJN)
        {
            string type = itemJN["type"];
            var obj = itemJN["obj"];
            return type switch
            {
                nameof(BeatmapObject) => BeatmapObject.Parse(obj),
                nameof(BackgroundLayer) => BackgroundLayer.Parse(obj),
                nameof(BackgroundObject) => BackgroundObject.Parse(obj),
                nameof(PrefabObject) => PrefabObject.Parse(obj),
                nameof(Prefab) => Prefab.Parse(obj),
                nameof(BeatmapTheme) => BeatmapTheme.Parse(obj),
                nameof(Modifier) => Modifier.Parse(obj),
                nameof(ModifierBlock) => ModifierBlock.Parse(obj),
                nameof(PlayerModel) => PlayerModel.Parse(obj),
                nameof(PlayerItem) => PlayerItem.Parse(obj),
                nameof(PAAnimation) => PAAnimation.Parse(obj),
                _ => null,
            };
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            if (!string.IsNullOrEmpty(name))
                jn["name"] = name;

            int num = 0;
            for (int i = 0; i < objects.Count; i++)
            {
                var item = objects[i];
                var objJN = ObjToJSON(item);
                if (objJN == null)
                    continue;

                jn["objs"][num] = objJN;
                num++;
            }

            return jn;
        }

        JSONNode ObjToJSON(PAObjectBase obj)
        {
            var type = GetObjectType(obj);
            if (type == PAObjectType.Null)
                return null;

            var jn = Parser.NewJSONObject();
            jn["type"] = type.ToString();
            jn["obj"] = obj.ToJSON();
            return jn;
        }

        static void Test()
        {
            var prize = new PrizeObject();

            prize.AddObject(new BeatmapObject());

            var beatmapObject = prize.GetObject<BeatmapObject>(0);
        }

        /// <summary>
        /// Gets an object from the package.
        /// </summary>
        /// <typeparam name="T">Type of the object.</typeparam>
        /// <param name="index">Index of the object to get.</param>
        /// <returns>Returns the object casted into the type.</returns>
        public T GetObject<T>(int index) where T : PAObject<T>, new()
        {
            var obj = objects.GetAtOrDefault(index, null);
            return obj is T paObj ? paObj : null;
        }

        /// <summary>
        /// Adds an object to the package.
        /// </summary>
        /// <param name="obj">Object to add.</param>
        public void AddObject(PAObjectBase obj)
        {
            if (!obj)
                return;

            objects.OverwriteAdd((other, index) => other && obj.id == other.id, obj);
        }

        public string GetFileName() => RTFile.FormatLegacyFileName(name) + FileFormat.Dot();

        public void ReadFromFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            var fileName = GetFileName();
            if (!path.EndsWith(fileName))
                path = RTFile.CombinePaths(path, fileName);

            var file = RTFile.ReadFromFile(path);
            if (string.IsNullOrEmpty(file))
                return;

            ReadJSON(JSON.Parse(file));
        }

        public void WriteToFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            var jn = ToJSON();
            RTFile.WriteToFile(path, jn.ToString());
        }

        #endregion
    }
}
