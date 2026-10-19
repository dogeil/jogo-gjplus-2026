using System;
using System.Collections;
using UnityEngine;

/// Template: vai numa entrada/objeto do cenário. Soltar a ferramenta certa em cima, com recursos no inventário,
/// desconta o custo e roda o minigame. Sucesso -> OnSuccess, falha -> OnFail (recursos caem no chão).
[RequireComponent(typeof(Collider2D))]
public abstract class Interactable : MonoBehaviour
{
    [Serializable]
    public struct Cost
    {
        public Recurso recurso;
        public int qtd;
        public GameObject dropPrefab; // item que cai no chão se falhar
    }

    [SerializeField] protected Cost[] costs;
    [SerializeField, Min(0f)] float anxietyRelief = 5f; // ansiedade removida ao vencer o minigame

    /// Algum minigame rodando (mesmo sem travar a sala). Outros objetos clicáveis, como a cortina, devem ignorar o mouse.
    public static bool AnyBusy { get; private set; }

    protected RoomView room;
    bool busy;

    public abstract Type RequiredTool { get; }
    protected virtual bool CanUse => true;
    protected abstract void OnSuccess();
    protected virtual void OnBegin() { } // minigame vai começar
    protected virtual bool LockRoom => true; // false: o jogador pode olhar pro lado durante o minigame (ele decide se cancela)

    protected virtual void Awake()
    {
        room = FindFirstObjectByType<RoomView>();
        GetComponent<Collider2D>().isTrigger = true; // só detecta o drop, não colide com a ferramenta
    }

    public bool TryStart(DraggableObject tool)
    {
        if (busy || !CanUse || !RequiredTool.IsInstanceOfType(tool)) return false;
        foreach (var c in costs)
            if (InventoryHandler.Quantidade(c.recurso) < c.qtd)
            {
                Debug.Log($"{name}: falta {c.recurso} ({InventoryHandler.Quantidade(c.recurso)}/{c.qtd})");
                return false;
            }

        foreach (var c in costs) InventoryHandler.Remover(c.recurso, c.qtd);
        StartCoroutine(Run(tool));
        return true;
    }

    IEnumerator Run(DraggableObject tool)
    {
        busy = AnyBusy = true;
        if (room != null && LockRoom) room.Locked = true;
        tool.gameObject.SetActive(false); // ferramenta em uso some até o fim
        bool ok = false;
        OnBegin();
        yield return Minigame(r => ok = r);
        tool.gameObject.SetActive(true);
        if (ok) tool.Suspend(); // venceu: fica no ar até tocar nele ou olhar pro lado; falhou: cai
        if (room != null && LockRoom && !GameManager.Lost) room.Locked = false; // perdeu no meio: a tela de derrota mantém travado
        busy = AnyBusy = false;
        if (ok) { Anxiety.Add(-anxietyRelief); OnSuccess(); } else OnFail();
    }

    protected abstract IEnumerator Minigame(Action<bool> done);

    protected virtual void OnFail() => DropCosts();

    /// Solta no chão uma fração aleatória (entre min e max) de cada recurso do custo. Padrão: tudo.
    /// `alwaysAll`: esse recurso cai inteiro, sem sorteio.
    protected void DropCosts(float minFraction = 1f, float maxFraction = 1f, Recurso? alwaysAll = null)
    {
        if (room == null) return;
        foreach (var c in costs)
        {
            if (c.dropPrefab == null) continue;
            int amount = c.recurso == alwaysAll
                ? c.qtd
                : Mathf.RoundToInt(c.qtd * UnityEngine.Random.Range(minFraction, maxFraction));
            for (int i = 0; i < amount; i++)
            {
                var item = Instantiate(c.dropPrefab, room.GetSurface(room.FloorIndex));
                room.DropOnFloor(item.transform);
                item.transform.Rotate(0f, 0f, UnityEngine.Random.Range(0f, 360f));
                // chão não tem gravidade: o prefab pode não ter DraggableObject pra zerar isso
                if (item.TryGetComponent(out Rigidbody2D rb))
                {
                    rb.gravityScale = 0f;
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
            }
        }
    }
}
