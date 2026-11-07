using System.Collections.Generic;
using UnityEngine;

namespace NebulaSoft
{
    /// <summary>
    /// Generic pool. Caches specified component allowing not to use GetComponent<> after each call. Can not be added into the PoolManager.
    /// To use just create new instance.
    /// </summary>
    /// <typeparam name="T">Component to cache.</typeparam>
    public class PoolGeneric<T> : IPool where T : Component
    {
        private GameObject prefab = null;
        private Transform objectsContainer = null;
        private string name;

        private bool capSize = false;
        private int maxSize = 10;

        public string Name => name;
        public GameObject Prefab => prefab;
        public int MaxSize => maxSize;
        public bool CapSize => capSize;

        public Transform ObjectsContainer => PoolManager.GetContainer(objectsContainer);

        private bool inited = false;

        public List<T> pooledObjects;

        public PoolGeneric(GameObject prefab)
        {
            this.prefab = prefab;

            name = prefab.name;

            Init();
        }

        public PoolGeneric(GameObject prefab, string name)
        {
            this.prefab = prefab;
            this.name = name;

            Init();
        }

        public PoolGeneric(GameObject prefab, Transform objectsContainer)
        {
            this.prefab = prefab;
            this.objectsContainer = objectsContainer;

            name = prefab.name;

            Init();
        }

        public PoolGeneric(GameObject prefab, string name, Transform objectsContainer)
        {
            this.prefab = prefab;
            this.name = name;
            this.objectsContainer = objectsContainer;

            Init();
        }

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

            if (prefab.GetComponent<T>() == null)
            {
                Debug.LogError(string.Format("[Pool] Pool initialization failed. There's no attached component ({0}) to prefab at pool: {1}.", typeof(T), name));

                return;
            }

            pooledObjects = new List<T>();

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
                T pooledComponent = pooledObjects[i];

                if (pooledComponent == null || pooledComponent.gameObject == null)
                {
                    Debug.LogError(string.Format("[Pool]: A pooled object ({0}) was destroyed externally. This may indicate that an object was not properly returned to the pool, or its parent object was destroyed. Please review your object management logic to prevent unintended object destruction.", name));

                    continue;
                }

                GameObject pooledObject = pooledComponent.gameObject;
                if (!pooledObject.activeSelf)
                {
                    pooledObject.SetActive(true);

                    return pooledObject;
                }
            }

            if (!capSize || pooledObjects.Count < maxSize)
            {
                T pooledComponent = AddObjectToPool(true);

                return pooledComponent.gameObject;
            }

            return null;
        }

        public T GetPooledComponent()
        {
            if (!inited)
                Init();

            if (!inited) return null;
            pooledObjects.RemoveAll(value => value == null);

            for (int i = 0; i < pooledObjects.Count; i++)
            {
                T pooledComponent = pooledObjects[i];

                if (pooledComponent == null)
                {
                    Debug.LogError(string.Format("[Pool]: A pooled object ({0}) was destroyed externally. This may indicate that an object was not properly returned to the pool, or its parent object was destroyed. Please review your object management logic to prevent unintended object destruction.", name));

                    continue;
                }

                GameObject pooledObject = pooledComponent.gameObject;
                if (!pooledObject.activeSelf)
                {
                    pooledObject.SetActive(true);

                    return pooledComponent;
                }
            }

            if (!capSize || pooledObjects.Count < maxSize)
            {
                return AddObjectToPool(true);
            }

            return null;
        }

        /// <summary>
        /// Adds one more object to a single type pool.
        /// </summary>
        /// <param name="pool">Pool at which should be added new object.</param>
        /// <returns>Returns reference to just added object.</returns>
        private T AddObjectToPool(bool state)
        {
            if (!inited)
                Init();

            GameObject newObject = GameObject.Instantiate(prefab, ObjectsContainer);
            newObject.name = PoolManager.FormatName(name, pooledObjects.Count);
            newObject.SetActive(state);

            T component = newObject.GetComponent<T>();

            pooledObjects.Add(component);

            return component;
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

                pooledObjects[i].gameObject.SetActive(false);
            }
        }

        public void Clear()
        {
            if (!inited) return;

            for (int i = 0; i < pooledObjects.Count; i++)
            {
                if (pooledObjects[i] != null)
                    PoolManager.DestroyObject(pooledObjects[i].gameObject);
            }

            pooledObjects.Clear();
        }
    }
}