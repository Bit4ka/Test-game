using UnityEngine;

namespace MaykerStudio
{
    [ExecuteInEditMode]
    public class CameraFollow : MonoBehaviour
    {
        public static CameraFollow Instance;

        public Transform Target;

        public Vector2 Offset;

        [Range(1f, 10f)]
        public float speed = 1;
        public float OriginalSize { get; set; }

        public Camera Cam { get; set; }

        private void Start()
        {
            Instance = this;

            Cam = GetComponent<Camera>();
            OriginalSize = Cam.orthographicSize;

        }

        private void LateUpdate()
        {
            if (Target != null)
            {
                Vector3 targetPos = new Vector3(Target.position.x, Target.position.y, -10f);

                transform.position = Vector3.Lerp(transform.position, targetPos + (Vector3)Offset, speed * Time.unscaledDeltaTime);
            }
        }
    }
}