using UnityEngine;
using UnityEngine.EventSystems;

/// Gaveta numa parede. Clicar: o jogador e forcado a olhar pro chao, onde drawerPrefab (sprite + colliders de parede
/// + pregos dentro) aparece como filho do Floor. Olhar pra cima guarda a gaveta; pregos que ficaram dentro continuam la
/// na proxima vez. Pregos arrastados pra fora ficam no chao como qualquer item.
public class Gaveta : ClickInteract, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] GameObject drawerPrefab; // posicao/escala locais do prefab = onde aparece no chao

    [Header("Pregos")]
    [SerializeField] GameObject nailPrefab;   // o mesmo prefab Prego dos recursos
    [SerializeField, Min(0)] int nailCount = 20;
    [SerializeField] Vector2 nailAreaCenter;  // area onde os pregos nascem, local a gaveta
    [SerializeField] Vector2 nailAreaSize = new Vector2(3f, 1.5f);

    RoomView room;
    Renderer[] renderers;
    Collider2D col;
    SpriteOutline outline;
    GameObject drawer;
    bool open;

    protected override void Awake()
    {
        // sem base.Awake: o HoverOutline (ordem -1) some atras do fundo da parede; usa SpriteOutline como a Cortina
        room = FindFirstObjectByType<RoomView>();
        renderers = GetComponentsInChildren<SpriteRenderer>(); // antes do Attach, que cria as copias do contorno
        col = GetComponent<Collider2D>();
        outline = SpriteOutline.Attach(GetComponent<SpriteRenderer>());
    }

    void OnEnable() { if (room != null) room.OnViewModeChanged += OnMode; }
    void OnDisable() { if (room != null) room.OnViewModeChanged -= OnMode; }
    public override void Interact(GameObject interacted)
    {
        if (open || room == null || drawerPrefab == null || GameManager.Lost || Interactable.AnyBusy) return;
        if (room.IsTurning || room.Mode != ViewMode.Wall) return;
        if (drawer == null) // pregos dentro persistem entre aberturas: so instancia na primeira
        {
            drawer = Instantiate(drawerPrefab, room.GetSurface(room.FloorIndex));
            FillNails();
            Physics2D.SyncTransforms();
        }
        SetOpen(true);
        room.LookDown();
    }

    // Pregos como filhos da gaveta: somem/voltam com ela. Sem gravidade: DraggableObject ve que estao no chao.
    void FillNails()
    {
        if (nailPrefab == null) return;
        var drawerSr = drawer.GetComponent<SpriteRenderer>();
        for (int i = 0; i < nailCount; i++)
        {
            var nail = Instantiate(nailPrefab, drawer.transform);
            if (drawerSr != null && nail.TryGetComponent(out SpriteRenderer sr))
            {
                sr.sortingLayerID = drawerSr.sortingLayerID;
                sr.sortingOrder = drawerSr.sortingOrder + 2; // +1 e do contorno do HoverOutline (ordem - 1)
            }
            nail.transform.localPosition = new Vector3(
                nailAreaCenter.x + Random.Range(-nailAreaSize.x, nailAreaSize.x) / 2f,
                nailAreaCenter.y + Random.Range(-nailAreaSize.y, nailAreaSize.y) / 2f,
                nail.transform.localPosition.z);
            nail.transform.Rotate(0f, 0f, Random.Range(0f, 360f));
        }
    }

    // Chegou no chao: segue aberta. Voltou pra parede: guarda.
    void OnMode(ViewMode mode)
    {
        if (open && mode == ViewMode.Wall) SetOpen(false);
    }

    public void OnPointerEnter(PointerEventData e) { if (!open) outline.Show(true); }
    public void OnPointerExit(PointerEventData e) => outline.Show(false);

    void SetOpen(bool value)
    {
        open = value;
        foreach (var r in renderers) r.enabled = !value;
        if (col != null) col.enabled = !value;
        if (value) outline.Show(false);
        if (drawer != null) drawer.SetActive(value);
    }
}
