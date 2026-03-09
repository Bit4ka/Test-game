using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MaykerStudio
{
    class EntityGraphAssetModProcessor : UnityEditor.AssetModificationProcessor
    {
        private static bool IsRunning;

        /// <summary> Automatically re-add loose node assets to the Graph node list </summary>
        [InitializeOnLoadMethod]
        private static void OnReloadEditor()
        {
            IsRunning = true;

            // Find all NodeGraph assets
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(AI2DTool.EntityGraph));
            for (int i = 0; i < guids.Length; i++)
            {
                string assetpath = AssetDatabase.GUIDToAssetPath(guids[i]);
                AI2DTool.EntityGraph graph = AssetDatabase.LoadAssetAtPath(assetpath, typeof(AI2DTool.EntityGraph)) as AI2DTool.EntityGraph;
                graph.RegisteredNodes = (from kv in graph.RegisteredNodes
                                        where kv.Value != null
                                        select kv).ToDictionary(kv => kv.Key, kv => kv.Value); //Remove null items
                Object[] objs = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetpath);
                // Ensure that all sub node assets are present in the graph node list
                for (int u = 0; u < objs.Length; u++)
                {
                    // Ignore null sub assets
                    if (objs[u] == null) continue;


                    if(graph.RegisteredNodes.ContainsKey(objs[u].GetType()) && graph.RegisteredNodes.TryGetValue(objs[u].GetType(), out List<XNode.Node> list))
                    {
                        if(!list.Contains(objs[u] as XNode.Node))
                            list.Add(objs[u] as XNode.Node);
                    }
                    else
                    {
                        List<XNode.Node> newList = new List<XNode.Node>
                        {
                            objs[u] as XNode.Node
                        };

                        graph.RegisteredNodes.Add(objs[u].GetType(), newList);
                    }
                }
            }

            IsRunning = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void SafeCheck()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(AI2DTool.EntityGraph));
            for (int i = 0; i < guids.Length; i++)
            {
                string assetpath = AssetDatabase.GUIDToAssetPath(guids[i]);
                AI2DTool.EntityGraph graph = AssetDatabase.LoadAssetAtPath(assetpath, typeof(AI2DTool.EntityGraph)) as AI2DTool.EntityGraph;
                if(graph.RegisteredNodes.Count == 0 && !IsRunning)
                {
                    OnReloadEditor();

                    Debug.Log("Reloading EntityGraphs");
                    break;
                }
                else if(IsRunning)
                {
                    break;
                }
            }
        }
    }

}

