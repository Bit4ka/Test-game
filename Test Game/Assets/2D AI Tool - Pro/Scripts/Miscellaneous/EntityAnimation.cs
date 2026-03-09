using MaykerStudio;
using UnityEngine;

namespace AI2DTool
{
    [System.Serializable]
    public class EntityAnimation
    {
        [BeginGroup]
        [Tooltip("The name of the animation. Warning: Make sure it's the same name in the animator.")]
        public string Name;
        public Sound SoundAsset;

        [Range(0f, 1f)]
        [Tooltip("You can use this to have a smooth transition when this animation plays for 2D mesh animations.")]
        public float TransitionLength = 0f;

        [Range(0.1f, 5f)]
        [Tooltip("If you want to change the current animation speed you need to setup the parameter and tick the 'Multiplier' in animator")]
        public float SpeedMultiplier = 1f;

        [EndGroup]
        [Tooltip("Check this If you don't want this animation to be overridden by the damaged animation. This only work properly if the animation" +
            " is played through the Entity API "+nameof(Entity.PlayAnim))]
        public bool CanBeStoppedByDamage;
    }
}