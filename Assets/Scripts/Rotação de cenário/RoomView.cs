using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public enum ViewMode { Wall, Floor, Ceiling }

public class RoomView : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] Camera cam;
    [SerializeField] Transform[] walls = new Transform[4]; // ordem girando para a direita
    [SerializeField] Transform floor;
    [SerializeField] Transform ceiling;
    [SerializeField] float wallWidth = 19.2f;   // 1920 px / 100 PPU
    [SerializeField] float viewHeight = 10.8f;  // 1080 px / 100 PPU
    [SerializeField] int startWall = 0;

    [Header("Motion")]
    [SerializeField] float turnDuration = 0.18f;
    [SerializeField] float lookDuration = 0.18f;
    [SerializeField] AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Blur")]
    [SerializeField] float shutter = 1.5f;
    [SerializeField] float maxBlur = 0.12f;
    [SerializeField] float blurFadeSpeed = 20f;

    static readonly int BlurAmountID = Shader.PropertyToID("_BlurAmount");
    static readonly int BlurDirID    = Shader.PropertyToID("_BlurDir");

    // Índices de superfície: 0..3 paredes, 4 chão, 5 teto
    public int FloorIndex   => walls.Length;
    public int CeilingIndex => walls.Length + 1;

    public int CurrentWall { get; private set; }
    public ViewMode Mode { get; private set; } = ViewMode.Wall;
    public bool IsTurning => busy;

    public event Action<int> OnTurnStarted;          // parede de destino, antes de mover
    public event Action<int> OnWallChanged;          // parede atual, depois do giro
    public event Action<ViewMode> OnViewModeChanged; // depois de olhar pra cima/baixo

    Transform[] surfaces;
    bool busy;
    Vector2 lastCamPos;
    Vector2 blurDir = Vector2.right;
    float blur;

    void Awake()
    {
        if (cam == null) cam = Camera.main;

        surfaces = new Transform[walls.Length + 2];
        for (int i = 0; i < walls.Length; i++) surfaces[i] = walls[i];
        surfaces[FloorIndex] = floor;
        surfaces[CeilingIndex] = ceiling;

        floor.position   = new Vector3(0f, -viewHeight, floor.position.z);
        ceiling.position = new Vector3(0f,  viewHeight, ceiling.position.z);

        CurrentWall = Mathf.Clamp(startWall, 0, walls.Length - 1);
        LayoutAround(CurrentWall);
        SnapCamera(Vector2.zero);

        blur = 0f;
        Shader.SetGlobalVector(BlurDirID, new Vector4(1f, 0f, 0f, 0f));
        Shader.SetGlobalFloat(BlurAmountID, 0f);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame)  TurnLeft();
        if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) TurnRight();
        if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)    LookUp();
        if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)  LookDown();
    }

    void LateUpdate()
    {
        Vector2 camPos = cam.transform.position;
        Vector2 delta = camPos - lastCamPos;
        lastCamPos = camPos;

        // Movimento deste frame em unidades de tela (UV)
        float h = cam.orthographicSize * 2f;
        float w = h * cam.aspect;
        Vector2 uvDelta = new Vector2(delta.x / w, delta.y / h);

        float target = Mathf.Min(uvDelta.magnitude * shutter, maxBlur);
        if (uvDelta.sqrMagnitude > 1e-8f) blurDir = uvDelta.normalized; // mantém a direção durante o fade

        blur = target > blur
            ? target
            : Mathf.Lerp(blur, target, 1f - Mathf.Exp(-blurFadeSpeed * Time.deltaTime));

        Shader.SetGlobalVector(BlurDirID, new Vector4(blurDir.x, blurDir.y, 0f, 0f));
        Shader.SetGlobalFloat(BlurAmountID, blur);
    }

    // ---------- Controles ----------

    public void TurnLeft()  { if (!busy && Mode == ViewMode.Wall) StartCoroutine(Turn(-1)); }
    public void TurnRight() { if (!busy && Mode == ViewMode.Wall) StartCoroutine(Turn(+1)); }

    public void LookUp()
    {
        if (busy) return;
        if (Mode == ViewMode.Wall)       StartCoroutine(Look(ViewMode.Ceiling));
        else if (Mode == ViewMode.Floor) StartCoroutine(Look(ViewMode.Wall));
    }

    public void LookDown()
    {
        if (busy) return;
        if (Mode == ViewMode.Wall)         StartCoroutine(Look(ViewMode.Floor));
        else if (Mode == ViewMode.Ceiling) StartCoroutine(Look(ViewMode.Wall));
    }

    // ---------- Superfícies (itens) ----------

    public Transform GetWall(int index) => walls[index];
    public Transform GetSurface(int index) => surfaces[index];
    public int IndexOfSurface(Transform t) => Array.IndexOf(surfaces, t);
    public bool IsFloor(Transform surface) => surface == floor;
    public bool IsCeiling(Transform surface) => surface == ceiling;
    public bool IsFloorOrCeiling(Transform surface) => surface == floor || surface == ceiling;

    /// Item caiu do teto: reparenta no chão em ponto aleatório.
    public void DropOnFloor(Transform item, float margin = 1f)
    {
        item.SetParent(floor, false);
        item.localPosition = new Vector3(
            UnityEngine.Random.Range(-wallWidth / 2f + margin, wallWidth / 2f - margin),
            UnityEngine.Random.Range(-viewHeight / 2f + margin, viewHeight / 2f - margin),
            item.localPosition.z);
        Physics2D.SyncTransforms();
    }

    /// Item em parede/teto passou da borda de baixo da superfície?
    public bool FellOffSurface(Transform item) =>
        item.parent != null && item.parent != floor && IndexOfSurface(item.parent) >= 0 &&
        item.position.y < item.parent.position.y - viewHeight / 2f;

    /// Superfície real mais próxima de um ponto (distância normalizada pelo tamanho da tela).
    public Transform SurfaceAt(Vector3 worldPos)
    {
        Transform best = surfaces[CurrentWall];
        float bestDist = float.MaxValue;
        foreach (var s in surfaces)
        {
            float dx = (worldPos.x - s.position.x) / wallWidth;
            float dy = (worldPos.y - s.position.y) / viewHeight;
            float d = dx * dx + dy * dy;
            if (d < bestDist) { bestDist = d; best = s; }
        }
        return best;
    }

    /// Prende o item na parede/chão/teto embaixo dele.
    public void AttachToWall(Transform item) => item.SetParent(SurfaceAt(item.position), true);

    // ---------- Movimento ----------

    IEnumerator Turn(int dir)
    {
        busy = true;

        int n = walls.Length;
        int target = ((CurrentWall + dir) % n + n) % n;
        OnTurnStarted?.Invoke(target);

        Vector3 start = cam.transform.position;
        yield return Pan(start, new Vector3(dir * wallWidth, start.y, start.z), turnDuration);

        CurrentWall = target;
        LayoutAround(CurrentWall);
        SnapCamera(Vector2.zero);

        busy = false;
        OnWallChanged?.Invoke(CurrentWall);
    }

    IEnumerator Look(ViewMode target)
    {
        busy = true;

        float y = target == ViewMode.Ceiling ?  viewHeight
                : target == ViewMode.Floor   ? -viewHeight
                : 0f;

        Vector3 start = cam.transform.position;
        yield return Pan(start, new Vector3(start.x, y, start.z), lookDuration);

        Mode = target;
        busy = false;
        OnViewModeChanged?.Invoke(Mode);
    }

    IEnumerator Pan(Vector3 from, Vector3 to, float duration)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            cam.transform.position = Vector3.LerpUnclamped(from, to, ease.Evaluate(Mathf.Clamp01(t)));
            yield return null;
        }
        cam.transform.position = to;
    }

    void LayoutAround(int center)
    {
        int n = walls.Length;
        for (int i = 0; i < n; i++)
        {
            int offset = (i - center + n) % n;
            if (offset == n - 1) offset = -1;

            Vector3 p = walls[i].position;
            p.x = offset * wallWidth;
            p.y = 0f;
            walls[i].position = p;
        }
        Physics2D.SyncTransforms();
    }

    void SnapCamera(Vector2 pos)
    {
        Vector3 p = cam.transform.position;
        p.x = pos.x;
        p.y = pos.y;
        cam.transform.position = p;
        lastCamPos = pos; // evita pico de blur no snap
    }

    void OnDisable() => Shader.SetGlobalFloat(BlurAmountID, 0f);
}