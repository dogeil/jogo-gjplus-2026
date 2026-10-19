using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// Luz do teto com parafusos nos cantos (um ParafusoTeto por canto, filhos deste objeto).
/// Parafusos caem do teto, cruzam a vista de qualquer parede e vão pro chão. Com os 4 caídos, alarme piscando
/// e o jogador tem invasionTime segundos pra recolocar ao menos 1; senão invadem pelo teto.
public class TetoLuz : MonoBehaviour
{
    [SerializeField] GameObject screwPrefab;      // o mesmo prefab Parafuso do chão
    [SerializeField] ParafusoTeto[] slots;        // se vazio, pega os filhos
    [SerializeField] float invasionTime = 6f;     // s entre o 4º parafuso cair e perder a fase
    [SerializeField] float anxietyOnDrop = 5f;    // ansiedade a cada parafuso que cai
    [SerializeField] float fallSpeed = 12f;      // unidades/s do parafuso cruzando a tela
    [SerializeField] float centerSpread = 3f;     // o parafuso nasce a até isso (unidades) do centro da tela
    [SerializeField] Sprite alarmSprite;          // sem sprite vira um quadrado vermelho
    [SerializeField] float alarmScale = 0.33f;
    [SerializeField] float blinkInterval = 0.3f;

    // Os cantos usam o mesmo sprite/escala/ritmo pro marcador em cima do furo vazio
    public Sprite AlarmSprite => alarmSprite;
    public float AlarmScale => alarmScale;
    public float BlinkInterval => blinkInterval;
    public Sprite ScrewSprite => screwPrefab != null && screwPrefab.TryGetComponent(out SpriteRenderer sr) ? sr.sprite : null; // reserva se o canto não tem Screw Visual

    const float AlarmMargin = 40f;
    const float FallbackAlarmSize = 120f;
    const float SpinSpeed = 180f; // graus/s do parafuso caindo

    RoomView room;
    GameObject alarmCanvas;
    Image alarm;
    float countdown;
    bool armed;

    void Awake()
    {
        room = FindFirstObjectByType<RoomView>();
        if (slots == null || slots.Length == 0) slots = GetComponentsInChildren<ParafusoTeto>();
    }

    /// Derruba um parafuso aleatório que ainda está no lugar.
    public void DropScrew()
    {
        if (GameManager.Lost) return;
        var intact = new List<ParafusoTeto>();
        foreach (var s in slots) if (!s.Fallen) intact.Add(s);
        if (intact.Count == 0) return;

        Anxiety.Add(anxietyOnDrop);
        intact[Random.Range(0, intact.Count)].SetFallen(true);
        StartCoroutine(Fall());
        if (intact.Count == 1) Arm(); // era o último
    }

    /// Chamado pelo canto quando um parafuso volta ao lugar: já não são os 4 caídos.
    public void OnRefilled()
    {
        if (armed) Disarm();
    }

    void Arm()
    {
        armed = true;
        countdown = invasionTime;
        alarmCanvas = new GameObject("CeilingAlarm", typeof(Canvas));
        alarmCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        alarmCanvas.GetComponent<Canvas>().sortingOrder = 90;
        alarm = new GameObject("Alarm", typeof(Image)).GetComponent<Image>();
        alarm.transform.SetParent(alarmCanvas.transform, false);
        alarm.raycastTarget = false;
        var rt = alarm.rectTransform;
        rt.localScale = Vector3.one * alarmScale;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f); // topo, no centro
        rt.anchoredPosition = new Vector2(0f, -AlarmMargin);
        if (alarmSprite != null) { alarm.sprite = alarmSprite; alarm.SetNativeSize(); }
        else { alarm.color = Color.red; rt.sizeDelta = Vector2.one * FallbackAlarmSize; }
    }

    void Disarm()
    {
        armed = false;
        Destroy(alarmCanvas);
    }

    void Update()
    {
        if (!armed) return;
        // Olhando pro teto o jogador já vê os parafusos: sem alarme
        alarm.enabled = room.Mode != ViewMode.Ceiling && (int)(Time.time / blinkInterval) % 2 == 0;
        if ((countdown -= Time.deltaTime) > 0f) return;
        Disarm();
        GameManager.Lose("Invadiram pelo teto", "Talvez tente recolocar os parafusos do teto na próxima vez?");
    }

    // Cai do topo da vista atual (qualquer parede) até passar da borda de baixo; aí vai pro chão.
    IEnumerator Fall()
    {
        var cam = Camera.main;
        Transform wall = room.GetSurface(room.CurrentWall);
        var screw = Instantiate(screwPrefab, wall);
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        var p = screw.transform.position;
        float spread = Mathf.Min(centerSpread, halfW - 1f);
        p.x = cam.transform.position.x + Random.Range(-spread, spread);
        p.y = wall.position.y + halfH + 1f;
        screw.transform.position = p;

        // sem física na queda: o rodapé da parede não pode segurar o parafuso
        screw.TryGetComponent(out Rigidbody2D rb);
        if (rb != null) rb.simulated = false;

        while (screw != null && !room.FellOffSurface(screw.transform))
        {
            screw.transform.position += Vector3.down * fallSpeed * Time.deltaTime;
            screw.transform.Rotate(0f, 0f, SpinSpeed * Time.deltaTime);
            yield return null;
        }
        if (screw == null) yield break;

        room.DropOnFloor(screw.transform);
        if (rb != null)
        {
            rb.gravityScale = 0f; // chão não tem gravidade
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = true;
        }
    }
}
