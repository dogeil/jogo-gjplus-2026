using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Porta de trás. Ataque (por ora disparado pelo botão de debug): aviso piscando no canto da tela, porta tremendo.
/// O jogador gira e clica na porta a tempo -> mash pra segurá-la. Não clicou a tempo ou perdeu o mash = brecha.
/// Arame (alicate + Arame, via WireTwist) posto antes é escudo de 1 brecha: some e evita a derrota.
public class PortaTraseira : Interactable, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] string entranceId = "PortaTraseira";
    [SerializeField] Transform handle;          // onde fica o anel do arame; se vazio, o próprio objeto
    [SerializeField] SpriteRenderer doorRenderer; // se vazio, pega o do próprio objeto ou de um filho
    [SerializeField] Sprite wireSprite;         // overlay do arame, alinhado ao sprite da porta

    [Header("Ataque")]
    [SerializeField] float reactionTime = 4f;   // s pra clicar na porta depois do aviso
    [SerializeField] float shakeAmp = 0.08f;
    [SerializeField] Sprite alertSprite;        // aviso no canto; sem sprite vira um quadrado vermelho
    [SerializeField] float alertScale = 0.33f;  // escala do aviso sobre o tamanho nativo do sprite
    [SerializeField] float blinkInterval = 0.3f;
    [SerializeField] AudioClip stepsClip;       // passos altos; sem clipe fica mudo

    [Header("Mash")]
    [SerializeField] float mashTime = 6f;
    [SerializeField] float mashPerClick = 0.07f; // fração da barra por clique
    [SerializeField] float mashDrain = 0.3f;     // fração da barra perdida por segundo

    const float AlertMargin = 40f;
    const float FallbackAlertSize = 120f;

    SpriteOutline outline;
    GameObject wireOverlay;
    int doorWall;
    bool wired, attacking, clicked, hover;

    public override Type RequiredTool => typeof(Alicate);
    protected override bool CanUse => !wired && !attacking;
    protected override bool LockRoom => false; // olhar pro lado cancela o minigame (WireTwist)

    protected override void Awake()
    {
        base.Awake();
        if (doorRenderer == null) doorRenderer = GetComponentInChildren<SpriteRenderer>();
        if (doorRenderer != null) outline = SpriteOutline.Attach(doorRenderer);
        if (wireSprite != null && doorRenderer != null)
        {
            wireOverlay = new GameObject("Arame");
            wireOverlay.transform.SetParent(doorRenderer.transform, false);
            var sr = wireOverlay.AddComponent<SpriteRenderer>();
            sr.sprite = wireSprite;
            sr.sortingLayerID = doorRenderer.sortingLayerID;
            sr.sortingOrder = doorRenderer.sortingOrder + 2; // +1 é o do contorno
            wireOverlay.SetActive(false);
        }
    }

    void Start() => doorWall = room != null ? room.IndexOfSurface(transform.parent) : 0;

    protected override IEnumerator Minigame(Action<bool> done) =>
        WireTwist.Run((handle != null ? handle : transform).position, room, done);

    protected override void OnSuccess()
    {
        wired = true;
        SecurityManager.Add(entranceId);
        if (wireOverlay != null) wireOverlay.SetActive(true);
    }

    public void Attack()
    {
        if (!attacking && !AnyBusy && !GameManager.Lost) StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        attacking = true;
        clicked = false;
        if (stepsClip != null) AudioSource.PlayClipAtPoint(stepsClip, Camera.main.transform.position);

        var alertCanvas = new GameObject("DoorAlert", typeof(Canvas));
        alertCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        alertCanvas.GetComponent<Canvas>().sortingOrder = 90;
        var alert = MakeAlert(alertCanvas.transform);

        Vector3 origin = transform.localPosition;
        for (float t = 0f; t < reactionTime && !clicked; t += Time.deltaTime)
        {
            transform.localPosition = origin + (Vector3)(UnityEngine.Random.insideUnitCircle * shakeAmp);
            UpdateAlert(alert);
            if (outline != null) outline.Show(hover);
            yield return null;
        }
        transform.localPosition = origin;
        Destroy(alertCanvas);
        if (outline != null) outline.Show(false);

        if (!clicked) // não reagiu a tempo
        {
            Breach();
        }
        else
        {
            if (room != null) room.Locked = true;
            bool won = false;
            yield return DoorMash.Run(transform.position, mashTime, mashPerClick, mashDrain, r => won = r);
            if (room != null) room.Locked = false;
            if (!won) Breach();
        }
        attacking = false;
    }

    // Arame é escudo de 1 brecha; sem ele, perde a fase.
    void Breach()
    {
        if (wired)
        {
            wired = false;
            SecurityManager.Add(entranceId, -1);
            if (wireOverlay != null) wireOverlay.SetActive(false);
            return;
        }
        GameManager.Lose("Invadiram pela porta de trás");
    }

    Image MakeAlert(Transform parent)
    {
        var img = new GameObject("Alert", typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(parent, false);
        img.raycastTarget = false;
        img.rectTransform.localScale = Vector3.one * alertScale;
        if (alertSprite != null) { img.sprite = alertSprite; img.SetNativeSize(); }
        else { img.color = Color.red; img.rectTransform.sizeDelta = Vector2.one * FallbackAlertSize; }
        return img;
    }

    // Canto do lado do giro mais curto até a porta; escondido se a porta já está na tela. Pisca.
    void UpdateAlert(Image alert)
    {
        int n = room.FloorIndex; // = número de paredes
        int diff = (doorWall - room.CurrentWall + n) % n;
        bool left = diff == n - 1;
        var rt = alert.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(left ? 0f : 1f, 1f);
        rt.anchoredPosition = new Vector2(left ? AlertMargin : -AlertMargin, -AlertMargin);
        alert.enabled = diff != 0 && (int)(Time.time / blinkInterval) % 2 == 0;
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (attacking && !clicked) clicked = true;
    }

    public void OnPointerEnter(PointerEventData e) => hover = true;
    public void OnPointerExit(PointerEventData e) => hover = false;

    // ponytail: debug via OnGUI, remover quando o inimigo disparar Attack() de verdade
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 170, 220, 40));
        if (!attacking && GUILayout.Button("Atacar porta traseira")) Attack();
        GUILayout.EndArea();
    }
}
