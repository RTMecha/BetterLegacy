using System;

using UnityEngine;

using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class InputDetection : ModifierTriggerBase
    {
        #region Constructors

        public InputDetection(DeviceType deviceType, PressType pressType)
        {
            this.deviceType = deviceType;
            this.pressType = pressType;
            Name = deviceType switch
            {
                DeviceType.Keyboard => "keyPress",
                DeviceType.Mouse => "mouseButton",
                DeviceType.Controller => "controlPress",
                _ => null,
            };
            if (pressType != PressType.Press)
                Name += pressType.ToString();
            SetupModifier("0", "True");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Controls;

        readonly DeviceType deviceType;

        readonly PressType pressType;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => deviceType switch
        {
            DeviceType.Keyboard => KeyPress(modifier, modifierLoop),
            DeviceType.Mouse => MouseButton(modifier, modifierLoop),
            DeviceType.Controller => ControlPress(modifier, modifierLoop),
            _ => false,
        };

        bool KeyPress(Modifier modifier, ModifierLoop modifierLoop)
        {
            var keyCode = (KeyCode)modifier.GetInt(0, 0, modifierLoop.variables);
            var list = pressType switch
            {
                PressType.Down => ProjectArrhythmia.Input.keyPressDownOnline,
                PressType.Press => ProjectArrhythmia.Input.keyPressOnline,
                PressType.Up => ProjectArrhythmia.Input.keyPressUpOnline,
                _ => null,
            };
            if (ProjectArrhythmia.State.IsInLobby && list.Contains(keyCode))
                return true;

            var active = pressType switch
            {
                PressType.Down => Input.GetKeyDown(keyCode),
                PressType.Press => Input.GetKey(keyCode),
                PressType.Up => Input.GetKeyUp(keyCode),
                _ => false,
            };
            if (ProjectArrhythmia.State.IsInLobby && active && modifier.GetBool(1, true, modifierLoop.variables))
                switch (pressType)
                {
                    case PressType.Down: {
                            NetworkFunction.KeyPressDown(keyCode);
                            break;
                        }
                    case PressType.Press: {
                            NetworkFunction.KeyPress(keyCode);
                            break;
                        }
                    case PressType.Up: {
                            NetworkFunction.KeyPressUp(keyCode);
                            break;
                        }
                }
            return active;
        }

        bool MouseButton(Modifier modifier, ModifierLoop modifierLoop)
        {
            var button = modifier.GetInt(0, 0, modifierLoop.variables);
            var list = pressType switch
            {
                PressType.Down => ProjectArrhythmia.Input.mouseButtonDownOnline,
                PressType.Press => ProjectArrhythmia.Input.mouseButtonOnline,
                PressType.Up => ProjectArrhythmia.Input.mouseButtonUpOnline,
                _ => null,
            };
            if (ProjectArrhythmia.State.IsInLobby && list.Contains(button))
                return true;

            var active = pressType switch
            {
                PressType.Down => Input.GetMouseButtonDown(button),
                PressType.Press => Input.GetMouseButton(button),
                PressType.Up => Input.GetMouseButtonUp(button),
                _ => false,
            };
            if (ProjectArrhythmia.State.IsInLobby && active && modifier.GetBool(1, true, modifierLoop.variables))
                switch (pressType)
                {
                    case PressType.Down: {
                            NetworkFunction.MouseButtonDown(button);
                            break;
                        }
                    case PressType.Press: {
                            NetworkFunction.MouseButton(button);
                            break;
                        }
                    case PressType.Up: {
                            NetworkFunction.MouseButtonUp(button);
                            break;
                        }
                }
            return active;
        }

        bool ControlPress(Modifier modifier, ModifierLoop modifierLoop)
        {
            var type = (PlayerInputControlType)modifier.GetInt(0, 0, modifierLoop.variables);
            var list = pressType switch
            {
                PressType.Down => ProjectArrhythmia.Input.controlPressDownOnline,
                PressType.Press => ProjectArrhythmia.Input.controlPressOnline,
                PressType.Up => ProjectArrhythmia.Input.controlPressUpOnline,
                _ => null,
            };
            if (ProjectArrhythmia.State.IsInLobby && list.Contains(type))
                return true;

            var transformable = modifierLoop.reference.AsTransformable();
            var player = modifierLoop.reference is PAPlayer p ? p : PlayerManager.GetClosestPlayer(transformable?.GetFullPosition() ?? Vector3.zero);
            var device = player?.device ?? InControl.InputManager.ActiveDevice;

            if (device == null)
                return false;

            if (Enum.TryParse(type.ToString(), out InControl.InputControlType inputControlType))
            {
                var active = pressType switch
                {
                    PressType.Down => device.GetControl(inputControlType).WasPressed,
                    PressType.Press => device.GetControl(inputControlType).IsPressed,
                    PressType.Up => device.GetControl(inputControlType).WasReleased,
                    _ => false,
                };
                if (ProjectArrhythmia.State.IsInLobby && active && modifier.GetBool(1, true, modifierLoop.variables))
                    switch (pressType)
                    {
                        case PressType.Down: {
                                NetworkFunction.ControlPressDown(inputControlType);
                                break;
                            }
                        case PressType.Press: {
                                NetworkFunction.ControlPress(inputControlType);
                                break;
                            }
                        case PressType.Up: {
                                NetworkFunction.ControlPressUp(inputControlType);
                                break;
                            }
                    }
                return active;
            }
            return false;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            switch (deviceType)
            {
                case DeviceType.Keyboard: {
                        var dropdownData = CoreHelper.ToDropdownData<KeyCode>();
                        modifierCard.DropdownGenerator(modifier, reference, "Key", 0, dropdownData.Key, dropdownData.Value);
                        break;
                    }
                case DeviceType.Mouse: {
                        modifierCard.IntegerGenerator(modifier, reference, "Button", 0);
                        break;
                    }
                case DeviceType.Controller: {
                        var dropdownData = CoreHelper.ToDropdownData<PlayerInputControlType>();
                        modifierCard.DropdownGenerator(modifier, reference, "Button", 0, dropdownData.Key, dropdownData.Value);
                        break;
                    }
            }
            modifierCard.BoolGenerator(modifier, reference, "Sync To Lobby", 1);
        }

        #endregion

        #region Sub Classes

        public enum DeviceType
        {
            Keyboard,
            Mouse,
            Controller,
        }

        public enum PressType
        {
            Down,
            Press,
            Up,
        }

        #endregion
    }
}
