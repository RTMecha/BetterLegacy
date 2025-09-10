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
            if (cameraTrack)
            {
                UpdateCameraTrack();
                return;
            }

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

        // port of the Camera Jiggle feature from VG. (idk why its called "camera jiggle" but ok)
        void UpdateCameraTrack()
        {
            prevPos = pos;
            pos = PlayerManager.CenterOfPlayers() / 10f * 0.35f;
            var t = Time.deltaTime * 10f;

            transform.localPosition = new Vector3(RTMath.Lerp(prevPos.x, pos.x, t), RTMath.Lerp(prevPos.y, pos.y, t));
        }

        public bool cameraTrack;

        Vector2 pos;
        Vector2 prevPos;

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
