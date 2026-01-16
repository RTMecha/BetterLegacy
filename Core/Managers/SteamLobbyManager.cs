using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SteamworksFacepunch;
using SteamworksFacepunch.Data;

using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers.Settings;

namespace BetterLegacy.Core.Managers
{
    public class SteamLobbyManager : BaseManager<SteamLobbyManager, ManagerSettings>
    {
        public Lobby CurrentLobby { get; set; }
        public LobbySettings LobbySettings { get; set; } = new LobbySettings();

        public Dictionary<SteamId, bool> loadedPlayers = new Dictionary<SteamId, bool>();

        public override void OnInit()
        {
            CoreHelper.Log($"Setting lobby events");
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
            ProjectArrhythmia.State.IsHosting = true;
            RTSteamManager.inst.StartServer();
            //SteamMatchmaking.CreateLobbyAsync(LobbySettings.PlayerCount);

            SteamMatchmaking.Internal.CreateLobby(LobbyType.FriendsOnly, LobbySettings.PlayerCount);
            CurrentLobby.Join();
        }

        void OnChatMessage(Lobby lobby, Friend friend, string message)
        {
            // handle chat message through chat bubble
            CoreHelper.Log($"{friend.Name} says {message}");
        }

        void OnLobbyDataChanged(Lobby lobby)
        {

        }

        void OnLobbyMemberDataChanged(Lobby lobby, Friend friend)
        {
            //if (lobby.GetMemberData(friend, "IsLoaded") != null)
            //    return;

            //SetLoaded(friend.Id);
        }

        void OnLobbyMemberDisconnected(Lobby lobby, Friend friend)
        {
            CoreHelper.Log($"Member left: [{friend.Name}]");

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
            CoreHelper.Log($"Member joined: [{friend.Name}]");

            SoundManager.inst.PlaySound(DefaultSounds.SpawnPlayer);

            AddPlayerToLoadList(friend.Id);
        }

        void OnLobbyEntered(Lobby lobby)
        {
            CoreHelper.Log($"Joined Lobby hosted by [{lobby.Owner.Name}]");
            CurrentLobby = lobby;
            ProjectArrhythmia.State.IsInLobby = true;

            if (lobby.Owner.Id == RTSteamManager.inst.steamUser.steamID)
            {
                AddPlayerToLoadList(lobby.Owner.Id);
                return;
            }

            foreach (var lobbyMember in lobby.Members)
            {
                var player = new PAPlayer(true, 0);
                player.IsLocalPlayer = false;
                player.ID = lobbyMember.Id;
                PlayerManager.Players.Add(player);
                AddPlayerToLoadList(lobby.Owner.Id);
            }
        }

        void OnLobbyCreated(Result result, Lobby lobby)
        {
            if (result != Result.OK)
            {
                CoreHelper.Log($"Failed to create lobby. Result: {result}");
                lobby.Leave();
                return;
            }
            CoreHelper.Log($"Lobby created!");
            CurrentLobby = lobby;
            ProjectArrhythmia.State.IsInLobby = true;

            if (LobbySettings.IsPrivate)
                lobby.SetFriendsOnly();
            else
                lobby.SetPublic();

            lobby.SetJoinable(true);
        }

        void AddPlayerToLoadList(SteamId id) => loadedPlayers.TryAdd(id, false);

        void RemovePlayerFromLoadList(SteamId id) => loadedPlayers?.Remove(id);

        void SetLoaded(SteamId id) => loadedPlayers[id] = true;

        public void LeaveLobby()
        {
            ProjectArrhythmia.State.IsInLobby = false;
            CurrentLobby.Leave();
        }

        public void UnloadAll()
        {
            foreach (var key in loadedPlayers.Keys.ToList())
                loadedPlayers[key] = false;
        }

        public bool IsPlayerLoaded(SteamId id) => loadedPlayers.GetValueOrDefault(id, false);

        public bool IsEveryoneLoaded => !loadedPlayers.ContainsValue(false);
    }
}
