using System.Collections;
using UnityEngine;

namespace MaykerStudio.Demo
{
    public class AutoDestroy : MonoBehaviour
    {
        [SerializeField]
        private float time;

        IEnumerator Start()
        {
            yield return new WaitForSeconds(time);

            Destroy(gameObject);
        }

    }
}

