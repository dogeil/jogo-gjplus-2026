using UnityEngine;

/// Contorno de sprite no hover: 8 cópias brancas do sprite, deslocadas e atrás do original.
/// ponytail: vale pra arte com borda transparente e espessura pequena; trocar por shader de outline se ficar feio.
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteOutline : MonoBehaviour
{
    [SerializeField] float thickness = 0.1f; // unidades locais (o pass de pixelação come contornos finos)
    [SerializeField] Color color = Color.white;

    SpriteRenderer src;
    SpriteRenderer[] copies;
    static Material whiteMat; // silhueta chapada; sem ele a cópia só ficaria com a cor da textura

    // ponytail: Shader.Find precisa do shader incluído no build (Always Included Shaders) ou usado numa cena
    static Material WhiteMaterial()
    {
        if (whiteMat != null) return whiteMat;
        var sh = Shader.Find("Custom/SpriteWhite");
        return sh != null ? whiteMat = new Material(sh) : null;
    }

    public static SpriteOutline Attach(SpriteRenderer sr) =>
        sr.TryGetComponent(out SpriteOutline o) ? o : sr.gameObject.AddComponent<SpriteOutline>();

    void Awake()
    {
        src = GetComponent<SpriteRenderer>();
        copies = new SpriteRenderer[8];
        for (int i = 0; i < copies.Length; i++)
        {
            float a = i * Mathf.PI / 4f;
            var go = new GameObject("Outline");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * thickness;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = color;
            sr.sharedMaterial = WhiteMaterial() ?? src.sharedMaterial;
            sr.enabled = false;
            copies[i] = sr;
        }
    }

    int baseOrder;
    bool shown;

    public void Show(bool on)
    {
        on &= src.sprite != null;
        if (on == shown) { if (on) Sync(); return; }

        // Cópias na mesma ordem do original (acima da parede) e o original +1 por cima delas.
        // Com "ordem - 1" o contorno ia pra trás do fundo e não aparecia.
        if (on) baseOrder = src.sortingOrder;
        src.sortingOrder = on ? baseOrder + 1 : baseOrder;
        shown = on;
        foreach (var c in copies) c.enabled = on;
        if (on) Sync();
    }

    void Sync()
    {
        foreach (var c in copies)
        {
            c.sprite = src.sprite; // acompanha troca de sprite (janela aberta/fechada)
            c.flipX = src.flipX;
            c.flipY = src.flipY;
            c.sortingLayerID = src.sortingLayerID;
            c.sortingOrder = baseOrder;
        }
    }
}
