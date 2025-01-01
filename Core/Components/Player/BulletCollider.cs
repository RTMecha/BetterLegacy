
using DG.Tweening;
using UnityEngine;

namespace BetterLegacy.Core.Components.Player
{
    /// <summary>
    /// Detects when the bullet hits something and when it does, kill the bullet.
    /// </summary>
    public class BulletCollider : MonoBehaviour
    {
        public RTPlayer.PlayerObject playerObject;
        public RTPlayer player;
        public Rigidbody2D rb;
        public bool kill = false;
        public Tweener tweener;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.transform.parent.parent.name == player.name || !kill)
                return;

            tweener.Kill();

            player.boosts.Remove(playerObject);
            playerObject = null;
            Destroy(transform.parent.gameObject);
        }

        void OnCollisionEnter2D()
        {
            if (!kill)
                return;

            tweener.Kill();

            player.boosts.Remove(playerObject);
            playerObject = null;
            Destroy(transform.parent.gameObject);
        }
    }
}
