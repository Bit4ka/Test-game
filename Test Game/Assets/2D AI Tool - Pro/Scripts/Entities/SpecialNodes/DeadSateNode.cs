using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("")]
    [NodeTint("#363636")]
    [DisallowMultipleNodes]
    public class DeadSateNode : Node
    {
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [Disable]
        public EntityState DeadState;

        public override object GetValue(NodePort port)
        {
            DeadState = (EntityState)(port.Connection?.node);

            return port.Connection?.node;
        }
    }
}