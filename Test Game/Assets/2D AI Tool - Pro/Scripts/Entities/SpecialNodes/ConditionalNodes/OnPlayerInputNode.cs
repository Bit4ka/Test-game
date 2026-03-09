using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AI2DTool
{
    [CreateNodeMenu("Special Nodes/Conditional Nodes/On Player Input")]
    [NodeTint("#949400")]
    [NodeWidth(210)]
    public class OnPlayerInputNode : OnEventNodeBase
    {
#if ENABLE_INPUT_SYSTEM
        [NotNull]
        [BeginGroup("Player input")]
        [Help("Use the EntityInput component to set up input, player tag, and layer.")]
        public InputActionAsset InputActions;

        [EndGroup]
        public string InteractActionName = "Interact";
#else
        [BeginGroup("Player input")]
        [Help("Use the EntityInput component to set up input, player tag, and layer.")]
        [EndGroup]
        [SearchableEnum]
        public KeyCode InteractInput = KeyCode.E;
#endif

        public override void TryTransitionToNextState(params object[] args) 
        {
            if (entityAI.IsDead)
            {
                return;
            }

            if (args.Length > 0)
            {
                try
                {
#if ENABLE_INPUT_SYSTEM
                    InputAction action = (InputAction)args[0];

                    if(action != null && action.name.Equals(InteractActionName))
                    {
                        StartTransition();
                    }
#else
                    KeyCode keyCode = (KeyCode)args[0];

                    if(keyCode == InteractInput)
                    {
                        StartTransition();
                    }
#endif
                }
                catch (System.InvalidCastException)
                {
                    Debug.LogWarning("Wrong params passed to "+name);
                }

            }
            
        }
    }
}