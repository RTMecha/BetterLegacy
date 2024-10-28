using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace BetterLegacy.Components
{
    /// <summary>
    /// Component used for Follow Player event keyframe.
    /// </summary>
    public class EventDelayTracker : MonoBehaviour
    {
        bool InHorizontalBounds => tracker.position.x > limitRight && tracker.position.x < limitLeft;
        bool InVerticalBounds => tracker.position.y > limitDown && tracker.position.y < limitUp;

        void Awake() => this.tracker = Creator.NewGameObject("camera track", EventManager.inst.transform).transform;

        void LateUpdate()
        {
            if (!active || !leader || !leader.gameObject.activeSelf || !leader.gameObject.activeInHierarchy)
            {
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.Euler(Vector3.zero);
                return;
            }

            if (tracker != null)
                tracker.position = PlayerManager.CenterOfPlayers();

            var t = (tracker.position + offset * tracker.transform.right) * anchor;

            if (InHorizontalBounds)
                target.x = t.x;
            if (InVerticalBounds)
                target.y = t.y;

            float p = Time.deltaTime * 60f * CoreHelper.ForwardPitch;
            float num = 1f - Mathf.Pow(1f - followSharpness, p);
            if (move)
                transform.localPosition += (target - transform.position) * num;
            else
                transform.localPosition = Vector3.zero;

            if (rotate)
            {
                if (InHorizontalBounds && InVerticalBounds)
                    quaternion = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, 0f, leader.transform.rotation.eulerAngles.z), num);
                transform.localRotation = quaternion;
            }
            else
                transform.localRotation = Quaternion.Euler(Vector3.zero);
        }

        public bool active;
        public bool rotate;
        public bool move;

        Transform tracker;
        public Transform leader;

        public float followSharpness = 0.1f;

        public float offset;

        public Quaternion quaternion;
        public Vector3 target;

        public float anchor = 1f;
        public float limitUp = 99999f;
        public float limitDown = -99999f;
        public float limitLeft = 99999f;
        public float limitRight = -99999f;
    }
}
