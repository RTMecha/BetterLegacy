using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using LSFunctions;

using InControl;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Menus.UI.Elements;
using BetterLegacy.Menus.UI.Layouts;

namespace BetterLegacy.Menus.UI.Interfaces
{
    /// <summary>
    /// Interface for selecting controller inputs.
    /// </summary>
    public class InputSelectInterface : BaseInterface
    {
        #region Constructors

        public InputSelectInterface()
        {
            name = "Input Select";
            regenerate = false;

            maxPlayers = ProjectArrhythmia.State.IsInLobby && !AllowMultipleLocalPlayers
                ? Mathf.Clamp(SteamLobbyManager.inst.CurrentLobby.Members.Count(), 1, 8)
                : 8;
            elements.AddRange(GenerateTopBar("Input Select | Specify Simulations", 6, 0f, false));

            layouts.Add("desc", new MenuVerticalLayout
            {
                name = "desc",
                rect = RectValues.Default.AnchoredPosition(-700f, 140f).SizeDelta(400f, 400f),
                regenerate = false,
            });

            elements.Add(new MenuText
            {
                id = LSText.randomNumString(16),
                name = "Text",
                text = "[BACK] or [ESCAPE] to return to previous menu.",
                parentLayout = "desc",
                length = 0.01f,
                rect = RectValues.Default.SizeDelta(300f, 46f),
                hideBG = true,
                textColor = 6,
                regenerate = false,
            });

            elements.Add(new MenuText
            {
                id = LSText.randomNumString(16),
                name = "Text",
                text = "[BOOST] or [SPACE] to add a simulation.",
                parentLayout = "desc",
                length = 0.01f,
                rect = RectValues.Default.SizeDelta(300f, 46f),
                hideBG = true,
                textColor = 6,
                regenerate = false,
            });

            layouts.Add("nanobots", new MenuVerticalLayout
            {
                name = "nanobots",
                rect = RectValues.Default.AnchoredPosition(-600f, -40f).SizeDelta(600f, 400f),
                regenerate = false,
            });

            for (int i = 0; i < maxPlayers; i++)
            {
                var menuText = new MenuText
                {
                    id = LSText.randomNumString(16),
                    name = "Text",
                    text = string.Empty,
                    parentLayout = "nanobots",
                    length = 0.01f,
                    rect = RectValues.Default.SizeDelta(300f, 46f),
                    hideBG = true,
                    textColor = 6,
                    opacity = 1f,
                    regenerate = false,
                };
                elements.Add(menuText);
                nanobots.Add(menuText);
                noTexts.Add($"<color={LSText.randomHex("666666")}>{LSText.randomString(36)}</color>");
                noColors.Add(LSText.randomHex("666666"));
            }

            elements.AddRange(GenerateBottomBar(6, 0f, false));

            changeTextCoroutine = CoroutineHelper.StartCoroutine(ChangeText());
            exitFunc = Exit;
            UpdateText(false);
            StartGeneration();
        }

        #endregion

        #region Values

        /// <summary>
        /// The current <see cref="InputSelectInterface"/>.
        /// </summary>
        public static InputSelectInterface Current { get; set; }

        /// <summary>
        /// Function that occurs when a player selects all controller inputs in the Input Select screen and loads the next scene.
        /// </summary>
        public static Action OnInputsSelected { get; set; }

        /// <summary>
        /// Function that occurs when a player exists the interface.
        /// </summary>
        public static Action OnExit { get; set; }

        Coroutine changeTextCoroutine;
        List<MenuText> nanobots = new List<MenuText>();
        List<string> noTexts = new List<string>();
        List<string> noColors = new List<string>();
        bool sentInputReady;
        int maxPlayers;
        static bool AllowMultipleLocalPlayers =>
            ProjectArrhythmia.State.IsInLobby &&
            (ProjectArrhythmia.State.IsClient ? LobbyInfo.HostLobbySettings?.AllowMultipleLocalPlayers == true : SteamLobbyManager.inst.LobbySettings.AllowMultipleLocalPlayers);

        #endregion

        #region Functions

        /// <summary>
        /// Initializes the <see cref="InputSelectInterface"/>.
        /// </summary>
        public static void Init()
        {
            InputDataManager.inst.BindMenuKeys();
            InputDataManager.inst.ClearInputs();
            ArcadeHelper.fromLevel = false;
            InputDataManager.inst.playersCanJoin = true;
            var removedStale = PlayerManager.inst.players.RemoveAll(x => x.IsLocalPlayer);
            PlayerManager.inst.localPlayers.Clear();
            CoreHelper.Log($"[InputSelect] Init. removedStaleLocalPlayers={removedStale} remainingPlayers={PlayerManager.inst.players.Count}");
            if (ProjectArrhythmia.State.IsHosting && ProjectArrhythmia.State.IsInLobby)
                SteamLobbyManager.inst.ClearInputReady();

            if (MenuConfig.Instance.PlayInputSelectMusic.Value)
            {
                SoundManager.inst.PlayMusic(DefaultMusic.loading);
                CoreHelper.Notify($"Now playing: Creo - Staring Down the Barrels", InterfaceManager.inst.CurrentTheme.guiColor);
            }

            Current = new InputSelectInterface();
            InterfaceManager.inst.CurrentInterface = Current;
        }

        IEnumerator ChangeText()
        {
            for (int i = 0; i < nanobots.Count; i++)
            {
                if (UnityRandom.value < 0.5f)
                {
                    noTexts[i] = $"<{LSText.randomHex("666666")}>{LSText.randomString(36)}</color>";
                    noColors[i] = LSText.randomHex("666666");
                }
            }
            yield return CoroutineHelper.Seconds(UnityRandom.Range(0f, 0.4f));
            changeTextCoroutine = CoroutineHelper.StartCoroutine(ChangeText());
            yield break;
        }

        void UpdateText(bool assignToUI = true)
        {
            var playerColors = InterfaceManager.inst.CurrentTheme.playerColors;
            for (int i = 0; i < nanobots.Count; i++)
            {
                string text;
                if (PlayerManager.inst.players.Count > i && PlayerManager.inst.players[i].active)
                {
                    var customPlayer = PlayerManager.inst.players[i];

                    string textColor = "#" + RTColors.ColorToHex(playerColors[customPlayer.index % playerColors.Count]);
                    string info;
                    if (customPlayer.IsLocalPlayer)
                    {
                        bool soloLocal = PlayerManager.inst.players.Count(x => x.IsLocalPlayer) == 1;
                        string device = soloLocal ? "Hybrid" : customPlayer.deviceType.ToString();
                        if (!soloLocal && device != customPlayer.deviceModel)
                            device = customPlayer.deviceType.ToString() + " (" + customPlayer.deviceModel + ")";
                        info = $"<b>Input Device:</b> {device}";
                    }
                    else
                        info = customPlayer.DisplayName;
                    text = customPlayer.index < 4 ?
                        $"<{textColor}><size=200%>■</color><voffset=0.25em><size=100%> <b>Nanobot:</b> {RTString.ToStoryNumber(customPlayer.index)}    {info}" :
                        $"<{textColor}><size=200%>●</color><voffset=0.25em><size=100%> <b>Nanobot:</b> {RTString.ToStoryNumber(customPlayer.index)}    {info}";
                }
                else
                {
                    string textColor = (noColors.Count > i) ? noColors[i] : "#666666";

                    text = i < 4 ?
                        $"<{textColor}><size=200%>■</color><voffset=0.25em><size=100%> {(noTexts.Count > i ? noTexts[i] : string.Empty)}" :
                        $"<{textColor}><size=200%>●</color><voffset=0.25em><size=100%> {(noTexts.Count > i ? noTexts[i] : string.Empty)}";
                }

                nanobots[i].text = text;
                if (assignToUI && nanobots[i].textUI)
                {
                    nanobots[i].textUI.maxVisibleCharacters = 9999;
                    nanobots[i].textUI.text = text;
                }
            }
        }

        public void Continue()
        {
            InputDataManager.inst.playersCanJoin = false;

            OnInputsSelected?.Invoke();

            if (OnInputsSelected != null) // if we want to run a custom function instead of doing the normal methods.
            {
                OnInputsSelected = null;
                return;
            }

            if (LevelManager.IsArcade)
            {
                SceneHelper.LoadScene(SceneName.Arcade_Select, false);
                return;
            }

            SaveManager.inst.LoadCurrentStoryLevel();
        }

        public void Exit()
        {
            InputDataManager.inst.playersCanJoin = false;
            InputDataManager.inst.ClearInputs();

            OnExit?.Invoke();

            if (OnExit != null)
            {
                OnExit = null;
                return;
            }

            SceneHelper.LoadScene(SceneName.Main_Menu, false);
        }

        public override void OnTick()
        {
            if (generating)
                return;
            if (AllowMultipleLocalPlayers && PlayerManager.inst.players.Count > maxPlayers)
                GrowNanobotSlots(PlayerManager.inst.players.Count);
            if (ProjectArrhythmia.State.IsInLobby && !AllowMultipleLocalPlayers)
                TickLobbyJoin();
            else
                TickLocalJoin();
            UpdateText();
            if (!ProjectArrhythmia.Input.IsUsingInputField && !PlayerManager.inst.players.IsEmpty() && InputDataManager.inst.menuActions.Start.WasPressed)
            {
                if (ProjectArrhythmia.State.IsInLobby && ProjectArrhythmia.State.IsClient)
                {
                    if (!sentInputReady)
                    {
                        sentInputReady = true;
                        CoreHelper.Log($"[InputSelect] Client sending input-ready. players={PlayerManager.inst.players.Count}");
                        NetworkFunction.SendPlayerInputReady();
                        CoreHelper.Notify("Waiting for host to continue...", InterfaceManager.inst.CurrentTheme.guiColor);
                    }
                }
                else if (!ProjectArrhythmia.State.IsInLobby || !ProjectArrhythmia.State.IsHosting || SteamLobbyManager.inst.IsEveryoneInputReady)
                {
                    CoreHelper.Log($"[InputSelect] Host continuing. inLobby={ProjectArrhythmia.State.IsInLobby} hosting={ProjectArrhythmia.State.IsHosting} everyoneReady={(ProjectArrhythmia.State.IsInLobby && ProjectArrhythmia.State.IsHosting ? SteamLobbyManager.inst.IsEveryoneInputReady.ToString() : "n/a")}");
                    Continue();
                }
                else
                    CoreHelper.Notify("Waiting for all players to select their inputs...", InterfaceManager.inst.CurrentTheme.guiColor);
            }
        }
        void GrowNanobotSlots(int newCount)
        {
            CoreHelper.Log($"[InputSelect] Growing nanobot slots. from={maxPlayers} to={newCount}");
            for (int i = nanobots.Count; i < newCount; i++)
            {
                var menuText = new MenuText
                {
                    id = LSText.randomNumString(16),
                    name = "Text",
                    text = string.Empty,
                    parentLayout = "nanobots",
                    length = 0.01f,
                    rect = RectValues.Default.SizeDelta(300f, 46f),
                    hideBG = true,
                    textColor = 6,
                    opacity = 1f,
                    regenerate = false,
                };
                elements.Add(menuText);
                nanobots.Add(menuText);
                noTexts.Add($"<color={LSText.randomHex("666666")}>{LSText.randomString(36)}</color>");
                noColors.Add(LSText.randomHex("666666"));
            }
            maxPlayers = newCount;
            UpdateText(false);
            regenerate = true;
            StartGeneration();
        }
        void TickLocalJoin()
        {
            if (PlayerManager.inst.players.Count >= maxPlayers)
                return;
            if (PlayerInput.controllerListener && PlayerInput.controllerListener.Join.WasPressed)
            {
                var activeDevice = InputManager.ActiveDevice;
                var notConnected = PlayerManager.inst.DeviceNotConnected(activeDevice);
                CoreHelper.Log($"[InputSelect] Controller Join pressed. device={activeDevice?.Name} notConnected={notConnected} players={PlayerManager.inst.players.Count} (local={PlayerManager.inst.players.FindAll(x => x.IsLocalPlayer).Count})");
                if (notConnected)
                {
                    PlayerManager.inst.players.Add(new PAPlayer(PlayerManager.inst.players.Count, activeDevice));
                    CoreHelper.Log($"[InputSelect] Added controller player. total={PlayerManager.inst.players.Count}");
                    SyncPlayers();
                }
            }
            if (PlayerInput.keyboardListener && PlayerInput.keyboardListener.Join.WasPressed)
            {
                var notConnected = PlayerManager.inst.KeyboardNotConnected();
                CoreHelper.Log($"[InputSelect] Keyboard Join pressed. notConnected={notConnected} players={PlayerManager.inst.players.Count} (local={PlayerManager.inst.players.FindAll(x => x.IsLocalPlayer).Count})");
                if (notConnected)
                {
                    PlayerManager.inst.players.Add(new PAPlayer(PlayerManager.inst.players.Count, null));
                    CoreHelper.Log($"[InputSelect] Added keyboard player. total={PlayerManager.inst.players.Count}");
                    SyncPlayers();
                }
            }
        }
        void TickLobbyJoin()
        {
            var localPlayer = PlayerManager.inst.players.Find(x => x.IsLocalPlayer);
            if (localPlayer != null)
                return;
            var controllerPressed = PlayerInput.controllerListener && PlayerInput.controllerListener.Join.WasPressed;
            var keyboardPressed = PlayerInput.keyboardListener && PlayerInput.keyboardListener.Join.WasPressed;
            if (!controllerPressed && !keyboardPressed)
                return;
            CoreHelper.Log($"[InputSelect] Join pressed (lobby), creating hybrid local player. viaController={controllerPressed} viaKeyboard={keyboardPressed} players={PlayerManager.inst.players.Count}");
            PlayerManager.inst.players.Add(new PAPlayer(PlayerManager.inst.players.Count, null));
            CoreHelper.Log($"[InputSelect] Added hybrid player. total={PlayerManager.inst.players.Count}");
            SyncPlayers();
        }

        void SyncPlayers()
        {
            if (!ProjectArrhythmia.State.IsInLobby)
                return;
            CoreHelper.Log($"[InputSelect] SyncPlayers. isClient={ProjectArrhythmia.State.IsClient} players={PlayerManager.inst.players.Count} ids=[{string.Join(",", PlayerManager.inst.players.Select(x => $"{x.id}:{x.IsLocalPlayer}"))}]");
            if (ProjectArrhythmia.State.IsClient)
                SteamLobbyManager.inst.SyncPlayersToServer();
            else
                SteamLobbyManager.inst.SyncPlayersToClients();
        }

        public override void Clear()
        {
            base.Clear();
            if (changeTextCoroutine != null)
                CoroutineHelper.StopCoroutine(changeTextCoroutine);
        }

        #endregion
    }
}
