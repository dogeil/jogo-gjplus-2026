using UnityEngine;
using UnityEngine.EventSystems;

public enum EstadoJanela { Aberta, Fechada, Cortina, Arame }

/// Janela. Aberta: um clique fecha, sem custo; fechada: clique não faz nada.
/// Cortina é um overlay arrastável (script Cortina) e só se move com a janela fechada; totalmente abaixada = estado Cortina.
/// Arame ainda não existe (será outro overlay por cima).
/// Hover: contorno só com a janela aberta (única hora em que o clique faz algo).
public class Janela : ClickInteract, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] EstadoJanela estado = EstadoJanela.Aberta;
    [SerializeField] SpriteRenderer janelaRenderer; // se vazio, pega o do próprio objeto ou de um filho
    [SerializeField] Sprite spriteAberta;
    [SerializeField] Sprite spriteFechada; // também usado nos estados Cortina e Arame (são overlays)
    [SerializeField] Cortina cortina;

    SpriteOutline outline;

    public EstadoJanela Estado => estado;

    void Awake()
    {
        if (janelaRenderer == null) janelaRenderer = GetComponentInChildren<SpriteRenderer>();
        if (janelaRenderer != null) outline = SpriteOutline.Attach(janelaRenderer);
        if (cortina != null) cortina.OnMoved += AoMoverCortina;
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
    }

    public void OnPointerEnter(PointerEventData e) { if (outline != null && estado == EstadoJanela.Aberta) outline.Show(true); }
    public void OnPointerExit(PointerEventData e) { if (outline != null) outline.Show(false); }

    // Cortina no limite de baixo = Cortina; qualquer outra altura = Fechada
    void AoMoverCortina()
    {
        if (estado == EstadoJanela.Aberta) return;
        estado = cortina.Baixada ? EstadoJanela.Cortina : EstadoJanela.Fechada;
    }
}
