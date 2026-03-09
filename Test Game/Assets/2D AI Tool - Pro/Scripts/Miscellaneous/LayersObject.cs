using UnityEngine;
namespace MaykerStudio.Demo
{
    [System.Serializable]
    public class LayersObject : ScriptableObject
    {
        [Layer]
        public int WhatIsGround;

        [Layer]
        public int WhatIsTarget;

        [Layer]
        public int PlayerLayer;

        [Layer]
        public int EntityLayer;

        [Layer]
        public int EntityProjectiles;

        [Layer]
        public int PlayerProjectiles;
    }

}

