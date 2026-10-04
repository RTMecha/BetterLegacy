using System.Collections;
using System.Collections.Generic;
using System.Linq;

using SteamworksFacepunch;
using SteamworksFacepunch.Data;

using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers.Settings;
using BetterLegacy.Editor.Managers;
using BetterLegacy.Menus.UI.Popups;

namespace BetterLegacy.Core.Managers
{
    /// <summary>
    /// Manages online lobbies.
    /// </summary>
    public class SteamLobbyManager : BaseManager<SteamLobbyManager, SteamLobbyManagerSettings>
    {
        #region Values

        /// <summary>
        /// The current online lobby.
        /// </summary>
        public Lobby CurrentLobby { get; set; }

        /// <summary>
        /// Current lobby settings.
        /// </summary>
        public LobbySettings LobbySettings { get; set; } = new LobbySettings();

        /// <summary>
        /// The current lobby channel.
        /// </summary>
        public string LobbyChannel { get; set; } = string.Empty;

        Dictionary<SteamId, bool> loadedPlayers = new Dictionary<SteamId, bool>();

        /// <summary>
        /// Scene loaded state.
        /// </summary>
        public const string SCENE_LOADED = "SceneLoaded";

        /// <summary>
        /// <see cref="UnityEngine.AudioClip"/> loaded state.
        /// </summary>
        public const string SONG_LOADED = "SongLoaded";

        /// <summary>
        /// <see cref="GameData"/> loaded state.
        /// </summary>
        public const string GAME_DATA_LOADED = "GameDataLoaded";

        /// <summary>
        /// Is loaded state.
        /// </summary>
        public const string IS_LOADED = "IsLoaded";

        /// <summary>
        /// If the scene has loaded.
        /// </summary>
        public bool sceneLoaded;

        /// <summary>
        /// If the current <see cref="UnityEngine.AudioClip"/> has loaded.
        /// </summary>
        public bool songLoaded;

        /// <summary>
        /// If the <see cref="GameData"/> has loaded.
        /// </summary>
        public bool gameDataLoaded;

        /// <summary>
        /// If the user has loaded the scene, song and gamedata.
        /// </summary>
        public bool AllLoaded => sceneLoaded && songLoaded && gameDataLoaded;

        #region Password

        /// <summary>
        /// Auth state.
        /// </summary>
        public const string AUTH_MARKER = "BLAUTH";

        /// <summary>
        /// Lobby has password state.
        /// </summary>
        public const string HAS_PASSWORD = "HasPassword";

        readonly HashSet<ulong> authorizedMembers = new HashSet<ulong>();
        string hostPassword;
        string joinPassword;
        bool awaitingAuth;

        /// <summary>
        /// If the host's lobby set a password.
        /// </summary>
        public bool HostHasPassword => ProjectArrhythmia.State.IsHosting && !string.IsNullOrEmpty(hostPassword);

        /// <summary>
        /// If a member is authorized.
        /// </summary>
        /// <param name="id">Identification of the member.</param>
        /// <returns>Returns <see langword="true"/> if there is no password or the user has correctly authenticated, otherwise returns <see langword="false"/>.</returns>
        public bool IsMemberAuthorized(ulong id) => !HostHasPassword || authorizedMembers.Contains(id);

        /// <summary>
        /// If a network ID is authorized.
        /// </summary>
        /// <param name="netId">Network identification.</param>
        /// <returns>Returns <see langword="true"/> if there is no password or the user has correctly authenticated, otherwise returns <see langword="false"/>.</returns>
        public bool IsNetAuthorized(int netId) =>
            !HostHasPassword || netId == 0 ||
            (Transport.Instance && Transport.Instance.steamIDToNetID.Any(pair => pair.Value == netId && authorizedMembers.Contains(pair.Key)));

        static string AuthHash(string password, ulong steamId, ulong lobbyId)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
                return System.BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes($"{password}\n{steamId}\n{lobbyId}"))).Replace("-", string.Empty);
        }

        #endregion

        #region Chat

        /// <summary>
        /// Chat marker.
        /// </summary>
        public const string CHAT_MARKER = "BLCHAT";

        const char CHAT_DELIMITER = '\u001F';

        /// <summary>
        /// Chat history marker.
        /// </summary>
        public const string HISTORY_MARKER = "BLHIST";

        const char HISTORY_RECORD_DELIMITER = '';
        const int HISTORY_MAX_MESSAGES = 200;
        const int HISTORY_MAX_CHUNK_LENGTH = 3000;

        #endregion

        #endregion

        #region Functions

        public override void OnInit()
        {
            Log($"Setting lobby events");
            SteamMatchmaking.OnLobbyCreated += OnLobbyCreated;
            SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;

            SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
            SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberDisconnected;
            SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberDisconnected;

            SteamMatchmaking.OnLobbyMemberDataChanged += OnLobbyMemberDataChanged;
            SteamMatchmaking.OnLobbyDataChanged += OnLobbyDataChanged;

            SteamMatchmaking.OnChatMessage += OnChatMessage;

            LoadLobbySettings();
        }

        public override void OnTick()
        {
            if (GameData.Current && ProjectArrhythmia.State.InGame && ProjectArrhythmia.State.IsHosting && TickCount % 100 == 0)
                NetworkFunction.SyncLevelToClients();

            if (ProjectArrhythmia.State.IsInLobby && ProjectArrhythmia.State.InEditor)
            {
                if (TickCount % 4 == 0)
                    EditorMultiplayer.BroadcastLocalPlayhead();

                if (EditorMultiplayer.selectionDirty)
                {
                    EditorMultiplayer.BroadcastLocalSelection();
                    EditorMultiplayer.selectionDirty = false;
                }
                if (EditorMultiplayer.metadataDirty)
                {
                    EditorMultiplayer.BroadcastMetaData();
                    EditorMultiplayer.metadataDirty = false;
                }
                if (ProjectArrhythmia.State.IsHosting && GameData.Current && TickCount % 200 == 0)
                {
                    var joinedIDs = string.Join("\n", GameData.Current.beatmapObjects.FindAll(x => !x.fromPrefab).Select(x => x.id));
                    NetworkFunction.ReconcileObjects(joinedIDs);

                    var joinedPrefabIDs = string.Join("\n", GameData.Current.prefabObjects.Select(x => x.id));
                    NetworkFunction.ReconcilePrefabs(joinedPrefabIDs);
                }
            }
        }

        #region Settings

        /// <summary>
        /// Saves the lobby settings.
        /// </summary>
        public void SaveLobbySettings()
        {
            LobbySettings.WriteToFile(RTFile.CombinePaths(RTFile.ApplicationDirectory, "settings", LobbySettings.GetFileName()));
            Log("Saved lobby settings!");
        }

        /// <summary>
        /// Loads the lobby settings.
        /// </summary>
        public void LoadLobbySettings()
        {
            var path = RTFile.CombinePaths(RTFile.ApplicationDirectory, "settings", LobbySettings.MAIN_FILE_NAME);
            if (!RTFile.FileExists(path))
                return;
            LobbySettings = RTFile.CreateFromFile<LobbySettings>(path);
            LobbyPopup.Instance.nameField?.SetTextWithoutNotify(LobbySettings.Name);
            LobbyPopup.Instance.passwordField?.SetTextWithoutNotify(LobbySettings.Password);
            LobbyPopup.Instance.playerCountField?.SetTextWithoutNotify(LobbySettings.PlayerCount.ToString());
            LobbyPopup.Instance.visibilityDropdown?.SetValueWithoutNotify((int)LobbySettings.Visibility);
            Log("Loaded lobby settings!");
        }

        #endregion

        #region Sync Players

        /// <summary>
        /// Syncs local players to the host.
        /// </summary>
        public void SyncPlayersToServer()
        {
            PlayerManager.inst.localPlayers = PlayerManager.inst.players.FindAll(x => x.IsLocalPlayer);
            PlayerManager.inst.SetLocalIndexes();
            NetworkManager.inst.RunFunction(NetworkFunction.Group.Player, NetworkFunction.SEND_SERVER_PLAYER_DATA, new PacketList<PAPlayer>(PlayerManager.inst.players));
        }

        /// <summary>
        /// Syncs host players to all clients.
        /// </summary>
        public void SyncPlayersToClients()
        {
            NetworkFunction.SendHostLobbySettings();
            NetworkManager.inst.RunFunction(NetworkFunction.Group.Player, NetworkFunction.SEND_CLIENT_PLAYER_DATA, new PacketList<PAPlayer>(PlayerManager.inst.players));
        }

        #endregion

        /// <summary>
        /// Deletes the temporary lobby level cache.
        /// </summary>
        public void DeleteLobbyLevelCache() => RTFile.DeleteDirectory(RTFile.CombinePaths(RTFile.ApplicationDirectory, "beatmaps/temp/lobby_level"));

        #region Lobby

        /// <summary>
        /// Creates a lobby.
        /// </summary>
        public void CreateLobby()
        {
            if (ProjectArrhythmia.State.IsInLobby)
            {
                LogError($"Cannot create a lobby because you're already in a lobby.");
                return;
            }
            if (PlayerManager.inst.NoPlayers)
                PlayerManager.inst.ValidatePlayers();
            if (string.IsNullOrEmpty(LobbySettings.Name))
            {
                LogError($"Cannot create a lobby with an empty name!");
                return;
            }

            Log($"Creating a lobby");
            hostPassword = LobbySettings.Password;
            authorizedMembers.Clear();
            ProjectArrhythmia.State.IsHosting = true;
            RTSteamManager.inst.StartServer();
            SteamMatchmaking.CreateLobbyAsync(LobbySettings.PlayerCount);
        }

        /// <summary>
        /// Finds a random lobby and joins it.
        /// </summary>
        public async void JoinRandomLobby()
        {
            if (ProjectArrhythmia.State.IsInLobby)
            {
                LogError($"Cannot join a lobby because you're already in a lobby.");
                return;
            }

            var lobbies = await new LobbyQuery().WithMaxResults(10).RequestAsync();
            LegacyPlugin.MainTick += () =>
            {
                if (lobbies == null)
                {
                    LogError($"No lobbies available.");
                    return;
                }
                Log($"Found lobbies: {lobbies.Length}");
                if (lobbies.IsEmpty())
                    return;
                var lobbyQueue = new List<Lobby>();
                for (int i = 0; i < lobbies.Length; i++)
                {
                    var lobby = lobbies[i];
                    Log($"Lobby {i}\n" +
                        $"ID: {lobby.Id}\n" +
                        $"Owner: {lobby.Owner.Name} - {lobby.Owner.Id}");
                    if (IsValidLobby(lobby) && lobby.GetData(HAS_PASSWORD) != "1")
                        lobbyQueue.Add(lobby);
                }

                if (!lobbyQueue.IsEmpty())
                    JoinLobby(lobbyQueue[UnityRandom.Range(0, lobbyQueue.Count)]);
                else
                    LogError($"No lobbies available.");
            };
        }

        /// <summary>
        /// Joins a specific lobby.
        /// </summary>
        /// <param name="id">Lobby ID.</param>
        public async void JoinLobby(SteamId id)
        {
            if (ProjectArrhythmia.State.IsInLobby)
            {
                LogError($"Cannot join a lobby because you're already in a lobby.");
                return;
            }

            var Qlobby = await SteamMatchmaking.JoinLobbyAsync(id);
            if (!Qlobby.TryGetValue(out Lobby lobby))
                return;

            CurrentLobby = lobby;
            RTSteamManager.inst.StartClient(lobby.Owner.Id);
        }

        /// <summary>
        /// Joins a specific lobby.
        /// </summary>
        /// <param name="lobby">Lobby reference.</param>
        public void JoinLobby(Lobby lobby, string password = null)
        {
            joinPassword = password;
            if (PlayerManager.inst.NoPlayers)
                SceneHelper.LoadInputSelect(() => CoroutineHelper.StartCoroutine(IJoinLobby(lobby)));
            else
                CoroutineHelper.StartCoroutine(IJoinLobby(lobby));
        }

        IEnumerator IJoinLobby(Lobby lobby)
        {
            if (ProjectArrhythmia.State.IsInLobby)
            {
                LogError($"Cannot join a lobby because you're already in a lobby.");
                yield break;
            }

            CurrentLobby = lobby;
            Log($"Joining lobby... [{lobby.Id}]");
            yield return CoroutineHelper.StartCoroutine(lobby.Join());
            Log($"Joined lobby! [{lobby.Id}]");
            RTSteamManager.inst.StartClient(lobby.Owner.Id);
            LobbyPopup.Instance?.OnLobbyJoined();
        }

        /// <summary>
        /// Leaves the current lobby.
        /// </summary>
        public void LeaveLobby()
        {
            ProjectArrhythmia.State.IsInLobby = false;
            authorizedMembers.Clear();
            hostPassword = null;
            awaitingAuth = false;
            CurrentLobby.Leave();
            LobbyPopup.Instance?.ClearChat();
            ClearLoaded();
            ClearInputReady();
            Editor.Managers.EditorMultiplayer.Clear();
        }

        /// <summary>
        /// Sends a chat to the current lobby.
        /// </summary>
        /// <param name="message">Message to send.</param>
        public void SendChat(string message) => CurrentLobby.SendChatString(message);

        public void SendChatMessage(string text, ChatMessageKind kind = ChatMessageKind.Player, string referenceLevelPath = null, ReferenceKind referenceKind = ReferenceKind.None, string referenceIds = null)
        {
            if (string.IsNullOrEmpty(text) || !ProjectArrhythmia.State.IsInLobby)
                return;
            var name = (CoreConfig.Instance.DisplayName.Value ?? "Player").Replace(CHAT_DELIMITER.ToString(), string.Empty);
            var colorHex = RTColors.ColorToHex(EditorConfig.Instance.TimelineCursorColor.Value);
            var ticks = System.DateTime.UtcNow.Ticks;
            var payload = string.Join(CHAT_DELIMITER.ToString(), CHAT_MARKER, name, colorHex, ticks.ToString(), (int) kind, referenceLevelPath, (int) referenceKind, referenceIds, System.Guid.NewGuid().ToString("N"), text);
            SendChat(payload);
            if (kind == ChatMessageKind.System && (referenceKind != ReferenceKind.None || !string.IsNullOrEmpty(referenceLevelPath)) && LobbyPopup.Instance)
            {
                LobbyPopup.Instance.Open();
                LobbyPopup.Instance.SetTab(LobbyPopup.LobbyTab.Chat);
            }
        }
        public void SendSystemChatMessage(string text, string referenceLevelPath = null) => SendChatMessage(text, ChatMessageKind.System, referenceLevelPath);

        static string SanitizeHistoryField(string value) => value?.Replace(CHAT_DELIMITER.ToString(), string.Empty).Replace(HISTORY_RECORD_DELIMITER.ToString(), string.Empty) ?? string.Empty;
        void RequestChatHistory()
        {
            if (!ProjectArrhythmia.State.IsInLobby || ProjectArrhythmia.State.IsHosting)
                return;
            SendChat(string.Join(CHAT_DELIMITER.ToString(), HISTORY_MARKER, "REQ"));
        }
        void SendChatHistory(ulong targetId)
        {
            if (!LobbyPopup.Instance)
                return;
            var history = LobbyPopup.Instance.GetChatHistory(HISTORY_MAX_MESSAGES, message => !loadSessionCards.ContainsValue(message));
            var header = string.Join(CHAT_DELIMITER.ToString(), HISTORY_MARKER, "RES", targetId.ToString()) + CHAT_DELIMITER;
            var builder = new System.Text.StringBuilder();
            foreach (var message in history)
            {
                var record = string.Join(CHAT_DELIMITER.ToString(),
                    SanitizeHistoryField(message.name),
                    SanitizeHistoryField(message.colorHex),
                    message.timeUtc.Ticks.ToString(),
                    (int)message.kind,
                    SanitizeHistoryField(message.referenceLevelPath),
                    (int)message.referenceKind,
                    SanitizeHistoryField(message.referenceIds),
                    message.id.ToString("N"),
                    SanitizeHistoryField(message.text));
                if (builder.Length > 0 && builder.Length + record.Length + 1 > HISTORY_MAX_CHUNK_LENGTH)
                {
                    SendChat(header + builder);
                    builder.Clear();
                }
                if (builder.Length > 0)
                    builder.Append(HISTORY_RECORD_DELIMITER);
                builder.Append(record);
            }
            if (builder.Length > 0)
                SendChat(header + builder);
        }
        void ApplyChatHistory(string records)
        {
            if (!LobbyPopup.Instance || string.IsNullOrEmpty(records))
                return;
            var messages = new List<ChatMessage>();
            foreach (var record in records.Split(HISTORY_RECORD_DELIMITER))
            {
                var fields = record.Split(new[] { CHAT_DELIMITER }, 9);
                if (fields.Length < 9)
                    continue;
                messages.Add(new ChatMessage
                {
                    name = fields[0],
                    colorHex = fields[1],
                    timeUtc = long.TryParse(fields[2], out var ticks) ? new System.DateTime(ticks, System.DateTimeKind.Utc) : System.DateTime.UtcNow,
                    kind = int.TryParse(fields[3], out var kindValue) ? (ChatMessageKind)kindValue : ChatMessageKind.Player,
                    referenceLevelPath = string.IsNullOrEmpty(fields[4]) ? null : fields[4],
                    referenceKind = int.TryParse(fields[5], out var referenceKindValue) ? (ReferenceKind)referenceKindValue : ReferenceKind.None,
                    referenceIds = string.IsNullOrEmpty(fields[6]) ? null : fields[6],
                    id = System.Guid.TryParse(fields[7], out var id) ? id : System.Guid.NewGuid(),
                    text = fields[8],
                });
            }
            LobbyPopup.Instance.InsertChatHistory(messages);
        }

        /// <summary>
        /// Checks if a lobby is valid. Specifically for other mods / vanilla that have their own lobbies.
        /// </summary>
        /// <param name="lobby">Lobby to check.</param>
        /// <returns>Returns <see langword="true"/> if the lobby is joinable, otherwise returns <see langword="false"/>.</returns>
        public bool IsValidLobby(Lobby lobby)
        {
            var collection = lobby.Data.ToDictionary(x => x.Key, x => x.Value);
            return collection.ContainsKey("BetterLegacy") && collection.TryGetValue("ModVersion", out string modVersion) && new Version(modVersion) == LegacyPlugin.ModVersion
                && collection.TryGetValue("ModSnapshot", out string modSnapshot) && modSnapshot == LegacyPlugin.SNAPSHOT_VERSION
                && (!collection.TryGetValue("LobbyChannel", out string channel) || channel == LobbyChannel);
        }

        #endregion

        #region Load State

        /// <summary>
        /// Sets the scene loaded state.
        /// </summary>
        /// <param name="sceneLoaded">State to set.</param>
        public void SetSceneLoaded(bool sceneLoaded)
        {
            this.sceneLoaded = sceneLoaded;
            CurrentLobby.SetMemberData(SCENE_LOADED, sceneLoaded ? "1" : "0");
            if (AllLoaded)
                CurrentLobby.SetMemberData(IS_LOADED, "1");
        }

        /// <summary>
        /// Sets the <see cref="UnityEngine.AudioClip"/> loaded state.
        /// </summary>
        /// <param name="songLoaded">State to set.</param>
        public void SetSongLoaded(bool songLoaded)
        {
            this.songLoaded = songLoaded;
            CurrentLobby.SetMemberData(SONG_LOADED, songLoaded ? "1" : "0");
            if (AllLoaded)
                CurrentLobby.SetMemberData(IS_LOADED, "1");
        }

        /// <summary>
        /// Sets the <see cref="GameData"/> loaded state.
        /// </summary>
        /// <param name="gameDataLoaded">State to set.</param>
        public void SetGameDataLoaded(bool gameDataLoaded)
        {
            this.gameDataLoaded = gameDataLoaded;
            CurrentLobby.SetMemberData(GAME_DATA_LOADED, gameDataLoaded ? "1" : "0");
            if (AllLoaded)
                CurrentLobby.SetMemberData(IS_LOADED, "1");
        }

        /// <summary>
        /// Sets all players as unloaded.
        /// </summary>
        public void UnloadAll()
        {
            foreach (var key in loadedPlayers.Keys.ToList())
                loadedPlayers[key] = false;
        }

        /// <summary>
        /// Checks if a player has loaded.
        /// </summary>
        /// <param name="id">ID of the user.</param>
        /// <returns>Returns <see langword="true"/> if the player is loaded, otherwise returns <see langword="false"/>.</returns>
        public bool IsPlayerLoaded(SteamId id) => loadedPlayers.GetValueOrDefault(id, false);

        /// <summary>
        /// If everyone has loaded.
        /// </summary>
        public bool IsEveryoneLoaded => !loadedPlayers.IsEmpty() && !loadedPlayers.ContainsValue(false);

        void AddPlayerToLoadList(SteamId id) => loadedPlayers.TryAdd(id, false);

        void RemovePlayerFromLoadList(SteamId id) => loadedPlayers.Remove(id);

        /// <summary>
        /// Sets a lobby member as loaded.
        /// </summary>
        /// <param name="id">Lobby member ID.</param>
        public void SetLoaded(SteamId id) => loadedPlayers[id] = true;

        /// <summary>
        /// Clears the loaded players list.
        /// </summary>
        public void ClearLoaded() => loadedPlayers.Clear();
        #endregion
        #region Input Ready
        Dictionary<SteamId, bool> inputReadyPlayers = new Dictionary<SteamId, bool>();
        public bool IsPlayerInputReady(SteamId id) => inputReadyPlayers.GetValueOrDefault(id, false);
        public bool IsEveryoneInputReady => !inputReadyPlayers.ContainsValue(false);
        void AddPlayerToInputReadyList(SteamId id) => inputReadyPlayers.TryAdd(id, false);
        void RemovePlayerFromInputReadyList(SteamId id) => inputReadyPlayers.Remove(id);
        public void SetInputReady(SteamId id) => inputReadyPlayers[id] = true;
        public void ClearInputReady() => inputReadyPlayers.Clear();

        #endregion

        #region Load Progress

        public const string LOAD_MARKER = "BLLOAD";

        static string GetLevelLoadId(Level level) => RTFile.RemoveEndSlash(level.path);

        static string GetLevelDisplayName(Level level) =>
            level.metadata && level.metadata.song ? level.metadata.song.title : System.IO.Path.GetFileName(RTFile.RemoveEndSlash(level.path));

        class LevelLoadSession
        {
            public string levelId;
            public string levelName;
            public bool isArcade;
            public readonly Dictionary<SteamId, string> statusText = new Dictionary<SteamId, string>();
            public readonly Dictionary<SteamId, float> lastPercent = new Dictionary<SteamId, float>();
            public readonly Dictionary<SteamId, System.DateTime> lastSampleTime = new Dictionary<SteamId, System.DateTime>();
            public readonly Dictionary<SteamId, bool> done = new Dictionary<SteamId, bool>();
        }

        readonly Dictionary<string, LevelLoadSession> loadSessions = new Dictionary<string, LevelLoadSession>();
        readonly Dictionary<string, ChatMessage> loadSessionCards = new Dictionary<string, ChatMessage>();
        class LoadMemberDisplay
        {
            public string status;
            public bool numeric;
            public bool hasEta;
            public float fromPercent;
            public float toPercent;
            public float fromEta;
            public float toEta;
            public float startTime;
            public float duration = 1f;
            public float lastUpdate = -1f;
            public float Progress(float now) => UnityEngine.Mathf.Clamp01((now - startTime) / UnityEngine.Mathf.Max(duration, 0.01f));
            public float Percent(float now) => UnityEngine.Mathf.Lerp(fromPercent, toPercent, Progress(now));
            public float Eta(float now) => UnityEngine.Mathf.Lerp(fromEta, toEta, Progress(now));
        }
        class LoadDisplay
        {
            public readonly List<ulong> order = new List<ulong>();
            public readonly Dictionary<ulong, LoadMemberDisplay> members = new Dictionary<ulong, LoadMemberDisplay>();
            public string levelName;
            public bool dirty;
        }
        readonly Dictionary<string, LoadDisplay> loadDisplays = new Dictionary<string, LoadDisplay>();
        float nextLoadDisplayRender;
        static readonly System.Text.RegularExpressions.Regex LoadStatusRegex = new System.Text.RegularExpressions.Regex(@"^(\d+)%\.\.\.(?:(\d+(?:\.\d+)?)s|\.\.\.)$");
        void UpdateLoadMember(LoadDisplay display, ulong id, string status, float now)
        {
            if (!display.members.TryGetValue(id, out var member))
            {
                member = new LoadMemberDisplay();
                display.members[id] = member;
                display.order.Add(id);
            }
            var match = LoadStatusRegex.Match(status);
            if (!match.Success)
            {
                member.numeric = false;
                member.status = status;
                return;
            }
            var percent = float.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            var hasEta = match.Groups[2].Success;
            var eta = hasEta ? float.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture) : 0f;
            member.fromPercent = member.numeric ? member.Percent(now) : percent;
            member.fromEta = member.numeric && member.hasEta && hasEta ? member.Eta(now) : eta;
            member.toPercent = percent;
            member.toEta = eta;
            member.hasEta = hasEta;
            member.duration = member.lastUpdate >= 0f ? UnityEngine.Mathf.Clamp(now - member.lastUpdate, 0.25f, 3f) : 1f;
            member.startTime = now;
            member.lastUpdate = now;
            member.numeric = true;
        }
        string BuildLoadText(LoadDisplay display, float now)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Loading Level {display.levelName}...");
            foreach (var id in display.order)
            {
                var member = display.members[id];
                SteamId steamId = id;
                var status = member.numeric ? $"{UnityEngine.Mathf.RoundToInt(member.Percent(now))}%...{(member.hasEta ? UnityEngine.Mathf.RoundToInt(member.Eta(now)) + "s" : "...")}" : member.status;
                sb.AppendLine($"{new Friend(steamId).Name}: [{status}]");
            }
            return sb.ToString().TrimEnd();
        }
        public void TickLoadDisplays()
        {
            if (loadDisplays.Count == 0 || UnityEngine.Time.unscaledTime < nextLoadDisplayRender)
                return;
            var now = UnityEngine.Time.unscaledTime;
            nextLoadDisplayRender = now + 0.1f;
            foreach (var pair in loadDisplays)
            {
                var display = pair.Value;
                if (!display.dirty || !loadSessionCards.TryGetValue(pair.Key, out var message))
                    continue;
                message.text = BuildLoadText(display, now);
                LobbyPopup.Instance?.UpdateChatMessageCard(message);
                var animating = false;
                foreach (var member in display.members.Values)
                    if (member.numeric && member.Progress(now) < 1f)
                        animating = true;
                display.dirty = animating;
            }
        }
        public string activeLoadLevelId;

        public void StartLevelLoadSession(Level level, bool isArcade) => StartLevelLoadSession(GetLevelLoadId(level), GetLevelDisplayName(level), isArcade);

        public void StartLevelLoadSession(string levelId, string levelName, bool isArcade)
        {
            if (!ProjectArrhythmia.State.IsHosting || string.IsNullOrEmpty(levelId) || loadSessions.ContainsKey(levelId))
                return;

            var session = new LevelLoadSession { levelId = levelId, levelName = levelName, isArcade = isArcade };
            var selfId = RTSteamManager.inst.steamUser.steamID;
            session.statusText[selfId] = "Done";
            session.done[selfId] = true;
            loadSessions[levelId] = session;

            var allDone = true;
            foreach (var member in CurrentLobby.Members)
                if (!session.done.GetValueOrDefault(member.Id, false))
                {
                    allDone = false;
                    break;
                }

            BroadcastLoadState(session, allDone);
            if (allDone)
                loadSessions.Remove(levelId);
        }

        public void ReportLevelLoadProgress(string levelId, int percent)
        {
            if (string.IsNullOrEmpty(levelId) || !ProjectArrhythmia.State.IsInLobby)
                return;

            if (ProjectArrhythmia.State.IsHosting)
            {
                ApplyLoadReport(levelId, RTSteamManager.inst.steamUser.steamID, percent);
                return;
            }

            var payload = string.Join(CHAT_DELIMITER.ToString(), LOAD_MARKER, "REPORT", levelId, percent.ToString());
            SendChat(payload);
        }

        void ApplyLoadReport(string levelId, SteamId steamId, int percent)
        {
            if (!loadSessions.TryGetValue(levelId, out var session) || session.done.GetValueOrDefault(steamId, false))
                return;

            var now = System.DateTime.UtcNow;
            string text;
            if (percent >= 100)
            {
                text = "Parsing...";
            }
            else
            {
                var eta = "...";
                if (session.lastPercent.TryGetValue(steamId, out var lastPercent) && session.lastSampleTime.TryGetValue(steamId, out var lastTime))
                {
                    var deltaPercent = percent - lastPercent;
                    var deltaTime = (now - lastTime).TotalSeconds;
                    if (deltaPercent > 0f && deltaTime > 0f)
                        eta = $"{System.Math.Round((100f - percent) / deltaPercent * deltaTime)}s";
                }
                text = $"{percent}%...{eta}";
            }

            session.lastPercent[steamId] = percent;
            session.lastSampleTime[steamId] = now;
            session.statusText[steamId] = text;
            BroadcastLoadState(session, false);
        }

        void OnLevelLoadPlayerDone(SteamId steamId)
        {
            foreach (var session in loadSessions.Values)
            {
                if (session.done.GetValueOrDefault(steamId, false))
                    continue;

                session.statusText[steamId] = "Done";
                session.done[steamId] = true;

                var allDone = true;
                foreach (var member in CurrentLobby.Members)
                {
                    if (!session.done.GetValueOrDefault(member.Id, false))
                    {
                        allDone = false;
                        break;
                    }
                }

                BroadcastLoadState(session, allDone);
                if (allDone)
                    loadSessions.Remove(session.levelId);
            }
        }

        void BroadcastLoadState(LevelLoadSession session, bool finished)
        {
            var lines = string.Join(";", session.statusText.Select(x => $"{(ulong) x.Key}:{x.Value}"));
            var payload = string.Join(CHAT_DELIMITER.ToString(), LOAD_MARKER, "STATE", session.levelId, session.levelName, session.isArcade ? "1" : "0", finished ? "1" : "0", lines);
            SendChat(payload);
        }

        void ApplyIncomingLoadState(string[] parts)
        {
            if (parts.Length < 7)
                return;

            var levelId = parts[2];
            var levelName = parts[3];
            var isArcade = parts[4] == "1";
            var finished = parts[5] == "1";
            var lines = parts[6];

            string text;
            if (finished)
            {
                text = isArcade ? $"Played level {levelName}." : $"Opened level {levelName}.";
                if (activeLoadLevelId == levelId)
                    activeLoadLevelId = null;
            }
            else
            {
                if (!loadDisplays.TryGetValue(levelId, out var display))
                    loadDisplays[levelId] = display = new LoadDisplay();
                display.levelName = levelName;
                var now = UnityEngine.Time.unscaledTime;
                if (!string.IsNullOrEmpty(lines))
                    foreach (var line in lines.Split(';'))
                    {
                        var split = line.Split(new[] { ':' }, 2);
                        if (split.Length < 2 || !ulong.TryParse(split[0], out var rawId))
                            continue;
                        UpdateLoadMember(display, rawId, split[1], now);
                    }
                display.dirty = true;
                text = BuildLoadText(display, now);
                if (!ProjectArrhythmia.State.IsHosting)
                    activeLoadLevelId = levelId;
            }
            if (loadSessionCards.TryGetValue(levelId, out var message))
            {
                message.text = text;
                LobbyPopup.Instance?.UpdateChatMessageCard(message);
            }
            else
            {
                message = LobbyPopup.Instance?.AddSystemMessage(text);
                if (message != null)
                    loadSessionCards[levelId] = message;
                if (!finished && LobbyPopup.Instance)
                {
                    LobbyPopup.Instance.Open();
                    LobbyPopup.Instance.SetTab(LobbyPopup.LobbyTab.Chat);
                }
            }
            if (finished)
            {
                loadSessionCards.Remove(levelId);
                loadDisplays.Remove(levelId);
            }
        }

        #endregion

        #region Events

        void OnChatMessage(Lobby lobby, Friend friend, string message)
        {
            var parts = string.IsNullOrEmpty(message) ? null : message.Split(new[] { CHAT_DELIMITER }, 9);

            if (parts != null && parts.Length >= 4 && parts[0] == LOAD_MARKER)
            {
                if (parts[1] == "REPORT" && ProjectArrhythmia.State.IsHosting && IsMemberAuthorized(friend.Id.Value) && int.TryParse(parts[3], out var reportedPercent))
                    ApplyLoadReport(parts[2], friend.Id, reportedPercent);
                else if (parts[1] == "STATE")
                    ApplyIncomingLoadState(parts);
                return;
            }

            if (parts != null && parts.Length >= 3 && parts[0] == AUTH_MARKER)
            {
                if (parts[1] == "REQ" && ProjectArrhythmia.State.IsHosting && friend.Id != RTSteamManager.inst.steamUser.steamID)
                    HandleAuthRequest(lobby, friend, parts[2]);
                else if (parts[1] == "RES" && !ProjectArrhythmia.State.IsHosting && friend.Id == lobby.Owner.Id && parts.Length >= 4 && parts[2] == RTSteamManager.inst.steamUser.steamID.Value.ToString())
                    HandleAuthResponse(lobby, parts[3] == "1");
                return;
            }
            if (parts != null && parts.Length >= 2 && parts[0] == HISTORY_MARKER)
            {
                if (parts[1] == "REQ" && ProjectArrhythmia.State.IsHosting && friend.Id != RTSteamManager.inst.steamUser.steamID && IsMemberAuthorized(friend.Id.Value))
                    SendChatHistory(friend.Id.Value);
                else if (parts[1] == "RES" && !ProjectArrhythmia.State.IsHosting && friend.Id == lobby.Owner.Id)
                {
                    var historyParts = message.Split(new[] { CHAT_DELIMITER }, 4);
                    if (historyParts.Length >= 4 && historyParts[2] == RTSteamManager.inst.steamUser.steamID.Value.ToString())
                        ApplyChatHistory(historyParts[3]);
                }
                return;
            }

            ChatMessage chat;
            var chatParts = parts != null && parts[0] == CHAT_MARKER ? message.Split(new[] { CHAT_DELIMITER }, 10) : null;
            if (chatParts != null && chatParts.Length >= 10)
            {
                chat = new ChatMessage
                {
                    name = chatParts[1],
                    colorHex = chatParts[2],
                    timeUtc = long.TryParse(chatParts[3], out var ticks) ? new System.DateTime(ticks, System.DateTimeKind.Utc) : System.DateTime.UtcNow,
                    kind = int.TryParse(chatParts[4], out var kindValue) ? (ChatMessageKind) kindValue : ChatMessageKind.Player,
                    referenceLevelPath = string.IsNullOrEmpty(chatParts[5]) ? null : chatParts[5],
                    referenceKind = int.TryParse(chatParts[6], out var referenceKindValue) ? (ReferenceKind) referenceKindValue : ReferenceKind.None,
                    referenceIds = string.IsNullOrEmpty(chatParts[7]) ? null : chatParts[7],
                    id = System.Guid.TryParse(chatParts[8], out var chatId) ? chatId : System.Guid.NewGuid(),
                    text = chatParts[9],
                };
            }
            else // foreign / vanilla chat: show it raw with the Steam name
            {
                chat = new ChatMessage
                {
                    name = friend.Name,
                    colorHex = "FFFFFFFF",
                    timeUtc = System.DateTime.UtcNow,
                    text = message,
                };
            }

            try
            {
                if (LobbyPopup.Instance)
                    LobbyPopup.Instance.AddChatMessage(chat);
            }
            catch (System.Exception ex)
            {
                LogError($"Failed to add chat message: {ex}");
            }
        }

        void OnLobbyDataChanged(Lobby lobby)
        {
            // handle lobby data
            try
            {
                LobbyPopup.Instance.Render();
            }
            catch
            {

            }
        }

        void OnLobbyMemberDataChanged(Lobby lobby, Friend friend)
        {
            if (lobby.GetMemberData(friend, IS_LOADED) != "1")
                return;

            SetLoaded(friend.Id);
            if (ProjectArrhythmia.State.IsHosting)
                OnLevelLoadPlayerDone(friend.Id);
            try
            {
                LobbyPopup.Instance.Render();
            }
            catch
            {

            }
        }

        void HandleAuthRequest(Lobby lobby, Friend friend, string proof)
        {
            if (!HostHasPassword || authorizedMembers.Contains(friend.Id.Value))
                return;
            var accepted = proof == AuthHash(hostPassword, friend.Id.Value, lobby.Id.Value);
            if (accepted)
                authorizedMembers.Add(friend.Id.Value);
            SendChat(string.Join(CHAT_DELIMITER.ToString(), AUTH_MARKER, "RES", friend.Id.Value.ToString(), accepted ? "1" : "0"));
            if (accepted)
            {
                SendSystemChatMessage($"{friend.Name} has joined the lobby.");
                HandleMemberJoined(friend);
            }
            else if (Transport.Instance && Transport.Instance.steamIDToNetID.TryGetValue(friend.Id, out int netId))
                NetworkManager.inst.KickClient(netId);
        }
        void HandleAuthResponse(Lobby lobby, bool accepted)
        {
            if (!awaitingAuth)
                return;
            awaitingAuth = false;
            joinPassword = null;
            if (accepted)
            {
                CompleteClientEntry(lobby);
                return;
            }
            LeaveLobby();
            LobbyPopup.Instance?.OnJoinRejected(lobby);
        }
        void CompleteClientEntry(Lobby lobby)
        {
            RequestChatHistory();
            if (Transport.Instance && NetworkManager.inst.IsConnectedToServer)
                SyncPlayersToServer();
            else
            {
                NetworkManager.inst.onClientConnectedTemp += connection => SyncPlayersToServer();
                if (!Transport.Instance)
                    RTSteamManager.inst.StartClient(lobby.Owner.Id);
            }
            foreach (var lobbyMember in lobby.Members)
            {
                AddPlayerToLoadList(lobbyMember.Id);
                AddPlayerToInputReadyList(lobbyMember.Id);
                if (lobby.GetMemberData(lobbyMember, IS_LOADED) == "1")
                    SetLoaded(lobbyMember.Id);
            }
        }
        void OnLobbyMemberDisconnected(Lobby lobby, Friend friend)
        {
            if (ProjectArrhythmia.State.IsHosting && HostHasPassword && friend.Id != RTSteamManager.inst.steamUser.steamID && !authorizedMembers.Contains(friend.Id.Value))
                return;
            authorizedMembers.Remove(friend.Id.Value);
            Log($"Member left: [{friend.Name}]");

            SoundManager.inst.PlaySound(DefaultSounds.Block); // maybe add a new sound?

            RemovePlayerFromLoadList(friend.Id);
            RemovePlayerFromInputReadyList(friend.Id);
            try
            {
                LobbyPopup.Instance.Render();
                LobbyPopup.Instance.AddSystemMessage($"{friend.Name} has left the lobby.");
            }
            catch
            {

            }

            PlayerManager.inst.players.ForLoopReverse((player, index) =>
            {
                if (player.ID == friend.Id)
                    PlayerManager.inst.RemovePlayer(player);
            });
            PlayerManager.inst.players.ForLoop((player, index) => player.index = index);

            EditorMultiplayer.RemovePeer(friend.Id);

            if (Transport.Instance && Transport.Instance.steamIDToNetID.TryGetValue(friend.Id, out int id))
                NetworkManager.inst.KickClient(id);
        }

        void OnLobbyMemberJoined(Lobby lobby, Friend friend)
        {
            if (lobby.GetData(HAS_PASSWORD) == "1" || HostHasPassword)
                return;
            HandleMemberJoined(friend);
        }
        void HandleMemberJoined(Friend friend)
        {
            Log($"Member joined: [{friend.Name}]");

            SoundManager.inst.PlaySound(DefaultSounds.SpawnPlayer);

            AddPlayerToLoadList(friend.Id);
            AddPlayerToInputReadyList(friend.Id);
            try
            {
                LobbyPopup.Instance.Render();
                if (!HostHasPassword)
                    LobbyPopup.Instance.AddSystemMessage($"{friend.Name} has joined the lobby.");
            }
            catch
            {

            }

            if (ProjectArrhythmia.State.IsHosting)
            {
                if (ProjectArrhythmia.State.InEditor && EditorManager.inst.hasLoadedLevel)
                {
                    var level = EditorLevelManager.inst.CurrentLevel;
                    StartLevelLoadSession(level, false);
                    NetworkFunction.LoadClientEditorLevel(level, friend.Id);
                }
                else if (ProjectArrhythmia.State.InGame)
                {
                    var level = LevelManager.CurrentLevel;
                    StartLevelLoadSession(level, true);
                    NetworkFunction.LoadClientLevel(level, friend.Id);
                }
                else
                    NetworkFunction.SetClientScene(SceneHelper.Current, true, -1);
            }
        }

        void OnLobbyEntered(Lobby lobby)
        {
            Log($"Joined Lobby hosted by [{lobby.Owner.Name}]");
            CurrentLobby = lobby;
            ProjectArrhythmia.State.IsInLobby = true;
            try
            {
                LobbyPopup.Instance.Render();
            }
            catch
            {

            }

            if (lobby.Owner.Id == RTSteamManager.inst.steamUser.steamID)
            {
                SetLoaded(lobby.Owner.Id);
                return;
            }

            LobbyPopup.Instance?.ClearChat();

            if (lobby.GetData(HAS_PASSWORD) == "1")
            {
                awaitingAuth = true;
                SendChat(string.Join(CHAT_DELIMITER.ToString(), AUTH_MARKER, "REQ", AuthHash(joinPassword ?? string.Empty, RTSteamManager.inst.steamUser.steamID.Value, lobby.Id.Value)));
                return;
            }
            CompleteClientEntry(lobby);
        }

        void OnLobbyCreated(Result result, Lobby lobby)
        {
            if (result != Result.OK)
            {
                Log($"Failed to create lobby. Result: {result}");
                lobby.Leave();
                return;
            }
            Log($"Lobby created!");
            CurrentLobby = lobby;
            ProjectArrhythmia.State.IsInLobby = true;
            LobbyPopup.Instance?.ClearChat();
            LobbyPopup.Instance?.AddSystemMessage($"{RTSteamManager.inst.steamUser.name} has started a lobby.");

            switch (LobbySettings.Visibility)
            {
                case LobbyVisibility.FriendsOnly: {
                        lobby.SetFriendsOnly();
                        break;
                    }
                case LobbyVisibility.Private: {
                        lobby.SetPrivate();
                        break;
                    }
                case LobbyVisibility.Invisible: {
                        lobby.SetInvisible();
                        break;
                    }
                default: {
                        lobby.SetPublic();
                        break;
                    }
            }

            lobby.SetJoinable(true);

            lobby.SetData("ModVersion", LegacyPlugin.ModVersion.ToString());
            lobby.SetData("ModSnapshot", LegacyPlugin.SNAPSHOT_VERSION);
            lobby.SetData("GameVersion", ProjectArrhythmia.VANILLA_VERSION);
            lobby.SetData("BetterLegacy", "true");
            if (HostHasPassword)
                lobby.SetData(HAS_PASSWORD, "1");
            lobby.SetData("LobbyName", LobbySettings.Name);
            if (!string.IsNullOrEmpty(LobbySettings.Channel))
                lobby.SetData("LobbyChannel", LobbySettings.Channel);
        }

        #endregion

        #endregion
    }
}
