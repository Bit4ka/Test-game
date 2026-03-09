using UnityEngine;

public interface IInteractable
{
    public bool CanInteract(GameObject interactor);

    public void Interact(GameObject interactor);
}
