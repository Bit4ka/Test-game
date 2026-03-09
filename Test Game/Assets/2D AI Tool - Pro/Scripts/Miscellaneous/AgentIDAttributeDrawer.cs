#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using MaykerStudio.Attributes;
using NavMeshPlus.Components.Editors;
using Toolbox.Editor.Drawers;
using UnityEditor;
using UnityEditor.AI;
using UnityEngine;

namespace MaykerStudio
{

    [CustomPropertyDrawer(typeof(AgentIDAttribute))]
    public class AgentIDAttributeDrawer : ToolboxSelfPropertyDrawer<AgentIDAttribute>
    {
        public override bool IsPropertyValid(SerializedProperty property)
        {
            return property.propertyType == SerializedPropertyType.Integer;
        }

        protected override void OnGuiSafe(SerializedProperty property, GUIContent label, AgentIDAttribute attribute)
        {
            EditorGUILayout.Space(30);
            NavMeshComponentsGUIUtility.AgentTypePopup(new Rect(18f, 26f, 622, 18), "Agent Type", property);
        }
    }
    #endif
}

