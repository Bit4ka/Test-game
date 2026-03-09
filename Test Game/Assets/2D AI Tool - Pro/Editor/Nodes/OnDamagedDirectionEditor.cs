using XNodeEditor;
using UnityEngine;
using UnityEditor;
using System.Linq;
using XNode;
using AI2DTool;

namespace MaykerStudio
{
    [CustomNodeEditor(typeof(OnDamagedDirectionNode))]
    public class OnDamagedDirectionEditor : NodeEditor
    {
        private int facingAndDamageValue = 0;

        private GUIStyle LabelStyle
        {
            get
            {
                if (_labelStyle == null)
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
            string[] excludes = { "m_Script", "graph", "position", "ports" };

            // Iterate through serialized properties and draw them like the Inspector (But with ports)
            SerializedProperty iterator = serializedObject.GetIterator();

            facingAndDamageValue = serializedObject.FindProperty(nameof(OnDamagedDirectionNode.facingAndDamage)).enumValueIndex;
     
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

                if (facingAndDamageValue == 0 && iterator.name.Equals(nameof(OnDamagedDirectionNode.facingAndDamage)))
                    EditorGUILayout.LabelField(new GUIContent("Oposite directions"), LabelStyle);

                else if(facingAndDamageValue == 1 && iterator.name.Equals(nameof(OnDamagedDirectionNode.facingAndDamage)))
                    EditorGUILayout.LabelField(new GUIContent("Same directions"), LabelStyle);

                else if(iterator.name.Equals(nameof(OnDamagedDirectionNode.facingAndDamage)))
                    EditorGUILayout.LabelField(new GUIContent("Any"), LabelStyle);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}