using MaykerStudio.Demo;
using UnityEngine;

namespace MaykerStudio
{
    public class ChangeLayerChilds : MonoBehaviour
    {
        public enum Layer
        {
            WhatIsGround,
            EntityLayer
        }

        public LayersObject layersObj;

        public Layer layerType;

        void Start()
        {
            foreach (Transform child in transform)
            {
                switch (layerType)
                {
                    case Layer.WhatIsGround:
                        gameObject.layer = layersObj.WhatIsGround;
                        child.gameObject.layer = layersObj.WhatIsGround;
                        break;
                    case Layer.EntityLayer:
                        gameObject.layer = layersObj.EntityLayer;
                        child.gameObject.layer = layersObj.EntityLayer;
                        break;
                    default:
                        break;
                }
            }
        }
    }
}