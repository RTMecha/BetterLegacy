using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using LSFunctions;

using Crosstales.FB;
using SimpleJSON;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Settings;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Data.Popups;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Manages editing <see cref="PlayerModel"/>s, <see cref="PlayerControl"/> and other misc player related things.
    /// </summary>
    public class PlayerEditor : BaseManager<PlayerEditor, EditorManagerSettings>
    {
        #region Values

        /// <summary>
        /// Dialog of the editor.
        /// </summary>
        public PlayerEditorDialog Dialog { get; set; }

        /// <summary>
        /// Player Models content popup.
        /// </summary>
        public ContentPopup ModelsPopup { get; set; }

        /// <summary>
        /// Custom objects content popup.
        /// </summary>
        public ContentPopup CustomObjectsPopup { get; set; }

        /// <summary>
        /// The selected player to edit.
        /// </summary>
        public int playerIndex = 0;

        /// <summary>
        /// The currently selected custom object ID.
        /// </summary>
        public string CustomObjectID { get; set; }

        /// <summary>
        /// The currently selected player.
        /// </summary>
        public PAPlayer CurrentPlayer => PlayerManager.Players.TryGetAt(playerIndex, out PAPlayer player) ? player : null;

        /// <summary>
        /// The currently selected player model.
        /// </summary>
        public PlayerModel CurrentModel => PlayersData.Current.GetPlayerModel(playerIndex);

        /// <summary>
        /// The currently selected custom object.
        /// </summary>
        public CustomPlayerObject CurrentCustomObject => !string.IsNullOrEmpty(CustomObjectID) ? CurrentModel.customObjects.Find(x => x.id == CustomObjectID) : null;

        /// <summary>
        /// If the player control values should be edited.
        /// </summary>
        public bool editControls;

        /// <summary>
        /// The current tab for the dialog.
        /// </summary>
        public Tab CurrentTab { get; set; } = Tab.Base;

        /// <summary>
        /// The tabs for the dialog.
        /// </summary>
        public enum Tab
        {
            /// <summary>
            /// Main global settings for the players.
            /// </summary>
            Global,
            /// <summary>
            /// Main settings for a player and player model.
            /// </summary>
            Base, // includes stretch
            /// <summary>
            /// GUI display for a player.
            /// </summary>
            GUI,
            /// <summary>
            /// The head part of a player.
            /// </summary>
            Head,
            /// <summary>
            /// The boost part of a player.
            /// </summary>
            Boost,
            /// <summary>
            /// The spawners of a player.
            /// </summary>
            Spawners, // Bullet and Pulse
            /// <summary>
            /// The tail of a player.
            /// </summary>
            Tail, // All tail related parts go here
            /// <summary>
            /// The custom objects of a player.
            /// </summary>
            Custom
        }

        /// <summary>
        /// Function to run when a player model is selected.
        /// </summary>
        public Action<PlayerModel> onSelectModel;

        /// <summary>
        /// Copied custom object.
        /// </summary>
        public CustomPlayerObject copiedCustomObject;

        /// <summary>
        /// List of player models.
        /// </summary>
        public List<PlayerModelPanel> ModelPanels { get; set; } = new List<PlayerModelPanel>();

        #endregion

        #region Functions

        public override void OnInit()
        {
            try
            {
                PlayersData.Load(null);
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }

            try
            {
                Dialog = new PlayerEditorDialog();
                Dialog.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog

            ModelsPopup = RTEditor.inst.GeneratePopup(EditorPopup.PLAYER_MODELS_POPUP, "Select a Player Model", Vector2.zero, new Vector2(600f, 450f), _val => RenderModelsPopup());
            ModelsPopup.InitTopElementsParent();
            ModelsPopup.InitReload(Reload);
            ModelsPopup.onRender = () =>
            {
                if (AssetPack.TryReadFromFile("editor/ui/popups/player_models_popup.json", out string uiFile))
                {
                    var jn = JSON.Parse(uiFile);
                    RectValues.TryParse(jn["base"]["rect"], RectValues.Default.SizeDelta(600f, 450f)).AssignToRectTransform(ModelsPopup.GameObject.transform.AsRT());
                    RectValues.TryParse(jn["top_panel"]["rect"], RectValues.FullAnchored.AnchorMin(0, 1).Pivot(0f, 0f).SizeDelta(32f, 32f)).AssignToRectTransform(ModelsPopup.TopPanel);
                    RectValues.TryParse(jn["search"]["rect"], new RectValues(Vector2.zero, Vector2.one, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 32f))).AssignToRectTransform(ModelsPopup.GameObject.transform.Find("search-box").AsRT());
                    RectValues.TryParse(jn["scrollbar"]["rect"], new RectValues(Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(0f, 0.5f), new Vector2(32f, 0f))).AssignToRectTransform(ModelsPopup.GameObject.transform.Find("Scrollbar").AsRT());

                    var layoutValues = LayoutValues.Parse(jn["layout"]);
                    if (layoutValues is GridLayoutValues gridLayoutValues)
                        gridLayoutValues.AssignToLayout(ModelsPopup.Grid ? ModelsPopup.Grid : ModelsPopup.GameObject.transform.Find("mask/content").GetComponent<GridLayoutGroup>());

                    if (jn["title"] != null)
                    {
                        ModelsPopup.title = jn["title"]["text"] != null ? jn["title"]["text"] : "Select a Player Model";

                        var title = ModelsPopup.Title;
                        RectValues.TryParse(jn["title"]["rect"], RectValues.FullAnchored.AnchoredPosition(2f, 0f).SizeDelta(-12f, -8f)).AssignToRectTransform(title.rectTransform);
                        title.alignment = jn["title"]["alignment"] != null ? (TextAnchor)jn["title"]["alignment"].AsInt : TextAnchor.MiddleLeft;
                        title.fontSize = jn["title"]["font_size"] != null ? jn["title"]["font_size"].AsInt : 20;
                        title.fontStyle = (FontStyle)jn["title"]["font_style"].AsInt;
                        title.horizontalOverflow = jn["title"]["horizontal_overflow"] != null ? (HorizontalWrapMode)jn["title"]["horizontal_overflow"].AsInt : HorizontalWrapMode.Wrap;
                        title.verticalOverflow = jn["title"]["vertical_overflow"] != null ? (VerticalWrapMode)jn["title"]["vertical_overflow"].AsInt : VerticalWrapMode.Overflow;
                    }

                    if (jn["anim"] != null)
                        ModelsPopup.ReadAnimationJSON(jn["anim"]);

                    if (jn["drag_mode"] != null && ModelsPopup.Dragger)
                        ModelsPopup.Dragger.mode = (DraggableUI.DragMode)jn["drag_mode"].AsInt;
                }
            };

            CustomObjectsPopup = RTEditor.inst.GeneratePopup(EditorPopup.PLAYER_OBJECTS_POPUP, "Select a Custom Object", Vector2.zero, new Vector2(600f, 450f), _val => RenderCustomObjectsPopup());
            CustomObjectsPopup.onRender = () =>
            {
                if (AssetPack.TryReadFromFile("editor/ui/popups/player_objects_popup.json", out string uiFile))
                {
                    var jn = JSON.Parse(uiFile);
                    RectValues.TryParse(jn["base"]["rect"], RectValues.Default.SizeDelta(600f, 450f)).AssignToRectTransform(CustomObjectsPopup.GameObject.transform.AsRT());
                    RectValues.TryParse(jn["top_panel"]["rect"], RectValues.FullAnchored.AnchorMin(0, 1).Pivot(0f, 0f).SizeDelta(32f, 32f)).AssignToRectTransform(CustomObjectsPopup.TopPanel);
                    RectValues.TryParse(jn["search"]["rect"], new RectValues(Vector2.zero, Vector2.one, new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 32f))).AssignToRectTransform(CustomObjectsPopup.GameObject.transform.Find("search-box").AsRT());
                    RectValues.TryParse(jn["scrollbar"]["rect"], new RectValues(Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(0f, 0.5f), new Vector2(32f, 0f))).AssignToRectTransform(CustomObjectsPopup.GameObject.transform.Find("Scrollbar").AsRT());

                    var layoutValues = LayoutValues.Parse(jn["layout"]);
                    if (layoutValues is GridLayoutValues gridLayoutValues)
                        gridLayoutValues.AssignToLayout(CustomObjectsPopup.Grid ? CustomObjectsPopup.Grid : CustomObjectsPopup.GameObject.transform.Find("mask/content").GetComponent<GridLayoutGroup>());

                    if (jn["title"] != null)
                    {
                        CustomObjectsPopup.title = jn["title"]["text"] != null ? jn["title"]["text"] : "Select an Achievement";

                        var title = CustomObjectsPopup.Title;
                        RectValues.TryParse(jn["title"]["rect"], RectValues.FullAnchored.AnchoredPosition(2f, 0f).SizeDelta(-12f, -8f)).AssignToRectTransform(title.rectTransform);
                        title.alignment = jn["title"]["alignment"] != null ? (TextAnchor)jn["title"]["alignment"].AsInt : TextAnchor.MiddleLeft;
                        title.fontSize = jn["title"]["font_size"] != null ? jn["title"]["font_size"].AsInt : 20;
                        title.fontStyle = (FontStyle)jn["title"]["font_style"].AsInt;
                        title.horizontalOverflow = jn["title"]["horizontal_overflow"] != null ? (HorizontalWrapMode)jn["title"]["horizontal_overflow"].AsInt : HorizontalWrapMode.Wrap;
                        title.verticalOverflow = jn["title"]["vertical_overflow"] != null ? (VerticalWrapMode)jn["title"]["vertical_overflow"].AsInt : VerticalWrapMode.Overflow;
                    }

                    if (jn["anim"] != null)
                        CustomObjectsPopup.ReadAnimationJSON(jn["anim"]);

                    if (jn["drag_mode"] != null && CustomObjectsPopup.Dragger)
                        CustomObjectsPopup.Dragger.mode = (DraggableUI.DragMode)jn["drag_mode"].AsInt;
                }
            };
        }

        /// <summary>
        /// Creates a new player model.
        /// </summary>
        public void CreateNewModel()
        {
            var playerModel = PlayersData.Current.CreateNewPlayerModel();
            PlayersData.Current.SetPlayerModel(playerIndex, playerModel.basePart.id);
            PlayerManager.RespawnPlayers();
            RenderDialog();
            EditorManager.inst.DisplayNotification("Created a new player model!", 1.5f, EditorManager.NotificationType.Success);
        }

        /// <summary>
        /// Saves the player models.
        /// </summary>
        public void Save()
        {
            try
            {
                if (PlayersData.Save())
                    EditorManager.inst.DisplayNotification("Successfully saved player models!", 2f, EditorManager.NotificationType.Success);
                else
                    EditorManager.inst.DisplayNotification("Failed to save player models.", 2f, EditorManager.NotificationType.Error);
            }
            catch (Exception ex)
            {
                EditorManager.inst.DisplayNotification("Failed to save player models.", 2f, EditorManager.NotificationType.Error);
                CoreHelper.LogException(ex);
            }
        }

        /// <summary>
        /// Reloads the player models.
        /// </summary>
        public void Reload()
        {
            if (EditorLevelManager.inst.CurrentLevel)
                PlayersData.Load(EditorLevelManager.inst.CurrentLevel.GetFile(Level.PLAYERS_LSB));
            PlayerManager.RespawnPlayers();
            if (Dialog.IsCurrent)
                RenderDialog();
            if (ModelsPopup.IsOpen)
                RenderModelsPopup();
            CustomObjectsPopup.Close();

            EditorManager.inst.DisplayNotification("Loaded player models", 1.5f, EditorManager.NotificationType.Success);
        }

        /// <summary>
        /// Gets the current tab of the dialog.
        /// </summary>
        /// <returns>Returns the current tab.</returns>
        public PlayerEditorTab GetCurrentTab() => GetTab(CurrentTab);

        /// <summary>
        /// Gets a tab from the dialog.
        /// </summary>
        /// <param name="tab">Tab to get.</param>
        /// <returns>Returns a tab from the dialog.</returns>
        public PlayerEditorTab GetTab(Tab tab) => Dialog.Tabs[(int)tab];

        /// <summary>
        /// Opens the dialog.
        /// </summary>
        public void OpenDialog()
        {
            Dialog.Open();
            RenderDialog();
        }

        /// <summary>
        /// Renders the dialog.
        /// </summary>
        public void RenderDialog()
        {
            var currentModel = CurrentModel;

            var isDefault = currentModel.IsDefault;
            Dialog.Content.Find("handler").gameObject.SetActive(isDefault && CurrentTab != Tab.Global);

            Dialog.ShowTab(CurrentTab);
            GetCurrentTab().SetActive(element => element.IsActive(isDefault));

            switch (CurrentTab)
            {
                case Tab.Global: {
                        RenderGlobalTab();
                        break;
                    }
                case Tab.Base: {
                        RenderBaseTab(currentModel);
                        break;
                    }
                case Tab.GUI: {
                        RenderGUITab(currentModel);
                        break;
                    }
                case Tab.Head: {
                        RenderHeadTab(currentModel);
                        break;
                    }
                case Tab.Boost: {
                        RenderBoostTab(currentModel);
                        break;
                    }
                case Tab.Spawners: {
                        RenderSpawnersTab(currentModel);
                        break;
                    }
                case Tab.Tail: {
                        RenderTailTab(currentModel);
                        break;
                    }
                case Tab.Custom: {
                        RenderCustomTab(currentModel);
                        break;
                    }
            }
        }

        void RenderSingle(InputFieldStorage inputFieldStorage, float value, Action<string> onValueChanged, Action<string> onEndEdit = null)
        {
            inputFieldStorage.inputField.SetTextWithoutNotify(value.ToString());
            inputFieldStorage.inputField.onValueChanged.NewListener(onValueChanged);
            if (onEndEdit != null)
                inputFieldStorage.inputField.onEndEdit.NewListener(onEndEdit);
            else
                inputFieldStorage.inputField.onEndEdit.ClearAll();

            TriggerHelper.IncreaseDecreaseButtons(inputFieldStorage);
            TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDelta(inputFieldStorage.inputField));
        }

        void RenderInteger(InputFieldStorage inputFieldStorage, int value, Action<string> onValueChanged, Action<string> onEndEdit = null)
        {
            inputFieldStorage.inputField.SetTextWithoutNotify(value.ToString());
            inputFieldStorage.inputField.onValueChanged.NewListener(onValueChanged);
            if (onEndEdit != null)
                inputFieldStorage.inputField.onEndEdit.NewListener(onEndEdit);
            else
                inputFieldStorage.inputField.onEndEdit.ClearAll();

            TriggerHelper.IncreaseDecreaseButtonsInt(inputFieldStorage);
            TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDeltaInt(inputFieldStorage.inputField));
        }

        void RenderVector2(InputFieldStorage xField, InputFieldStorage yField, Vector2 value, Action<string> onXValueChanged, Action<string> onYValueChanged, Action<string> onXEndEdit = null, Action<string> onYEndEdit = null)
        {
            xField.inputField.SetTextWithoutNotify(value.x.ToString());
            xField.inputField.onValueChanged.NewListener(onXValueChanged);
            if (onXEndEdit != null)
                xField.inputField.onEndEdit.NewListener(onXEndEdit);
            else
                xField.inputField.onEndEdit.ClearAll();

            yField.inputField.SetTextWithoutNotify(value.y.ToString());
            yField.inputField.onValueChanged.NewListener(onYValueChanged);
            if (onYEndEdit != null)
                yField.inputField.onEndEdit.NewListener(onYEndEdit);
            else
                yField.inputField.onEndEdit.ClearAll();

            TriggerHelper.IncreaseDecreaseButtons(xField);
            TriggerHelper.IncreaseDecreaseButtons(yField);
            TriggerHelper.AddEventTriggers(xField.inputField.gameObject,
                TriggerHelper.ScrollDelta(xField.inputField, multi: true),
                TriggerHelper.ScrollDeltaVector2(xField.inputField, yField.inputField));
            TriggerHelper.AddEventTriggers(yField.inputField.gameObject,
                TriggerHelper.ScrollDelta(yField.inputField, multi: true),
                TriggerHelper.ScrollDeltaVector2(xField.inputField, Dialog.GlobalTab.LimitMoveSpeed.YField.inputField));

        }

        void RenderEasing(Dropdown dropdown, int value, Action<int> onValueChanged)
        {
            RTEditor.inst.SetupEaseDropdown(dropdown);
            RenderDropdown(dropdown, RTEditor.inst.GetEaseIndex((Easing)value),
                onValueChanged: _val => onValueChanged?.Invoke((int)RTEditor.inst.GetEasing(_val)));
        }

        void RenderDropdown(Dropdown dropdown, int value, Action<int> onValueChanged)
        {
            dropdown.SetValueWithoutNotify(value);
            dropdown.onValueChanged.NewListener(onValueChanged);
            TriggerHelper.AddEventTriggers(dropdown.gameObject, TriggerHelper.ScrollDelta(dropdown));
        }

        void RenderColors(List<Button> colorButtons, int value, Action<int> onValueChanged)
        {
            for (int i = 0; i < colorButtons.Count; i++)
            {
                var colorIndex = i;
                var colorButton = colorButtons[i];
                colorButton.transform.GetChild(0).gameObject.SetActive(value == i);
                colorButton.GetComponent<Image>().color = RTColors.GetPlayerColor(playerIndex, i, 1f, "FFFFFF");
                colorButton.onClick.NewListener(() =>
                {
                    RenderColors(colorButtons, colorIndex, onValueChanged);
                    onValueChanged?.Invoke(colorIndex);
                });
            }
        }

        void RenderObject(PlayerEditorObjectTab tab, IPlayerObject playerObject)
        {
            if (tab.Active && tab.Active.Toggle)
            {
                tab.Active.Toggle.SetIsOnWithoutNotify(playerObject.Active);
                tab.Active.Toggle.onValueChanged.NewListener(_val =>
                {
                    playerObject.Active = _val;
                    PlayerManager.UpdatePlayerModels();
                });
            }

            RenderShape(tab.Shape, playerObject as IShapeable);

            RenderVector2(tab.Position.XField, tab.Position.YField, playerObject.Position,
                onXValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        var y = playerObject.Position.y;
                        playerObject.Position = new Vector2(num, y);
                        PlayerManager.UpdatePlayerModels();
                    }
                },
                onYValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        var x = playerObject.Position.x;
                        playerObject.Position = new Vector2(x, num);
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderVector2(tab.Scale.XField, tab.Scale.YField, playerObject.Scale,
                onXValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        var y = playerObject.Scale.y;
                        playerObject.Scale = new Vector2(num, y);
                        PlayerManager.UpdatePlayerModels();
                    }
                },
                onYValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        var x = playerObject.Scale.x;
                        playerObject.Scale = new Vector2(x, num);
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderSingle(tab.Rotation.Field, playerObject.Rotation,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        playerObject.Rotation = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderColors(tab.Color.ColorButtons, playerObject.Color,
                onValueChanged: _val =>
                {
                    playerObject.Color = _val;
                    PlayerManager.UpdatePlayerModels();
                });

            tab.CustomColor.Field.SetTextWithoutNotify(playerObject.CustomColor);
            tab.CustomColor.Field.onValueChanged.NewListener(_val =>
            {
                playerObject.CustomColor = _val.Length == 6 || _val.Length == 8 ? _val : LSColors.ColorToHex(RTColors.errorColor); ;
                PlayerManager.UpdatePlayerModels();
            });

            RenderSingle(tab.Opacity.Field, playerObject.Opacity,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        playerObject.Opacity = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderSingle(tab.Depth.Field, playerObject.Depth,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        playerObject.Depth = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            if (playerObject.Trail && tab.TrailEmitting)
            {
                tab.TrailEmitting.Toggle.SetIsOnWithoutNotify(playerObject.Trail.emitting);
                tab.TrailEmitting.Toggle.onValueChanged.NewListener(_val =>
                {
                    playerObject.Trail.emitting = _val;
                    PlayerManager.UpdatePlayerModels();
                });

                RenderSingle(tab.TrailTime.Field, playerObject.Trail.time,
                    onValueChanged: _val =>
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            playerObject.Trail.time = num;
                            PlayerManager.UpdatePlayerModels();
                        }
                    });

                RenderSingle(tab.TrailStartWidth.Field, playerObject.Trail.startWidth,
                    onValueChanged: _val =>
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            playerObject.Trail.startWidth = num;
                            PlayerManager.UpdatePlayerModels();
                        }
                    });

                RenderSingle(tab.TrailEndWidth.Field, playerObject.Trail.endWidth,
                    onValueChanged: _val =>
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            playerObject.Trail.endWidth = num;
                            PlayerManager.UpdatePlayerModels();
                        }
                    });

                RenderColors(tab.TrailStartColor.ColorButtons, playerObject.Trail.startColor,
                    onValueChanged: _val =>
                    {
                        playerObject.Trail.startColor = _val;
                        PlayerManager.UpdatePlayerModels();
                    });

                tab.TrailStartCustomColor.Field.SetTextWithoutNotify(playerObject.Trail.startCustomColor);
                tab.TrailStartCustomColor.Field.onValueChanged.NewListener(_val =>
                {
                    playerObject.Trail.startCustomColor = _val.Length == 6 || _val.Length == 8 ? _val : LSColors.ColorToHex(RTColors.errorColor); ;
                    PlayerManager.UpdatePlayerModels();
                });

                RenderSingle(tab.TrailStartOpacity.Field, playerObject.Trail.startOpacity,
                    onValueChanged: _val =>
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            playerObject.Trail.startOpacity = num;
                            PlayerManager.UpdatePlayerModels();
                        }
                    });

                RenderColors(tab.TrailEndColor.ColorButtons, playerObject.Trail.endColor,
                    onValueChanged: _val =>
                    {
                        playerObject.Trail.endColor = _val;
                        PlayerManager.UpdatePlayerModels();
                    });

                tab.TrailEndCustomColor.Field.SetTextWithoutNotify(playerObject.Trail.endCustomColor);
                tab.TrailEndCustomColor.Field.onValueChanged.NewListener(_val =>
                {
                    playerObject.Trail.endCustomColor = _val.Length == 6 || _val.Length == 8 ? _val : LSColors.ColorToHex(RTColors.errorColor); ;
                    PlayerManager.UpdatePlayerModels();
                });

                RenderSingle(tab.TrailEndOpacity.Field, playerObject.Trail.endOpacity,
                    onValueChanged: _val =>
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            playerObject.Trail.endOpacity = num;
                            PlayerManager.UpdatePlayerModels();
                        }
                    });

                RenderVector2(tab.TrailPositionOffset.XField, tab.TrailPositionOffset.YField, playerObject.Trail.positionOffset,
                    onXValueChanged: _val =>
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            playerObject.Trail.positionOffset.x = num;
                            PlayerManager.UpdatePlayerModels();
                        }
                    },
                    onYValueChanged: _val =>
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            playerObject.Trail.positionOffset.y = num;
                            PlayerManager.UpdatePlayerModels();
                        }
                    });
            }

            if (playerObject.Particles && tab.ParticlesEmitting)
            {
                tab.ParticlesEmitting.Toggle.SetIsOnWithoutNotify(playerObject.Particles.emitting);
                tab.ParticlesEmitting.Toggle.onValueChanged.NewListener(_val =>
                {
                    playerObject.Particles.emitting = _val;
                    PlayerManager.UpdatePlayerModels();
                });

                RenderShape(tab.ParticlesShape, playerObject.Particles);

                RenderColors(tab.ParticlesColor.ColorButtons, playerObject.Particles.color,
                    onValueChanged: _val =>
                    {
                        playerObject.Particles.color = _val;
                        PlayerManager.UpdatePlayerModels();
                    });

                tab.ParticlesCustomColor.Field.SetTextWithoutNotify(playerObject.Particles.customColor);
                tab.ParticlesCustomColor.Field.onValueChanged.NewListener(_val =>
                {
                    playerObject.Particles.customColor = _val.Length == 6 || _val.Length == 8 ? _val : LSColors.ColorToHex(RTColors.errorColor); ;
                    PlayerManager.UpdatePlayerModels();
                });

                RenderSingle(tab.ParticlesStartOpacity.Field, playerObject.Particles.startOpacity,
                    onValueChanged: _val =>
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            playerObject.Particles.startOpacity = num;
                            PlayerManager.UpdatePlayerModels();
                        }
                    });

                RenderSingle(tab.ParticlesEndOpacity.Field, playerObject.Particles.endOpacity,
                    onValueChanged: _val =>
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            playerObject.Particles.endOpacity = num;
                            PlayerManager.UpdatePlayerModels();
                        }
                    });

                RenderSingle(tab.ParticlesStartScale.Field, playerObject.Particles.startScale,
                    onValueChanged: _val =>
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            playerObject.Particles.startScale = num;
                            PlayerManager.UpdatePlayerModels();
                        }
                    });

                RenderSingle(tab.ParticlesEndScale.Field, playerObject.Particles.endScale,
                    onValueChanged: _val =>
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            playerObject.Particles.endScale = num;
                            PlayerManager.UpdatePlayerModels();
                        }
                    });

                RenderSingle(tab.ParticlesRotation.Field, playerObject.Particles.rotation,
                    onValueChanged: _val =>
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            playerObject.Particles.rotation = num;
                            PlayerManager.UpdatePlayerModels();
                        }
                    });

                RenderSingle(tab.ParticlesLifetime.Field, playerObject.Particles.lifeTime,
                    onValueChanged: _val =>
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            playerObject.Particles.lifeTime = num;
                            PlayerManager.UpdatePlayerModels();
                        }
                    });

                RenderSingle(tab.ParticlesSpeed.Field, playerObject.Particles.speed,
                    onValueChanged: _val =>
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            playerObject.Particles.speed = num;
                            PlayerManager.UpdatePlayerModels();
                        }
                    });

                RenderSingle(tab.ParticlesAmount.Field, playerObject.Particles.amount,
                    onValueChanged: _val =>
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            playerObject.Particles.amount = num;
                            PlayerManager.UpdatePlayerModels();
                        }
                    });

                RenderVector2(tab.ParticlesForce.XField, tab.ParticlesForce.YField, playerObject.Particles.force,
                    onXValueChanged: _val =>
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            playerObject.Particles.force.x = num;
                            PlayerManager.UpdatePlayerModels();
                        }
                    },
                    onYValueChanged: _val =>
                    {
                        if (float.TryParse(_val, out float num))
                        {
                            playerObject.Particles.force.y = num;
                            PlayerManager.UpdatePlayerModels();
                        }
                    });

                tab.ParticlesTrailEmitting.Toggle.SetIsOnWithoutNotify(playerObject.Particles.trailEmitting);
                tab.ParticlesTrailEmitting.Toggle.onValueChanged.NewListener(_val =>
                {
                    playerObject.Particles.trailEmitting = _val;
                    PlayerManager.UpdatePlayerModels();
                });
            }
        }
        
        void RenderShape(PlayerEditorShape ui, IShapeable shapeable)
        {
            var shape = ui.GameObject.transform.Find("shape");
            var shapeSettings = ui.GameObject.transform.Find("shapesettings");

            shape.AsRT().sizeDelta = new Vector2(400f, 32);
            shapeSettings.AsRT().sizeDelta = new Vector2(400f, 32);

            var shapeGLG = shape.GetComponent<GridLayoutGroup>();
            shapeGLG.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            shapeGLG.constraintCount = 1;
            shapeGLG.spacing = new Vector2(7.6f, 0f);

            LSHelpers.SetActiveChildren(shapeSettings, false);

            int type = 0;
            int option = 0;
            if (shapeable != null)
            {
                type = shapeable.Shape;
                option = shapeable.ShapeOption;
            }

            if (type >= shapeSettings.childCount)
            {
                CoreHelper.Log($"Somehow, the object ended up being at a higher shape than normal.");
                if (shapeable != null)
                {
                    shapeable.Shape = 0;
                    shapeable.ShapeOption = 0;
                }

                PlayerManager.UpdatePlayerModels();
                RenderShape(ui, shapeable);
                return;
            }

            shapeSettings.GetChild(type).gameObject.SetActive(true);

            int num = 0;
            foreach (var toggle in ui.ShapeToggles)
            {
                int index = num;
                toggle.gameObject.SetActive(RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes.Length);
                toggle.SetIsOnWithoutNotify(type == index);
                toggle.onValueChanged.NewListener(_val =>
                {
                    CoreHelper.Log($"Set shape to {index}");
                    if (shapeable != null)
                    {
                        shapeable.Shape = index;
                        shapeable.ShapeOption = 0;
                    }

                    if (shapeable.Polygon && shapeable.ShapeType == ShapeType.Polygon && EditorConfig.Instance.AutoPolygonRadius.Value)
                        shapeable.Polygon.Radius = shapeable.Polygon.GetAutoRadius();

                    PlayerManager.UpdatePlayerModels();
                    RenderShape(ui, shapeable);
                    LayoutRebuilder.ForceRebuildLayoutImmediate(ui.GameObject.transform.parent.AsRT());
                });

                num++;
            }
            
            switch ((ShapeType)type)
            {
                case ShapeType.Text: {
                        if (shapeable is not CustomPlayerObject customObject)
                        {
                            CoreHelper.Log($"Player shape cannot be text.");
                            if (shapeable != null)
                            {
                                shapeable.Shape = 0;
                                shapeable.ShapeOption = 0;
                            }

                            PlayerManager.UpdatePlayerModels();
                            RenderShape(ui, shapeable);

                            break;
                        }

                        ui.GameObject.transform.AsRT().sizeDelta = new Vector2(750f, 114f);
                        shapeSettings.AsRT().anchoredPosition = new Vector2(568f, -54f);
                        shapeSettings.AsRT().sizeDelta = new Vector2(400f, 74f);
                        shapeSettings.GetChild(4).AsRT().sizeDelta = new Vector2(400f, 74f);

                        var textIF = shapeSettings.Find("5").GetComponent<InputField>();
                        textIF.textComponent.alignment = TextAnchor.UpperLeft;
                        textIF.GetPlaceholderText().alignment = TextAnchor.UpperLeft;
                        textIF.lineType = InputField.LineType.MultiLineNewline;
                        textIF.SetTextWithoutNotify(customObject.text);
                        textIF.onValueChanged.NewListener(_val =>
                        {
                            CoreHelper.Log($"Set text to {_val}");
                            customObject.text = _val;

                            PlayerManager.UpdatePlayerModels();
                        });

                        break;
                    }
                case ShapeType.Image: {
                        if (shapeable is not CustomPlayerObject customObject)
                        {
                            CoreHelper.Log($"Player shape cannot be image.");
                            if (shapeable != null)
                            {
                                shapeable.Shape = 0;
                                shapeable.ShapeOption = 0;
                            }

                            PlayerManager.UpdatePlayerModels();
                            RenderShape(ui, shapeable);

                            break;
                        }

                        ui.GameObject.transform.AsRT().sizeDelta = new Vector2(750f, 92f);
                        shapeSettings.AsRT().anchoredPosition = new Vector2(568f, -54f);
                        shapeSettings.AsRT().sizeDelta = new Vector2(351f, 32f);

                        var textIF = shapeSettings.Find("5").GetComponent<InputField>();
                        textIF.onValueChanged.ClearAll();
                        textIF.text = customObject.text;
                        textIF.onValueChanged.AddListener(_val =>
                        {
                            CoreHelper.Log($"Set text to {_val}");
                            customObject.text = _val;

                            PlayerManager.UpdatePlayerModels();
                        });
                        var select = shapeSettings.Find("7/select").GetComponent<Button>();
                        select.onClick.NewListener(() => OpenImageSelector(ui, shapeable));
                        shapeSettings.Find("7/text").GetComponent<Text>().text = string.IsNullOrEmpty(customObject.text) ? "No image selected" : customObject.text;

                        var currentModel = PlayersData.Current.GetPlayerModel(playerIndex);

                        // Stores / Removes Image Data for transfering of Image Objects between levels.
                        var dataText = shapeSettings.Find("7/set/Text").GetComponent<Text>();
                        dataText.text = !currentModel.assets.sprites.Has(x => x.name == customObject.text) ? "Store Data" : "Clear Data";
                        var set = shapeSettings.Find("7/set").GetComponent<Button>();
                        set.onClick.NewListener(() =>
                        {
                            var path = RTFile.CombinePaths(RTFile.BasePath, customObject.text);

                            if (!currentModel.assets.sprites.Has(x => x.name == customObject.text))
                                StoreImage(ui, shapeable, path);
                            else
                            {
                                currentModel.assets.RemoveSprite(customObject.text);
                                if (!RTFile.FileExists(path))
                                    customObject.text = string.Empty;
                            }

                            PlayerManager.UpdatePlayerModels();

                            RenderShape(ui, shapeable);
                        });

                        break;
                    }
                case ShapeType.Polygon: {
                        if (shapeable is PlayerParticles)
                        {
                            CoreHelper.Log($"Player shape cannot be polygon.");
                            shapeable.Shape = 0;
                            shapeable.ShapeOption = 0;

                            PlayerManager.UpdatePlayerModels();
                            RenderShape(ui, shapeable);

                            break;
                        }

                        ui.GameObject.transform.AsRT().sizeDelta = new Vector2(750f, 332f);
                        shapeSettings.AsRT().anchoredPosition = new Vector2(568f, -156f);
                        shapeSettings.AsRT().sizeDelta = new Vector2(351f, 320);

                        var radius = shapeSettings.Find("10/radius").gameObject.GetComponent<InputFieldStorage>();
                        radius.OnValueChanged.ClearAll();
                        radius.SetTextWithoutNotify(shapeable.Polygon.Radius.ToString());
                        radius.SetInteractible(!EditorConfig.Instance.AutoPolygonRadius.Value);
                        if (!EditorConfig.Instance.AutoPolygonRadius.Value)
                        {
                            radius.OnValueChanged.AddListener(_val =>
                            {
                                if (float.TryParse(_val, out float num))
                                {
                                    num = Mathf.Clamp(num, 0.1f, 10f);
                                    shapeable.Polygon.Radius = num;

                                    PlayerManager.UpdatePlayerModels();
                                }
                            });

                            TriggerHelper.IncreaseDecreaseButtons(radius, min: 0.1f, max: 10f);
                            TriggerHelper.AddEventTriggers(radius.inputField.gameObject, TriggerHelper.ScrollDelta(radius.inputField, min: 0.1f, max: 10f));
                        }

                        EditorContextMenu.AddContextMenu(radius.inputField.gameObject,
                            getEditorElements: () =>
                            {
                                var editorElements = new List<EditorElement>()
                                {
                                    ButtonElement.ToggleButton("Auto Assign Radius", () => EditorConfig.Instance.AutoPolygonRadius.Value, () =>
                                    {
                                        EditorConfig.Instance.AutoPolygonRadius.Value = !EditorConfig.Instance.AutoPolygonRadius.Value;
                                        RenderShape(ui, shapeable);
                                    })
                                };
                                if (!EditorConfig.Instance.AutoPolygonRadius.Value)
                                {
                                    editorElements.Add(new ButtonElement("Set to Triangle Radius", () =>
                                    {
                                        shapeable.Polygon.Radius = PolygonShape.TRIANGLE_RADIUS;

                                        PlayerManager.UpdatePlayerModels();
                                    }));
                                    editorElements.Add(new ButtonElement("Set to Square Radius", () =>
                                    {
                                        shapeable.Polygon.Radius = PolygonShape.SQUARE_RADIUS;

                                        PlayerManager.UpdatePlayerModels();
                                    }));
                                    editorElements.Add(new ButtonElement("Set to Normal Radius", () =>
                                    {
                                        shapeable.Polygon.Radius = PolygonShape.NORMAL_RADIUS;

                                        PlayerManager.UpdatePlayerModels();
                                    }));
                                }
                                return editorElements;
                            });

                        var sides = shapeSettings.Find("10/sides").gameObject.GetComponent<InputFieldStorage>();
                        sides.SetTextWithoutNotify(shapeable.Polygon.Sides.ToString());
                        sides.OnValueChanged.NewListener(_val =>
                        {
                            if (int.TryParse(_val, out int num))
                            {
                                num = Mathf.Clamp(num, 3, 32);
                                shapeable.Polygon.Sides = num;
                                if (EditorConfig.Instance.AutoPolygonRadius.Value)
                                {
                                    shapeable.Polygon.Radius = shapeable.Polygon.GetAutoRadius();
                                    RenderShape(ui, shapeable);
                                }

                                PlayerManager.UpdatePlayerModels();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtonsInt(sides, min: 3, max: 32);
                        TriggerHelper.AddEventTriggers(sides.inputField.gameObject, TriggerHelper.ScrollDeltaInt(sides.inputField, min: 3, max: 32));
                        
                        var roundness = shapeSettings.Find("10/roundness").gameObject.GetComponent<InputFieldStorage>();
                        roundness.SetTextWithoutNotify(shapeable.Polygon.Roundness.ToString());
                        roundness.OnValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                num = Mathf.Clamp(num, 0f, 1f);
                                shapeable.Polygon.Roundness = num;

                                PlayerManager.UpdatePlayerModels();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(roundness, max: 1f);
                        TriggerHelper.AddEventTriggers(roundness.inputField.gameObject, TriggerHelper.ScrollDelta(roundness.inputField, max: 1f));

                        var thickness = shapeSettings.Find("10/thickness").gameObject.GetComponent<InputFieldStorage>();
                        thickness.SetTextWithoutNotify(shapeable.Polygon.Thickness.ToString());
                        thickness.OnValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                num = Mathf.Clamp(num, 0f, 1f);
                                shapeable.Polygon.Thickness = num;

                                PlayerManager.UpdatePlayerModels();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thickness, max: 1f);
                        TriggerHelper.AddEventTriggers(thickness.inputField.gameObject, TriggerHelper.ScrollDelta(thickness.inputField, max: 1f));
                        
                        var thicknessOffsetX = shapeSettings.Find("10/thickness offset/x").gameObject.GetComponent<InputFieldStorage>();
                        thicknessOffsetX.SetTextWithoutNotify(shapeable.Polygon.ThicknessOffset.x.ToString());
                        thicknessOffsetX.OnValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                shapeable.Polygon.ThicknessOffset = new Vector2(num, shapeable.Polygon.ThicknessOffset.y);

                                PlayerManager.UpdatePlayerModels();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thicknessOffsetX);
                        TriggerHelper.AddEventTriggers(thicknessOffsetX.inputField.gameObject, TriggerHelper.ScrollDelta(thicknessOffsetX.inputField));
                        
                        var thicknessOffsetY = shapeSettings.Find("10/thickness offset/y").gameObject.GetComponent<InputFieldStorage>();
                        thicknessOffsetY.SetTextWithoutNotify(shapeable.Polygon.ThicknessOffset.y.ToString());
                        thicknessOffsetY.OnValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                shapeable.Polygon.ThicknessOffset = new Vector2(shapeable.Polygon.ThicknessOffset.x, num);

                                PlayerManager.UpdatePlayerModels();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thicknessOffsetY);
                        TriggerHelper.AddEventTriggers(thicknessOffsetY.inputField.gameObject, TriggerHelper.ScrollDelta(thicknessOffsetY.inputField));
                        
                        var thicknessScaleX = shapeSettings.Find("10/thickness scale/x").gameObject.GetComponent<InputFieldStorage>();
                        thicknessScaleX.SetTextWithoutNotify(shapeable.Polygon.ThicknessScale.x.ToString());
                        thicknessScaleX.OnValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                shapeable.Polygon.ThicknessScale = new Vector2(num, shapeable.Polygon.ThicknessScale.y);

                                PlayerManager.UpdatePlayerModels();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thicknessScaleX);
                        TriggerHelper.AddEventTriggers(thicknessScaleX.inputField.gameObject, TriggerHelper.ScrollDelta(thicknessScaleX.inputField));
                        
                        var thicknessScaleY = shapeSettings.Find("10/thickness scale/y").gameObject.GetComponent<InputFieldStorage>();
                        thicknessScaleY.SetTextWithoutNotify(shapeable.Polygon.ThicknessScale.y.ToString());
                        thicknessScaleY.OnValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                shapeable.Polygon.ThicknessScale = new Vector2(shapeable.Polygon.ThicknessScale.x, num);

                                PlayerManager.UpdatePlayerModels();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thicknessScaleY);
                        TriggerHelper.AddEventTriggers(thicknessScaleY.inputField.gameObject, TriggerHelper.ScrollDelta(thicknessScaleY.inputField));

                        var thicknessRotation = shapeSettings.Find("10/thickness angle").gameObject.GetComponent<InputFieldStorage>();
                        thicknessRotation.SetTextWithoutNotify(shapeable.Polygon.ThicknessRotation.ToString());
                        thicknessRotation.OnValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                shapeable.Polygon.ThicknessRotation = num;

                                PlayerManager.UpdatePlayerModels();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thicknessRotation, 15f, 3f);
                        TriggerHelper.AddEventTriggers(thicknessRotation.inputField.gameObject, TriggerHelper.ScrollDelta(thicknessRotation.inputField, 15f, 3f));

                        var slices = shapeSettings.Find("10/slices").gameObject.GetComponent<InputFieldStorage>();
                        slices.SetTextWithoutNotify(shapeable.Polygon.Slices.ToString());
                        slices.OnValueChanged.NewListener(_val =>
                        {
                            if (int.TryParse(_val, out int num))
                            {
                                num = Mathf.Clamp(num, 1, 32);
                                shapeable.Polygon.Slices = num;

                                PlayerManager.UpdatePlayerModels();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtonsInt(slices, min: 1, max: 32);
                        TriggerHelper.AddEventTriggers(slices.inputField.gameObject, TriggerHelper.ScrollDeltaInt(slices.inputField, min: 1, max: 32));

                        var rotation = shapeSettings.Find("10/rotation").gameObject.GetComponent<InputFieldStorage>();
                        rotation.SetTextWithoutNotify(shapeable.Polygon.Angle.ToString());
                        rotation.OnValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                shapeable.Polygon.Angle = num;

                                PlayerManager.UpdatePlayerModels();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(rotation, 15f, 3f);
                        TriggerHelper.AddEventTriggers(rotation.inputField.gameObject, TriggerHelper.ScrollDelta(rotation.inputField, 15f, 3f));

                        break;
                    }
                default: {
                        ui.GameObject.transform.AsRT().sizeDelta = new Vector2(750f, 92f);
                        shapeSettings.AsRT().anchoredPosition = new Vector2(568f, -54f);
                        shapeSettings.AsRT().sizeDelta = new Vector2(351f, 32f);

                        num = 0;
                        foreach (var toggle in ui.ShapeOptionToggles[type])
                        {
                            int index = num;
                            toggle.onValueChanged.ClearAll();
                            toggle.isOn = option == index;
                            toggle.gameObject.SetActive(RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes[type]);

                            if (RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes[type])
                                toggle.onValueChanged.AddListener(_val =>
                                {
                                    if (_val)
                                    {
                                        CoreHelper.Log($"Set shape option to {index}");
                                        if (shapeable != null)
                                        {
                                            shapeable.Shape = type;
                                            shapeable.ShapeOption = index;
                                        }

                                        PlayerManager.UpdatePlayerModels();
                                        RenderShape(ui, shapeable);
                                    }
                                });

                            num++;
                        }

                        break;
                    }
            }
        }

        /// <summary>
        /// Renders the global tab.
        /// </summary>
        public void RenderGlobalTab()
        {
            Dialog.GlobalTab.RespawnPlayers.Button.onClick.NewListener(PlayerManager.RespawnPlayers);
            Dialog.GlobalTab.UpdateProperties.Button.onClick.NewListener(RTPlayer.SetGameDataProperties);

            RenderSingle(Dialog.GlobalTab.Speed.Field, GameData.Current.data.level.speedMultiplier,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float result))
                    {
                        GameData.Current.data.level.speedMultiplier = result;
                        RTPlayer.SetGameDataProperties();
                    }
                });

            Dialog.GlobalTab.LockBoost.Toggle.SetIsOnWithoutNotify(GameData.Current.data.level.lockBoost);
            Dialog.GlobalTab.LockBoost.Toggle.onValueChanged.NewListener(_val =>
            {
                GameData.Current.data.level.lockBoost = _val;
                RTPlayer.SetGameDataProperties();
            });

            Dialog.GlobalTab.GameMode.Dropdown.SetValueWithoutNotify((int)GameData.Current.data.level.gameMode);
            Dialog.GlobalTab.GameMode.Dropdown.onValueChanged.NewListener(_val =>
            {
                GameData.Current.data.level.gameMode = (GameMode)_val;
                RTPlayer.SetGameDataProperties();
            });
            TriggerHelper.AddEventTriggers(Dialog.GlobalTab.GameMode.Dropdown.gameObject, TriggerHelper.ScrollDelta(Dialog.GlobalTab.GameMode.Dropdown));

            Dialog.GlobalTab.AllowJumpingToggle.Toggle.SetIsOnWithoutNotify(GameData.Current.data.level.allowJumping);
            Dialog.GlobalTab.AllowJumpingToggle.Toggle.onValueChanged.NewListener(_val =>
            {
                GameData.Current.data.level.allowJumping = _val;
                RTPlayer.SetGameDataProperties();
            });
            
            Dialog.GlobalTab.AllowReversedJumpingToggle.Toggle.SetIsOnWithoutNotify(GameData.Current.data.level.allowReversedJumping);
            Dialog.GlobalTab.AllowReversedJumpingToggle.Toggle.onValueChanged.NewListener(_val =>
            {
                GameData.Current.data.level.allowReversedJumping = _val;
                RTPlayer.SetGameDataProperties();
            });
            
            Dialog.GlobalTab.AllowWallJumpingToggle.Toggle.SetIsOnWithoutNotify(GameData.Current.data.level.allowWallJumping);
            Dialog.GlobalTab.AllowWallJumpingToggle.Toggle.onValueChanged.NewListener(_val =>
            {
                GameData.Current.data.level.allowWallJumping = _val;
                RTPlayer.SetGameDataProperties();
            });
            
            Dialog.GlobalTab.AllowWallStickingToggle.Toggle.SetIsOnWithoutNotify(GameData.Current.data.level.allowWallSticking);
            Dialog.GlobalTab.AllowWallStickingToggle.Toggle.onValueChanged.NewListener(_val =>
            {
                GameData.Current.data.level.allowWallSticking = _val;
                RTPlayer.SetGameDataProperties();
            });

            RenderInteger(Dialog.GlobalTab.MaxJumpCount.Field, GameData.Current.data.level.maxJumpCount,
                onValueChanged: _val =>
                {
                    if (int.TryParse(_val, out int result))
                    {
                        GameData.Current.data.level.maxJumpCount = result;
                        RTPlayer.SetGameDataProperties();
                    }
                });

            RenderInteger(Dialog.GlobalTab.MaxJumpBoostCount.Field, GameData.Current.data.level.maxJumpBoostCount,
                onValueChanged: _val =>
                {
                    if (int.TryParse(_val, out int result))
                    {
                        GameData.Current.data.level.maxJumpBoostCount = result;
                        RTPlayer.SetGameDataProperties();
                    }
                });

            RenderSingle(Dialog.GlobalTab.JumpGravity.Field, GameData.Current.data.level.jumpGravity,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float result))
                    {
                        GameData.Current.data.level.jumpGravity = result;
                        RTPlayer.SetGameDataProperties();
                    }
                });

            RenderSingle(Dialog.GlobalTab.JumpIntensity.Field, GameData.Current.data.level.jumpIntensity,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float result))
                    {
                        GameData.Current.data.level.jumpIntensity = result;
                        RTPlayer.SetGameDataProperties();
                    }
                });

            RenderInteger(Dialog.GlobalTab.MaxHealth.Field, GameData.Current.data.level.maxHealth,
                onValueChanged: _val =>
                {
                    if (int.TryParse(_val, out int result))
                    {
                        GameData.Current.data.level.maxHealth = result;
                        RTPlayer.SetGameDataProperties();
                    }
                });

            Dialog.GlobalTab.SpawnPlayers.Toggle.SetIsOnWithoutNotify(GameData.Current.data.level.spawnPlayers);
            Dialog.GlobalTab.SpawnPlayers.Toggle.onValueChanged.NewListener(_val => GameData.Current.data.level.spawnPlayers = _val);
            
            Dialog.GlobalTab.RespawnImmediately.Toggle.SetIsOnWithoutNotify(GameData.Current.data.level.respawnImmediately);
            Dialog.GlobalTab.RespawnImmediately.Toggle.onValueChanged.NewListener(_val => GameData.Current.data.level.respawnImmediately = _val);

            Dialog.GlobalTab.AllowCustomPlayerModels.Toggle.SetIsOnWithoutNotify(GameData.Current.data.level.allowCustomPlayerModels);
            Dialog.GlobalTab.AllowCustomPlayerModels.Toggle.onValueChanged.NewListener(_val =>
            {
                GameData.Current.data.level.allowCustomPlayerModels = _val;
                RTPlayer.SetGameDataProperties();
            });
            
            Dialog.GlobalTab.AllowPlayerModelControls.Toggle.SetIsOnWithoutNotify(GameData.Current.data.level.allowPlayerModelControls);
            Dialog.GlobalTab.AllowPlayerModelControls.Toggle.onValueChanged.NewListener(_val =>
            {
                GameData.Current.data.level.allowPlayerModelControls = _val;
                RTPlayer.SetGameDataProperties();
            });

            Dialog.GlobalTab.LimitPlayer.Toggle.SetIsOnWithoutNotify(GameData.Current.data.level.limitPlayer);
            Dialog.GlobalTab.LimitPlayer.Toggle.onValueChanged.NewListener(_val =>
            {
                GameData.Current.data.level.limitPlayer = _val;
                RTPlayer.SetGameDataProperties();
            });

            RenderVector2(Dialog.GlobalTab.LimitMoveSpeed.XField, Dialog.GlobalTab.LimitMoveSpeed.YField, GameData.Current.data.level.limitMoveSpeed,
                onXValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        GameData.Current.data.level.limitMoveSpeed.x = num;
                        RTPlayer.SetGameDataProperties();
                    }
                },
                onYValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        GameData.Current.data.level.limitMoveSpeed.y = num;
                        RTPlayer.SetGameDataProperties();
                    }
                });

            RenderVector2(Dialog.GlobalTab.LimitBoostSpeed.XField, Dialog.GlobalTab.LimitBoostSpeed.YField, GameData.Current.data.level.limitBoostSpeed,
                onXValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        GameData.Current.data.level.limitBoostSpeed.x = num;
                        RTPlayer.SetGameDataProperties();
                    }
                },
                onYValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        GameData.Current.data.level.limitBoostSpeed.y = num;
                        RTPlayer.SetGameDataProperties();
                    }
                });

            RenderVector2(Dialog.GlobalTab.LimitBoostCooldown.XField, Dialog.GlobalTab.LimitBoostCooldown.YField, GameData.Current.data.level.limitBoostCooldown,
                onXValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        GameData.Current.data.level.limitBoostCooldown.x = num;
                        RTPlayer.SetGameDataProperties();
                    }
                },
                onYValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        GameData.Current.data.level.limitBoostCooldown.y = num;
                        RTPlayer.SetGameDataProperties();
                    }
                });

            RenderVector2(Dialog.GlobalTab.LimitBoostMinTime.XField, Dialog.GlobalTab.LimitBoostMinTime.YField, GameData.Current.data.level.limitBoostMinTime,
                onXValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        GameData.Current.data.level.limitBoostMinTime.x = num;
                        RTPlayer.SetGameDataProperties();
                    }
                },
                onYValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        GameData.Current.data.level.limitBoostMinTime.y = num;
                        RTPlayer.SetGameDataProperties();
                    }
                });

            RenderVector2(Dialog.GlobalTab.LimitBoostMaxTime.XField, Dialog.GlobalTab.LimitBoostMaxTime.YField, GameData.Current.data.level.limitBoostMaxTime,
                onXValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        GameData.Current.data.level.limitBoostMaxTime.x = num;
                        RTPlayer.SetGameDataProperties();
                    }
                },
                onYValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        GameData.Current.data.level.limitBoostMaxTime.y = num;
                        RTPlayer.SetGameDataProperties();
                    }
                });

            RenderVector2(Dialog.GlobalTab.LimitHitCooldown.XField, Dialog.GlobalTab.LimitHitCooldown.YField, GameData.Current.data.level.limitHitCooldown,
                onXValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        GameData.Current.data.level.limitHitCooldown.x = num;
                        RTPlayer.SetGameDataProperties();
                    }
                },
                onYValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        GameData.Current.data.level.limitHitCooldown.y = num;
                        RTPlayer.SetGameDataProperties();
                    }
                });
        }

        /// <summary>
        /// Renders the base tab.
        /// </summary>
        public void RenderBaseTab(PlayerModel currentModel)
        {
            var control = PlayersData.Current.playerControls.GetAt(playerIndex);

            var text = Dialog.BaseTab.ID.GameObject.transform.GetChild(0).GetComponent<Text>();
            RectValues.Default.AnchoredPosition(-32f, 0f).SizeDelta(750f, 32f).AssignToRectTransform(text.rectTransform);
            text.alignment = TextAnchor.MiddleRight;
            text.text = currentModel.basePart.id.ToString() + " (Click to copy)";
            Dialog.BaseTab.ID.Button.onClick.NewListener(() =>
            {
                LSText.CopyToClipboard(currentModel.basePart.id.ToString());
                EditorManager.inst.DisplayNotification($"Copied ID \"{currentModel.basePart.id}\" to clipboard!", 2f, EditorManager.NotificationType.Success);
            });

            Dialog.BaseTab.Name.Field.SetTextWithoutNotify(currentModel.basePart.name);
            Dialog.BaseTab.Name.Field.onValueChanged.NewListener(_val =>
            {
                currentModel.basePart.name = _val;
                PlayerManager.UpdatePlayerModels();
            });
            
            Dialog.BaseTab.Creator.Field.SetTextWithoutNotify(currentModel.creator);
            Dialog.BaseTab.Creator.Field.onValueChanged.NewListener(_val =>
            {
                currentModel.creator = _val;
                PlayerManager.UpdatePlayerModels();
            });
            
            Dialog.BaseTab.Version.Field.SetTextWithoutNotify(currentModel.ObjectVersion);
            Dialog.BaseTab.Version.Field.onValueChanged.NewListener(_val =>
            {
                currentModel.ObjectVersion = _val;
                PlayerManager.UpdatePlayerModels();
            });
            Dialog.BaseTab.Version.Field.onEndEdit.NewListener(_val => RenderBaseTab(currentModel));
            EditorContextMenu.AddContextMenu(Dialog.BaseTab.Version.Field.gameObject, EditorContextMenu.GetObjectVersionFunctions(currentModel, () => RenderBaseTab(currentModel)));

            Dialog.BaseTab.EditControls.Toggle.SetIsOnWithoutNotify(editControls);
            Dialog.BaseTab.EditControls.Toggle.onValueChanged.NewListener(_val =>
            {
                editControls = _val;
                Dialog.BaseTab.Health.ShowInDefault = _val;
                Dialog.BaseTab.Lives.ShowInDefault = _val;
                Dialog.BaseTab.MoveSpeed.ShowInDefault = _val;
                Dialog.BaseTab.BoostSpeed.ShowInDefault = _val;
                Dialog.BaseTab.BoostCooldown.ShowInDefault = _val;
                Dialog.BaseTab.MinBoostTime.ShowInDefault = _val;
                Dialog.BaseTab.MaxBoostTime.ShowInDefault = _val;
                Dialog.BaseTab.HitCooldown.ShowInDefault = _val;
                Dialog.BaseTab.CollisionAccurate.ShowInDefault = _val;
                Dialog.BaseTab.SprintSneakActive.ShowInDefault = _val;
                Dialog.BaseTab.SneakSpeed.ShowInDefault = _val;
                Dialog.BaseTab.CanBoost.ShowInDefault = _val;
                Dialog.BaseTab.JumpGravity.ShowInDefault = _val;
                Dialog.BaseTab.JumpIntensity.ShowInDefault = _val;
                Dialog.BaseTab.JumpCount.ShowInDefault = _val;
                Dialog.BaseTab.JumpBoostCount.ShowInDefault = _val;
                Dialog.BaseTab.Bounciness.ShowInDefault = _val;
                RenderDialog();
            });

            RenderInteger(Dialog.BaseTab.Health.Field, editControls ? control.Health : currentModel.basePart.health,
                onValueChanged: _val =>
                {
                    if (int.TryParse(_val, out int num))
                    {
                        if (editControls)
                            control.Health = num;
                        else
                            currentModel.basePart.health = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });
            
            RenderInteger(Dialog.BaseTab.Lives.Field, editControls ? control.lives : currentModel.basePart.lives,
                onValueChanged: _val =>
                {
                    if (int.TryParse(_val, out int num))
                    {
                        if (editControls)
                            control.lives = num;
                        else
                            currentModel.basePart.lives = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderSingle(Dialog.BaseTab.MoveSpeed.Field, editControls ? control.moveSpeed : currentModel.basePart.moveSpeed,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        if (editControls)
                            control.moveSpeed = num;
                        else
                            currentModel.basePart.moveSpeed = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderSingle(Dialog.BaseTab.BoostSpeed.Field, editControls ? control.boostSpeed : currentModel.basePart.boostSpeed,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        if (editControls)
                            control.boostSpeed = num;
                        else
                            currentModel.basePart.boostSpeed = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderSingle(Dialog.BaseTab.BoostCooldown.Field, editControls ? control.boostCooldown : currentModel.basePart.boostCooldown,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        if (editControls)
                            control.boostCooldown = num;
                        else
                            currentModel.basePart.boostCooldown = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderSingle(Dialog.BaseTab.MinBoostTime.Field, editControls ? control.minBoostTime : currentModel.basePart.minBoostTime,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        if (editControls)
                            control.minBoostTime = num;
                        else
                            currentModel.basePart.minBoostTime = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderSingle(Dialog.BaseTab.MaxBoostTime.Field, editControls ? control.maxBoostTime : currentModel.basePart.maxBoostTime,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        if (editControls)
                            control.maxBoostTime = num;
                        else
                            currentModel.basePart.maxBoostTime = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderSingle(Dialog.BaseTab.HitCooldown.Field, editControls ? control.hitCooldown : currentModel.basePart.hitCooldown,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        if (editControls)
                            control.hitCooldown = num;
                        else
                            currentModel.basePart.hitCooldown = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderDropdown(Dialog.BaseTab.RotateMode.Dropdown, (int)currentModel.basePart.rotateMode,
                onValueChanged: _val =>
                {
                    currentModel.basePart.rotateMode = (PlayerModel.Base.BaseRotateMode)_val;
                    PlayerManager.UpdatePlayerModels();
                });

            RenderEasing(Dialog.BaseTab.RotationCurve.Dropdown, (int)currentModel.basePart.rotationCurveType,
                onValueChanged: _val =>
                {
                    currentModel.basePart.rotationCurveType = (Easing)_val;
                    PlayerManager.UpdatePlayerModels();
                });

            RenderSingle(Dialog.BaseTab.RotationSpeed.Field, currentModel.basePart.rotationSpeed,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.basePart.rotationSpeed = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            Dialog.BaseTab.CollisionAccurate.Toggle.SetIsOnWithoutNotify(editControls ? control.collisionAccurate : currentModel.basePart.collisionAccurate);
            Dialog.BaseTab.CollisionAccurate.Toggle.onValueChanged.NewListener(_val =>
            {
                if (editControls)
                    control.collisionAccurate = _val;
                else
                    currentModel.basePart.collisionAccurate = _val;
                PlayerManager.UpdatePlayerModels();
            });

            Dialog.BaseTab.SprintSneakActive.Toggle.SetIsOnWithoutNotify(editControls ? control.sprintSneakActive : currentModel.basePart.sprintSneakActive);
            Dialog.BaseTab.SprintSneakActive.Toggle.onValueChanged.NewListener(_val =>
            {
                if (editControls)
                    control.sprintSneakActive = _val;
                else
                    currentModel.basePart.sprintSneakActive = _val;
                PlayerManager.UpdatePlayerModels();
            });

            RenderSingle(Dialog.BaseTab.SprintSpeed.Field, editControls ? control.sprintSpeed : currentModel.basePart.sprintSpeed,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        if (editControls)
                            control.sprintSpeed = num;
                        else
                            currentModel.basePart.sprintSpeed = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });
            
            RenderSingle(Dialog.BaseTab.SneakSpeed.Field, editControls ? control.sprintSpeed : currentModel.basePart.sneakSpeed,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        if (editControls)
                            control.sneakSpeed = num;
                        else
                            currentModel.basePart.sneakSpeed = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            Dialog.BaseTab.CanBoost.Toggle.SetIsOnWithoutNotify(editControls ? control.canBoost : currentModel.basePart.canBoost);
            Dialog.BaseTab.CanBoost.Toggle.onValueChanged.NewListener(_val =>
            {
                if (editControls)
                    control.canBoost = _val;
                else
                    currentModel.basePart.canBoost = _val;
                PlayerManager.UpdatePlayerModels();
            });

            RenderSingle(Dialog.BaseTab.JumpGravity.Field, editControls ? control.jumpGravity : currentModel.basePart.jumpGravity,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        if (editControls)
                            control.jumpGravity = num;
                        else
                            currentModel.basePart.jumpGravity = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderSingle(Dialog.BaseTab.JumpIntensity.Field, editControls ? control.jumpIntensity : currentModel.basePart.jumpIntensity,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        if (editControls)
                            control.jumpIntensity = num;
                        else
                            currentModel.basePart.jumpIntensity = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderInteger(Dialog.BaseTab.JumpCount.Field, editControls ? control.jumpCount : currentModel.basePart.jumpCount,
                onValueChanged: _val =>
                {
                    if (int.TryParse(_val, out int num))
                    {
                        if (editControls)
                            control.jumpCount = num;
                        else
                            currentModel.basePart.jumpCount = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });
            
            RenderInteger(Dialog.BaseTab.JumpBoostCount.Field, editControls ? control.jumpBoostCount : currentModel.basePart.jumpBoostCount,
                onValueChanged: _val =>
                {
                    if (int.TryParse(_val, out int num))
                    {
                        if (editControls)
                            control.jumpBoostCount = num;
                        else
                            currentModel.basePart.jumpBoostCount = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderSingle(Dialog.BaseTab.Bounciness.Field, editControls ? control.bounciness : currentModel.basePart.bounciness,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        if (editControls)
                            control.bounciness = num;
                        else
                            currentModel.basePart.bounciness = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            Dialog.BaseTab.StretchActive.Toggle.SetIsOnWithoutNotify(currentModel.basePart.stretchActive);
            Dialog.BaseTab.StretchActive.Toggle.onValueChanged.NewListener(_val =>
            {
                currentModel.basePart.stretchActive = _val;
                PlayerManager.UpdatePlayerModels();
            });

            RenderSingle(Dialog.BaseTab.StretchAmount.Field, currentModel.basePart.stretchAmount,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.basePart.stretchAmount = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderEasing(Dialog.BaseTab.StretchEasing.Dropdown, currentModel.basePart.stretchEasing,
                onValueChanged: _val =>
                {
                    currentModel.basePart.stretchEasing = _val;
                    PlayerManager.UpdatePlayerModels();
                });

            RenderVector2(Dialog.BaseTab.FacePosition.XField, Dialog.BaseTab.FacePosition.YField, currentModel.facePosition,
                onXValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.facePosition.x = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                },
                onYValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.facePosition.y = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            Dialog.BaseTab.FaceControlActive.Toggle.SetIsOnWithoutNotify(currentModel.faceControlActive);
            Dialog.BaseTab.FaceControlActive.Toggle.onValueChanged.NewListener(_val =>
            {
                currentModel.faceControlActive = _val;
                PlayerManager.UpdatePlayerModels();
            });

            try
            {
                CoroutineHelper.StartCoroutine(Dialog.BaseTab.TickModifiers.Modifiers.RenderModifiers(PlayersData.Current.playerControls.GetAt(playerIndex).TickModifierBlock));
                CoroutineHelper.StartCoroutine(Dialog.BaseTab.ModelModifiers.Modifiers.RenderModifiers(currentModel));
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Error: {ex}");
            }
        }

        /// <summary>
        /// Renders the gui tab.
        /// </summary>
        public void RenderGUITab(PlayerModel currentModel)
        {
            Dialog.GUITab.HealthActive.Toggle.SetIsOnWithoutNotify(currentModel.guiPart.active);
            Dialog.GUITab.HealthActive.Toggle.onValueChanged.NewListener(_val =>
            {
                currentModel.guiPart.active = _val;
                PlayerManager.UpdatePlayerModels();
            });

            RenderDropdown(Dialog.GUITab.HealthMode.Dropdown, (int)currentModel.guiPart.mode,
                onValueChanged: _val =>
                {
                    currentModel.guiPart.mode = (PlayerModel.GUI.GUIHealthMode)_val;
                    PlayerManager.UpdatePlayerModels();
                });

            RenderColors(Dialog.GUITab.HealthTopColor.ColorButtons, currentModel.guiPart.topColor,
                onValueChanged: _val =>
                {
                    currentModel.guiPart.topColor = _val;
                    PlayerManager.UpdatePlayerModels();
                });

            Dialog.GUITab.HealthTopCustomColor.Field.SetTextWithoutNotify(currentModel.guiPart.topCustomColor);
            Dialog.GUITab.HealthTopCustomColor.Field.onValueChanged.NewListener(_val =>
            {
                currentModel.guiPart.topCustomColor = _val.Length == 6 || _val.Length == 8 ? _val : RTColors.ColorToHex(RTColors.errorColor); ;
                PlayerManager.UpdatePlayerModels();
            });

            RenderSingle(Dialog.GUITab.HealthTopOpacity.Field, currentModel.guiPart.topOpacity,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.guiPart.topOpacity = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderColors(Dialog.GUITab.HealthBaseColor.ColorButtons, currentModel.guiPart.baseColor,
                onValueChanged: _val =>
                {
                    currentModel.guiPart.baseColor = _val;
                    PlayerManager.UpdatePlayerModels();
                });

            Dialog.GUITab.HealthBaseCustomColor.Field.SetTextWithoutNotify(currentModel.guiPart.baseCustomColor);
            Dialog.GUITab.HealthBaseCustomColor.Field.onValueChanged.NewListener(_val =>
            {
                currentModel.guiPart.baseCustomColor = _val.Length == 6 || _val.Length == 8 ? _val : RTColors.ColorToHex(RTColors.errorColor); ;
                PlayerManager.UpdatePlayerModels();
            });

            RenderSingle(Dialog.GUITab.HealthBaseOpacity.Field, currentModel.guiPart.baseOpacity,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.guiPart.baseOpacity = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });
        }

        /// <summary>
        /// Renders the head tab.
        /// </summary>
        public void RenderHeadTab(PlayerModel currentModel) => RenderObject(Dialog.HeadTab, currentModel.headPart);

        /// <summary>
        /// Renders the boost tab.
        /// </summary>
        public void RenderBoostTab(PlayerModel currentModel) => RenderObject(Dialog.BoostTab, currentModel.boostPart);

        /// <summary>
        /// Renders the spawner tab.
        /// </summary>
        public void RenderSpawnersTab(PlayerModel currentModel)
        {
            #region Pulse

            Dialog.SpawnerTab.PulseActive.Toggle.SetIsOnWithoutNotify(currentModel.pulsePart.active);
            Dialog.SpawnerTab.PulseActive.Toggle.onValueChanged.NewListener(_val =>
            {
                currentModel.pulsePart.active = _val;
                PlayerManager.UpdatePlayerModels();
            });
            
            Dialog.SpawnerTab.PulseRotateToHead.Toggle.SetIsOnWithoutNotify(currentModel.pulsePart.rotateToHead);
            Dialog.SpawnerTab.PulseRotateToHead.Toggle.onValueChanged.NewListener(_val =>
            {
                currentModel.pulsePart.rotateToHead = _val;
                PlayerManager.UpdatePlayerModels();
            });

            RenderShape(Dialog.SpawnerTab.PulseShape, currentModel.pulsePart);

            RenderSingle(Dialog.SpawnerTab.PulseDuration.Field, currentModel.pulsePart.duration,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.pulsePart.duration = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderColors(Dialog.SpawnerTab.PulseStartColor.ColorButtons, currentModel.pulsePart.startColor,
                onValueChanged: _val =>
                {
                    currentModel.pulsePart.startColor = _val;
                    PlayerManager.UpdatePlayerModels();
                });

            Dialog.SpawnerTab.PulseStartCustomColor.Field.SetTextWithoutNotify(currentModel.pulsePart.startCustomColor);
            Dialog.SpawnerTab.PulseStartCustomColor.Field.onValueChanged.NewListener(_val =>
            {
                currentModel.pulsePart.startCustomColor = _val.Length == 6 || _val.Length == 8 ? _val : LSColors.ColorToHex(RTColors.errorColor); ;
                PlayerManager.UpdatePlayerModels();
            });

            RenderSingle(Dialog.SpawnerTab.PulseStartOpacity.Field, currentModel.pulsePart.startOpacity,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.pulsePart.startOpacity = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderColors(Dialog.SpawnerTab.PulseEndColor.ColorButtons, currentModel.pulsePart.endColor,
                onValueChanged: _val =>
                {
                    currentModel.pulsePart.endColor = _val;
                    PlayerManager.UpdatePlayerModels();
                });

            Dialog.SpawnerTab.PulseEndCustomColor.Field.SetTextWithoutNotify(currentModel.pulsePart.endCustomColor);
            Dialog.SpawnerTab.PulseEndCustomColor.Field.onValueChanged.NewListener(_val =>
            {
                currentModel.pulsePart.endCustomColor = _val.Length == 6 || _val.Length == 8 ? _val : LSColors.ColorToHex(RTColors.errorColor); ;
                PlayerManager.UpdatePlayerModels();
            });

            RenderSingle(Dialog.SpawnerTab.PulseEndOpacity.Field, currentModel.pulsePart.endOpacity,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.pulsePart.endOpacity = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderEasing(Dialog.SpawnerTab.PulseColorEasing.Dropdown, currentModel.pulsePart.easingColor,
                onValueChanged: _val =>
                {
                    currentModel.pulsePart.easingColor = _val;
                    PlayerManager.UpdatePlayerModels();
                });

            RenderEasing(Dialog.SpawnerTab.PulseOpacityEasing.Dropdown, currentModel.pulsePart.easingOpacity,
                onValueChanged: _val =>
                {
                    currentModel.pulsePart.easingOpacity = _val;
                    PlayerManager.UpdatePlayerModels();
                });

            RenderSingle(Dialog.SpawnerTab.PulseDepth.Field, currentModel.pulsePart.depth,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.pulsePart.depth = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderVector2(Dialog.SpawnerTab.PulseStartPosition.XField, Dialog.SpawnerTab.PulseStartPosition.YField, currentModel.pulsePart.startPosition,
                onXValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.pulsePart.startPosition.x = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                },
                onYValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.pulsePart.startPosition.y = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderVector2(Dialog.SpawnerTab.PulseEndPosition.XField, Dialog.SpawnerTab.PulseEndPosition.YField, currentModel.pulsePart.endPosition,
                onXValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.pulsePart.endPosition.x = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                },
                onYValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.pulsePart.endPosition.y = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderEasing(Dialog.SpawnerTab.PulsePositionEasing.Dropdown, currentModel.pulsePart.easingPosition,
                onValueChanged: _val =>
                {
                    currentModel.pulsePart.easingPosition = _val;
                    PlayerManager.UpdatePlayerModels();
                });

            RenderVector2(Dialog.SpawnerTab.PulseStartScale.XField, Dialog.SpawnerTab.PulseStartScale.YField, currentModel.pulsePart.startScale,
                onXValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.pulsePart.startScale.x = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                },
                onYValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.pulsePart.startScale.y = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderVector2(Dialog.SpawnerTab.PulseEndScale.XField, Dialog.SpawnerTab.PulseEndScale.YField, currentModel.pulsePart.endScale,
                onXValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.pulsePart.endScale.x = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                },
                onYValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.pulsePart.endScale.y = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderEasing(Dialog.SpawnerTab.PulseScaleEasing.Dropdown, currentModel.pulsePart.easingScale,
                onValueChanged: _val =>
                {
                    currentModel.pulsePart.easingScale = (int)RTEditor.inst.GetEasing(_val);
                    PlayerManager.UpdatePlayerModels();
                });

            RenderSingle(Dialog.SpawnerTab.PulseStartRotation.Field, currentModel.pulsePart.startRotation,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.pulsePart.startRotation = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderSingle(Dialog.SpawnerTab.PulseEndRotation.Field, currentModel.pulsePart.endRotation,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.pulsePart.endRotation = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderEasing(Dialog.SpawnerTab.PulseRotationEasing.Dropdown, currentModel.pulsePart.easingRotation,
                onValueChanged: _val =>
                {
                    currentModel.pulsePart.easingRotation = _val;
                    PlayerManager.UpdatePlayerModels();
                });

            #endregion

            #region Bullet

            Dialog.SpawnerTab.BulletActive.Toggle.SetIsOnWithoutNotify(currentModel.bulletPart.active);
            Dialog.SpawnerTab.BulletActive.Toggle.onValueChanged.NewListener(_val =>
            {
                currentModel.bulletPart.active = _val;
                PlayerManager.UpdatePlayerModels();
            });
            
            Dialog.SpawnerTab.BulletAutoKill.Toggle.SetIsOnWithoutNotify(currentModel.bulletPart.autoKill);
            Dialog.SpawnerTab.BulletAutoKill.Toggle.onValueChanged.NewListener(_val =>
            {
                currentModel.bulletPart.autoKill = _val;
                PlayerManager.UpdatePlayerModels();
            });

            RenderSingle(Dialog.SpawnerTab.BulletLifetime.Field, currentModel.bulletPart.lifeTime,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.bulletPart.lifeTime = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            Dialog.SpawnerTab.BulletConstant.Toggle.SetIsOnWithoutNotify(currentModel.bulletPart.constant);
            Dialog.SpawnerTab.BulletConstant.Toggle.onValueChanged.NewListener(_val =>
            {
                currentModel.bulletPart.constant = _val;
                PlayerManager.UpdatePlayerModels();
            });

            Dialog.SpawnerTab.BulletHurtPlayers.Toggle.SetIsOnWithoutNotify(currentModel.bulletPart.hurtPlayers);
            Dialog.SpawnerTab.BulletHurtPlayers.Toggle.onValueChanged.NewListener(_val =>
            {
                currentModel.bulletPart.hurtPlayers = _val;
                PlayerManager.UpdatePlayerModels();
            });

            RenderSingle(Dialog.SpawnerTab.BulletSpeed.Field, currentModel.bulletPart.speed,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.bulletPart.speed = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderSingle(Dialog.SpawnerTab.BulletCooldown.Field, currentModel.bulletPart.cooldown,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.bulletPart.cooldown = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderVector2(Dialog.SpawnerTab.BulletOrigin.XField, Dialog.SpawnerTab.BulletOrigin.YField, currentModel.bulletPart.origin,
                onXValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.bulletPart.origin.x = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                },
                onYValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.bulletPart.origin.y = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderShape(Dialog.SpawnerTab.BulletShape, currentModel.bulletPart);

            RenderColors(Dialog.SpawnerTab.BulletStartColor.ColorButtons, currentModel.bulletPart.startColor,
                onValueChanged: _val =>
                {
                    currentModel.bulletPart.startColor = _val;
                    PlayerManager.UpdatePlayerModels();
                });

            Dialog.SpawnerTab.BulletStartCustomColor.Field.SetTextWithoutNotify(currentModel.bulletPart.startCustomColor);
            Dialog.SpawnerTab.BulletStartCustomColor.Field.onValueChanged.NewListener(_val =>
            {
                currentModel.bulletPart.startCustomColor = _val.Length == 6 || _val.Length == 8 ? _val : LSColors.ColorToHex(RTColors.errorColor); ;
                PlayerManager.UpdatePlayerModels();
            });

            RenderSingle(Dialog.SpawnerTab.BulletStartOpacity.Field, currentModel.bulletPart.startOpacity,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.bulletPart.startOpacity = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderColors(Dialog.SpawnerTab.BulletEndColor.ColorButtons, currentModel.bulletPart.endColor,
                onValueChanged: _val =>
                {
                    currentModel.bulletPart.endColor = _val;
                    PlayerManager.UpdatePlayerModels();
                });

            Dialog.SpawnerTab.BulletEndCustomColor.Field.SetTextWithoutNotify(currentModel.bulletPart.endCustomColor);
            Dialog.SpawnerTab.BulletEndCustomColor.Field.onValueChanged.NewListener(_val =>
            {
                currentModel.bulletPart.endCustomColor = _val.Length == 6 || _val.Length == 8 ? _val : LSColors.ColorToHex(RTColors.errorColor); ;
                PlayerManager.UpdatePlayerModels();
            });

            RenderSingle(Dialog.SpawnerTab.BulletEndOpacity.Field, currentModel.bulletPart.endOpacity,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.bulletPart.endOpacity = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderSingle(Dialog.SpawnerTab.BulletColorDuration.Field, currentModel.bulletPart.durationColor,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.bulletPart.durationColor = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderEasing(Dialog.SpawnerTab.BulletColorEasing.Dropdown, currentModel.bulletPart.easingColor,
                onValueChanged: _val =>
                {
                    currentModel.bulletPart.easingColor = _val;
                    PlayerManager.UpdatePlayerModels();
                });

            RenderSingle(Dialog.SpawnerTab.BulletOpacityDuration.Field, currentModel.bulletPart.durationOpacity,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.bulletPart.durationOpacity = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderEasing(Dialog.SpawnerTab.BulletOpacityEasing.Dropdown, currentModel.bulletPart.easingOpacity,
                onValueChanged: _val =>
                {
                    currentModel.bulletPart.easingOpacity = _val;
                    PlayerManager.UpdatePlayerModels();
                });

            RenderSingle(Dialog.SpawnerTab.BulletDepth.Field, currentModel.bulletPart.depth,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.bulletPart.depth = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderVector2(Dialog.SpawnerTab.BulletStartPosition.XField, Dialog.SpawnerTab.BulletStartPosition.YField, currentModel.bulletPart.startPosition,
                onXValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.bulletPart.startPosition.x = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                },
                onYValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.bulletPart.startPosition.y = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderVector2(Dialog.SpawnerTab.BulletEndPosition.XField, Dialog.SpawnerTab.BulletEndPosition.YField, currentModel.bulletPart.endPosition,
                onXValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.bulletPart.endPosition.x = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                },
                onYValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.bulletPart.endPosition.y = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderSingle(Dialog.SpawnerTab.BulletPositionDuration.Field, currentModel.bulletPart.durationPosition,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.bulletPart.durationPosition = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderEasing(Dialog.SpawnerTab.BulletPositionEasing.Dropdown, currentModel.bulletPart.easingPosition,
                onValueChanged: _val =>
                {
                    currentModel.bulletPart.easingPosition = _val;
                    PlayerManager.UpdatePlayerModels();
                });

            RenderVector2(Dialog.SpawnerTab.BulletStartScale.XField, Dialog.SpawnerTab.BulletStartScale.YField, currentModel.bulletPart.startScale,
                onXValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.bulletPart.startScale.x = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                },
                onYValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.bulletPart.startScale.y = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderVector2(Dialog.SpawnerTab.BulletEndScale.XField, Dialog.SpawnerTab.BulletEndScale.YField, currentModel.bulletPart.endScale,
                onXValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.bulletPart.endScale.x = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                },
                onYValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.bulletPart.endScale.y = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderSingle(Dialog.SpawnerTab.BulletScaleDuration.Field, currentModel.bulletPart.durationScale,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.bulletPart.durationScale = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderEasing(Dialog.SpawnerTab.BulletScaleEasing.Dropdown, currentModel.bulletPart.easingScale,
                onValueChanged: _val =>
                {
                    currentModel.bulletPart.easingScale = _val;
                    PlayerManager.UpdatePlayerModels();
                });

            RenderSingle(Dialog.SpawnerTab.BulletStartRotation.Field, currentModel.bulletPart.startRotation,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.bulletPart.startRotation = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderSingle(Dialog.SpawnerTab.BulletEndRotation.Field, currentModel.bulletPart.endRotation,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.bulletPart.endRotation = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderSingle(Dialog.SpawnerTab.BulletRotationDuration.Field, currentModel.bulletPart.durationRotation,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.bulletPart.durationRotation = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderEasing(Dialog.SpawnerTab.BulletRotationEasing.Dropdown, currentModel.bulletPart.easingRotation,
                onValueChanged: _val =>
                {
                    currentModel.bulletPart.easingRotation = _val;
                    PlayerManager.UpdatePlayerModels();
                });

            #endregion
        }

        /// <summary>
        /// Renders the tail tab.
        /// </summary>
        public void RenderTailTab(PlayerModel currentModel)
        {
            RenderSingle(Dialog.TailTab.BaseDistance.Field, currentModel.tailBase.distance,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.tailBase.distance = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderDropdown(Dialog.TailTab.BaseMode.Dropdown, (int)currentModel.tailBase.mode,
                onValueChanged: _val =>
                {
                    currentModel.tailBase.mode = (PlayerModel.TailBase.TailMode)_val;
                    PlayerManager.UpdatePlayerModels();
                });

            Dialog.TailTab.BaseGrows.Toggle.SetIsOnWithoutNotify(currentModel.tailBase.grows);
            Dialog.TailTab.BaseGrows.Toggle.onValueChanged.NewListener(_val =>
            {
                currentModel.tailBase.grows = _val;
                PlayerManager.UpdatePlayerModels();
            });

            RenderSingle(Dialog.TailTab.BaseTime.Field, currentModel.tailBase.time,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.tailBase.time = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderObject(Dialog.TailTab.BoostPart, currentModel.boostTailPart);

            CoroutineHelper.StartCoroutine(IRenderTails(currentModel));

            Dialog.TailTab.AddTail = Dialog.SetupButton("Add Tail", Tab.Tail, editorTab: Dialog.TailTab);
            Dialog.TailTab.AddTail.Button.onClick.NewListener(() =>
            {
                currentModel.AddTail();
                PlayerManager.UpdatePlayerModels();
                RenderTailTab(currentModel);
            });

            Dialog.elements.RemoveAll(x => x.Name.Contains("Tail Part "));
        }

        IEnumerator IRenderTails(PlayerModel currentModel)
        {
            for (int i = 0; i < Dialog.TailTab.TailParts.Count; i++)
                Dialog.TailTab.TailParts[i].ClearContent();
            Dialog.TailTab.TailParts.Clear();
            if (Dialog.TailTab.AddTail)
                CoreHelper.Delete(Dialog.TailTab.AddTail.GameObject);

            for (int i = 0; i < currentModel.tailParts.Count; i++)
            {
                var index = i;
                var tab = new PlayerEditorObjectTab();
                Dialog.SetupObjectTab(tab, $"Tail Part {i + 1}", Tab.Tail, doRemove: true);
                tab.Remove.Button.onClick.NewListener(() =>
                {
                    currentModel.RemoveTail(index);
                    PlayerManager.UpdatePlayerModels();
                    RenderTailTab(currentModel);
                });
                Dialog.TailTab.TailParts.Add(tab);
                RenderObject(tab, currentModel.tailParts[i]);
            }

            yield break;
        }

        /// <summary>
        /// Renders the custom tab.
        /// </summary>
        public void RenderCustomTab(PlayerModel currentModel)
        {
            Dialog.CustomObjectTab.SelectCustomObject.Button.onClick.NewListener(OpenCustomObjectsPopup);

            CustomPlayerObject customObject = null;
            var customActive = Dialog.CustomObjectTab.SelectCustomObject.IsActive(currentModel.IsDefault) && !string.IsNullOrEmpty(CustomObjectID) && currentModel.customObjects.TryFind(x => x.id == CustomObjectID, out customObject);
            Dialog.CustomObjectTab.SetActive(customActive);
            Dialog.CustomObjectTab.SelectCustomObject.GameObject.SetActive(true);
            if (!customActive)
                return;

            var text = Dialog.CustomObjectTab.ID.GameObject.transform.GetChild(0).GetComponent<Text>();
            RectValues.Default.AnchoredPosition(-32f, 0f).SizeDelta(750f, 32f).AssignToRectTransform(text.rectTransform);
            text.alignment = TextAnchor.MiddleRight;
            text.text = customObject.id + " (Click to copy)";
            Dialog.CustomObjectTab.ID.Button.onClick.NewListener(() =>
            {
                LSText.CopyToClipboard(customObject.id);
                EditorManager.inst.DisplayNotification($"Copied ID \"{customObject.id}\" to clipboard!", 2f, EditorManager.NotificationType.Success);
            });

            Dialog.CustomObjectTab.Name.Field.SetTextWithoutNotify(customObject.name);
            Dialog.CustomObjectTab.Name.Field.onValueChanged.NewListener(_val => customObject.name = _val);

            RTEditor.inst.RenderTags(customObject, Dialog.CustomObjectTab.Tags);

            Dialog.CustomObjectTab.Parent.Dropdown.SetValueWithoutNotify(customObject.parent);
            Dialog.CustomObjectTab.Parent.Dropdown.onValueChanged.NewListener(_val =>
            {
                customObject.parent = _val;
                PlayerManager.UpdatePlayerModels();
            });

            Dialog.CustomObjectTab.CustomParent.Field.SetTextWithoutNotify(customObject.customParent);
            Dialog.CustomObjectTab.CustomParent.Field.onValueChanged.NewListener(_val =>
            {
                customObject.customParent = _val;
                PlayerManager.UpdatePlayerModels();
            });

            RenderSingle(Dialog.CustomObjectTab.PositionOffset.Field, customObject.positionOffset,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        customObject.positionOffset = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderSingle(Dialog.CustomObjectTab.ScaleOffset.Field, customObject.scaleOffset,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        customObject.scaleOffset = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            Dialog.CustomObjectTab.ScaleParent.Toggle.SetIsOnWithoutNotify(customObject.scaleParent);
            Dialog.CustomObjectTab.ScaleParent.Toggle.onValueChanged.NewListener(_val =>
            {
                customObject.scaleParent = _val;
                PlayerManager.UpdatePlayerModels();
            });

            RenderSingle(Dialog.CustomObjectTab.RotationOffset.Field, customObject.rotationOffset,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        customObject.rotationOffset = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            Dialog.CustomObjectTab.RotationParent.Toggle.SetIsOnWithoutNotify(customObject.rotationParent);
            Dialog.CustomObjectTab.RotationParent.Toggle.onValueChanged.NewListener(_val =>
            {
                customObject.rotationParent = _val;
                PlayerManager.UpdatePlayerModels();
            });

            Dialog.CustomObjectTab.VisibilitySettings.GameObject.transform.AsRT().sizeDelta = new Vector2(750f, 32f * (customObject.visibilitySettings.Count + 1));
            LayoutRebuilder.ForceRebuildLayoutImmediate(Dialog.CustomObjectTab.VisibilitySettings.GameObject.transform.AsRT());
            LayoutRebuilder.ForceRebuildLayoutImmediate(Dialog.CustomObjectTab.VisibilitySettings.GameObject.transform.parent.AsRT());

            var content = Dialog.CustomObjectTab.VisibilitySettings.GameObject.transform.Find("ScrollRect/Mask/Content");
            LSHelpers.DeleteChildren(content);

            var add = EditorPrefabHolder.Instance.CreateAddButton(content);
            add.Text = "Add Visibilty Setting";
            add.OnClick.NewListener(() =>
            {
                var newVisibility = new CustomPlayerObject.Visibility();
                newVisibility.command = IntToVisibility(0);
                customObject.visibilitySettings.Add(newVisibility);
                RenderDialog();
            });

            int num = 0;
            foreach (var visibility in customObject.visibilitySettings)
            {
                int index = num;
                var bar = Creator.NewUIObject($"Visiblity {num}", content);
                bar.transform.AsRT().sizeDelta = new Vector2(500f, 32f);
                bar.AddComponent<HorizontalLayoutGroup>().spacing = 4;
                bar.transform.localScale = Vector3.one;

                var image = bar.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0.03f);

                var toggle = EditorPrefabHolder.Instance.Function2Button.Duplicate(bar.transform, "not").GetComponent<FunctionButtonStorage>();

                toggle.Text = $"Not: {(visibility.not ? "Yes" : "No")}";
                toggle.OnClick.NewListener(() =>
                {
                    visibility.not = !visibility.not;
                    toggle.Text = $"Not: {(visibility.not ? "Yes" : "No")}";
                });
                EditorThemeManager.ApplySelectable(toggle.button, ThemeGroup.Function_2);
                EditorThemeManager.ApplyGraphic(toggle.label, ThemeGroup.Function_2_Text);

                var x = EditorPrefabHolder.Instance.Dropdown.Duplicate(bar.transform);
                x.transform.SetParent(bar.transform);
                x.transform.localScale = Vector3.one;

                Destroy(x.GetComponent<HoverTooltip>());
                Destroy(x.GetComponent<HideDropdownOptions>());
                var layoutElement = x.GetComponent<LayoutElement>();
                layoutElement.minWidth = 200f;
                layoutElement.preferredWidth = 400f;

                var dropdown = x.GetComponent<Dropdown>();
                dropdown.template.sizeDelta = new Vector2(120f, 192f);
                dropdown.options = CoreHelper.StringToOptionData("Is Boosting", "Is Taking Hit", "Is Zen Mode", "Is Health Percentage Greater", "Is Health Greater Equals", "Is Health Equals", "Is Health Greater", "Is Pressing Key");
                dropdown.SetValueWithoutNotify(VisibilityToInt(visibility.command));
                dropdown.onValueChanged.NewListener(_val => visibility.command = IntToVisibility(_val));
                EditorThemeManager.ApplyDropdown(dropdown);

                // Value
                {
                    var value = EditorPrefabHolder.Instance.NumberInputField.Duplicate(bar.transform, "input");
                    var valueStorage = value.GetComponent<InputFieldStorage>();

                    valueStorage.inputField.SetTextWithoutNotify(visibility.value.ToString());
                    valueStorage.inputField.onValueChanged.NewListener(_val =>
                    {
                        if (float.TryParse(_val, out float result))
                            visibility.value = result;
                    });

                    DestroyImmediate(valueStorage.leftGreaterButton.gameObject);
                    DestroyImmediate(valueStorage.middleButton.gameObject);
                    DestroyImmediate(valueStorage.rightGreaterButton.gameObject);

                    TriggerHelper.AddEventTriggers(value, TriggerHelper.ScrollDelta(valueStorage.inputField));
                    TriggerHelper.IncreaseDecreaseButtons(valueStorage);
                    EditorThemeManager.ApplyInputField(valueStorage.inputField);
                    EditorThemeManager.ApplySelectable(valueStorage.leftButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.ApplySelectable(valueStorage.rightButton, ThemeGroup.Function_2, false);
                }

                var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(bar.transform, "delete");

                delete.transform.AsRT().anchoredPosition = new Vector2(-5f, 0f);

                delete.GetComponent<LayoutElement>().ignoreLayout = false;

                var deleteButton = delete.GetComponent<DeleteButtonStorage>();
                deleteButton.OnClick.NewListener(() =>
                {
                    customObject.visibilitySettings.RemoveAt(index);
                    RenderDialog();
                });
                EditorThemeManager.ApplyDeleteButton(deleteButton);

                num++;
            }

            RenderObject(Dialog.CustomObjectTab, customObject);

            ITransformable transformable = null;
            var player = CurrentPlayer;
            if (player && player.RuntimePlayer)
            {
                var id = CustomObjectID;
                if (!string.IsNullOrEmpty(id))
                    transformable = player.RuntimePlayer.customObjects.Find(x => x.id == id);
            }

            Dialog.CustomObjectTab.ViewAnimations.Button.onClick.NewListener(() => AnimationEditor.inst.OpenPopup(customObject.animations, PlayAnimation, currentObject: transformable, onReturn: OpenDialog));

            CoroutineHelper.StartCoroutine(Dialog.CustomObjectTab.Modifiers.Modifiers.RenderModifiers(customObject));
        }

        /// <summary>
        /// Plays an animation on a player.
        /// </summary>
        /// <param name="animation">Animation to play.</param>
        public void PlayAnimation(PAAnimation animation)
        {
            if (!PlayerManager.Players.TryGetAt(playerIndex, out PAPlayer player) || !player.RuntimePlayer || !player.RuntimePlayer.customObjects.TryFind(x => x.id == CustomObjectID, out RTPlayer.RTCustomPlayerObject customObject))
                return;

            var runtimeAnimation = new RTAnimation("Custom Animation");
            runtimeAnimation.SetDefaultOnComplete(player.RuntimePlayer.animationController);
            player.RuntimePlayer.ApplyAnimation(runtimeAnimation, animation, customObject);
            player.RuntimePlayer.animationController.Play(runtimeAnimation);
        }

        /// <summary>
        /// Opens the models popup.
        /// </summary>
        /// <param name="onSelect">Function to run on select.</param>
        public void OpenModelsPopup(Action<PlayerModel> onSelect = null)
        {
            ModelsPopup.Open();
            RenderModelsPopup(onSelect);
        }

        /// <summary>
        /// Renders the models popup.
        /// </summary>
        /// <param name="onSelect">Function to run on select.</param>
        public void RenderModelsPopup(Action<PlayerModel> onSelect = null)
        {
            onSelectModel = onSelect;
            ModelsPopup.ClearContent();
            ModelsPopup.SearchField.onValueChanged.NewListener(_val => RenderModelsPopup(onSelect));

            ModelPanels.Clear();

            int num = 0;
            foreach (var playerModel in PlayersData.externalPlayerModels)
            {
                int index = num;
                var name = playerModel.Value.basePart.name;
                if (!RTString.SearchString(ModelsPopup.SearchTerm, name))
                {
                    num++;
                    continue;
                }

                var playerModelPanel = new PlayerModelPanel(index);
                playerModelPanel.Init(playerModel.Value);
                if (onSelect != null)
                    playerModelPanel.onClick = pointerEventData => onSelect.Invoke(playerModel.Value);
                ModelPanels.Add(playerModelPanel);
                num++;
            }
        }

        /// <summary>
        /// Opens the custom objects popup.
        /// </summary>
        public void OpenCustomObjectsPopup()
        {
            CustomObjectsPopup.Open();
            RenderCustomObjectsPopup();
        }

        /// <summary>
        /// Renders the custom objects popup.
        /// </summary>
        public void RenderCustomObjectsPopup()
        {
            CustomObjectsPopup.ClearContent();
            CustomObjectsPopup.SearchField.onValueChanged.NewListener(_val => RenderCustomObjectsPopup());

            var currentModel = PlayersData.Current.GetPlayerModel(playerIndex);

            var isDefault = PlayerModel.DefaultModels.Any(x => currentModel.basePart.id == x.basePart.id);

            if (isDefault)
                return;

            var add = EditorPrefabHolder.Instance.CreateAddButton(CustomObjectsPopup.Content, "Create");
            add.Text = "Create Custom Object";
            add.OnClick.NewListener(() =>
            {
                var customObject = new CustomPlayerObject();
                var id = LSText.randomNumString(16);
                customObject.id = id;
                currentModel.customObjects.Add(customObject);

                CustomObjectID = id;

                RenderCustomObjectsPopup();
                RenderDialog();
                PlayerManager.UpdatePlayerModels();
            });

            int num = 0;
            foreach (var customObject in currentModel.customObjects)
            {
                int index = num;
                if (!RTString.SearchString(CustomObjectsPopup.SearchTerm, customObject.name))
                    continue;

                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(CustomObjectsPopup.Content, customObject.name);
                var folderButtonFunction = gameObject.AddComponent<FolderButtonFunction>();
                var folderButtonStorage = gameObject.GetComponent<FunctionButtonStorage>();
                folderButtonStorage.Text = customObject.name;
                EditorThemeManager.ApplySelectable(folderButtonStorage.button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(folderButtonStorage.label);

                folderButtonStorage.OnClick.ClearAll();
                folderButtonFunction.onClick = eventData =>
                {
                    if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        EditorContextMenu.inst.ShowContextMenu(
                            new ButtonElement("Open", () =>
                            {
                                CustomObjectID = customObject.id;
                                RenderDialog();
                            }),
                            new ButtonElement("Delete", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this custom object?", () =>
                            {
                                currentModel.customObjects.RemoveAt(index);
                                RenderCustomObjectsPopup();
                                RenderDialog();
                                PlayerManager.UpdatePlayerModels();
                                RTEditor.inst.HideWarningPopup();
                            })),
                            new ButtonElement("Duplicate", () =>
                            {
                                var duplicateObject = customObject.Copy();
                                while (currentModel.customObjects.Has(x => x.id == duplicateObject.id)) // Ensure ID is not in list.
                                    duplicateObject.id = LSText.randomNumString(16);

                                var id = duplicateObject.id;
                                currentModel.customObjects.Add(duplicateObject);

                                CustomObjectID = id;

                                RenderCustomObjectsPopup();
                                RenderDialog();
                                PlayerManager.UpdatePlayerModels();
                            }),
                            new ButtonElement("Copy", () =>
                            {
                                copiedCustomObject = customObject.Copy(false);
                                EditorManager.inst.DisplayNotification("Copied custom player object!", 2f, EditorManager.NotificationType.Success);
                            }),
                            new ButtonElement("Paste", () =>
                            {
                                if (!copiedCustomObject)
                                {
                                    EditorManager.inst.DisplayNotification("No copied object yet.", 2f, EditorManager.NotificationType.Warning);
                                    return;
                                }

                                var duplicateObject = copiedCustomObject.Copy();
                                while (currentModel.customObjects.Has(x => x.id == duplicateObject.id)) // Ensure ID is not in list.
                                    duplicateObject.id = LSText.randomNumString(16);

                                var id = duplicateObject.id;
                                currentModel.customObjects.Add(duplicateObject);

                                CustomObjectID = id;

                                RenderCustomObjectsPopup();
                                RenderDialog();
                                PlayerManager.UpdatePlayerModels();
                                EditorManager.inst.DisplayNotification("Pasted custom player object!", 2f, EditorManager.NotificationType.Success);
                            })
                            );
                        return;
                    }

                    CustomObjectID = customObject.id;
                    RenderDialog();
                    CustomObjectsPopup.Close();
                };

                var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(gameObject.transform, "Delete");
                UIManager.SetRectTransform(delete.transform.AsRT(), new Vector2(280f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(32f, 32f));
                var deleteStorage = delete.GetComponent<DeleteButtonStorage>();
                deleteStorage.OnClick.NewListener(() => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this custom object?", () =>
                {
                    currentModel.customObjects.RemoveAt(index);
                    RenderCustomObjectsPopup();
                    RenderDialog();
                    PlayerManager.UpdatePlayerModels();
                }));
                EditorThemeManager.ApplyDeleteButton(deleteStorage);

                var duplicate = EditorPrefabHolder.Instance.Function1Button.Duplicate(gameObject.transform, "Duplicate");
                UIManager.SetRectTransform(duplicate.transform.AsRT(), new Vector2(180f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(120f, 32f));
                var duplicateStorage = duplicate.GetComponent<FunctionButtonStorage>();
                duplicateStorage.OnClick.NewListener(() =>
                {
                    var duplicateObject = customObject.Copy();
                    while (currentModel.customObjects.Has(x => x.id == duplicateObject.id)) // Ensure ID is not in list.
                        duplicateObject.id = LSText.randomNumString(16);

                    var id = duplicateObject.id;
                    currentModel.customObjects.Add(duplicateObject);

                    CustomObjectID = id;

                    RenderCustomObjectsPopup();
                    RenderDialog();
                    PlayerManager.UpdatePlayerModels();
                });

                duplicateStorage.Text = "Duplicate";

                EditorThemeManager.ApplyGraphic(duplicateStorage.button.image, ThemeGroup.Paste, true);
                EditorThemeManager.ApplyGraphic(duplicateStorage.label, ThemeGroup.Paste_Text);

                num++;
            }
        }

        /// <summary>
        /// Sets the current player model for the current player.
        /// </summary>
        /// <param name="playerModel">Player model to set.</param>
        public void SetCurrentModel(PlayerModel playerModel)
        {
            if (!playerModel)
                return;

            PlayersData.Current.playerModels[playerModel.basePart.id] = playerModel;
            PlayersData.Current.SetPlayerModel(playerIndex, playerModel.basePart.id);
            PlayerManager.RespawnPlayers();
            RenderDialog();
        }

        void OpenImageSelector(PlayerEditorShape ui, IShapeable shapeable, bool copyFile = true, bool storeImage = false)
        {
            var editorPath = RTFile.RemoveEndSlash(EditorLevelManager.inst.CurrentLevel.path);
            string jpgFile = FileBrowser.OpenSingleFile("Select an image!", editorPath, new string[] { "png", "jpg" });
            SelectImage(jpgFile, ui, shapeable, copyFile: copyFile, storeImage: storeImage);
        }

        void StoreImage(PlayerEditorShape ui, IShapeable shapeable, string file)
        {
            var currentModel = PlayersData.Current.GetPlayerModel(playerIndex);

            if (RTFile.FileExists(file))
            {
                var imageData = File.ReadAllBytes(file);

                var texture2d = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                texture2d.LoadImage(imageData);

                texture2d.wrapMode = TextureWrapMode.Clamp;
                texture2d.filterMode = FilterMode.Point;
                texture2d.Apply();

                currentModel.assets.AddSprite(shapeable.Text, SpriteHelper.CreateSprite(texture2d));
            }
            else
            {
                var imageData = LegacyPlugin.PALogoSprite.texture.EncodeToPNG();

                var texture2d = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                texture2d.LoadImage(imageData);

                texture2d.wrapMode = TextureWrapMode.Clamp;
                texture2d.filterMode = FilterMode.Point;
                texture2d.Apply();

                currentModel.assets.AddSprite(shapeable.Text, SpriteHelper.CreateSprite(texture2d));
            }
        }

        void SelectImage(string file, PlayerEditorShape ui, IShapeable shapeable, bool renderEditor = true, bool updateObject = true, bool copyFile = true, bool storeImage = false)
        {
            var editorPath = RTFile.RemoveEndSlash(EditorLevelManager.inst.CurrentLevel.path);
            RTFile.CreateDirectory(RTFile.CombinePaths(editorPath, "images"));

            file = RTFile.ReplaceSlash(file);
            CoreHelper.Log($"Selected file: {file}");
            if (!RTFile.FileExists(file))
                return;

            string jpgFileLocation = RTFile.CombinePaths(editorPath, "images", Path.GetFileName(file));

            if (copyFile && (EditorConfig.Instance.OverwriteImportedImages.Value || !RTFile.FileExists(jpgFileLocation)) && !file.Contains(editorPath))
                RTFile.CopyFile(file, jpgFileLocation);

            shapeable.Text = jpgFileLocation.Remove(editorPath + "/");

            if (storeImage)
                StoreImage(ui, shapeable, file);

            // Since setting image has no affect on the timeline object, we will only need to update the physical object.
            if (updateObject)
                PlayerManager.UpdatePlayerModels();

            if (renderEditor)
                RenderShape(ui, shapeable);
        }

        int VisibilityToInt(string vis) => vis switch
        {
            "isBoosting" => 0,
            "isTakingHit" => 1,
            "isZenMode" => 2,
            "isHealthPercentageGreater" => 3,
            "isHealthGreaterEquals" => 4,
            "isHealthEquals" => 5,
            "isHealthGreater" => 6,
            "isPressingKey" => 7,
            _ => 0,
        };

        string IntToVisibility(int val) => val switch
        {
            0 => "isBoosting",
            1 => "isTakingHit",
            2 => "isZenMode",
            3 => "isHealthPercentageGreater",
            4 => "isHealthGreaterEquals",
            5 => "isHealthEquals",
            6 => "isHealthGreater",
            7 => "isPressingKey",
            _ => "isBoosting",
        };

        #endregion
    }
}
