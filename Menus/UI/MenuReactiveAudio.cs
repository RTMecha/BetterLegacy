using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using BetterLegacy.Core;

namespace BetterLegacy.Menus.UI
{
    public class MenuReactiveAudio : MonoBehaviour
    {
        public ReactiveSetting reactiveSetting;

        public Vector2 ogPosition;
        public Vector3 ogScale = new Vector3(1f, 1f, 1f);

        void Update()
        {
            switch (reactiveSetting.controls)
            {
                case ReactiveSetting.ControlType.Position:
                    {
                        float x = InterfaceManager.inst.samples[reactiveSetting.channels[0]] * reactiveSetting.intensity[0];
                        float y = InterfaceManager.inst.samples[reactiveSetting.channels[1]] * reactiveSetting.intensity[1];

                        gameObject.transform.localPosition = ogPosition + new Vector2(x, y);

                        break;
                    }
                case ReactiveSetting.ControlType.Scale:
                    {
                        float x = InterfaceManager.inst.samples[reactiveSetting.channels[0]] * reactiveSetting.intensity[0];
                        float y = InterfaceManager.inst.samples[reactiveSetting.channels[1]] * reactiveSetting.intensity[1];

                        gameObject.transform.localScale = ogScale + new Vector3(x, y, 0f);

                        break;
                    }
                case ReactiveSetting.ControlType.Rotation:
                    {
                        float x = InterfaceManager.inst.samples[reactiveSetting.channels[0]] * reactiveSetting.intensity[0];

                        gameObject.transform.SetLocalRotationEulerZ(x);

                        break;
                    }
            }
        }
    }
}
