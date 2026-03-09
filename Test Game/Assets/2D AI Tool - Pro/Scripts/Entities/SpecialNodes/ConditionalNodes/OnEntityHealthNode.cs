using UnityEngine;
using UnityEngine.Serialization;
using XNode;


namespace AI2DTool
{
    [CreateNodeMenu("Special Nodes/Conditional Nodes/On Entity Health")]
    [NodeTint("#0b4a0d")]
    public class OnEntityHealthNode : OnEventNodeBase
    {
        [BeginGroup("Health Options")]
        [NodeEnum]
        [Tooltip("How the health will be calculated to trigger the transition.")]
        public HealthType healthType;

        [ShowIf(nameof(healthType), HealthType.Value)]
        [FormerlySerializedAs("Health Value")]
        [Min(0)]
        public int healthValue;

        [ShowIf(nameof(healthType), HealthType.Percentage)]
        [FormerlySerializedAs("Health Percentage")]
        [Min(0)]
        [EndGroup]
        public int healthPercentage;

        public enum HealthType { Percentage, Value}

        public override void TryTransitionToNextState(Entity entity, DamageDetails details)
        {
            if (entityAI.IsDead)
            {
                return;
            }

            if (healthType == HealthType.Percentage)
            {
                if (Mathf.RoundToInt(((entity.CurrentHealth - details.damageAmount) / entity.MaxHealth) * 100f) <= healthPercentage)
                {
                    StartTransition();
                }
            }
            else
            {
                if (entity.CurrentHealth - details.damageAmount <= healthValue)
                {
                    StartTransition();
                }
            }
        }

    }
}