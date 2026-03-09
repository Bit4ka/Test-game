using AI2DTool;
using System.Linq;
using UnityEditor;
using XNode;
using XNodeEditor;

namespace MaykerStudio
{
    [CustomNodeEditor(typeof(EntityState))]
    public class EntityNodeEditor : NodeEditor
    {
        private float stateDurationValue = 0;

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

                if (port != null && !port.fieldName.Equals(nameof(EntityState.TimeIsOver)))
                    NodeEditorGUILayout.PropertyField(iterator, true);
                else if(port != null && port.fieldName.Equals(nameof(EntityState.TimeIsOver)))
                {
                    stateDurationValue = serializedObject.FindProperty(nameof(EntityState.stateDuration)).floatValue;

                    if(stateDurationValue > 0)
                        NodeEditorGUILayout.PropertyField(iterator, true);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}