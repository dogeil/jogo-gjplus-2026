using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Anxiety : MonoBehaviour
{
    public static float Level { get; private set; }

    [SerializeField] float decayPerSecond = 2f;

    public static void Add(float amount) => Level = Mathf.Clamp(Level + amount, 0f, 100f);

    [SerializeField] Volume volume; // vazio = acha o primeiro Volume da cena
    [SerializeField] float maxVignette = 0.5f;
    [SerializeField] float maxChromatic = 1f;

    [Header("Shake")]
    [SerializeField] float shakeThreshold = 60f;
    [SerializeField] float maxShakeAngle = 0.4f; // graus de roll em ansiedade 100
    [SerializeField] float shakeSpeed = 25f;

    [Header("Pixelation")]
    [SerializeField] Material pixelationMaterial; // o mesmo material do Full Screen Pass
    [SerializeField] float pixelationStart = 50f;
    [SerializeField] float maxPixelation = 3.5f;
    [SerializeField] float minPixelation = 0.0001f; // 0 exato pode dividir por zero no graph

    static readonly int PixelationID = Shader.PropertyToID("_Pixelation");

    Vignette vignette;
    ChromaticAberration chromatic;
    float baseVignette, baseChromatic, basePixelation;

    void Start()
    {
        if (pixelationMaterial != null) basePixelation = pixelationMaterial.GetFloat(PixelationID);
        if (volume == null) volume = FindFirstObjectByType<Volume>();
        if (volume == null) return;
        // .profile (nao sharedProfile): instancia, nao suja o asset no disco
        if (volume.profile.TryGet(out vignette)) { vignette.intensity.overrideState = true; baseVignette = vignette.intensity.value; }
        if (volume.profile.TryGet(out chromatic)) { chromatic.intensity.overrideState = true; baseChromatic = chromatic.intensity.value; }
    }

    void Update()
    {
        Add(-decayPerSecond * Time.deltaTime);
        float t = Level / 100f;
        if (vignette != null) vignette.intensity.value = Mathf.Lerp(baseVignette, maxVignette, t);
        if (chromatic != null) chromatic.intensity.value = Mathf.Lerp(baseChromatic, maxChromatic, t);
        if (pixelationMaterial != null)
            pixelationMaterial.SetFloat(PixelationID,
                Mathf.Max(minPixelation, Mathf.Lerp(0f, maxPixelation, Mathf.InverseLerp(pixelationStart, 100f, Level))));
    }

    // material e asset compartilhado: no editor a alteracao persiste, entao restaura
    void OnDisable()
    {
        if (pixelationMaterial != null) pixelationMaterial.SetFloat(PixelationID, basePixelation);
    }

    // Roll na camera (nao posicao): RoomView controla posicao e mede blur por ela.
    void LateUpdate()
    {
        float s = Mathf.InverseLerp(shakeThreshold, 100f, Level);
        float noise = Mathf.PerlinNoise(Time.time * shakeSpeed, 0f) * 2f - 1f;
        Camera.main.transform.rotation = Quaternion.Euler(0f, 0f, noise * s * maxShakeAngle);
    }

    // ponytail: debug via OnGUI, trocar por barra UI + remover botoes quando tiver HUD
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 40, 220, 80));
        GUILayout.Label($"Ansiedade: {Mathf.RoundToInt(Level)} / 100");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-10")) Add(-10f);
        if (GUILayout.Button("+10")) Add(10f);
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }
}
