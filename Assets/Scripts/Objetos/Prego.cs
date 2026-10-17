using UnityEngine;

public class Prego : ClickInteract
{
    public override void Interact(GameObject interacted)
    {
        InventoryHandler.Adicionar(Recurso.Prego);
        Destroy(interacted);
    }
}
