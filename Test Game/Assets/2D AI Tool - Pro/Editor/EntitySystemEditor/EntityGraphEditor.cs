using AI2DTool;
using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;

namespace MaykerStudio
{
    [CustomNodeGraphEditor(typeof(EntityGraph))]
    public class EntityGraphEditor : NodeGraphEditor
    {
        Color inputColor = Color.blue;

        public override void OnGUI()
        {
            if (!NodeEditorPreferences.GetSettings().autoSave)
            {
                GUIStyle style = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold
                }
                ;

                GUILayout.BeginArea(new Rect(Screen.width / 2, 10f, Screen.width, 30));

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Save", style, GUILayout.Width(60)))
                {
                    AssetDatabase.SaveAssets();
                }

                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }
        }

        public override Color GetPortColor(NodePort port)
        {
            if (port.IsInput)
            {
                return inputColor;
            }
            else
            {
                return GetTypeColor(port.ValueType);
            }
        }

        public override void OnDropObjects(Object[] objects)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                Object o = objects[i];

                if (o.GetType().BaseType == typeof(AI2DTool.EntityState))
                {
                    // Vector2 pos = NodeEditorWindow.current.WindowToGridPosition(Event.current.mousePosition);
                    CopyNode(o as Node);
                }
            }
        }

        public override void OnWindowFocusLost()
        {
            base.OnWindowFocusLost();

            AssetDatabase.SaveAssets();
        }
    }


}

