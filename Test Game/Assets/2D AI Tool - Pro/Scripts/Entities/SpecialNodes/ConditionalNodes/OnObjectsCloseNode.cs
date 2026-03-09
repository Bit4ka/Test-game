using MaykerStudio;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("Special Nodes/Conditional Nodes/On Objects Close")]
    [NodeTint("#266e6a")]
    [NodeWidth(210)]
    public class OnObjectsCloseNode : OnEventNodeBase
    {
        [Help("Use the EntityInteractable component to trigger the transition.")]
        [BeginGroup("Options")]
        [SerializeField]
        [TagSelector]
        [EndGroup]
        private string ObjectTag;

        public override void TryTransitionToNextState(params object[] args)
        {
            if (entityAI.IsDead)
            {
                return;
            }

            if (args.Length > 0)
            {
                GameObject go = args[0] as GameObject;

                if (go != null && go.CompareTag(ObjectTag))
                {
                    StartTransition();
                }
            }
        }
    }
}