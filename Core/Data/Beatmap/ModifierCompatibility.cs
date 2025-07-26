using SimpleJSON;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Data.Beatmap
{
    /// <summary>
    /// Allows multiple modifiers to be compatible across multiple types of objects.
    /// </summary>
    public struct ModifierCompatibility
    {
        public ModifierCompatibility(
            bool beatmapObject,
            bool backgroundObject,
            bool prefabObject,
            bool paPlayer,
            bool playerModel,
            bool gameData)
        {
            BeatmapObject = beatmapObject;
            BackgroundObject = backgroundObject;
            PrefabObject = prefabObject;
            PAPlayer = paPlayer;
            PlayerModel = playerModel;
            GameData = gameData;
        }

        #region Defaults

        /// <summary>
        /// The modifier is compatible with all object types. This most likely means it doesn't need a reference object.
        /// </summary>
        public static ModifierCompatibility AllCompatible => new ModifierCompatibility()
        {
            All = true,
        };
        /// <summary>
        /// The modifier is only compatible with <see cref="Beatmap.BeatmapObject"/>.
        /// </summary>
        public static ModifierCompatibility BeatmapObjectCompatible => new ModifierCompatibility()
        {
            BeatmapObject = true,
        };
        /// <summary>
        /// The modifier is only compatible with <see cref="Beatmap.BackgroundObject"/>.
        /// </summary>
        public static ModifierCompatibility BackgroundObjectCompatible => new ModifierCompatibility()
        {
            BackgroundObject = true,
        };
        /// <summary>
        /// The modifier is only compatible with <see cref="Beatmap.PrefabObject"/>.
        /// </summary>
        public static ModifierCompatibility PrefabObjectCompatible => new ModifierCompatibility()
        {
            PrefabObject = true,
        };
        /// <summary>
        /// The modifier is only compatible with <see cref="Player.PAPlayer"/>.
        /// </summary>
        public static ModifierCompatibility PAPlayerCompatible => new ModifierCompatibility()
        {
            PAPlayer = true,
        };
        /// <summary>
        /// The modifier is only compatible with <see cref="Player.PlayerModel"/>.
        /// </summary>
        public static ModifierCompatibility PlayerModelCompatible => new ModifierCompatibility()
        {
            PlayerModel = true,
        };
        /// <summary>
        /// The modifier is only compatible with <see cref="Beatmap.GameData"/>.
        /// </summary>
        public static ModifierCompatibility GameDataCompatible => new ModifierCompatibility()
        {
            GameData = true,
        };

        #endregion

        #region Values

        /// <summary>
        /// If all types are compatible.
        /// </summary>
        public bool All
        {
            get => BeatmapObject && BackgroundObject && PrefabObject && PAPlayer && GameData;
            set
            {
                BeatmapObject = value;
                BackgroundObject = value;
                PrefabObject = value;
                PAPlayer = value;
                GameData = value;
            }
        }
        /// <summary>
        /// If the modifier is only compatible with <see cref="Beatmap.BeatmapObject"/>.
        /// </summary>
        public bool BeatmapObject { get; set; }
        /// <summary>
        /// If the modifier is only compatible with <see cref="Beatmap.BackgroundObject"/>.
        /// </summary>
        public bool BackgroundObject { get; set; }
        /// <summary>
        /// If the modifier is only compatible with <see cref="Beatmap.PrefabObject"/>.
        /// </summary>
        public bool PrefabObject { get; set; }
        /// <summary>
        /// If the modifier is only compatible with <see cref="Player.PAPlayer"/>.
        /// </summary>
        public bool PAPlayer { get; set; }
        /// <summary>
        /// If the modifier is only compatible with <see cref="Player.PlayerModel"/>.
        /// </summary>
        public bool PlayerModel { get; set; }
        /// <summary>
        /// If the modifier is only compatible with <see cref="Beatmap.GameData"/>.
        /// </summary>
        public bool GameData { get; set; }

        public const string OBJ = "obj";
        public const string BG_OBJ = "bg_obj";
        public const string PREFAB_OBJ = "prefab_obj";
        public const string PLAYER = "player";
        public const string PLAYER_MODEL = "player_model";
        public const string GAME = "game";

        #endregion

        #region Methods

        public ModifierCompatibility WithBeatmapObject(bool compat)
        {
            BeatmapObject = compat;
            return this;
        }
        
        public ModifierCompatibility WithBackgroundObject(bool compat)
        {
            BackgroundObject = compat;
            return this;
        }
        
        public ModifierCompatibility WithPrefabObject(bool compat)
        {
            PrefabObject = compat;
            return this;
        }
        
        public ModifierCompatibility WithPAPlayer(bool compat)
        {
            PAPlayer = compat;
            return this;
        }
        
        public ModifierCompatibility WithPlayerModel(bool compat)
        {
            PlayerModel = compat;
            return this;
        }

        public ModifierCompatibility WithGameData(bool compat)
        {
            GameData = compat;
            return this;
        }

        /// <summary>
        /// Sets the compatibility value.
        /// </summary>
        /// <param name="value">Value to set. If it equals one of the constant names, the associated value will be enabled.</param>
        public void SetCompat(string value)
        {
            BeatmapObject = value == OBJ;
            BackgroundObject = value == BG_OBJ;
            PrefabObject = value == PREFAB_OBJ;
            PAPlayer = value == PLAYER;
            PlayerModel = value == PLAYER_MODEL;
            GameData = value == GAME;
        }

        /// <summary>
        /// Parses a modifier compatibility from JSON.
        /// </summary>
        /// <param name="jn">JSON to parse.</param>
        /// <returns>Returns the specified modifier compatibility.</returns>
        public static ModifierCompatibility Parse(JSONNode jn)
        {
            var compatibility = AllCompatible;

            if (jn == null)
                return compatibility;

            if (jn.IsString)
            {
                var value = jn.Value;

                if (value.Contains("0") || value.Contains("1"))
                {
                    compatibility.BeatmapObject = value.Length > 0 && value[0] == '1';
                    compatibility.BackgroundObject = value.Length > 1 && value[1] == '1';
                    compatibility.PrefabObject = value.Length > 2 && value[2] == '1';
                    compatibility.PAPlayer = value.Length > 3 && value[3] == '1';
                    compatibility.PlayerModel = value.Length > 4 && value[4] == '1';
                    compatibility.GameData = value.Length > 5 && value[5] == '1';
                }
                else
                    compatibility.SetCompat(value);

                return compatibility;
            }

            if (jn.IsArray)
            {
                compatibility.BeatmapObject = jn[0].AsBool;
                compatibility.BackgroundObject = jn[1].AsBool;
                compatibility.PrefabObject = jn[2].AsBool;
                compatibility.PAPlayer = jn[3].AsBool;
                compatibility.PlayerModel = jn[4].AsBool;
                compatibility.GameData = jn[5].AsBool;
                return compatibility;
            }

            if (jn[OBJ] != null)
                compatibility.BeatmapObject = jn[OBJ].AsBool;
            
            if (jn[BG_OBJ] != null)
                compatibility.BackgroundObject = jn[BG_OBJ].AsBool;
            
            if (jn[PREFAB_OBJ] != null)
                compatibility.PrefabObject = jn[PREFAB_OBJ].AsBool;
            
            if (jn[PLAYER] != null)
                compatibility.PAPlayer = jn[PLAYER].AsBool;
            
            if (jn[PLAYER_MODEL] != null)
                compatibility.PlayerModel = jn[PLAYER_MODEL].AsBool;
            
            if (jn[GAME] != null)
                compatibility.GameData = jn[GAME].AsBool;

            return compatibility;
        }

        /// <summary>
        /// Compares the reference type with the compatibility.
        /// </summary>
        /// <param name="referenceType">Reference Type.</param>
        /// <returns>Returns true if the reference type is equal, otherwise returns false.</returns>
        public bool CompareType(ModifierReferenceType referenceType) => referenceType switch
        {
            ModifierReferenceType.BeatmapObject => BeatmapObject,
            ModifierReferenceType.BackgroundObject => BackgroundObject,
            ModifierReferenceType.PrefabObject => PrefabObject,
            ModifierReferenceType.PAPlayer => PAPlayer,
            ModifierReferenceType.PlayerModel => PlayerModel,
            ModifierReferenceType.GameData => GameData,
            _ => false,
        };

        /// <summary>
        /// Gets a modifier compatibility from a reference type.
        /// </summary>
        /// <param name="referenceType">Reference Type.</param>
        /// <returns>Returns the associated reference type.</returns>
        public static ModifierCompatibility FromType(ModifierReferenceType referenceType) => referenceType switch
        {
            ModifierReferenceType.BeatmapObject => BeatmapObjectCompatible,
            ModifierReferenceType.BackgroundObject => BackgroundObjectCompatible,
            ModifierReferenceType.PrefabObject => PrefabObjectCompatible,
            ModifierReferenceType.PAPlayer => PAPlayerCompatible,
            ModifierReferenceType.PlayerModel => PlayerModelCompatible,
            ModifierReferenceType.GameData => GameDataCompatible,
            _ => AllCompatible,
        };

        public override int GetHashCode() => CoreHelper.CombineHashCodes(BeatmapObject, BackgroundObject, PrefabObject, PAPlayer, PlayerModel, GameData);

        public override bool Equals(object obj) => obj is ModifierCompatibility compatibility &&
            BeatmapObject == compatibility.BeatmapObject &&
            BackgroundObject == compatibility.BackgroundObject &&
            PrefabObject == compatibility.PrefabObject &&
            PAPlayer == compatibility.PAPlayer &&
            PlayerModel == compatibility.PlayerModel &&
            GameData == compatibility.GameData;

        #endregion
    }
}
