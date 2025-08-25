using SimpleJSON;

using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Data.Modifiers
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
            bool playerObject,
            bool gameData,
            bool storyOnly = false)
        {
            BeatmapObject = beatmapObject;
            BackgroundObject = backgroundObject;
            PrefabObject = prefabObject;
            PAPlayer = paPlayer;
            PlayerModel = playerModel;
            PlayerObject = playerObject;
            GameData = gameData;
            StoryOnly = storyOnly;
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
        /// The modifier is only compatible with <see cref="Player.PlayerObject"/>.
        /// </summary>
        public static ModifierCompatibility PlayerObjectCompatible => new ModifierCompatibility()
        {
            PlayerObject = true,
        };
        /// <summary>
        /// The modifier is only compatible with <see cref="Beatmap.GameData"/>.
        /// </summary>
        public static ModifierCompatibility GameDataCompatible => new ModifierCompatibility()
        {
            GameData = true,
        };
        /// <summary>
        /// The modifier is only compatible with <see cref="Player.PAPlayer"/> and <see cref="Player.PlayerModel"/>.
        /// </summary>
        public static ModifierCompatibility FullPlayerCompatible => new ModifierCompatibility()
        {
            PAPlayer = true,
            PlayerModel = true,
            PlayerObject = true,
        };
        /// <summary>
        /// The modifier is only compatible with all objects in a beatmap.
        /// </summary>
        public static ModifierCompatibility FullBeatmapCompatible => new ModifierCompatibility()
        {
            BeatmapObject = true,
            BackgroundObject = true,
            PrefabObject = true,
        };
        /// <summary>
        /// The modifier is only compatible with all objects in a beatmap.
        /// </summary>
        public static ModifierCompatibility LevelControlCompatible => new ModifierCompatibility()
        {
            BeatmapObject = true,
            BackgroundObject = true,
            PrefabObject = true,
            PAPlayer = true,
            GameData = true,
        };
        /// <summary>
        /// If the modifier can only run in the story mode.
        /// </summary>
        public static ModifierCompatibility StoryOnlyCompatible => new ModifierCompatibility()
        {
            StoryOnly = true,
        };

        #endregion

        #region Values

        /// <summary>
        /// If all types are compatible.
        /// </summary>
        public bool All
        {
            get => BeatmapObject && BackgroundObject && PrefabObject && PAPlayer && PlayerModel && PlayerObject && GameData;
            set
            {
                BeatmapObject = value;
                BackgroundObject = value;
                PrefabObject = value;
                PAPlayer = value;
                PlayerModel = value;
                PlayerObject = value;
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
        /// If the modifier is only compatible with <see cref="Player.PlayerObject"/>.
        /// </summary>
        public bool PlayerObject { get; set; }
        /// <summary>
        /// If the modifier is only compatible with <see cref="Beatmap.GameData"/>.
        /// </summary>
        public bool GameData { get; set; }

        /// <summary>
        /// If the modifier can only run in the story mode.
        /// </summary>
        public bool StoryOnly { get; set; }

        public const string OBJ = "obj";
        public const string BG_OBJ = "bg_obj";
        public const string PREFAB_OBJ = "prefab_obj";
        public const string PLAYER = "player";
        public const string PLAYER_MODEL = "player_model";
        public const string PLAYER_OBJ = "player_obj";
        public const string GAME = "game";

        public const string FULL_PLAYER = "full_player";
        public const string FULL_BEATMAP = "full_beatmap";
        public const string LEVEL_CONTROL = "level_control";

        public const string STORY_ONLY = "story_only";

        #endregion

        #region Methods

        public ModifierCompatibility WithBeatmapObject(bool compat = true)
        {
            BeatmapObject = compat;
            return this;
        }
        
        public ModifierCompatibility WithBackgroundObject(bool compat = true)
        {
            BackgroundObject = compat;
            return this;
        }
        
        public ModifierCompatibility WithPrefabObject(bool compat = true)
        {
            PrefabObject = compat;
            return this;
        }
        
        public ModifierCompatibility WithPAPlayer(bool compat = true)
        {
            PAPlayer = compat;
            return this;
        }
        
        public ModifierCompatibility WithPlayerModel(bool compat = true)
        {
            PlayerModel = compat;
            return this;
        }

        public ModifierCompatibility WithPlayerObject(bool compat = true)
        {
            PlayerObject = compat;
            return this;
        }

        public ModifierCompatibility WithGameData(bool compat = true)
        {
            GameData = compat;
            return this;
        }

        public ModifierCompatibility WithStoryOnly(bool compat = true)
        {
            StoryOnly = compat;
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
            PlayerObject = value == PLAYER_OBJ;
            GameData = value == GAME;
            if (value == FULL_PLAYER)
            {
                PAPlayer = true;
                PlayerModel = true;
                PlayerObject = true;
            }
            if (value == FULL_BEATMAP)
            {
                BeatmapObject = true;
                BackgroundObject = true;
                PrefabObject = true;
            }
            if (value == LEVEL_CONTROL)
            {
                BeatmapObject = true;
                BackgroundObject = true;
                PrefabObject = true;
                PAPlayer = true;
                GameData = true;
            }

            StoryOnly = value == STORY_ONLY;
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

            compatibility.All = false;

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
                    compatibility.PlayerObject = value.Length > 5 && value[5] == '1';
                    compatibility.GameData = value.Length > 6 && value[6] == '1';
                    compatibility.StoryOnly = value.Length > 7 && value[7] == '1';
                }
                else
                    compatibility.SetCompat(value);

                return compatibility;
            }

            if (jn.IsArray)
            {
                for (int i = 0; i < jn.Count; i++)
                {
                    if (jn[i].IsString)
                    {
                        compatibility.SetCompat(jn[i]);
                        continue;
                    }

                    if (jn[i].IsBoolean)
                    {
                        switch (i)
                        {
                            case 0: {
                                    compatibility.BeatmapObject = jn[0].AsBool;
                                    break;
                                }
                            case 1: {
                                    compatibility.BackgroundObject = jn[1].AsBool;
                                    break;
                                }
                            case 2: {
                                    compatibility.PrefabObject = jn[2].AsBool;
                                    break;
                                }
                            case 3: {
                                    compatibility.PAPlayer = jn[3].AsBool;
                                    break;
                                }
                            case 4: {
                                    compatibility.PlayerModel = jn[4].AsBool;
                                    break;
                                }
                            case 5: {
                                    compatibility.PlayerObject = jn[5].AsBool;
                                    break;
                                }
                            case 6: {
                                    compatibility.GameData = jn[6].AsBool;
                                    break;
                                }
                            case 7: {
                                    compatibility.StoryOnly = jn[7].AsBool;
                                    break;
                                }
                        }
                    }
                }

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
            
            if (jn[PLAYER_OBJ] != null)
                compatibility.PlayerObject = jn[PLAYER_OBJ].AsBool;

            if (jn[GAME] != null)
                compatibility.GameData = jn[GAME].AsBool;

            if (jn[FULL_PLAYER] != null)
            {
                var compat = jn[FULL_PLAYER].AsBool;
                compatibility.PAPlayer = compat;
                compatibility.PlayerModel = compat;
            }
            
            if (jn[FULL_BEATMAP] != null)
            {
                var compat = jn[FULL_BEATMAP].AsBool;
                compatibility.BeatmapObject = compat;
                compatibility.BackgroundObject = compat;
                compatibility.PrefabObject = compat;
            }
            
            if (jn[LEVEL_CONTROL] != null)
            {
                var compat = jn[LEVEL_CONTROL].AsBool;
                compatibility.BeatmapObject = compat;
                compatibility.BackgroundObject = compat;
                compatibility.PrefabObject = compat;
                compatibility.PAPlayer = compat;
                compatibility.GameData = compat;
            }

            if (jn[STORY_ONLY] != null)
                compatibility.StoryOnly = jn[STORY_ONLY].AsBool;

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
            ModifierReferenceType.PlayerObject => PlayerObject,
            ModifierReferenceType.GameData => GameData,
            ModifierReferenceType.ModifierBlock => BeatmapObject || BackgroundObject || PrefabObject || PAPlayer || GameData,
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
            ModifierReferenceType.PlayerObject => PlayerObjectCompatible,
            ModifierReferenceType.GameData => GameDataCompatible,
            _ => AllCompatible,
        };

        public override int GetHashCode() => CoreHelper.CombineHashCodes(BeatmapObject, BackgroundObject, PrefabObject, PAPlayer, PlayerModel, PlayerObject, GameData);

        public override bool Equals(object obj) => obj is ModifierCompatibility compatibility &&
            BeatmapObject == compatibility.BeatmapObject &&
            BackgroundObject == compatibility.BackgroundObject &&
            PrefabObject == compatibility.PrefabObject &&
            PAPlayer == compatibility.PAPlayer &&
            PlayerModel == compatibility.PlayerModel &&
            PlayerObject == compatibility.PlayerObject &&
            GameData == compatibility.GameData;

        #endregion
    }
}
