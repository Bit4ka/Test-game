using UnityEngine;

namespace AI2DTool
{
    [CreateAssetMenu(fileName = "newEntityData", menuName = "Data/Entity Data/Base Data")]
    public class D_Entity : ScriptableObject
    {
        public GameType gameType;

        [BeginGroup("Health and Fall Speed")]
        [Line(HexColor = "#ff0000")]
        [Min(0.1f)]
        public float maxHealth = 100;

        [Line(HexColor = "#ff0000")]
        [Range(1f, 20f)]
        [Tooltip("When the Entity is falling the gravity scale will be multiplied by this value. Leave at 1 if you don't want to use this.")]
        public float GravityScaleMultiplier = 6f;

        [EndGroup]
        [Range(-30, -200)]
        [Tooltip("The entity will clamp the negative Y velocity of the Rigidbody to this value. States can ignore if they set " + nameof(Entity.IgnoreFallClamp) + " to true.")]
        public float maxFallSpeed = -30f;

        [Line(HexColor = "#723287")]
        [BeginGroup("Topdown Rotation")]
        [Tooltip("If checked then the transform of the entity will rotate in Z axis when asked. If not checked, then the animator will set parameters to indicate directions for sprites.")]
        [ShowIf(nameof(gameType), GameType.Topdown2D)]
        public bool rotateTheTransform;

        [ShowIf(nameof(gameType), GameType.Topdown2D)]
        [EndGroup]
        [Range(0, 100)]
        [Tooltip("Put 0 if you don't want a smooth rotation.")]
        public int rotationSpeed = 0;

        [Line(HexColor = "#0000ff")]
        [Min(0.0f)]
        [BeginGroup("Checkers")]
        public float wallCheckDistance = 1f;

        [Min(0.0f)]
        [HideIf(nameof(gameType), GameType.Topdown2D)]
        [Tooltip("This check if the entity is going to step in a hole or a ledge.")]
        public float ledgeCheckDistance = 0.75f;
        
        [Min(0.0f)]
        [HideIf(nameof(gameType), GameType.Topdown2D)]
        [Tooltip("This set the minimum angle a collider for the entity be 'Grounded'. Use with caution, the default values works best for platformers.")]
        public float groundMinNormalAngle = 45f;

        [EndGroup]
        [Min(0.0f)]
        [HideIf(nameof(gameType), GameType.Topdown2D)]
        [Tooltip("This set the minimum angle of a collider for the entity be 'Grounded'. Use with caution, the default values works best for platformers.")]
        public float groundMaxNormalAngle = 135f;

        [Line(HexColor = "#00ff00")]
        [BeginGroup("Box 2D detection")]
        [Help("Don't use negative values on box size, use originOffset to tweak position")]
        [Tooltip("entity will check for targets inside a BoxCast with this size, originOffset and boxDetectionAngle")]
        public Vector2 boxDetectionSize;

        public Vector2 originOffset;

        [EndGroup]
        public float boxDetectionAngle;

        [Line(HexColor = "#d1cb1b")]
        [BeginGroup("Stun vars")]
        [Tooltip("The amount of stun damage the entity needs to receive to get stunned. Put 0 if you don't want this mechanic.")]
        public float stunResistance = 0f;

        [EndGroup]
        [Tooltip("If the entity doesn't receive damage for this amount of time the stun resistance will reset. This works as if it were a stamina.")]
        public float stunRecoveryTime = 2f;

        [Line(HexColor = "#bdbdbd")]
        [BeginGroup("Layers")]
        [Tooltip("The layers that the entity will collide.")]
        public LayerMask whatIsObstacles;

        [EndGroup]
        [Tooltip("The layers that the entity will collide.")]
        public LayerMask whatIsTarget;

        [Line(HexColor = "#a60fa1")]
        [Min(0.001f)]
        [BeginGroup("Damage")]
        [Tooltip("This value will be the amount of time the entity is in a state of damaged. The entity can't be damaged if it's already in a state of damaged.")]
        public float maxDamagedTimer = 0.1f;

        [Tooltip("If checked the entity will flip or face to the target damage direction.")]
        public bool faceDamageDirection = true;

        [Tooltip("If checked the entity will receive damage amount when it reaches the negative 'fall Y velocity'")]
        public bool receiveFallDamage;

        [ShowIf(nameof(receiveFallDamage), true)]
        public float fallDamageAmount = 5f;

        [ShowIf(nameof(receiveFallDamage), true)]
        [Range(-5f, -100f)]
        [EndGroup]
        [Tooltip("The minimum velocity to receive damage.")]
        public float fallYVelocity = -20f;

        public enum GameType
        {
            Platformer2D,
            Topdown2D
        }
    }
}