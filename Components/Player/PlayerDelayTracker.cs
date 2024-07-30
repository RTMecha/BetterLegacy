using BetterLegacy.Core.Helpers;
using UnityEngine;

namespace BetterLegacy.Components.Player
{
    public class PlayerDelayTracker : MonoBehaviour
    {
        public RTPlayer player;

        void LateUpdate()
        {
            if (player != null && leader != null)
            {
                float pitch = CoreHelper.ForwardPitch;

                if (player.rotateMode == RTPlayer.RotateMode.FlipX || player.rotateMode == RTPlayer.RotateMode.RotateFlipX)
                {
                    if (player.lastMovement.x > 0.1f)
                        target = leader.position + offset * leader.transform.right;
                    if (player.lastMovement.x < 0.1f)
                        target = leader.position + -offset * leader.transform.right;
                }
                else
                {
                    target = leader.position + offset * leader.transform.right;
                }

                float p = Time.deltaTime * 60f * pitch;
                float po = 1f - Mathf.Pow(1f - Mathf.Clamp(positionOffset, 0.001f, 1f), p);
                float so = 1f - Mathf.Pow(1f - Mathf.Clamp(scaleOffset, 0.001f, 1f), p);
                float ro = 1f - Mathf.Pow(1f - Mathf.Clamp(rotationOffset, 0.001f, 1f), p);
                if (gameObject.name != "Player" && (!gameObject.name.ToLower().Contains("tail") || player.tailMode == 1))
                {
                    transform.position += (target - transform.position) * po;
                    if (rotationParent)
                        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, 0f, leader.transform.rotation.eulerAngles.z), ro);

                    if (!scaleParent)
                        return;

                    if (gameObject.name.ToLower().Contains("tail") && player.tailMode == 1 && (player.rotateMode == RTPlayer.RotateMode.RotateToDirection || player.rotateMode == RTPlayer.RotateMode.RotateFlipX || player.rotateMode == RTPlayer.RotateMode.RotateFlipY))
                        transform.localScale = Vector3.Lerp(transform.localScale, leader.parent.localScale, so);
                    else
                        transform.localScale = Vector3.Lerp(transform.localScale, leader.localScale, so);
                }
            }
        }

        public Transform leader;

        public float positionOffset = 0.1f;
        public float scaleOffset = 0.1f;
        public float rotationOffset = 0.1f;

        public float offset;

        public Vector3 target;

        public bool scaleParent = false;
        public bool rotationParent = true;
    }
}
