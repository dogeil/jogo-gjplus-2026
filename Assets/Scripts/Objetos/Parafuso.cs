using UnityEngine;

public class Parafuso : ClickInteract
{
    public override void Interact(GameObject interacted)
    {
        InventoryHandler.Adicionar(Recurso.Parafuso);
        Destroy(interacted);
    }
}
