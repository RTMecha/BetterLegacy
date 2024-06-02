
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

        void OnTriggerExit2D(Collider2D other) => player?.OnObjectCollisionExit(other);
        void OnTriggerExit(Collider other) => player?.OnObjectCollisionExit(other);
    }
}
