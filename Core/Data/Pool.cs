using System.Collections.Generic;

using UnityEngine;

namespace BetterLegacy.Core.Data
{
    // Thanks Tera for the pooling code
    public class Pool : Data.Exists
    {
        #region Constructors

        public Pool() { }

        public Pool(GameObject prefab, int count)
        {
            this.prefab = prefab;
            InitPool(count);
        }
        
        public Pool(GameObject prefab, Transform parent, int count)
        {
            this.prefab = prefab;
            InitPool(count, parent);
        }

        #endregion

        #region Values

        GameObject prefab;
        Queue<GameObject> pool = new Queue<GameObject>();
        Transform poolParent;

        static Transform mainPoolParent;

        /// <summary>
        /// The default pooled object count.
        /// </summary>
        public const int DEFAULT_POOL_COUNT = 400;

        #endregion

        #region Functions

        GameObject InitPoolObject(PoolPrefab poolPrefab, Transform parent, string name)
        {
            var gameObject = prefab.Duplicate(parent);
            if (!string.IsNullOrEmpty(name))
                gameObject.name = name;
            var poolObject = gameObject.GetOrAddComponent<PoolObject>();
            poolObject.prefab = prefab;
            poolObject.poolPrefab = poolPrefab;
            return gameObject;
        }

        /// <summary>
        /// Initializes a pool based on a prefab.
        /// </summary>
        /// <param name="count">Amount of objects in the pool.</param>
        public void InitPool(int count, Transform parent = null)
        {
            if (!parent && !mainPoolParent)
                mainPoolParent = Creator.NewPersistentGameObject("Pools").transform;
            if (!poolParent)
                poolParent = Creator.NewGameObject(prefab?.name ?? "Pool", parent ?? mainPoolParent).transform;
            var poolPrefab = prefab.GetOrAddComponent<PoolPrefab>();
            poolPrefab.pool = pool;
            for (int i = 0; i < count; i++)
            {
                var gameObject = InitPoolObject(poolPrefab, poolParent, null);
                pool.Enqueue(gameObject);
            }
        }

        /// <summary>
        /// Gets an object from the pool.
        /// </summary>
        /// <param name="parent">Transform to parent the object to.</param>
        /// <returns>If the pool has an object, returns the object from the pool, otherwise returns a new duplicate of the object.</returns>
        public GameObject Get(Transform parent) => Get(parent, null);

        /// <summary>
        /// Gets an object from the pool.
        /// </summary>
        /// <param name="parent">Transform to parent the object to.</param>
        /// <param name="name">Name of the object.</param>
        /// <returns>If the pool has an object, returns the object from the pool, otherwise returns a new duplicate of the object.</returns>
        public GameObject Get(Transform parent, string name)
        {
            if (!pool.IsEmpty())
            {
                var gameObject = pool.Dequeue();
                gameObject.transform.SetParent(parent);
                gameObject.transform.localScale = Vector3.one;
                gameObject.SetActive(true);
                if (!string.IsNullOrEmpty(name))
                    gameObject.name = name;
                return gameObject;
            }

            return InitPoolObject(prefab.GetOrAddComponent<PoolPrefab>(), parent, name).gameObject;
        }

        /// <summary>
        /// Returns an object to the pool.
        /// </summary>
        /// <param name="gameObject">Object to return to the pool.</param>
        public void Return(GameObject gameObject)
        {
            gameObject.SetActive(false);
            gameObject.transform.SetParent(poolParent);
            pool.Enqueue(gameObject);
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
        /// <param name="parent">Parent of the children to return to the pool.</param>
        public void ReturnChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                var obj = child.GetComponent<PoolObject>();
                if (obj && prefab == obj.prefab)
                    Return(child.gameObject);
            }
        }

        #endregion

        #region Sub Classes

        /// <summary>
        /// Pool prefab component.
        /// </summary>
        public class PoolPrefab : MonoBehaviour
        {
            /// <summary>
            /// Pool queue.
            /// </summary>
            public Queue<GameObject> pool;

            /// <summary>
            /// Pool reference.
            /// </summary>
            public Pool poolReference;

            void OnDestroy() => poolReference.prefab = null;
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

        #endregion
    }
}
