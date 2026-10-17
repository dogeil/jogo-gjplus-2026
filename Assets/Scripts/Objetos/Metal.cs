using UnityEngine;

public class Metal : ClickInteract
{
    public override void Interact(GameObject interacted)
    {
        InventoryHandler.Adicionar(Recurso.Metal);
        Destroy(interacted);
    }
}
