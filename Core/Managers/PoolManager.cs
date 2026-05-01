using System.Collections.Generic;

using UnityEngine;

using BetterLegacy.Core.Managers.Settings;

namespace BetterLegacy.Core.Managers
{
    // Thanks Tera for the pooling code
    /// <summary>
    /// Manages pools.
    /// </summary>
    public class PoolManager : BaseManager<PoolManager, ManagerSettings>
    {
        #region Values

        Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();
        Transform poolParent;

        /// <summary>
        /// The default pooled object count.
        /// </summary>
        public const int DEFAULT_POOL_COUNT = 400;

        #endregion

        #region Functions

        GameObject InitPoolObject(GameObject prefab, PoolPrefab poolPrefab, Transform parent)
        {
            var gameObject = prefab.Duplicate(parent);
            var poolObject = gameObject.GetOrAddComponent<PoolObject>();
            poolObject.prefab = prefab;
            poolObject.poolPrefab = poolPrefab;
            return gameObject;
        }

        /// <summary>
        /// Initializes a pool based on a prefab.
        /// </summary>
        /// <param name="prefab">Prefab to create a pool for.</param>
        public void InitPool(GameObject prefab) => InitPool(prefab, DEFAULT_POOL_COUNT);
        
        /// <summary>
        /// Initializes a pool based on a prefab.
        /// </summary>
        /// <param name="prefab">Prefab to create a pool for.</param>
        /// <param name="count">Amount of objects in the pool.</param>
        public void InitPool(GameObject prefab, int count)
        {
            if (!pools.TryGetValue(prefab, out Queue<GameObject> pool))
            {
                pool = new Queue<GameObject>();
                pools[prefab] = pool;
            }
            var poolPrefab = prefab.GetOrAddComponent<PoolPrefab>();
            poolPrefab.pool = pool;
            for (int i = 0; i < count; i++)
            {
                var gameObject = InitPoolObject(prefab, poolPrefab, poolParent);
                pool.Enqueue(gameObject);
            }
        }

        /// <summary>
        /// Gets an object from the pool.
        /// </summary>
        /// <param name="prefab">Prefab to get from the pool or duplicate.</param>
        /// <param name="parent">Transform to parent the object to.</param>
        /// <returns>If the pool has an object, returns the object from the pool, otherwise returns a new duplicate of the object.</returns>
        public GameObject Get(GameObject prefab, Transform parent)
        {
            if (!pools.TryGetValue(prefab, out Queue<GameObject> pool))
            {
                pool = new Queue<GameObject>();
                pools[prefab] = pool;
            }

            if (!pool.IsEmpty())
            {
                var gameObject = pool.Dequeue();
                gameObject.transform.SetParent(parent);
                gameObject.SetActive(true);
                return gameObject;
            }

            return InitPoolObject(prefab, prefab.GetOrAddComponent<PoolPrefab>(), parent).gameObject;
        }

        /// <summary>
        /// Returns an object to the pool.
        /// </summary>
        /// <param name="prefab">Prefab of <paramref name="gameObject"/>.</param>
        /// <param name="gameObject">Object to return to the pool.</param>
        public void Return(GameObject prefab, GameObject gameObject)
        {
            gameObject.SetActive(false);
            gameObject.transform.SetParent(poolParent);
            pools[prefab].Enqueue(gameObject);
        }

        /// <summary>
        /// Returns an object to the pool.
        /// </summary>
        /// <param name="poolObject">Object to return to the pool.</param>
        public void Return(PoolObject poolObject)
        {
            poolObject.gameObject.SetActive(false);
            poolObject.transform.SetParent(poolParent);
            poolObject.poolPrefab.pool.Enqueue(poolObject.gameObject);
            
        }

        /// <summary>
        /// Returns all children of an object to the pool.
        /// </summary>
        /// <param name="prefab">Prefab of the </param>
        /// <param name="parent">Parent of the children to return to the pool.</param>
        public void ReturnChildren(GameObject prefab, Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                var obj = child.GetComponent<PoolObject>();
                if (prefab == obj.prefab)
                    Return(prefab, child.gameObject);
            }
        }

        #endregion

        /// <summary>
        /// Pool prefab component.
        /// </summary>
        public class PoolPrefab : MonoBehaviour
        {
            /// <summary>
            /// Pool queue.
            /// </summary>
            public Queue<GameObject> pool;

            void OnDestroy() => inst.pools.Remove(gameObject);
        }

        /// <summary>
        /// Pool object component.
        /// </summary>
        public class PoolObject : MonoBehaviour
        {
            /// <summary>
            /// Pool prefab reference.
            /// </summary>
            public PoolPrefab poolPrefab;

            /// <summary>
            /// Prefab of the object.
            /// </summary>
            public GameObject prefab;
        }
    }
}
