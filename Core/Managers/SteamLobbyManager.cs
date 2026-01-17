using System.Collections;
using System.Collections.Generic;
using System.Linq;

using SteamworksFacepunch;
using SteamworksFacepunch.Data;

using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers.Settings;

namespace BetterLegacy.Core.Managers
{
    public class SteamLobbyManager : BaseManager<SteamLobbyManager, SteamLobbyManagerSettings>
    {
        public Lobby CurrentLobby { get; set; }
        public LobbySettings LobbySettings { get; set; } = new LobbySettings();

        public Dictionary<SteamId, bool> loadedPlayers = new Dictionary<SteamId, bool>();

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
        }

        public void CreateLobby()
        {
            Log($"Creating a lobby");
            ProjectArrhythmia.State.IsHosting = true;
            RTSteamManager.inst.StartServer();
            SteamMatchmaking.CreateLobbyAsync(LobbySettings.PlayerCount);
        }

        public async void JoinRandomLobby()
        {
            var query = new LobbyQuery();
            var lobbies = await query.WithMaxResults(10).RequestAsync();
            LegacyPlugin.MainTick += () =>
            {
                if (lobbies == null)
                    return;
                Log($"Found lobbies: {lobbies.Length}");
                if (lobbies.IsEmpty())
                    return;
                for (int i = 0; i < lobbies.Length; i++)
                {
                    var lobby = lobbies[i];
                    Log($"Lobby 0: Owner {lobby.Owner.Name}");

                    var collection = lobby.Data.ToDictionary(x => x.Key, x => x.Value);
                    if (collection.ContainsKey("ModVersion"))
                    {
                        JoinLobby(lobby);
                        return;
                    }

                    Log($"Could not join lobby as it does not contain mod version data.");
                }
            };
        }

        public async void JoinLobby(SteamId id)
        {
            var Qlobby = await SteamMatchmaking.JoinLobbyAsync(id);
            if (!Qlobby.TryGetValue(out Lobby lobby))
                return;

            CurrentLobby = lobby;
            RTSteamManager.inst.StartClient(lobby.Owner.Id);
        }

        public void JoinLobby(Lobby lobby) => CoroutineHelper.StartCoroutine(IJoinLobby(lobby));

        IEnumerator IJoinLobby(Lobby lobby)
        {
            CurrentLobby = lobby;
            yield return CoroutineHelper.StartCoroutine(lobby.Join());
            RTSteamManager.inst.StartClient(lobby.Owner.Id);
        }

        public void LeaveLobby()
        {
            ProjectArrhythmia.State.IsInLobby = false;
            CurrentLobby.Leave();
        }

        public void SendChat(string message) => CurrentLobby.SendChatString(message);

        void OnChatMessage(Lobby lobby, Friend friend, string message)
        {
            // handle chat message through chat bubble
            Log($"{friend.Name} says {message}");
        }

        void OnLobbyDataChanged(Lobby lobby)
        {
            // handle lobby data
        }

        void OnLobbyMemberDataChanged(Lobby lobby, Friend friend)
        {
            if (lobby.GetMemberData(friend, "IsLoaded") != "1")
                return;

            SetLoaded(friend.Id);
        }

        void OnLobbyMemberDisconnected(Lobby lobby, Friend friend)
        {
            Log($"Member left: [{friend.Name}]");

            SoundManager.inst.PlaySound(DefaultSounds.Block); // maybe add a new sound?

            RemovePlayerFromLoadList(friend.Id);

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
        }

        void OnLobbyEntered(Lobby lobby)
        {
            Log($"Joined Lobby hosted by [{lobby.Owner.Name}]");
            CurrentLobby = lobby;
            ProjectArrhythmia.State.IsInLobby = true;

            if (lobby.Owner.Id == RTSteamManager.inst.steamUser.steamID)
            {
                AddPlayerToLoadList(lobby.Owner.Id);
                return;
            }

            if (!Transport.Instance)
                RTSteamManager.inst.StartClient(lobby.Owner.Id);
            foreach (var lobbyMember in lobby.Members)
            {
                var player = new PAPlayer(true, 0);
                player.IsLocalPlayer = false;
                player.ID = lobbyMember.Id;
                PlayerManager.Players.Add(player);
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

            if (LobbySettings.IsPrivate)
                lobby.SetFriendsOnly();
            else
                lobby.SetPublic();

            lobby.SetJoinable(true);

            lobby.SetData("ModVersion", LegacyPlugin.ModVersion.ToString());
            lobby.SetData("GameVersion", ProjectArrhythmia.VANILLA_VERSION);
        }

        void AddPlayerToLoadList(SteamId id) => loadedPlayers.TryAdd(id, false);

        void RemovePlayerFromLoadList(SteamId id) => loadedPlayers?.Remove(id);

        void SetLoaded(SteamId id) => loadedPlayers[id] = true;

        public void UnloadAll()
        {
            foreach (var key in loadedPlayers.Keys.ToList())
                loadedPlayers[key] = false;
        }

        public bool IsPlayerLoaded(SteamId id) => loadedPlayers.GetValueOrDefault(id, false);

        public bool IsEveryoneLoaded => !loadedPlayers.ContainsValue(false);
    }
}
