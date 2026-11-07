using UnityEngine;
using System.Collections.Generic;

namespace NebulaSoft
{
    [System.Serializable]
    public sealed class PoolMultiple : IPool
    {
        [SerializeField] List<MultiPoolPrefab> multiPoolPrefabsList = new List<MultiPoolPrefab>();
        [SerializeField] bool capSize = false;
        [SerializeField] int maxSize = 10;

        [SerializeField] Transform objectsContainer = null;

        [SerializeField] string name;

        public string Name => name;

        private List<List<GameObject>> multiPooledObjects;
        private bool inited = false;

        public PoolMultiple(List<MultiPoolPrefab> multiPoolPrefabs, string name, Transform container = null)
        {
            this.multiPoolPrefabsList = multiPoolPrefabs;
            this.name = name;
            this.objectsContainer = PoolManager.GetContainer(container);

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

            if (multiPoolPrefabsList == null || multiPoolPrefabsList.Count == 0)
                throw new System.ArgumentException("A multi-pool requires at least one prefab.");
            bool hasNullPrefab = false;
            for (int i = 0; i < multiPoolPrefabsList.Count; i++)
            {
                if (multiPoolPrefabsList[i].Prefab == null)
                {
                    Debug.LogError(string.Format("[Pool] Pool initialization failed. There's no attached prefab at pool: {0}.", name));

                    hasNullPrefab = true;
                }
            }

            if (hasNullPrefab) return;

            multiPooledObjects = new List<List<GameObject>>();
            for (int i = 0; i < multiPoolPrefabsList.Count; i++)
            {
                multiPooledObjects.Add(new List<GameObject>());
            }

            PoolManager.AddPool(this);

            inited = true;
        }

        public GameObject GetPooledObject()
        {
            return GetPooledObject(-1);
        }

        /// <summary>
        /// Internal implementation of GetPooledObject and GetHierarchyPooledObject methods for Multi type pool.
        /// </summary>
        /// <param name="checkTypeActiveSelf">Which type of checking object's activation state is used: active self or active in hierarchy.</param>
        /// <param name="activateObject">If true object will be set as active.</param>
        /// <param name="position">Sets object to specified position.</param>
        /// <returns></returns>
        private GameObject GetPooledObject(int poolIndex)
        {
            if (!inited)
                Init();

            if (!inited) return null;
            int chosenPoolIndex = 0;

            if (poolIndex != -1)
            {
                chosenPoolIndex = poolIndex;
            }
            else
            {
                int totalWeight = multiPoolPrefabsList.Sum(x => x.Weight);
                int randomValue = Random.Range(1, totalWeight + 1);
                int currentWeight = 0;

                for (int i = 0; i < multiPoolPrefabsList.Count; i++)
                {
                    currentWeight += multiPoolPrefabsList[i].Weight;

                    if (currentWeight >= randomValue)
                    {
                        chosenPoolIndex = i;

                        break;
                    }
                }
            }

            List<GameObject> objectsList = multiPooledObjects[chosenPoolIndex];
            objectsList.RemoveAll(value => value == null);
            for (int i = 0; i < objectsList.Count; i++)
            {
                GameObject pooledObject = objectsList[i];

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

            if (!capSize || objectsList.Count < maxSize)
            {
                return AddObjectToPool(chosenPoolIndex, true);
            }

            return null;
        }

        /// <summary>
        /// Adds one more object to multi type Pool.
        /// </summary>
        /// <param name="pool">Pool at which should be added new object.</param>
        /// <returns>Returns reference to just added object.</returns>
        private GameObject AddObjectToPool(int poolIndex, bool state)
        {
            if (!inited)
                Init();

            GameObject newObject = GameObject.Instantiate(multiPoolPrefabsList[poolIndex].Prefab, objectsContainer);
            newObject.name = PoolManager.FormatName(name, multiPooledObjects[poolIndex].Count);
            newObject.SetActive(state);

            multiPooledObjects[poolIndex].Add(newObject);

            return newObject;
        }

        public void ReturnToPoolEverything(bool resetParent = false)
        {
            if (!inited) return;
            for (int i = 0; i < multiPooledObjects.Count; i++)
            {
                for (int j = 0; j < multiPooledObjects[i].Count; j++)
                {
                    if (multiPooledObjects[i][j] == null) continue;
                    if (resetParent)
                    {
                        multiPooledObjects[i][j].transform.SetParent(objectsContainer != null ? objectsContainer : PoolManager.DefaultContainer);
                    }

                    multiPooledObjects[i][j].SetActive(false);
                }
            }
        }

        /// <summary>
        /// Destroys all spawned objects. Note, this method is performance heavy.
        /// </summary>
        public void Clear()
        {
            if (!inited) return;
            for (int i = 0; i < multiPooledObjects.Count; i++)
            {
                for (int j = 0; j < multiPooledObjects[i].Count; j++)
                {
                    PoolManager.DestroyObject(multiPooledObjects[i][j]);
                }

                multiPooledObjects[i].Clear();
            }
        }

        /// <summary>
        /// Rerurns prefab from multi type pool by it's index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public MultiPoolPrefab GetPrefabByIndex(int index)
        {
            return multiPoolPrefabsList[index];
        }

        public void CreatePoolObjects(int count)
        {
            if (!inited)
                Init();

            if (!inited) return;
            if (capSize) count = Mathf.Min(count, maxSize);
            for (int i = 0; i < multiPooledObjects.Count; i++)
            {
                multiPooledObjects[i].RemoveAll(value => value == null);
                int sizeDifference = count - multiPooledObjects[i].Count;
                if (sizeDifference > 0)
                {
                    for (int j = 0; j < sizeDifference; j++)
                    {
                        AddObjectToPool(i, false);
                    }
                }
            }
        }

        [System.Serializable]
        public struct MultiPoolPrefab
        {
            public GameObject Prefab;
            public int Weight;

            public bool isWeightLocked;

            public MultiPoolPrefab(GameObject prefab, int weight, bool isWeightLocked)
            {
                this.Prefab = prefab;
                this.Weight = weight;
                this.isWeightLocked = isWeightLocked;
            }
        }
    }
}