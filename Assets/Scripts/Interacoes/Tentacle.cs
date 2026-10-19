using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// Tentáculo de uma entrada. Um objeto por origem, filho da superfície, com SpriteRenderer e Collider2D cobrindo o sprite.
/// Door/Window: nasce no nível 1 e cresce a cada growTime (EnemyManager). Na janela, a partir do windowOpenLevel abre a janela à força.
/// No nível máximo encosta no jogador (ansiedade sobe, no EnemyManager) e o Floor aparece; enquanto algum está no máximo, os outros não chegam lá.
/// Martelo no tentáculo (ou no Floor) = NailHammer sem custo; vencer faz o do nível máximo recuar. Falhar não faz nada.
public class Tentacle : Interactable
{
    public enum Kind { Door, Window, Floor }

    [SerializeField] Kind kind;
    [SerializeField] Janela janela; // só Window
    [SerializeField] int hits = 2; // acertos do minigame, todos no ponto onde o martelo foi solto

    static readonly List<Tentacle> All = new List<Tentacle>();

    static readonly List<Vector2> shape = new List<Vector2>();

    SpriteRenderer sr;
    Collider2D col;
    Sprite fitted;
    float growAt;

    public int Level { get; private set; }

    public override Type RequiredTool => typeof(Martelo);
    protected override bool CanUse => kind == Kind.Floor ? MaxSource != null : Level > 0; // o do chão tem Level 0 sempre

    EnemyManager.Tentacles Cfg => EnemyManager.Instance.tentacles;

    /// Origem (não o chão) que está no nível máximo, ou null.
    public static Tentacle MaxSource
    {
        get
        {
            foreach (var t in All)
                if (t.kind != Kind.Floor && t.Level >= t.Cfg.maxLevel) return t;
            return null;
        }
    }

    /// Sorteia uma origem sem tentáculo e o faz aparecer.
    public static void SpawnRandom()
    {
        var idle = All.FindAll(t => t.kind != Kind.Floor && t.Level == 0);
        if (idle.Count > 0) idle[UnityEngine.Random.Range(0, idle.Count)].Spawn();
    }

    protected override void Awake()
    {
        base.Awake();
        sr = GetComponentInChildren<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Start() => Refresh();

    void Update()
    {
        if (EnemyManager.Instance == null || GameManager.Lost) return;
        if (kind == Kind.Floor) { Refresh(); return; }
        if (Level == 0 || Time.time < growAt) return;
        Grow();
    }

    void Spawn()
    {
        Anxiety.Add(Cfg.anxietyOnGrow);
        // Arame na janela: o tentáculo remove o arame em vez de aparecer
        if (kind == Kind.Window && janela != null && janela.Estado == EstadoJanela.Arame)
        {
            janela.Definir(EstadoJanela.Fechada);
            return;
        }
        SetLevel(1);
    }

    void Grow()
    {
        growAt = Time.time + Cfg.growTime;
        int next = Level + 1;
        if (next > Cfg.maxLevel) return;
        if (next == Cfg.maxLevel && MaxSource != null) return; // só um chega ao máximo por vez
        Anxiety.Add(Cfg.anxietyOnGrow);

        if (kind == Kind.Window && janela != null && next >= Cfg.windowOpenLevel && janela.Estado != EstadoJanela.Aberta)
        {
            if (janela.Estado == EstadoJanela.Arame) { janela.Definir(EstadoJanela.Fechada); return; } // o arame segura uma vez
            janela.Definir(EstadoJanela.Aberta);
        }
        SetLevel(next);
    }

    void Retreat(int levels) => SetLevel(Mathf.Max(0, Level - levels));

    void SetLevel(int level)
    {
        Level = level;
        growAt = Time.time + Cfg.growTime;
        Refresh();
    }

    void Refresh()
    {
        if (EnemyManager.Instance == null) return;
        bool on = kind == Kind.Floor ? MaxSource != null : Level > 0;
        sr.enabled = col.enabled = on;
        if (!on) return;

        Sprite s = kind == Kind.Floor ? Cfg.floorSprite : Pick(kind == Kind.Door ? Cfg.doorSprites : Cfg.windowSprites);
        if (s != null) sr.sprite = s; // sem sprite: fica o que já está no SpriteRenderer
        FitCollider();
    }

    // O PolygonCollider2D do editor só tem o contorno do sprite da hora; cada nível tem outro desenho
    void FitCollider()
    {
        if (!(col is PolygonCollider2D poly) || sr.sprite == null || sr.sprite == fitted) return;
        fitted = sr.sprite;
        int n = fitted.GetPhysicsShapeCount();
        poly.pathCount = n;
        for (int i = 0; i < n; i++)
        {
            fitted.GetPhysicsShape(i, shape);
            poly.SetPath(i, shape);
        }
    }

    Sprite Pick(Sprite[] byLevel) => byLevel != null && Level - 1 < byLevel.Length ? byLevel[Level - 1] : null;

    protected override IEnumerator Minigame(Action<bool> done)
    {
        // O minigame começa no frame do drop: o mouse ainda está onde soltou o martelo
        var p = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        p.z = transform.position.z;
        var points = new Vector3[hits];
        for (int i = 0; i < points.Length; i++) points[i] = p;
        return NailHammer.Run(points, done);
    }

    protected override void OnSuccess()
    {
        var target = kind == Kind.Floor ? MaxSource : this;
        if (target != null) target.Retreat(Cfg.retreatLevels);
    }

    protected override void OnFail() { } // sem custo, nada acontece
}
