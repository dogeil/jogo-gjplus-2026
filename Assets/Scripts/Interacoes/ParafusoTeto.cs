using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// Um canto da luz do teto (filho do TetoLuz, com Collider2D pequeno em cima do furo).
/// Parafusadeira solta aqui, canto vazio e 1 Parafuso no inventário (custo no Inspector): o parafuso aparece grande
/// (mais perto da câmera) com contorno no hover; segurar o mouse nele o faz girar e diminuir até assentar.
/// Não tem como falhar; soltar o mouse só pausa. A câmera fica travada durante a tarefa.
public class ParafusoTeto : Interactable
{
    [SerializeField] TetoLuz luz;               // se vazio, o do pai
    [SerializeField] GameObject screwVisual;    // parafuso desenhado no canto; some quando cai
    [SerializeField] float holdTime = 2f;       // s segurando pra parafusar
    [SerializeField] Vector2 alarmOffset = new Vector2(0f, 0.6f); // marcador piscando acima do furo vazio

    const float FocusScale = 3f;   // tamanho inicial do parafuso "perto da câmera", em múltiplos do normal
    const float SpinTurns = 8f;    // voltas completas até assentar (mais voltas no mesmo holdTime = gira mais rápido)
    const int FocusOrderBoost = 20; // fica por cima do resto enquanto aparafusa

    SpriteRenderer marker;

    public bool Fallen { get; private set; }

    public override Type RequiredTool => typeof(Parafusadeira);
    protected override bool CanUse => Fallen;

    protected override void Awake()
    {
        base.Awake();
        if (luz == null) luz = GetComponentInParent<TetoLuz>();

        var go = new GameObject("Marker");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = alarmOffset;
        go.transform.localScale = Vector3.one * luz.AlarmScale;
        marker = go.AddComponent<SpriteRenderer>();
        marker.sprite = luz.AlarmSprite != null ? luz.AlarmSprite : NailHammer.Ring(); // sem sprite: anel vermelho
        if (luz.AlarmSprite == null) marker.color = Color.red;
        var src = screwVisual != null ? screwVisual.GetComponentInChildren<SpriteRenderer>() : null;
        marker.sortingLayerID = src != null ? src.sortingLayerID : 0;
        marker.sortingOrder = src != null ? src.sortingOrder + 1 : 10;
        marker.enabled = false;
    }

    void Update() => marker.enabled = Fallen && (int)(Time.time / luz.BlinkInterval) % 2 == 0;

    public void SetFallen(bool fallen)
    {
        Fallen = fallen;
        if (screwVisual != null) screwVisual.SetActive(!fallen);
    }

    protected override IEnumerator Minigame(Action<bool> done) => Hold(done);

    protected override void OnSuccess()
    {
        SetFallen(false);
        luz.OnRefilled();
    }

    // Cópia do parafuso do canto (ou um sprite novo, sem screwVisual) no lugar do furo.
    GameObject MakeFocus()
    {
        GameObject go;
        if (screwVisual != null)
        {
            go = Instantiate(screwVisual, screwVisual.transform.parent); // mantém posição/escala locais
            go.SetActive(true);
        }
        else
        {
            go = new GameObject("ScrewFocus");
            go.transform.SetParent(transform, false);
            go.AddComponent<SpriteRenderer>().sprite = luz.ScrewSprite;
        }
        return go;
    }

    IEnumerator Hold(Action<bool> done)
    {
        var cam = Camera.main;
        var focus = MakeFocus();
        var sr = focus.GetComponentInChildren<SpriteRenderer>();
        sr.sortingOrder += FocusOrderBoost;
        var outline = SpriteOutline.Attach(sr);

        Vector3 baseScale = focus.transform.localScale;
        Quaternion baseRot = focus.transform.localRotation;
        focus.transform.localScale = baseScale * FocusScale;
        Bounds hit = sr.bounds; // área clicável fixa (a do tamanho inicial), mesmo com o parafuso diminuindo

        float progress = 0f;
        while (progress < 1f && !GameManager.Lost)
        {
            yield return null;
            var mouse = Mouse.current;
            if (mouse == null) break;

            Vector3 wp = cam.ScreenToWorldPoint(mouse.position.ReadValue());
            bool over = hit.Contains(new Vector3(wp.x, wp.y, hit.center.z));
            outline.Show(over);
            if (over && mouse.leftButton.isPressed) progress += Time.deltaTime / holdTime;

            focus.transform.localScale = baseScale * Mathf.Lerp(FocusScale, 1f, progress);
            focus.transform.localRotation = baseRot * Quaternion.Euler(0f, 0f, -progress * 360f * SpinTurns); // horário
        }

        Destroy(focus);
        done(progress >= 1f);
    }
}
