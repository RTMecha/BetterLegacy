using BetterLegacy.Core.Data.Modifiers.Functions;

namespace BetterLegacy.Core.Data.Modifiers
{
    /// <summary>
    /// Library of modifier functions.
    /// </summary>
    public static class ModifierFunctions
    {
        #region Main

        public static Break breakModifier = new Break();

        public static Continue continueModifier = new Continue(false);

        public static Continue returnModifier = new Continue(true);

        public static Else elseModifier = new Else();

        public static DisableModifier disableModifier = new DisableModifier();

        public static ForLoop forLoop = new ForLoop();

        public static ResetLoop resetLoop = new ResetLoop();

        public static Await await = new Await();

        public static AwaitCounter awaitCounter = new AwaitCounter();

        #region Variable

        public static GetToggle getToggle = new GetToggle();

        public static GetFloat getFloat = new GetFloat();

        public static GetInt getInt = new GetInt();

        public static GetIncrementFloat getIncrementFloat = new GetIncrementFloat();

        public static GetIncrementInt getIncrementInt = new GetIncrementInt();

        public static GetString getString = new GetString(GetString.Type.Normal);

        public static GetString getStringLower = new GetString(GetString.Type.Lower);

        public static GetString getStringUpper = new GetString(GetString.Type.Upper);

        public static GetString getStringLength = new GetString(GetString.Type.Length);

        public static GetEnum getEnum = new GetEnum();

        public static GetRandom getRandom = new GetRandom();

        public static GetRandomVector2 getRandomVector2 = new GetRandomVector2();

        public static GetMath getMath = new GetMath();

        public static GetSubString getSubString = new GetSubString();

        public static GetSplitString getSplitString = new GetSplitString(GetSplitString.Type.Array);

        public static GetSplitString getSplitStringAt = new GetSplitString(GetSplitString.Type.At);

        public static GetSplitString getSplitStringCount = new GetSplitString(GetSplitString.Type.Count);

        public static GetParsedString getParsedString = new GetParsedString();

        public static GetRegex getRegex = new GetRegex();

        public static GetFormatVariable getFormatVariable = new GetFormatVariable();

        public static GetComparison getComparison = new GetComparison(false);

        public static GetComparison getComparisonMath = new GetComparison(true);

        public static GetQuickElement getQuickElement = new GetQuickElement();

        public static GetObjectName getObjectName = new GetObjectName();

        #endregion

        #region Random

        public static RandomCompare randomEquals = new RandomCompare(NumberComparison.Equals);

        public static RandomCompare randomLesser = new RandomCompare(NumberComparison.Lesser);

        public static RandomCompare randomGreater = new RandomCompare(NumberComparison.Greater);

        #endregion

        #region Math

        public static MathCompare mathEquals = new MathCompare(NumberComparison.Equals);

        public static MathCompare mathLesserEquals = new MathCompare(NumberComparison.LesserEquals);

        public static MathCompare mathGreaterEquals = new MathCompare(NumberComparison.GreaterEquals);

        public static MathCompare mathLesser = new MathCompare(NumberComparison.Lesser);

        public static MathCompare mathGreater = new MathCompare(NumberComparison.Greater);

        #endregion

        #region Object Variable

        public static GetObjectVariable getObjectVariable = new GetObjectVariable(false);

        public static GetObjectVariable getObjectVariableOther = new GetObjectVariable(true);

        public static ObjectVariableCompare objectVariableEquals = new ObjectVariableCompare(NumberComparison.Equals, false);

        public static ObjectVariableCompare objectVariableLesserEquals = new ObjectVariableCompare(NumberComparison.LesserEquals, false);

        public static ObjectVariableCompare objectVariableGreaterEquals = new ObjectVariableCompare(NumberComparison.GreaterEquals, false);

        public static ObjectVariableCompare objectVariableLesser = new ObjectVariableCompare(NumberComparison.Lesser, false);

        public static ObjectVariableCompare objectVariableGreater = new ObjectVariableCompare(NumberComparison.Greater, false);

        public static ObjectVariableCompare objectVariableOtherEquals = new ObjectVariableCompare(NumberComparison.Equals, true);

        public static ObjectVariableCompare objectVariableOtherLesserEquals = new ObjectVariableCompare(NumberComparison.LesserEquals, true);

        public static ObjectVariableCompare objectVariableOtherGreaterEquals = new ObjectVariableCompare(NumberComparison.GreaterEquals, true);

        public static ObjectVariableCompare objectVariableOtherLesser = new ObjectVariableCompare(NumberComparison.Lesser, true);

        public static ObjectVariableCompare objectVariableOtherGreater = new ObjectVariableCompare(NumberComparison.Greater, true);

        public static ObjectVariable addObjectVariable = new ObjectVariable(ObjectVariable.Operation.Add, false, false);

        public static ObjectVariable addObjectVariableOther = new ObjectVariable(ObjectVariable.Operation.Add, true, false);

        public static ObjectVariable subObjectVariable = new ObjectVariable(ObjectVariable.Operation.Sub, false, false);

        public static ObjectVariable subObjectVariableOther = new ObjectVariable(ObjectVariable.Operation.Sub, true, false);

        public static ObjectVariable setObjectVariable = new ObjectVariable(ObjectVariable.Operation.Set, false, false);

        public static ObjectVariable setObjectVariableOther = new ObjectVariable(ObjectVariable.Operation.Set, true, false);

        public static ObjectVariable setObjectVariableMath = new ObjectVariable(ObjectVariable.Operation.Set, false, true);

        public static ObjectVariable setObjectVariableMathOther = new ObjectVariable(ObjectVariable.Operation.Set, true, true);

        public static AnimateVariableOther animateVariableOther = new AnimateVariableOther();

        public static ClampVariable clampVariable = new ClampVariable(false);

        public static ClampVariable clampVariableOther = new ClampVariable(true);

        #endregion

        #endregion

        #region Editor

        public static Comment comment = new Comment();

        public static EditorNotify editorNotify = new EditorNotify();

        public static GetEditorDataProperty getEditorBin = new GetEditorDataProperty(GetEditorDataProperty.EditorDataProperty.Bin);

        public static GetEditorDataProperty getEditorLayer = new GetEditorDataProperty(GetEditorDataProperty.EditorDataProperty.Layer);

        public static Region region = new Region(false);

        public static Region endregion = new Region(true);

        #endregion

        #region Modifier

        public static GetTag getTag = new GetTag();

        public static GetSignaledVariables getSignaledVariables = new GetSignaledVariables();

        public static SignalLocalVariables signalLocalVariables = new SignalLocalVariables();

        public static ClearLocalVariables clearLocalVariables = new ClearLocalVariables();

        public static StoreLocalVariables storeLocalVariables = new StoreLocalVariables();

        public static LocalVariableCompare localVariableEquals = new LocalVariableCompare(NumberComparison.Equals);

        public static LocalVariableCompare localVariableLesserEquals = new LocalVariableCompare(NumberComparison.LesserEquals);

        public static LocalVariableCompare localVariableGreaterEquals = new LocalVariableCompare(NumberComparison.GreaterEquals);

        public static LocalVariableCompare localVariableLesser = new LocalVariableCompare(NumberComparison.Lesser);

        public static LocalVariableCompare localVariableGreater = new LocalVariableCompare(NumberComparison.Greater);

        public static LocalVariableContains localVariableContains = new LocalVariableContains();

        public static LocalVariableStartsWith localVariableStartsWith = new LocalVariableStartsWith();

        public static LocalVariableEndsWith localVariableEndsWith = new LocalVariableEndsWith();

        public static LocalVariableExists localVariableExists = new LocalVariableExists();

        public static ContainsTag containsTag = new ContainsTag();

        public static RequireSignal requireSignal = new RequireSignal();

        public static SignalModifier signalModifier = new SignalModifier();

        public static ActivateModifier activateModifier = new ActivateModifier();

        public static CallModifierBlock callModifierBlock = new CallModifierBlock();

        public static CallModifierBlockTrigger callModifierBlockTrigger = new CallModifierBlockTrigger();

        public static CallModifiers callModifiers = new CallModifiers();

        public static CallModifiersTrigger callModifiersTrigger = new CallModifiersTrigger();

        public static AddTag addTag = new AddTag();

        #endregion

        #region Audio

        public static GetAudioProperty getPitch = new GetAudioProperty(GetAudioProperty.Property.Pitch);

        public static GetAudioProperty getMusicTime = new GetAudioProperty(GetAudioProperty.Property.MusicTime);

        public static GetSample getSample = new GetSample();

        public static PitchCompare pitchEquals = new PitchCompare(NumberComparison.Equals);

        public static PitchCompare pitchLesserEquals = new PitchCompare(NumberComparison.LesserEquals);

        public static PitchCompare pitchGreaterEquals = new PitchCompare(NumberComparison.GreaterEquals);

        public static PitchCompare pitchLesser = new PitchCompare(NumberComparison.Lesser);

        public static PitchCompare pitchGreater = new PitchCompare(NumberComparison.Greater);

        public static MusicTimeCompare musicTimeEquals = new MusicTimeCompare(NumberComparison.Equals);

        public static MusicTimeCompare musicTimeLesserEquals = new MusicTimeCompare(NumberComparison.LesserEquals);

        public static MusicTimeCompare musicTimeGreaterEquals = new MusicTimeCompare(NumberComparison.GreaterEquals);

        public static MusicTimeCompare musicTimeLesser = new MusicTimeCompare(NumberComparison.Lesser);

        public static MusicTimeCompare musicTimeGreater = new MusicTimeCompare(NumberComparison.Greater);

        public static MusicTimeInRange musicTimeInRange = new MusicTimeInRange();

        public static MusicPlaying musicPlaying = new MusicPlaying();

        public static OnBPM onBPM = new OnBPM();

        public static SetAudioProperty setPitch = new SetAudioProperty(MathOperation.Set, SetAudioProperty.AudioProperty.Pitch, SetAudioProperty.ObjectProperty.None, false);

        public static SetAudioProperty addPitch = new SetAudioProperty(MathOperation.Addition, SetAudioProperty.AudioProperty.Pitch, SetAudioProperty.ObjectProperty.None, false);

        public static SetAudioProperty setPitchMath = new SetAudioProperty(MathOperation.Set, SetAudioProperty.AudioProperty.Pitch, SetAudioProperty.ObjectProperty.None, true);

        public static SetAudioProperty addPitchMath = new SetAudioProperty(MathOperation.Addition, SetAudioProperty.AudioProperty.Pitch, SetAudioProperty.ObjectProperty.None, true);

        public static SetAudioProperty setMusicTime = new SetAudioProperty(MathOperation.Set, SetAudioProperty.AudioProperty.MusicTime, SetAudioProperty.ObjectProperty.None, false);

        public static SetAudioProperty setMusicTimeMath = new SetAudioProperty(MathOperation.Set, SetAudioProperty.AudioProperty.MusicTime, SetAudioProperty.ObjectProperty.None, true);

        public static SetAudioProperty setMusicTimeStartTime = new SetAudioProperty(MathOperation.Set, SetAudioProperty.AudioProperty.MusicTime, SetAudioProperty.ObjectProperty.StartTime, false);

        public static SetAudioProperty setMusicTimeAutokill = new SetAudioProperty(MathOperation.Set, SetAudioProperty.AudioProperty.MusicTime, SetAudioProperty.ObjectProperty.Autokill, false);

        public static SetMusicPlaying setMusicPlaying = new SetMusicPlaying();

        public static PlaySound playSound = new PlaySound(PlaySound.SoundSource.Regular);

        public static PlaySound playOnlineSound = new PlaySound(PlaySound.SoundSource.Online);

        public static PlaySound playDefaultSound = new PlaySound(PlaySound.SoundSource.Default);

        public static AudioSourceModifier audioSource = new AudioSourceModifier();

        public static LoadSoundAsset loadSoundAsset = new LoadSoundAsset();

        #region Reactive

        public static ReactiveProperty reactivePos = new ReactiveProperty(ReactiveProperty.Property.Pos, false);

        public static ReactiveProperty reactiveSca = new ReactiveProperty(ReactiveProperty.Property.Sca, false);

        public static ReactiveProperty reactiveRot = new ReactiveProperty(ReactiveProperty.Property.Rot, false);

        public static ReactiveProperty reactiveCol = new ReactiveProperty(ReactiveProperty.Property.Col, false);

        public static ReactiveProperty reactiveColLerp = new ReactiveProperty(ReactiveProperty.Property.Col, true);

        public static ReactiveProperty reactivePosChain = new ReactiveProperty(ReactiveProperty.Property.Pos, true);

        public static ReactiveProperty reactiveScaChain = new ReactiveProperty(ReactiveProperty.Property.Sca, true);

        public static ReactiveProperty reactiveRotChain = new ReactiveProperty(ReactiveProperty.Property.Rot, true);

        public static ReactiveProperty reactiveIterations = new ReactiveProperty(ReactiveProperty.Property.Iterations, false);

        #endregion

        #endregion

        #region Level

        #region Game State

        public static GameState inZenMode = new GameState(GameState.Property.inZenMode);

        public static GameState inNormal = new GameState(GameState.Property.inNormal);

        public static GameState in1Life = new GameState(GameState.Property.in1Life);

        public static GameState inNoHit = new GameState(GameState.Property.inNoHit);

        public static GameState inPractice = new GameState(GameState.Property.inPractice);

        public static GameState inEditor = new GameState(GameState.Property.inEditor);

        public static GameState isEditing = new GameState(GameState.Property.isEditing);

        public static GameState isHosting = new GameState(GameState.Property.isHosting);

        public static GameState inLobby = new GameState(GameState.Property.inLobby);

        #endregion

        #region Level Rank

        public static LevelRankCompare levelRankEquals = new LevelRankCompare(LevelRankCompare.From.CurrentLevel, NumberComparison.Equals);

        public static LevelRankCompare levelRankLesserEquals = new LevelRankCompare(LevelRankCompare.From.CurrentLevel, NumberComparison.LesserEquals);

        public static LevelRankCompare levelRankGreaterEquals = new LevelRankCompare(LevelRankCompare.From.CurrentLevel, NumberComparison.GreaterEquals);

        public static LevelRankCompare levelRankLesser = new LevelRankCompare(LevelRankCompare.From.CurrentLevel, NumberComparison.Lesser);

        public static LevelRankCompare levelRankGreater = new LevelRankCompare(LevelRankCompare.From.CurrentLevel, NumberComparison.Greater);

        public static LevelRankCompare levelRankOtherEquals = new LevelRankCompare(LevelRankCompare.From.Other, NumberComparison.Equals);

        public static LevelRankCompare levelRankOtherLesserEquals = new LevelRankCompare(LevelRankCompare.From.Other, NumberComparison.LesserEquals);

        public static LevelRankCompare levelRankOtherGreaterEquals = new LevelRankCompare(LevelRankCompare.From.Other, NumberComparison.GreaterEquals);

        public static LevelRankCompare levelRankOtherLesser = new LevelRankCompare(LevelRankCompare.From.Other, NumberComparison.Lesser);

        public static LevelRankCompare levelRankOtherGreater = new LevelRankCompare(LevelRankCompare.From.Other, NumberComparison.Greater);

        public static LevelRankCompare levelRankCurrentEquals = new LevelRankCompare(LevelRankCompare.From.Current, NumberComparison.Equals);

        public static LevelRankCompare levelRankCurrentLesserEquals = new LevelRankCompare(LevelRankCompare.From.Current, NumberComparison.LesserEquals);

        public static LevelRankCompare levelRankCurrentGreaterEquals = new LevelRankCompare(LevelRankCompare.From.Current, NumberComparison.GreaterEquals);

        public static LevelRankCompare levelRankCurrentLesser = new LevelRankCompare(LevelRankCompare.From.Current, NumberComparison.Lesser);

        public static LevelRankCompare levelRankCurrentGreater = new LevelRankCompare(LevelRankCompare.From.Current, NumberComparison.Greater);

        #endregion

        public static OnLevelStart onLevelStart = new OnLevelStart();

        public static OnLevelRestart onLevelRestart = new OnLevelRestart();

        public static OnLevelRewind onLevelRewind = new OnLevelRewind();

        public static LevelUnlocked levelUnlocked = new LevelUnlocked();

        public static LevelCompleted levelCompleted = new LevelCompleted(false);

        public static LevelCompleted levelCompletedOther = new LevelCompleted(true);

        public static LevelExists levelExists = new LevelExists(false);

        public static LevelExists levelPathExists = new LevelExists(true);

        #region Achievement

        public static AchievementUnlocked achievementUnlocked = new AchievementUnlocked();

        public static AchievementModifier unlockAchievement = new AchievementModifier(true);

        public static AchievementModifier lockAchievement = new AchievementModifier(false);

        public static GetAchievementUnlocked getAchievementUnlocked = new GetAchievementUnlocked();

        #endregion

        public static SaveLevelData saveLevelData = new SaveLevelData();

        #region Level Rank Properties

        public static RankPropertyModifier clearHits = new RankPropertyModifier(RankPropertyModifier.Action.Clear, RankPropertyModifier.Property.Hit);

        public static RankPropertyModifier addHit = new RankPropertyModifier(RankPropertyModifier.Action.Add, RankPropertyModifier.Property.Hit);

        public static RankPropertyModifier subHit = new RankPropertyModifier(RankPropertyModifier.Action.Sub, RankPropertyModifier.Property.Hit);

        public static RankPropertyModifier clearDeaths = new RankPropertyModifier(RankPropertyModifier.Action.Clear, RankPropertyModifier.Property.Death);

        public static RankPropertyModifier addDeath = new RankPropertyModifier(RankPropertyModifier.Action.Add, RankPropertyModifier.Property.Death);

        public static RankPropertyModifier subDeath = new RankPropertyModifier(RankPropertyModifier.Action.Sub, RankPropertyModifier.Property.Death);

        public static GetRankProperty getHitCount = new GetRankProperty(GetRankProperty.Property.Hit);

        public static GetRankProperty getDeathCount = new GetRankProperty(GetRankProperty.Property.Death);

        #endregion

        #region Load Level

        public static LoadLevel loadLevel = new LoadLevel(LoadLevel.Type.Path);

        public static LoadLevel loadLevelID = new LoadLevel(LoadLevel.Type.ID);

        public static LoadLevel loadLevelInternal = new LoadLevel(LoadLevel.Type.Internal);

        public static LoadLevel loadLevelPrevious = new LoadLevel(LoadLevel.Type.Previous);

        public static LoadLevel loadLevelHub = new LoadLevel(LoadLevel.Type.Hub);

        public static LoadLevel loadLevelInCollection = new LoadLevel(LoadLevel.Type.Previous);

        public static LoadLevel loadLevelCollection = new LoadLevel(LoadLevel.Type.Collection);

        public static DownloadLevel downloadLevel = new DownloadLevel();

        #endregion

        public static EndLevel endLevel = new EndLevel();

        public static SetLevelProperty setAudioTransition = new SetLevelProperty(SetLevelProperty.Property.AudioTransition);

        public static SetLevelProperty setIntroFade = new SetLevelProperty(SetLevelProperty.Property.IntroFade);

        public static SetLevelProperty setLevelEndFunc = new SetLevelProperty(SetLevelProperty.Property.LevelEndFunc);

        public static GetCurrentLevelProperty getCurrentLevelID = new GetCurrentLevelProperty(GetCurrentLevelProperty.Property.ID);

        public static GetCurrentLevelProperty getCurrentArtistName = new GetCurrentLevelProperty(GetCurrentLevelProperty.Property.ArtistName);

        public static GetCurrentLevelProperty getCurrentSongTitle = new GetCurrentLevelProperty(GetCurrentLevelProperty.Property.SongTitle);

        public static GetCurrentLevelProperty getCurrentLevelName = new GetCurrentLevelProperty(GetCurrentLevelProperty.Property.LevelName);

        public static GetCurrentLevelProperty getCurrentLevelRank = new GetCurrentLevelProperty(GetCurrentLevelProperty.Property.LevelRank);

        #region Level Variable

        public static GetLevelVariable getLevelVariable = new GetLevelVariable(false, GetLevelVariable.Type.Level);

        public static SetLevelVariable setLevelVariable = new SetLevelVariable(false, SetLevelVariable.Type.Level);

        public static RemoveLevelVariable removeLevelVariable = new RemoveLevelVariable(false, RemoveLevelVariable.Type.Level);

        public static ClearLevelVariables clearLevelVariables = new ClearLevelVariables(false, ClearLevelVariables.Type.Level);

        public static GetLevelVariable getCurrentLevelVariable = new GetLevelVariable(true, GetLevelVariable.Type.Level);

        public static SetLevelVariable setCurrentLevelVariable = new SetLevelVariable(true, SetLevelVariable.Type.Level);

        public static RemoveLevelVariable removeCurrentLevelVariable = new RemoveLevelVariable(true, RemoveLevelVariable.Type.Level);

        public static ClearLevelVariables clearCurrentLevelVariables = new ClearLevelVariables(true, ClearLevelVariables.Type.Level);

        public static GetLevelVariable getCollectionVariable = new GetLevelVariable(false, GetLevelVariable.Type.Collection);

        public static SetLevelVariable setCollectionVariable = new SetLevelVariable(false, SetLevelVariable.Type.Collection);

        public static RemoveLevelVariable removeCollectionVariable = new RemoveLevelVariable(false, RemoveLevelVariable.Type.Collection);

        public static ClearLevelVariables clearCollectionVariables = new ClearLevelVariables(false, ClearLevelVariables.Type.Collection);

        public static GetLevelVariable getCurrentCollectionVariable = new GetLevelVariable(true, GetLevelVariable.Type.Collection);

        public static SetLevelVariable setCurrentCollectionVariable = new SetLevelVariable(true, SetLevelVariable.Type.Collection);

        public static RemoveLevelVariable removeCurrentCollectionVariable = new RemoveLevelVariable(true, RemoveLevelVariable.Type.Collection);

        public static ClearLevelVariables clearCurrentCollectionVariables = new ClearLevelVariables(true, ClearLevelVariables.Type.Collection);

        #endregion

        public static ShowTitleCard showTitleCard = new ShowTitleCard();

        public static SetTimelineLength setTimelineLength = new SetTimelineLength();

        #endregion

        #region Events

        public static GetEventValue getEventValue = new GetEventValue();

        public static EventCompare eventEquals = new EventCompare(NumberComparison.Equals);

        public static EventCompare eventLesserEquals = new EventCompare(NumberComparison.LesserEquals);

        public static EventCompare eventGreaterEquals = new EventCompare(NumberComparison.GreaterEquals);

        public static EventCompare eventLesser = new EventCompare(NumberComparison.Lesser);

        public static EventCompare eventGreater = new EventCompare(NumberComparison.Greater);

        public static EventOffset eventOffset = new EventOffset(EventOffset.Type.Normal);

        public static EventOffset eventOffsetVariable = new EventOffset(EventOffset.Type.Variable);

        public static EventOffset eventOffsetMath = new EventOffset(EventOffset.Type.Math);

        public static EventOffsetAnimate eventOffsetAnimate = new EventOffsetAnimate();

        public static EventOffsetCopyAxis eventOffsetCopyAxis = new EventOffsetCopyAxis();

        public static VignetteTracksPlayer vignetteTracksPlayer = new VignetteTracksPlayer();

        public static LensTracksPlayer lensTracksPlayer = new LensTracksPlayer();

        public static DatamoshFunction datamoshGlitch = new DatamoshFunction(DatamoshFunction.Function.Glitch);

        public static DatamoshFunction datamoshReset = new DatamoshFunction(DatamoshFunction.Function.Reset);

        #endregion

        #region Player

        public static GetNearestPlayer getNearestPlayer = new GetNearestPlayer();

        public static GetCollidingPlayers getCollidingPlayers = new GetCollidingPlayers();

        public static GetPlayerProperty getPlayerHealth = new GetPlayerProperty(GetPlayerProperty.Property.Health);

        public static GetPlayerProperty getPlayerLives = new GetPlayerProperty(GetPlayerProperty.Property.Lives);

        public static GetPlayerProperty getPlayerPosX = new GetPlayerProperty(GetPlayerProperty.Property.PosX);

        public static GetPlayerProperty getPlayerPosY = new GetPlayerProperty(GetPlayerProperty.Property.PosY);

        public static GetPlayerProperty getPlayerRot = new GetPlayerProperty(GetPlayerProperty.Property.Rot);

        public static GetPlayerVariable getPlayerVariable = new GetPlayerVariable();

        public static PlayerCollide playerCollide = new PlayerCollide(PlayerTriggerBase.Requirement.Nearest, false);

        public static PlayerCollide playerCollideIndex = new PlayerCollide(PlayerTriggerBase.Requirement.Index, false);

        public static PlayerCollide playerCollideOther = new PlayerCollide(PlayerTriggerBase.Requirement.Nearest, true);

        public static PlayerCollide playerCollideIndexOther = new PlayerCollide(PlayerTriggerBase.Requirement.Index, true);

        public static PlayerHealthCompare playerHealthEquals = new PlayerHealthCompare(NumberComparison.Equals);

        public static PlayerHealthCompare playerHealthLesserEquals = new PlayerHealthCompare(NumberComparison.LesserEquals);

        public static PlayerHealthCompare playerHealthGreaterEquals = new PlayerHealthCompare(NumberComparison.GreaterEquals);

        public static PlayerHealthCompare playerHealthLesser = new PlayerHealthCompare(NumberComparison.Lesser);

        public static PlayerHealthCompare playerHealthGreater = new PlayerHealthCompare(NumberComparison.Greater);

        public static PlayerDeathsCompare playerDeathsEquals = new PlayerDeathsCompare(NumberComparison.Equals);

        public static PlayerDeathsCompare playerDeathsLesserEquals = new PlayerDeathsCompare(NumberComparison.LesserEquals);

        public static PlayerDeathsCompare playerDeathsGreaterEquals = new PlayerDeathsCompare(NumberComparison.GreaterEquals);

        public static PlayerDeathsCompare playerDeathsLesser = new PlayerDeathsCompare(NumberComparison.Lesser);

        public static PlayerDeathsCompare playerDeathsGreater = new PlayerDeathsCompare(NumberComparison.Greater);

        public static PlayerBoostCompare playerBoostEquals = new PlayerBoostCompare(NumberComparison.Equals);

        public static PlayerBoostCompare playerBoostLesserEquals = new PlayerBoostCompare(NumberComparison.LesserEquals);

        public static PlayerBoostCompare playerBoostGreaterEquals = new PlayerBoostCompare(NumberComparison.GreaterEquals);

        public static PlayerBoostCompare playerBoostLesser = new PlayerBoostCompare(NumberComparison.Lesser);

        public static PlayerBoostCompare playerBoostGreater = new PlayerBoostCompare(NumberComparison.Greater);

        public static PlayerCountCompare playerCountEquals = new PlayerCountCompare(NumberComparison.Equals);

        public static PlayerCountCompare playerCountLesserEquals = new PlayerCountCompare(NumberComparison.LesserEquals);

        public static PlayerCountCompare playerCountGreaterEquals = new PlayerCountCompare(NumberComparison.GreaterEquals);

        public static PlayerCountCompare playerCountLesser = new PlayerCountCompare(NumberComparison.Lesser);

        public static PlayerCountCompare playerCountGreater = new PlayerCountCompare(NumberComparison.Greater);

        public static PlayerMoving playerMoving = new PlayerMoving(PlayerTriggerBase.Requirement.Nearest);

        public static PlayerMoving playerMovingIndex = new PlayerMoving(PlayerTriggerBase.Requirement.Index);

        public static PlayerBoosting playerBoosting = new PlayerBoosting(PlayerTriggerBase.Requirement.Nearest);

        public static PlayerBoosting playerBoostingIndex = new PlayerBoosting(PlayerTriggerBase.Requirement.Index);

        public static PlayerJumping playerJumping = new PlayerJumping(PlayerTriggerBase.Requirement.Nearest);

        public static PlayerJumping playerJumpingIndex = new PlayerJumping(PlayerTriggerBase.Requirement.Index);

        public static PlayerAlive playerAlive = new PlayerAlive(PlayerTriggerBase.Requirement.Nearest);

        public static PlayerAlive playerAliveIndex = new PlayerAlive(PlayerTriggerBase.Requirement.Index);

        public static PlayerAlive playerAliveAll = new PlayerAlive(PlayerTriggerBase.Requirement.All);

        public static PlayerInputTrigger playerInput = new PlayerInputTrigger(PlayerTriggerBase.Requirement.Nearest);

        public static PlayerInputTrigger playerInputIndex = new PlayerInputTrigger(PlayerTriggerBase.Requirement.Index);

        public static PlayerDistanceCompare playerDistanceGreater = new PlayerDistanceCompare(NumberComparison.Greater);

        public static PlayerDistanceCompare playerDistanceLesser = new PlayerDistanceCompare(NumberComparison.Lesser);

        public static OnPlayerHit onPlayerHit = new OnPlayerHit();

        public static OnPlayerDeath onPlayerDeath = new OnPlayerDeath();

        public static OnPlayerBoosted onPlayerBoosted = new OnPlayerBoosted();

        public static OnPlayerJumped onPlayerJumped = new OnPlayerJumped();

        public static PlayerIsLocal playerIsLocal = new PlayerIsLocal(PlayerTriggerBase.Requirement.Nearest);

        public static PlayerIsLocal playerIsLocalIndex = new PlayerIsLocal(PlayerTriggerBase.Requirement.Index);

        public static ForLoopPlayer forLoopPlayer = new ForLoopPlayer();

        public static PlayerHit playerHit = new PlayerHit(PlayerActionBase.Selector.Nearest);

        public static PlayerHit playerHitIndex = new PlayerHit(PlayerActionBase.Selector.Index);

        public static PlayerHit playerHitAll = new PlayerHit(PlayerActionBase.Selector.All);

        public static PlayerHeal playerHeal = new PlayerHeal(PlayerActionBase.Selector.Nearest);

        public static PlayerHeal playerHealIndex = new PlayerHeal(PlayerActionBase.Selector.Index);

        public static PlayerHeal playerHealAll = new PlayerHeal(PlayerActionBase.Selector.All);

        public static PlayerKill playerKill = new PlayerKill(PlayerActionBase.Selector.Nearest);

        public static PlayerKill playerKillIndex = new PlayerKill(PlayerActionBase.Selector.Index);

        public static PlayerKill playerKillAll = new PlayerKill(PlayerActionBase.Selector.All);

        public static PlayerRespawn playerRespawn = new PlayerRespawn(PlayerActionBase.Selector.Nearest);

        public static PlayerRespawn playerRespawnIndex = new PlayerRespawn(PlayerActionBase.Selector.Index);

        public static PlayerRespawn playerRespawnAll = new PlayerRespawn(PlayerActionBase.Selector.All);

        public static PlayerLock playerLockX = new PlayerLock(0, PlayerActionBase.Selector.Nearest);

        public static PlayerLock playerLockXIndex = new PlayerLock(0, PlayerActionBase.Selector.Index);

        public static PlayerLock playerLockXAll = new PlayerLock(0, PlayerActionBase.Selector.All);

        public static PlayerLock playerLockY = new PlayerLock(1, PlayerActionBase.Selector.Nearest);

        public static PlayerLock playerLockYIndex = new PlayerLock(1, PlayerActionBase.Selector.Index);

        public static PlayerLock playerLockYAll = new PlayerLock(1, PlayerActionBase.Selector.All);

        public static PlayerEnable playerEnable = new PlayerEnable(PlayerActionBase.Selector.Nearest);

        public static PlayerEnable playerEnableIndex = new PlayerEnable(PlayerActionBase.Selector.Index);

        public static PlayerEnable playerEnableAll = new PlayerEnable(PlayerActionBase.Selector.All);

        public static PlayerMove playerMove = new PlayerMove(-1, false, PlayerActionBase.Selector.Nearest);

        public static PlayerMove playerMoveIndex = new PlayerMove(-1, false, PlayerActionBase.Selector.Index);

        public static PlayerMove playerMoveAll = new PlayerMove(-1, false, PlayerActionBase.Selector.All);

        public static PlayerMove playerMoveX = new PlayerMove(0, false, PlayerActionBase.Selector.Nearest);

        public static PlayerMove playerMoveXIndex = new PlayerMove(0, false, PlayerActionBase.Selector.Index);

        public static PlayerMove playerMoveXAll = new PlayerMove(0, false, PlayerActionBase.Selector.All);

        public static PlayerMove playerMoveY = new PlayerMove(1, false, PlayerActionBase.Selector.Nearest);

        public static PlayerMove playerMoveYIndex = new PlayerMove(1, false, PlayerActionBase.Selector.Index);

        public static PlayerMove playerMoveYAll = new PlayerMove(1, false, PlayerActionBase.Selector.All);

        public static PlayerRotate playerRotate = new PlayerRotate(false, PlayerActionBase.Selector.Nearest);

        public static PlayerRotate playerRotateIndex = new PlayerRotate(false, PlayerActionBase.Selector.Index);

        public static PlayerRotate playerRotateAll = new PlayerRotate(false, PlayerActionBase.Selector.All);

        public static PlayerMove playerMoveToObject = new PlayerMove(-1, true, PlayerActionBase.Selector.Nearest);

        public static PlayerMove playerMoveIndexToObject = new PlayerMove(-1, true, PlayerActionBase.Selector.Index);

        public static PlayerMove playerMoveAllToObject = new PlayerMove(-1, true, PlayerActionBase.Selector.All);

        public static PlayerMove playerMoveXToObject = new PlayerMove(0, true, PlayerActionBase.Selector.Nearest);

        public static PlayerMove playerMoveXIndexToObject = new PlayerMove(0, true, PlayerActionBase.Selector.Index);

        public static PlayerMove playerMoveXAllToObject = new PlayerMove(0, true, PlayerActionBase.Selector.All);

        public static PlayerMove playerMoveYToObject = new PlayerMove(1, true, PlayerActionBase.Selector.Nearest);

        public static PlayerMove playerMoveYIndexToObject = new PlayerMove(1, true, PlayerActionBase.Selector.Index);

        public static PlayerMove playerMoveYAllToObject = new PlayerMove(1, true, PlayerActionBase.Selector.All);

        public static PlayerRotate playerRotateToObject = new PlayerRotate(true, PlayerActionBase.Selector.Nearest);

        public static PlayerRotate playerRotateIndexToObject = new PlayerRotate(true, PlayerActionBase.Selector.Index);

        public static PlayerRotate playerRotateAllToObject = new PlayerRotate(true, PlayerActionBase.Selector.All);

        public static PlayerDrag playerDrag = new PlayerDrag();

        public static PlayerBoost playerBoost = new PlayerBoost(PlayerActionBase.Selector.Nearest);

        public static PlayerBoost playerBoostIndex = new PlayerBoost(PlayerActionBase.Selector.Index);

        public static PlayerBoost playerBoostAll = new PlayerBoost(PlayerActionBase.Selector.All);

        public static PlayerCancelBoost playerCancelBoost = new PlayerCancelBoost(PlayerActionBase.Selector.Nearest);

        public static PlayerCancelBoost playerCancelBoostIndex = new PlayerCancelBoost(PlayerActionBase.Selector.Index);

        public static PlayerCancelBoost playerCancelBoostAll = new PlayerCancelBoost(PlayerActionBase.Selector.All);

        public static PlayerLockBoostAll playerLockBoostAll = new PlayerLockBoostAll();

        public static PlayerEnableProperty playerEnableBoost = new PlayerEnableProperty(PlayerEnableProperty.Property.Boost, PlayerActionBase.Selector.Nearest);

        public static PlayerEnableProperty playerEnableBoostIndex = new PlayerEnableProperty(PlayerEnableProperty.Property.Boost, PlayerActionBase.Selector.Index);

        public static PlayerEnableProperty playerEnableBoostAll = new PlayerEnableProperty(PlayerEnableProperty.Property.Boost, PlayerActionBase.Selector.All);

        public static PlayerEnableProperty playerEnableMove = new PlayerEnableProperty(PlayerEnableProperty.Property.Move, PlayerActionBase.Selector.Nearest);

        public static PlayerEnableProperty playerEnableMoveIndex = new PlayerEnableProperty(PlayerEnableProperty.Property.Move, PlayerActionBase.Selector.Index);

        public static PlayerEnableProperty playerEnableMoveAll = new PlayerEnableProperty(PlayerEnableProperty.Property.Move, PlayerActionBase.Selector.All);

        public static SetGlobalPlayerSpeed setGlobalPlayerSpeed = new SetGlobalPlayerSpeed();

        public static SetPlayerVelocity setPlayerVelocity = new SetPlayerVelocity(-1, PlayerActionBase.Selector.Nearest);

        public static SetPlayerVelocity setPlayerVelocityIndex = new SetPlayerVelocity(-1, PlayerActionBase.Selector.Index);

        public static SetPlayerVelocity setPlayerVelocityAll = new SetPlayerVelocity(-1, PlayerActionBase.Selector.All);

        public static SetPlayerVelocity setPlayerVelocityX = new SetPlayerVelocity(0, PlayerActionBase.Selector.Nearest);

        public static SetPlayerVelocity setPlayerVelocityXIndex = new SetPlayerVelocity(0, PlayerActionBase.Selector.Index);

        public static SetPlayerVelocity setPlayerVelocityXAll = new SetPlayerVelocity(0, PlayerActionBase.Selector.All);

        public static SetPlayerVelocity setPlayerVelocityY = new SetPlayerVelocity(1, PlayerActionBase.Selector.Nearest);

        public static SetPlayerVelocity setPlayerVelocityYIndex = new SetPlayerVelocity(1, PlayerActionBase.Selector.Index);

        public static SetPlayerVelocity setPlayerVelocityYAll = new SetPlayerVelocity(1, PlayerActionBase.Selector.All);

        public static PlayerEnableProperty playerEnableDamage = new PlayerEnableProperty(PlayerEnableProperty.Property.Damage, PlayerActionBase.Selector.Nearest);

        public static PlayerEnableProperty playerEnableDamageIndex = new PlayerEnableProperty(PlayerEnableProperty.Property.Damage, PlayerActionBase.Selector.Index);

        public static PlayerEnableProperty playerEnableDamageAll = new PlayerEnableProperty(PlayerEnableProperty.Property.Damage, PlayerActionBase.Selector.All);

        public static PlayerEnableProperty playerEnableJump = new PlayerEnableProperty(PlayerEnableProperty.Property.Jump, PlayerActionBase.Selector.Nearest);

        public static PlayerEnableProperty playerEnableJumpIndex = new PlayerEnableProperty(PlayerEnableProperty.Property.Jump, PlayerActionBase.Selector.Index);

        public static PlayerEnableProperty playerEnableJumpAll = new PlayerEnableProperty(PlayerEnableProperty.Property.Jump, PlayerActionBase.Selector.All);

        public static PlayerEnableProperty playerEnableReversedJump = new PlayerEnableProperty(PlayerEnableProperty.Property.ReversedJump, PlayerActionBase.Selector.Nearest);

        public static PlayerEnableProperty playerEnableReversedJumpIndex = new PlayerEnableProperty(PlayerEnableProperty.Property.ReversedJump, PlayerActionBase.Selector.Index);

        public static PlayerEnableProperty playerEnableReversedJumpAll = new PlayerEnableProperty(PlayerEnableProperty.Property.ReversedJump, PlayerActionBase.Selector.All);

        public static PlayerEnableProperty playerEnableWallJump = new PlayerEnableProperty(PlayerEnableProperty.Property.WallJump, PlayerActionBase.Selector.Nearest);

        public static PlayerEnableProperty playerEnableWallJumpIndex = new PlayerEnableProperty(PlayerEnableProperty.Property.WallJump, PlayerActionBase.Selector.Index);

        public static PlayerEnableProperty playerEnableWallJumpAll = new PlayerEnableProperty(PlayerEnableProperty.Property.WallJump, PlayerActionBase.Selector.All);

        public static SetPlayerProperty setPlayerMoveSpeed = new SetPlayerProperty(SetPlayerProperty.Property.MoveSpeed, PlayerActionBase.Selector.Nearest);

        public static SetPlayerProperty setPlayerMoveSpeedIndex = new SetPlayerProperty(SetPlayerProperty.Property.MoveSpeed, PlayerActionBase.Selector.Index);

        public static SetPlayerProperty setPlayerMoveSpeedAll = new SetPlayerProperty(SetPlayerProperty.Property.MoveSpeed, PlayerActionBase.Selector.All);

        public static SetPlayerProperty setPlayerBoostSpeed = new SetPlayerProperty(SetPlayerProperty.Property.BoostSpeed, PlayerActionBase.Selector.Nearest);

        public static SetPlayerProperty setPlayerBoostSpeedIndex = new SetPlayerProperty(SetPlayerProperty.Property.BoostSpeed, PlayerActionBase.Selector.Index);

        public static SetPlayerProperty setPlayerBoostSpeedAll = new SetPlayerProperty(SetPlayerProperty.Property.BoostSpeed, PlayerActionBase.Selector.All);

        public static SetPlayerProperty setPlayerJumpGravity = new SetPlayerProperty(SetPlayerProperty.Property.JumpGravity, PlayerActionBase.Selector.Nearest);

        public static SetPlayerProperty setPlayerJumpGravityIndex = new SetPlayerProperty(SetPlayerProperty.Property.JumpGravity, PlayerActionBase.Selector.Index);

        public static SetPlayerProperty setPlayerJumpGravityAll = new SetPlayerProperty(SetPlayerProperty.Property.JumpGravity, PlayerActionBase.Selector.All);

        public static SetPlayerProperty setPlayerJumpIntensity = new SetPlayerProperty(SetPlayerProperty.Property.JumpIntensity, PlayerActionBase.Selector.Nearest);

        public static SetPlayerProperty setPlayerJumpIntensityIndex = new SetPlayerProperty(SetPlayerProperty.Property.JumpIntensity, PlayerActionBase.Selector.Index);

        public static SetPlayerProperty setPlayerJumpIntensityAll = new SetPlayerProperty(SetPlayerProperty.Property.JumpIntensity, PlayerActionBase.Selector.All);

        public static SetPlayerMask setPlayerMask = new SetPlayerMask(PlayerActionBase.Selector.Nearest);

        public static SetPlayerMask setPlayerMaskIndex = new SetPlayerMask(PlayerActionBase.Selector.Index);

        public static SetPlayerMask setPlayerMaskAll = new SetPlayerMask(PlayerActionBase.Selector.All);

        public static SetPlayerVariable setPlayerVariable = new SetPlayerVariable(PlayerActionBase.Selector.Nearest);

        public static SetPlayerVariable setPlayerVariableIndex = new SetPlayerVariable(PlayerActionBase.Selector.Index);

        public static SetPlayerVariable setPlayerVariableAll = new SetPlayerVariable(PlayerActionBase.Selector.All);
        
        public static RemovePlayerVariable removePlayerVariable = new RemovePlayerVariable(PlayerActionBase.Selector.Nearest);

        public static RemovePlayerVariable removePlayerVariableIndex = new RemovePlayerVariable(PlayerActionBase.Selector.Index);

        public static RemovePlayerVariable removePlayerVariableAll = new RemovePlayerVariable(PlayerActionBase.Selector.All);

        public static SetPlayerModel setPlayerModel = new SetPlayerModel();

        public static SetGameMode setGameMode = new SetGameMode();

        public static BlackHole blackHole = new BlackHole(false, PlayerActionBase.Selector.Nearest);

        public static BlackHole blackHoleIndex = new BlackHole(false, PlayerActionBase.Selector.Index);

        public static BlackHole blackHoleAll = new BlackHole(false, PlayerActionBase.Selector.All);

        public static BlackHole whiteHole = new BlackHole(true, PlayerActionBase.Selector.Nearest);

        public static BlackHole whiteHoleIndex = new BlackHole(true, PlayerActionBase.Selector.Index);

        public static BlackHole whiteHoleAll = new BlackHole(true, PlayerActionBase.Selector.All);

        #endregion

        #region Controls

        public static GetCurrentKey getCurrentKey = new GetCurrentKey();

        public static InputDetection keyPressDown = new InputDetection(InputDetection.DeviceType.Keyboard, InputDetection.PressType.Down);

        public static InputDetection keyPress = new InputDetection(InputDetection.DeviceType.Keyboard, InputDetection.PressType.Press);

        public static InputDetection keyPressUp = new InputDetection(InputDetection.DeviceType.Keyboard, InputDetection.PressType.Up);

        public static InputDetection controlPressDown = new InputDetection(InputDetection.DeviceType.Controller, InputDetection.PressType.Down);

        public static InputDetection controlPress = new InputDetection(InputDetection.DeviceType.Controller, InputDetection.PressType.Press);

        public static InputDetection controlPressUp = new InputDetection(InputDetection.DeviceType.Controller, InputDetection.PressType.Up);

        public static InputDetection mouseButtonDown = new InputDetection(InputDetection.DeviceType.Mouse, InputDetection.PressType.Down);

        public static InputDetection mouseButton = new InputDetection(InputDetection.DeviceType.Mouse, InputDetection.PressType.Press);

        public static InputDetection mouseButtonUp = new InputDetection(InputDetection.DeviceType.Mouse, InputDetection.PressType.Up);

        public static MouseOver mouseOver = new MouseOver();

        public static ShowMouse showMouse = new ShowMouse();

        public static SetMousePosition setMousePosition = new SetMousePosition();

        public static FollowMousePosition followMousePosition = new FollowMousePosition();

        #endregion

        #region Component

        public static ParticleSystemModifier particleSystem = new ParticleSystemModifier(false);

        public static ParticleSystemModifier particleSystemHex = new ParticleSystemModifier(true);

        public static TrailRendererModifier trailRenderer = new TrailRendererModifier(false);

        public static TrailRendererModifier trailRendererHex = new TrailRendererModifier(true);

        public static RigidbodyModifier rigidbody = new RigidbodyModifier(false);

        public static RigidbodyModifier rigidbodyOther = new RigidbodyModifier(true);

        #endregion

        #region Rendering

        public static Blur blur = new Blur(Blur.Type.None, false);

        public static Blur blurOther = new Blur(Blur.Type.None, true);

        public static Blur blurVariable = new Blur(Blur.Type.Variable, false);

        public static Blur blurVariableOther = new Blur(Blur.Type.Variable, true);

        public static Blur blurColored = new Blur(Blur.Type.Colored, false);

        public static Blur blurColoredOther = new Blur(Blur.Type.Colored, true);

        public static DoubleSided doubleSided = new DoubleSided();

        public static SetRenderType setRenderType = new SetRenderType(false);

        public static SetRenderType setRenderTypeOther = new SetRenderType(true);

        public static SetRendering setRendering = new SetRendering();

        public static SetOutline setOutline = new SetOutline(false, false);

        public static SetOutline setOutlineOther = new SetOutline(false, true);

        public static SetOutline setOutlineHex = new SetOutline(true, false);

        public static SetOutline setOutlineHexOther = new SetOutline(true, true);

        public static SetDepthOffset setDepthOffset = new SetDepthOffset();

        public static SetMask setMask = new SetMask(false);

        public static SetMask setMaskOther = new SetMask(true);

        public static ActorFrameTexture actorFrameTexture = new ActorFrameTexture();

        #endregion

        #region Enable

        public static ObjectActive objectActive = new ObjectActive();

        public static ObjectCustomActive objectCustomActive = new ObjectCustomActive();

        public static ObjectActiveOther objectActiveOther = new ObjectActiveOther();

        public static ObjectSpawned objectSpawned = new ObjectSpawned();

        public static EnableObject enableObject = new EnableObject(false, false);

        public static EnableObject enableObjectTree = new EnableObject(true, false);

        public static EnableObject enableObjectOther = new EnableObject(false, true);

        public static EnableObject enableObjectTreeOther = new EnableObject(true, true);

        public static EnableObjectGroup enableObjectGroup = new EnableObjectGroup();

        #endregion

        #region Color

        public static GetColor getColor = new GetColor();

        public static GetModifiedColor getModifiedColor = new GetModifiedColor();

        public static GetMixedColors getMixedColors = new GetMixedColors();

        public static GetLerpColor getLerpColor = new GetLerpColor(false);

        public static GetLerpColor getAddColor = new GetLerpColor(true);

        public static GetVisualColor getVisualColor = new GetVisualColor(false, false);

        public static GetVisualColor getVisualColorOther = new GetVisualColor(true, false);

        public static GetVisualColor getVisualColorRGBA = new GetVisualColor(false, true);

        public static GetVisualColor getVisualColorRGBAOther = new GetVisualColor(true, true);

        public static GetVisualOpacity getVisualOpacity = new GetVisualOpacity(false);

        public static GetVisualOpacity getVisualOpacityOther = new GetVisualOpacity(true);

        public static GetColorSlotHexCode getColorSlotHexCode = new GetColorSlotHexCode();

        public static GetFloatFromHexCode getFloatFromHexCode = new GetFloatFromHexCode();

        public static GetHexCodeFromFloat getHexCodeFromFloat = new GetHexCodeFromFloat();

        public static SetTheme setTheme = new SetTheme();

        public static LerpTheme lerpTheme = new LerpTheme();

        public static ColorModifier addColor = new ColorModifier(ColorModifier.MixType.Add, false, false);

        public static ColorModifier addColorOther = new ColorModifier(ColorModifier.MixType.Add, true, false);

        public static ColorModifier lerpColor = new ColorModifier(ColorModifier.MixType.Lerp, false, false);

        public static ColorModifier lerpColorOther = new ColorModifier(ColorModifier.MixType.Lerp, true, false);

        public static ColorModifier addColorPlayerDistance = new ColorModifier(ColorModifier.MixType.Add, false, true);

        public static ColorModifier lerpColorPlayerDistance = new ColorModifier(ColorModifier.MixType.Lerp, false, true);

        public static SetOpacity setOpacity = new SetOpacity(false);

        public static SetOpacity setOpacityOther = new SetOpacity(true);

        public static ModifyColorHSV modifyColorHSV = new ModifyColorHSV(false);

        public static ModifyColorHSV modifyColorHSVOther = new ModifyColorHSV(true);

        public static CopyColor copyColor = new CopyColor(false);

        public static CopyColor copyColorOther = new CopyColor(true);

        public static ApplyColorGroup applyColorGroup = new ApplyColorGroup();

        public static SetColor setColorHex = new SetColor(SetColor.Mode.Hex, false);

        public static SetColor setColorHexOther = new SetColor(SetColor.Mode.Hex, true);

        public static SetColor setColorRGBA = new SetColor(SetColor.Mode.RGBA, false);

        public static SetColor setColorRGBAOther = new SetColor(SetColor.Mode.RGBA, true);

        public static AnimateColorKF animateColorKF = new AnimateColorKF(AnimateColorKF.Mode.Slot);

        public static AnimateColorKF animateColorKFHex = new AnimateColorKF(AnimateColorKF.Mode.Hex);

        #endregion

        #region Shape

        public static SetShape setShape = new SetShape();

        public static SetPolygonShape setPolygonShape = new SetPolygonShape(false);

        public static SetPolygonShape setPolygonShapeOther = new SetPolygonShape(true);

        public static TranslateShape translateShape = new TranslateShape(false);

        public static TranslateShape translateShape3D = new TranslateShape(true);

        public static BackgroundShape backgroundShape = new BackgroundShape();

        public static SphereShape sphereShape = new SphereShape();

        public static CustomMesh customMesh = new CustomMesh();

        public static GetText getText = new GetText(false);

        public static GetText getTextOther = new GetText(true);

        public static TextModifier setText = new TextModifier(TextModifier.Operation.Set, false);

        public static TextModifier setTextOther = new TextModifier(TextModifier.Operation.Set, true);

        public static TextModifier addText = new TextModifier(TextModifier.Operation.Add, false);

        public static TextModifier addTextOther = new TextModifier(TextModifier.Operation.Add, true);

        public static TextModifier removeText = new TextModifier(TextModifier.Operation.Remove, false);

        public static TextModifier removeTextOther = new TextModifier(TextModifier.Operation.Remove, true);

        public static TextModifier removeAtText = new TextModifier(TextModifier.Operation.RemoveAt, false);

        public static TextModifier removeAtTextOther = new TextModifier(TextModifier.Operation.RemoveAt, true);

        public static TextModifier replaceText = new TextModifier(TextModifier.Operation.Replace, false);

        public static TextModifier replaceTextOther = new TextModifier(TextModifier.Operation.Replace, true);

        public static FormatText formatText = new FormatText();

        public static TextSequence textSequence = new TextSequence();

        public static SetImage setImage = new SetImage(false);

        public static SetImage setImageOther = new SetImage(true);

        #endregion

        #region Animation

        public static GetEasing getEasing = new GetEasing(false);

        public static GetEasing getEasingName = new GetEasing(true);

        public static GetAxis getAxis = new GetAxis(false);

        public static GetAxis getAxisMath = new GetAxis(true);

        public static GetKeyframeValue getKeyframeValue = new GetKeyframeValue();

        public static GetAnimateVariable getAnimateVariable = new GetAnimateVariable(false);

        public static GetAnimateVariable getAnimateVariableMath = new GetAnimateVariable(true);

        public static AxisCompare axisEquals = new AxisCompare(NumberComparison.Equals);

        public static AxisCompare axisLesserEquals = new AxisCompare(NumberComparison.LesserEquals);

        public static AxisCompare axisGreaterEquals = new AxisCompare(NumberComparison.GreaterEquals);

        public static AxisCompare axisLesser = new AxisCompare(NumberComparison.Lesser);

        public static AxisCompare axisGreater = new AxisCompare(NumberComparison.Greater);

        public static AnimateObject animateObject = new AnimateObject(false, false, false);

        public static AnimateObject animateObjectOther = new AnimateObject(false, false, true);

        public static AnimateObject animateSignal = new AnimateObject(true, false, false);

        public static AnimateObject animateSignalOther = new AnimateObject(true, false, true);

        public static AnimateObject animateObjectMath = new AnimateObject(false, true, false);

        public static AnimateObject animateObjectMathOther = new AnimateObject(false, true, true);

        public static AnimateObject animateSignalMath = new AnimateObject(true, true, false);

        public static AnimateObject animateSignalMathOther = new AnimateObject(true, true, true);

        public static ApplyAnimation applyAnimation = new ApplyAnimation(ApplyAnimation.Type.Both, false);

        public static ApplyAnimation applyAnimationFrom = new ApplyAnimation(ApplyAnimation.Type.From, false);

        public static ApplyAnimation applyAnimationTo = new ApplyAnimation(ApplyAnimation.Type.To, false);

        public static ApplyAnimation applyAnimationMath = new ApplyAnimation(ApplyAnimation.Type.Both, true);

        public static ApplyAnimation applyAnimationFromMath = new ApplyAnimation(ApplyAnimation.Type.From, true);

        public static ApplyAnimation applyAnimationToMath = new ApplyAnimation(ApplyAnimation.Type.To, true);

        public static CopyAxis copyAxis = new CopyAxis(CopyAxis.Type.Normal);

        public static CopyAxis copyAxisMath = new CopyAxis(CopyAxis.Type.Math);

        public static CopyAxis copyAxisGroup = new CopyAxis(CopyAxis.Type.Group);

        public static CopyAxis copyAxisChain = new CopyAxis(CopyAxis.Type.Chain);

        public static RunAnimation runAnimation = new RunAnimation();

        public static CopyPlayerAxis copyPlayerAxis = new CopyPlayerAxis();

        public static SetOffsetOperation setOffsetOperation = new SetOffsetOperation();

        public static LegacyTail legacyTail = new LegacyTail();

        public static InverseKinematicsModifier inverseKinematics = new InverseKinematicsModifier();

        public static Gravity gravity = new Gravity(false);

        public static Gravity gravityOther = new Gravity(true);

        #endregion

        #region Prefab

        public static SpawnPrefab spawnPrefab = new SpawnPrefab(false, false, false, false);

        public static SpawnPrefab spawnPrefabOffset = new SpawnPrefab(true, false, false, false);

        public static SpawnPrefab spawnPrefabOffsetOther = new SpawnPrefab(true, true, false, false);

        public static SpawnPrefab spawnPrefabCopy = new SpawnPrefab(false, false, false, true);

        public static SpawnPrefab spawnMultiPrefab = new SpawnPrefab(false, false, true, false);

        public static SpawnPrefab spawnMultiPrefabOffset = new SpawnPrefab(true, false, true, false);

        public static SpawnPrefab spawnMultiPrefabOffsetOther = new SpawnPrefab(true, true, true, false);

        public static SpawnPrefab spawnMultiPrefabCopy = new SpawnPrefab(false, false, true, true);

        public static ClearSpawnedPrefabs clearSpawnedPrefabs = new ClearSpawnedPrefabs();

        public static SetPrefabTime setPrefabTime = new SetPrefabTime();

        public static EnablePrefab enablePrefab = new EnablePrefab();

        public static UpdatePrefab updatePrefab = new UpdatePrefab();

        public static SpawnClone spawnClone = new SpawnClone(false);

        public static SpawnClone spawnCloneMath = new SpawnClone(true);

        public static FromPrefab fromPrefab = new FromPrefab();

        #endregion

        #region Runtime

        public static GetRuntimeVariable getRuntimeVariable = new GetRuntimeVariable();

        public static SetRuntimeVariable setRuntimeVariable = new SetRuntimeVariable();

        public static ReinitLevel reinitLevel = new ReinitLevel();

        public static UpdateObject updateObject = new UpdateObject(false);

        public static UpdateObject updateObjectOther = new UpdateObject(true);

        public static SetParent setParent = new SetParent(false);

        public static SetParent setParentOther = new SetParent(true);

        public static DetachParent detachParent = new DetachParent(false);

        public static DetachParent detachParentOther = new DetachParent(true);

        public static SetSeed setSeed = new SetSeed();

        public static SetAudio setAudio = new SetAudio();

        public static SetGameData setGameData = new SetGameData();

        #endregion

        #region Physics

        public static SetCollision setCollision = new SetCollision(false);

        public static SetCollision setCollisionOther = new SetCollision(true);

        public static ForceCollision forceCollision = new ForceCollision(false);

        public static ForceCollision forceCollisionOther = new ForceCollision(true);

        public static BulletCollide bulletCollide = new BulletCollide();

        public static ObjectCollide objectCollide = new ObjectCollide();

        #endregion

        #region Checkpoints

        public static GetCheckpointIndex getActiveCheckpointIndex = new GetCheckpointIndex(GetCheckpointIndex.Type.Active);

        public static GetCheckpointIndex getLastCheckpointIndex = new GetCheckpointIndex(GetCheckpointIndex.Type.Last);

        public static GetCheckpointIndex getNextCheckpointIndex = new GetCheckpointIndex(GetCheckpointIndex.Type.Next);

        public static GetMarkerIndex getLastMarkerIndex = new GetMarkerIndex(GetMarkerIndex.Type.Last);

        public static GetMarkerIndex getNextMarkerIndex = new GetMarkerIndex(GetMarkerIndex.Type.Next);

        public static GetCheckpointCount getCheckpointCount = new GetCheckpointCount();

        public static GetMarkerCount getMarkerCount = new GetMarkerCount();

        public static GetCheckpointTime getCheckpointTime = new GetCheckpointTime();

        public static GetMarkerTime getMarkerTime = new GetMarkerTime();

        public static CreateCheckpoint createCheckpoint = new CreateCheckpoint();

        public static ResetCheckpoint resetCheckpoint = new ResetCheckpoint();

        public static SetCurrentCheckpoint setCurrentCheckpoint = new SetCurrentCheckpoint();

        public static OnMarker onMarker = new OnMarker();

        public static OnCheckpoint onCheckpoint = new OnCheckpoint();

        #endregion

        #region Interfaces

        public static LoadInterface loadInterface = new LoadInterface();

        public static ExitInterface exitInterface = new ExitInterface();

        public static PauseLevel pauseLevel = new PauseLevel();

        public static QuitToMenu quitToMenu = new QuitToMenu();

        public static QuitToArcade quitToArcade = new QuitToArcade();

        #endregion

        #region JSON

        public static GetJSON getJSONString = new GetJSON(GetJSON.Type.String);

        public static GetJSON getJSONFloat = new GetJSON(GetJSON.Type.Float);

        public static GetJSON getJSON = new GetJSON(GetJSON.Type.Object);

        public static LoadJSONCompare loadJSONEquals = new LoadJSONCompare(NumberComparison.Equals);

        public static LoadJSONCompare loadJSONLesserEquals = new LoadJSONCompare(NumberComparison.LesserEquals);

        public static LoadJSONCompare loadJSONGreaterEquals = new LoadJSONCompare(NumberComparison.GreaterEquals);

        public static LoadJSONCompare loadJSONLesser = new LoadJSONCompare(NumberComparison.Lesser);

        public static LoadJSONCompare loadJSONGreater = new LoadJSONCompare(NumberComparison.Greater);

        public static LoadJSONExists loadJSONExists = new LoadJSONExists();

        public static SaveJSON saveJSON = new SaveJSON();

        #endregion

        #region Application

        public static RealTimeCompare realTimeEquals = new RealTimeCompare(NumberComparison.Equals);

        public static RealTimeCompare realTimeLesserEquals = new RealTimeCompare(NumberComparison.LesserEquals);

        public static RealTimeCompare realTimeGreaterEquals = new RealTimeCompare(NumberComparison.GreaterEquals);

        public static RealTimeCompare realTimeLesser = new RealTimeCompare(NumberComparison.Lesser);

        public static RealTimeCompare realTimeGreater = new RealTimeCompare(NumberComparison.Greater);

        public static UsernameEquals usernameEquals = new UsernameEquals();

        public static LanguageEquals languageEquals = new LanguageEquals();

        public static ConfigSetting configLDM = new ConfigSetting(ConfigSetting.Setting.LDM);

        public static ConfigSetting configShowEffects = new ConfigSetting(ConfigSetting.Setting.ShowEffects);

        public static ConfigSetting configShowPlayerGUI = new ConfigSetting(ConfigSetting.Setting.ShowPlayerGUI);

        public static ConfigSetting configShowIntro = new ConfigSetting(ConfigSetting.Setting.ShowIntro);

        public static IsFocused isFocused = new IsFocused();

        public static IsFullscreen isFullscreen = new IsFullscreen();

        public static SetWindowTitle setWindowTitle = new SetWindowTitle(false);

        public static SetWindowTitle resetWindowTitle = new SetWindowTitle(true);

        public static SetDiscordStatus setDiscordStatus = new SetDiscordStatus(false);

        public static SetDiscordStatus resetDiscordStatus = new SetDiscordStatus(true);

        #endregion

        #region Player Only

        public static SetCustomObjectActive setCustomObjectActive = new SetCustomObjectActive();

        public static SetCustomObjectIdle setCustomObjectIdle = new SetCustomObjectIdle();

        public static SetIdleAnimation setIdleAnimation = new SetIdleAnimation();

        public static PlayAnimation playAnimation = new PlayAnimation();

        public static PlayerFunction kill = new PlayerFunction(PlayerFunction.Type.Kill);

        public static PlayerFunction hit = new PlayerFunction(PlayerFunction.Type.Hit);

        public static PlayerFunction boost = new PlayerFunction(PlayerFunction.Type.Boost);

        public static PlayerFunction shoot = new PlayerFunction(PlayerFunction.Type.Shoot);

        public static PlayerFunction pulse = new PlayerFunction(PlayerFunction.Type.Pulse);

        public static PlayerFunction jump = new PlayerFunction(PlayerFunction.Type.Jump);

        public static GetPlayerPropertyInstance getHealth = new GetPlayerPropertyInstance(GetPlayerPropertyInstance.Property.Health);

        public static GetPlayerPropertyInstance getLives = new GetPlayerPropertyInstance(GetPlayerPropertyInstance.Property.Lives);

        public static GetPlayerPropertyInstance getMaxHealth = new GetPlayerPropertyInstance(GetPlayerPropertyInstance.Property.MaxHealth);

        public static GetPlayerPropertyInstance getMaxLives = new GetPlayerPropertyInstance(GetPlayerPropertyInstance.Property.MaxLives);

        public static GetPlayerPropertyInstance getIndex = new GetPlayerPropertyInstance(GetPlayerPropertyInstance.Property.Index);

        public static GetPlayerPropertyInstance getMove = new GetPlayerPropertyInstance(GetPlayerPropertyInstance.Property.Move);

        public static GetPlayerPropertyInstance getMoveX = new GetPlayerPropertyInstance(GetPlayerPropertyInstance.Property.MoveX);

        public static GetPlayerPropertyInstance getMoveY = new GetPlayerPropertyInstance(GetPlayerPropertyInstance.Property.MoveY);

        public static GetPlayerPropertyInstance getLook = new GetPlayerPropertyInstance(GetPlayerPropertyInstance.Property.Look);

        public static GetPlayerPropertyInstance getLookX = new GetPlayerPropertyInstance(GetPlayerPropertyInstance.Property.LookX);

        public static GetPlayerPropertyInstance getLookY = new GetPlayerPropertyInstance(GetPlayerPropertyInstance.Property.LookY);

        #endregion

        #region DEVONLY

        public static LoadScene loadSceneDEVONLY = new LoadScene();

        public static LoadStoryLevel loadStoryLevelDEVONLY = new LoadStoryLevel();

        public static StorySavePropertyCompare storyLoadBoolDEVONLY = new StorySavePropertyCompare(StorySavePropertyCompare.Property.Bool, NumberComparison.Equals);

        public static StorySavePropertyCompare storyLoadIntEqualsDEVONLY = new StorySavePropertyCompare(StorySavePropertyCompare.Property.Int, NumberComparison.Equals);
        
        public static StorySavePropertyCompare storyLoadIntLesserEqualsDEVONLY = new StorySavePropertyCompare(StorySavePropertyCompare.Property.Int, NumberComparison.LesserEquals);
        
        public static StorySavePropertyCompare storyLoadIntGreaterEqualsDEVONLY = new StorySavePropertyCompare(StorySavePropertyCompare.Property.Int, NumberComparison.GreaterEquals);
        
        public static StorySavePropertyCompare storyLoadIntLesserDEVONLY = new StorySavePropertyCompare(StorySavePropertyCompare.Property.Int, NumberComparison.Lesser);
        
        public static StorySavePropertyCompare storyLoadIntGreaterDEVONLY = new StorySavePropertyCompare(StorySavePropertyCompare.Property.Int, NumberComparison.Greater);

        public static StorySaveProperty storySaveBoolDEVONLY = new StorySaveProperty(StorySaveProperty.Property.Bool);

        public static StorySaveProperty storySaveIntDEVONLY = new StorySaveProperty(StorySaveProperty.Property.Int);

        public static StorySaveProperty storySaveFloatDEVONLY = new StorySaveProperty(StorySaveProperty.Property.Float);

        public static StorySaveProperty storySaveStringDEVONLY = new StorySaveProperty(StorySaveProperty.Property.String);

        public static StorySaveProperty storySaveIntVariableDEVONLY = new StorySaveProperty(StorySaveProperty.Property.IntVariable);

        public static GetStorySaveProperty getStorySaveBoolDEVONLY = new GetStorySaveProperty(GetStorySaveProperty.Property.Bool);

        public static GetStorySaveProperty getStorySaveIntDEVONLY = new GetStorySaveProperty(GetStorySaveProperty.Property.Int);

        public static GetStorySaveProperty getStorySaveFloatDEVONLY = new GetStorySaveProperty(GetStorySaveProperty.Property.Float);

        public static GetStorySaveProperty getStorySaveStringDEVONLY = new GetStorySaveProperty(GetStorySaveProperty.Property.String);

        public static ExampleEnable exampleEnableDEVONLY = new ExampleEnable();

        public static ExampleSay exampleSayDEVONLY = new ExampleSay();

        #endregion
    }
}
