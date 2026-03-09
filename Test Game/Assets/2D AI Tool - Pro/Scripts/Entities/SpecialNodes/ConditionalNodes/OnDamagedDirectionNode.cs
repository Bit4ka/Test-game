using UnityEngine;

namespace AI2DTool
{
    [CreateNodeMenu("Special Nodes/Conditional Nodes/On Damaged Direction")]
    [NodeTint("#b335b5")]
    public class OnDamagedDirectionNode : OnEventNodeBase
    {
        [BeginGroup("Direction Options")]
        [Tooltip("Select whether the direction of the damage and the direction the entity is facing should be opposite or equal to initiate a transition." +
            "Or if you just want to transition no matter the direction of damage.")]
        
        public Direction facingAndDamage;

        [EndGroup]
        public bool MaxOutAggro = true;

        public override void TryTransitionToNextState(Entity entity, DamageDetails details)
        {
            if (entityAI.IsDead)
            {
                return;
            }

            switch (facingAndDamage)        
            {
                case Direction.OppositeDirection:
                    if (!entity.IsFacingPosition(details.position))
                        StartTransition();
                    break;
                case Direction.SameDirection:
                    if (entity.IsFacingPosition(details.position))
                        StartTransition();
                    break;
                case Direction.Any:
                    StartTransition();
                    break;
            }

            if (MaxOutAggro)
                entityAI.InstantFillAggro();
        }

        public enum Direction
        {
            OppositeDirection,
            SameDirection,
            Any
        }
    }
}