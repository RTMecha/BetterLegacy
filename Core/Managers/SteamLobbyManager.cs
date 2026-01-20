using System.Collections;
using System.Collections.Generic;
using System.Linq;

using SteamworksFacepunch;
using SteamworksFacepunch.Data;

using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers.Settings;
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

        public List<PAPlayer> localPlayers = new List<PAPlayer>();

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

        public void SaveLobbySettings() => LobbySettings.WriteToFile(RTFile.CombinePaths(RTFile.ApplicationDirectory, "settings", LobbySettings.GetFileName()));

        public void LoadLobbySettings()
        {
            var path = RTFile.CombinePaths(RTFile.ApplicationDirectory, "settings", LobbySettings.MAIN_FILE_NAME);
            if (!RTFile.FileExists(path))
                return;
            LobbySettings = RTFile.CreateFromFile<LobbySettings>(path);
            LobbyPopup.Instance.nameField?.SetTextWithoutNotify(LobbySettings.Name);
            LobbyPopup.Instance.playerCountField?.SetTextWithoutNotify(LobbySettings.PlayerCount.ToString());
            LobbyPopup.Instance.visibilityDropdown?.SetValueWithoutNotify((int)LobbySettings.Visibility);
        }

        public void SyncPlayersToServer()
        {
            localPlayers = new List<PAPlayer>(PlayerManager.Players);
            NetworkManager.inst.RunFunction(NetworkFunction.SEND_SERVER_PLAYER_DATA, new PacketList<PAPlayer>(PlayerManager.Players));
        }

        public void SyncPlayersToClients()
        {
            NetworkManager.inst.RunFunction(NetworkFunction.SEND_CLIENT_PLAYER_DATA, new PacketList<PAPlayer>(PlayerManager.Players));
        }

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
            if (string.IsNullOrEmpty(LobbySettings.Name))
            {
                LogError($"Cannot create a lobby with an empty name!");
                return;
            }

            Log($"Creating a lobby");
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
                    if (IsValidLobby(lobby))
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
        public void JoinLobby(Lobby lobby) => CoroutineHelper.StartCoroutine(IJoinLobby(lobby));

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
        }

        /// <summary>
        /// Leaves the current lobby.
        /// </summary>
        public void LeaveLobby()
        {
            ProjectArrhythmia.State.IsInLobby = false;
            CurrentLobby.Leave();
        }

        /// <summary>
        /// Sends a chat to the current lobby.
        /// </summary>
        /// <param name="message">Message to send.</param>
        public void SendChat(string message) => CurrentLobby.SendChatString(message);

        /// <summary>
        /// Checks if a lobby is valid. Specifically for other mods / vanilla that have their own lobbies.
        /// </summary>
        /// <param name="lobby">Lobby to check.</param>
        /// <returns>Returns <see langword="true"/> if the lobby is joinable, otherwise returns <see langword="false"/>.</returns>
        public bool IsValidLobby(Lobby lobby)
        {
            var collection = lobby.Data.ToDictionary(x => x.Key, x => x.Value);
            return collection.ContainsKey("BetterLegacy") && collection.TryGetValue("ModVersion", out string modVersion) && new Version(modVersion) == LegacyPlugin.ModVersion
                && (!collection.TryGetValue("LobbyChannel", out string channel) || channel == LobbyChannel);
        }

        #endregion

        #region Load State

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

        void SetLoaded(SteamId id) => loadedPlayers[id] = true;

        #endregion

        #region Events

        void OnChatMessage(Lobby lobby, Friend friend, string message)
        {
            // handle chat message through chat bubble
            Log($"{friend.Name} says {message}");
            try
            {
                LobbyPopup.Instance.Render();
            }
            catch
            {

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
            if (lobby.GetMemberData(friend, "IsLoaded") != "1")
                return;

            SetLoaded(friend.Id);
            try
            {
                LobbyPopup.Instance.Render();
            }
            catch
            {

            }
        }

        void OnLobbyMemberDisconnected(Lobby lobby, Friend friend)
        {
            Log($"Member left: [{friend.Name}]");

            SoundManager.inst.PlaySound(DefaultSounds.Block); // maybe add a new sound?

            RemovePlayerFromLoadList(friend.Id);
            try
            {
                LobbyPopup.Instance.Render();
            }
            catch
            {

            }

            PlayerManager.Players.ForLoopReverse((player, index) =>
            {
                if (player.ID == friend.Id)
                    PlayerManager.RemovePlayer(player);
            });
            PlayerManager.Players.ForLoop((player, index) => player.index = index);

            if (Transport.Instance && Transport.Instance.steamIDToNetID.TryGetValue(friend.Id, out int id))
                Transport.Instance.KickConnection(id);
        }

        void OnLobbyMemberJoined(Lobby lobby, Friend friend)
        {
            Log($"Member joined: [{friend.Name}]");

            SoundManager.inst.PlaySound(DefaultSounds.SpawnPlayer);

            AddPlayerToLoadList(friend.Id);
            try
            {
                LobbyPopup.Instance.Render();
            }
            catch
            {

            }

            if (ProjectArrhythmia.State.IsHosting)
                NetworkFunction.SetClientGameData(GameData.Current, friend.Id.ToString());
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

            if (!Transport.Instance)
            {
                NetworkManager.inst.onClientConnectedTemp += connection => SyncPlayersToServer();
                RTSteamManager.inst.StartClient(lobby.Owner.Id);
            }
            else
                SyncPlayersToServer();
            foreach (var lobbyMember in lobby.Members)
            {
                //if (lobbyMember.Id != RTSteamManager.inst.steamUser.steamID)
                //    PlayerManager.Players.Add(new PAPlayer(PlayerManager.Players.Count, lobbyMember.Id));
                AddPlayerToLoadList(lobbyMember.Id);
                if (lobby.GetMemberData(lobbyMember, "IsLoaded") == "1")
                    SetLoaded(lobbyMember.Id);
            }
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
            lobby.SetData("GameVersion", ProjectArrhythmia.VANILLA_VERSION);
            lobby.SetData("BetterLegacy", "true");
            lobby.SetData("LobbyName", LobbySettings.Name);
            if (!string.IsNullOrEmpty(LobbySettings.Channel))
                lobby.SetData("LobbyChannel", LobbySettings.Channel);
        }

        #endregion

        #endregion
    }
}
