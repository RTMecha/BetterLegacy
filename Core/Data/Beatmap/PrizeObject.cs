using System.Collections.Generic;

using SimpleJSON;

using BetterLegacy.Core.Data.Player;

namespace BetterLegacy.Core.Data.Beatmap
{
    public class PrizeObject : PAObject<PrizeObject>
    {
        public PrizeObject() => id = GetNumberID();

        public override void CopyData(PrizeObject orig, bool newID = true)
        {
            obj = orig.obj; // atm can only copy the instance reference
        }

        public override void ReadJSON(JSONNode jn)
        {
            unlockConditions = Parser.TryParse(jn["unlock_conditions"], UnlockConditions.LevelCompletion);
            unlockData = jn["unlock_data"];

            if (jn["objs"] != null)
            {
                var list = new List<PAObjectBase>();

                for (int i = 0; i < jn["objs"].Count; i++)
                {
                    var itemJN = jn["objs"][i];
                    var obj = ParseObj(itemJN);
                    if (obj)
                        list.Add(ParseObj(itemJN));
                }

                obj = list;
                return;
            }

            obj = ParseObj(jn);
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

                // todo: rework modifier reference system so this can be done
                //nameof(ModifierBase) => ModifierBase.Parse(itemJN["obj"]),
                // todo: rework playermodel to be PAObject
                //nameof(PlayerModel) => PlayerModel.Parse(itemJN["obj"]),

                nameof(PlayerItem) => PlayerItem.Parse(obj),
                _ => null,
            };
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            if (unlockConditions != UnlockConditions.LevelCompletion)
                jn["unlock_conditions"] = (int)unlockConditions;
            if (!string.IsNullOrEmpty(unlockData))
                jn["unlock_data"] = unlockData;

            if (obj is List<PAObjectBase> list)
            {
                int num = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    var objJN = ObjToJSON(item);
                    if (objJN == null)
                        continue;

                    jn["objs"][num] = objJN;
                    num++;
                }
            }
            else if (obj is PAObjectBase paObj)
            {
                var objJN = ObjToJSON(paObj);
                if (objJN != null)
                    jn = objJN;
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

        /// <summary>
        /// Condition type to unlock the Prize Object by.
        /// </summary>
        public UnlockConditions unlockConditions;

        /// <summary>
        /// Data reference for <see cref="unlockConditions"/>.
        /// </summary>
        public string unlockData;

        public enum UnlockConditions
        {
            /// <summary>
            /// Unlocks after level completion. No rank is required.
            /// </summary>
            LevelCompletion,
            /// <summary>
            /// Unlocks after level completion. Requires a specific rank.
            /// </summary>
            LevelCompletionRanked,
            /// <summary>
            /// Unlocks via the prizeObject modifier.
            /// </summary>
            Modifier,
        }

        /// <summary>
        /// The object to be rewarded.
        /// </summary>
        public object obj;
    }
}
