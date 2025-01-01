﻿using UnityEngine;

namespace BetterLegacy.Core.Components.Player
{
    /// <summary>
    /// Component for a bullet shot from a player.
    /// </summary>
    public class Bullet : MonoBehaviour
    {
        public void Assign()
        {
            if (!player)
                return;
            if (player.lastMovement.x > 0f)
                lastMove = true;
            if (player.lastMovement.x < 0f)
                lastMove = false;
        }

        void Update()
        {
            if (!player)
                return;

            if (player.rotateMode != RTPlayer.RotateMode.FlipX)
            {
                transform.position += transform.right * speed * 0.1f * AudioManager.inst.pitch;
                return;
            }
            if (lastMove)
                transform.position += transform.right * speed * 0.1f * AudioManager.inst.pitch;
            else
                transform.position -= transform.right * speed * 0.1f * AudioManager.inst.pitch;
        }

        /// <summary>
        /// Speed of the bullet.
        /// </summary>
        public float speed = 1f;

        bool lastMove;

        public RTPlayer player;
    }
}