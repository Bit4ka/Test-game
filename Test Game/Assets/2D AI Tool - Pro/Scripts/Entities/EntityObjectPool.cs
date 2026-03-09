using System.Collections.Generic;
using UnityEngine;

namespace AI2DTool
{
    /// <summary>
    /// This class can hold different gameobject on the same pool. The name of the object will be the key of the dictionary, 
    /// so I recommend that you don't add objects from scene, use the prefab reference instead, because prefab instances can have '(Clone)' or '(1)' in the name, and this can make things complicated.
    /// </summary>
    public class EntityObjectPool : MonoBehaviour
    {
        private readonly Dictionary<string, Queue<GameObject>> pool = new Dictionary<string, Queue<GameObject>>();

        private void GrowPool(GameObject prefab, Vector2 pos, Transform parent)
        {
            for (int i = 0; i < 3; i++)
            {
                var instaceToAdd = Instantiate(prefab, pos, prefab.transform.rotation, parent);
                instaceToAdd.name = prefab.name;
                AddToPool(instaceToAdd);
            }
        }

        /// <summary>
        /// This method will clear the entire pool dictionary. If you want to clear only a specific object pool then see the overload <see cref=""/>
        /// </summary>
        public virtual void ClearPool()
        {
            pool.Clear();
        }

        /// <summary>
        /// This method will clear a specific pool from the instanceToClear object.
        /// </summary>
        /// <param name="instanceToClear"></param>
        public virtual void ClearPool(GameObject instanceToClear)
        {
            if(pool.TryGetValue(instanceToClear.name, out Queue<GameObject> queue))
            {
                queue.Clear();
            }
        }

        /// <summary>
        /// This can commonly used to return the object to the pool, But it may happen that the instance was not in the pool from the beginning, so the method will create a key for the instance.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="parent"></param>
        public virtual void AddToPool(GameObject instance, Transform parent = null)
        {
            instance.SetActive(false);

            instance.transform.position = Vector2.zero;

            if (!pool.TryGetValue(instance.name, out Queue<GameObject> queue))
            {
                pool.Add(instance.name, new Queue<GameObject>());
                pool.TryGetValue(instance.name, out queue);

#if UNITY_EDITOR
                Debug.LogWarning(instance.name + " added to pool from instance reference. " +
                    "Please, always use prefab reference instead of instance, because the name of the game object is used to get from the pool.");
#endif
            }

            queue.Enqueue(instance);
        }

        /// <summary>
        /// Add a key value on the dictionary represent the prefab. Please, if the game object name contains '(Clone)' or '(1)' try to remove it before adding the key.
        /// Useful for entities that is manually placed on the scenes.
        /// </summary>
        /// <param name="instance"></param>
        public virtual void AddKey(GameObject instance)
        {
            if (!pool.ContainsKey(instance.name))
            {
                pool.Add(instance.name, new Queue<GameObject>());
            }
        }

        /// <summary>
        /// This function is used to add and get the object from pool. If the prefab was not in the pool then the method will add it and grow the pool on <see cref="GrowPool(GameObject, Vector2, Transform)"/>
        /// </summary>
        /// <param name="originalPrefab"></param>
        /// <param name="pos"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public virtual GameObject Get(GameObject originalPrefab, Vector2 pos, Transform parent)
        {
            GameObject entity;

            //If the gameobject is not on the pool, then add it and growPool.
            if (!pool.ContainsKey(originalPrefab.name))
            {
                pool.Add(originalPrefab.name, new Queue<GameObject>());
                GrowPool(originalPrefab, pos, parent);
            }

            pool.TryGetValue(originalPrefab.name, out Queue<GameObject> queue);

            //If the current pool is empty, than grow it.
            if (queue.Count == 0)
            {
                GrowPool(originalPrefab, pos, parent);
            }

            entity = queue.Dequeue();

            entity.transform.position = pos;
            entity.SetActive(true);
            
            return entity;
        }

    }
}