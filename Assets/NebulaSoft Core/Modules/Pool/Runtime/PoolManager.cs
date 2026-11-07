#pragma warning disable 0414

using UnityEngine;
using System.Collections.Generic;

namespace NebulaSoft
{
    /// <summary>
    /// Class that manages all pool operations.
    /// </summary>
    [StaticUnload]
    public static class PoolManager
    {
        private const string OBJECT_FORMAT = "{0} e{1}";

        /// <summary>
        /// List of all existing pools.
        /// </summary>
        private static List<IPool> poolsList = new List<IPool>();

        /// <summary>
        /// Dictionary which allows to acces Pool by name.
        /// </summary>
        private static Dictionary<string, IPool> poolsDictionary;

        public static Transform DefaultContainer { get; private set; }

        static PoolManager()
        {
            poolsList = new List<IPool>();
            poolsDictionary = new Dictionary<string, IPool>(System.StringComparer.Ordinal);
        }

        public static void ReturnToPool()
        {
            if (!poolsList.IsNullOrEmpty())
            {
                for (int i = 0; i < poolsList.Count; i++)
                {
                    poolsList[i].ReturnToPoolEverything(true);
                }
            }
        }

        /// <summary>
        /// Returns reference to Pool by it's name.
        /// </summary>
        /// <param name="poolName">Name of Pool which should be returned.</param>
        /// <returns>Reference to Pool.</returns>
        public static IPool GetPoolByName(string poolName)
        {
            string poolHash = poolName;

            if (poolsDictionary.ContainsKey(poolHash))
            {
                return poolsDictionary[poolHash];
            }

            Debug.LogError("[Pool] Not found pool with name: '" + poolName + "'");

            return null;
        }

        public static void AddPool(IPool pool)
        {
            if(pool == null)
            {
                Debug.LogError("[Pool]: Attempted to add a null pool reference. Please ensure a valid IPool instance is provided.");

                return;
            }

            string poolHash = pool.Name;

            if (poolsDictionary.ContainsKey(poolHash))
            {
                Debug.LogError("[Pool] Adding a new pool failed. Name \"" + pool.Name + "\" already exists.");

                return;
            }

            poolsDictionary.Add(poolHash, pool);
            poolsList.Add(pool);
        }

        public static bool HasPool(string name)
        {
            return poolsDictionary.ContainsKey(name);
        }

        public static void DestroyPool(IPool pool)
        {
            if (pool == null)
            {
                Debug.LogError("[Pool]: Attempted to destroy a null pool reference. Please ensure a valid IPool instance is provided.");

                return;
            }

            pool.Clear();

            if (poolsDictionary.TryGetValue(pool.Name, out var registered) && ReferenceEquals(registered, pool))
                poolsDictionary.Remove(pool.Name);
            poolsList.Remove(pool);
        }

        public static bool PoolExists(string name)
        {
            return poolsDictionary.ContainsKey(name);
        }

        public static Transform GetContainer(Transform poolContainer)
        {
            if (poolContainer == null)
            {
                if(DefaultContainer == null)
                {
                    // Create container object
                    GameObject containerObject = new GameObject("[POOL OBJECTS]");
                    DefaultContainer = containerObject.transform;
                    DefaultContainer.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    DefaultContainer.localScale = Vector3.one;

                    if (Application.isPlaying) GameObject.DontDestroyOnLoad(containerObject);
                }

                return DefaultContainer;
            }

            return poolContainer;
        }

        public static string FormatName(string name, int elementIndex)
        {
            return string.Format(OBJECT_FORMAT, name, elementIndex);
        }

        public static void Shutdown()
        {
            foreach (var pool in poolsList.ToArray()) pool.Clear();
            poolsList.Clear();
            poolsDictionary.Clear();
            if (DefaultContainer != null)
            {
                if (Application.isPlaying) GameObject.Destroy(DefaultContainer.gameObject);
                else GameObject.DestroyImmediate(DefaultContainer.gameObject);
            }
            DefaultContainer = null;
        }

        internal static void DestroyObject(GameObject target)
        {
            if (target == null) return;
            if (Application.isPlaying) GameObject.Destroy(target);
            else GameObject.DestroyImmediate(target);
        }

        private static void UnloadStatic()
        {
            Shutdown();
            poolsDictionary.Clear();
        }
    }
}
