using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using TMPro;
using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Popups
{
    /// <summary>
    /// Represents a popup in the editor.
    /// </summary>
    public class EditorPopup : Exists
    {
        public EditorPopup() { }

        public EditorPopup(string name)
        {
            Name = name;
            UpdateCustom();
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
        /// Top panel of the editor popup.
        /// </summary>
        public RectTransform TopPanel { get; set; }
        /// <summary>
        /// Title of the editor popup.
        /// </summary>
        public Text Title { get; set; }
        public TextMeshProUGUI TMPTitle { get; set; }

        /// <summary>
        /// If the editor popup is open.
        /// </summary>
        public bool IsOpen => GameObject && GameObject.activeInHierarchy;

        #endregion

        #region Constants

        /// <summary>
        /// Path to customizable editor settings.
        /// </summary>
        public const string SETTINGS_EDITOR_PATH = "settings/editor";

        public const string NEW_FILE_POPUP = "New File Popup";
        public const string FILE_INFO_POPUP = "File Info Popup";
        public const string SAVE_AS_POPUP = "Save As Popup";
        public const string OPEN_FILE_POPUP = "Open File Popup";
        public const string QUICK_ACTIONS_POPUP = "Quick Actions Popup";
        public const string PARENT_SELECTOR = "Parent Selector";
        public const string PREFAB_POPUP = "Prefab Popup";
        public const string COLOR_PICKER = "Color Picker";
        public const string OBJECT_OPTIONS_POPUP = "Object Options Popup";
        public const string BG_OPTIONS_POPUP = "BG Options Popup";
        public const string OBJECT_TEMPLATES_POPUP = "Object Templates Popup";
        public const string BROWSER_POPUP = "Browser Popup";
        public const string OBJECT_SEARCH_POPUP = "Object Search Popup";
        public const string WARNING_POPUP = "Warning Popup";
        public const string DEBUGGER_POPUP = "Debugger Popup";
        public const string AUTOSAVE_POPUP = "Autosave Popup";
        public const string THEME_POPUP = "Theme Popup";
        public const string KEYBIND_LIST_POPUP = "Keybind List Popup";
        public const string PLAYER_MODELS_POPUP = "Player Models Popup";
        public const string DEFAULT_MODIFIERS_POPUP = "Default Modifiers Popup";
        public const string DEFAULT_BACKGROUND_MODIFIERS_POPUP = "Default Background Modifiers Popup";
        public const string DOCUMENTATION_POPUP = "Documentation Popup";
        public const string FOLDER_CREATOR_POPUP = "Folder Creator Popup";
        public const string PREFAB_TYPES_POPUP = "Prefab Types Popup";
        public const string TEXT_EDITOR = "Text Editor";
        public const string FONT_SELECTOR_POPUP = "Font Selector Popup";

        #endregion

        #region Fields

        /// <summary>
        /// Animation of the editor popup.
        /// </summary>
        public EditorAnimation editorAnimation;

        public CustomEditorAnimation customEditorAnimation;

        RTAnimation cachedRTAnim;

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
        /// Close editor popup function.
        /// </summary>
        public Action close;

        #endregion

        #region Methods

        /// <summary>
        /// Opens the editor popup.
        /// </summary>
        public virtual void Open() => PlayAnimation(true);

        /// <summary>
        /// Closes the editor popup.
        /// </summary>
        public virtual void Close() => PlayAnimation(false);

        /// <summary>
        /// Plays the popups' animation.
        /// </summary>
        /// <param name="active"></param>
        public void PlayAnimation(bool active)
        {
            var play = EditorConfig.Instance.PlayEditorAnimations.Value;

            if (play && customEditorAnimation)
            {
                if (cachedRTAnim)
                {
                    AnimationManager.inst.Remove(cachedRTAnim.id);
                    cachedRTAnim = null;
                }

                cachedRTAnim = customEditorAnimation.Play(active, this);
                return;
            }

            if (play && !editorAnimation && RTEditor.inst.editorAnimations.TryFind(x => x.name == Name, out EditorAnimation dialogAnimation))
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
        public virtual void Assign(GameObject popup)
        {
            GameObject = popup;
            if (popup.transform.TryFind("New File Popup", out Transform n))
                popup = n.gameObject;

            if (popup.transform.TryFind("Panel", out Transform topPanel))
            {
                TopPanel = topPanel.AsRT();
                if (topPanel.TryFind("Text", out Transform text))
                {
                    if (text.gameObject.TryGetComponent(out Text textText))
                        Title = textText;
                    else if (text.gameObject.TryGetComponent(out TextMeshProUGUI textTmp))
                        TMPTitle = textTmp;
                }
                else if (topPanel.TryFind("Title", out Transform title))
                {
                    if (title.gameObject.TryGetComponent(out Text textText))
                        Title = textText;
                    else if (title.gameObject.TryGetComponent(out TextMeshProUGUI textTmp))
                        TMPTitle = textTmp;
                }

                if (topPanel.TryFind("x", out Transform close) && close.gameObject.TryGetComponent(out Button closeButton))
                    CloseButton = closeButton;
            }
        }

        /// <summary>
        /// Initializes the editor popup.
        /// </summary>
        public virtual void Init()
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

            Render();
        }

        /// <summary>
        /// Renders the whole editor popup.
        /// </summary>
        public virtual void Render()
        {
            RenderTitle();
            RenderSize();
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
        public virtual void RenderSize(Vector2 size)
        {
            if (!GameObject)
                return;

            var inSize = size == Vector2.zero ? new Vector2(600f, 450f) : size;
            GameObject.transform.AsRT().anchoredPosition = defaultPosition;
            GameObject.transform.AsRT().sizeDelta = inSize;
            if (TopPanel)
                TopPanel.sizeDelta = new Vector2(inSize.x + 32f, 32f);
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
                Close();
                close?.Invoke();
            });
        }

        /// <summary>
        /// Updates the editor popups' customizable animations and layout.
        /// </summary>
        public void UpdateCustom()
        {
            var fileName = RTFile.FormatLegacyFileName(Name) + FileFormat.JSON.Dot();
            if (!RTFile.FileExists(RTFile.CombinePaths(RTFile.ApplicationDirectory, SETTINGS_EDITOR_PATH, fileName)))
            {
                customEditorAnimation = null;
                return;
            }

            try
            {
                var jn = JSON.Parse(RTFile.ReadFromFile(RTFile.CombinePaths(RTFile.ApplicationDirectory, SETTINGS_EDITOR_PATH, fileName)));

                var jnLayout = jn["layout"];
                if (jnLayout != null && GameObject)
                {
                    for (int i = 0; i < jnLayout.Count; i++)
                    {
                        string path = jnLayout[i]["path"];
                        if (GameObject.transform.TryFind(path, out Transform transform) && transform is RectTransform rectTransform)
                        {
                            var rect = RectValues.Parse(jnLayout[i]["rect"]);
                            rect.AssignToRectTransform(rectTransform);
                        }
                    }
                }

                if (jn["open"] == null || jn["close"] == null)
                {
                    customEditorAnimation = null;
                    return;
                }

                customEditorAnimation = new CustomEditorAnimation(Name);
                if (jn["open"] != null)
                    customEditorAnimation.OpenAnimation = PAAnimation.Parse(jn["open"]);
                if (jn["close"] != null)
                    customEditorAnimation.CloseAnimation = PAAnimation.Parse(jn["close"]);
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
                customEditorAnimation = null;
            }
        }

        /// <summary>
        /// Gets the original dialog.
        /// </summary>
        /// <returns>Returns the vanilla dialog class.</returns>
        public EditorManager.EditorDialog GetLegacyDialog() => EditorManager.inst.GetDialog(Name);

        public override string ToString() => $"{Name} - {title}";

        #endregion
    }
}
