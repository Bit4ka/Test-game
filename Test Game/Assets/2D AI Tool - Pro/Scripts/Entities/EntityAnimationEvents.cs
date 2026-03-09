using UnityEngine;

namespace MaykerStudio
{
    /// <summary>
    /// This component is for the animator be able to call functions from the entity that can be used for States. Attach this component with an animator.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class EntityAnimationEvents : MonoBehaviour
    {
        private AI2DTool.EntityAI entity;

        private void Awake()
        {
            entity = GetComponentInParent<AI2DTool.EntityAI>();
        }

        public void AnimationTrigger1()
        {
            if (entity == null)
                entity = GetComponentInParent<AI2DTool.EntityAI>();

            entity.AnimationTrigger1();
        }

        public void AnimationTrigger2()
        {
            if (entity == null)
                entity = GetComponentInParent<AI2DTool.EntityAI>();

            entity.AnimationTrigger2();
        }

        public void AnimationFinish()
        {
            if (entity == null)
                entity = GetComponentInParent<AI2DTool.EntityAI>();

            entity.AnimationFinish();
        }

        public void InstantiatePrefab(GameObject prefab)
        {
            if (entity == null)
                entity = GetComponentInParent<AI2DTool.EntityAI>();

            entity.InstantiatePrefab(prefab);
        }

        public void PlaySound(Sound soundAsset)
        {
            if (entity == null)
                entity = GetComponentInParent<AI2DTool.EntityAI>();

            entity.PlayAudio(soundAsset, true);
        }
    }
}