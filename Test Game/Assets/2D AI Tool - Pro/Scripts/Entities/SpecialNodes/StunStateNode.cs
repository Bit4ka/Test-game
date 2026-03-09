using UnityEngine;
using XNode;

namespace AI2DTool
{
    [CreateNodeMenu("")]
    [NodeTint("#5c5535")]
    [DisallowMultipleNodes]
    public class StunStateNode : Node
    {
        [Output(ShowBackingValue.Never, ConnectionType.Override)]
        [Disable]
        public EntityState StunState;

        public override object GetValue(NodePort port)
        {
            StunState = (EntityState)(port.Connection?.node);

            return port.Connection?.node;
        }
    }
}