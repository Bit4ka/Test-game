using AI2DTool;
using MaykerStudio.Demo;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace MaykerStudio
{
    public class EntitySystemEditor : EditorWindow
    {
        private static Texture2D tex;

        //-----------Create entity vars------------\\
        private static int tabs;
        private static Direction entityDir = Direction.Right;
        private static D_Entity.GameType gameType = D_Entity.GameType.Platformer2D;
        private static DefaultAsset targetFolder = null;
        private static string entityName = "";
        private static EditorWindow mainWindow;
        private static int entityLayer;

        //-----------Edit entity vars------------\\
        private bool isActive;
        private GameObject previousEntityPrefabField;
        public GameObject entityPrefabField;
        public EntityAI currentEntityScript;
        readonly Dictionary<Object, Editor> inspectors = new Dictionary<Object, Editor>();

        Vector2 dataScrollsPos;

        [MenuItem("Tools/2D AI Tool/Entity System Editor")]
        public static void ShowWindow()
        {
            mainWindow = GetWindow<EntitySystemEditor>("Entity System Editor");
            mainWindow.minSize = new Vector2(500, 300);
        }

        private void BGTex()
        {
            ColorUtility.TryParseHtmlString("#34356D", out Color C);

            tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, C);
            tex.Apply();
        }

        private void OnGUI()
        {
            if (tex == null)
                BGTex();

            GUI.DrawTexture(new Rect(0, 0, maxSize.x, maxSize.y), tex, ScaleMode.StretchToFill);

            tabs = GUILayout.Toolbar(tabs, new string[] { "Create entity", "Edit entity" });

            GUILayout.Space(5f);

            #region CreateTab
            if (tabs == 0)
            {
                entityName = EditorGUILayout.TextField("Entity Name: ", entityName);

                GUILayout.Space(10f);

                entityDir = (Direction)EditorGUILayout.EnumPopup("Entity Facing Direction: ", entityDir);

                GUILayout.Space(10f);

                gameType = (D_Entity.GameType)EditorGUILayout.EnumPopup("Game type: ", gameType);

                GUILayout.Space(10f);

                entityLayer = EditorGUILayout.LayerField("Entity Layer: ", entityLayer);

                GUILayout.Space(10f);

                EditorGUILayout.BeginHorizontal();
                targetFolder = (DefaultAsset)EditorGUILayout.ObjectField("Folder to save: ", targetFolder, typeof(DefaultAsset), false, GUILayout.MinWidth(150f));

                if (GUILayout.Button("Set current folder", GUILayout.MaxWidth(150f)))
                {
                    MethodInfo getActiveFolderPath = typeof(ProjectWindowUtil).GetMethod(
                    "GetActiveFolderPath",
                    BindingFlags.Static | BindingFlags.NonPublic);

                    string folderPath = (string)getActiveFolderPath.Invoke(null, null);

                    targetFolder = (DefaultAsset)AssetDatabase.LoadAssetAtPath(folderPath, typeof(DefaultAsset));
                }

                EditorGUILayout.EndHorizontal();

                CheckEverythingIsOk(entityName);

                GUILayout.Space(50f);

                if (GUILayout.Button("Generate Entity") && CheckEverythingIsOk(entityName))
                {
                    Object template;

                    template = Resources.Load("EntityTemplate");

                    if (template == null)
                    {
                        EditorUtility.DisplayDialog("Error", "Entity template not found; Please, re-install the package.", "Ok");
                        return;
                    }

                    GameObject EntityPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(AssetDatabase.GetAssetPath(targetFolder) + "/" + entityName + ".prefab", typeof(GameObject));

                    if (EntityPrefab != null)
                    {
                        if (!EditorUtility.DisplayDialog("Overwrite?",
                        "Already exists a prefab with the same name on the target folder, do you want to overwrite?", "Overwrite", "Cancel"))
                        {
                            return;
                        }
                        else
                        {
                            Debug.Log(entityName + " overwritten");
                        }
                    }

                    AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(template), AssetDatabase.GetAssetPath(targetFolder) + "/" + entityName + ".prefab");

                    EntityPrefab = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(targetFolder) + "/" + entityName + ".prefab");

                    ScriptableObject entityData = CreateInstance("D_Entity");

                    ScriptableObject EntityGraph = CreateInstance(typeof(EntityGraph));

                    AssetDatabase.CreateAsset(entityData, AssetDatabase.GetAssetPath(targetFolder) + "/" + "entityData_" + entityName + ".asset");
                    AssetDatabase.CreateAsset(EntityGraph, AssetDatabase.GetAssetPath(targetFolder) + "/" + entityName + "_graph" + ".asset");

                    XNode.NodeGraph graph = (XNode.NodeGraph)EntityGraph;

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    LayersObject layers = getLayers();

                    EntityAI EntityScript = EntityPrefab.GetComponent<EntityAI>();
                    EntityScript.entityData = (D_Entity)entityData;
                    EntityScript.entityData.whatIsObstacles |= (1 << layers.WhatIsGround);
                    EntityScript.entityData.whatIsTarget |= (1 << layers.WhatIsTarget);
                    EntityScript.entityGraph = graph as EntityGraph;
                    EntityScript.entityData.gameType = gameType;

                    if (gameType == D_Entity.GameType.Topdown2D)
                    {
                        EntityScript.GetComponent<Rigidbody2D>().gravityScale = 0f;
                        EntityScript.entityData.gameType = D_Entity.GameType.Topdown2D;
                    }

                    if (entityDir == Direction.Right)
                        EntityScript.initialFacingDirection = 1;
                    else
                        EntityScript.initialFacingDirection = -1;

                    Vector3 rotation = EntityPrefab.transform.rotation.eulerAngles;
                    rotation.y = EntityScript.initialFacingDirection == 1 ? 0.0f : 180f;

                    EntityPrefab.transform.rotation = Quaternion.Euler(rotation);

                    EntityPrefab.layer = entityLayer;

                    PrefabUtility.SaveAsPrefabAsset(EntityPrefab, AssetDatabase.GetAssetPath(targetFolder) + "/" + entityName + ".prefab");
                    PrefabUtility.UnloadPrefabContents(EntityPrefab);

                    AssetDatabase.SaveAssets();

                    AssetDatabase.Refresh();

                    Selection.activeObject = EntityPrefab;
                }
            }
            #endregion

            #region EditTab
            else if (tabs == 1)
            {
                entityPrefabField = (GameObject)EditorGUILayout.ObjectField("Select your Entity prefab: ", entityPrefabField, typeof(GameObject), false);

                if (entityPrefabField)
                {
                    try
                    {
                        if (PrefabUtility.IsPartOfPrefabAsset(entityPrefabField) && !isActive)
                        {
                            previousEntityPrefabField = entityPrefabField;

                            GameObject EntityPrefab = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(entityPrefabField));

                            if (EntityPrefab != null)
                            {
                                if (EntityPrefab.name.Contains("Template"))
                                {
                                    EditorGUILayout.HelpBox("Please, don't edit the template.", MessageType.Warning);

                                    return;
                                }

                                EntityPrefab.TryGetComponent(out currentEntityScript);

                                isActive = true;

                                if (currentEntityScript == null)
                                {
                                    isActive = false;
                                    PrefabUtility.UnloadPrefabContents(EntityPrefab);
                                    EditorGUILayout.HelpBox("Select a prefab with an Entity script", MessageType.Warning);
                                }
                            }
                            else
                            {
                                isActive = false;
                                PrefabUtility.UnloadPrefabContents(EntityPrefab);
                                EditorGUILayout.HelpBox("Select an Entity prefab", MessageType.Warning);
                            }
                        }
                        else
                        {
                            EditorGUILayout.HelpBox(entityPrefabField.name + " selected", MessageType.Info);

                            if (entityPrefabField != previousEntityPrefabField)
                            {
                                isActive = false;
                                previousEntityPrefabField = entityPrefabField;
                                ClearLists();
                            }
                        }
                    }
                    catch (System.ArgumentException)
                    {
                        isActive = false;
                        ClearLists();
                        Debug.Log("No prefab file selected");
                    }
                }
                else
                {
                    isActive = false;
                    ClearLists();
                }

                if (isActive)
                {
                    if (currentEntityScript == null)
                    {
                        isActive = false;
                        return;
                    }

                    if (currentEntityScript.entityData == null)
                    {
                        EditorGUILayout.HelpBox("No entity data found in " + entityPrefabField.name, MessageType.Error);
                        return;
                    }

                    Editor E = GetInspector(currentEntityScript.entityData);

                    GUIStyle style = new GUIStyle(GUI.skin.box)
                    {
                        fontSize = 20,
                        alignment = TextAnchor.MiddleCenter
                    };

                    EditorGUILayout.LabelField(entityPrefabField.name + " Data", style, GUILayout.ExpandWidth(true));

                    dataScrollsPos = GUILayout.BeginScrollView(dataScrollsPos);
                    E.OnInspectorGUI();

                    GUILayout.EndScrollView();

                    if (currentEntityScript.entityGraph != null)
                    {
                        if (GUILayout.Button("Edit Graph"))
                        {
                            if (NodeEditorWindow.current == null)
                                NodeEditorWindow.Open(currentEntityScript.entityGraph);
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("No Entity graph found in " + entityPrefabField.name, MessageType.Error);
                    }

                }
            }
            #endregion
        }
        public bool CheckEverythingIsOk(string EntityName)
        {
            if (EntityName.Length < 1)
            {
                EditorGUILayout.HelpBox("Name cannot be empty", MessageType.Error);
                return false;
            }

            if (targetFolder == null)
            {
                EditorGUILayout.HelpBox("Select a folder", MessageType.Error);
                return false;
            }

            return true;
        }

        public enum Direction
        {
            Left,
            Right
        }

        public void ClearLists()
        {
            inspectors.Clear();
        }

        private LayersObject getLayers()
        {
            var ids = AssetDatabase.FindAssets("LayersObject t:LayersObject");
            if (ids.Length > 0)
            {
                var lo = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(ids[0]));

                return (LayersObject)lo;
            }
            else
            {
                LayersObject lo = ScriptableObject.CreateInstance(typeof(LayersObject)) as LayersObject;

                AssetDatabase.CreateAsset(lo, "Assets");

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                return lo;
            }
        }

        private Editor GetInspector(Object target)
        {
            if (inspectors.Count > 0 && inspectors.ContainsKey(target))
            {
                if (inspectors[target].target == target)
                {
                    return inspectors[target];
                }
                else
                {
                    Editor ne = Editor.CreateEditor(target);
                    inspectors.Add(target, ne);

                    return ne;
                }
            }
            else
            {
                Editor ne = Editor.CreateEditor(target);
                inspectors.Add(target, ne);

                return ne;
            }
        }
    }
}