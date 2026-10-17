using UnityEngine;

public class Madeira : ClickInteract
{
    public override void Interact(GameObject interacted)
    {
        InventoryHandler.Adicionar(Recurso.Madeira);
        Destroy(interacted);
    }
}
