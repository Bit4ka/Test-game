using AI2DTool;
using UnityEngine;

namespace MaykerStudio
{
    /// <summary>
    /// This component holds a <see cref="DamageDetails"/> that can be used in combat related objects, so entities can get the damage details from this holder.
    /// </summary>
    public class AttackDetailsHolder : MonoBehaviour
    {
        public DamageDetails details;
    }
}