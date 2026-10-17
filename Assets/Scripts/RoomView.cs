using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class RoomView : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] Camera cam;
    [SerializeField] Transform[] walls = new Transform[4]; // in turning-right order
    [SerializeField] float wallWidth = 19.2f;              // 1920 px / 100 PPU
    [SerializeField] int startWall = 0;

    [Header("Turn")]
    [SerializeField] float duration = 0.18f;
    [SerializeField] AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Blur")]
    [SerializeField] float shutter = 1.5f;
    [SerializeField] float maxBlur = 0.12f;
    [SerializeField] float blurFadeSpeed = 20f;

    static readonly int BlurAmountID = Shader.PropertyToID("_BlurAmount");
    static readonly int BlurDirID    = Shader.PropertyToID("_BlurDir");

    public int CurrentWall { get; private set; }

    bool turning;
    float lastCamX;
    float blur;

    void Awake()
    {
        if (cam == null) cam = Camera.main;

        LayoutWalls();
        Shader.SetGlobalVector(BlurDirID, new Vector4(1f, 0f, 0f, 0f));
        SnapTo(startWall);
        blur = 0f;
        Shader.SetGlobalFloat(BlurAmountID, 0f);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame)  TurnLeft();
        if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) TurnRight();
    }

    void LateUpdate()
    {
        float camX = cam.transform.position.x;
        float dx = camX - lastCamX;
        lastCamX = camX;

        float screenWidth = cam.orthographicSize * 2f * cam.aspect;
        float target = Mathf.Min(Mathf.Abs(dx) / screenWidth * shutter, maxBlur);

        blur = target > blur
            ? target
            : Mathf.Lerp(blur, target, 1f - Mathf.Exp(-blurFadeSpeed * Time.deltaTime));

        Shader.SetGlobalFloat(BlurAmountID, blur);
    }

    // ---------- Public (hook these to UI buttons) ----------

    public void TurnLeft()  { if (!turning) StartCoroutine(Turn(-1)); }
    public void TurnRight() { if (!turning) StartCoroutine(Turn(+1)); }

    // ---------- Internals ----------

    void LayoutWalls()
    {
        for (int i = 0; i < walls.Length; i++)
            walls[i].position = SlotPos(i);

        // Ghosts: copy of last wall on the far left, copy of first wall on the far right
        CreateGhost(walls[walls.Length - 1], -1);
        CreateGhost(walls[0], walls.Length);
    }

    void CreateGhost(Transform source, int slot)
    {
        Transform ghost = Instantiate(source, source.parent);
        ghost.name = source.name + "_Ghost";
        ghost.position = SlotPos(slot);

        // Ghosts are visual only — disable any logic/colliders on them
        foreach (var mb in ghost.GetComponentsInChildren<MonoBehaviour>()) mb.enabled = false;
        foreach (var col in ghost.GetComponentsInChildren<Collider2D>()) col.enabled = false;
    }

    IEnumerator Turn(int dir)
    {
        turning = true;

        int slot = CurrentWall + dir; // can be -1 or 4 (a ghost)
        Vector3 start = cam.transform.position;
        Vector3 end = new Vector3(SlotPos(slot).x, start.y, start.z);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            cam.transform.position = Vector3.LerpUnclamped(start, end, ease.Evaluate(Mathf.Clamp01(t)));
            yield return null;
        }

        // Wrap: if we landed on a ghost, snap to the identical real wall
        int n = walls.Length;
        SnapTo(((slot % n) + n) % n);

        turning = false;
    }

    void SnapTo(int index)
    {
        CurrentWall = index;
        Vector3 p = cam.transform.position;
        p.x = SlotPos(index).x;
        cam.transform.position = p;
        lastCamX = p.x; // prevents a blur spike from the snap
    }

    Vector3 SlotPos(int slot) => new Vector3(slot * wallWidth, 0f, 0f);

    void OnDisable() => Shader.SetGlobalFloat(BlurAmountID, 0f);
}