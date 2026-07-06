using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Menus.UI.Popups
{
    /// <summary>
    /// Represents a floating menu that can appear anywhere.
    /// </summary>
    public abstract class PopupBase : Exists
    {
        #region Values

        /// <summary>
        /// Popups canvas.
        /// </summary>
        public static UICanvas canvas;

        /// <summary>
        /// Popups parent.
        /// </summary>
        public static Transform Parent => canvas.GameObject.transform;

        /// <summary>
        /// Global list of popups.
        /// </summary>
        public static List<PopupBase> popups = new List<PopupBase>();

        /// <summary>
        /// If any of the popups are active.
        /// </summary>
        public static bool AnyPopupActive => popups.Any(x => x.Active);

        /// <summary>
        /// Unity Game Object reference.
        /// </summary>
        public GameObject gameObject;

        /// <summary>
        /// If the popup is active.
        /// </summary>
        public bool Active => gameObject && gameObject.activeSelf;

        public DraggableUI draggable;

        public GameObject topPanel;

        #region Prefabs

        public static GameObject numberFieldStorage;

        #endregion

        #endregion

        #region Functions

        /// <summary>
        /// Initializes the popups parent canvas.
        /// </summary>
        public static void InitPopupsCanvas()
        {
            canvas = UIManager.GenerateUICanvas("Popups Canvas", null, true);
            canvas.Canvas.scaleFactor = 1f;
            canvas.CanvasScaler.referenceResolution = new Vector2(1920f, 1080f);

            // Prefab
            {
                var page = Creator.NewUIObject("Page", Parent.transform);
                RectValues.BottomLeftAnchored.AnchoredPosition(580f, 32f).Pivot(0.5f, 0.5f).SizeDelta(0f, 32f).AssignToRectTransform(page.transform.AsRT());

                numberFieldStorage = page;

                var pageHorizontalLayoutGroup = page.AddComponent<HorizontalLayoutGroup>();
                pageHorizontalLayoutGroup.spacing = 8f;
                pageHorizontalLayoutGroup.childControlHeight = false;
                pageHorizontalLayoutGroup.childControlWidth = false;
                pageHorizontalLayoutGroup.childForceExpandHeight = true;
                pageHorizontalLayoutGroup.childForceExpandWidth = false;

                var pageInput = Creator.NewUIObject("input", page.transform);
                var pageInputImage = pageInput.AddComponent<Image>();
                var pageInputField = pageInput.AddComponent<InputField>();
                var pageInputLayoutElement = pageInput.AddComponent<LayoutElement>();
                pageInputLayoutElement.preferredWidth = 10000f;
                RectValues.LeftAnchored.SizeDelta(151f, 32f).AssignToRectTransform(pageInput.transform.AsRT());

                var pageInputPlaceholder = Creator.NewUIObject("Placeholder", pageInput.transform);
                var pageInputPlaceholderText = pageInputPlaceholder.AddComponent<Text>();
                pageInputPlaceholderText.alignment = TextAnchor.MiddleLeft;
                pageInputPlaceholderText.font = Font.GetDefault();
                pageInputPlaceholderText.fontSize = 20;
                pageInputPlaceholderText.fontStyle = FontStyle.Italic;
                pageInputPlaceholderText.horizontalOverflow = HorizontalWrapMode.Wrap;
                pageInputPlaceholderText.verticalOverflow = VerticalWrapMode.Overflow;
                pageInputPlaceholderText.color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);
                pageInputPlaceholderText.text = "Set number...";
                RectValues.FullAnchored.SizeDelta(-8f, -8f).AssignToRectTransform(pageInputPlaceholder.transform.AsRT());

                var pageInputText = Creator.NewUIObject("Text", pageInput.transform);
                var pageInputTextText = pageInputText.AddComponent<Text>();
                pageInputTextText.alignment = TextAnchor.MiddleCenter;
                pageInputTextText.font = Font.GetDefault();
                pageInputTextText.fontSize = 20;
                pageInputTextText.fontStyle = FontStyle.Normal;
                pageInputTextText.horizontalOverflow = HorizontalWrapMode.Wrap;
                pageInputTextText.verticalOverflow = VerticalWrapMode.Overflow;
                pageInputTextText.color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);
                RectValues.FullAnchored.SizeDelta(-8f, -8f).AssignToRectTransform(pageInputText.transform.AsRT());

                pageInputField.placeholder = pageInputPlaceholderText;
                pageInputField.textComponent = pageInputTextText;
                pageInputField.characterValidation = InputField.CharacterValidation.None;
                pageInputField.contentType = InputField.ContentType.Standard;
                pageInputField.inputType = InputField.InputType.Standard;
                pageInputField.keyboardType = TouchScreenKeyboardType.Default;
                pageInputField.lineType = InputField.LineType.SingleLine;

                pageInputField.onValueChanged.ClearAll();
                pageInputField.text = "0";

                var leftGreater = Creator.NewUIObject("<<", page.transform);
                var leftGreaterImage = leftGreater.AddComponent<Image>();
                leftGreaterImage.sprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/left_double{FileFormat.PNG.Dot()}"));
                var leftGreaterButton = leftGreater.AddComponent<Button>();
                leftGreaterButton.image = leftGreaterImage;
                var leftGreaterLayoutElement = leftGreater.AddComponent<LayoutElement>();
                leftGreaterLayoutElement.minWidth = 32f;
                leftGreaterLayoutElement.preferredWidth = 32f;
                RectValues.LeftAnchored.AnchoredPosition(175f, -16f).Pivot(0.5f, 0.5f).SizeDelta(32f, 32f).AssignToRectTransform(leftGreater.transform.AsRT());

                var left = Creator.NewUIObject("<", page.transform);
                var leftImage = left.AddComponent<Image>();
                leftImage.sprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/left_small{FileFormat.PNG.Dot()}"));
                var leftButton = left.AddComponent<Button>();
                leftButton.image = leftImage;
                var leftLayoutElement = left.AddComponent<LayoutElement>();
                leftLayoutElement.minWidth = 32f;
                leftLayoutElement.preferredWidth = 32f;
                RectValues.LeftAnchored.AnchoredPosition(199f, -16f).SizeDelta(16f, 32f).AssignToRectTransform(left.transform.AsRT());

                var right = Creator.NewUIObject(">", page.transform);
                var rightImage = right.AddComponent<Image>();
                rightImage.sprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/right_small{FileFormat.PNG.Dot()}"));
                var rightButton = right.AddComponent<Button>();
                rightButton.image = rightImage;
                var rightLayoutElement = right.AddComponent<LayoutElement>();
                rightLayoutElement.minWidth = 32f;
                rightLayoutElement.preferredWidth = 32f;
                RectValues.LeftAnchored.AnchoredPosition(247f, -16f).SizeDelta(16f, 32f).AssignToRectTransform(right.transform.AsRT());

                var rightGreater = Creator.NewUIObject(">>", page.transform);
                var rightGreaterImage = rightGreater.AddComponent<Image>();
                rightGreaterImage.sprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/right_double{FileFormat.PNG.Dot()}"));
                var rightGreaterButton = rightGreater.AddComponent<Button>();
                rightGreaterButton.image = rightGreaterImage;
                var rightGreaterLayoutElement = rightGreater.AddComponent<LayoutElement>();
                rightGreaterLayoutElement.minWidth = 32f;
                rightGreaterLayoutElement.preferredWidth = 32f;
                RectValues.LeftAnchored.AnchoredPosition(271f, -16f).Pivot(0.5f, 0.5f).SizeDelta(32f, 32f).AssignToRectTransform(rightGreater.transform.AsRT());

                var fieldStorage = page.AddComponent<InputFieldStorage>();
                fieldStorage.inputField = pageInputField;
                fieldStorage.leftGreaterButton = leftGreaterButton;
                fieldStorage.leftButton = leftButton;
                fieldStorage.rightButton = rightButton;
                fieldStorage.rightGreaterButton = rightGreaterButton;

                page.SetActive(false);
            }
        }

        /// <summary>
        /// Ticks all popups.
        /// </summary>
        public static void TickPopups()
        {
            if (canvas != null)
                canvas.Canvas.scaleFactor = CoreHelper.ScreenScale;
            for (int i = 0; i < popups.Count; i++)
                popups[i].Tick();
        }

        /// <summary>
        /// Registers a popup to the popups list.
        /// </summary>
        /// <typeparam name="T">Type of the popup to register.</typeparam>
        public static void RegisterPopup<T>() where T : PopupBase, new()
        {
            var popup = new T();
            popup.Init();
            popups.Add(popup);
        }

        /// <summary>
        /// Initializes the popup.
        /// </summary>
        public abstract void Init();

        /// <summary>
        /// Renders the popup.
        /// </summary>
        public abstract void Render();

        /// <summary>
        /// Ticks the popup.
        /// </summary>
        public virtual void Tick() { }

        /// <summary>
        /// Shows the popup.
        /// </summary>
        public virtual void Open()
        {
            gameObject.SetActive(true);
            gameObject.transform.SetAsLastSibling();
            Render();
        }

        /// <summary>
        /// Hides the popup.
        /// </summary>
        public virtual void Close() => gameObject.SetActive(false);

        /// <summary>
        /// Toggles the popup.
        /// </summary>
        public virtual void Toggle()
        {
            var active = !Active;
            if (active)
                Open();
            else
                Close();
        }

        /// <summary>
        /// Initializes the dragging component.
        /// </summary>
        public void InitDragging()
        {
            if (draggable)
                return;

            draggable = gameObject.GetOrAddComponent<DraggableUI>();
            draggable.target = gameObject.transform;
            draggable.onStartDrag = pos => MoveToFront();
        }

        /// <summary>
        /// Initializes the top panel.
        /// </summary>
        public void InitTopPanel()
        {
            topPanel = Creator.NewUIObject("Panel", gameObject.transform);
            new RectValues(Vector2.zero, Vector2.one, new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, 32f)).AssignToRectTransform(topPanel.transform.AsRT());

            var panelImage = topPanel.AddComponent<Image>();
            EditorThemeManager.ApplyGraphic(panelImage, ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);
        }

        /// <summary>
        /// Initializes the popup title.
        /// </summary>
        /// <param name="text">Text of the title. Can be a lang key.</param>
        public void InitTitle(string text) => InitTitle(text, text);

        /// <summary>
        /// Initializes the popup title.
        /// </summary>
        /// <param name="text">Lang key of the title.</param>
        /// <param name="defaultText">Default text to use if no lang entry exists.</param>
        public void InitTitle(string text, string defaultText)
        {
            var title = Creator.NewUIObject("Title", topPanel.transform);
            RectValues.FullAnchored.AnchoredPosition(2f, 0f).SizeDelta(-12f, -8f).AssignToRectTransform(title.transform.AsRT());

            var titleText = title.AddComponent<Text>();
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.font = Font.GetDefault();
            titleText.fontSize = 20;
            titleText.text = Lang.Current.GetOrDefault(text, defaultText);
            EditorThemeManager.ApplyLightText(titleText);
        }

        /// <summary>
        /// Initializes the close button.
        /// </summary>
        public void InitCloseButton()
        {
            var close = Creator.NewUIObject("x", topPanel.transform);
            RectValues.TopRightAnchored.SizeDelta(32f, 32f).AssignToRectTransform(close.transform.AsRT());

            var closeImage = close.AddComponent<Image>();
            var closeButton = close.AddComponent<Button>();
            closeButton.image = closeImage;

            EditorThemeManager.ApplySelectable(closeButton, ThemeGroup.Close);

            closeButton.onClick.AddListener(Close);

            var closeX = Creator.NewUIObject("Image", close.transform);
            RectValues.FullAnchored.SizeDelta(-8f, -8f).AssignToRectTransform(closeX.transform.AsRT());

            var closeXImage = closeX.AddComponent<Image>();
            closeXImage.sprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/close{FileFormat.PNG.Dot()}"));

            EditorThemeManager.ApplyGraphic(closeXImage, ThemeGroup.Close_X);
        }

        /// <summary>
        /// Moves the popup to the front.
        /// </summary>
        public void MoveToFront()
        {
            if (gameObject)
                gameObject.transform.SetAsLastSibling();
        }

        internal Text GenerateText(Transform parent, string text, RectValues rectValues, TextAnchor alignment = TextAnchor.MiddleLeft, int fontSize = 16)
        {
            var label = Creator.NewUIObject("Label", parent);
            rectValues.AssignToRectTransform(label.transform.AsRT());

            var labelText = label.AddComponent<Text>();
            labelText.alignment = alignment;
            labelText.font = Font.GetDefault();
            labelText.fontSize = fontSize;
            labelText.text = text;
            return labelText;
        }

        #endregion
    }
}
