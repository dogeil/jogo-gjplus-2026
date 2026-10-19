using UnityEngine;
using UnityEngine.EventSystems;

public enum EstadoJanela { Aberta, Fechada, Cortina, Arame }

/// Janela. Aberta: um clique fecha, sem custo; fechada: clique não faz nada.
/// Cortina é um overlay arrastável (script Cortina) e só se move com a janela fechada; totalmente abaixada = estado Cortina.
/// Arame: overlay criado em runtime (sprite 19.2x10.8 alinhado à janela), por baixo da cortina; amarra o puxador.
/// Com o arame, o estado fica Arame e a cortina continua cobrindo/descobrindo ele sem mudar o estado.
/// Hover: contorno só com a janela aberta (única hora em que o clique faz algo).
public class Janela : ClickInteract, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] EstadoJanela estado = EstadoJanela.Aberta;
    [SerializeField] SpriteRenderer janelaRenderer; // se vazio, pega o do próprio objeto ou de um filho
    [SerializeField] Sprite spriteAberta;
    [SerializeField] Sprite spriteFechada; // também usado nos estados Cortina e Arame (são overlays)
    [SerializeField] Cortina cortina;
    [SerializeField] Sprite spriteArame;

    SpriteOutline outline;
    GameObject arameOverlay;

    public EstadoJanela Estado => estado;
    public bool CortinaBaixada => cortina != null && cortina.Baixada; // vale também com arame (o estado fica Arame)
    public void RecolherCortina() { if (cortina != null) cortina.Recolher(); }

    void Awake()
    {
        foreach (var c in GetComponents<Collider2D>()) c.isTrigger = true; // só detecta o mouse, não empurra itens
        if (janelaRenderer == null) janelaRenderer = GetComponentInChildren<SpriteRenderer>();
        if (janelaRenderer != null) outline = SpriteOutline.Attach(janelaRenderer);
        if (cortina != null) cortina.OnMoved += AoMoverCortina;
        CriarArame();
    }

    // Overlay logo acima da janela e abaixo da cortina (sobe a cortina se precisar).
    void CriarArame()
    {
        if (spriteArame == null || janelaRenderer == null) return;
        arameOverlay = new GameObject("Arame");
        arameOverlay.transform.SetParent(janelaRenderer.transform, false);
        var sr = arameOverlay.AddComponent<SpriteRenderer>();
        sr.sprite = spriteArame;
        sr.sortingLayerID = janelaRenderer.sortingLayerID;
        sr.sortingOrder = janelaRenderer.sortingOrder + 1;
        arameOverlay.SetActive(false);

        if (cortina == null) return;
        var cr = cortina.GetComponentInChildren<SpriteRenderer>();
        if (cr != null && cr.sortingOrder <= sr.sortingOrder) cr.sortingOrder = sr.sortingOrder + 1;
    }

    void OnDestroy()
    {
        if (cortina != null) cortina.OnMoved -= AoMoverCortina;
    }

    void Start() => Definir(estado);

    public override void Interact(GameObject interacted)
    {
        if (estado == EstadoJanela.Aberta) Definir(EstadoJanela.Fechada);
    }

    public void Definir(EstadoJanela novo)
    {
        estado = novo;
        if (janelaRenderer != null)
        {
            var s = estado == EstadoJanela.Aberta ? spriteAberta : spriteFechada;
            if (s != null) janelaRenderer.sprite = s;
        }
        if (outline != null) outline.Show(false); // fechou: some o contorno
        if (cortina != null) cortina.Travar(estado == EstadoJanela.Aberta);
        if (arameOverlay != null) arameOverlay.SetActive(estado == EstadoJanela.Arame);
    }

    public void ColocarArame() => Definir(EstadoJanela.Arame);

    public void OnPointerEnter(PointerEventData e) { if (outline != null && estado == EstadoJanela.Aberta) outline.Show(true); }
    public void OnPointerExit(PointerEventData e) { if (outline != null) outline.Show(false); }

    // Cortina no limite de baixo = Cortina; qualquer outra altura = Fechada
    void AoMoverCortina()
    {
        if (estado == EstadoJanela.Aberta || estado == EstadoJanela.Arame) return;
        estado = cortina.Baixada ? EstadoJanela.Cortina : EstadoJanela.Fechada;
    }
}
