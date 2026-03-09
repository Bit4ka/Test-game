using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("Special Nodes/Multi Transition Node")]
    [NodeTint("#c42f2f")]
    public class MultiTransitionNode : EntityState
    {
        [BeginGroup("Options")]
        [EndGroup]
        [Tooltip("The InOrder will transition the state at the same order of the "+nameof(nextStates)+" list. Random_Unique will transition in random order without" +
            " the chance of repeating the next state.")]
        public TransitionType transitionType;

        [Output(ShowBackingValue.Never, ConnectionType.Multiple)]
        [ReorderableList(ListStyle.Round, elementLabel:"NextState", fixedSize: true)]
        [Tooltip("The values connected to this node. Please, do not change the values of the list manually, only the order.")]
        public List<EntityState> nextStates;

        private int nextIndex = 0;

        private List<int> indices;

        protected override void Init()
        {
            base.Init();

            indices = new List<int>();

            PopulateIndicesList();
        }

        public override void Enter()
        {
            base.Enter();

            for (int i = 0; i < nextStates.Count; i++)
            {
                nextStates[i] = CheckState(nextStates[i]);
            }

            if (nextStates.Count == 0)
            {
                Debug.LogError("No states found on MultiTransition node.");
                return;
            }

            switch (transitionType)
            {
                case TransitionType.InOrder:
                    if(nextIndex < nextStates.Count)
                    {
                        StateMachine.ChangeState(nextStates[nextIndex]);
                        nextIndex++;
                    }
                    else
                    {
                        StateMachine.ChangeState(nextStates[0]);
                        nextIndex = 1;
                    }
                    break;

                case TransitionType.Random_Unique:
                    if (indices.Count == 0)
                        PopulateIndicesList();

                    if (nextStates.Count < 2)
                    {
                        Debug.LogWarning("There aren't enough states for Random Unique");
                        return;
                    }

                    int index = indices[UnityEngine.Random.Range(0, indices.Count)];

                    StateMachine.ChangeState(nextStates[index]);

                    indices.Remove(index);

                    break;
                case TransitionType.Random:

                    StateMachine.ChangeState(nextStates[UnityEngine.Random.Range(0, nextStates.Count)]);

                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Function called by the xNode editor to change the list of nextStates. This method shouldn't be called in game.
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public override object GetValue(NodePort port)
        {
            List<NodePort> nodePorts = port.GetConnections();

            if (port.fieldName.Equals(nameof(nextStates)))
            {
                nextStates = nodePorts.ConvertAll(new Converter<NodePort, EntityState>(GetStatesNodes));
            }

            return nextStates;
        }

        private void PopulateIndicesList()
        {
            if(nextStates != null)
            {
                for (int i = 0; i < nextStates.Count; i++)
                {
                    indices.Add(i);
                }
            }
        }

        private EntityState GetStatesNodes(NodePort port)
        {
            return port.node as EntityState;
        }

        public enum TransitionType
        {
            InOrder,
            Random_Unique,
            Random
        }
    }
}