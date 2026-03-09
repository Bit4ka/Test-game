using MaykerStudio;
using System.Collections.Generic;
using UnityEngine;

namespace MaykerStudio.Demo
{
    [ExecuteAlways]
    public class ProjectilesSetup : MonoBehaviour
    {
        public GameObject playerProjectile;

        [ReorderableList(ListStyle.Round)]
        public GameObject[] entityProjectiles;

        public LayersObject layersObj;

        private void Start()
        {
            playerProjectile.layer = layersObj.PlayerProjectiles;
            playerProjectile.TryGetComponent(out Projectile playerP);

            //Setup player projectile;
            if (playerP != null)
            {
                playerP.whatIsGround = 1 << layersObj.WhatIsGround;
                playerP.whatIsTarget = 1 << layersObj.EntityLayer;
            }

            foreach (GameObject projectile in entityProjectiles)
            {
                projectile.layer = layersObj.EntityProjectiles;
                projectile.TryGetComponent(out Projectile entityP);

                //Setup entity projectile;
                if (entityP != null)
                {
                    entityP.whatIsGround = 1 << layersObj.WhatIsGround;
                    entityP.whatIsTarget = 1 << layersObj.PlayerLayer;
                }
            }
        }
    }

}

