using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Elements
{
    /// <summary>
    /// Object for storing prefab panel data.
    /// </summary>
    public class PrefabPanel : EditorPanel<Prefab>, ISelectable
    {
        #region Constructors

        public PrefabPanel() { }

        public PrefabPanel(int index) => this.index = index;

        public PrefabPanel(ObjectSource source, int index) : this(index) => Source = source;

        #endregion

        #region Values

        #region UI

        /// <summary>
        /// The delete button for the prefab panel.
        /// </summary>
        public Button DeleteButton { get; set; }

        /// <summary>
        /// The type text of the prefab panel.
        /// </summary>
        public Text TypeText { get; set; }

        /// <summary>
        /// The base image for the prefab type of the prefab panel.
        /// </summary>
        public Image TypeImage { get; set; }

        /// <summary>
        /// The icon for the prefab type of the prefab panel.
        /// </summary>
        public Image TypeIcon { get; set; }

        #endregion

        #region Data

        public override float FocusSize => EditorConfig.Instance.PrefabButtonHoverSize.Value;

        /// <summary>
        /// What dialog this prefab panel is located in.
        /// </summary>
        public ObjectSource Source { get; set; }

        /// <summary>
        /// If the prefab panel is external.
        /// </summary>
        public bool IsExternal => Source == ObjectSource.External;

        /// <summary>
        /// The icon of the prefab panel.
        /// </summary>
        public Image IconImage { get; set; }

        public bool Selected { get; set; }

        public override string DisplayName => isFolder ? System.IO.Path.GetFileName(Path) : Item.name;

        #endregion

        #region Asset Packs

        #region Internal

        public static RectValues internalBaseRect = new RectValues(Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 1f), RectValues.CenterPivot, new Vector2(584f, 32f));

        public static RectValues internalTypeBaseRect = new RectValues(new Vector2(0f, -16f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(32f, 32f));

        public static RectValues internalTypeShadeRect = RectValues.FullAnchored.SizeDelta(-4f, -4f);

        public static RectValues internalTypeIconRect = RectValues.FullAnchored;

        public static RectValues internalNameLabelRect = new RectValues(new Vector2(163.4375f, -16f), new Vector2(0f, 1f), new Vector2(0f, 1f), RectValues.CenterPivot, new Vector2(246.875f, 32f));

        public static TextAnchor internalNameLabelAlignment = TextAnchor.MiddleLeft;

        public static HorizontalWrapMode internalNameLabelHorizontalWrap = HorizontalWrapMode.Overflow;

        public static VerticalWrapMode internalNameLabelVerticalWrap = VerticalWrapMode.Overflow;

        public static int internalNameLabelFontSize = 20;

        public static RectValues internalTypeLabelRect = new RectValues(new Vector2(368.9375f, -16f), new Vector2(0f, 1f), new Vector2(0f, 1f), RectValues.CenterPivot, new Vector2(148.125f, 32f));

        public static TextAnchor internalTypeLabelAlignment = TextAnchor.MiddleLeft;

        public static HorizontalWrapMode internalTypeLabelHorizontalWrap = HorizontalWrapMode.Overflow;

        public static VerticalWrapMode internalTypeLabelVerticalWrap = VerticalWrapMode.Overflow;

        public static int internalTypeLabelFontSize = 20;

        public static RectValues internalDeleteRect = new RectValues(new Vector2(467f, -16f), new Vector2(0f, 1f), new Vector2(0f, 1f), RectValues.CenterPivot, new Vector2(32f, 32f));

        #endregion

        #region External

        public static RectValues externalBaseRect = new RectValues(Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 1f), RectValues.CenterPivot, new Vector2(584f, 32f));

        public static RectValues externalTypeBaseRect = new RectValues(new Vector2(0f, -16f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(32f, 32f));

        public static RectValues externalTypeShadeRect = RectValues.FullAnchored.SizeDelta(-4f, -4f);

        public static RectValues externalTypeIconRect = RectValues.FullAnchored;

        public static RectValues externalNameLabelRect = new RectValues(new Vector2(163.4375f, -16f), new Vector2(0f, 1f), new Vector2(0f, 1f), RectValues.CenterPivot, new Vector2(246.875f, 32f));

        public static TextAnchor externalNameLabelAlignment = TextAnchor.MiddleLeft;

        public static HorizontalWrapMode externalNameLabelHorizontalWrap = HorizontalWrapMode.Overflow;

        public static VerticalWrapMode externalNameLabelVerticalWrap = VerticalWrapMode.Overflow;

        public static int externalNameLabelFontSize = 20;

        public static RectValues externalTypeLabelRect = new RectValues(new Vector2(368.9375f, -16f), new Vector2(0f, 1f), new Vector2(0f, 1f), RectValues.CenterPivot, new Vector2(148.125f, 32f));

        public static TextAnchor externalTypeLabelAlignment = TextAnchor.MiddleLeft;

        public static HorizontalWrapMode externalTypeLabelHorizontalWrap = HorizontalWrapMode.Overflow;

        public static VerticalWrapMode externalTypeLabelVerticalWrap = VerticalWrapMode.Overflow;

        public static int externalTypeLabelFontSize = 20;

        public static RectValues externalDeleteRect = new RectValues(new Vector2(467f, -16f), new Vector2(0f, 1f), new Vector2(0f, 1f), RectValues.CenterPivot, new Vector2(32f, 32f));

        #endregion

        #endregion

        #endregion

        #region Functions

        public override void Init(string directory)
        {
            var path = RTFile.ReplaceSlash(directory);
            var fileName = System.IO.Path.GetFileName(directory);

            var gameObjectFolder = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(PrefabEditor.inst.externalContent, $"Folder [{fileName}]");
            externalBaseRect.AssignToRectTransform(gameObjectFolder.transform.AsRT());
            var folderButtonStorageFolder = gameObjectFolder.GetComponent<SpriteFunctionButtonStorage>();
            var folderButtonFunctionFolder = gameObjectFolder.AddComponent<FolderButtonFunction>();

            var hover = gameObjectFolder.AddComponent<HoverUI>();
            hover.animatePos = false;
            hover.animateSca = true;

            folderButtonStorageFolder.OnClick.ClearAll();
            folderButtonFunctionFolder.onClick = eventData =>
            {
                if (!path.Contains(RTEditor.inst.BeatmapsPath + "/"))
                {
                    EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                    return;
                }

                if (eventData.button == PointerEventData.InputButton.Right)
                {
                    EditorContextMenu.inst.ShowContextMenu(EditorContextMenu.GetFolderPanelFunctions(this, RenderIcon,
                        onOpenFolder: () =>
                        {
                            RTPrefabEditor.inst.Popups.External.PathField.text = path.Remove(RTEditor.inst.BeatmapsPath + "/");
                            RTEditor.inst.UpdatePrefabPath(false);
                        },
                        onFolderUpdate: () => RTPrefabEditor.inst.LoadPrefabs(RTPrefabEditor.inst.RenderExternalPrefabs),
                        paste: RTPrefabEditor.inst.PastePrefab));
                    return;
                }

                RTPrefabEditor.inst.Popups.External.PathField.text = path.Remove(RTEditor.inst.BeatmapsPath + "/");
                RTEditor.inst.UpdatePrefabPath(false);
            };

            EditorThemeManager.ApplySelectable(folderButtonStorageFolder.button, ThemeGroup.List_Button_1);
            EditorThemeManager.ApplyLightText(folderButtonStorageFolder.label);

            GameObject = gameObjectFolder;
            Label = folderButtonStorageFolder.label;
            HoverFocus = hover;
            Path = directory;
            Source = ObjectSource.External;
            isFolder = true;
            IconImage = folderButtonStorageFolder.image;

            Render();
        }

        public override void Init(Prefab prefab)
        {
            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            Item = prefab;
            Item.prefabPanel = this;
            var source = Source;

            gameObject = PrefabEditor.inst.AddPrefab.Duplicate(RTPrefabEditor.inst.Popups.GetPopup(source).Content);

            var hover = gameObject.AddComponent<HoverUI>();
            hover.animateSca = true;
            hover.animatePos = false;

            var storage = gameObject.GetComponent<PrefabPanelStorage>();

            var name = storage.label;
            var typeName = storage.typeNameText;
            var typeImage = storage.typeImage;
            var typeImageShade = storage.typeImageShade;
            var typeIconImage = storage.typeIconImage;
            var addPrefabObject = storage.button;
            var delete = storage.deleteButton;

            EditorThemeManager.ApplySelectable(addPrefabObject, ThemeGroup.List_Button_1);
            EditorThemeManager.ApplyLightText(name);
            EditorThemeManager.ApplyLightText(typeName);
            EditorThemeManager.ApplyGraphic(delete.image, ThemeGroup.Delete, true);
            EditorThemeManager.ApplyGraphic(delete.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Delete_Text);
            EditorThemeManager.ApplyGraphic(typeImage, ThemeGroup.Null, true);
            EditorThemeManager.ApplyGraphic(typeImageShade, ThemeGroup.Null, true);

            TooltipHelper.AssignTooltip(gameObject, $"{source} Prefab List Button", 3.2f);

            addPrefabObject.onClick.ClearAll();
            delete.onClick.ClearAll();

            GameObject = gameObject;
            HoverFocus = hover;
            Button = gameObject.AddComponent<FolderButtonFunction>();
            DeleteButton = delete;
            Label = name;
            TypeText = typeName;
            TypeImage = typeImage;
            TypeIcon = typeIconImage;
            Path = prefab.filePath;

            Render();
            SetActive(RTPrefabEditor.inst.ContainsName(Item, source));
        }

        public override void Render()
        {
            var gameObject = GameObject;
            if (!gameObject)
                return;

            var rect = IsExternal ? externalBaseRect : internalBaseRect;
            rect.AssignToRectTransform(GameObject.transform.AsRT());

            RenderHover();

            RenderIcon();
            RenderLabel();

            if (isFolder)
            {
                RenderTooltip();
                return;
            }

            var prefab = Item;
            var prefabType = prefab.GetPrefabType();

            RenderPrefabType(prefabType);
            RenderTooltip(prefab, prefabType);
            RenderDeleteButton();
            UpdateFunction();
        }

        /// <summary>
        /// Renders the folder button's icon.
        /// </summary>
        public void RenderIcon()
        {
            if (!isFolder || !IconImage)
                return;

            IconImage.sprite = RTFile.FileExists(RTFile.CombinePaths(Path, $"folder_icon{FileFormat.PNG.Dot()}")) ?
                SpriteHelper.LoadSprite(RTFile.CombinePaths(Path, $"folder_icon{FileFormat.PNG.Dot()}")) :
                EditorSprites.OpenSprite;
        }

        public override void RenderLabel(string name)
        {
            Label.text = name;
            var isExternal = IsExternal;
            Label.alignment = isExternal ? externalNameLabelAlignment : internalNameLabelAlignment;
            Label.horizontalOverflow = isExternal ? externalNameLabelHorizontalWrap : internalNameLabelHorizontalWrap;
            Label.verticalOverflow = isExternal ? externalNameLabelVerticalWrap : internalNameLabelVerticalWrap;
            Label.fontSize = isExternal ? externalNameLabelFontSize : internalNameLabelFontSize;
        }

        /// <summary>
        /// Renders the prefab panel prefab type.
        /// </summary>
        public void RenderPrefabType() => RenderPrefabType(Item.GetPrefabType());

        /// <summary>
        /// Renders the prefab panel prefab type.
        /// </summary>
        /// <param name="prefabType">Type of the prefab.</param>
        public void RenderPrefabType(PrefabType prefabType) => RenderPrefabType(prefabType.name, prefabType.color, Item.GetIcon() ?? prefabType.icon);

        /// <summary>
        /// Renders the prefab panel prefab type.
        /// </summary>
        /// <param name="name">Name of the prefab type.</param>
        /// <param name="color">Color of the prefab type.</param>
        /// <param name="icon">Icon of the prefab type.</param>
        public void RenderPrefabType(string name, Color color, Sprite icon)
        {
            TypeText.text = name;
            TypeImage.color = color;
            TypeIcon.sprite = icon;

            var isExternal = IsExternal;
            TypeText.alignment = isExternal ? externalTypeLabelAlignment : internalTypeLabelAlignment;
            TypeText.horizontalOverflow = isExternal ? externalTypeLabelHorizontalWrap : internalTypeLabelHorizontalWrap;
            TypeText.verticalOverflow = isExternal ? externalTypeLabelVerticalWrap : internalTypeLabelVerticalWrap;
            TypeText.fontSize = isExternal ? externalTypeLabelFontSize : internalTypeLabelFontSize;
        }

        public override void RenderTooltip()
        {
            if (isFolder)
            {
                GetFolderTooltip();
                return;
            }

            RenderTooltip(Item, Item.GetPrefabType());
        }

        /// <summary>
        /// Renders the prefab panel tooltip.
        /// </summary>
        /// <param name="prefab">Prefab reference.</param>
        /// <param name="prefabType">Prefab type reference.</param>
        public void RenderTooltip(Prefab prefab, PrefabType prefabType)
        {
            TooltipHelper.AddHoverTooltip(GameObject,
                "<#" + LSColors.ColorToHex(prefabType.color) + ">" + prefab.name + "</color>",
                "Offset: " + prefab.offset +
                "<br>Type: " + prefabType.name +
                "<br>Count: " + prefab.beatmapObjects.Count +
                "<br>Description: " + prefab.description, clear: true);
        }

        /// <summary>
        /// Updates the prefab panels' function.
        /// </summary>
        /// <param name="updateCurrentPrefab">If the current quick prefab should be set instead of importing.</param>
        public void UpdateFunction()
        {
            var prefab = Item;
            switch (Source)
            {
                case ObjectSource.Internal: {
                        Button.onClick = eventData =>
                        {
                            if (RTPrefabEditor.inst.onSelectPrefab != null)
                            {
                                RTPrefabEditor.inst.onSelectPrefab.Invoke(this);
                                RTPrefabEditor.inst.onSelectPrefab = null;
                                return;
                            }

                            if (RTEditor.inst.prefabPickerEnabled)
                            {
                                var prefabInstanceID = PAObjectBase.GetStringID();
                                if (RTEditor.inst.selectingMultiple)
                                {
                                    foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                                    {
                                        if (!timelineObject.TryGetPrefabable(out IPrefabable prefabable))
                                            return;

                                        prefabable.PrefabID = prefab.id;
                                        prefabable.PrefabInstanceID = prefabInstanceID;
                                        EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                    }
                                }
                                else if (EditorTimeline.inst.CurrentSelection.TryGetPrefabable(out IPrefabable singlePrefabable))
                                {
                                    singlePrefabable.PrefabID = prefab.id;
                                    singlePrefabable.PrefabInstanceID = prefabInstanceID;
                                    EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.CurrentSelection);

                                    if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                                        ObjectEditor.inst.OpenDialog(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>());
                                    if (EditorTimeline.inst.CurrentSelection.isBackgroundObject)
                                        RTBackgroundEditor.inst.OpenDialog(EditorTimeline.inst.CurrentSelection.GetData<BackgroundObject>());
                                }

                                RTEditor.inst.prefabPickerEnabled = false;

                                return;
                            }

                            if (RTPrefabEditor.inst.selectingQuickPrefab)
                            {
                                RTPrefabEditor.inst.UpdateCurrentPrefab(prefab);
                                CoroutineHelper.StartCoroutine(RTPrefabEditor.inst.RefreshInternalPrefabs());
                                return;
                            }

                            if (eventData.button == PointerEventData.InputButton.Right)
                            {
                                var buttonFunctions = new List<EditorElement>
                                {
                                    new ButtonElement("Add to Level", () =>
                                    {
                                        RTPrefabEditor.inst.AddPrefabObjectToLevel(prefab);
                                        RTPrefabEditor.inst.Popups.Close();
                                    }),
                                    new ButtonElement("Create Prefab", () =>
                                    {
                                        PrefabEditor.inst.OpenDialog();
                                        RTPrefabEditor.inst.createInternal = true;
                                    }),
                                    new ButtonElement("Assign to Quick Prefab", () =>
                                    {
                                        RTPrefabEditor.inst.UpdateCurrentPrefab(prefab);
                                        CoroutineHelper.StartCoroutine(RTPrefabEditor.inst.RefreshInternalPrefabs());
                                    }),
                                    new SpacerElement(),
                                    new ButtonElement("Edit", () => RTPrefabEditor.inst.OpenPrefabEditorDialog(this)),
                                    new ButtonElement("Delete", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this prefab? (This is permanent!)", () => RTPrefabEditor.inst.DeleteInternalPrefab(Item))),
                                    new ButtonElement("Export", () => RTPrefabEditor.inst.SavePrefab(Item.Copy(false)), "Internal Prefab Export"),
                                    new SpacerElement(),
                                };
                                buttonFunctions.AddRange(EditorContextMenu.GetMoveIndexFunctions(GameData.Current.prefabs, index, () =>
                                {
                                    CoroutineHelper.StartCoroutine(RTPrefabEditor.inst.RefreshInternalPrefabs());
                                }));

                                EditorContextMenu.inst.ShowContextMenu(buttonFunctions);
                                return;
                            }

                            RTPrefabEditor.inst.AddPrefabObjectToLevel(prefab);
                            RTPrefabEditor.inst.Popups.Close();
                        };
                        break;
                    }
                case ObjectSource.External: {
                        Button.onClick = eventData =>
                        {
                            if (RTPrefabEditor.inst.onSelectPrefab != null)
                            {
                                RTPrefabEditor.inst.onSelectPrefab.Invoke(this);
                                RTPrefabEditor.inst.onSelectPrefab = null;
                                return;
                            }

                            if (RTEditor.inst.prefabPickerEnabled)
                                RTEditor.inst.prefabPickerEnabled = false;

                            if (RTPrefabEditor.inst.savingToPrefab && RTPrefabEditor.inst.prefabToSaveFrom != null)
                            {
                                RTPrefabEditor.inst.savingToPrefab = false;

                                var prefabToSaveTo = Item;
                                var prefabToSaveFrom = RTPrefabEditor.inst.prefabToSaveFrom;

                                prefabToSaveTo.beatmapObjects = prefabToSaveFrom.beatmapObjects.Clone();
                                prefabToSaveTo.prefabObjects = prefabToSaveFrom.prefabObjects.Clone();
                                prefabToSaveTo.backgroundObjects = prefabToSaveFrom.backgroundObjects.Clone();
                                prefabToSaveTo.backgroundLayers = prefabToSaveFrom.backgroundLayers.Clone();
                                prefabToSaveTo.prefabs = prefabToSaveFrom.prefabs.Clone();
                                prefabToSaveTo.beatmapThemes = prefabToSaveFrom.beatmapThemes.Clone();
                                prefabToSaveTo.modifierBlocks = prefabToSaveFrom.modifierBlocks.Clone();
                                prefabToSaveTo.offset = prefabToSaveFrom.offset;
                                prefabToSaveTo.type = prefabToSaveFrom.type;
                                prefabToSaveTo.typeID = prefabToSaveFrom.typeID;

                                var prefabType = prefabToSaveTo.GetPrefabType();

                                RenderLabel();
                                RenderPrefabType(prefabType);
                                RenderTooltip(prefab, prefabType);

                                prefabToSaveTo.WriteToFile(prefabToSaveTo.filePath);

                                RTPrefabEditor.inst.Popups.Close();

                                RTPrefabEditor.inst.prefabToSaveFrom = null;

                                EditorManager.inst.DisplayNotification("Applied all changes to External Prefab.", 2f, EditorManager.NotificationType.Success);

                                return;
                            }

                            if (eventData.button == PointerEventData.InputButton.Right)
                            {
                                EditorContextMenu.inst.ShowContextMenu(
                                    new ButtonElement("Import", () => RTPrefabEditor.inst.ImportPrefabIntoLevel(Item)),
                                    new ButtonElement("Update", () =>
                                    {
                                        if (RTPrefabEditor.inst.UpdateLevelPrefab(Item))
                                            EditorManager.inst.DisplayNotification($"Updated internal Prefab [ {Item.name} ]!", 2f, EditorManager.NotificationType.Success);
                                        else
                                            EditorManager.inst.DisplayNotification($"No internal Prefab was found to update!", 2f, EditorManager.NotificationType.Warning);
                                    }),
                                    new ButtonElement("Convert to VG", () => RTPrefabEditor.inst.ConvertPrefab(Item)),
                                    new ButtonElement("Open", () =>  RTPrefabEditor.inst.OpenPrefabEditorDialog(this)),
                                    new SpacerElement(),
                                    new ButtonElement("Create folder", () => RTEditor.inst.ShowFolderCreator(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PrefabPath), () => { RTPrefabEditor.inst.LoadPrefabs(RTPrefabEditor.inst.RenderExternalPrefabs); RTEditor.inst.HideNameEditor(); })),
                                    new ButtonElement("Create Prefab", () =>
                                    {
                                        PrefabEditor.inst.OpenDialog();
                                        RTPrefabEditor.inst.createInternal = false;
                                    }),
                                    new SpacerElement(),
                                    new ButtonElement("Cut", () =>
                                    {
                                        RTPrefabEditor.inst.shouldCutPrefab = true;
                                        RTPrefabEditor.inst.copiedPrefabPath = Path;
                                        EditorManager.inst.DisplayNotification($"Cut {prefab.name}!", 1.5f, EditorManager.NotificationType.Success);
                                        CoreHelper.Log($"Cut prefab: {RTPrefabEditor.inst.copiedPrefabPath}");
                                    }),
                                    new ButtonElement("Copy", () =>
                                    {
                                        RTPrefabEditor.inst.shouldCutPrefab = false;
                                        RTPrefabEditor.inst.copiedPrefabPath = Path;
                                        EditorManager.inst.DisplayNotification($"Copied {prefab.name}!", 1.5f, EditorManager.NotificationType.Success);
                                        CoreHelper.Log($"Copied prefab: {RTPrefabEditor.inst.copiedPrefabPath}");
                                    }),
                                    new ButtonElement("Paste", RTPrefabEditor.inst.PastePrefab),
                                    new ButtonElement("Delete", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this prefab? (This is permanent!)", () => RTPrefabEditor.inst.DeleteExternalPrefab(this)))
                                    );

                                return;
                            }

                            if (!EditorConfig.Instance.ImportPrefabsDirectly.Value)
                                RTPrefabEditor.inst.OpenPrefabEditorDialog(this);
                            else
                                RTPrefabEditor.inst.ImportPrefabIntoLevel(Item);
                        };
                        break;
                    }
            }
        }

        /// <summary>
        /// Renders the prefab panel delete button.
        /// </summary>
        public void RenderDeleteButton()
        {
            var deleteButton = DeleteButton;

            var rect = IsExternal ? externalDeleteRect : internalDeleteRect;
            rect.AssignToRectTransform(deleteButton.transform.AsRT());

            deleteButton.onClick.NewListener(() => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this prefab? (This is permanent!)", () =>
            {
                switch (Source)
                {
                    case ObjectSource.Internal: {
                            RTPrefabEditor.inst.DeleteInternalPrefab(Item);
                            break;
                        }
                    case ObjectSource.External: {
                            RTPrefabEditor.inst.DeleteExternalPrefab(this);
                            break;
                        }
                }
            }));
        }

        public override string ToString() => isFolder ? Name : Item?.ToString();

        #endregion
    }
}
