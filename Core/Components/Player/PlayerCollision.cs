using UnityEngine;

namespace BetterLegacy.Core.Components.Player
{
    /// <summary>
    /// Handles the players' collision.
    /// </summary>
    public class PlayerCollision : MonoBehaviour
    {
        public RTPlayer player;

        void OnTriggerEnter2D(Collider2D other) => player?.HandleCollision(other);

        void OnTriggerEnter(Collider other) => player?.HandleCollision(other);

        void OnTriggerStay2D(Collider2D other) => player?.HandleCollision(other, false);

        void OnTriggerStay(Collider other) => player?.HandleCollision(other, false);

        void OnTriggerExit2D(Collider2D other)
        {
            if (player)
                player.triggerColliding = false;
        }
        
        void OnTriggerExit(Collider other)
        {
            if (player)
                player.triggerColliding = false;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (!player)
                return;
            
            player.colliding = true;
            player.cachedCollision = collision;
            player.currentCollision = collision;
        }

        void OnCollisionStay2D(Collision2D collision)
        {
            if (!player)
                return;

            player.cachedCollision = collision;
            player.currentCollision = collision;
        }

        void OnCollisionExit2D()
        {
            if (player)
                player.currentCollision = null;
        }
    }
}
