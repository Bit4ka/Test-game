using MaykerStudio.Demo;
using MaykerStudio.Help;
using UnityEditor;
using UnityEngine;

namespace MaykerStudio
{
    [CustomEditor(typeof(Readme))]
    [InitializeOnLoad]
    public class ReadmeEditor : Editor
    {
        static readonly float kSpace = 16f;

        private LayersObject layers;

        Vector2 scrollPos;

        static ReadmeEditor()
        {
            EditorApplication.delayCall += SelectReadmeAutomatically;
        }

        static void SelectReadmeAutomatically()
        {
            var readme = SelectReadme();

            if (readme && !readme.hasShowedOnce)
            {
                readme.hasShowedOnce = true;

                Selection.activeObject = readme;

                EditorGUIUtility.PingObject(Selection.activeObject);

                EditorUtility.SetDirty(readme);
            }
        }


        static Readme SelectReadme()
        {
            var ids = AssetDatabase.FindAssets("Readme t:Readme");
            if (ids.Length == 1)
            {
                var readmeObject = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(ids[0]));

                return (Readme)readmeObject;
            }
            else
            {
                Debug.LogWarning("Couldn't find a readme file");
                return null;
            }
        }

        [MenuItem("Tools/2D AI Tool/Help")]
        public static void SelectReadmeEditor()
        {
            var readme = SelectReadme();

            if (readme)
            {
                Selection.activeObject = readme;

                EditorGUIUtility.PingObject(Selection.activeObject);
            }
        }


        private void OnEnable()
        {
            layers = GetLayers();
        }

        public LayersObject GetLayers()
        {
            var ids = AssetDatabase.FindAssets("LayersObject t:LayersObject");
            if (ids.Length > 0)
            {
                Object lo = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(ids[0]));

                LayersObject t = (LayersObject)lo;

                return t;
            }
            else
            {
                LayersObject lo = CreateInstance(typeof(LayersObject)) as LayersObject;

                AssetDatabase.CreateAsset(lo, "Assets/LayersObject.asset");

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("A new layersObject was created, please check the readme.");

                return lo;
            }
        }

        protected override void OnHeaderGUI()
        {
            var readme = (Readme)target;
            Init();

            var iconWidth = Mathf.Min(EditorGUIUtility.currentViewWidth / 3f - 20f, 128f);

            GUILayout.BeginHorizontal("In BigTitle");
            {
                GUILayout.Label(readme.icon, GUILayout.Width(iconWidth), GUILayout.Height(iconWidth));
                GUILayout.Label(readme.title, TitleStyle);
            }
            GUILayout.EndHorizontal();
        }

        public override void OnInspectorGUI()
        {
            var readme = (Readme)target;
            Init();

            GUILayout.BeginScrollView(scrollPos);

            foreach (var section in readme.sections)
            {
                if (!string.IsNullOrEmpty(section.heading))
                {
                    GUILayout.Label(section.heading, HeadingStyle);
                }
                if (!string.IsNullOrEmpty(section.text))
                {
                    GUILayout.Label(section.text, BodyStyle);
                }
                if (!string.IsNullOrEmpty(section.linkText))
                {
                    if (LinkLabel(new GUIContent(section.linkText)))
                    {
                        Application.OpenURL(section.url);
                    }
                }
                GUILayout.Space(kSpace);
            }

            GUILayout.Label(new GUIContent("Setup Layers for Demo scenes", "The demo scene will get this values to set on enemies, platforms and player. Also, the Entity system Editor use this values too."), HeadingStyle);

            layers.EntityLayer = EditorGUILayout.LayerField(new GUIContent("Entity Layer", "The entites can be whatever you want, a enemy or npc, in the demo scene they are mostly enemies, so create a Enemy layer for it"), layers.EntityLayer);
            layers.PlayerLayer = EditorGUILayout.LayerField(new GUIContent("Player Layer"), layers.PlayerLayer);
            layers.EntityProjectiles = EditorGUILayout.LayerField(new GUIContent("Entity projectiles", "Projectiles should keep on different layers, so they don't get recognized as targets."), layers.EntityProjectiles);
            layers.PlayerProjectiles = EditorGUILayout.LayerField(new GUIContent("Player projectiles", "Projectiles should keep on different layers, so they don't get recognized as targets."), layers.PlayerProjectiles);

            layers.WhatIsGround = EditorGUILayout.LayerField(new GUIContent("What is grounds", "This layer is used for the enemies to detect grounds, walls, or any obstacle"), layers.WhatIsGround);
            layers.WhatIsTarget = EditorGUILayout.LayerField(new GUIContent("What is entities targets", "The default targets for entities, you can put the player layer for demo scene."), layers.WhatIsTarget);

            EditorUtility.SetDirty(layers);

            GUILayout.EndScrollView();
        }


        bool m_Initialized;

        GUIStyle LinkStyle { get { return m_LinkStyle; } }
        [SerializeField] GUIStyle m_LinkStyle;

        GUIStyle TitleStyle { get { return m_TitleStyle; } }
        [SerializeField] GUIStyle m_TitleStyle;

        GUIStyle HeadingStyle { get { return m_HeadingStyle; } }
        [SerializeField] GUIStyle m_HeadingStyle;

        GUIStyle BodyStyle { get { return m_BodyStyle; } }
        [SerializeField] GUIStyle m_BodyStyle;

        void Init()
        {
            if (m_Initialized)
                return;
            m_BodyStyle = new GUIStyle(EditorStyles.label);
            m_BodyStyle.wordWrap = true;
            m_BodyStyle.fontSize = 12;

            m_TitleStyle = new GUIStyle(m_BodyStyle);
            m_TitleStyle.fontSize = 26;

            m_HeadingStyle = new GUIStyle(m_BodyStyle);
            m_HeadingStyle.fontSize = 18;

            m_LinkStyle = new GUIStyle(m_BodyStyle);
            m_LinkStyle.wordWrap = false;
            // Match selection color which works nicely for both light and dark skins
            m_LinkStyle.normal.textColor = new Color(0x00 / 255f, 0x78 / 255f, 0xDA / 255f, 1f);
            m_LinkStyle.stretchWidth = false;

            m_Initialized = true;
        }

        bool LinkLabel(GUIContent label, params GUILayoutOption[] options)
        {
            var position = GUILayoutUtility.GetRect(label, LinkStyle, options);

            Handles.BeginGUI();
            Handles.color = LinkStyle.normal.textColor;
            Handles.DrawLine(new Vector3(position.xMin, position.yMax), new Vector3(position.xMax, position.yMax));
            Handles.color = Color.white;
            Handles.EndGUI();

            EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);

            return GUI.Button(position, label, LinkStyle);
        }
    }
}