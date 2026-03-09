using AI2DTool;
using System.Linq;
using UnityEditor;
using UnityEngine;

using XNode;
using XNodeEditor;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MaykerStudio
{
    [CustomNodeEditor(typeof(OnPlayerInputNode))]

    public class OnPlayerInputEditor : NodeEditor
    {
#if ENABLE_INPUT_SYSTEM
        private InputActionAsset inputActionAsset;
        private InputAction inputAction;
#endif
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

#if ENABLE_INPUT_SYSTEM
                
                if (iterator.name.Equals(nameof(OnPlayerInputNode.InputActions)))
                {
                    if (inputActionAsset == null)
                        inputActionAsset = (InputActionAsset)iterator.objectReferenceValue;

                    if(inputAction == null && inputActionAsset != null)
                        inputAction = inputActionAsset.FindAction(serializedObject.FindProperty(nameof(OnPlayerInputNode.InteractActionName)).stringValue);

                    if(inputAction != null)
                        EditorGUILayout.LabelField(new GUIContent("Input: "+ inputAction.GetBindingDisplayString()), LabelStyle);
                }
#else
                if (iterator.name.Equals(nameof(OnPlayerInputNode.InteractInput)))
                {
                    EditorGUILayout.LabelField(new GUIContent("Input: " + System.Enum.GetValues(typeof(KeyCode)).GetValue(iterator.enumValueIndex)), LabelStyle);
                }
#endif

            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}