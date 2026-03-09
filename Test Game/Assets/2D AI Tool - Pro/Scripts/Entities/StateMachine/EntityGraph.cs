using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateAssetMenu]
    [RequireNode(typeof(FirstStateNode), typeof(DeadSateNode), typeof(StunStateNode))]
    public class EntityGraph : NodeGraph
    {
        [SerializeField]
        public Dictionary<Type, List<Node>> RegisteredNodes = new Dictionary<Type, List<Node>>();

        private void OnEnable()
        {
            if (RegisteredNodes.Count == 0 && nodes != null && nodes.Count > 0)
                RegisteredNodes = nodes.GroupBy(k => k.GetType(), v => v).ToDictionary(g => g.Key, g => g.ToList());
        }

        public EntityState GetFirstState()
        {
            FirstStateNode fsn = (FirstStateNode)RegisteredNodes[typeof(FirstStateNode)][0];

            if (fsn != null)
                return fsn.FirstState;

            return null;
        }

        public EntityState GetDeadState()
        {
            DeadSateNode dsn = (DeadSateNode)RegisteredNodes[typeof(DeadSateNode)][0];

            if (dsn != null)
                return dsn.DeadState;

            return null;
        }

        public EntityState GetStunState()
        {
            StunStateNode dsn = (StunStateNode)RegisteredNodes[typeof(StunStateNode)][0];

            if (dsn != null)
                return dsn.StunState;

            return null;
        }

        public List<Node> GetOnEventNodes()
        {
            List<Node> list = new List<Node>();

            for (int i = 0; i < RegisteredNodes.Count; i++)
            {
                if(RegisteredNodes.ElementAt(i).Key.BaseType == typeof(OnEventNodeBase))
                    list.AddRange(RegisteredNodes[RegisteredNodes.ElementAt(i).Key]);
            }

            return list;
        }

        public T GetState<T>() where T : EntityState
        {
            if (RegisteredNodes.TryGetValue(typeof(T), out List<Node> value))
            {
                return (T) value[0];
            }

            return default;
        }

        public override Node AddNode(Type type)
        {
            Node node = base.AddNode(type);

            if (RegisteredNodes.TryGetValue(type, out List<Node> list))
            {
                list.Add(node);
            }
            else
            {
                RegisteredNodes.Add(type, new List<Node>());
                RegisteredNodes[type].Add(node);
            }

            return node;
        }

        public override Node CopyNode(Node original)
        {
            Node node = base.CopyNode(original);

            if (RegisteredNodes.TryGetValue(original.GetType(), out List<Node> list))
            {
                list.Add(node);
            }
            else
            {
                RegisteredNodes.Add(original.GetType(), new List<Node>());
                RegisteredNodes[original.GetType()].Add(node);
            }

            return node;
        }

        public override void RemoveNode(Node node)
        {
            if (RegisteredNodes.TryGetValue(node.GetType(), out List<Node> list))
            {
                list.Remove(node);
            }

            base.RemoveNode(node);
        }
    }
}