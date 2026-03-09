using UnityEngine;

namespace AI2DTool
{
    /// <summary>
    /// Data struct that holds informations about a damage.
    /// </summary>
    public struct DamageDetails
    {
        public Vector2 position;

        public GameObject sender;

        public float damageAmount;

        public float stunDamageAmount;
    }
}