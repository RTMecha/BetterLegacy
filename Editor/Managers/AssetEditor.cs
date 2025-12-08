using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Settings;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Popups;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Editor class that manages level assets.
    /// </summary>
    public class AssetEditor : BaseManager<AssetEditor, EditorManagerSettings>
    {
        #region Values

        /// <summary>
        /// Asset list popup.
        /// </summary>
        public ContentPopup Popup { get; set; }

        /// <summary>
        /// Loaded external sound assets.
        /// </summary>
        public List<SoundAsset> soundAssets = new List<SoundAsset>();

        /// <summary>
        /// Loaded external sprite assets.
        /// </summary>
        public List<SpriteAsset> spriteAssets = new List<SpriteAsset>();

        #endregion

        #region Functions

        public override void OnInit()
        {
            try
            {
                Popup = RTEditor.inst.GeneratePopup(EditorPopup.ASSET_POPUP, "Edit Assets", Vector2.zero, new Vector2(600f, 400f),
                    _val => RenderPopup(), placeholderText: "Search asset...");

                EditorHelper.AddEditorDropdown("Edit Assets", string.Empty, EditorHelper.EDIT_DROPDOWN, EditorSprites.OpenSprite, OpenPopup);

                var reload = EditorPrefabHolder.Instance.SpriteButton.Duplicate(Popup.TopPanel, "reload");
                RectValues.Default.SizeDelta(32f, 32f).AssignToRectTransform(reload.transform.AsRT());
                var reloadButton = reload.GetComponent<Button>();
                reloadButton.onClick.NewListener(() => LoadAssets(RenderPopup));
                reloadButton.image.sprite = EditorSprites.ReloadSprite;

                EditorThemeManager.AddSelectable(reloadButton, ThemeGroup.Function_2, false);
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog
        }

        /// <summary>
        /// Clears the loaded assets.
        /// </summary>
        public void Clear()
        {
            soundAssets.ForLoop(soundAsset => soundAsset.UnloadAudioClip());
            soundAssets.Clear();
            spriteAssets.ForLoop(spriteAsset => spriteAsset.UnloadSprite());
            spriteAssets.Clear();
            CoreHelper.Cleanup();
        }

        /// <summary>
        /// Loads the levels' external assets.
        /// </summary>
        /// <param name="onLoad">Function to run when assets have loaded.</param>
        public void LoadAssets(Action onLoad = null) => CoroutineHelper.StartCoroutine(ILoadAssets(onLoad));

        /// <summary>
        /// Loads the levels' external assets.
        /// </summary>
        /// <param name="onLoad">Function to run when assets have loaded.</param>
        public IEnumerator ILoadAssets(Action onLoad = null)
        {
            Clear();

            var files = Directory.GetFiles(RTFile.BasePath, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var fileName = Path.GetFileName(file);
                var fileFormat = RTFile.GetFileFormat(file);
                if (fileFormat.IsAudio())
                {
                    if (CoreHelper.Equals(fileName, Level.LEVEL_OGG, Level.LEVEL_WAV, Level.LEVEL_MP3))
                        continue;

                    soundAssets.Add(new SoundAsset(fileName));
                    continue;
                }

                if (fileFormat.IsImage())
                {
                    var spriteAsset = new SpriteAsset(fileName);
                    spriteAsset.sprite = SpriteHelper.LoadSprite(file);
                    spriteAssets.Add(spriteAsset);
                }
            }

            onLoad?.Invoke();
            yield break;
        }

        /// <summary>
        /// Opens the asset list popup.
        /// </summary>
        public void OpenPopup() => OpenPopup(null, null);

        /// <summary>
        /// Opens the asset list popup.
        /// </summary>
        /// <param name="onSoundAssetSelected">Function to run when a sound asset is selected.</param>
        /// <param name="onSpriteAssetSelected">Function to select when a sprite asset is selected.</param>
        /// <param name="showSounds">If sound assets should display.</param>
        /// <param name="showSprites">If sprite asssets should display.</param>
        public void OpenPopup(Action<string> onSoundAssetSelected, Action<string> onSpriteAssetSelected, bool showSounds = true, bool showSprites = true)
        {
            if (!EditorManager.inst.hasLoadedLevel)
                return;

            Popup.Open();
            RenderPopup(onSoundAssetSelected, onSpriteAssetSelected, showSounds, showSprites);
        }

        /// <summary>
        /// Renders the asset list popup.
        /// </summary>
        public void RenderPopup() => RenderPopup(null, null);

        /// <summary>
        /// Renders the asset list popup.
        /// </summary>
        /// <param name="onSoundAssetSelected">Function to run when a sound asset is selected.</param>
        /// <param name="onSpriteAssetSelected">Function to select when a sprite asset is selected.</param>
        /// <param name="showSounds">If sound assets should display.</param>
        /// <param name="showSprites">If sprite asssets should display.</param>
        public void RenderPopup(Action<string> onSoundAssetSelected, Action<string> onSpriteAssetSelected, bool showSounds = true, bool showSprites = true)
        {
            Popup.ClearContent();
            Popup.SearchField.onValueChanged.NewListener(_val => RenderPopup(onSoundAssetSelected, onSpriteAssetSelected));

            int internalAssetsTotal = 0;
            int defaultSoundsTotal = 0;

            if (showSounds)
            {
                var defaultSounds = Enum.GetNames(typeof(DefaultSounds));
                for (int i = 0; i < defaultSounds.Length; i++)
                {
                    var defaultSound = (DefaultSounds)i;
                    var soundName = defaultSound.ToString();
                    if (!RTString.SearchString(Popup.SearchTerm, soundName))
                        continue;

                    var gameObject = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(Popup.Content, soundName);
                    var fileButton = gameObject.GetComponent<SpriteFunctionButtonStorage>();
                    fileButton.OnClick.ClearAll();
                    var contextClickable = gameObject.GetOrAddComponent<ContextClickable>();
                    contextClickable.onClick = pointerEventData =>
                    {
                        if (onSoundAssetSelected != null)
                        {
                            onSoundAssetSelected.Invoke(soundName);
                            return;
                        }

                        SoundManager.inst.PlaySound(defaultSound);
                    };

                    fileButton.Sprite = EditorSprites.SoundSprite;
                    fileButton.Text = soundName + " (Default)";

                    EditorThemeManager.ApplyGraphic(fileButton.image, ThemeGroup.Light_Text);
                    EditorThemeManager.ApplySelectable(fileButton.button, ThemeGroup.List_Button_1);
                    EditorThemeManager.ApplyLightText(fileButton.label);
                    defaultSoundsTotal++;
                    internalAssetsTotal++;
                }

                var internalSounds = soundAssets.FindAll(x => RTString.SearchString(Popup.SearchTerm, x.name));
                if (internalSounds.Count > 0 && defaultSoundsTotal > 0)
                {
                    var spacer = Creator.NewUIObject("spacer", Popup.Content);
                    RectValues.Default.SizeDelta(0f, 16f).AssignToRectTransform(spacer.transform.AsRT());
                    var spacerImage = spacer.AddComponent<Image>();
                    EditorThemeManager.ApplyGraphic(spacerImage, ThemeGroup.Background_3, true);
                }

                for (int i = 0; i < internalSounds.Count; i++)
                {
                    var soundAsset = internalSounds[i];

                    var gameObject = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(Popup.Content, soundAsset.name);
                    var fileButton = gameObject.GetComponent<SpriteFunctionButtonStorage>();
                    fileButton.OnClick.ClearAll();
                    var contextClickable = gameObject.GetOrAddComponent<ContextClickable>();
                    contextClickable.onClick = pointerEventData =>
                    {
                        if (pointerEventData.button == PointerEventData.InputButton.Right)
                        {
                            var buttonFunctions = new List<EditorElement>()
                            {
                                new ButtonElement("Play", () =>
                                {
                                    if (!soundAsset.audio)
                                        CoroutineHelper.StartCoroutine(soundAsset.LoadAudioClip(() => SoundManager.inst.PlaySound(soundAsset.audio)));
                                    else
                                        SoundManager.inst.PlaySound(soundAsset.audio);
                                }),
                                new ButtonElement("Load", () =>
                                {
                                    CoroutineHelper.StartCoroutine(soundAsset.LoadAudioClip(() => EditorManager.inst.DisplayNotification($"Loaded audio clip!", 2f, EditorManager.NotificationType.Success)));
                                }),
                                new ButtonElement("Unload", soundAsset.UnloadAudioClip),
                            };

                            if (!GameData.Current.assets.sounds.Has(x => x.name == soundAsset.name))
                            {
                                buttonFunctions.Add(new SpacerElement());
                                buttonFunctions.Add(new ButtonElement("Import", () =>
                                {
                                    GameData.Current.assets.AddAndLoadSound(soundAsset.name);
                                    RenderPopup();
                                }));
                            }

                            buttonFunctions.Add(new SpacerElement());
                            buttonFunctions.Add(new ButtonElement("Reload Assets", () => LoadAssets(RenderPopup)));

                            EditorContextMenu.inst.ShowContextMenu(buttonFunctions);
                            return;
                        }

                        if (onSoundAssetSelected != null)
                        {
                            onSoundAssetSelected.Invoke(soundAsset.name);
                            return;
                        }

                        if (!soundAsset.audio)
                        {
                            if (EditorConfig.Instance.LoadSoundAssetOnClick.Value)
                                CoroutineHelper.StartCoroutine(soundAsset.LoadAudioClip(() =>
                                {
                                    SoundManager.inst.PlaySound(soundAsset.audio);
                                    EditorManager.inst.DisplayNotification($"Loaded and played {soundAsset.name}!", 2f, EditorManager.NotificationType.Success);
                                }));
                            else
                                EditorManager.inst.DisplayNotification($"Sound asset hasn't been loaded yet.", 2f, EditorManager.NotificationType.Warning);
                        }
                        else
                        {
                            SoundManager.inst.PlaySound(soundAsset.audio);
                            EditorManager.inst.DisplayNotification($"Played {soundAsset.name}!", 2f, EditorManager.NotificationType.Success);
                        }
                    };

                    fileButton.Sprite = EditorSprites.SoundSprite;
                    fileButton.Text = soundAsset.name + " (External)";

                    EditorThemeManager.ApplyGraphic(fileButton.image, ThemeGroup.Light_Text);
                    EditorThemeManager.ApplySelectable(fileButton.button, ThemeGroup.List_Button_1);
                    EditorThemeManager.ApplyLightText(fileButton.label);
                    internalAssetsTotal++;
                }
            }
            if (showSprites)
            {
                for (int i = 0; i < spriteAssets.Count; i++)
                {
                    var spriteAsset = spriteAssets[i];
                    if (!RTString.SearchString(Popup.SearchTerm, spriteAsset.name))
                        continue;

                    var gameObject = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(Popup.Content, spriteAsset.name);
                    var fileButton = gameObject.GetComponent<SpriteFunctionButtonStorage>();
                    fileButton.OnClick.ClearAll();
                    var contextClickable = gameObject.GetOrAddComponent<ContextClickable>();
                    contextClickable.onClick = pointerEventData =>
                    {
                        if (pointerEventData.button == PointerEventData.InputButton.Right)
                        {
                            EditorContextMenu.inst.ShowContextMenu(
                                new ButtonElement("Import", () =>
                                {
                                    GameData.Current.assets.AddSprite(spriteAsset.name, spriteAsset.sprite);
                                    RenderPopup();
                                }),
                                new SpacerElement(),
                                new ButtonElement("Reload Assets", () => LoadAssets(RenderPopup)));
                            return;
                        }

                        onSpriteAssetSelected?.Invoke(spriteAsset.name);
                    };

                    fileButton.Sprite = spriteAsset.sprite ?? EditorSprites.PALogo;
                    fileButton.Text = spriteAsset.name + " (External)";

                    EditorThemeManager.ApplyGraphic(fileButton.image, ThemeGroup.Light_Text);
                    EditorThemeManager.ApplySelectable(fileButton.button, ThemeGroup.List_Button_1);
                    EditorThemeManager.ApplyLightText(fileButton.label);
                    internalAssetsTotal++;
                }
            }

            int total = 0;

            var sounds = GameData.Current.assets.sounds.FindAll(x => RTString.SearchString(Popup.SearchTerm, x.name));
            var sprites = GameData.Current.assets.sprites.FindAll(x => RTString.SearchString(Popup.SearchTerm, x.name));

            if (showSounds)
                total += sounds.Count;
            if (showSprites)
                total += sprites.Count;

            if (total > 0 && internalAssetsTotal > 0)
            {
                var spacer = Creator.NewUIObject("spacer", Popup.Content);
                RectValues.Default.SizeDelta(0f, 16f).AssignToRectTransform(spacer.transform.AsRT());
                var spacerImage = spacer.AddComponent<Image>();
                EditorThemeManager.ApplyGraphic(spacerImage, ThemeGroup.Background_3, true);
            }

            if (showSounds)
                for (int i = 0; i < sounds.Count; i++)
                {
                    var soundAsset = sounds[i];

                    var gameObject = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(Popup.Content, soundAsset.name);
                    var fileButton = gameObject.GetComponent<SpriteFunctionButtonStorage>();
                    fileButton.OnClick.ClearAll();
                    var contextClickable = gameObject.GetOrAddComponent<ContextClickable>();
                    contextClickable.onClick = pointerEventData =>
                    {
                        if (pointerEventData.button == PointerEventData.InputButton.Right)
                        {
                            EditorContextMenu.inst.ShowContextMenu(
                                new ButtonElement("Play", () =>
                                {
                                    if (!soundAsset.audio)
                                        CoroutineHelper.StartCoroutine(soundAsset.LoadAudioClip(() => SoundManager.inst.PlaySound(soundAsset.audio)));
                                    else
                                        SoundManager.inst.PlaySound(soundAsset.audio);
                                }),
                                new ButtonElement("Load", () =>
                                {
                                    CoroutineHelper.StartCoroutine(soundAsset.LoadAudioClip(() => EditorManager.inst.DisplayNotification($"Loaded audio clip!", 2f, EditorManager.NotificationType.Success)));
                                }),
                                new ButtonElement("Unload", soundAsset.UnloadAudioClip),
                                new ButtonElement("Remove", () =>
                                {
                                    RTEditor.inst.ShowWarningPopup("Are you sure you want to remove this sound from the asset list?", () =>
                                    {
                                        if (soundAsset.audio)
                                            CoreHelper.Destroy(soundAsset.audio);
                                        GameData.Current.assets.RemoveSound(soundAsset.name);
                                        RenderPopup();
                                        RTEditor.inst.HideWarningPopup();
                                    }, RTEditor.inst.HideWarningPopup);
                                }),
                                new SpacerElement(),
                                new ButtonElement($"Autoload Sound [{(soundAsset.autoLoad ? "On" : "Off")}]", () =>
                                {
                                    soundAsset.autoLoad = !soundAsset.autoLoad;
                                }),
                                new SpacerElement(),
                                new ButtonElement("Reload Assets", () => LoadAssets(RenderPopup)));
                            return;
                        }

                        if (onSoundAssetSelected != null)
                        {
                            onSoundAssetSelected.Invoke(soundAsset.name);
                            return;
                        }

                        if (!soundAsset.audio)
                        {
                            if (EditorConfig.Instance.LoadSoundAssetOnClick.Value)
                                CoroutineHelper.StartCoroutine(soundAsset.LoadAudioClip(() =>
                                {
                                    SoundManager.inst.PlaySound(soundAsset.audio);
                                    EditorManager.inst.DisplayNotification($"Loaded and played {soundAsset.name}!", 2f, EditorManager.NotificationType.Success);
                                }));
                            else
                                EditorManager.inst.DisplayNotification($"Sound asset hasn't been loaded yet.", 2f, EditorManager.NotificationType.Warning);
                        }
                        else
                        {
                            SoundManager.inst.PlaySound(soundAsset.audio);
                            EditorManager.inst.DisplayNotification($"Played {soundAsset.name}!", 2f, EditorManager.NotificationType.Success);
                        }
                    };

                    fileButton.Sprite = EditorSprites.SoundSprite;
                    fileButton.Text = soundAsset.name + " (Internal)";

                    EditorThemeManager.ApplyGraphic(fileButton.image, ThemeGroup.Light_Text);
                    EditorThemeManager.ApplySelectable(fileButton.button, ThemeGroup.List_Button_1);
                    EditorThemeManager.ApplyLightText(fileButton.label);
                }
            if (showSprites)
                for (int i = 0; i < sprites.Count; i++)
                {
                    var spriteAsset = sprites[i];

                    var gameObject = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(Popup.Content, spriteAsset.name);
                    var fileButton = gameObject.GetComponent<SpriteFunctionButtonStorage>();
                    fileButton.OnClick.ClearAll();
                    var contextClickable = gameObject.GetOrAddComponent<ContextClickable>();
                    contextClickable.onClick = pointerEventData =>
                    {
                        if (pointerEventData.button == PointerEventData.InputButton.Right)
                        {
                            var buttonFunctions = new List<EditorElement>()
                            {
                                new ButtonElement("Remove", () =>
                                {
                                    RTEditor.inst.ShowWarningPopup("Are you sure you want to remove this sprite from the asset list? This will also remove the sprite from all image objects that have a reference to this sprite.", () =>
                                    {
                                        GameData.Current.assets.RemoveSprite(spriteAsset.name);
                                        RenderPopup();
                                        RTLevel.Reinit();
                                        RTEditor.inst.HideWarningPopup();
                                    }, RTEditor.inst.HideWarningPopup);
                                }),
                                new SpacerElement(),
                                new ButtonElement("Reload Assets", () => LoadAssets(RenderPopup)),
                                new SpacerElement(),
                                new ButtonElement((spriteAsset.wrapMode == TextureWrapMode.Repeat ? "> " : string.Empty) + "Repeat Wrap Mode", () =>
                                {
                                    spriteAsset.SetWrapMode(TextureWrapMode.Repeat);
                                    EditorManager.inst.DisplayNotification("The texture will now repeat!", 2f, EditorManager.NotificationType.Success);
                                }),
                                new ButtonElement((spriteAsset.wrapMode == TextureWrapMode.Clamp ? "> " : string.Empty) + "Clamp Wrap Mode", () =>
                                {
                                    spriteAsset.SetWrapMode(TextureWrapMode.Clamp);
                                    EditorManager.inst.DisplayNotification("The texture will now clamp!", 2f, EditorManager.NotificationType.Success);
                                }),
                                new ButtonElement((spriteAsset.wrapMode == TextureWrapMode.Mirror ? "> " : string.Empty) + "Mirror Wrap Mode", () =>
                                {
                                    spriteAsset.SetWrapMode(TextureWrapMode.Mirror);
                                    EditorManager.inst.DisplayNotification("The texture will now repeat and mirror!", 2f, EditorManager.NotificationType.Success);
                                }),
                            };

                            EditorContextMenu.inst.ShowContextMenu(buttonFunctions);
                            return;
                        }

                        onSpriteAssetSelected?.Invoke(spriteAsset.name);
                    };

                    fileButton.Sprite = spriteAsset.sprite ?? EditorSprites.PALogo;
                    fileButton.Text = spriteAsset.name + " (Internal)";

                    EditorThemeManager.ApplyGraphic(fileButton.image, ThemeGroup.Light_Text);
                    EditorThemeManager.ApplySelectable(fileButton.button, ThemeGroup.List_Button_1);
                    EditorThemeManager.ApplyLightText(fileButton.label);
                }
        }

        #endregion
    }
}
