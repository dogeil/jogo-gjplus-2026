using UnityEngine;

public class Arame : ClickInteract
{
    public override void Interact(GameObject interacted)
    {
        InventoryHandler.Adicionar(Recurso.Arame);
        Destroy(interacted);
    }
}
