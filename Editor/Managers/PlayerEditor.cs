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

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Popups;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// The Player editor.
    /// </summary>
    public class PlayerEditor : MonoBehaviour
    {
        public static PlayerEditor inst;

        public string modelSearchTerm;
        public int playerModelIndex = 0;
        public string CustomObjectID { get; set; }

        public PlayerEditorDialog Dialog { get; set; }
        public ContentPopup ModelsPopup { get; set; }

        public static void Init() => Creator.NewGameObject(nameof(PlayerEditor), EditorManager.inst.transform.parent).AddComponent<PlayerEditor>();

        void Awake()
        {
            inst = this;

            try
            {
                PlayersData.Load(null);
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }

            StartCoroutine(GenerateUI());
        }

        public enum Tab
        {
            Global,
            Base, // includes stretch
            GUI,
            Head,
            Boost,
            Spawners, // Bullet and Pulse
            Tail, // All tail related parts go here
            Custom
        }

        public static Tab ParseTab(string str)
        {
            return
                str.Contains("Base") && !str.Contains("GUI") && !str.Contains("Tail") || str.Contains("Stretch") ? Tab.Base :
                !str.Contains("Pulse") && str.Contains("Head") || str.Contains("Face") ? Tab.Head :
                str.Contains("GUI") ? Tab.GUI :
                str.Contains("Boost") && !str.Contains("Tail") ? Tab.Boost :
                str.Contains("Pulse") || str.Contains("Bullet") ? Tab.Spawners :
                str.Contains("Tail") ? Tab.Tail : Tab.Custom;
        }

        public IEnumerator GenerateUI()
        {
            try
            {
                Dialog = new PlayerEditorDialog();
                Dialog.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog

            ModelsPopup = RTEditor.inst.GeneratePopup(EditorPopup.PLAYER_MODELS_POPUP, "Player Models", Vector2.zero, Vector2.zero, _val =>
            {
                modelSearchTerm = _val;
                StartCoroutine(RefreshModels());
            });

            yield break;
        }

        public void ShowTab(Tab tab)
        {
            for (int i = 0; i < Dialog.Tabs.Count; i++)
                Dialog.Tabs[i].SetActive((int)tab == i);
        }

        public PlayerEditorTab GetCurrentTab() => Dialog.Tabs[(int)CurrentTab];

        public PlayerEditorTab GetTab(Tab tab) => Dialog.Tabs[(int)tab];

        public IEnumerator RefreshEditor()
        {
            var currentModel = PlayersData.Current.GetPlayerModel(playerModelIndex);

            var isDefault = currentModel.IsDefault;
            Dialog.Content.Find("handler").gameObject.SetActive(isDefault && CurrentTab != Tab.Global);

            ShowTab(CurrentTab);
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

            yield break;
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
                colorButton.GetComponent<Image>().color = RTColors.GetPlayerColor(playerModelIndex, i, 1f, "FFFFFF");
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

            Dialog.GlobalTab.GameMode.Dropdown.SetValueWithoutNotify(GameData.Current.data.level.gameMode);
            Dialog.GlobalTab.GameMode.Dropdown.onValueChanged.NewListener(_val =>
            {
                GameData.Current.data.level.gameMode = _val;
                RTPlayer.SetGameDataProperties();
            });
            TriggerHelper.AddEventTriggers(Dialog.GlobalTab.GameMode.Dropdown.gameObject, TriggerHelper.ScrollDelta(Dialog.GlobalTab.GameMode.Dropdown));

            RenderDropdown(Dialog.BaseTab.RotateMode.Dropdown, GameData.Current.data.level.gameMode,
                onValueChanged: _val =>
                {
                    GameData.Current.data.level.gameMode = _val;
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

        public void RenderBaseTab(PlayerModel currentModel)
        {
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

            RenderInteger(Dialog.BaseTab.Health.Field, currentModel.basePart.health,
                onValueChanged: _val =>
                {
                    if (int.TryParse(_val, out int num))
                    {
                        currentModel.basePart.health = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderSingle(Dialog.BaseTab.MoveSpeed.Field, currentModel.basePart.moveSpeed,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.basePart.moveSpeed = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderSingle(Dialog.BaseTab.BoostSpeed.Field, currentModel.basePart.boostSpeed,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.basePart.boostSpeed = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderSingle(Dialog.BaseTab.BoostCooldown.Field, currentModel.basePart.boostCooldown,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.basePart.boostCooldown = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderSingle(Dialog.BaseTab.MinBoostTime.Field, currentModel.basePart.minBoostTime,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.basePart.minBoostTime = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderSingle(Dialog.BaseTab.MaxBoostTime.Field, currentModel.basePart.maxBoostTime,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.basePart.maxBoostTime = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderSingle(Dialog.BaseTab.HitCooldown.Field, currentModel.basePart.hitCooldown,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
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

            Dialog.BaseTab.CollisionAccurate.Toggle.SetIsOnWithoutNotify(currentModel.basePart.collisionAccurate);
            Dialog.BaseTab.CollisionAccurate.Toggle.onValueChanged.NewListener(_val =>
            {
                currentModel.basePart.collisionAccurate = _val;
                PlayerManager.UpdatePlayerModels();
            });

            Dialog.BaseTab.SprintSneakActive.Toggle.SetIsOnWithoutNotify(currentModel.basePart.sprintSneakActive);
            Dialog.BaseTab.SprintSneakActive.Toggle.onValueChanged.NewListener(_val =>
            {
                currentModel.basePart.sprintSneakActive = _val;
                PlayerManager.UpdatePlayerModels();
            });
            
            Dialog.BaseTab.CanBoost.Toggle.SetIsOnWithoutNotify(currentModel.basePart.canBoost);
            Dialog.BaseTab.CanBoost.Toggle.onValueChanged.NewListener(_val =>
            {
                currentModel.basePart.canBoost = _val;
                PlayerManager.UpdatePlayerModels();
            });

            RenderSingle(Dialog.BaseTab.JumpGravity.Field, currentModel.basePart.jumpGravity,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.basePart.jumpGravity = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderSingle(Dialog.BaseTab.JumpIntensity.Field, currentModel.basePart.jumpIntensity,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.basePart.jumpIntensity = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderInteger(Dialog.BaseTab.JumpCount.Field, currentModel.basePart.jumpCount,
                onValueChanged: _val =>
                {
                    if (int.TryParse(_val, out int num))
                    {
                        currentModel.basePart.jumpCount = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });
            
            RenderInteger(Dialog.BaseTab.JumpBoostCount.Field, currentModel.basePart.jumpBoostCount,
                onValueChanged: _val =>
                {
                    if (int.TryParse(_val, out int num))
                    {
                        currentModel.basePart.jumpBoostCount = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderSingle(Dialog.BaseTab.Bounciness.Field, currentModel.basePart.bounciness,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.basePart.bounciness = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            Dialog.BaseTab.StretchActive.Toggle.SetIsOnWithoutNotify(currentModel.stretchPart.active);
            Dialog.BaseTab.StretchActive.Toggle.onValueChanged.NewListener(_val =>
            {
                currentModel.stretchPart.active = _val;
                PlayerManager.UpdatePlayerModels();
            });

            RenderSingle(Dialog.BaseTab.StretchAmount.Field, currentModel.stretchPart.amount,
                onValueChanged: _val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        currentModel.stretchPart.amount = num;
                        PlayerManager.UpdatePlayerModels();
                    }
                });

            RenderDropdown(Dialog.BaseTab.StretchEasing.Dropdown, currentModel.stretchPart.easing,
                onValueChanged: _val =>
                {
                    currentModel.stretchPart.easing = _val;
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
        }

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

        public void RenderHeadTab(PlayerModel currentModel) => RenderObject(Dialog.HeadTab, currentModel.headPart);

        public void RenderBoostTab(PlayerModel currentModel) => RenderObject(Dialog.BoostTab, currentModel.boostPart);

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

            RenderDropdown(Dialog.SpawnerTab.PulseColorEasing.Dropdown, currentModel.pulsePart.easingColor,
                onValueChanged: _val =>
                {
                    currentModel.pulsePart.easingColor = _val;
                    PlayerManager.UpdatePlayerModels();
                });

            RenderDropdown(Dialog.SpawnerTab.PulseOpacityEasing.Dropdown, currentModel.pulsePart.easingOpacity,
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

            RenderDropdown(Dialog.SpawnerTab.PulsePositionEasing.Dropdown, currentModel.pulsePart.easingPosition,
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

            RenderDropdown(Dialog.SpawnerTab.PulseScaleEasing.Dropdown, currentModel.pulsePart.easingScale,
                onValueChanged: _val =>
                {
                    currentModel.pulsePart.easingScale = _val;
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

            RenderDropdown(Dialog.SpawnerTab.PulseRotationEasing.Dropdown, currentModel.pulsePart.easingRotation,
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

            RenderDropdown(Dialog.SpawnerTab.BulletColorEasing.Dropdown, currentModel.bulletPart.easingColor,
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

            RenderDropdown(Dialog.SpawnerTab.BulletOpacityEasing.Dropdown, currentModel.bulletPart.easingOpacity,
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

            RenderDropdown(Dialog.SpawnerTab.BulletPositionEasing.Dropdown, currentModel.bulletPart.easingPosition,
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

            RenderDropdown(Dialog.SpawnerTab.BulletScaleEasing.Dropdown, currentModel.bulletPart.easingScale,
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

            RenderDropdown(Dialog.SpawnerTab.BulletRotationEasing.Dropdown, currentModel.bulletPart.easingRotation,
                onValueChanged: _val =>
                {
                    currentModel.bulletPart.easingRotation = _val;
                    PlayerManager.UpdatePlayerModels();
                });

            #endregion
        }

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
                tab.Remove.Button.onClick.NewListener(() => currentModel.RemoveTail(index));
                Dialog.TailTab.TailParts.Add(tab);
                RenderObject(tab, currentModel.tailParts[i]);
            }

            yield break;
        }

        public void RenderCustomTab(PlayerModel currentModel)
        {
            Dialog.CustomObjectTab.SelectCustomObject.Button.onClick.NewListener(() =>
            {
                ModelsPopup.Open();
                StartCoroutine(RefreshCustomObjects());
            });

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

            var add = PrefabEditor.inst.CreatePrefab.Duplicate(content, "Add");
            var addText = add.transform.Find("Text").GetComponent<Text>();
            addText.text = "Add Visiblity Setting";
            ((RectTransform)add.transform).sizeDelta = new Vector2(760f, 32f);
            var addButton = add.GetComponent<Button>();
            addButton.onClick.NewListener(() =>
            {
                var newVisibility = new CustomPlayerObject.Visibility();
                newVisibility.command = IntToVisibility(0);
                customObject.visibilitySettings.Add(newVisibility);
                StartCoroutine(RefreshEditor());
            });
            EditorThemeManager.ApplyGraphic(addButton.image, ThemeGroup.Add);
            EditorThemeManager.ApplyGraphic(addText, ThemeGroup.Add_Text);

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

                toggle.label.text = $"Not: {(visibility.not ? "Yes" : "No")}";
                toggle.button.onClick.NewListener(() =>
                {
                    visibility.not = !visibility.not;
                    toggle.label.text = $"Not: {(visibility.not ? "Yes" : "No")}";
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
                deleteButton.button.onClick.NewListener(() =>
                {
                    customObject.visibilitySettings.RemoveAt(index);
                    StartCoroutine(RefreshEditor());
                });
                EditorThemeManager.ApplyGraphic(deleteButton.baseImage, ThemeGroup.Delete, true);
                EditorThemeManager.ApplyGraphic(deleteButton.image, ThemeGroup.Delete_Text);

                num++;
            }

            RenderObject(Dialog.CustomObjectTab, customObject);
        }

        public IEnumerator RefreshModels(Action<PlayerModel> onSelect = null)
        {
            ModelsPopup.ClearContent();
            ModelsPopup.SearchField.onValueChanged.NewListener(_val =>
            {
                modelSearchTerm = _val;
                StartCoroutine(RefreshModels(onSelect));
            });

            int num = 0;
            foreach (var playerModel in PlayersData.externalPlayerModels)
            {
                int index = num;
                var name = playerModel.Value.basePart.name;
                if (!RTString.SearchString(modelSearchTerm, name))
                {
                    num++;
                    continue;
                }

                var model = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(ModelsPopup.Content, name);
                var modelButton = model.GetComponent<Button>();
                modelButton.onClick.NewListener(() =>
                {
                    if (onSelect != null)
                    {
                        onSelect.Invoke(playerModel.Value);
                        return;
                    }

                    PlayersData.Current.playerModels[playerModel.Key] = playerModel.Value;
                    PlayersData.Current.SetPlayerModel(playerModelIndex, playerModel.Key);
                    PlayerManager.RespawnPlayers();
                    StartCoroutine(RefreshEditor());
                });

                var modelContextMenu = model.AddComponent<ContextClickable>();
                modelContextMenu.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Open & Use", () =>
                        {
                            PlayersData.Current.SetPlayerModel(playerModelIndex, playerModel.Key);
                            PlayerManager.RespawnPlayers();
                            StartCoroutine(RefreshEditor());
                        }),
                        new ButtonFunction("Set to Global", () => PlayerManager.PlayerIndexes[playerModelIndex].Value = playerModel.Key),
                        new ButtonFunction("Create New", CreateNewModel),
                        new ButtonFunction("Save", Save),
                        new ButtonFunction("Reload", Reload),
                        new ButtonFunction(true),
                        new ButtonFunction("Duplicate", () =>
                        {
                            var dup = PlayersData.Current.DuplicatePlayerModel(playerModel.Key);
                            PlayersData.externalPlayerModels[dup.basePart.id] = dup;
                            if (dup)
                                PlayersData.Current.SetPlayerModel(playerModelIndex, dup.basePart.id);
                        }),
                        new ButtonFunction("Delete", () =>
                        {
                            if (index < 5)
                            {
                                EditorManager.inst.DisplayNotification($"Cannot delete a default player model.", 2f, EditorManager.NotificationType.Warning);
                                return;
                            }

                            RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this Player Model?", () =>
                            {
                                PlayersData.Current.SetPlayerModel(playerModelIndex, PlayerModel.DEFAULT_ID);
                                PlayersData.externalPlayerModels.Remove(playerModel.Key);
                                PlayersData.Current.playerModels.Remove(playerModel.Key);
                                PlayerManager.RespawnPlayers();
                                StartCoroutine(RefreshEditor());
                                StartCoroutine(RefreshModels(onSelect));

                                RTEditor.inst.HideWarningPopup();
                            }, RTEditor.inst.HideWarningPopup);
                        })
                        );
                };

                var text = model.transform.GetChild(0).GetComponent<Text>();
                text.text = name;

                var image = model.transform.Find("Image").GetComponent<Image>();
                image.sprite = EditorSprites.PlayerSprite;

                EditorThemeManager.ApplySelectable(modelButton, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyGraphic(image, ThemeGroup.Light_Text);
                EditorThemeManager.ApplyLightText(text);

                if (index < 5)
                {
                    num++;
                    continue;
                }

                var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(model.transform, "Delete");
                UIManager.SetRectTransform(delete.transform.AsRT(), new Vector2(280f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(32f, 32f));
                var deleteStorage = delete.GetComponent<DeleteButtonStorage>();
                deleteStorage.button.onClick.NewListener(() =>
                {
                    RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this Player Model?", () =>
                    {
                        PlayersData.Current.SetPlayerModel(playerModelIndex, PlayerModel.DEFAULT_ID);
                        PlayersData.externalPlayerModels.Remove(playerModel.Key);
                        PlayersData.Current.playerModels.Remove(playerModel.Key);
                        PlayerManager.RespawnPlayers();
                        StartCoroutine(RefreshEditor());
                        StartCoroutine(RefreshModels(onSelect));


                        RTEditor.inst.HideWarningPopup();
                    }, RTEditor.inst.HideWarningPopup);
                });

                EditorThemeManager.ApplyGraphic(deleteStorage.baseImage, ThemeGroup.Delete, true);
                EditorThemeManager.ApplyGraphic(deleteStorage.image, ThemeGroup.Delete_Text);

                num++;
            }

            yield break;
        }

        public IEnumerator RefreshCustomObjects()
        {
            ModelsPopup.ClearContent();

            var currentModel = PlayersData.Current.GetPlayerModel(playerModelIndex);

            var isDefault = PlayerModel.DefaultModels.Any(x => currentModel.basePart.id == x.basePart.id);

            if (isDefault)
                yield break;

            var createNew = PrefabEditor.inst.CreatePrefab.Duplicate(ModelsPopup.Content, "Create");
            var createNewButton = createNew.GetComponent<Button>();
            createNewButton.onClick.NewListener(() =>
            {
                var customObject = new CustomPlayerObject();
                var id = LSText.randomNumString(16);
                customObject.id = id;
                currentModel.customObjects.Add(customObject);

                CustomObjectID = id;

                StartCoroutine(RefreshCustomObjects());
                StartCoroutine(RefreshEditor());
                PlayerManager.UpdatePlayerModels();
            });

            var createNewText = createNew.transform.GetChild(0).GetComponent<Text>();
            createNewText.text = "Create custom object";

            EditorThemeManager.ApplyGraphic(createNewButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(createNewText, ThemeGroup.Add_Text);

            int num = 0;
            foreach (var customObject in currentModel.customObjects)
            {
                int index = num;
                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(ModelsPopup.Content, customObject.name);
                var folderButtonFunction = gameObject.AddComponent<FolderButtonFunction>();
                var folderButtonStorage = gameObject.GetComponent<FunctionButtonStorage>();
                folderButtonStorage.label.text = customObject.name;
                EditorThemeManager.ApplySelectable(folderButtonStorage.button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(folderButtonStorage.label);

                var button = gameObject.GetComponent<Button>();
                button.onClick.ClearAll();
                folderButtonFunction.onClick = eventData =>
                {
                    if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        EditorContextMenu.inst.ShowContextMenu(
                            new ButtonFunction("Open", () =>
                            {
                                CustomObjectID = customObject.id;
                                StartCoroutine(RefreshEditor());
                            }),
                            new ButtonFunction("Delete", () =>
                            {
                                RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this custom object?", () =>
                                {
                                    currentModel.customObjects.RemoveAll(x => x.id == CustomObjectID);
                                    StartCoroutine(RefreshCustomObjects());
                                    StartCoroutine(RefreshEditor());
                                    PlayerManager.UpdatePlayerModels();
                                    RTEditor.inst.HideWarningPopup();
                                }, RTEditor.inst.HideWarningPopup);
                            }),
                            new ButtonFunction("Duplicate", () =>
                            {
                                var duplicateObject = customObject.Copy();
                                while (currentModel.customObjects.Has(x => x.id == duplicateObject.id)) // Ensure ID is not in list.
                                    duplicateObject.id = LSText.randomNumString(16);

                                var id = duplicateObject.id;
                                currentModel.customObjects.Add(duplicateObject);

                                CustomObjectID = id;

                                StartCoroutine(RefreshCustomObjects());
                                StartCoroutine(RefreshEditor());
                                PlayerManager.UpdatePlayerModels();
                            }),
                            new ButtonFunction("Copy", () =>
                            {
                                copiedCustomObject = customObject.Copy(false);
                                EditorManager.inst.DisplayNotification("Copied custom player object!", 2f, EditorManager.NotificationType.Success);
                            }),
                            new ButtonFunction("Paste", () =>
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

                                StartCoroutine(RefreshCustomObjects());
                                StartCoroutine(RefreshEditor());
                                PlayerManager.UpdatePlayerModels();
                                EditorManager.inst.DisplayNotification("Pasted custom player object!", 2f, EditorManager.NotificationType.Success);
                            })
                            );
                        return;
                    }

                    CustomObjectID = customObject.id;
                    StartCoroutine(RefreshEditor());
                    ModelsPopup.Close();
                };

                var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(gameObject.transform, "Delete");
                UIManager.SetRectTransform(delete.transform.AsRT(), new Vector2(280f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(32f, 32f));
                var deleteStorage = delete.GetComponent<DeleteButtonStorage>();
                deleteStorage.button.onClick.NewListener(() =>
                {
                    RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this custom object?", () =>
                    {
                        currentModel.customObjects.RemoveAll(x => x.id == CustomObjectID);
                        StartCoroutine(RefreshCustomObjects());
                        StartCoroutine(RefreshEditor());
                        PlayerManager.UpdatePlayerModels();
                        RTEditor.inst.HideWarningPopup();
                    }, RTEditor.inst.HideWarningPopup);
                });
                EditorThemeManager.ApplyGraphic(deleteStorage.baseImage, ThemeGroup.Delete, true);
                EditorThemeManager.ApplyGraphic(deleteStorage.image, ThemeGroup.Delete_Text);

                var duplicate = EditorPrefabHolder.Instance.Function1Button.Duplicate(gameObject.transform, "Duplicate");
                UIManager.SetRectTransform(duplicate.transform.AsRT(), new Vector2(180f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(120f, 32f));
                var duplicateStorage = duplicate.GetComponent<FunctionButtonStorage>();
                duplicateStorage.button.onClick.NewListener(() =>
                {
                    var duplicateObject = customObject.Copy();
                    while (currentModel.customObjects.Has(x => x.id == duplicateObject.id)) // Ensure ID is not in list.
                        duplicateObject.id = LSText.randomNumString(16);

                    var id = duplicateObject.id;
                    currentModel.customObjects.Add(duplicateObject);

                    CustomObjectID = id;

                    StartCoroutine(RefreshCustomObjects());
                    StartCoroutine(RefreshEditor());
                    PlayerManager.UpdatePlayerModels();
                });

                duplicateStorage.label.text = "Duplicate";

                EditorThemeManager.ApplyGraphic(duplicateStorage.button.image, ThemeGroup.Paste, true);
                EditorThemeManager.ApplyGraphic(duplicateStorage.label, ThemeGroup.Paste_Text);

                num++;
            }

            yield break;
        }

        public CustomPlayerObject copiedCustomObject;

        public void RenderShape(PlayerEditorShape ui, IShapeable shapeable)
        {
            var shape = ui.GameObject.transform.Find("shape");
            var shapeSettings = ui.GameObject.transform.Find("shapesettings");

            shape.AsRT().sizeDelta = new Vector2(400f, 32);
            shapeSettings.AsRT().sizeDelta = new Vector2(400f, 32);

            var shapeGLG = shape.GetComponent<GridLayoutGroup>();
            shapeGLG.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            shapeGLG.constraintCount = 1;
            shapeGLG.spacing = new Vector2(7.6f, 0f);

            if (!ui.updatedShapes)
            {
                // Initial removing
                DestroyImmediate(shape.GetComponent<ToggleGroup>());

                var toDestroy = new List<GameObject>();

                for (int j = 0; j < shape.childCount; j++)
                    toDestroy.Add(shape.GetChild(j).gameObject);

                for (int j = 0; j < shapeSettings.childCount; j++)
                {
                    if (j != 4 && j != 6)
                        for (int k = 0; k < shapeSettings.GetChild(j).childCount; k++)
                        {
                            toDestroy.Add(shapeSettings.GetChild(j).GetChild(k).gameObject);
                        }
                }

                foreach (var obj in toDestroy)
                    DestroyImmediate(obj);

                toDestroy = null;

                for (int i = 0; i < ShapeManager.inst.Shapes2D.Count; i++)
                {
                    var shapeType = (ShapeType)i;
                    var obj = ObjectEditor.inst.shapeButtonPrefab.Duplicate(shape, (i + 1).ToString(), i);
                    if (obj.transform.Find("Image") && obj.transform.Find("Image").gameObject.TryGetComponent(out Image image))
                    {
                        image.sprite = ShapeManager.inst.Shapes2D[i].icon;
                        EditorThemeManager.ApplyGraphic(image, ThemeGroup.Toggle_1_Check);
                    }

                    if (!obj.GetComponent<HoverUI>())
                    {
                        var hoverUI = obj.AddComponent<HoverUI>();
                        hoverUI.animatePos = false;
                        hoverUI.animateSca = true;
                        hoverUI.size = 1.1f;
                    }

                    var shapeToggle = obj.GetComponent<Toggle>();
                    EditorThemeManager.ApplyToggle(shapeToggle, ThemeGroup.Background_1);

                    ui.ShapeToggles.Add(shapeToggle);

                    ui.ShapeOptionToggles.Add(new List<Toggle>());

                    if (shapeType != ShapeType.Text && shapeType != ShapeType.Image && shapeType != ShapeType.Polygon)
                    {
                        var so = shapeSettings.Find((i + 1).ToString());
                        if (!so)
                        {
                            so = shapeSettings.Find("6").gameObject.Duplicate(shapeSettings, (i + 1).ToString()).transform;
                            CoreHelper.DestroyChildren(so);
                        }

                        var rect = so.AsRT();
                        if (!so.GetComponent<ScrollRect>())
                        {
                            var scroll = so.gameObject.AddComponent<ScrollRect>();
                            so.gameObject.AddComponent<Mask>();
                            var ad = so.gameObject.AddComponent<Image>();

                            scroll.horizontal = true;
                            scroll.vertical = false;
                            scroll.content = rect;
                            scroll.viewport = rect;
                            ad.color = new Color(1f, 1f, 1f, 0.01f);
                        }

                        for (int j = 0; j < ShapeManager.inst.Shapes2D[i].Count; j++)
                        {
                            var opt = ObjectEditor.inst.shapeButtonPrefab.Duplicate(shapeSettings.GetChild(i), (j + 1).ToString(), j);
                            if (opt.transform.Find("Image") && opt.transform.Find("Image").gameObject.TryGetComponent(out Image image1))
                            {
                                image1.sprite = ShapeManager.inst.Shapes2D[i][j].icon;
                                EditorThemeManager.ApplyGraphic(image1, ThemeGroup.Toggle_1_Check);
                            }

                            if (!opt.GetComponent<HoverUI>())
                            {
                                var hoverUI = opt.AddComponent<HoverUI>();
                                hoverUI.animatePos = false;
                                hoverUI.animateSca = true;
                                hoverUI.size = 1.1f;
                            }

                            var shapeOptionToggle = opt.GetComponent<Toggle>();
                            EditorThemeManager.ApplyToggle(shapeOptionToggle, ThemeGroup.Background_1);

                            ui.ShapeOptionToggles[i].Add(shapeOptionToggle);

                            var layoutElement = opt.AddComponent<LayoutElement>();
                            layoutElement.layoutPriority = 1;
                            layoutElement.minWidth = 32f;

                            ((RectTransform)opt.transform).sizeDelta = new Vector2(32f, 32f);

                            if (!opt.GetComponent<HoverUI>())
                            {
                                var he = opt.AddComponent<HoverUI>();
                                he.animatePos = false;
                                he.animateSca = true;
                                he.size = 1.1f;
                            }
                        }

                        ObjectEditor.inst.LastGameObject(shapeSettings.GetChild(i));
                    }

                    if (shapeType == ShapeType.Polygon)
                    {
                        var so = shapeSettings.Find((i + 1).ToString());

                        if (!so)
                        {
                            so = shapeSettings.Find("6").gameObject.Duplicate(shapeSettings, (i + 1).ToString()).transform;
                            CoreHelper.DestroyChildren(so);
                        }

                        var rect = so.AsRT();
                        DestroyImmediate(so.GetComponent<ScrollRect>());
                        DestroyImmediate(so.GetComponent<HorizontalLayoutGroup>());

                        so.gameObject.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.05f);

                        var verticalLayoutGroup = so.gameObject.GetOrAddComponent<VerticalLayoutGroup>();
                        verticalLayoutGroup.spacing = 4f;

                        // Polygon Settings
                        {
                            #region Sides

                            var sides = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "sides");
                            var sidesStorage = sides.GetComponent<InputFieldStorage>();

                            Destroy(sidesStorage.addButton.gameObject);
                            Destroy(sidesStorage.subButton.gameObject);
                            Destroy(sidesStorage.leftGreaterButton.gameObject);
                            Destroy(sidesStorage.middleButton.gameObject);
                            Destroy(sidesStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.AddInputField(sidesStorage.inputField);
                            EditorThemeManager.AddSelectable(sidesStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(sidesStorage.rightButton, ThemeGroup.Function_2, false);

                            var sidesLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(sides.transform, "label", 0);
                            var sidesLabelText = sidesLabel.GetComponent<Text>();
                            sidesLabelText.alignment = TextAnchor.MiddleLeft;
                            sidesLabelText.text = "Sides";
                            sidesLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                            EditorThemeManager.AddLightText(sidesLabelText);
                            var sidesLabelLayout = sidesLabel.AddComponent<LayoutElement>();
                            sidesLabelLayout.minWidth = 100f;

                            #endregion

                            #region Roundness

                            var roundness = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "roundness");
                            var roundnessStorage = roundness.GetComponent<InputFieldStorage>();

                            Destroy(roundnessStorage.addButton.gameObject);
                            Destroy(roundnessStorage.subButton.gameObject);
                            Destroy(roundnessStorage.leftGreaterButton.gameObject);
                            Destroy(roundnessStorage.middleButton.gameObject);
                            Destroy(roundnessStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.AddInputField(roundnessStorage.inputField);
                            EditorThemeManager.AddSelectable(roundnessStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(roundnessStorage.rightButton, ThemeGroup.Function_2, false);

                            var roundnessLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(roundness.transform, "label", 0);
                            var roundnessLabelText = roundnessLabel.GetComponent<Text>();
                            roundnessLabelText.alignment = TextAnchor.MiddleLeft;
                            roundnessLabelText.text = "Roundness";
                            roundnessLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                            EditorThemeManager.AddLightText(roundnessLabelText);
                            var roundnessLabelLayout = roundnessLabel.AddComponent<LayoutElement>();
                            roundnessLabelLayout.minWidth = 100f;

                            #endregion

                            #region Thickness

                            var thickness = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "thickness");
                            var thicknessStorage = thickness.GetComponent<InputFieldStorage>();

                            Destroy(thicknessStorage.addButton.gameObject);
                            Destroy(thicknessStorage.subButton.gameObject);
                            Destroy(thicknessStorage.leftGreaterButton.gameObject);
                            Destroy(thicknessStorage.middleButton.gameObject);
                            Destroy(thicknessStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.AddInputField(thicknessStorage.inputField);
                            EditorThemeManager.AddSelectable(thicknessStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(thicknessStorage.rightButton, ThemeGroup.Function_2, false);

                            var thicknessLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(thickness.transform, "label", 0);
                            var thicknessLabelText = thicknessLabel.GetComponent<Text>();
                            thicknessLabelText.alignment = TextAnchor.MiddleLeft;
                            thicknessLabelText.text = "Thickness";
                            thicknessLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                            EditorThemeManager.AddLightText(thicknessLabelText);
                            var thicknessLabelLayout = thicknessLabel.AddComponent<LayoutElement>();
                            thicknessLabelLayout.minWidth = 100f;

                            #endregion

                            #region Thickness Offset

                            var thicknessOffset = Creator.NewUIObject("thickness offset", so);
                            var thicknessOffsetLayout = thicknessOffset.AddComponent<HorizontalLayoutGroup>();

                            var thicknessOffsetLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(thicknessOffset.transform, "label");
                            var thicknessOffsetLabelText = thicknessOffsetLabel.GetComponent<Text>();
                            thicknessOffsetLabelText.alignment = TextAnchor.MiddleLeft;
                            thicknessOffsetLabelText.text = "Thick Offset";
                            thicknessOffsetLabelText.rectTransform.sizeDelta = new Vector2(130f, 32f);
                            EditorThemeManager.AddLightText(thicknessOffsetLabelText);
                            var thicknessOffsetLabelLayout = thicknessOffsetLabel.AddComponent<LayoutElement>();
                            thicknessOffsetLabelLayout.minWidth = 130f;

                            var thicknessOffsetX = EditorPrefabHolder.Instance.NumberInputField.Duplicate(thicknessOffset.transform, "x");
                            var thicknessOffsetXStorage = thicknessOffsetX.GetComponent<InputFieldStorage>();

                            Destroy(thicknessOffsetXStorage.addButton.gameObject);
                            Destroy(thicknessOffsetXStorage.subButton.gameObject);
                            Destroy(thicknessOffsetXStorage.leftGreaterButton.gameObject);
                            Destroy(thicknessOffsetXStorage.middleButton.gameObject);
                            Destroy(thicknessOffsetXStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.AddInputField(thicknessOffsetXStorage.inputField);
                            EditorThemeManager.AddSelectable(thicknessOffsetXStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(thicknessOffsetXStorage.rightButton, ThemeGroup.Function_2, false);

                            var thicknessOffsetY = EditorPrefabHolder.Instance.NumberInputField.Duplicate(thicknessOffset.transform, "y");
                            var thicknessOffsetYStorage = thicknessOffsetY.GetComponent<InputFieldStorage>();

                            Destroy(thicknessOffsetYStorage.addButton.gameObject);
                            Destroy(thicknessOffsetYStorage.subButton.gameObject);
                            Destroy(thicknessOffsetYStorage.leftGreaterButton.gameObject);
                            Destroy(thicknessOffsetYStorage.middleButton.gameObject);
                            Destroy(thicknessOffsetYStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.AddInputField(thicknessOffsetYStorage.inputField);
                            EditorThemeManager.AddSelectable(thicknessOffsetYStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(thicknessOffsetYStorage.rightButton, ThemeGroup.Function_2, false);

                            #endregion

                            #region Thickness Scale

                            var thicknessScale = Creator.NewUIObject("thickness scale", so);
                            var thicknessScaleLayout = thicknessScale.AddComponent<HorizontalLayoutGroup>();

                            var thicknessScaleLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(thicknessScale.transform, "label");
                            var thicknessScaleLabelText = thicknessScaleLabel.GetComponent<Text>();
                            thicknessScaleLabelText.alignment = TextAnchor.MiddleLeft;
                            thicknessScaleLabelText.text = "Thick Scale";
                            thicknessScaleLabelText.rectTransform.sizeDelta = new Vector2(130f, 32f);
                            EditorThemeManager.AddLightText(thicknessScaleLabelText);
                            var thicknessScaleLabelLayout = thicknessScaleLabel.AddComponent<LayoutElement>();
                            thicknessScaleLabelLayout.minWidth = 130f;

                            var thicknessScaleX = EditorPrefabHolder.Instance.NumberInputField.Duplicate(thicknessScale.transform, "x");
                            var thicknessScaleXStorage = thicknessScaleX.GetComponent<InputFieldStorage>();

                            Destroy(thicknessScaleXStorage.addButton.gameObject);
                            Destroy(thicknessScaleXStorage.subButton.gameObject);
                            Destroy(thicknessScaleXStorage.leftGreaterButton.gameObject);
                            Destroy(thicknessScaleXStorage.middleButton.gameObject);
                            Destroy(thicknessScaleXStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.AddInputField(thicknessScaleXStorage.inputField);
                            EditorThemeManager.AddSelectable(thicknessScaleXStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(thicknessScaleXStorage.rightButton, ThemeGroup.Function_2, false);

                            var thicknessScaleY = EditorPrefabHolder.Instance.NumberInputField.Duplicate(thicknessScale.transform, "y");
                            var thicknessScaleYStorage = thicknessScaleY.GetComponent<InputFieldStorage>();

                            Destroy(thicknessScaleYStorage.addButton.gameObject);
                            Destroy(thicknessScaleYStorage.subButton.gameObject);
                            Destroy(thicknessScaleYStorage.leftGreaterButton.gameObject);
                            Destroy(thicknessScaleYStorage.middleButton.gameObject);
                            Destroy(thicknessScaleYStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.AddInputField(thicknessScaleYStorage.inputField);
                            EditorThemeManager.AddSelectable(thicknessScaleYStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(thicknessScaleYStorage.rightButton, ThemeGroup.Function_2, false);

                            #endregion

                            #region Slices

                            var slices = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "slices");
                            var slicesStorage = slices.GetComponent<InputFieldStorage>();

                            Destroy(slicesStorage.addButton.gameObject);
                            Destroy(slicesStorage.subButton.gameObject);
                            Destroy(slicesStorage.leftGreaterButton.gameObject);
                            Destroy(slicesStorage.middleButton.gameObject);
                            Destroy(slicesStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.AddInputField(slicesStorage.inputField);
                            EditorThemeManager.AddSelectable(slicesStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(slicesStorage.rightButton, ThemeGroup.Function_2, false);

                            var slicesLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(slices.transform, "label", 0);
                            var slicesLabelText = slicesLabel.GetComponent<Text>();
                            slicesLabelText.alignment = TextAnchor.MiddleLeft;
                            slicesLabelText.text = "Slices";
                            slicesLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                            EditorThemeManager.AddLightText(slicesLabelText);
                            var slicesLabelLayout = slicesLabel.AddComponent<LayoutElement>();
                            slicesLabelLayout.minWidth = 100f;

                            #endregion
                        }
                    }
                }

                ui.updatedShapes = true;
            }

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
                toggle.onValueChanged.ClearAll();
                toggle.isOn = type == index;
                toggle.gameObject.SetActive(RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes.Length);

                if (RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes.Length)
                    toggle.onValueChanged.AddListener(_val =>
                    {
                        if (_val)
                        {
                            CoreHelper.Log($"Set shape to {index}");
                            if (shapeable != null)
                            {
                                shapeable.Shape = index;
                                shapeable.ShapeOption = 0;
                            }

                            if (shapeable is CustomPlayerObject customObject && customObject.ShapeType == ShapeType.Polygon && EditorConfig.Instance.AutoPolygonRadius.Value)
                                customObject.polygonShape.Radius = customObject.polygonShape.GetAutoRadius();

                            PlayerManager.UpdatePlayerModels();
                            RenderShape(ui, shapeable);
                            LayoutRebuilder.ForceRebuildLayoutImmediate(ui.GameObject.transform.parent.AsRT());
                        }
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
                        textIF.onValueChanged.ClearAll();
                        textIF.text = customObject.text;
                        textIF.onValueChanged.AddListener(_val =>
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

                        var currentModel = PlayersData.Current.GetPlayerModel(playerModelIndex);

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
                            if (shapeable != null)
                            {
                                shapeable.Shape = 0;
                                shapeable.ShapeOption = 0;
                            }

                            PlayerManager.UpdatePlayerModels();
                            RenderShape(ui, shapeable);

                            break;
                        }

                        ui.GameObject.transform.AsRT().sizeDelta = new Vector2(750f, 332f);
                        shapeSettings.AsRT().anchoredPosition = new Vector2(568f, -145f);
                        shapeSettings.AsRT().sizeDelta = new Vector2(351f, 244f);

                        var radius = shapeSettings.Find("10/radius").gameObject.GetComponent<InputFieldStorage>();
                        radius.inputField.onValueChanged.ClearAll();
                        radius.inputField.text = shapeable.Polygon.Radius.ToString();
                        radius.SetInteractible(!EditorConfig.Instance.AutoPolygonRadius.Value);
                        if (!EditorConfig.Instance.AutoPolygonRadius.Value)
                        {
                            radius.inputField.onValueChanged.AddListener(_val =>
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

                        var contextMenu = radius.inputField.gameObject.GetOrAddComponent<ContextClickable>();
                        contextMenu.onClick = eventData =>
                        {
                            if (eventData.button != PointerEventData.InputButton.Right)
                                return;

                            var buttonFunctions = new List<ButtonFunction>()
                            {
                                new ButtonFunction($"Auto Assign Radius [{(EditorConfig.Instance.AutoPolygonRadius.Value ? "On" : "Off")}]", () =>
                                {
                                    EditorConfig.Instance.AutoPolygonRadius.Value = !EditorConfig.Instance.AutoPolygonRadius.Value;
                                    RenderShape(ui, shapeable);
                                })
                            };
                            if (!EditorConfig.Instance.AutoPolygonRadius.Value)
                            {
                                buttonFunctions.Add(new ButtonFunction("Set to Triangle Radius", () =>
                                {
                                    shapeable.Polygon.Radius = PolygonShape.TRIANGLE_RADIUS;

                                    PlayerManager.UpdatePlayerModels();
                                }));
                                buttonFunctions.Add(new ButtonFunction("Set to Square Radius", () =>
                                {
                                    shapeable.Polygon.Radius = PolygonShape.SQUARE_RADIUS;

                                    PlayerManager.UpdatePlayerModels();
                                }));
                                buttonFunctions.Add(new ButtonFunction("Set to Normal Radius", () =>
                                {
                                    shapeable.Polygon.Radius = PolygonShape.NORMAL_RADIUS;

                                    PlayerManager.UpdatePlayerModels();
                                }));
                            }

                            EditorContextMenu.inst.ShowContextMenu(buttonFunctions);
                        };

                        var sides = shapeSettings.Find("10/sides").gameObject.GetComponent<InputFieldStorage>();
                        sides.inputField.onValueChanged.ClearAll();
                        sides.inputField.text = shapeable.Polygon.Sides.ToString();
                        sides.inputField.onValueChanged.AddListener(_val =>
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
                        roundness.inputField.onValueChanged.ClearAll();
                        roundness.inputField.text = shapeable.Polygon.Roundness.ToString();
                        roundness.inputField.onValueChanged.AddListener(_val =>
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
                        thickness.inputField.onValueChanged.ClearAll();
                        thickness.inputField.text = shapeable.Polygon.Thickness.ToString();
                        thickness.inputField.onValueChanged.AddListener(_val =>
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
                        thicknessOffsetX.inputField.onValueChanged.ClearAll();
                        thicknessOffsetX.inputField.text = shapeable.Polygon.ThicknessOffset.x.ToString();
                        thicknessOffsetX.inputField.onValueChanged.AddListener(_val =>
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
                        thicknessOffsetY.inputField.onValueChanged.ClearAll();
                        thicknessOffsetY.inputField.text = shapeable.Polygon.ThicknessOffset.y.ToString();
                        thicknessOffsetY.inputField.onValueChanged.AddListener(_val =>
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
                        thicknessScaleX.inputField.onValueChanged.ClearAll();
                        thicknessScaleX.inputField.text = shapeable.Polygon.ThicknessScale.x.ToString();
                        thicknessScaleX.inputField.onValueChanged.AddListener(_val =>
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
                        thicknessScaleY.inputField.onValueChanged.ClearAll();
                        thicknessScaleY.inputField.text = shapeable.Polygon.ThicknessScale.y.ToString();
                        thicknessScaleY.inputField.onValueChanged.AddListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                shapeable.Polygon.ThicknessScale = new Vector2(shapeable.Polygon.ThicknessScale.x, num);

                                PlayerManager.UpdatePlayerModels();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thicknessScaleY);
                        TriggerHelper.AddEventTriggers(thicknessScaleY.inputField.gameObject, TriggerHelper.ScrollDelta(thicknessScaleY.inputField));

                        var slices = shapeSettings.Find("10/slices").gameObject.GetComponent<InputFieldStorage>();
                        slices.inputField.onValueChanged.ClearAll();
                        slices.inputField.text = shapeable.Polygon.Slices.ToString();
                        slices.inputField.onValueChanged.AddListener(_val =>
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

        public void OpenImageSelector(PlayerEditorShape ui, IShapeable shapeable, bool copyFile = true, bool storeImage = false)
        {
            var editorPath = RTFile.RemoveEndSlash(EditorLevelManager.inst.CurrentLevel.path);
            string jpgFile = FileBrowser.OpenSingleFile("Select an image!", editorPath, new string[] { "png", "jpg" });
            SelectImage(jpgFile, ui, shapeable, copyFile: copyFile, storeImage: storeImage);
        }

        public void StoreImage(PlayerEditorShape ui, IShapeable shapeable, string file)
        {
            var currentModel = PlayersData.Current.GetPlayerModel(playerModelIndex);

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

        public int VisibilityToInt(string vis) => vis switch
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

        public string IntToVisibility(int val) => val switch
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

        public void CreateNewModel()
        {
            var playerModel = PlayersData.Current.CreateNewPlayerModel();
            PlayersData.Current.SetPlayerModel(playerModelIndex, playerModel.basePart.id);
            PlayerManager.RespawnPlayers();
            StartCoroutine(RefreshEditor());
            EditorManager.inst.DisplayNotification("Created a new player model!", 1.5f, EditorManager.NotificationType.Success);
        }

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

        public void Reload()
        {
            if (EditorLevelManager.inst.CurrentLevel)
                PlayersData.Load(EditorLevelManager.inst.CurrentLevel.GetFile(Level.PLAYERS_LSB));
            PlayerManager.RespawnPlayers();
            if (Dialog.IsCurrent)
                StartCoroutine(RefreshEditor());
            ModelsPopup.Close();

            EditorManager.inst.DisplayNotification("Loaded player models", 1.5f, EditorManager.NotificationType.Success);
        }

        public Tab CurrentTab { get; set; } = Tab.Base;
    }
}
