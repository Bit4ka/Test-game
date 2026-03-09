using UnityEditor;
using UnityEngine;

namespace MaykerStudio.Demo
{
    [DefaultExecutionOrder(10000)]
    public class CompanionSetup : MonoBehaviour
    {
        [EditorButton(nameof(SpawnOnPlayerPos), "Spawn companion", ButtonActivityType.OnPlayMode)]
        public GameObject companionPrefab;

        public LayersObject layersObj;

        private void Start()
        {
            companionPrefab.layer = layersObj.PlayerLayer;
            companionPrefab.TryGetComponent(out AI2DTool.EntityAI e);

            companionPrefab.TryGetComponent(out EntityInput eI);

            eI.PlayerLayer = layersObj.PlayerLayer;

            companionPrefab.SetActive(false);

            if (e != null)
            {
                e.entityData.whatIsObstacles |= 1 << layersObj.WhatIsGround;
                e.entityData.whatIsTarget |= 1 << layersObj.EntityLayer;
            }
        }

        private void SpawnOnPlayerPos()
        {
            GameObject co = Instantiate(companionPrefab, GameObject.Find("Player").transform.position, companionPrefab.transform.rotation);
            co.SetActive(true);

#if UNITY_EDITOR
            Undo.RegisterCreatedObjectUndo(co, "Spawned companion");
#endif
        }
    }
}