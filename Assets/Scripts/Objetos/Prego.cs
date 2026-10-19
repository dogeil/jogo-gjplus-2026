using UnityEngine;

public class Prego : ClickInteract
{
    // Chão (e gaveta dentro dele) não tem gravidade; nas paredes vale a do prefab
    void Start()
    {
        var room = FindFirstObjectByType<RoomView>();
        if (room != null && room.IsOnFloor(transform) && TryGetComponent(out Rigidbody2D rb))
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    public override void Interact(GameObject interacted)
    {
        InventoryHandler.Adicionar(Recurso.Prego);
        Destroy(interacted);
    }
}
