using UnityEngine;

namespace MaykerStudio.Demo
{
    public class TeleportTo : MonoBehaviour
    {
        public Transform ToTransform;
        public LayersObject LayersOBJ;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!LayerMask.LayerToName(collision.gameObject.layer).Equals(LayerMask.LayerToName(LayersOBJ.EntityLayer)))
            {
                Vector2 p = new Vector2(ToTransform.position.x, ToTransform.position.y + 5f);

                collision.transform.position = p;
            }


        }
    }
}