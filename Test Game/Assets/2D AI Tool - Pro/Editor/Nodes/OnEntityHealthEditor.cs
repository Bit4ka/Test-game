using AI2DTool;
using Toolbox;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;
using Toolbox.Editor;

namespace MaykerStudio
{
    [CustomNodeEditor(typeof(OnEntityHealthNode))]
    public class OnEntityHealthEditor : NodeEditor
    {
        private int healthTypeEnumValue;

        private GUIStyle LabelStyle
        {
            get
            {
                if(_labelStyle == null)
                {
                    _labelStyle = new GUIStyle(EditorStyles.helpBox)
                    {
                        fontSize = 15,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter
                    };

                    _labelStyle.normal.textColor = Color.white;
                }

                return _labelStyle;
            }

        }
        private GUIStyle _labelStyle = null;

        public override void OnBodyGUI()
        {
            serializedObject.Update();
            string[] excludes = { "m_Script", "graph", "position", "ports"};

            // Iterate through serialized properties and draw them like the Inspector (But with ports)
            SerializedProperty iterator = serializedObject.GetIterator();

            healthTypeEnumValue = serializedObject.FindProperty(nameof(OnEntityHealthNode.healthType)).enumValueIndex;

            bool enterChildren = true;
            Node node = serializedObject.targetObject as Node;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (excludes.Contains(iterator.name)) continue;

                EditorGUIUtility.labelWidth = 140;

                NodePort port = node.GetPort(iterator.name);

                if (port != null)
                    NodeEditorGUILayout.PropertyField(iterator, true);

                if (healthTypeEnumValue == 0 && iterator.name.Equals(nameof(OnEntityHealthNode.healthPercentage)))
                {
                    SerializedProperty p = serializedObject.FindProperty(nameof(OnEntityHealthNode.healthPercentage));

                    EditorGUILayout.LabelField(new GUIContent("On " + p.intValue.ToString() + "% of Health"), LabelStyle);
                }
                else if (healthTypeEnumValue == 1 && iterator.name.Equals(nameof(OnEntityHealthNode.healthValue)))
                {
                    SerializedProperty p = serializedObject.FindProperty(nameof(OnEntityHealthNode.healthValue));

                    EditorGUILayout.LabelField(new GUIContent("On "+ p.intValue.ToString() + " of Health"), LabelStyle);
                }

            }

            serializedObject.ApplyModifiedProperties();
        }
    }

    //[CustomEditor(typeof(OnEntityHealthNode))]
    public class OnEntityHealthCustomEditor : Toolbox.Editor.ToolboxEditor 
    {
       
    }

}