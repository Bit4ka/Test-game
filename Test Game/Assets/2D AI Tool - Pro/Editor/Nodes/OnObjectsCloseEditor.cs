using XNodeEditor;
using UnityEngine;
using UnityEditor;
using System.Linq;
using XNode;
using AI2DTool;

namespace MaykerStudio
{
    [CustomNodeEditor(typeof(OnObjectsCloseNode))]
    public class OnObjectsCloseEditor : NodeEditor
    {
        private GUIStyle LabelStyle
        {
            get
            {
                if (_labelStyle == null)
                {
                    _labelStyle = new GUIStyle(EditorStyles.helpBox)
                    {
                        fontSize = 16,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter
                    };

                    _labelStyle.normal.textColor = Color.white;
                }

                return _labelStyle;
            }

        }

        private GUIStyle _labelStyle = null;

        private string objectTagValue;

        public override void OnBodyGUI()
        {
            serializedObject.Update();
            string[] excludes = { "m_Script", "graph", "position", "ports" };

            // Iterate through serialized properties and draw them like the Inspector (But with ports)
            SerializedProperty iterator = serializedObject.GetIterator();

            objectTagValue = serializedObject.FindProperty("ObjectTag").stringValue;

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

                if (iterator.name.Equals("ObjectTag"))
                    EditorGUILayout.LabelField(new GUIContent(objectTagValue), LabelStyle);

            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}