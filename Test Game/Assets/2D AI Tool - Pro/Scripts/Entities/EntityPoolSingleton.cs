using UnityEngine;

namespace AI2DTool
{
    /// <summary>
    /// This is a custom implementation of <see cref="EntityObjectPool"/> of the 2DAITool that uses singleton, you can override the functions to make your own implementation.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class EntityPoolSingleton : EntityObjectPool
    {
        [Tooltip("This will be the parent transform of the pooled objects.")]
        public Transform entitiesHolder;

        public static EntityPoolSingleton Instance;

        public void Start()
        {
            Instance = this;

            //Uncomment this if you want this pool to live even if the scene dies. If you want to reuse entities though out scenes you can set the entitiesHolder
            //to this gameobject transform. Remember that if you don't set the holder to this gameobject transform and the scene gets unloaded the entity pool will lost the reference
            //to all the entities, and this can lead to Missing Reference or Null Exception errors.
            //DontDestroyOnLoad(_instance.gameObject);
        }

        public void AddToPool(GameObject instance)
        {
            base.AddToPool(instance, entitiesHolder);
        }

        public override GameObject Get(GameObject originalPrefab, Vector2 pos, Transform parent)
        {
            return base.Get(originalPrefab, pos, entitiesHolder);
        }
    }
}