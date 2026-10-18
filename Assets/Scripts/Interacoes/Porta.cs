using System;
using System.Collections;
using UnityEngine;

/// Porta: martelo + recursos (configurar costs no Inspector: 2 Madeira, 8 Prego) -> barricada.
public class Porta : Interactable
{
    [SerializeField] string entranceId = "Porta";
    [SerializeField] GameObject barricadePrefab;

    // Ao arrombar, cai uma fração aleatória (min, max) do custo de cada recurso. 0 a 1; (1,1) = custo inteiro.
    [SerializeField] Vector2 breachDropFraction = new Vector2(0.3f, 0.7f);

    // Posição de cada prego, em unidades locais do pivô da barricada, na ordem das batidas.
    // Ajuste aqui até alinhar com os pregos do sprite (ou edite no Inspector; ele vence este default).
    [SerializeField] Vector2[] nailPositions =
    {
        new Vector2(-1.5f,  2f),
        new Vector2( 1.5f,  2f),
        new Vector2(-1.5f, -2f),
        new Vector2( 1.5f, -2f),
    };

    // Sprites do estado da porta: [0] intacta, [1] danificada, [2] quase destruída (3 estados).
    [SerializeField] Sprite[] damageSprites = new Sprite[3];
    [SerializeField] SpriteRenderer doorRenderer; // se vazio, pega o do próprio objeto ou de um filho

    bool barricaded;
    GameObject barricade;
    int damageState;

    protected override void Awake()
    {
        base.Awake();
        if (doorRenderer == null) doorRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public override Type RequiredTool => typeof(Martelo);
    protected override bool CanUse => !barricaded;

    // A barricada aparece quando o minigame começa; some se falhar.
    protected override void OnBegin()
    {
        barricade = Instantiate(barricadePrefab, transform.parent); // filho da superfície
        barricade.transform.localPosition = transform.localPosition;
    }

    protected override IEnumerator Minigame(Action<bool> done)
    {
        var points = new Vector3[nailPositions.Length];
        for (int i = 0; i < points.Length; i++)
            points[i] = barricade.transform.TransformPoint(nailPositions[i]);
        return NailHammer.Run(points, done);
    }

    protected override void OnSuccess()
    {
        barricaded = true;
        SecurityManager.Add(entranceId);
    }

    protected override void OnFail()
    {
        Destroy(barricade);
        base.OnFail();
    }

    bool shaking;

    // Treme a porta (e a barricada, se houver) em rajadas, como alguém forçando a entrada.
    IEnumerator Shake(float duration = 2f, float amp = 0.1f)
    {
        shaking = true;
        Vector3 doorOrigin = transform.localPosition;
        Vector3 barOrigin = barricade != null ? barricade.transform.localPosition : default;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = (1f - t / duration) * Mathf.Abs(Mathf.Sin(t * 9f)); // ~3 pancadas por segundo, perdendo força
            Vector3 off = (Vector3)(UnityEngine.Random.insideUnitCircle * amp * k);
            transform.localPosition = doorOrigin + off;
            if (barricade != null) barricade.transform.localPosition = barOrigin + off;
            yield return null;
        }
        transform.localPosition = doorOrigin;
        if (barricade != null) barricade.transform.localPosition = barOrigin;
        shaking = false;

        TakeDamage();
    }

    /// Barricada funciona como escudo de 1 dano: é consumida (some, parte dos recursos cai) e a porta não sofre.
    /// Sem barricada, a porta avança um estado de dano; dano no último estado perde a fase.
    public void TakeDamage()
    {
        if (barricaded)
        {
            barricaded = false;
            SecurityManager.Add(entranceId, -1);
            Destroy(barricade);
            DropCosts(breachDropFraction.x, breachDropFraction.y);
            return;
        }

        if (damageState >= damageSprites.Length - 1) { GameOver(); return; }
        SetDamageState(damageState + 1);
    }

    void SetDamageState(int state)
    {
        damageState = state;
        if (doorRenderer != null && damageSprites[state] != null) doorRenderer.sprite = damageSprites[state];
    }

    void GameOver() => GameManager.Lose($"Invadiram pela {entranceId}");

    // ponytail: debug via OnGUI, remover quando o inimigo disparar o Shake de verdade
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 130, 220, 40));
        if (!shaking && GUILayout.Button("Arrombar porta")) StartCoroutine(Shake());
        GUILayout.EndArea();
    }

    // Selecione a porta na cena: mostra os pregos (a barricada nasce no pivô da porta, então bate com o jogo).
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        foreach (var p in nailPositions)
            Gizmos.DrawWireSphere(transform.TransformPoint(p), 0.4f);
    }
}
