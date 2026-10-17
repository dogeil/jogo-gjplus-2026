using UnityEngine;

public class Parafuso : ClickInteract 
{
    public override void Interact(GameObject interacted)
    {
        Debug.Log(interacted.name + " foi adicionado ao Inventário!");
        // adicionar ao inventario
        Destroy(interacted);
    }
}