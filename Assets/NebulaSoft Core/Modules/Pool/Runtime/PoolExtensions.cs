using UnityEngine;

namespace NebulaSoft
{
    public static class PoolExtensions
    {
        public static IPool GetPool(this GameObject gameObject)
        {
            if(PoolManager.HasPool(gameObject.name))
                return PoolManager.GetPoolByName(gameObject.name);

            return new Pool(gameObject);
        }

        public static void Destroy(this IPool pool)
        {
            if(pool != null)
                PoolManager.DestroyPool(pool);
        }

        public static GameObject SetPosition(this GameObject gameObject, Vector3 position)
        {
            gameObject.transform.position = position;

            return gameObject;
        }

        public static GameObject SetPositionAndRotation(this GameObject gameObject, Vector3 position, Quaternion rotation)
        {
            gameObject.transform.SetPositionAndRotation(position, rotation);

            return gameObject;
        }

        public static GameObject SetLocalPosition(this GameObject gameObject, Vector3 localPosition)
        {
            gameObject.transform.localPosition = localPosition;

            return gameObject;
        }

        public static GameObject SetScale(this GameObject gameObject, Vector3 scale)
        {
            gameObject.transform.localScale = scale;

            return gameObject;
        }

        public static GameObject SetEulerAngles(this GameObject gameObject, Vector3 eulerAngles)
        {
            gameObject.transform.eulerAngles = eulerAngles;

            return gameObject;
        }

        public static GameObject SetLocalEulerAngles(this GameObject gameObject, Vector3 localEulerAngles)
        {
            gameObject.transform.localEulerAngles = localEulerAngles;

            return gameObject;
        }

        public static GameObject SetRotation(this GameObject gameObject, Quaternion rotation)
        {
            gameObject.transform.rotation = rotation;

            return gameObject;
        }

        public static GameObject SetLocalRotation(this GameObject gameObject, Quaternion localRotation)
        {
            gameObject.transform.localRotation = localRotation;

            return gameObject;
        }

        public static GameObject SetParent(this GameObject gameObject, Transform parent)
        {
            gameObject.transform.SetParent(parent);

            return gameObject;
        }
    }
}

