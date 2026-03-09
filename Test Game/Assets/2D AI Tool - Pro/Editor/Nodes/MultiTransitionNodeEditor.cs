using System.Linq;
using UnityEditor;
using XNode;
using XNodeEditor;
using Toolbox;
using Toolbox.Editor;

namespace AI2DTool
{
    [CustomNodeEditor(typeof(MultiTransitionNode))]
    public class MultiTransitionNodeEditor : NodeEditor
    {
        public override void OnBodyGUI()
        {
            serializedObject.Update();
            string[] excludes = { "m_Script", "graph", "position", "ports",  };

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

                if (port != null && !port.fieldName.Equals("TimeIsOver"))
                    NodeEditorGUILayout.PropertyField(iterator, true);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}