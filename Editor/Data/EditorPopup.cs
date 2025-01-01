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
        /// If the editor popup is open.
        /// </summary>
        public bool IsOpen => GameObject && GameObject.activeInHierarchy;

        #endregion

        #region Fields

        public EditorAnimation editorAnimation;

        #endregion

        #region Methods

        /// <summary>
        /// Opens the editor popup.
        /// </summary>
        public void Open() => PlayAnimation(true);

        /// <summary>
        /// Closes the editor popup.
        /// </summary>
        public void Close() => PlayAnimation(false);

        public void PlayAnimation(bool active)
        {
            var play = EditorConfig.Instance.PlayEditorAnimations.Value;

            if (!editorAnimation && RTEditor.inst.editorAnimations.TryFind(x => x.name == Name, out EditorAnimation dialogAnimation))
                editorAnimation = dialogAnimation;

            if (play && editorAnimation && IsOpen != active)
            {
                if (!editorAnimation.Active)
                {
                    GameObject.SetActive(active);

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
                    dialog.gameObject.SetActive(active);

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
                GameObject.SetActive(active);
        }

        public void Assign(string name, GameObject gameObject, Button close, InputField searchField, Transform content, GridLayoutGroup grid, RectTransform topPanel, Text title)
        {
            Name = name;
            GameObject = gameObject;
            CloseButton = close;
            SearchField = searchField;
            Content = content;
            Grid = grid;
            TopPanel = topPanel;
            Title = title;
        }

        public void Init(string name, string title, Vector2 defaultPosition, Vector2 size, Action<string> refreshSearch = null, Action close = null, string placeholderText = "Search...")
        {
            Name = name;

            var popup = GameObject;
            if (popup)
                CoreHelper.Destroy(popup);

            popup = EditorManager.inst.GetDialog("Parent Selector").Dialog.gameObject.Duplicate(RTEditor.inst.popups, name);
            GameObject = popup;
            popup.transform.localPosition = Vector3.zero;

            var inSize = size == Vector2.zero ? new Vector2(600f, 450f) : size;
            popup.transform.AsRT().anchoredPosition = defaultPosition;
            popup.transform.AsRT().sizeDelta = inSize;
            TopPanel = popup.transform.Find("Panel").AsRT();
            TopPanel.sizeDelta = new Vector2(inSize.x + 32f, 32f);
            var text = TopPanel.Find("Text").GetComponent<Text>();
            Title = text;
            text.text = title;

            popup.transform.Find("search-box").AsRT().sizeDelta = new Vector2(inSize.x, 32f);
            Grid = popup.transform.Find("mask/content").GetComponent<GridLayoutGroup>();
            Grid.cellSize = new Vector2(inSize.x - 5f, 32f);
            popup.transform.Find("Scrollbar").AsRT().sizeDelta = new Vector2(32f, inSize.y);

            CloseButton = TopPanel.Find("x").GetComponent<Button>();
            CloseButton.onClick.ClearAll();
            CloseButton.onClick.AddListener(() =>
            {
                EditorManager.inst.HideDialog(name);
                close?.Invoke();
            });

            SearchField = popup.transform.Find("search-box/search").GetComponent<InputField>();
            SearchField.onValueChanged.ClearAll();
            SearchField.onValueChanged.AddListener(_val => refreshSearch?.Invoke(_val));
            SearchField.GetPlaceholderText().text = placeholderText;
            Content = popup.transform.Find("mask/content");

            EditorHelper.AddEditorPopup(name, popup);

            EditorThemeManager.AddGraphic(popup.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Bottom_Left_I);

            EditorThemeManager.AddGraphic(TopPanel.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);

            EditorThemeManager.AddSelectable(CloseButton, ThemeGroup.Close);

            EditorThemeManager.AddGraphic(CloseButton.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

            EditorThemeManager.AddLightText(text);

            var scrollbar = popup.transform.Find("Scrollbar").GetComponent<Scrollbar>();
            scrollbar.value = 1f;
            EditorThemeManager.AddScrollbar(scrollbar, scrollbarRoundedSide: SpriteHelper.RoundedSide.Bottom_Right_I);

            EditorThemeManager.AddInputField(popup.transform.Find("search-box/search").GetComponent<InputField>(), ThemeGroup.Search_Field_1, 1, SpriteHelper.RoundedSide.Bottom);
        }

        #endregion
    }
}
