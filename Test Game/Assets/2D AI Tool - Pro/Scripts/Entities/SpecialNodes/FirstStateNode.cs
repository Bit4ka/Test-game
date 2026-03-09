using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("")]
    [NodeTint("#6e6e6e")]
    [DisallowMultipleNodes]
    public class FirstStateNode : Node
    {
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [Disable]
        public EntityState FirstState;

        public override object GetValue(NodePort port)
        {
            FirstState = (EntityState)(port.Connection?.node);

            return port.Connection?.node;
        }
    }
}