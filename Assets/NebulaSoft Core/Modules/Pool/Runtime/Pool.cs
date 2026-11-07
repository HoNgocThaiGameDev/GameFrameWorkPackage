using UnityEngine;
using System.Collections.Generic;

namespace NebulaSoft
{
    /// <summary>
    /// Basic pool class. Contains pool settings and references to pooled objects.
    /// </summary>
    [System.Serializable]
    public sealed class Pool : IPool
    {
        [SerializeField] GameObject prefab = null;
        [SerializeField] Transform objectsContainer = null;
        [SerializeField] string name;

        [SerializeField] bool capSize = false;
        [SerializeField] int maxSize = 10;

        public string Name => name;
        public GameObject Prefab => prefab;
        public int MaxSize => maxSize;
        public bool CapSize => capSize;

        public Transform ObjectsContainer => PoolManager.GetContainer(objectsContainer);

        private List<GameObject> pooledObjects;
        private bool inited = false;

        public Pool(GameObject prefab)
        {
            this.prefab = prefab;

            name = prefab.name;

            Init();
        }

        public Pool(GameObject prefab, Transform objectsContainer)
        {
            this.prefab = prefab;
            this.objectsContainer = objectsContainer;

            name = prefab.name;

            Init();
        }

        public Pool(GameObject prefab, string name)
        {
            this.prefab = prefab;
            this.name = name;

            Init();
        }

        public Pool(GameObject prefab, string name, Transform objectsContainer)
        {
            this.name = name;
            this.prefab = prefab;
            this.objectsContainer = objectsContainer;

            Init();
        }

        /// <summary>
        /// Initializes pool.
        /// </summary>
        public void Init()
        {
            if (inited) return;

            if (string.IsNullOrEmpty(name))
            {
                Debug.LogError("[Pool]: Pool initialization failed. A unique name (ID) must be provided. Please ensure the 'name' field is not empty before proceeding.");

                return;
            }

            if (PoolManager.HasPool(name))
            {
                Debug.LogError(string.Format("[Pool]: Pool initialization failed. A pool with the name '{0}' already exists in the PoolManager. Please use a unique name for each pool to avoid conflicts.", name));

                return;
            }

            if (prefab == null)
            {
                Debug.LogError(string.Format("[Pool] Pool initialization failed. There's no attached prefab at pool: {0}.", name));

                return;
            }

            pooledObjects = new List<GameObject>();

            PoolManager.AddPool(this);

            inited = true;
        }

        /// <summary>
        /// Returns reference to pooled object if it's currently available.
        /// </summary>
        /// <param name="activateObject">If true object will be set as active.</param>
        /// <returns>Pooled object or null if there is no available objects and new one can not be created.</returns>
        public GameObject GetPooledObject()
        {
            if (!inited)
                Init();

            if (!inited) return null;
            pooledObjects.RemoveAll(value => value == null);

            for (int i = 0; i < pooledObjects.Count; i++)
            {
                GameObject pooledObject = pooledObjects[i];

                if (pooledObject == null)
                {
                    Debug.LogError(string.Format("[Pool]: A pooled object ({0}) was destroyed externally. This may indicate that an object was not properly returned to the pool, or its parent object was destroyed. Please review your object management logic to prevent unintended object destruction.", name));

                    continue;
                }

                if (!pooledObject.activeSelf)
                {
                    pooledObject.SetActive(true);

                    return pooledObject;
                }
            }

            if (!capSize || pooledObjects.Count < maxSize)
            {
                return AddObjectToPool(true);
            }

            return null;
        }

        public T GetPooledComponent<T>() where T : Component
        {
            GameObject pooledObject = GetPooledObject();
            if (pooledObject != null)
            {
                return pooledObject.GetComponent<T>();
            }

            return null;
        }

        /// <summary>
        /// Adds one more object to a single type pool.
        /// </summary>
        /// <param name="pool">Pool at which should be added new object.</param>
        /// <returns>Returns reference to just added object.</returns>
        private GameObject AddObjectToPool(bool state)
        {
            if (!inited)
                Init();

            GameObject newObject = GameObject.Instantiate(prefab, ObjectsContainer);
            newObject.name = PoolManager.FormatName(name, pooledObjects.Count);
            newObject.SetActive(state);

            pooledObjects.Add(newObject);

            return newObject;
        }

        public void CreatePoolObjects(int count)
        {
            if (!inited)
                Init();

            if (!inited) return;
            pooledObjects.RemoveAll(value => value == null);
            if (capSize) count = Mathf.Min(count, maxSize);
            int sizeDifference = count - pooledObjects.Count;
            if (sizeDifference > 0)
            {
                for (int i = 0; i < sizeDifference; i++)
                {
                    AddObjectToPool(false);
                }
            }
        }

        /// <summary>
        /// Disables all active objects from this pool.
        /// </summary>
        /// <param name="resetParrent">Sets default parrent if checked.</param>
        public void ReturnToPoolEverything(bool resetParent = false)
        {
            if (!inited) return;

            for (int i = 0; i < pooledObjects.Count; i++)
            {
                if (pooledObjects[i] == null) continue;
                if (resetParent)
                {
                    pooledObjects[i].transform.SetParent(objectsContainer != null ? objectsContainer : PoolManager.DefaultContainer);
                }

                pooledObjects[i].SetActive(false);
            }
        }

        /// <summary>
        /// Destroys all spawned objects. Note, this method is performance heavy.
        /// </summary>
        public void Clear()
        {
            if (!inited) return;

            for (int i = 0; i < pooledObjects.Count; i++)
            {
                if (pooledObjects[i] != null)
                    PoolManager.DestroyObject(pooledObjects[i]);
            }

            pooledObjects.Clear();
        }
    }
}