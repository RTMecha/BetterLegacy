using BetterLegacy.Core;
using System.Collections.Generic;
using UnityEngine;

namespace BetterLegacy.Components
{
    public class EventDelayTracker : MonoBehaviour
    {
        bool InHorizontalBounds
        {
            get
            {
                return tracker.position.x > limitRight && tracker.position.x < limitLeft;
            }
        }
        bool InVerticalBounds
        {
            get
            {
                return tracker.position.y > limitDown && tracker.position.y < limitUp;
            }
        }

        void Awake()
        {
            var tracker = new GameObject("track");
            this.tracker = tracker.transform;
        }

        Vector2 PlayerCenter()
        {
            var list = new List<Vector3>();

            for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
            {
                if (GameManager.inst.players.transform.TryFind("Player " + (i + 1).ToString(), out Transform result))
                    list.Add(result.Find("Player").position);
            }

            return RTMath.CenterOfVectors(list);
        }

        void LateUpdate()
        {
            if (active && leader != null && leader.gameObject.activeSelf && leader.gameObject.activeInHierarchy)
            {
                if (tracker != null)
                    tracker.position = PlayerCenter();

                float pitch = AudioManager.inst.CurrentAudioSource.pitch;
                if (pitch < 0f)
                    pitch = -pitch;
                if (pitch == 0f)
                    pitch = 0.001f;

                var t = (tracker.position + offset * tracker.transform.right) * anchor;

                if (InHorizontalBounds)
                    target.x = t.x;
                if (InVerticalBounds)
                    target.y = t.y;

                float p = Time.deltaTime * 60f * pitch;
                float num = 1f - Mathf.Pow(1f - followSharpness, p);
                if (move)
                {
                    transform.localPosition += (target - transform.position) * num;
                }
                else
                {
                    transform.localPosition = Vector3.zero;
                }
                if (rotate)
                {
                    if (InHorizontalBounds && InVerticalBounds)
                    {
                        quaternion = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, 0f, leader.transform.rotation.eulerAngles.z), num);
                    }
                    transform.localRotation = quaternion;
                }
                else
                    transform.localRotation = Quaternion.Euler(Vector3.zero);
            }
            else
            {
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.Euler(Vector3.zero);
            }
        }

        private void OnDrawGizmos()
        {
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
