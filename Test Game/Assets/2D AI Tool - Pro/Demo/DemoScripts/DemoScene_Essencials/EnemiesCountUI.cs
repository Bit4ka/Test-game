using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MaykerStudio
{
    public class EnemiesCountUI : MonoBehaviour
    {
        public Transform entityParentTransform;

        public Text text;

        private int count;
        private int _count;

        IEnumerator Start()
        {
            while (enabled)
            {
                text.text = "Enemies amount: " + count;

                _count = 0;

                foreach (Transform child in entityParentTransform)
                {
                    if (child.gameObject.activeSelf)
                    {
                        _count++;
                    }
                }

                count = _count;

                yield return null;
            }


        }
    }
}