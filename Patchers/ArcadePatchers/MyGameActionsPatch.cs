using UnityEngine;

using HarmonyLib;

using InControl;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(MyGameActions))]
    public class MyGameActionsPatch : MonoBehaviour
    {
        [HarmonyPatch(nameof(MyGameActions.CreateWithJoystickBindings))]
        [HarmonyPrefix]
        static bool CreateWithJoystickBindingsPrefix(ref MyGameActions __result)
        {
            var myGameActions = new MyGameActions();

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

            __result = myGameActions;
            return false;
        }

        [HarmonyPatch(nameof(MyGameActions.CreateWithKeyboardBindings))]
        [HarmonyPrefix]
        static bool CreateWithKeyboardBindingsPrefix(ref MyGameActions __result, int __0 = -1)
        {
            var myGameActions = new MyGameActions();

            if (__0 == -1)
            {
                myGameActions.Up.AddDefaultBinding(Key.UpArrow);
                myGameActions.Up.AddDefaultBinding(Key.W);
                myGameActions.Down.AddDefaultBinding(Key.DownArrow);
                myGameActions.Down.AddDefaultBinding(Key.S);
                myGameActions.Left.AddDefaultBinding(Key.LeftArrow);
                myGameActions.Left.AddDefaultBinding(Key.A);
                myGameActions.Right.AddDefaultBinding(Key.RightArrow);
                myGameActions.Right.AddDefaultBinding(Key.D);
                myGameActions.Boost.AddDefaultBinding(Key.Space);
                myGameActions.Boost.AddDefaultBinding(Key.Return);
                myGameActions.Join.AddDefaultBinding(Key.Space);
                myGameActions.Join.AddDefaultBinding(Key.A);
                myGameActions.Join.AddDefaultBinding(Key.S);
                myGameActions.Join.AddDefaultBinding(Key.D);
                myGameActions.Join.AddDefaultBinding(Key.W);
                myGameActions.Join.AddDefaultBinding(Key.LeftArrow);
                myGameActions.Join.AddDefaultBinding(Key.RightArrow);
                myGameActions.Join.AddDefaultBinding(Key.DownArrow);
                myGameActions.Join.AddDefaultBinding(Key.UpArrow);
                myGameActions.Pause.AddDefaultBinding(Key.Escape);
                myGameActions.Escape.AddDefaultBinding(Key.Escape);
            }
            else if (__0 == 0)
            {
                myGameActions.Up.AddDefaultBinding(Key.W);
                myGameActions.Down.AddDefaultBinding(Key.S);
                myGameActions.Left.AddDefaultBinding(Key.A);
                myGameActions.Right.AddDefaultBinding(Key.D);
                myGameActions.Boost.AddDefaultBinding(Key.Space);
                myGameActions.Join.AddDefaultBinding(Key.Space);
                myGameActions.Pause.AddDefaultBinding(Key.Escape);
                myGameActions.Escape.AddDefaultBinding(Key.Escape);
            }
            else if (__0 == 1)
            {
                myGameActions.Up.AddDefaultBinding(Key.UpArrow);
                myGameActions.Down.AddDefaultBinding(Key.DownArrow);
                myGameActions.Left.AddDefaultBinding(Key.LeftArrow);
                myGameActions.Right.AddDefaultBinding(Key.RightArrow);
                myGameActions.Pause.AddDefaultBinding(Key.Escape);
                myGameActions.Escape.AddDefaultBinding(Key.Escape);
            }

            __result = myGameActions;
            return false;
        }
    }
}
