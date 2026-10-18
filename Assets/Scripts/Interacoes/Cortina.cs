using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// Cortina: overlay por cima da janela, arrastada para cima/baixo entre dois limites (Y local ao pai).
/// Vai num objeto filho da superfície da janela, com Collider2D cobrindo o sprite da cortina.
/// Arrastando, a câmera fica travada. Se a câmera se mexer com a cortina parcialmente baixada, ela sobe sozinha.
[RequireComponent(typeof(Collider2D))]
public class Cortina : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] float topY = 2f;     // Y local com a cortina recolhida
    [SerializeField] float bottomY = -2f; // Y local com a cortina totalmente abaixada
    [SerializeField] float riseSpeed = 8f; // unidades/s na subida automática

    public event Action OnMoved;

    public bool Baixada => Mathf.Abs(transform.localPosition.y - bottomY) < 0.01f;
    bool Recolhida => Mathf.Abs(transform.localPosition.y - topY) < 0.01f;

    Camera cam;
    Collider2D col;
    RoomView room;
    SpriteOutline outline;
    float dragOffset;
    bool dragging, rising, travada;

    void Awake()
    {
        cam = Camera.main;
        col = GetComponent<Collider2D>();
        foreach (var c in GetComponents<Collider2D>()) c.isTrigger = true; // só detecta o mouse, não empurra itens
        room = FindFirstObjectByType<RoomView>();
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) outline = SpriteOutline.Attach(sr);
    }

    void Update()
    {
        // Olhou pro lado com a cortina no meio do caminho: sobe completamente
        if (!dragging && room != null && room.IsMoving && !Baixada && !Recolhida) rising = true;

        if (rising)
        {
            SetY(Mathf.MoveTowards(transform.localPosition.y, topY, riseSpeed * Time.deltaTime));
            if (Recolhida) rising = false;
        }
    }

    /// Janela aberta: cortina sobe à força e não pode ser arrastada.
    public void Travar(bool travada)
    {
        this.travada = travada;
        col.enabled = !travada;
        if (travada)
        {
            rising = false;
            SetY(topY);
            if (outline != null) outline.Show(false);
        }
    }

    float PointerLocalY(PointerEventData e) =>
        transform.parent.InverseTransformPoint(cam.ScreenToWorldPoint(e.position)).y;

    public void OnBeginDrag(PointerEventData e)
    {
        if (Interactable.AnyBusy || (room != null && room.Locked)) return; // minigame rodando: ignora
        dragging = true;
        rising = false;
        if (room != null) room.Locked = true; // arrastando: não dá pra olhar pros lados
        dragOffset = transform.localPosition.y - PointerLocalY(e);
    }

    public void OnDrag(PointerEventData e) { if (dragging) SetY(PointerLocalY(e) + dragOffset); }

    public void OnEndDrag(PointerEventData e)
    {
        if (!dragging) return;
        dragging = false;
        if (room != null) room.Locked = false;
    }

    public void OnPointerEnter(PointerEventData e) { if (outline != null && !travada) outline.Show(true); }
    public void OnPointerExit(PointerEventData e) { if (outline != null) outline.Show(false); }

    void SetY(float y)
    {
        var p = transform.localPosition;
        p.y = Mathf.Clamp(y, Mathf.Min(topY, bottomY), Mathf.Max(topY, bottomY));
        transform.localPosition = p;
        OnMoved?.Invoke();
    }
}
