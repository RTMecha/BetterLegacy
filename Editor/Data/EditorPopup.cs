using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Managers;
using LSFunctions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Editor.Data
{
    /// <summary>
    /// Represents a popup in the editor.
    /// </summary>
    public class EditorPopup
    {
        public EditorPopup() { }

        public EditorPopup(string name) => Name = name;

        public EditorPopup(string name, string title, Vector2 defaultPosition, Vector2 size, Action<string> refreshSearch = null, Action close = null, string placeholderText = "Search...")
        {
            Name = name;
            this.title = title;
            this.defaultPosition = defaultPosition;
            this.size = size;
            this.refreshSearch = refreshSearch;
            this.close = close;
            this.placeholderText = placeholderText;
        }

        #region Properties

        /// <summary>
        /// Name of the editor popup.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Game object of the editor popup.
        /// </summary>
        public GameObject GameObject { get; set; }
        /// <summary>
        /// Close button of the editor popup.
        /// </summary>
        public Button CloseButton { get; set; }
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
        /// Top panel of the editor popup.
        /// </summary>
        public RectTransform TopPanel { get; set; }
        /// <summary>
        /// Title of the editor popup.
        /// </summary>
        public Text Title { get; set; }

        /// <summary>
        /// Scrollbar of the editor popups' content.
        /// </summary>
        public Scrollbar ContentScrollbar { get; set; }

        /// <summary>
        /// If the editor popup is open.
        /// </summary>
        public bool IsOpen => GameObject && GameObject.activeInHierarchy;

        #endregion

        #region Fields

        /// <summary>
        /// Animation of the editor popup.
        /// </summary>
        public EditorAnimation editorAnimation;

        /// <summary>
        /// Title of the editor popup.
        /// </summary>
        public string title;

        /// <summary>
        /// Default position the editor popup should be at.
        /// </summary>
        public Vector2 defaultPosition;

        /// <summary>
        /// Size of the editor popup.
        /// </summary>
        public Vector2 size;

        /// <summary>
        /// Refresh search function.
        /// </summary>
        public Action<string> refreshSearch;

        /// <summary>
        /// Close editor popup function.
        /// </summary>
        public Action close;

        /// <summary>
        /// The placeholder string of the editor popups' search field.
        /// </summary>
        public string placeholderText = "Search...";

        #endregion

        #region Methods

        /// <summary>
        /// Creates an editor popup from an existing popup game object.
        /// </summary>
        /// <param name="name">Name of the popup.</param>
        /// <param name="gameObject">Game object reference.</param>
        /// <returns>Returns an editor popup made from a game object.</returns>
        public static EditorPopup FromGameObject(string name, GameObject gameObject)
        {
            var editorPopup = new EditorPopup(name);
            editorPopup.Assign(gameObject);

            try
            {
                if (editorPopup.Title)
                    editorPopup.title = editorPopup.Title.text;
                if (editorPopup.SearchField)
                    editorPopup.placeholderText = editorPopup.SearchField.GetPlaceholderText().text;
            }
            catch { }

            return editorPopup;
        }

        /// <summary>
        /// Opens the editor popup.
        /// </summary>
        public void Open() => PlayAnimation(true);

        /// <summary>
        /// Closes the editor popup.
        /// </summary>
        public void Close() => PlayAnimation(false);

        /// <summary>
        /// Plays the popups' animation.
        /// </summary>
        /// <param name="active"></param>
        public void PlayAnimation(bool active)
        {
            var play = EditorConfig.Instance.PlayEditorAnimations.Value;

            if (!editorAnimation && RTEditor.inst.editorAnimations.TryFind(x => x.name == Name, out EditorAnimation dialogAnimation))
                editorAnimation = dialogAnimation;

            if (play && editorAnimation && IsOpen != active)
            {
                if (!editorAnimation.Active)
                {
                    SetActive(active);

                    return;
                }

                var dialog = GameObject.transform;

                var scrollbar = dialog.GetComponentsInChildren<Scrollbar>().ToList();
                var scrollAmounts = scrollbar.Select(x => x.value).ToList();

                var animation = new RTAnimation("Popup Open");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, active ? editorAnimation.PosStart.x : editorAnimation.PosEnd.x, Ease.Linear),
                        new FloatKeyframe(active ? editorAnimation.PosXStartDuration : editorAnimation.PosXEndDuration, active ? editorAnimation.PosEnd.x : editorAnimation.PosStart.x, active ? Ease.GetEaseFunction(editorAnimation.PosXStartEase) : Ease.GetEaseFunction(editorAnimation.PosXEndEase)),
                    }, x =>
                    {
                        if (editorAnimation.PosActive)
                        {
                            var pos = dialog.localPosition;
                            pos.x = x;
                            dialog.localPosition = pos;
                        }
                    }),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, active ? editorAnimation.PosStart.y : editorAnimation.PosEnd.y, Ease.Linear),
                        new FloatKeyframe(active ? editorAnimation.PosYStartDuration : editorAnimation.PosYEndDuration, active ? editorAnimation.PosEnd.y : editorAnimation.PosStart.y, active ? Ease.GetEaseFunction(editorAnimation.PosYStartEase) : Ease.GetEaseFunction(editorAnimation.PosYEndEase)),
                    }, x =>
                    {
                        if (editorAnimation.PosActive)
                        {
                            var pos = dialog.localPosition;
                            pos.y = x;
                            dialog.localPosition = pos;
                        }
                    }),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, active ? editorAnimation.ScaStart.x : editorAnimation.ScaEnd.x, Ease.Linear),
                        new FloatKeyframe(active ? editorAnimation.ScaXStartDuration : editorAnimation.ScaXEndDuration, active ? editorAnimation.ScaEnd.x : editorAnimation.ScaStart.x, active ? Ease.GetEaseFunction(editorAnimation.ScaXStartEase) : Ease.GetEaseFunction(editorAnimation.ScaXEndEase)),
                    }, x =>
                    {
                        if (editorAnimation.ScaActive)
                        {
                            var pos = dialog.localScale;
                            pos.x = x;
                            dialog.localScale = pos;

                            for (int i = 0; i < scrollbar.Count; i++)
                                scrollbar[i].value = scrollAmounts[i];
                        }
                    }),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, active ? editorAnimation.ScaStart.y : editorAnimation.ScaEnd.y, Ease.Linear),
                        new FloatKeyframe(active ? editorAnimation.ScaYStartDuration : editorAnimation.ScaYEndDuration, active ? editorAnimation.ScaEnd.y : editorAnimation.ScaStart.y, active ? Ease.GetEaseFunction(editorAnimation.ScaYStartEase) : Ease.GetEaseFunction(editorAnimation.ScaYEndEase)),
                    }, x =>
                    {
                        if (editorAnimation.ScaActive)
                        {
                            var pos = dialog.localScale;
                            pos.y = x;
                            dialog.localScale = pos;

                            for (int i = 0; i < scrollbar.Count; i++)
                                scrollbar[i].value = scrollAmounts[i];
                        }
                    }),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, active ? editorAnimation.RotStart : editorAnimation.RotEnd, Ease.Linear),
                        new FloatKeyframe(active ? editorAnimation.RotStartDuration : editorAnimation.RotEndDuration, active ? editorAnimation.RotEnd : editorAnimation.RotStart, active ? Ease.GetEaseFunction(editorAnimation.RotStartEase) : Ease.GetEaseFunction(editorAnimation.RotEndEase)),
                    }, x =>
                    {
                        if (editorAnimation.RotActive)
                            dialog.localRotation = Quaternion.Euler(0f, 0f, x);
                    }),
                };

                animation.onComplete = () =>
                {
                    SetActive(active);

                    if (editorAnimation.PosActive)
                        dialog.localPosition = new Vector3(active ? editorAnimation.PosEnd.x : editorAnimation.PosStart.x, active ? editorAnimation.PosEnd.y : editorAnimation.PosStart.y, 0f);
                    if (editorAnimation.ScaActive)
                        dialog.localScale = new Vector3(active ? editorAnimation.ScaEnd.x : editorAnimation.ScaStart.x, editorAnimation.ScaEnd.y, 1f);
                    if (editorAnimation.RotActive)
                        dialog.localRotation = Quaternion.Euler(0f, 0f, active ? editorAnimation.RotEnd : editorAnimation.RotStart);

                    AnimationManager.inst.Remove(animation.id);
                };

                AnimationManager.inst.Play(animation);
            }

            if (!play || !editorAnimation || active)
                SetActive(active);
        }

        /// <summary>
        /// Sets the editor popup active state.
        /// </summary>
        /// <param name="active">Active state to set.</param>
        public void SetActive(bool active)
        {
            if (GameObject)
                GameObject.SetActive(active);
        }

        /// <summary>
        /// Assigns the elements of a game object to this editor popup.
        /// </summary>
        /// <param name="popup">Poppup game object to assign from.</param>
        public void Assign(GameObject popup)
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

        /// <summary>
        /// Initializes the editor popup.
        /// </summary>
        public void Init()
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

        /// <summary>
        /// Renders the whole editor popup.
        /// </summary>
        public void Render()
        {
            RenderTitle();
            RenderSize();
            RenderPlaceholderText();
            UpdateSearchFunction(refreshSearch);
            UpdateCloseFunction(close);
        }

        /// <summary>
        /// Renders the editor popup title.
        /// </summary>
        public void RenderTitle() => RenderTitle(title);

        /// <summary>
        /// Renders the editor popup title.
        /// </summary>
        /// <param name="title">Title of the editor popup.</param>
        public void RenderTitle(string title)
        {
            if (Title)
                Title.text = title;
        }

        /// <summary>
        /// Renders the editor popup position and size.
        /// </summary>
        public void RenderSize() => RenderSize(size);

        /// <summary>
        /// Renders the editor popup position and size.
        /// </summary>
        /// <param name="size">Size of the editor popup.</param>
        public void RenderSize(Vector2 size)
        {
            if (!GameObject)
                return;

            var inSize = size == Vector2.zero ? new Vector2(600f, 450f) : size;
            GameObject.transform.AsRT().anchoredPosition = defaultPosition;
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
            SearchField.text = searchTerm;
            SearchField.onValueChanged.AddListener(_val => onSearch?.Invoke(_val));
        }

        /// <summary>
        /// Updates the close button.
        /// </summary>
        /// <param name="close">Runs when the user clicks the close popup button.</param>
        public void UpdateCloseFunction(Action close)
        {
            CloseButton.onClick.ClearAll();
            CloseButton.onClick.AddListener(() =>
            {
                EditorManager.inst.HideDialog(Name);
                close?.Invoke();
            });
        }

        public override string ToString() => $"{Name} - {title}";

        #endregion
    }
}
