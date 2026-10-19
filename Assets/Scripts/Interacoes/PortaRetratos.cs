using UnityEngine;
using UnityEngine.EventSystems;

/// Porta-retratos numa parede. Clicar: o jogador e forcado a olhar pro chao, onde uma mao (floorSprite, filho do Floor)
/// segura o porta-retratos. Enquanto olha o chao, ansiedade nao sobe e desce sozinha. Olhar pra cima devolve o item.
public class PortaRetratos : ClickInteract, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] GameObject floorSprite; // filho do Floor; comeca desativado
    [SerializeField] float calmPerSecond = 8f; // ansiedade removida por segundo enquanto ve o porta-retratos

    RoomView room;
    Renderer[] renderers;
    Collider2D col;
    SpriteOutline outline;
    bool held;

    protected override void Awake()
    {
        // sem base.Awake: o HoverOutline (ordem -1) some atras do fundo da parede; usa SpriteOutline como a Cortina
        room = FindFirstObjectByType<RoomView>();
        renderers = GetComponentsInChildren<SpriteRenderer>(); // antes do Attach, que cria as copias do contorno
        col = GetComponent<Collider2D>();
        outline = SpriteOutline.Attach(GetComponent<SpriteRenderer>());
        if (floorSprite != null) floorSprite.SetActive(false);
    }

    void OnEnable() { if (room != null) room.OnViewModeChanged += OnMode; }
    void OnDisable() { if (room != null) room.OnViewModeChanged -= OnMode; if (held) { held = false; Anxiety.Soothed = false; } }

    public override void Interact(GameObject interacted)
    {
        if (held || room == null || GameManager.Lost || Interactable.AnyBusy) return;
        if (room.IsTurning || room.Mode != ViewMode.Wall) return;
        SetHeld(true);
        room.LookDown();
    }

    // Chegou no chao: segue segurando. Voltou pra parede: devolve.
    void OnMode(ViewMode mode)
    {
        if (held && mode == ViewMode.Wall) SetHeld(false);
    }

    void Update()
    {
        if (held) Anxiety.Add(-calmPerSecond * Time.deltaTime);
    }

    public void OnPointerEnter(PointerEventData e) { if (!held) outline.Show(true); }
    public void OnPointerExit(PointerEventData e) => outline.Show(false);

    void SetHeld(bool value)
    {
        held = value;
        Anxiety.Soothed = value;
        foreach (var r in renderers) r.enabled = !value;
        if (col != null) col.enabled = !value;
        if (value) outline.Show(false);
        if (floorSprite != null) floorSprite.SetActive(value);
    }
}
