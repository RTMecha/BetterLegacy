
using HarmonyLib;
using InControl;
using UnityEngine;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(MyGameActions))]
    public class MyGameActionsPatch : MonoBehaviour
    {
        [HarmonyPatch("CreateWithJoystickBindings")]
        [HarmonyPrefix]
        private static bool CreateWithJoystickBindingsPatch(ref MyGameActions __result)
        {
            __result = CreateWithJoystickBindings();
            return false;
        }

        public static MyGameActions CreateWithJoystickBindings()
        {
            MyGameActions myGameActions = new MyGameActions();
            myGameActions.Up.AddDefaultBinding(InputControlType.DPadUp);
            myGameActions.Up.AddDefaultBinding(InputControlType.LeftStickUp);
            myGameActions.Down.AddDefaultBinding(InputControlType.DPadDown);
            myGameActions.Down.AddDefaultBinding(InputControlType.LeftStickDown);
            myGameActions.Left.AddDefaultBinding(InputControlType.DPadLeft);
            myGameActions.Left.AddDefaultBinding(InputControlType.LeftStickLeft);
            myGameActions.Right.AddDefaultBinding(InputControlType.DPadRight);
            myGameActions.Right.AddDefaultBinding(InputControlType.LeftStickRight);
            myGameActions.Boost.AddDefaultBinding(InputControlType.Action1);
            myGameActions.Boost.AddDefaultBinding(InputControlType.Action3);
            myGameActions.Join.AddDefaultBinding(InputControlType.Action1);
            myGameActions.Join.AddDefaultBinding(InputControlType.Action2);
            myGameActions.Join.AddDefaultBinding(InputControlType.Action3);
            myGameActions.Join.AddDefaultBinding(InputControlType.Action4);
            myGameActions.Pause.AddDefaultBinding(InputControlType.Command);
            myGameActions.Escape.AddDefaultBinding(InputControlType.Action2);
            myGameActions.Escape.AddDefaultBinding(InputControlType.Action4);
            return myGameActions;
        }

        [HarmonyPatch("CreateWithKeyboardBindings")]
        [HarmonyPrefix]
        private static bool CreateWithKeyboardBindingsPatch(ref MyGameActions __result, int __0 = -1)
        {
            __result = CreateWithKeyboardBindings(__0);
            return false;
        }

        public static MyGameActions CreateWithKeyboardBindings(int _playerIndex = -1)
        {
            MyGameActions myGameActions = new MyGameActions();
            if (_playerIndex == -1)
            {
                myGameActions.Up.AddDefaultBinding(new Key[]
                {
                    Key.UpArrow
                });
                myGameActions.Up.AddDefaultBinding(new Key[]
                {
                    Key.W
                });
                myGameActions.Down.AddDefaultBinding(new Key[]
                {
                    Key.DownArrow
                });
                myGameActions.Down.AddDefaultBinding(new Key[]
                {
                    Key.S
                });
                myGameActions.Left.AddDefaultBinding(new Key[]
                {
                    Key.LeftArrow
                });
                myGameActions.Left.AddDefaultBinding(new Key[]
                {
                    Key.A
                });
                myGameActions.Right.AddDefaultBinding(new Key[]
                {
                    Key.RightArrow
                });
                myGameActions.Right.AddDefaultBinding(new Key[]
                {
                    Key.D
                });
                myGameActions.Boost.AddDefaultBinding(new Key[]
                {
                    Key.Space
                });
                myGameActions.Boost.AddDefaultBinding(new Key[]
                {
                    Key.Return
                });
                myGameActions.Join.AddDefaultBinding(new Key[]
                {
                    Key.Space
                });
                myGameActions.Join.AddDefaultBinding(new Key[]
                {
                    Key.A
                });
                myGameActions.Join.AddDefaultBinding(new Key[]
                {
                    Key.S
                });
                myGameActions.Join.AddDefaultBinding(new Key[]
                {
                    Key.D
                });
                myGameActions.Join.AddDefaultBinding(new Key[]
                {
                    Key.W
                });
                myGameActions.Join.AddDefaultBinding(new Key[]
                {
                    Key.LeftArrow
                });
                myGameActions.Join.AddDefaultBinding(new Key[]
                {
                    Key.RightArrow
                });
                myGameActions.Join.AddDefaultBinding(new Key[]
                {
                    Key.DownArrow
                });
                myGameActions.Join.AddDefaultBinding(new Key[]
                {
                    Key.UpArrow
                });
                myGameActions.Pause.AddDefaultBinding(new Key[]
                {
                    Key.Escape
                });
                myGameActions.Escape.AddDefaultBinding(new Key[]
                {
                    Key.Escape
                });
            }
            else if (_playerIndex == 0)
            {
                myGameActions.Up.AddDefaultBinding(new Key[]
                {
                    Key.W
                });
                myGameActions.Down.AddDefaultBinding(new Key[]
                {
                    Key.S
                });
                myGameActions.Left.AddDefaultBinding(new Key[]
                {
                    Key.A
                });
                myGameActions.Right.AddDefaultBinding(new Key[]
                {
                    Key.D
                });
                myGameActions.Boost.AddDefaultBinding(new Key[]
                {
                    Key.Space
                });
                myGameActions.Join.AddDefaultBinding(new Key[]
                {
                    Key.Space
                });
                myGameActions.Pause.AddDefaultBinding(new Key[]
                {
                    Key.Escape
                });
                myGameActions.Escape.AddDefaultBinding(new Key[]
                {
                    Key.Escape
                });
            }
            else if (_playerIndex == 0)
            {
                myGameActions.Up.AddDefaultBinding(new Key[]
                {
                    Key.UpArrow
                });
                myGameActions.Down.AddDefaultBinding(new Key[]
                {
                    Key.DownArrow
                });
                myGameActions.Left.AddDefaultBinding(new Key[]
                {
                    Key.LeftArrow
                });
                myGameActions.Right.AddDefaultBinding(new Key[]
                {
                    Key.RightArrow
                });
                myGameActions.Pause.AddDefaultBinding(new Key[]
                {
                    Key.Escape
                });
                myGameActions.Escape.AddDefaultBinding(new Key[]
                {
                    Key.Escape
                });
            }
            return myGameActions;
        }
    }
}
