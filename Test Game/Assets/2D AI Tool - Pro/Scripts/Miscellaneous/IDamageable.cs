using UnityEngine;

namespace AI2DTool
{
    public interface IDamageable
    {
        void Damage(DamageDetails details);

        void KnockBack(float knockBackLevel, float knockBackDuration, Vector2 knockBackDirection);
    }
}