using UnityEngine;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;

namespace BetterLegacy.Core.Components
{
    /// <summary>
    /// Component used for Follow Player event keyframe.
    /// </summary>
    public class EventDelayTracker : MonoBehaviour
    {
        bool InHorizontalBounds => tracker.position.x > limitRight && tracker.position.x < limitLeft;
        bool InVerticalBounds => tracker.position.y > limitDown && tracker.position.y < limitUp;

        void Awake() => tracker = Creator.NewGameObject("camera track", EventManager.inst.transform).transform;

        void LateUpdate()
        {
            if (!active || !GameManager.inst.players.activeSelf || !GameManager.inst.players.activeInHierarchy)
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
                if (InHorizontalBounds && InVerticalBounds && !PlayerManager.Players.IsEmpty() && PlayerManager.Players[0].RuntimePlayer)
                    quaternion = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, 0f, PlayerManager.Players[0].RuntimePlayer.rb.transform.eulerAngles.z), num);
                transform.localRotation = quaternion;
            }
            else
                transform.localRotation = Quaternion.Euler(Vector3.zero);
        }

        public bool active;
        public bool rotate;
        public bool move;

        Transform tracker;

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
