using System.Linq;

using UnityEngine;

namespace BetterLegacy.Core.Components.Player
{
    /// <summary>
    /// Stores raycast collision information for the player.
    /// </summary>
    public struct PlayerCastCollision
    {
        #region Values

        /// <summary>
        /// Player reference.
        /// </summary>
        public RTPlayer player;

        /// <summary>
        /// If the player is colliding with colliders with <see cref="Collider2D.isTrigger"/> on. (Damage)
        /// </summary>
        public bool triggerColliding;

        /// <summary>
        /// If the player is colliding with colliders with <see cref="Collider2D.isTrigger"/> off. (Solid)
        /// </summary>
        public bool solidColliding;

        /// <summary>
        /// Left raycast.
        /// </summary>
        public RaycastHit2D[] leftCasts;

        /// <summary>
        /// Right raycast.
        /// </summary>
        public RaycastHit2D[] rightCasts;

        /// <summary>
        /// Up raycast.
        /// </summary>
        public RaycastHit2D[] upCasts;

        /// <summary>
        /// Down raycast.
        /// </summary>
        public RaycastHit2D[] downCasts;

        /// <summary>
        /// All raycasts.
        /// </summary>
        public RaycastHit2D[] All { get; set; }

        /// <summary>
        /// Detected raycast.
        /// </summary>
        public RaycastHit2D Cast
        {
            get
            {
                if (TryGetCast(leftCasts, out RaycastHit2D leftCast))
                    return leftCast;
                if (TryGetCast(rightCasts, out RaycastHit2D rightCast))
                    return rightCast;
                if (TryGetCast(upCasts, out RaycastHit2D upCast))
                    return upCast;
                if (TryGetCast(downCasts, out RaycastHit2D downCast))
                    return downCast;
                return default;
            }
        }

        /// <summary>
        /// The current collider.
        /// </summary>
        public Collider2D Collider => Cast.collider;

        #endregion

        #region Functions

        /// <summary>
        /// Tries to find a valid raycast.
        /// </summary>
        /// <param name="raycastHits">Array of raycasts.</param>
        /// <param name="cast">Raycast result.</param>
        /// <returns>Returns <see langword="true"/> if a raycast is found with a valid collider, otherwise returns <see langword="false"/>.</returns>
        public bool TryGetCast(RaycastHit2D[] raycastHits, out RaycastHit2D cast)
        {
            for (int i = 0; i < raycastHits.Length; i++)
            {
                cast = raycastHits[i];
                if (cast.collider)
                    return true;
            }
            cast = default;
            return false;
        }

        /// <summary>
        /// Checks if any raycasts hit a solid collision.
        /// </summary>
        /// <param name="raycastHits">Array of raycasts.</param>
        /// <returns>Returns <see langword="true"/> if a raycast hit a solid collider, otherwise returns <see langword="false"/>.</returns>
        public bool AnySolid(RaycastHit2D[] raycastHits)
        {
            var collider2D = player.CurrentCollider;
            for (int i = 0; i < raycastHits.Length; i++)
            {
                var cast = raycastHits[i];
                if (cast.collider && cast.collider != collider2D && !cast.collider.isTrigger)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if any raycasts hit a trigger collision.
        /// </summary>
        /// <param name="raycastHits">Array of raycasts.</param>
        /// <returns>Returns <see langword="true"/> if a raycast hit a trigger collider, otherwise returns <see langword="false"/>.</returns>
        public bool AnyTrigger(RaycastHit2D[] raycastHits)
        {
            var collider2D = player.CurrentCollider;
            for (int i = 0; i < raycastHits.Length; i++)
            {
                var cast = raycastHits[i];
                if (cast.collider && cast.collider != collider2D && cast.collider.isTrigger)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if any raycasts hit a solid collision.
        /// </summary>
        /// <param name="raycastHits">Array of raycasts.</param>
        /// <param name="cast">Raycast result.</param>
        /// <returns>Returns <see langword="true"/> if a raycast hit a solid collider, otherwise returns <see langword="false"/>.</returns>
        public bool AnySolid(RaycastHit2D[] raycastHits, out RaycastHit2D cast)
        {
            var collider2D = player.CurrentCollider;
            for (int i = 0; i < raycastHits.Length; i++)
            {
                cast = raycastHits[i];
                if (cast.collider && cast.collider != collider2D && !cast.collider.isTrigger)
                    return true;
            }
            cast = default;
            return false;
        }

        /// <summary>
        /// Checks if any raycasts hit a trigger collision.
        /// </summary>
        /// <param name="raycastHits">Array of raycasts.</param>
        /// <param name="cast">Raycast result.</param>
        /// <returns>Returns <see langword="true"/> if a raycast hit a trigger collider, otherwise returns <see langword="false"/>.</returns>
        public bool AnyTrigger(RaycastHit2D[] raycastHits, out RaycastHit2D cast)
        {
            var collider2D = player.CurrentCollider;
            for (int i = 0; i < raycastHits.Length; i++)
            {
                cast = raycastHits[i];
                if (cast.collider && cast.collider != collider2D && cast.collider.isTrigger)
                    return true;
            }
            cast = default;
            return false;
        }

        /// <summary>
        /// Gets all raycasts.
        /// </summary>
        /// <returns>Returns an array that combines all raycast arrays.</returns>
        public RaycastHit2D[] GetAll()
        {
            var collider2D = player.CurrentCollider;
            var collection = leftCasts.Where(x => x.collider != collider2D);
            if (!rightCasts.IsEmpty())
                collection = collection.Union(rightCasts.Where(x => x.collider != collider2D));
            if (!upCasts.IsEmpty())
                collection = collection.Union(upCasts.Where(x => x.collider != collider2D));
            if (!downCasts.IsEmpty())
                collection = collection.Union(downCasts.Where(x => x.collider != collider2D));
            return collection.ToArray();
        }

        #endregion
    }
}
