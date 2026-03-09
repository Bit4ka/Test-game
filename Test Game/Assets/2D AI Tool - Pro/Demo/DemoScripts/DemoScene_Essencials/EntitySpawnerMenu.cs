using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MaykerStudio.Demo
{
    [DefaultExecutionOrder(-1000)]
    public class EntitySpawnerMenu : MonoBehaviour
    {
        [BeginGroup("Important")]
        [SerializeField]
        [Help("Check the ReadMe file for more information")]
        [EndGroup]
        private LayersObject LayersObject;

        [ReorderableList]
        public GameObject[] entitiesPrefabs;

        public GameObject contentGO;

        public GameObject scrollView;

        public GameObject mainText;

        public GameObject buttonTemplate;

        private bool isOpen;

        private bool isSpawning;

        private GameObject spawnPrefab;

        private void OnEnable()
        {
            Physics2D.IgnoreLayerCollision(LayersObject.PlayerLayer, LayersObject.EntityLayer, true);
            Physics2D.IgnoreLayerCollision(LayersObject.PlayerLayer, LayersObject.PlayerLayer, true);
            Physics2D.IgnoreLayerCollision(LayersObject.EntityLayer, LayersObject.EntityLayer, true);

            Physics2D.IgnoreLayerCollision(LayersObject.EntityProjectiles, LayersObject.EntityProjectiles, true);
            Physics2D.IgnoreLayerCollision(LayersObject.EntityProjectiles, LayersObject.EntityLayer, true);

            Physics2D.IgnoreLayerCollision(LayersObject.PlayerProjectiles, LayersObject.PlayerProjectiles, true);
            Physics2D.IgnoreLayerCollision(LayersObject.PlayerProjectiles, LayersObject.PlayerLayer, true);
        }

        void Start()
        {
            foreach (GameObject entity in entitiesPrefabs)
            {
                GameObject buttonGo = Instantiate(buttonTemplate, contentGO.transform);

                buttonGo.GetComponentInChildren<Text>().text = entity.name;

                Button button = buttonGo.GetComponent<Button>();

                button.onClick.AddListener(() => SpawnentityAndFollowMouse(entity));

                buttonGo.SetActive(true);

                ChangeEntityLayers(entity);
            }

            isOpen = false;
            mainText.SetActive(true);
            scrollView.SetActive(false);     
        }

        public void Update()
        {
#if !ENABLE_INPUT_SYSTEM
            if (Input.GetKeyDown(KeyCode.Tab) && !isSpawning)
            {
                CloseOpenScrollView();
            }
#else

            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                CloseOpenScrollView();
            }
#endif

            if (isSpawning)
            {
                FollowMouse(spawnPrefab);


#if ENABLE_INPUT_SYSTEM
                if (Mouse.current.rightButton.wasPressedThisFrame)
#else
                if (Input.GetMouseButtonDown(1))
#endif
                {
                    isSpawning = false;

                    CameraFollow.Instance.Target = GameObject.Find("Player").transform;
                    CameraFollow.Instance.Cam.orthographicSize = CameraFollow.Instance.OriginalSize;

                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    CloseOpenScrollView();

                    Time.timeScale = 1f;

                    AI2DTool.EntityPoolSingleton.Instance.AddToPool(spawnPrefab);
                }
            }
        }

        public void CloseOpenScrollView()
        {
            isOpen = !isOpen;

            scrollView.SetActive(isOpen);

            mainText.SetActive(!isOpen);
        }

        public void SpawnentityAndFollowMouse(GameObject prefab)
        {
            CloseOpenScrollView();

            GameObject entity = AI2DTool.EntityPoolSingleton.Instance.Get(prefab, Camera.main.ScreenToWorldPoint(Input.mousePosition), null);

            spawnPrefab = entity;
            isSpawning = true;

            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = false;

            Time.timeScale = 0f;
        }

        void FollowMouse(GameObject prefab)
        {
            CameraFollow.Instance.Target = prefab.transform;
            CameraFollow.Instance.Cam.orthographicSize = 15f;

            prefab.TryGetComponent(out AI2DTool.EntityAI e);

#if ENABLE_INPUT_SYSTEM
            if(!Mouse.current.leftButton.wasPressedThisFrame)
#else
            if (!Input.GetMouseButtonDown(0))
#endif

            {
                if (e != null)
                {
                    e.enabled = false;
                }

                Vector2 pos2D = Camera.main.ScreenToWorldPoint(Input.mousePosition) * .75f;

                prefab.transform.position = pos2D;

                return;
            }

            e.enabled = true;

            spawnPrefab = AI2DTool.EntityPoolSingleton.Instance.Get(prefab, Camera.main.ScreenToWorldPoint(Input.mousePosition), null);
        }

        private void ChangeEntityLayers(GameObject entity)
        {
            entity.layer = LayersObject.EntityLayer;

            AI2DTool.EntityAI script = entity.GetComponent<AI2DTool.EntityAI>();

            script.entityData.whatIsObstacles = LayerMask.GetMask(LayerMask.LayerToName(LayersObject.WhatIsGround));
            script.entityData.whatIsTarget = LayerMask.GetMask(LayerMask.LayerToName(LayersObject.WhatIsTarget));
        }

        public void ReturnToMenu()
        {
            SceneManager.LoadScene(0);
        }
    }
}