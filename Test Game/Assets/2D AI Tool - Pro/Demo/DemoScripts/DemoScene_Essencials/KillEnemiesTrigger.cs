using AI2DTool;
using UnityEngine;

namespace MaykerStudio.Demo
{
    public class KillEnemiesTrigger : MonoBehaviour
    {
        public LayersObject layersOBJ;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.gameObject && collision.gameObject.layer == layersOBJ.EntityLayer)
            {
                collision.TryGetComponent(out EntityAI e);

                if (e != null)
                {
                    DamageDetails details = new DamageDetails
                    {
                        damageAmount = 1000000
                    };

                    e.Damage(details);
                }
            }
        }
    }
}