using System;
using UnityEngine;

/// Inimigos, num GameObject vazio da cena. Cada bloco tem liga/desliga e intervalos em segundos, tudo no Inspector.
/// Ativos por enquanto: arrombador da porta principal, corredor da porta traseira, vulto da janela e tentáculos (ver Tentacle).
public class EnemyManager : MonoBehaviour
{
    [Serializable]
    public struct Interval
    {
        public float min, max;
        public float Roll() => UnityEngine.Random.Range(min, max);
    }

    [Serializable]
    public class DoorBreaker
    {
        public bool enabled = true;
        [Tooltip("Tempo entre tentativas de arrombar")] public Interval interval = new Interval { min = 30f, max = 60f };
        [Tooltip("Dano na porta por tentativa (barricada absorve; sem barricada perde)")] public int damage = 1;
    }

    [Serializable]
    public class BackDoorRunner
    {
        public bool enabled = true;
        [Tooltip("Tempo entre ataques na porta traseira")] public Interval interval = new Interval { min = 40f, max = 80f };
    }

    [Serializable]
    public class WindowGhost
    {
        public bool enabled = true;
        [Tooltip("Tempo entre aparições (num sorteio entre as janelas sem arame; só aparece com a parede fora da tela)")] public Interval interval = new Interval { min = 25f, max = 50f };
        [Tooltip("Segundos com a janela aberta e o vulto lá até perder (zera se a janela fechar)")] public float loseTime = 5f;
        [Tooltip("Ansiedade por segundo enquanto ele está lá")] public float anxietyPerSecond = 5f;
        [Tooltip("Nasce filho da parede com o transform do próprio prefab (posição, rotação, escala). Ajuste a ordem de desenho no próprio prefab")] public GameObject prefab;
    }

    [Serializable]
    public class Tentacles
    {
        public bool enabled = true;
        [Tooltip("Tempo entre aparições de um tentáculo novo (porta principal e janelas)")] public Interval spawnInterval = new Interval { min = 20f, max = 40f };
        [Tooltip("Segundos pra crescer 1 nível")] public float growTime = 15f;
        [Tooltip("Nível máximo; nele encosta no jogador e aparece no chão")] public int maxLevel = 3;
        [Tooltip("Nível a partir do qual o tentáculo da janela a abre à força")] public int windowOpenLevel = 2;
        [Tooltip("Ansiedade por segundo com um tentáculo no nível máximo")] public float anxietyPerSecond = 15f;
        [Tooltip("Ansiedade a cada vez que um tentáculo surge ou cresce 1 nível")] public float anxietyOnGrow = 4f;
        [Tooltip("Níveis que o tentáculo recua ao vencer o minigame")] public int retreatLevels = 1;
        [Tooltip("Sprite por nível (índice 0 = nível 1)")] public Sprite[] doorSprites;
        public Sprite[] windowSprites;
        [Tooltip("Aparece no chão quando algum tentáculo chega ao nível máximo")] public Sprite floorSprite;
    }

    public DoorBreaker doorBreaker;
    public BackDoorRunner backDoor;
    public WindowGhost windowGhost;
    public Tentacles tentacles;

    [Header("Alvos (vazio = acha na cena)")]
    [SerializeField] Porta frontDoor;
    [SerializeField] PortaTraseira rearDoor;

    public static EnemyManager Instance { get; private set; }

    [SerializeField] Janela leftWindow;  // vazios = acha pela parede (nome com "esq" / "dir")
    [SerializeField] Janela rightWindow;

    // Um vulto por janela. waiting = quer aparecer e espera poder (parede fora da tela).
    class GhostSlot
    {
        public Janela window;
        public int wall;
        public GameObject sprite;
        public float openFor;
        public bool waiting;
        public bool Present => sprite != null;
    }

    float nextBreak, nextRun, nextTentacle, nextGhost;
    RoomView room;
    GhostSlot left, right;
    GhostSlot[] ghosts;

    void Awake() => Instance = this;

    void Start()
    {
        if (frontDoor == null) frontDoor = FindFirstObjectByType<Porta>();
        if (rearDoor == null) rearDoor = FindFirstObjectByType<PortaTraseira>();
        room = FindFirstObjectByType<RoomView>();
        foreach (var w in FindObjectsByType<Janela>(FindObjectsSortMode.None))
        {
            int i = WallOf(w);
            if (i < 0) continue;
            string wall = room.GetSurface(i).name.ToLowerInvariant();
            if (leftWindow == null && wall.Contains("esq")) leftWindow = w;
            else if (rightWindow == null && wall.Contains("dir")) rightWindow = w;
        }
        left = MakeSlot(leftWindow);
        right = MakeSlot(rightWindow);
        ghosts = new[] { left, right };

        nextBreak = Time.time + doorBreaker.interval.Roll();
        nextRun = Time.time + backDoor.interval.Roll();
        nextTentacle = Time.time + tentacles.spawnInterval.Roll();
        nextGhost = Time.time + windowGhost.interval.Roll();
    }

    GhostSlot MakeSlot(Janela w) => new GhostSlot { window = w, wall = w != null ? WallOf(w) : -1 };

    // Parede da janela: sobe na hierarquia até achar uma superfície
    int WallOf(Janela w)
    {
        for (var t = w.transform; t != null; t = t.parent)
        {
            int i = room.IndexOfSurface(t);
            if (i >= 0) return i;
        }
        return -1;
    }

    bool Usable(GhostSlot g) => g.window != null && g.wall >= 0;

    void UpdateGhosts()
    {
        if (windowGhost.enabled && Time.time >= nextGhost)
        {
            var free = new System.Collections.Generic.List<GhostSlot>();
            foreach (var g in ghosts)
                if (Usable(g) && !g.Present && !g.waiting && g.window.Estado != EstadoJanela.Arame) free.Add(g);
            if (free.Count > 0) free[UnityEngine.Random.Range(0, free.Count)].waiting = true;
            nextGhost = Time.time + windowGhost.interval.Roll();
        }

        foreach (var g in ghosts)
        {
            if (!Usable(g)) continue;
            if (g.waiting) TryAppear(g);
            if (g.Present) Tick(g);
        }
    }

    // Com arame não aparece (desiste); olhando pra parede da janela espera ela sair da tela.
    void TryAppear(GhostSlot g)
    {
        if (g.window.Estado == EstadoJanela.Arame) { g.waiting = false; return; }
        if (room.Mode == ViewMode.Wall && room.CurrentWall == g.wall) return;

        g.waiting = false;
        g.openFor = 0f;
        var parent = room.GetSurface(g.wall); // filho da superfície, como todo item
        g.sprite = windowGhost.prefab != null ? Instantiate(windowGhost.prefab, parent) : new GameObject("WindowGhost", typeof(Transform));
        g.sprite.transform.SetParent(parent, false);
        if (windowGhost.prefab == null) g.sprite.transform.position = g.window.transform.position; // com prefab, vale o transform dele

        g.window.RecolherCortina(); // ao aparecer ele abre a cortina
    }

    // Enquanto está lá: ansiedade sobe; janela aberta por loseTime seguidos = derrota; sai quando a cortina fecha.
    void Tick(GhostSlot g)
    {
        Anxiety.Add(windowGhost.anxietyPerSecond * Time.deltaTime);

        g.openFor = g.window.Estado == EstadoJanela.Aberta ? g.openFor + Time.deltaTime : 0f;
        if (g.openFor >= windowGhost.loseTime) { GameManager.Lose("Invadiram pela janela", "Talvez tente fechar a janela na próxima vez?"); return; }

        if (g.window.CortinaBaixada) { Destroy(g.sprite); g.sprite = null; }
    }

    void Update()
    {
        if (GameManager.Lost) return;

        if (doorBreaker.enabled && frontDoor != null && Time.time >= nextBreak)
        {
            frontDoor.Breach(doorBreaker.damage);
            nextBreak = Time.time + doorBreaker.interval.Roll();
        }

        // Attack() se recusa durante outro minigame/ataque; nesse caso só sorteia de novo
        if (backDoor.enabled && rearDoor != null && Time.time >= nextRun)
        {
            rearDoor.Attack();
            nextRun = Time.time + backDoor.interval.Roll();
        }

        UpdateGhosts();

        if (!tentacles.enabled) return;
        if (Time.time >= nextTentacle)
        {
            Tentacle.SpawnRandom();
            nextTentacle = Time.time + tentacles.spawnInterval.Roll();
        }
        if (Tentacle.MaxSource != null) Anxiety.Add(tentacles.anxietyPerSecond * Time.deltaTime); // encostou no jogador
    }

    // Aparece assim que a parede sair da tela (e sem arame)
    void Request(GhostSlot g)
    {
        if (g != null && Usable(g) && !g.Present) g.waiting = true;
    }

    // ponytail: debug via OnGUI, remover quando o ritmo do jogo estiver ajustado
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 250, 220, 80));
        if (GUILayout.Button("Surgir tentáculo")) Tentacle.SpawnRandom();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Vulto esquerda")) Request(left);
        if (GUILayout.Button("Vulto direita")) Request(right);
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }
}
