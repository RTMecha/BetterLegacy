using System;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Popups
{
    /// <summary>
    /// Represents a popup that can contain items.
    /// </summary>
    public class ContentPopup : EditorPopup, IContentUI
    {
        public ContentPopup(string name) : base(name) { }

        public ContentPopup(string name, string title, Vector2? defaultPosition = null, Vector2? size = null, Action<string> refreshSearch = null, Action close = null, string placeholderText = "Search...") : base(name)
        {
            this.title = title;
            this.defaultPosition = defaultPosition;
            this.size = size;
            this.refreshSearch = refreshSearch;
            this.close = close;
            this.placeholderText = placeholderText;
        }

        #region Values

        /// <summary>
        /// Search field of the editor popup.
        /// </summary>
        public InputField SearchField { get; set; }

        /// <summary>
        /// Content transform of the editor popup.
        /// </summary>
        public Transform Content { get; set; }

        /// <summary>
        /// Grid layout of the editor popups' content.
        /// </summary>
        public GridLayoutGroup Grid { get; set; }

        /// <summary>
        /// Scrollbar of the editor popups' content.
        /// </summary>
        public Scrollbar ContentScrollbar { get; set; }

        /// <summary>
        /// Gets and sets the search input field text.
        /// </summary>
        public string SearchTerm { get => SearchField.text; set => SearchField.text = value; }

        /// <summary>
        /// Refresh search function.
        /// </summary>
        public Action<string> refreshSearch;

        /// <summary>
        /// The placeholder string of the editor popups' search field.
        /// </summary>
        public string placeholderText = "Search...";

        #endregion

        #region Methods

        /// <summary>
        /// Clears the content from the popup.
        /// </summary>
        public void ClearContent() => LSHelpers.DeleteChildren(Content);

        /// <summary>
        /// Assigns the elements of a game object to this editor popup.
        /// </summary>
        /// <param name="popup">Poppup game object to assign from.</param>
        public override void Assign(GameObject popup)
        {
            GameObject = popup;
            if (popup.transform.TryFind("Panel", out Transform topPanel))
            {
                TopPanel = topPanel.AsRT();
                if (topPanel.TryFind("Text", out Transform title) && title.gameObject.TryGetComponent(out Text titleText))
                    Title = titleText;

                if (topPanel.TryFind("x", out Transform close) && close.gameObject.TryGetComponent(out Button closeButton))
                    CloseButton = closeButton;
            }

            if (popup.transform.TryFind("search-box/search", out Transform searchBox) && searchBox.gameObject.TryGetComponent(out InputField searchField))
                SearchField = searchField;

            if (popup.transform.TryFind("mask/content", out Transform content))
            {
                Content = content;
                Grid = content.GetComponent<GridLayoutGroup>();
            }

            if (popup.transform.TryFind("Scrollbar", out Transform sidebar) && sidebar.gameObject.TryGetComponent(out Scrollbar contentScrollbar))
                ContentScrollbar = contentScrollbar;
        }

        public override void Init()
        {
            var name = Name;
            var popup = GameObject;
            if (popup)
                CoreHelper.Destroy(popup);

            popup = EditorManager.inst.GetDialog("Parent Selector").Dialog.gameObject.Duplicate(RTEditor.inst.popups, name);
            Assign(popup);
            popup.transform.localPosition = Vector3.zero;

            EditorHelper.AddEditorPopup(name, popup);

            EditorThemeManager.AddGraphic(popup.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Bottom_Left_I);
            EditorThemeManager.AddGraphic(TopPanel.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);
            EditorThemeManager.AddSelectable(CloseButton, ThemeGroup.Close);
            EditorThemeManager.AddGraphic(CloseButton.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);
            EditorThemeManager.AddLightText(Title);
            EditorThemeManager.AddScrollbar(ContentScrollbar, scrollbarRoundedSide: SpriteHelper.RoundedSide.Bottom_Right_I);
            EditorThemeManager.AddInputField(SearchField, ThemeGroup.Search_Field_1, 1, SpriteHelper.RoundedSide.Bottom);

            Render();
        }

        public override void Render()
        {
            RenderTitle();
            RenderSize();
            RenderPlaceholderText();
            UpdateSearchFunction(refreshSearch);
            UpdateCloseFunction(close);
        }

        public override void RenderSize(Vector2 size)
        {
            if (!GameObject)
                return;

            var inSize = size == Vector2.zero ? new Vector2(600f, 450f) : size;
            if (defaultPosition is Vector2 defaultPositionValue)
                GameObject.transform.AsRT().anchoredPosition = defaultPositionValue;
            GameObject.transform.AsRT().sizeDelta = inSize;

            if (TopPanel)
                TopPanel.sizeDelta = new Vector2(inSize.x + 32f, 32f);

            if (SearchField)
                SearchField.gameObject.transform.parent.AsRT().sizeDelta = new Vector2(inSize.x, 32f);

            if (Grid)
                Grid.cellSize = new Vector2(inSize.x - 5f, 32f);

            if (ContentScrollbar)
            {
                ContentScrollbar.value = 1f;
                ContentScrollbar.gameObject.transform.AsRT().sizeDelta = new Vector2(32f, inSize.y);
            }
        }

        /// <summary>
        /// Renders the editor popups' search field placeholder.
        /// </summary>
        public void RenderPlaceholderText() => RenderPlaceholderText(placeholderText);

        /// <summary>
        /// Renders the editor popups' search field placeholder.
        /// </summary>
        /// <param name="text">Text to set.</param>
        public void RenderPlaceholderText(string text)
        {
            if (SearchField)
                SearchField.GetPlaceholderText().text = text;
        }

        /// <summary>
        /// Updates the search field.
        /// </summary>
        /// <param name="onSearch">Runs when the user types in the search field.</param>
        public void UpdateSearchFunction(Action<string> onSearch)
        {
            SearchField.onValueChanged.ClearAll();
            SearchField.onValueChanged.AddListener(_val => onSearch?.Invoke(_val));
        }

        /// <summary>
        /// Updates the search field.
        /// </summary>
        /// <param name="searchTerm">What has been searched.</param>
        /// <param name="onSearch">Runs when the user types in the search field.</param>
        public void UpdateSearchFunction(string searchTerm, Action<string> onSearch)
        {
            SearchField.onValueChanged.ClearAll();
            SearchTerm = searchTerm;
            SearchField.onValueChanged.AddListener(_val => onSearch?.Invoke(_val));
        }

        /// <summary>
        /// Generates a list button.
        /// </summary>
        /// <param name="name">Name to display on the button.</param>
        /// <param name="onClick">Function to run when the button is clicked.</param>
        public FunctionButtonStorage GenerateListButton(string name, Action<PointerEventData> onClick)
        {
            var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(Content, "anim");
            var storage = gameObject.GetComponent<FunctionButtonStorage>();
            storage.button.onClick.ClearAll();
            var contextClickable = gameObject.GetOrAddComponent<ContextClickable>();
            contextClickable.onClick = onClick;

            storage.label.text = name;

            EditorThemeManager.ApplySelectable(storage.button, ThemeGroup.List_Button_1);
            EditorThemeManager.ApplyLightText(storage.label);

            return storage;
        }

        /// <summary>
        /// Generates a list button.
        /// </summary>
        /// <param name="name">Name to display on the button.</param>
        /// <param name="sprite">Sprite to display on the button.</param>
        /// <param name="onClick">Function to run when the button is clicked.</param>
        public SpriteFunctionButtonStorage GenerateListButton(string name, Sprite sprite, Action<PointerEventData> onClick)
        {
            var gameObject = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(Content, "anim");
            var storage = gameObject.GetComponent<SpriteFunctionButtonStorage>();
            storage.button.onClick.ClearAll();
            var contextClickable = gameObject.GetOrAddComponent<ContextClickable>();
            contextClickable.onClick = onClick;

            storage.image.sprite = sprite;
            storage.label.text = name;

            EditorThemeManager.ApplyGraphic(storage.image, ThemeGroup.Light_Text);
            EditorThemeManager.ApplySelectable(storage.button, ThemeGroup.List_Button_1);
            EditorThemeManager.ApplyLightText(storage.label);

            return storage;
        }

        #endregion
    }
}
