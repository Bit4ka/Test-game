using UnityEngine;
using UnityEngine.UI;
using MaykerStudio;

namespace MaykerStudio
{

#if ENABLE_INPUT_SYSTEM
    using UnityEngine.InputSystem;
#endif

    [RequireComponent(typeof(Text))]
    public class ShowInteractHint : MonoBehaviour
    {
#if ENABLE_INPUT_SYSTEM
        private InputAction interactAction;
#endif

        private Text text;

        public void Setup(EntityInput entityInput)
        {
            if (text == null)
                text = GetComponent<Text>();

#if ENABLE_INPUT_SYSTEM
            interactAction = entityInput.InteractAction;

            if (interactAction != null)
                text.text = "Press " + interactAction.GetBindingDisplayString() + " to interact with the entity";

#else

        text.text = "Press " + entityInput.InteractInput + " to interact with the entity";
#endif

        }
    }

}
