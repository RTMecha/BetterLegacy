using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using SteamworksFacepunch;
using SteamworksFacepunch.Data;

using BetterLegacy.Configs;
using BetterLegacy.Core;
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

        /// <summary>
        /// The current <see cref="LobbyPopup"/> instance.
        /// </summary>
        public static LobbyPopup Instance { get; set; }

        /// <summary>
        /// The currently selected tab.
        /// </summary>
        public LobbyTab CurrentTab { get; set; } = LobbyTab.List;

        /// <summary>
        /// List of tabs to display.
        /// </summary>
        public enum LobbyTab
        {
            /// <summary>
            /// The current lobby view.
            /// </summary>
            Current,
            /// <summary>
            /// Chat with other players in the lobby.
            /// </summary>
            Chat,
            /// <summary>
            /// Creates and hosts a lobby.
            /// </summary>
            Create,
            /// <summary>
            /// Edits the lobby's settings. Takes over "Create" when a lobby is created. 
            Edit,
            /// <summary>
            /// List of lobbies to join.
            /// </summary>
            List,
            /// <summary>
            /// Manages player settings.
            /// </summary>
            Settings,
        }

        public Transform tabs;
        public Transform lobbyContent;
        public Transform playerSettingsContent;
        public Transform lobbySettingsContent;
        public string searchTerm;

        public List<GameObject> tabObjects = new List<GameObject>();
        public Text createTabTitle;
        static Sprite checkmarkSprite;

        public InputField nameField;

        public InputField passwordField;
        GameObject passwordPrompt;
        InputField promptInput;
        Text promptTitle;
        Text promptStatus;
        Lobby promptLobby;
        public InputFieldStorage playerCountField;

        public Dropdown visibilityDropdown;

        public Button createButton;

        public bool loadingLobbies;

        public Transform playersParent;

        public Button closeLobbyButton;

        public Text closeLobbyLabel;

        int tickCount;

        public const int MAX_LOBBIES_PER_PAGE = 14;

        static Sprite closeSprite;

        public GameObject currentTabButton;

        #region Chat

        public const string SYSTEM_NAME = "System";
        public const string SYSTEM_COLOR_HEX = "F0756BFF";

        public GameObject chatTabButton;

        public Transform chatContent;

        public RectTransform chatViewportRT;

        public InputField chatInput;

        public RectTransform chatInputRT;

        public readonly List<ChatMessage> chatMessages = new List<ChatMessage>();
        readonly List<GameObject> chatCards = new List<GameObject>();
        public const int MAX_CHAT_MESSAGES = 500;
        const float CHAT_INPUT_MIN_HEIGHT = 40f;
        const float CHAT_INPUT_MAX_HEIGHT = 160f;
        const float CHAT_INPUT_BOTTOM_OFFSET = 6f;
        const float CHAT_CARD_WIDTH = 830f;
        static int CHAT_FONT_SIZE => MenuConfig.Instance.ChatFontSize.Value;
        const float CHAT_SCROLL_SPEED = 30f;
        static readonly UnityEngine.Color CHAT_BACKGROUND = new UnityEngine.Color(0.06f, 0.06f, 0.06f, 1f);

        readonly Dictionary<ChatMessage, bool> referenceValidity = new Dictionary<ChatMessage, bool>();
        float nextReferenceRefresh;
        const float REFERENCE_OUTLINE_THICKNESS = 1.5f;
        const float REFERENCE_OUTLINE_HOVER_THICKNESS = 3f;

        readonly List<(Text text, Func<string> build)> chatTimeUpdaters = new List<(Text, Func<string>)>();
        float nextChatTimeRefresh;

        #endregion

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
            InitTitle("popups.lobby.title", "Lobby Manager");
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
                tabTitleText.text = Lang.Current.GetOrDefault("popups.lobby." + value.ToString().ToLower(), value.ToString());

                if (value == LobbyTab.Current)
                {
                    currentTabButton = tab;
                    tab.SetActive(false);
                }
                if (value == LobbyTab.Create)
                    createTabTitle = tabTitleText;
                if (value == LobbyTab.Edit)
                    tab.SetActive(false);
                if (value == LobbyTab.Chat)
                {
                    chatTabButton = tab;
                    tab.SetActive(false);
                }

                var tabButton = tabBase.AddComponent<Button>();
                tabButton.image = tabBaseImage;
                tabButton.onClick.NewListener(() =>
                {
                    if (value == LobbyTab.Create)
                    {
                        SetTab(ProjectArrhythmia.State.IsInLobby ? LobbyTab.Edit : LobbyTab.Create);
                        return;
                    }
                    SetTab(value);
                });

                EditorThemeManager.ApplySelectable(tabButton, ThemeGroup.Function_2);
                EditorThemeManager.ApplyGraphic(tabTitleText, ThemeGroup.Function_2_Text);

                var tabObject = Creator.NewUIObject(value.ToString(), gameObject.transform);
                tabObjects.Add(tabObject);
                tabObject.SetActive(CurrentTab == value);
                RectValues.FullAnchored.AssignToRectTransform(tabObject.transform.AsRT());

                switch (value)
                {
                    case LobbyTab.Current: {
                            playersParent = Creator.NewUIObject("Players", tabObject.transform).transform;
                            var playersParentVerticalLayoutGroup = playersParent.gameObject.AddComponent<VerticalLayoutGroup>();
                            playersParentVerticalLayoutGroup.spacing = 8f;
                            playersParentVerticalLayoutGroup.childControlHeight = false;
                            playersParentVerticalLayoutGroup.childForceExpandHeight = false;
                            RectValues.Default.AnchoredPosition(0f, 0f).SizeDelta(400f, 800f).AssignToRectTransform(playersParent.AsRT());

                            var closeLobby = Creator.NewUIObject("Close Lobby", tabObject.transform);
                            RectValues.Default.AnchoredPosition(0f, -300f).SizeDelta(400f, 32f).AssignToRectTransform(closeLobby.transform.AsRT());
                            var closeLobbyImage = closeLobby.AddComponent<Image>();
                            closeLobbyButton = closeLobby.AddComponent<Button>();
                            closeLobbyButton.image = closeLobbyImage;
                            closeLobbyButton.onClick.NewListener(() =>
                            {
                                LegacyPlugin.CanEdit = true;
                                if (ProjectArrhythmia.State.IsHosting)
                                {
                                    RTSteamManager.inst.EndServer();
                                    SetTab(LobbyTab.Create);
                                }
                                else
                                {
                                    RTSteamManager.inst.EndClient();
                                    PlayerManager.inst.players.ForLoopReverse(player =>
                                    {
                                        if (player.ID != RTSteamManager.inst.steamUser.steamID)
                                            PlayerManager.inst.RemovePlayer(player);
                                    });
                                    SetTab(LobbyTab.List);
                                }
                                SteamLobbyManager.inst.ClearLoaded();
                            });

                            closeLobbyLabel = GenerateText(closeLobby.transform, "Close Lobby", RectValues.FullAnchored.SizeDelta(-12f, 0f), TextAnchor.MiddleCenter);

                            EditorThemeManager.ApplyGraphic(closeLobbyImage, ThemeGroup.Delete, true);
                            EditorThemeManager.ApplyGraphic(closeLobbyLabel, ThemeGroup.Delete_Text);
                            break;
                        }
                    case LobbyTab.Chat: {
                            var viewport = Creator.NewUIObject("Chat Viewport", tabObject.transform);
                            new RectValues(Vector2.zero, new Vector2(0.995f, 0.98f), new Vector2(0.136f, 0.08f), new Vector2(0.5f, 0.5f), Vector2.zero).AssignToRectTransform(viewport.transform.AsRT());
                            chatViewportRT = viewport.transform.AsRT();
                            var viewportImage = viewport.AddComponent<Image>();
                            EditorThemeManager.ApplyGraphic(viewportImage, ThemeGroup.Background_2, true);
                            viewport.AddComponent<RectMask2D>();
                            var content = Creator.NewUIObject("Content", viewport.transform);
                            chatContent = content.transform;
                            var contentRT = content.transform.AsRT();
                            contentRT.anchorMin = new Vector2(0f, 0f);
                            contentRT.anchorMax = new Vector2(1f, 0f);
                            contentRT.pivot = new Vector2(0.5f, 0f);
                            contentRT.sizeDelta = Vector2.zero;
                            contentRT.anchoredPosition = Vector2.zero;
                            var contentVerticalLayoutGroup = content.AddComponent<VerticalLayoutGroup>();
                            contentVerticalLayoutGroup.spacing = 6f;
                            contentVerticalLayoutGroup.childAlignment = TextAnchor.LowerLeft;
                            contentVerticalLayoutGroup.childControlHeight = false;
                            contentVerticalLayoutGroup.childForceExpandHeight = false;
                            var contentSizeFitter = content.AddComponent<ContentSizeFitter>();
                            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                            var scrollTrigger = viewport.AddComponent<EventTrigger>();
                            var scrollEntry = new EventTrigger.Entry { eventID = EventTriggerType.Scroll };
                            scrollEntry.callback.AddListener(data => ScrollChat(((PointerEventData)data).scrollDelta.y));
                            scrollTrigger.triggers.Add(scrollEntry);
                            var inputObj = numberFieldStorage.transform.Find("input").gameObject.Duplicate(tabObject.transform);
                            inputObj.SetActive(true);
                            chatInputRT = inputObj.transform.AsRT();
                            new RectValues(new Vector2(0f, CHAT_INPUT_BOTTOM_OFFSET), new Vector2(1f, 0f), new Vector2(0.136f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, CHAT_INPUT_MIN_HEIGHT)).AssignToRectTransform(chatInputRT);
                            chatInput = inputObj.GetComponent<InputField>();
                            chatInput.textComponent.alignment = TextAnchor.LowerLeft;
                            chatInput.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
                            chatInput.textComponent.verticalOverflow = VerticalWrapMode.Overflow;
                            chatInput.lineType = InputField.LineType.MultiLineNewline;
                            chatInput.characterLimit = 0;
                            chatInput.SetTextWithoutNotify(string.Empty);
                            chatInput.onValueChanged.NewListener(_val => UpdateChatInputHeight());
                            chatInput.onValidateInput = (text, charIndex, addedChar) =>
                            {
                                if (addedChar == '\n' && !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
                                {
                                    SendChatFromInput();
                                    return '\0';
                                }
                                return addedChar;
                            };
                            chatInput.GetPlaceholderText().text = "Message...";
                            EditorThemeManager.ApplyInputField(chatInput, ThemeGroup.Search_Field_1);

                            break;
                        }
                    case LobbyTab.Create: {
                            #region Name

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
                                LobbySettingsChanged();
                            });
                            nameField.GetPlaceholderText().text = "Set name...";
                            EditorThemeManager.ApplyInputField(nameField, ThemeGroup.Search_Field_1);

                            #endregion

                            #region Player Count

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
                                LobbySettingsChanged();
                                if (ProjectArrhythmia.State.IsInLobby)
                                {
                                    var lobby = SteamLobbyManager.inst.CurrentLobby;
                                    lobby.MaxMembers = num;
                                }
                            });

                            TriggerHelper.IncreaseDecreaseButtonsInt(playerCountField, min: LobbySettings.MIN_PLAYER_COUNT, max: LobbySettings.MAX_PLAYER_COUNT);
                            TriggerHelper.AddEventTriggers(playerCountField.gameObject, TriggerHelper.ScrollDeltaInt(playerCountField.inputField, min: LobbySettings.MIN_PLAYER_COUNT, max: LobbySettings.MAX_PLAYER_COUNT));

                            EditorThemeManager.ApplyInputField(playerCountField);

                            #endregion

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
                                LobbySettingsChanged();
                            });

                            EditorThemeManager.ApplyDropdown(visibilityDropdown);

                            #endregion

                            #region Password

                            var passwordLabel = GenerateText(tabObject.transform, "Password", RectValues.Default.AnchoredPosition(-200f, 0f).SizeDelta(300f, 32f));
                            EditorThemeManager.ApplyLightText(passwordLabel);
                            passwordField = CreatePasswordInput(tabObject.transform, 0f, 400f, "(No Password)");
                            passwordField.SetTextWithoutNotify(string.Empty);
                            passwordField.onEndEdit.NewListener(_val =>
                            {
                                SteamLobbyManager.inst.LobbySettings.Password = _val ?? string.Empty;
                                SteamLobbyManager.inst.SaveLobbySettings();
                            });

                            #endregion

                            var create = Creator.NewUIObject("Create Lobby", tabObject.transform);
                            RectValues.Default.AnchoredPosition(0f, -300f).SizeDelta(400f, 32f).AssignToRectTransform(create.transform.AsRT());
                            var createImage = create.AddComponent<Image>();
                            createButton = create.AddComponent<Button>();
                            createButton.image = createImage;
                            createButton.onClick.NewListener(() =>
                            {
                                SteamLobbyManager.inst.CreateLobby();
                                SetTab(LobbyTab.Current);
                            });

                            var labelText = GenerateText(create.transform, "Create Lobby", RectValues.FullAnchored.SizeDelta(-12f, 0f), TextAnchor.MiddleCenter);

                            EditorThemeManager.ApplyGraphic(createImage, ThemeGroup.Function_1, true);
                            EditorThemeManager.ApplyGraphic(labelText, ThemeGroup.Function_1_Text);
                            break;
                        }
                    case LobbyTab.Edit: {
                            lobbySettingsContent = Creator.NewUIObject("Content", tabObject.transform).transform;
                            var contentVerticalLayoutGroup = lobbySettingsContent.gameObject.AddComponent<VerticalLayoutGroup>();
                            contentVerticalLayoutGroup.spacing = 4f;
                            contentVerticalLayoutGroup.childControlHeight = false;
                            contentVerticalLayoutGroup.childForceExpandHeight = false;
                            new RectValues(Vector2.zero, new Vector2(0.995f, 0.98f), new Vector2(0.136f, 0.02f), new Vector2(0.5f, 0.5f), Vector2.zero).AssignToRectTransform(lobbySettingsContent.AsRT());

                            break;
                        }
                    case LobbyTab.List: {
                            var searchField = numberFieldStorage.transform.Find("input").gameObject.Duplicate(tabObject.transform);
                            searchField.SetActive(true);
                            RectValues.LeftAnchored.AnchoredPosition(134f, 0f).SizeDelta(724f, 32f).AssignToRectTransform(searchField.transform.AsRT());
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

                            var random = Creator.NewUIObject("Random", tabObject.transform);
                            RectValues.RightAnchored.SizeDelta(132f, 32f).AssignToRectTransform(random.transform.AsRT());

                            var randomBase = Creator.NewUIObject("Image", random.transform);
                            RectValues.FullAnchored.SizeDelta(-8f, -8f).AssignToRectTransform(randomBase.transform.AsRT());
                            var randomButtonBaseImage = randomBase.AddComponent<Image>();

                            var randomTitle = Creator.NewUIObject("Title", randomBase.transform);
                            RectValues.FullAnchored.AssignToRectTransform(randomTitle.transform.AsRT());
                            var randomTitleText = randomTitle.AddComponent<Text>();
                            randomTitleText.alignment = TextAnchor.MiddleCenter;
                            randomTitleText.font = Font.GetDefault();
                            randomTitleText.fontSize = 15;
                            randomTitleText.text = Lang.Current.GetOrDefault("popups.lobby.random", "Random");

                            var randomButton = randomBase.AddComponent<Button>();
                            randomButton.onClick.NewListener(() =>
                            {
                                if (!ProjectArrhythmia.State.IsInLobby)
                                    SteamLobbyManager.inst.JoinRandomLobby();
                            });

                            EditorThemeManager.ApplySelectable(randomButton, ThemeGroup.Function_2);
                            EditorThemeManager.ApplyGraphic(randomTitleText, ThemeGroup.Function_2_Text);

                            lobbyContent = Creator.NewUIObject("Content", tabObject.transform).transform;
                            var contentVerticalLayoutGroup = lobbyContent.gameObject.AddComponent<VerticalLayoutGroup>();
                            contentVerticalLayoutGroup.spacing = 8f;
                            contentVerticalLayoutGroup.childControlHeight = false;
                            contentVerticalLayoutGroup.childForceExpandHeight = false;
                            new RectValues(Vector2.zero, new Vector2(0.995f, 0.95f), new Vector2(0.136f, 0.136f), new Vector2(0.5f, 0.5f), Vector2.zero).AssignToRectTransform(lobbyContent.AsRT());

                            break;
                        }
                    case LobbyTab.Settings: {
                            playerSettingsContent = Creator.NewUIObject("Content", tabObject.transform).transform;
                            var contentVerticalLayoutGroup = playerSettingsContent.gameObject.AddComponent<VerticalLayoutGroup>();
                            contentVerticalLayoutGroup.spacing = 8f;
                            contentVerticalLayoutGroup.childControlHeight = false;
                            contentVerticalLayoutGroup.childForceExpandHeight = false;
                            new RectValues(Vector2.zero, new Vector2(0.995f, 0.95f), new Vector2(0.136f, 0.136f), new Vector2(0.5f, 0.5f), Vector2.zero).AssignToRectTransform(playerSettingsContent.AsRT());
                            break;
                        }
                }
            }

            Close();
        }

        public override void Render()
        {
            if (!SteamLobbyManager.inst)
                return;

            if (!closeSprite)
                closeSprite = SpriteHelper.LoadSprite(AssetPack.GetFile("core/sprites/icons/operations/close.png"));

            if (currentTabButton)
                currentTabButton.SetActive(ProjectArrhythmia.State.IsInLobby);
            if (chatTabButton)
                chatTabButton.SetActive(ProjectArrhythmia.State.IsInLobby);
            if (!ProjectArrhythmia.State.IsInLobby && CurrentTab == LobbyTab.Chat)
                CurrentTab = LobbyTab.List;
            tabObjects.ForLoop((gameObject, index) => gameObject.SetActive(index == (int)CurrentTab));
            if (createTabTitle)
                createTabTitle.text = ProjectArrhythmia.State.IsInLobby
                    ? Lang.Current.GetOrDefault("popups.lobby.edit", "Edit")
                    : Lang.Current.GetOrDefault("popups.lobby.create", "Create");
            switch (CurrentTab)
            {
                case LobbyTab.Current: {
                        GetTab(LobbyTab.Current).SetActive(ProjectArrhythmia.State.IsInLobby);
                        if (!ProjectArrhythmia.State.IsInLobby)
                            break;

                        closeLobbyLabel.text = ProjectArrhythmia.State.IsHosting ? Lang.Current.GetOrDefault("popups.lobby.close", "Close Lobby") : Lang.Current.GetOrDefault("popups.lobby.leave", "Leave Lobby");
                        LSHelpers.DeleteChildren(playersParent);
                        foreach (var member in SteamLobbyManager.inst.CurrentLobby.Members)
                            GenerateMember(member);
                        break;
                    }
                case LobbyTab.Create: {
                        break;
                    }
                case LobbyTab.Edit: {
                        RenderLobbySettings();
                        break;
                    }
                case LobbyTab.List: {
                        GetLobbies();
                        break;
                    }
                case LobbyTab.Settings: {
                        RenderPlayerSettings();
                        break;
                    }
                case LobbyTab.Chat: {
                        if (chatContent)
                        {
                            LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent.AsRT());
                            SetChatScrollY(0f);
                        }
                        UpdateChatInputHeight();
                        break;
                    }
            }
        }

        async void GetLobbies()
        {
            if (ProjectArrhythmia.State.IsInLobby)
            {
                LegacyPlugin.MainTick += () =>
                {
                    loadingLobbies = false;
                    LSHelpers.DeleteChildren(lobbyContent);
                };
                return;
            }

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
                LSHelpers.DeleteChildren(lobbyContent);
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
                    var gameObject = Creator.NewUIObject("Lobby", lobbyContent);
                    gameObject.transform.AsRT().sizeDelta = new Vector2(830f, 38f);

                    // add hover ui here

                    var image = gameObject.AddComponent<Image>();
                    var button = gameObject.AddComponent<Button>();
                    button.image = image;
                    button.onClick.NewListener(() =>
                    {
                        if (lobby.GetData(SteamLobbyManager.HAS_PASSWORD) == "1")
                            ShowPasswordPrompt(lobby);
                        else
                            SteamLobbyManager.inst.JoinLobby(lobby);
                    });

                    var label = GenerateText(gameObject.transform, (lobby.GetName() ?? "Invalid") + (lobby.GetData(SteamLobbyManager.HAS_PASSWORD) == "1" ? " [Password]" : string.Empty), RectValues.FullAnchored.SizeDelta(-12f, 0f));

                    EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
                    EditorThemeManager.ApplyLightText(label);
                }
            };
        }

        void RenderPlayerSettings()
        {
            LSHelpers.DeleteChildren(playerSettingsContent);
            for (int i = 0; i < PlayerManager.inst.playerSettings.Count; i++)
            {
                var playerSettings = PlayerManager.inst.playerSettings[i];
                var gameObject = Creator.NewUIObject("Settings", playerSettingsContent);
                gameObject.transform.AsRT().sizeDelta = new Vector2(830f, 100f);

                var verticalLayoutGroup = gameObject.AddComponent<VerticalLayoutGroup>();
                verticalLayoutGroup.childControlHeight = false;
                verticalLayoutGroup.childForceExpandHeight = false;
                var contentSizeFitter = gameObject.AddComponent<ContentSizeFitter>();
                contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

                #region Player Index

                var playerIndexLabel = GenerateText(gameObject.transform, Lang.Current.GetOrDefault("popups.lobby.local_player_index", "Local Player Index"), RectValues.Default.SizeDelta(300f, 32f));
                EditorThemeManager.ApplyLightText(playerIndexLabel);

                var playerIndexField = numberFieldStorage.Duplicate(gameObject.transform, "Player Index").GetComponent<InputFieldStorage>();
                playerIndexField.gameObject.SetActive(true);
                RectValues.Default.SizeDelta(400f, 32f).AssignToRectTransform(playerIndexField.transform.AsRT());
                playerIndexField.SetTextWithoutNotify(playerSettings.index.ToString());
                playerIndexField.OnValueChanged.NewListener(_val =>
                {
                    if (!int.TryParse(_val, out int num))
                        return;
                    playerSettings.index = num;
                    SteamLobbyManager.inst.SaveLobbySettings();
                    if (ProjectArrhythmia.State.IsOnlineMultiplayer)
                        NetworkFunction.SendPlayerSettings();
                    if (!ProjectArrhythmia.State.InEditor)
                        return;
                    PlayerManager.inst.RespawnPlayers();
                });

                TriggerHelper.IncreaseDecreaseButtonsInt(playerIndexField, max: int.MaxValue);
                TriggerHelper.AddEventTriggers(playerIndexField.gameObject, TriggerHelper.ScrollDeltaInt(playerIndexField.inputField, max: int.MaxValue));

                EditorThemeManager.ApplyInputField(playerIndexField);

                #endregion

                #region Model ID

                var modelIDLabel = GenerateText(gameObject.transform, Lang.Current.GetOrDefault("popups.lobby.model_id", "Model ID"), RectValues.Default.AnchoredPosition(-200f, 300f).SizeDelta(300f, 32f));
                EditorThemeManager.ApplyLightText(modelIDLabel);

                var modelID = numberFieldStorage.transform.Find("input").gameObject.Duplicate(gameObject.transform);
                modelID.SetActive(true);
                RectValues.Default.AnchoredPosition(0f, 300f).SizeDelta(400f, 32f).AssignToRectTransform(modelID.transform.AsRT());
                var modelIDField = modelID.GetComponent<InputField>();
                modelIDField.textComponent.alignment = TextAnchor.MiddleLeft;
                modelIDField.SetTextWithoutNotify(playerSettings.playerModelID);
                modelIDField.onValueChanged.ClearAll();
                modelIDField.onEndEdit.NewListener(_val =>
                {
                    playerSettings.playerModelID = _val;
                    SteamLobbyManager.inst.SaveLobbySettings();
                    if (PlayerManager.inst.players.TryFind(x => x.localIndex == playerSettings.index, out var player))
                        PlayerManager.inst.RespawnPlayer(player);
                    if (ProjectArrhythmia.State.IsOnlineMultiplayer)
                        NetworkFunction.SendPlayerSettings();
                });
                modelIDField.GetPlaceholderText().text = "Set ID...";
                EditorThemeManager.ApplyInputField(modelIDField, ThemeGroup.Search_Field_1);

                #endregion

                #region Color Slot

                var colorSlotLabel = GenerateText(gameObject.transform, Lang.Current.GetOrDefault("popups.lobby.color_slot", "Color Slot"), RectValues.Default.SizeDelta(300f, 32f));
                EditorThemeManager.ApplyLightText(colorSlotLabel);

                var colorSlotField = numberFieldStorage.Duplicate(gameObject.transform, "Color Slot").GetComponent<InputFieldStorage>();
                colorSlotField.gameObject.SetActive(true);
                RectValues.Default.SizeDelta(400f, 32f).AssignToRectTransform(colorSlotField.transform.AsRT());
                colorSlotField.SetTextWithoutNotify(playerSettings.colorSlot.ToString());
                colorSlotField.OnValueChanged.NewListener(_val =>
                {
                    if (!int.TryParse(_val, out int num))
                        return;
                    playerSettings.colorSlot = num;
                    SteamLobbyManager.inst.SaveLobbySettings();
                    if (!PlayerManager.inst.players.TryFind(x => x.localIndex == playerSettings.index, out var player))
                        return;

                    player.colorSlot = num;
                    if (player.RuntimePlayer)
                        player.RuntimePlayer.colorSlot = num;
                    if (ProjectArrhythmia.State.IsOnlineMultiplayer)
                        NetworkFunction.SendPlayerSettings();
                });

                TriggerHelper.IncreaseDecreaseButtonsInt(colorSlotField, min: -1, max: int.MaxValue);
                TriggerHelper.AddEventTriggers(colorSlotField.gameObject, TriggerHelper.ScrollDeltaInt(colorSlotField.inputField, min: -1, max: int.MaxValue));

                EditorThemeManager.ApplyInputField(colorSlotField);

                #endregion

                #region Display Name

                var displayNameLabel = GenerateText(gameObject.transform, Lang.Current.GetOrDefault("popups.lobby.display_name", "Display Name"), RectValues.Default.AnchoredPosition(-200f, 300f).SizeDelta(300f, 32f));
                EditorThemeManager.ApplyLightText(displayNameLabel);

                var displayName = numberFieldStorage.transform.Find("input").gameObject.Duplicate(gameObject.transform);
                displayName.SetActive(true);
                RectValues.Default.AnchoredPosition(0f, 300f).SizeDelta(400f, 32f).AssignToRectTransform(displayName.transform.AsRT());
                var displayNameField = displayName.GetComponent<InputField>();
                displayNameField.textComponent.alignment = TextAnchor.MiddleLeft;
                displayNameField.SetTextWithoutNotify(playerSettings.displayName);
                displayNameField.onValueChanged.ClearAll();
                displayNameField.onEndEdit.NewListener(_val =>
                {
                    playerSettings.displayName = _val;
                    SteamLobbyManager.inst.SaveLobbySettings();
                    if (PlayerManager.inst.players.TryFind(x => x.localIndex == playerSettings.index, out var player))
                        PlayerManager.inst.RespawnPlayer(player);
                    if (ProjectArrhythmia.State.IsOnlineMultiplayer)
                        NetworkFunction.SendPlayerSettings();
                });
                displayNameField.GetPlaceholderText().text = "Set name...";
                EditorThemeManager.ApplyInputField(displayNameField, ThemeGroup.Search_Field_1);

                #endregion

                LayoutRebuilder.ForceRebuildLayoutImmediate(gameObject.transform.AsRT());
            }

            var addObject = Creator.NewUIObject("Add Settings", playerSettingsContent);
            addObject.transform.AsRT().sizeDelta = new Vector2(830f, 38f);

            var image = addObject.AddComponent<Image>();
            var button = addObject.AddComponent<Button>();
            button.image = image;
            button.onClick.NewListener(() =>
            {
                PlayerManager.inst.playerSettings.Add(new Core.Data.Player.PlayerSettings
                {
                    index = PlayerManager.inst.playerSettings.Count,
                });
                PlayerManager.inst.SavePlayerSettings();
                RenderPlayerSettings();
            });

            var label = GenerateText(addObject.transform, Lang.Current.GetOrDefault("popups.lobby.add_local_player_settings", "Add Local Player Settings"), RectValues.FullAnchored.SizeDelta(-12f, 0f));

            EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
            EditorThemeManager.ApplyLightText(label);
        }

        void RenderLobbySettings()
        {
            LSHelpers.DeleteChildren(lobbySettingsContent);
            bool editable = ProjectArrhythmia.State.IsHosting;
            var settings = editable ? SteamLobbyManager.inst.LobbySettings : LobbyInfo.HostLobbySettings;
            if (settings == null)
            {
                var waiting = GenerateText(lobbySettingsContent, "Waiting for host settings...", RectValues.Default.SizeDelta(860f, 32f));
                EditorThemeManager.ApplyLightText(waiting);
                return;
            }
            var toggles = new (string name, string description, Func<bool> get, Action<bool> set)[]
            {
                ("Allow Multiple Local Players", "If the player can connect more than one player per user.", () => settings.AllowMultipleLocalPlayers, v => settings.AllowMultipleLocalPlayers = v),
                ("Separate Rank", "If each player's hits should be ranked separately.", () => settings.SeparateRank, v => settings.SeparateRank = v),
                ("Read-Only", "If the player can edit the level at all.", () => !settings.CanEdit, v => settings.CanEdit = !v),
                ("Can View Editor Levels", "If the player can view the host's editor level list.", () => settings.CanViewEditorLevels, v => settings.CanViewEditorLevels = v),
                ("Can Import Prefabs", "If the player can import prefabs from their external prefab list.", () => settings.CanImportPrefabs, v => settings.CanImportPrefabs = v),
                ("Can Expand Prefabs", "If the player can expand prefabs into the level.", () => settings.CanExpandPrefabs, v => settings.CanExpandPrefabs = v),
                ("Can Edit Objects", "If the player can create and edit objects (timeline, keyframes, etc.).", () => settings.CanEditObjects, v => settings.CanEditObjects = v),
                ("Can Edit Markers", "If the player can create and edit markers.", () => settings.CanEditMarkers, v => settings.CanEditMarkers = v),
                ("Can Draw Annotations", "If the player can draw annotations.", () => settings.CanDrawAnnotations, v => settings.CanDrawAnnotations = v),
                ("Can Edit Events", "If the player can edit events (screen shake, bloom, etc.).", () => settings.CanEditEvents, v => settings.CanEditEvents = v),
                ("Can Edit Themes", "If the player can edit the theme layer of events.", () => settings.CanEditThemes, v => settings.CanEditThemes = v),
                ("Can Use Modifiers", "If the player can use and edit modifiers.", () => settings.CanUseModifiers, v => settings.CanUseModifiers = v),
                ("Can Edit Pinned Editor Layers", "If the player can edit the pinned editor layers.", () => settings.CanEditPinnedEditorLayers, v => settings.CanEditPinnedEditorLayers = v),
                ("Can View Outside Range", "If the player can view objects/keyframes outside their song time restriction.", () => settings.CanViewOutsideRange, v => settings.CanViewOutsideRange = v),
                ("Can Edit Players", "If the player can edit player settings (model, speed, boost).", () => settings.CanEditPlayers, v => settings.CanEditPlayers = v),
                ("Can Import Files", "If the player can add external files/media to the level.", () => settings.CanImportFiles, v => settings.CanImportFiles = v),
                ("Can Edit Achievements", "If the player can create and edit achievements.", () => settings.CanEditAchievements, v => settings.CanEditAchievements = v),
                ("Can Edit Level Properties", "If the player can edit the level's properties.", () => settings.CanEditLevelProperties, v => settings.CanEditLevelProperties = v),
                ("Can Edit Metadata", "If the player can edit the level's metadata (difficulty, etc.).", () => settings.CanEditMetaData, v => settings.CanEditMetaData = v),
            };
            for (int i = 0; i < toggles.Length; i++)
            {
                var entry = toggles[i];
                GenerateSettingToggle(entry.name, entry.description, entry.get, entry.set, editable);
            }
        }

        void GenerateSettingToggle(string name, string description, Func<bool> get, Action<bool> set, bool editable)
        {
            if (!checkmarkSprite)
                checkmarkSprite = SpriteHelper.LoadSprite(AssetPack.GetFile("core/sprites/icons/operations/checkmark.png"));

            var row = Creator.NewUIObject("Setting", lobbySettingsContent);
            row.transform.AsRT().sizeDelta = new Vector2(860f, 32f);
            var toggleObj = Creator.NewUIObject("Toggle", row.transform);
            RectValues.LeftAnchored.AnchoredPosition(16f, 0f).SizeDelta(32f, 32f).AssignToRectTransform(toggleObj.transform.AsRT());
            var toggleImage = toggleObj.AddComponent<Image>();
            var checkmark = Creator.NewUIObject("Checkmark", toggleObj.transform);
            RectValues.FullAnchored.SizeDelta(-8f, -8f).AssignToRectTransform(checkmark.transform.AsRT());
            var checkmarkImage = checkmark.AddComponent<Image>();
            checkmarkImage.sprite = checkmarkSprite;
            var toggle = toggleObj.AddComponent<Toggle>();
            toggle.image = toggleImage;
            toggle.graphic = checkmarkImage;
            toggle.interactable = editable;
            toggle.SetIsOnWithoutNotify(get());
            if (editable)
                toggle.onValueChanged.NewListener(_val =>
                {
                    set(_val);
                    SteamLobbyManager.inst.SaveLobbySettings();
                    LobbySettingsChanged();
                });

            EditorThemeManager.ApplyToggle(toggle);

            var label = GenerateText(row.transform, name, RectValues.LeftAnchored.AnchoredPosition(52f, 0f).SizeDelta(260f, 32f));
            EditorThemeManager.ApplyLightText(label);

            var desc = GenerateText(row.transform, description, RectValues.LeftAnchored.AnchoredPosition(324f, 0f).SizeDelta(520f, 32f));
            desc.fontSize = 12;
            desc.horizontalOverflow = HorizontalWrapMode.Overflow;
            EditorThemeManager.ApplyLightText(desc);
        }

        void LobbySettingsChanged()
        {
            if (ProjectArrhythmia.State.IsInLobby)
                NetworkFunction.SendHostLobbySettings();
        }

        /// <summary>
        /// Function occurs when the user joins a lobby.
        /// </summary>
        public void OnLobbyJoined()
        {
            HidePasswordPrompt();
            SetTab(LobbyTab.Current);
        }

        /// <summary>
        /// Function occurs when the user is rejected from the lobby. So sad.
        /// </summary>
        /// <param name="lobby"></param>
        public void OnJoinRejected(Lobby lobby)
        {
            SetTab(LobbyTab.List);
            ShowPasswordPrompt(lobby, "Incorrect password.");
        }

        public override void Tick()
        {
            if ((!ConfigPopup.Instance || !ConfigPopup.Instance.watchingKeybind) && Input.GetKeyDown(CoreConfig.Instance.OpenLobbyKey.Value))
                Toggle();

            if (chatTabButton && chatTabButton.activeSelf != ProjectArrhythmia.State.IsInLobby)
                chatTabButton.SetActive(ProjectArrhythmia.State.IsInLobby);
            UpdateChatTimes();
            UpdateReferenceValidity();
            SteamLobbyManager.inst?.TickLoadDisplays();

            if (RTEditor.inst && RTEditor.inst.hideOtherUsersDropdown && RTEditor.inst.hideOtherUsersDropdown.activeSelf != ProjectArrhythmia.State.IsInLobby)
                RTEditor.inst.hideOtherUsersDropdown.SetActive(ProjectArrhythmia.State.IsInLobby);
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

        public GameObject GetTab(LobbyTab lobbyTab) => tabObjects[(int)lobbyTab];

        /// <summary>
        /// Sets the lobby tab.
        /// </summary>
        /// <param name="lobbyTab">Tab to set.</param>
        public void SetTab(LobbyTab lobbyTab)
        {
            CurrentTab = lobbyTab;
            Render();
        }

        void GenerateMember(Friend member)
        {
            var gameObject = Creator.NewUIObject("Member", playersParent);
            gameObject.transform.AsRT().sizeDelta = new Vector2(830f, 38f);

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

            // handle kicking
            if (ProjectArrhythmia.State.IsHosting && member.Id != RTSteamManager.inst.steamUser.steamID)
            {
                var kickObj = Creator.NewUIObject("kick", gameObject.transform);
                RectValues.RightAnchored.AssignToRectTransform(kickObj.transform.AsRT());
                var kickObjImage = kickObj.AddComponent<Image>();
                var kickObjX = Creator.NewUIObject("x", kickObj.transform);
                var kickObjXImage = kickObjX.AddComponent<Image>();
                kickObjXImage.sprite = closeSprite;

                EditorThemeManager.ApplyGraphic(kickObjImage, ThemeGroup.Delete, true);
                EditorThemeManager.ApplyGraphic(kickObjXImage, ThemeGroup.Delete_Text);

                kickObj.AddComponent<Button>().onClick.AddListener(() =>
                {
                    SteamLobbyManager.Log($"Kicking user: {member.Id}\n" +
                        $"Name: {member.Name}\n" +
                        $"Nickname: {member.Nickname}");
                    if (Transport.Instance && Transport.Instance.steamIDToNetID.TryGetValue(member.Id, out int clientID))
                        NetworkManager.inst.KickClient(clientID);
                });
            }
        }

        #region Password
        
        void HidePasswordPrompt()
        {
            if (passwordPrompt)
                passwordPrompt.SetActive(false);
        }
        void ShowPasswordPrompt(Lobby lobby, string status = "")
        {
            if (!passwordPrompt)
            {
                passwordPrompt = Creator.NewUIObject("Password Prompt", gameObject.transform);
                RectValues.Default.SizeDelta(500f, 220f).AssignToRectTransform(passwordPrompt.transform.AsRT());
                var promptImage = passwordPrompt.AddComponent<Image>();
                EditorThemeManager.ApplyGraphic(promptImage, ThemeGroup.Background_3, true);
                promptTitle = GenerateText(passwordPrompt.transform, string.Empty, RectValues.Default.AnchoredPosition(0f, 76f).SizeDelta(460f, 32f), TextAnchor.MiddleCenter);
                EditorThemeManager.ApplyLightText(promptTitle);
                promptInput = CreatePasswordInput(passwordPrompt.transform, 30f, 420f, "Password...");
                promptStatus = GenerateText(passwordPrompt.transform, string.Empty, RectValues.Default.AnchoredPosition(0f, -10f).SizeDelta(460f, 32f), TextAnchor.MiddleCenter);
                EditorThemeManager.ApplyLightText(promptStatus);
                GeneratePromptButton("Join", -110f, () =>
                {
                    promptStatus.text = string.Empty;
                    var password = promptInput.text;
                    promptInput.SetTextWithoutNotify(string.Empty);
                    HidePasswordPrompt();
                    SteamLobbyManager.inst.JoinLobby(promptLobby, password);
                });
                GeneratePromptButton("Cancel", 110f, HidePasswordPrompt);
            }
            promptLobby = lobby;
            promptTitle.text = lobby.GetName() ?? "Lobby";
            promptStatus.text = status;
            promptInput.SetTextWithoutNotify(string.Empty);
            passwordPrompt.SetActive(true);
            passwordPrompt.transform.SetAsLastSibling();
        }
        InputField CreatePasswordInput(Transform parent, float y, float width, string placeholder)
        {
            var input = numberFieldStorage.transform.Find("input").gameObject.Duplicate(parent);
            input.SetActive(true);
            RectValues.Default.AnchoredPosition(0f, y).SizeDelta(width, 32f).AssignToRectTransform(input.transform.AsRT());
            var field = input.GetComponent<InputField>();
            field.textComponent.alignment = TextAnchor.MiddleLeft;
            field.contentType = InputField.ContentType.Password;
            field.onValueChanged.ClearAll();
            field.GetPlaceholderText().text = placeholder;
            EditorThemeManager.ApplyInputField(field, ThemeGroup.Search_Field_1);
            return field;
        }
        void GeneratePromptButton(string text, float x, Action onClick)
        {
            var buttonObject = Creator.NewUIObject(text, passwordPrompt.transform);
            RectValues.Default.AnchoredPosition(x, -70f).SizeDelta(200f, 32f).AssignToRectTransform(buttonObject.transform.AsRT());
            var buttonImage = buttonObject.AddComponent<Image>();
            var button = buttonObject.AddComponent<Button>();
            button.image = buttonImage;
            button.onClick.NewListener(() => onClick());
            var buttonLabel = GenerateText(buttonObject.transform, text, RectValues.FullAnchored.SizeDelta(-12f, 0f), TextAnchor.MiddleCenter);
            EditorThemeManager.ApplyGraphic(buttonImage, ThemeGroup.Function_1, true);
            EditorThemeManager.ApplyGraphic(buttonLabel, ThemeGroup.Function_1_Text);
        }

        #endregion

        #region Chat

        /// <summary>
        /// Adds a chat message.
        /// </summary>
        /// <param name="message">Chat message to add.</param>
        public void AddChatMessage(ChatMessage message)
        {
            if (message == null || chatMessages.Exists(x => x.id == message.id))
                return;
            chatMessages.Add(message);
            if (chatContent)
                chatCards.Add(CreateChatCard(message));
            while (chatMessages.Count > MAX_CHAT_MESSAGES)
            {
                referenceValidity.Remove(chatMessages[0]);
                chatMessages.RemoveAt(0);
                if (chatCards.Count > 0)
                {
                    var old = chatCards[0];
                    chatCards.RemoveAt(0);
                    if (old)
                        CoreHelper.Delete(old);
                }
            }
            if (chatContent && chatContent.gameObject.activeInHierarchy)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent.AsRT());
                SetChatScrollY(0f);
            }
        }

        /// <summary>
        /// Clears the chat history.
        /// </summary>
        public void ClearChat()
        {
            foreach (var card in chatCards)
                if (card)
                    CoreHelper.Delete(card);
            chatCards.Clear();
            chatMessages.Clear();
            chatTimeUpdaters.Clear();
            referenceValidity.Clear();
        }

        /// <summary>
        /// Gets the chat message history.
        /// </summary>
        /// <param name="max">Max amount of messages to get.</param>
        /// <param name="filter">Function to filter specific chat messages.</param>
        /// <returns>Returns a list of chat messages from the history.</returns>
        public List<ChatMessage> GetChatHistory(int max, Func<ChatMessage, bool> filter)
        {
            var result = new List<ChatMessage>();
            for (int i = chatMessages.Count - 1; i >= 0 && result.Count < max; i--)
                if (filter == null || filter(chatMessages[i]))
                    result.Add(chatMessages[i]);
            result.Reverse();
            return result;
        }

        /// <summary>
        /// Adds the chat message history from before the user joined the lobby.
        /// </summary>
        /// <param name="messages">List of messages to add.</param>
        public void InsertChatHistory(List<ChatMessage> messages)
        {
            var added = false;
            foreach (var message in messages)
            {
                if (chatMessages.Exists(x => x.id == message.id))
                    continue;
                chatMessages.Add(message);
                added = true;
            }
            if (!added)
                return;
            var sorted = System.Linq.Enumerable.ToList(System.Linq.Enumerable.OrderBy(chatMessages, x => x.timeUtc));
            chatMessages.Clear();
            chatMessages.AddRange(sorted);
            if (chatMessages.Count > MAX_CHAT_MESSAGES)
                chatMessages.RemoveRange(0, chatMessages.Count - MAX_CHAT_MESSAGES);
            RebuildChatCards();
        }

        /// <summary>
        /// Adds a system message.
        /// </summary>
        /// <param name="text">Text of the message.</param>
        /// <param name="referenceLevelPath">Referenced level path if a level should be referenced.</param>
        /// <returns>Returns the chat message.</returns>
        public ChatMessage AddSystemMessage(string text, string referenceLevelPath = null)
        {
            var message = new ChatMessage
            {
                name = SYSTEM_NAME,
                colorHex = SYSTEM_COLOR_HEX,
                text = text,
                timeUtc = DateTime.UtcNow,
                kind = ChatMessageKind.System,
                referenceLevelPath = referenceLevelPath,
            };
            AddChatMessage(message);
            return message;
        }

        /// <summary>
        /// Updates the chat message card.
        /// </summary>
        /// <param name="message">Chat message to update.</param>
        public void UpdateChatMessageCard(ChatMessage message)
        {
            if (message == null || !chatContent)
                return;
            var index = chatMessages.IndexOf(message);
            if (index < 0 || index >= chatCards.Count)
                return;
            var old = chatCards[index];
            var siblingIndex = old ? old.transform.GetSiblingIndex() : index;
            var card = CreateChatCard(message);
            card.transform.SetSiblingIndex(siblingIndex);
            chatCards[index] = card;
            if (old)
                CoreHelper.Delete(old);

            if (chatContent.gameObject.activeInHierarchy)
                LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent.AsRT());
        }

        /// <summary>
        /// Rebuilds the chat message cards.
        /// </summary>
        public void RebuildChatCards()
        {
            if (!chatContent)
                return;

            chatTimeUpdaters.Clear();

            foreach (var card in chatCards)
                if (card)
                    CoreHelper.Delete(card);
            chatCards.Clear();

            foreach (var message in chatMessages)
                chatCards.Add(CreateChatCard(message));

            if (chatContent.gameObject.activeInHierarchy)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent.AsRT());
                SetChatScrollY(0f);
            }
        }

        void ScrollChat(float delta) => SetChatScrollY(chatContent ? chatContent.AsRT().anchoredPosition.y - delta * CHAT_SCROLL_SPEED : 0f);
        void SetChatScrollY(float y)
        {
            if (!chatContent || !chatViewportRT)
                return;
            var contentRT = chatContent.AsRT();
            var overflow = Mathf.Max(0f, contentRT.rect.height - chatViewportRT.rect.height);
            contentRT.anchoredPosition = new Vector2(contentRT.anchoredPosition.x, Mathf.Clamp(y, -overflow, 0f));
        }

        void UpdateChatInputHeight()
        {
            if (!chatInput || !chatInputRT)
                return;
            var preferred = chatInput.textComponent.preferredHeight + 16f;
            var height = Mathf.Clamp(preferred, CHAT_INPUT_MIN_HEIGHT, CHAT_INPUT_MAX_HEIGHT);
            chatInputRT.sizeDelta = new Vector2(chatInputRT.sizeDelta.x, height);
        }
        void SendChatFromInput()
        {
            if (!chatInput)
                return;
            var text = chatInput.text?.Trim();
            if (string.IsNullOrEmpty(text))
                return;
            if (SteamLobbyManager.inst)
                SteamLobbyManager.inst.SendChatMessage(text);
            chatInput.SetTextWithoutNotify(string.Empty);
            UpdateChatInputHeight();
            chatInput.ActivateInputField();
        }

        GameObject CreateChatCard(ChatMessage message)
        {
            var style = MenuConfig.Instance.ChatStyle.Value;
            var useNameColor = message.kind == ChatMessageKind.System || MenuConfig.Instance.ChatUseNameColor.Value;
            var opacity = MenuConfig.Instance.ChatMessageOpacity.Value;
            var color = ChatColorUtility.MakeReadable(RTColors.HexToColor(message.colorHex), CHAT_BACKGROUND);
            var name = message.name ?? string.Empty;
            var text = message.text ?? string.Empty;
            var localTime = message.timeUtc.ToLocalTime();
            var hasReference = HasReference(message);
            var valid = hasReference && EvaluateReference(message);
            if (hasReference)
            {
                referenceValidity[message] = valid;
                name = SYSTEM_NAME;
                color = ChatColorUtility.MakeReadable(RTColors.HexToColor(SYSTEM_COLOR_HEX), CHAT_BACKGROUND);
                if (!valid && (string.IsNullOrEmpty(message.referenceLevelPath) || ProjectArrhythmia.State.IsHosting))
                    text = "It's referencing somewhere else...";
            }
            GameObject card;
            if (style == ChatStyle.Story)
                card = BuildStoryCard(name, text, localTime, color, useNameColor, opacity);
            else
                card = CreateNonStoryChatCard(style, name, text, localTime, color, useNameColor, opacity);
            if (!valid)
                return card;
            if (!string.IsNullOrEmpty(message.referenceLevelPath))
                WireReferenceClick(card, () =>
                {
                    if (!ProjectArrhythmia.State.IsHosting)
                        return;
                    EditorLevelManager.inst.LoadLevel(new BetterLegacy.Core.Data.Level.Level(message.referenceLevelPath));
                });
            else
                WireReferenceClick(card, () => ApplySelectionReference(message.referenceKind, message.referenceIds));
            AddReferenceOutline(card, RTColors.HexToColor(message.colorHex));
            return card;
        }
        static bool HasReference(ChatMessage message) =>
            message.kind == ChatMessageKind.System && (!string.IsNullOrEmpty(message.referenceLevelPath) || message.referenceKind != ReferenceKind.None && !string.IsNullOrEmpty(message.referenceIds));
        enum ReferenceState { Valid, Unavailable, Missing }
        ReferenceState GetReferenceState(ChatMessage message)
        {
            if (!HasReference(message))
                return ReferenceState.Missing;
            if (!string.IsNullOrEmpty(message.referenceLevelPath))
            {
                if (!ProjectArrhythmia.State.IsHosting)
                    return ReferenceState.Unavailable;
                return System.IO.Directory.Exists(message.referenceLevelPath) || System.IO.File.Exists(message.referenceLevelPath) ? ReferenceState.Valid : ReferenceState.Missing;
            }
            if (!ProjectArrhythmia.State.InEditor || !EditorManager.inst || !EditorManager.inst.hasLoadedLevel || !BetterLegacy.Core.Data.Beatmap.GameData.Current)
                return ReferenceState.Unavailable;
            bool found;
            switch (message.referenceKind)
            {
                case ReferenceKind.Objects: found = EditorTimeline.inst.IsObjectReferenceValid(message.referenceIds); break;
                case ReferenceKind.Marker: found = RTMarkerEditor.inst.IsMarkerReferenceValid(message.referenceIds); break;
                case ReferenceKind.Keyframes: found = EditorTimeline.inst.IsKeyframeReferenceValid(message.referenceIds); break;
                case ReferenceKind.Layer: found = int.TryParse(message.referenceIds, out var layer) && layer >= 0; break;
                case ReferenceKind.Time: found = float.TryParse(message.referenceIds, out var time) && time >= 0; break;
                default: found = false; break;
            }
            return found ? ReferenceState.Valid : ReferenceState.Missing;
        }
        bool EvaluateReference(ChatMessage message)
        {
            if (message.referenceDead)
                return false;
            var state = GetReferenceState(message);
            if (state == ReferenceState.Valid)
            {
                message.referenceWasValid = true;
                return true;
            }
            if (state == ReferenceState.Missing && message.referenceWasValid)
                message.referenceDead = true;
            return false;
        }
        void AddReferenceOutline(GameObject card, UnityEngine.Color color)
        {
            var edges = new List<(RectTransform rect, bool horizontal)>();
            void MakeEdge(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, bool horizontal)
            {
                var edge = Creator.NewUIObject(name, card.transform);
                var rect = edge.transform.AsRT();
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.pivot = pivot;
                rect.anchoredPosition = Vector2.zero;
                var image = edge.AddComponent<Image>();
                image.color = color;
                image.raycastTarget = false;
                edges.Add((rect, horizontal));
            }
            MakeEdge("Outline Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), true);
            MakeEdge("Outline Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), true);
            MakeEdge("Outline Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), false);
            MakeEdge("Outline Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), false);
            void SetThickness(float thickness)
            {
                foreach (var (rect, horizontal) in edges)
                    rect.sizeDelta = horizontal ? new Vector2(0f, thickness) : new Vector2(thickness, 0f);
            }
            SetThickness(REFERENCE_OUTLINE_THICKNESS);
            var trigger = card.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => SetThickness(REFERENCE_OUTLINE_HOVER_THICKNESS));
            trigger.triggers.Add(enter);
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => SetThickness(REFERENCE_OUTLINE_THICKNESS));
            trigger.triggers.Add(exit);
        }
        void UpdateReferenceValidity()
        {
            if (!Active || CurrentTab != LobbyTab.Chat || Time.unscaledTime < nextReferenceRefresh)
                return;
            nextReferenceRefresh = Time.unscaledTime + 2f;
            foreach (var message in chatMessages.ToArray())
            {
                if (message.referenceDead || !HasReference(message) || !referenceValidity.TryGetValue(message, out var wasValid))
                    continue;
                if (EvaluateReference(message) != wasValid)
                    UpdateChatMessageCard(message);
            }
        }
        void ApplySelectionReference(ReferenceKind referenceKind, string referenceIds)
        {
            if (!ProjectArrhythmia.State.InEditor)
                return;
            if (AudioManager.inst && AudioManager.inst.CurrentAudioSource && AudioManager.inst.CurrentAudioSource.clip)
            {
                AudioManager.inst.CurrentAudioSource.Pause();
                EditorManager.inst.UpdatePlayButton();
            }
            var add = InputDataManager.inst.editorActions.MultiSelect.IsPressed;
            var success = false;
            switch (referenceKind)
            {
                case ReferenceKind.Objects:
                    success = EditorTimeline.inst.ReferenceObjects(referenceIds, add);
                    break;
                case ReferenceKind.Marker:
                    success = RTMarkerEditor.inst.ReferenceMarkerByID(referenceIds, add);
                    break;
                case ReferenceKind.Keyframes:
                    success = EditorTimeline.inst.ReferenceKeyframe(referenceIds);
                    break;
                case ReferenceKind.Layer:
                    if (int.TryParse(referenceIds, out var layer))
                    {
                        EditorTimeline.inst.SetLayer(layer, EditorTimeline.LayerType.Objects);
                        success = true;
                    }
                    break;
                case ReferenceKind.Time:
                    if (float.TryParse(referenceIds, out var time))
                    {
                        AudioManager.inst.SetMusicTime(time);
                        success = true;
                    }
                    break;
            }
            if (!success)
                EditorManager.inst.DisplayNotification("Could not find what the message is referencing.", 2f, EditorManager.NotificationType.Warning);
            else
                Close();
        }

        void WireReferenceClick(GameObject card, System.Action action)
        {
            var button = card.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => action());
        }

        GameObject CreateNonStoryChatCard(ChatStyle style, string name, string text, DateTime localTime, UnityEngine.Color color, bool useNameColor, float opacity)
        {
            var innerWidth = CHAT_CARD_WIDTH - 16f;

            if (style == ChatStyle.Terminal)
            {
                var colorTag = ColorUtility.ToHtmlStringRGB(color);
                string BuildContent() { var timeStr = FormatChatTime(localTime); return useNameColor ? $"[<color=#{colorTag}>{name}</color> - {timeStr}]\n{text}" : $"[{name} - {timeStr}]\n{text}"; }
                var content = BuildContent();
                var height = MeasureChatTextHeight(content, true, innerWidth) + 8f;

                var card = Creator.NewUIObject("Message", chatContent);
                card.transform.AsRT().sizeDelta = new Vector2(CHAT_CARD_WIDTH, height);
                var cardImage = card.AddComponent<Image>();
                cardImage.color = RTColors.FadeColor(UnityEngine.Color.black, opacity);

                var label = MakeChatText(card.transform, RectValues.FullAnchored.SizeDelta(-16f, -8f), TextAnchor.UpperLeft, true, true);
                label.text = content;
                RegisterChatTimeUpdater(label, BuildContent);
                return card;
            }
            else
            {
                var lineHeight = CHAT_FONT_SIZE + 6f;
                var openWidth = MeasureChatTextWidth("<") + 1f;
                var closeWidth = MeasureChatTextWidth(">") + 1f;
                var nameMax = Mathf.Max(20f, innerWidth * 0.5f - openWidth - closeWidth);
                var nameWidth = Mathf.Min(MeasureChatTextWidth(name) + 2f, nameMax);
                var nameArea = openWidth + nameWidth + closeWidth;
                var messageWidth = innerWidth - nameArea - 6f;
                var height = Mathf.Max(MeasureChatTextHeight(text, false, messageWidth), lineHeight) + 8f;

                var card = Creator.NewUIObject("Message", chatContent);
                card.transform.AsRT().sizeDelta = new Vector2(CHAT_CARD_WIDTH, height);
                var cardImage = card.AddComponent<Image>();
                cardImage.color = RTColors.FadeColor(UnityEngine.Color.black, opacity);

                var openLabel = MakeChatText(card.transform, new RectValues(new Vector2(8f, -4f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(openWidth, lineHeight)), TextAnchor.UpperLeft, false, false);
                openLabel.text = "<";

                var nameLabel = MakeChatText(card.transform, new RectValues(new Vector2(8f + openWidth, -4f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(nameWidth, lineHeight)), TextAnchor.UpperLeft, false, false);
                if (useNameColor)
                    nameLabel.color = color;
                nameLabel.text = name;

                var closeLabel = MakeChatText(card.transform, new RectValues(new Vector2(8f + openWidth + nameWidth, -4f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(closeWidth, lineHeight)), TextAnchor.UpperLeft, false, false);
                closeLabel.text = ">";

                var messageLabel = MakeChatText(card.transform, new RectValues(new Vector2(8f + nameArea + 6f, -4f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(messageWidth, height - 8f)), TextAnchor.UpperLeft, true, false);
                messageLabel.text = text;
                return card;
            }
        }
        GameObject BuildStoryCard(string name, string text, DateTime localTime, UnityEngine.Color color, bool useNameColor, float opacity)
        {
            var nameDistance = MenuConfig.Instance.ChatNameDistance.Value;
            var timeStr = FormatChatTime(localTime);
            var timeWidth = Mathf.Max(60f, MeasureChatTextWidth(MenuConfig.Instance.ChatTimeFormat.Value == ChatTimeFormat.Relative ? "59 seconds ago" : timeStr) + 6f);
            var rightWidth = 8f + nameDistance + timeWidth + 8f;
            var leftWidth = Mathf.Max(100f, CHAT_CARD_WIDTH - 16f - rightWidth - 8f);
            var cardHeight = Mathf.Max(MeasureChatTextHeight(text, false, leftWidth), CHAT_FONT_SIZE + 8f) + 8f;
            var card = Creator.NewUIObject("Message", chatContent);
            card.transform.AsRT().sizeDelta = new Vector2(CHAT_CARD_WIDTH, cardHeight);
            var cardImage = card.AddComponent<Image>();
            cardImage.color = RTColors.FadeColor(UnityEngine.Color.black, opacity);
            var label = MakeChatText(card.transform, new RectValues(new Vector2(8f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0.5f), new Vector2(leftWidth, -8f)), TextAnchor.UpperLeft, true, false);
            if (useNameColor)
                label.color = color;
            label.text = text;
            var region = Creator.NewUIObject("Name Region", card.transform);
            new RectValues(new Vector2(-8f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(rightWidth, 0f)).AssignToRectTransform(region.transform.AsRT());
            var regionImage = region.AddComponent<Image>();
            regionImage.color = RTColors.FadeColor(UnityEngine.Color.black, Mathf.Clamp01(opacity * 2f));
            var nameText = MakeChatText(region.transform, new RectValues(new Vector2(8f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0.5f), new Vector2(nameDistance, 0f)), TextAnchor.MiddleLeft, false, false);
            nameText.text = name;
            var timeText = MakeChatText(region.transform, new RectValues(new Vector2(8f + nameDistance, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0.5f), new Vector2(timeWidth, 0f)), TextAnchor.MiddleLeft, false, false);
            timeText.text = timeStr;
            RegisterChatTimeUpdater(timeText, () => FormatChatTime(localTime));
            return card;
        }
        Text MakeChatText(Transform parent, RectValues rectValues, TextAnchor alignment, bool wrap, bool richText)
        {
            var labelObject = Creator.NewUIObject("Text", parent);
            rectValues.AssignToRectTransform(labelObject.transform.AsRT());
            var text = labelObject.AddComponent<Text>();
            text.font = Font.GetDefault();
            text.fontSize = CHAT_FONT_SIZE;
            text.supportRichText = richText;
            text.alignment = alignment;
            text.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            EditorThemeManager.ApplyLightText(text);
            return text;
        }
        static float MeasureChatTextHeight(string content, bool richText, float width)
        {
            float measured = 0f;
            var temp = new GameObject("chat measure", typeof(RectTransform));
            try
            {
                var text = temp.AddComponent<Text>();
                text.font = Font.GetDefault();
                text.fontSize = CHAT_FONT_SIZE;
                text.supportRichText = richText;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.text = content;
                var settings = text.GetGenerationSettings(new Vector2(width, 0f));
                measured = text.cachedTextGeneratorForLayout.GetPreferredHeight(content, settings) / text.pixelsPerUnit;
            }
            catch { }
            UnityEngine.Object.DestroyImmediate(temp);
            var lines = string.IsNullOrEmpty(content) ? 1 : content.Split('\n').Length;
            return Mathf.Max(measured, lines * (CHAT_FONT_SIZE + 6f));
        }
        static float MeasureChatTextWidth(string content)
        {
            float measured = 0f;
            var temp = new GameObject("chat measure", typeof(RectTransform));
            try
            {
                var text = temp.AddComponent<Text>();
                text.font = Font.GetDefault();
                text.fontSize = CHAT_FONT_SIZE;
                text.supportRichText = false;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.text = content;
                var settings = text.GetGenerationSettings(Vector2.zero);
                measured = text.cachedTextGeneratorForLayout.GetPreferredWidth(content, settings) / text.pixelsPerUnit;
            }
            catch { }
            UnityEngine.Object.DestroyImmediate(temp);
            return measured;
        }
        string FormatChatTime(DateTime localTime)
        {
            var showSeconds = MenuConfig.Instance.ChatShowSeconds.Value;
            switch (MenuConfig.Instance.ChatTimeFormat.Value)
            {
                case ChatTimeFormat.Hour12: return localTime.ToString(showSeconds ? "hh:mm:ss tt" : "hh:mm tt");
                case ChatTimeFormat.Relative: {
                        var span = DateTime.Now - localTime;
                        var seconds = Mathf.Max(0, (int)span.TotalSeconds);
                        if (showSeconds && seconds < 60)
                            return seconds == 1 ? "1 second ago" : $"{seconds} seconds ago";
                        var minutes = seconds / 60;
                        if (minutes < 1)
                            return "just now";
                        if (minutes < 60)
                            return minutes == 1 ? "1 minute ago" : $"{minutes} minutes ago";
                        var hours = minutes / 60;
                        return hours == 1 ? "1 hour ago" : $"{hours} hours ago";
                    }
                default: return localTime.ToString(showSeconds ? "HH:mm:ss" : "HH:mm");
            }
        }
        void RegisterChatTimeUpdater(Text text, Func<string> build)
        {
            if (MenuConfig.Instance.ChatTimeFormat.Value == ChatTimeFormat.Relative)
                chatTimeUpdaters.Add((text, build));
        }
        void UpdateChatTimes()
        {
            if (chatTimeUpdaters.Count == 0 || !Active || Time.unscaledTime < nextChatTimeRefresh)
                return;
            nextChatTimeRefresh = Time.unscaledTime + (MenuConfig.Instance.ChatShowSeconds.Value ? 1f : 15f);
            for (int i = chatTimeUpdaters.Count - 1; i >= 0; i--)
            {
                var (text, build) = chatTimeUpdaters[i];
                if (!text)
                {
                    chatTimeUpdaters.RemoveAt(i);
                    continue;
                }
                text.text = build();
            }
        }

        #endregion

        #endregion
    }
}
