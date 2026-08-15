using System;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Elements
{
    public class PlayerModelPanel : EditorPanel<PlayerModel>, ISelectable
    {
        #region Constructors

        public PlayerModelPanel(int index) : base() => this.index = index;

        public PlayerModelPanel(ObjectSource source, int index) : this(index) => this.Source = source;

        #endregion

        #region Values

        #region UI

        /// <summary>
        /// The icon of the player model panel.
        /// </summary>
        public Image IconImage { get; set; }

        /// <summary>
        /// The delete button of the player model panel.
        /// </summary>
        public DeleteButtonStorage DeleteButton { get; set; }

        public Action<PointerEventData> onClick;

        #endregion

        #region Data

        public override string DisplayName => Item.basePart.name;

        /// <summary>
        /// What dialog this player model panel is located in.
        /// </summary>
        public ObjectSource Source { get; set; }

        /// <summary>
        /// If the player model panel is external.
        /// </summary>
        public bool IsExternal => Source == ObjectSource.External;

        public override float FocusSize => EditorConfig.Instance.OpenLevelButtonHoverSize.Value;

        public bool Selected { get; set; }

        #endregion

        #region Asset Pack

        #region Asset Packs

        //public static string labelFormat = "{0}";

        public static TextAnchor labelAlignment = TextAnchor.MiddleLeft;

        public static HorizontalWrapMode labelHorizontalWrap = HorizontalWrapMode.Wrap;

        public static VerticalWrapMode labelVerticalWrap = VerticalWrapMode.Truncate;

        public static int labelFontSize = 20;

        #region Internal

        public static RectValues internalBaseRect = new RectValues(Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 1f), RectValues.CenterPivot, new Vector2(584f, 32f));

        public static RectValues internalIconRect = RectValues.Default.AnchoredPosition(-276f, 0f).SizeDelta(26f, 26f);

        public static RectValues internalNameLabelRect = RectValues.FullAnchored.AnchoredPosition(32f, 0f).SizeDelta(-12f, -8f);

        public static TextAnchor internalNameLabelAlignment = TextAnchor.MiddleLeft;

        public static HorizontalWrapMode internalNameLabelHorizontalWrap = HorizontalWrapMode.Overflow;

        public static VerticalWrapMode internalNameLabelVerticalWrap = VerticalWrapMode.Overflow;

        public static int internalNameLabelFontSize = 20;

        public static RectValues internalDeleteRect = new RectValues(Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(32f, 0f));

        #endregion

        #region External

        public static RectValues externalBaseRect = new RectValues(Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 1f), RectValues.CenterPivot, new Vector2(584f, 32f));

        public static RectValues externalIconRect = RectValues.Default.AnchoredPosition(-276f, 0f).SizeDelta(26f, 26f);

        public static RectValues externalNameLabelRect = RectValues.FullAnchored.AnchoredPosition(32f, 0f).SizeDelta(-12f, -8f);

        public static TextAnchor externalNameLabelAlignment = TextAnchor.MiddleLeft;

        public static HorizontalWrapMode externalNameLabelHorizontalWrap = HorizontalWrapMode.Overflow;

        public static VerticalWrapMode externalNameLabelVerticalWrap = VerticalWrapMode.Overflow;

        public static int externalNameLabelFontSize = 20;

        public static RectValues externalDeleteRect = new RectValues(Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(32f, 0f));

        #endregion

        #endregion

        #endregion

        #endregion

        #region Functions

        public override void Init(PlayerModel item)
        {
            Item = item;
            Path = item.path;
            item.editorPanel = this;

            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(PlayerEditor.inst.ModelsPopup.GetPopup(Source).Content, $"Folder [{Name}]");
            (IsExternal ? externalBaseRect : internalBaseRect).AssignToRectTransform(gameObject.transform.AsRT());
            GameObject = gameObject;
            var folderButtonFunction = gameObject.AddComponent<FolderButtonFunction>();

            HoverFocus = gameObject.AddComponent<HoverUI>();
            HoverFocus.size = FocusSize;
            HoverFocus.animatePos = false;
            HoverFocus.animateSca = true;

            var folderButtonStorage = gameObject.GetComponent<FunctionButtonStorage>();
            Label = folderButtonStorage.label;
            Label.enabled = true;
            (IsExternal ? externalNameLabelRect : internalNameLabelRect).AssignToRectTransform(Label.rectTransform);
            folderButtonStorage.OnClick.ClearAll();
            Button = folderButtonFunction;
            EditorThemeManager.ApplySelectable(folderButtonStorage.button, ThemeGroup.List_Button_1);
            EditorThemeManager.ApplyLightText(folderButtonStorage.label);

            var iconBase = Creator.NewUIObject("icon base", gameObject.transform);
            var iconBaseImage = iconBase.AddComponent<Image>();
            iconBase.AddComponent<Mask>().showMaskGraphic = false;
            (IsExternal ? externalIconRect : internalIconRect).AssignToRectTransform(iconBaseImage.rectTransform);
            EditorThemeManager.ApplyGraphic(iconBaseImage, ThemeGroup.Null, true);

            var icon = Creator.NewUIObject("icon", iconBase.transform);
            RectValues.FullAnchored.AssignToRectTransform(icon.transform.AsRT());

            IconImage = icon.AddComponent<Image>();

            //SelectedUI = Creator.NewUIObject("selected", gameObject.transform);
            //SelectedUI.SetActive(false);
            //var selectedImage = SelectedUI.AddComponent<Image>();
            //selectedImage.color = LSColors.HexToColorAlpha("0088FF25");

            //RectValues.FullAnchored.AssignToRectTransform(selectedImage.rectTransform);

            if (!Item.IsDefault)
            {
                var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(gameObject.transform, "Delete");
                (IsExternal ? externalDeleteRect : internalDeleteRect).AssignToRectTransform(delete.transform.AsRT());
                DeleteButton = delete.GetComponent<DeleteButtonStorage>();
                EditorThemeManager.ApplyDeleteButton(DeleteButton);
            }

            Render();
        }

        public override void Render()
        {
            RenderIcon();
            RenderLabel();
            RenderHover();
            RenderTooltip();
            UpdateFunction();
            UpdateDeleteFunction();
        }

        /// <summary>
        /// Renders the level panel icon.
        /// </summary>
        public void RenderIcon()
        {
            if (isFolder)
                return;

            RenderIcon(Item?.icon ?? EditorSprites.PlayerSprite);
        }

        /// <summary>
        /// Renders the level panel icon.
        /// </summary>
        /// <param name="icon">Icon of the level panel.</param>
        public void RenderIcon(Sprite icon)
        {
            if (IconImage)
                IconImage.sprite = icon;
        }

        public override void RenderLabel(string text)
        {
            Label.text = text;

            Label.alignment = labelAlignment;
            Label.horizontalOverflow = labelHorizontalWrap;
            Label.verticalOverflow = labelVerticalWrap;
            Label.fontSize = labelFontSize;
        }

        public void UpdateFunction()
        {
            if (isFolder)
            {
                Button.onClick = pointerEventData =>
                {
                    var path = Path;
                    if (!path.Contains(RTEditor.inst.BeatmapsPath + "/"))
                    {
                        EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                        return;
                    }

                    if (pointerEventData.button == PointerEventData.InputButton.Right)
                    {
                        EditorContextMenu.inst.ShowContextMenu(EditorContextMenu.GetFolderPanelFunctions(this, RenderIcon,
                            onOpenFolder: () =>
                            {
                                PlayerEditor.inst.ModelsPopup.External.PathField.text = path.Remove(RTEditor.inst.BeatmapsPath + "/");
                                PlayerEditor.inst.Reload();
                            },
                            onFolderUpdate: () => PlayerEditor.inst.Reload(),
                            paste: PlayerEditor.inst.PastePlayerModel));
                        return;
                    }

                    PlayerEditor.inst.ModelsPopup.External.PathField.text = path.Remove(RTEditor.inst.BeatmapsPath + "/");
                    PlayerEditor.inst.Reload();
                };
            }

            Button.onClick = pointerEventData =>
            {
                switch (Source)
                {
                    case ObjectSource.Internal: {
                            if (pointerEventData.button == PointerEventData.InputButton.Right)
                            {
                                EditorContextMenu.inst.ShowContextMenu(
                                    new ButtonElement("Open & Use", () =>
                                    {
                                        PlayersData.Current.SetPlayerModel(PlayerEditor.inst.playerIndex, Item.basePart.id);
                                        PlayerManager.RespawnPlayers();
                                        PlayerEditor.inst.RenderDialog();
                                    }),
                                    new ButtonElement("Set to Global", () => PlayerManager.PlayerIndexes[PlayerEditor.inst.playerIndex].Value = Item.basePart.id),
                                    new ButtonElement("Create New", PlayerEditor.inst.CreateNewModel),
                                    new ButtonElement("Save", PlayerEditor.inst.Save),
                                    new ButtonElement("Reload", PlayerEditor.inst.Reload),
                                    new SpacerElement(),
                                    new ButtonElement("Duplicate", () =>
                                    {
                                        var dup = PlayersData.Current.DuplicatePlayerModel(Item.basePart.id);
                                        PlayersData.externalPlayerModels[dup.basePart.id] = dup;
                                        if (dup)
                                            PlayersData.Current.SetPlayerModel(PlayerEditor.inst.playerIndex, dup.basePart.id);
                                    }),
                                    new ButtonElement("Delete", () =>
                                    {
                                        if (index < 5)
                                        {
                                            EditorManager.inst.DisplayNotification($"Cannot delete a default player model.", 2f, EditorManager.NotificationType.Warning);
                                            return;
                                        }

                                        RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this Player Model?", () =>
                                        {
                                            PlayersData.Current.SetPlayerModel(PlayerEditor.inst.playerIndex, PlayerModel.DEFAULT_ID);
                                            PlayersData.externalPlayerModels.Remove(Item.basePart.id);
                                            PlayersData.Current.playerModels.Remove(Item.basePart.id);
                                            PlayerManager.RespawnPlayers();
                                            PlayerEditor.inst.RenderDialog();
                                            PlayerEditor.inst.RenderInternalPlayerModelsPopup(PlayerEditor.inst.onSelectModel);
                                            PlayerEditor.inst.RenderExternalPlayerModelsPopup();
                                        });
                                    }));
                                return;
                            }

                            if (onClick != null)
                            {
                                onClick.Invoke(pointerEventData);
                                return;
                            }

                            PlayerEditor.inst.SetCurrentModel(Item);
                            break;
                        }
                    case ObjectSource.External: {
                            if (pointerEventData.button == PointerEventData.InputButton.Right)
                            {
                                //TODO: ADD CONTEXT MENU
                                return;
                            }

                            PlayerEditor.inst.ImportPlayerModel(Item);
                            break;
                        }
                }
            };
        }

        /// <summary>
        /// Updates the player model panels' deletion function
        /// </summary>
        public void UpdateDeleteFunction()
        {
            if (DeleteButton)
                DeleteButton.OnClick.NewListener(() => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this Player Model?", () =>
                {
                    PlayersData.Current.SetPlayerModel(PlayerEditor.inst.playerIndex, PlayerModel.DEFAULT_ID);
                    if (IsExternal)
                    {
                        PlayersData.externalPlayerModels.Remove(Item.basePart.id);
                        RTFile.DeleteFile(Path);
                    }
                    else
                        PlayersData.Current.playerModels.Remove(Item.basePart.id);
                    PlayerManager.RespawnPlayers();
                    PlayerEditor.inst.RenderDialog();
                    PlayerEditor.inst.RenderInternalPlayerModelsPopup(PlayerEditor.inst.onSelectModel);
                    PlayerEditor.inst.RenderExternalPlayerModelsPopup();
                }));
        }

        public override string ToString() => isFolder ? Name : Item?.ToString();

        #endregion
    }
}
