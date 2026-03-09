using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MaykerStudio.Demo
{
    public class InteractableObjectsSetup : MonoBehaviour
    {
        public LayersObject LayersObj;

        void Start()
        {
            foreach (Transform t in transform)
            {
                if(t.TryGetComponent(out EntityInteractable ei))
                {
                    ei.EntityLayers = 1 << LayersObj.EntityLayer;
                }
            }
        }

    }
}