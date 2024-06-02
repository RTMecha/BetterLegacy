
using BetterLegacy.Core.Helpers;
using UnityEngine;

namespace BetterLegacy.Components.Player
{
    public class PlayerCollision : MonoBehaviour
    {
        public RTPlayer player;

        void OnTriggerEnter2D(Collider2D other) => player?.OnObjectCollisionEnter(other);

        void OnTriggerEnter(Collider other) => player?.OnObjectCollisionEnter(other);

        void OnTriggerStay2D(Collider2D other) => player?.OnObjectCollisionStay(other);

        void OnTriggerStay(Collider other) => player?.OnObjectCollisionStay(other);

        void OnCollisionEnter2D()
        {
            if (player)
                player.colliding = true;
        }
    }
}
