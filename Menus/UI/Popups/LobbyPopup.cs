using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using SteamworksFacepunch;
using SteamworksFacepunch.Data;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

using Image = UnityEngine.UI.Image;

namespace BetterLegacy.Menus.UI.Popups
{
    /// <summary>
    /// Popup for viewing, joining and creating online lobbies.
    /// </summary>
    public class LobbyPopup : PopupBase
    {
        #region Values

        public static LobbyPopup Instance { get; set; }

        public LobbyTab CurrentTab { get; set; } = LobbyTab.List;
        public enum LobbyTab
        {
            Create,
            List,
            Random,
        }

        public Transform tabs;
        public Transform content;
        public string searchTerm;

        public List<GameObject> tabObjects = new List<GameObject>();

        public InputField nameField;

        public InputFieldStorage playerCountField;

        public Dropdown visibilityDropdown;

        public Button createButton;

        public bool loadingLobbies;

        public GameObject lobbyView;

        public Transform playersParent;

        public Button closeLobbyButton;

        public Text closeLobbyLabel;

        int tickCount;

        public const int MAX_LOBBIES_PER_PAGE = 14;

        #endregion

        #region Functions

        public override void Init()
        {
            Instance = this;
            gameObject = Creator.NewUIObject(nameof(LobbyPopup), Parent);
            RectValues.Default.SizeDelta(1000f, 800f).AssignToRectTransform(gameObject.transform.AsRT());
            var configBaseImage = gameObject.AddComponent<Image>();

            EditorThemeManager.ApplyGraphic(configBaseImage, ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Bottom);

            InitDragging();
            InitTopPanel();
            InitTitle("popups.lobby.title", "Lobbies");
            InitCloseButton();

            var tabs = Creator.NewUIObject("Tabs", gameObject.transform);
            new RectValues(Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, 0.5f), new Vector2(132f, 0f)).AssignToRectTransform(tabs.transform.AsRT());

            var subTabsImage = tabs.AddComponent<Image>();
            EditorThemeManager.ApplyGraphic(subTabsImage, ThemeGroup.Background_2, true);

            var subTabsVerticalLayout = tabs.AddComponent<VerticalLayoutGroup>();
            subTabsVerticalLayout.childControlHeight = false;
            subTabsVerticalLayout.childForceExpandHeight = false;

            this.tabs = tabs.transform;

            var values = EnumHelper.GetValues<LobbyTab>();
            for (int i = 0; i < values.Length; i++)
            {
                var value = values[i];

                var tab = Creator.NewUIObject($"Tab {i}", this.tabs);
                tab.transform.AsRT().sizeDelta = new Vector2(0f, 32f);

                var tabBase = Creator.NewUIObject("Image", tab.transform);
                RectValues.FullAnchored.SizeDelta(-8f, -8f).AssignToRectTransform(tabBase.transform.AsRT());
                var tabBaseImage = tabBase.AddComponent<Image>();

                var tabTitle = Creator.NewUIObject("Title", tabBase.transform);
                RectValues.FullAnchored.AssignToRectTransform(tabTitle.transform.AsRT());
                var tabTitleText = tabTitle.AddComponent<Text>();
                tabTitleText.alignment = TextAnchor.MiddleCenter;
                tabTitleText.font = Font.GetDefault();
                tabTitleText.fontSize = 15;
                tabTitleText.text = value.ToString();

                var tabButton = tabBase.AddComponent<Button>();
                tabButton.image = tabBaseImage;
                tabButton.onClick.NewListener(() =>
                {
                    if (value == LobbyTab.Random)
                    {
                        SteamLobbyManager.inst.JoinRandomLobby();
                        return;
                    }

                    CurrentTab = value;
                    Render();
                });

                EditorThemeManager.ApplySelectable(tabButton, ThemeGroup.Function_2);
                EditorThemeManager.ApplyGraphic(tabTitleText, ThemeGroup.Function_2_Text);

                var tabObject = Creator.NewUIObject(value.ToString(), gameObject.transform);
                tabObjects.Add(tabObject);
                tabObject.SetActive(CurrentTab == value);
                switch (value)
                {
                    case LobbyTab.Create: {
                            RectValues.Default.SizeDelta(0f, 0f).AssignToRectTransform(tabObject.transform.AsRT());

                            var nameLabel = GenerateText(tabObject.transform, "Lobby Name", RectValues.Default.AnchoredPosition(-200f, 300f).SizeDelta(300f, 32f));
                            EditorThemeManager.ApplyLightText(nameLabel);

                            RectValues.FullAnchored.AssignToRectTransform(tabObject.transform.AsRT());
                            var name = numberFieldStorage.transform.Find("input").gameObject.Duplicate(tabObject.transform);
                            name.SetActive(true);
                            RectValues.Default.AnchoredPosition(0f, 300f).SizeDelta(400f, 32f).AssignToRectTransform(name.transform.AsRT());
                            nameField = name.GetComponent<InputField>();
                            nameField.textComponent.alignment = TextAnchor.MiddleLeft;
                            nameField.SetTextWithoutNotify(CoreConfig.Instance.DisplayName.Value);
                            nameField.onValueChanged.ClearAll();
                            nameField.onEndEdit.NewListener(_val =>
                            {
                                if (string.IsNullOrEmpty(_val))
                                {
                                    nameField.text = SteamLobbyManager.inst.LobbySettings.Name;
                                    SteamLobbyManager.Log($"Lobby cannot have an empty name!");
                                    return;
                                }

                                SteamLobbyManager.inst.LobbySettings.Name = _val;
                                SteamLobbyManager.inst.SaveLobbySettings();
                            });
                            nameField.GetPlaceholderText().text = "Set name...";
                            EditorThemeManager.ApplyInputField(nameField, ThemeGroup.Search_Field_1);

                            var playerCountLabel = GenerateText(tabObject.transform, "Player Count", RectValues.Default.AnchoredPosition(-200f, 200f).SizeDelta(300f, 32f));
                            EditorThemeManager.ApplyLightText(playerCountLabel);

                            playerCountField = numberFieldStorage.Duplicate(tabObject.transform, "Player Count").GetComponent<InputFieldStorage>();
                            playerCountField.gameObject.SetActive(true);
                            RectValues.Default.AnchoredPosition(0f, 200f).SizeDelta(400f, 32f).AssignToRectTransform(playerCountField.transform.AsRT());
                            playerCountField.SetTextWithoutNotify(LobbySettings.MAX_PLAYER_COUNT.ToString());
                            playerCountField.OnValueChanged.NewListener(_val =>
                            {
                                if (!int.TryParse(_val, out int num))
                                    return;
                                SteamLobbyManager.inst.LobbySettings.PlayerCount = num;
                                SteamLobbyManager.inst.SaveLobbySettings();
                            });

                            TriggerHelper.IncreaseDecreaseButtonsInt(playerCountField, min: LobbySettings.MIN_PLAYER_COUNT, max: LobbySettings.MAX_PLAYER_COUNT);
                            TriggerHelper.AddEventTriggers(playerCountField.gameObject, TriggerHelper.ScrollDeltaInt(playerCountField.inputField, min: LobbySettings.MIN_PLAYER_COUNT, max: LobbySettings.MAX_PLAYER_COUNT));

                            EditorThemeManager.ApplyInputField(playerCountField);

                            #region Visibility

                            var visibilityLabel = GenerateText(tabObject.transform, "Visibility", RectValues.Default.AnchoredPosition(-200f, 100f).SizeDelta(300f, 32f));
                            EditorThemeManager.ApplyLightText(visibilityLabel);

                            var visibility = UIManager.GenerateDropdown("Dropdown", tabObject.transform);
                            visibilityDropdown = visibility.dropdown;
                            var visibilityHide = visibility.hideOptions;

                            RectValues.Default.AnchoredPosition(0f, 100f).SizeDelta(400f, 32f).AssignToRectTransform(visibility.transform.AsRT());

                            visibilityDropdown.onValueChanged.ClearAll();
                            visibilityDropdown.options.Clear();
                            visibilityHide.DisabledOptions = new List<bool>();
                            visibilityHide.remove = true;

                            var visibilities = EnumHelper.GetValues<LobbyVisibility>();

                            for (int j = 0; j < visibilities.Length; j++)
                                visibilityDropdown.options.Add(new Dropdown.OptionData(visibilities[j].ToString()));

                            visibilityDropdown.SetValueWithoutNotify(0);
                            visibilityDropdown.onValueChanged.NewListener(_val =>
                            {
                                SteamLobbyManager.inst.LobbySettings.Visibility = (LobbyVisibility)_val;
                                SteamLobbyManager.inst.SaveLobbySettings();
                            });

                            EditorThemeManager.ApplyDropdown(visibilityDropdown);

                            #endregion

                            var create = Creator.NewUIObject("Create Lobby", tabObject.transform);
                            RectValues.Default.AnchoredPosition(0f, -300f).SizeDelta(400f, 32f).AssignToRectTransform(create.transform.AsRT());
                            var createImage = create.AddComponent<Image>();
                            createButton = create.AddComponent<Button>();
                            createButton.image = createImage;
                            createButton.onClick.NewListener(() => SteamLobbyManager.inst.CreateLobby());

                            var labelText = GenerateText(create.transform, "Create Lobby", RectValues.FullAnchored.SizeDelta(-12f, 0f), TextAnchor.MiddleCenter);

                            EditorThemeManager.ApplyGraphic(createImage, ThemeGroup.Function_1, true);
                            EditorThemeManager.ApplyGraphic(labelText, ThemeGroup.Function_1_Text);
                            break;
                        }
                    case LobbyTab.List: {
                            RectValues.FullAnchored.AssignToRectTransform(tabObject.transform.AsRT());
                            var searchField = numberFieldStorage.transform.Find("input").gameObject.Duplicate(tabObject.transform);
                            searchField.SetActive(true);
                            RectValues.LeftAnchored.AnchoredPosition(134f, 0f).SizeDelta(856f, 32f).AssignToRectTransform(searchField.transform.AsRT());
                            var searchFieldInput = searchField.GetComponent<InputField>();
                            searchFieldInput.textComponent.alignment = TextAnchor.MiddleLeft;
                            searchFieldInput.SetTextWithoutNotify(string.Empty);
                            searchFieldInput.onValueChanged.NewListener(_val => searchTerm = _val);
                            searchFieldInput.onEndEdit.NewListener(_val =>
                            {
                                searchTerm = _val;
                                Render();
                            });
                            searchFieldInput.GetPlaceholderText().text = "Search lobby...";
                            EditorThemeManager.ApplyInputField(searchFieldInput, ThemeGroup.Search_Field_1);

                            content = Creator.NewUIObject("Content", tabObject.transform).transform;
                            var contentVerticalLayoutGroup = content.gameObject.AddComponent<VerticalLayoutGroup>();
                            contentVerticalLayoutGroup.spacing = 8f;
                            contentVerticalLayoutGroup.childControlHeight = false;
                            contentVerticalLayoutGroup.childForceExpandHeight = false;
                            new RectValues(Vector2.zero, new Vector2(0.995f, 0.95f), new Vector2(0.136f, 0.136f), new Vector2(0.5f, 0.5f), Vector2.zero).AssignToRectTransform(content.AsRT());

                            break;
                        }
                    case LobbyTab.Random: {
                            break;
                        }
                }
            }

            #region Lobby View

            lobbyView = Creator.NewUIObject("View", gameObject.transform);
            RectValues.Default.SizeDelta(0f, 0f).AssignToRectTransform(lobbyView.transform.AsRT());

            playersParent = Creator.NewUIObject("Players", lobbyView.transform).transform;
            var playersParentVerticalLayoutGroup = playersParent.gameObject.AddComponent<VerticalLayoutGroup>();
            playersParentVerticalLayoutGroup.spacing = 8f;
            playersParentVerticalLayoutGroup.childControlHeight = false;
            playersParentVerticalLayoutGroup.childForceExpandHeight = false;
            RectValues.Default.AnchoredPosition(0f, 0f).SizeDelta(400f, 800f).AssignToRectTransform(playersParent.AsRT());

            var closeLobby = Creator.NewUIObject("Close Lobby", lobbyView.transform);
            RectValues.Default.AnchoredPosition(0f, -300f).SizeDelta(400f, 32f).AssignToRectTransform(closeLobby.transform.AsRT());
            var closeLobbyImage = closeLobby.AddComponent<Image>();
            closeLobbyButton = closeLobby.AddComponent<Button>();
            closeLobbyButton.image = closeLobbyImage;
            closeLobbyButton.onClick.NewListener(() =>
            {
                if (ProjectArrhythmia.State.IsHosting)
                    RTSteamManager.inst.EndServer();
                else
                {
                    RTSteamManager.inst.EndClient();
                    PlayerManager.Players.ForLoopReverse(player =>
                    {
                        if (player.ID != RTSteamManager.inst.steamUser.steamID)
                            PlayerManager.RemovePlayer(player);
                    });
                }
            });

            closeLobbyLabel = GenerateText(closeLobby.transform, "Close Lobby", RectValues.FullAnchored.SizeDelta(-12f, 0f), TextAnchor.MiddleCenter);

            EditorThemeManager.ApplyGraphic(closeLobbyImage, ThemeGroup.Delete, true);
            EditorThemeManager.ApplyGraphic(closeLobbyLabel, ThemeGroup.Delete_Text);

            lobbyView.SetActive(false);

            #endregion

            Close();
        }

        public override void Render()
        {
            if (!SteamLobbyManager.inst)
                return;

            lobbyView.SetActive(ProjectArrhythmia.State.IsInLobby);
            if (ProjectArrhythmia.State.IsInLobby)
            {
                closeLobbyLabel.text = ProjectArrhythmia.State.IsHosting ? "Close Lobby" : "Leave Lobby";
                tabObjects.ForLoop(gameObject => gameObject.SetActive(false));
                LSHelpers.DeleteChildren(playersParent);
                foreach (var member in SteamLobbyManager.inst.CurrentLobby.Members)
                {
                    var gameObject = Creator.NewUIObject("Member", playersParent);
                    gameObject.transform.AsRT().sizeDelta = new Vector2(830f, 38f);

                    // add hover ui here

                    var image = gameObject.AddComponent<Image>();
                    var button = gameObject.AddComponent<Button>();
                    button.image = image;
                    button.onClick.NewListener(() =>
                    {
                        SteamLobbyManager.Log($"ID: {member.Id}\n" +
                            $"Name: {member.Name}\n" +
                            $"Nickname: {member.Nickname}");
                        SoundManager.inst.PlaySound(DefaultSounds.blip);
                    });

                    var label = GenerateText(gameObject.transform, member.Nickname ?? member.Name ?? member.Id.ToString(), RectValues.FullAnchored.SizeDelta(-12f, 0f));

                    EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
                    EditorThemeManager.ApplyLightText(label);
                }
                return;
            }

            tabObjects.ForLoop((gameObject, index) => gameObject.SetActive(index == (int)CurrentTab));
            switch (CurrentTab)
            {
                case LobbyTab.Create: {
                        break;
                    }
                case LobbyTab.List: {
                        GetLobbies();
                        break;
                    }
                case LobbyTab.Random: {
                        break;
                    }
            }
        }

        async void GetLobbies()
        {
            if (loadingLobbies)
                return;
            loadingLobbies = true;

            var query = new LobbyQuery().WithMaxResults(MAX_LOBBIES_PER_PAGE);
            if (!string.IsNullOrEmpty(searchTerm))
                query.WithKeyValue("LobbyName", searchTerm);
            var lobbies = await query.RequestAsync();
            LegacyPlugin.MainTick += () =>
            {
                loadingLobbies = false;
                LSHelpers.DeleteChildren(content);
                if (lobbies == null)
                {
                    SteamLobbyManager.LogError($"Could not get lobbies!");
                    return;
                }

                for (int i = 0; i < lobbies.Length; i++)
                {
                    var lobby = lobbies[i];
                    SteamLobbyManager.Log($"Lobby {i}\n" +
                        $"ID: {lobby.Id}\n" +
                        $"Owner: {lobby.Owner.Name} - {lobby.Owner.Id}");
                    if (!SteamLobbyManager.inst.IsValidLobby(lobby))
                    {
                        SteamLobbyManager.Log($"Lobby is not valid, so cannot join.");
                        continue;
                    }
                    var gameObject = Creator.NewUIObject("Lobby", content);
                    gameObject.transform.AsRT().sizeDelta = new Vector2(830f, 38f);

                    // add hover ui here

                    var image = gameObject.AddComponent<Image>();
                    var button = gameObject.AddComponent<Button>();
                    button.image = image;
                    button.onClick.NewListener(() => SteamLobbyManager.inst.JoinLobby(lobby));

                    var label = GenerateText(gameObject.transform, lobby.GetName() ?? "Invalid", RectValues.FullAnchored.SizeDelta(-12f, 0f));

                    EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
                    EditorThemeManager.ApplyLightText(label);
                }
            };
        }

        public override void Tick()
        {
            // update list every 1000 ticks
            if (!Active || CurrentTab != LobbyTab.List)
                return;

            if (ProjectArrhythmia.State.IsInLobby)
                return;

            tickCount++;

            if (tickCount % 1000 != 0)
                return;

            Render();
        }

        #endregion
    }
}
