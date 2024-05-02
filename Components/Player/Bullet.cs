using UnityEngine;

namespace BetterLegacy.Components.Player
{
    public class Bullet : MonoBehaviour
    {
        void Awake()
        {

        }

        public void Assign()
        {
            if (player != null)
            {
                if (player.lastMovement.x > 0f)
                    lastMove = true;
                if (player.lastMovement.x < 0f)
                    lastMove = false;
            }
        }

        void Update()
        {
            if (player != null)
                if (player.rotateMode != RTPlayer.RotateMode.FlipX)
                    transform.position += transform.right * speed * 0.1f * AudioManager.inst.pitch;
                else
                {
                    if (lastMove)
                        transform.position += transform.right * speed * 0.1f * AudioManager.inst.pitch;
                    else
                        transform.position -= transform.right * speed * 0.1f * AudioManager.inst.pitch;
                }
        }

        public float speed = 1f;

        bool lastMove;

        public RTPlayer player;
    }
}
