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

        void OnCollisionEnter2D()
        {
            if (player)
                player.colliding = true;
        }
    }
}
