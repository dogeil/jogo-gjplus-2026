using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// QTE: em cada prego (mundo, em ordem) um anel fecha em direção ao anel fixo.
/// Clicar em qualquer lugar da tela com o anel dentro da janela crava; cedo, tarde ou fechar sem clique falha. Cada prego fecha mais rápido.
public static class NailHammer
{
    const float TargetSize = 90f;      // px do anel fixo
    const float StartScale = 3f;       // anel começa 3x maior
    const float FirstStartScale = 6f;  // 1º anel começa bem mais longe, pro jogador entender o que fazer
    const float EndScale = 0.7f;       // passou daqui sem clique = falha
    const float Tolerance = 0.4f;      // janela: |escala - 1| <= isso (menor = mais difícil)
    const float BaseTime = 1.5f;       // s para fechar o 1º anel
    const float SpeedUp = 0.95f;       // cada prego dura 95% do anterior

    static Sprite ring;

    public static Sprite Ring()
    {
        if (ring != null) return ring;
        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        float c = n / 2f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, d <= 1f && d >= 0.88f ? 1f : 0f));
            }
        tex.Apply();
        return ring = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }

    static RectTransform MakeRing(Transform parent, Color color)
    {
        var go = new GameObject("Ring", typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = Ring();
        img.color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(TargetSize, TargetSize);
        return rt;
    }

    public static IEnumerator Run(Vector3[] points, Action<bool> done)
    {
        var cam = Camera.main;
        var canvasGo = new GameObject("NailHammer", typeof(Canvas));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var target = MakeRing(canvasGo.transform, Color.white);
        var closing = MakeRing(canvasGo.transform, Color.yellow);

        bool ok = true;
        for (int i = 0; i < points.Length && ok; i++)
        {
            target.position = closing.position = cam.WorldToScreenPoint(points[i]);
            float start = i == 0 ? FirstStartScale : StartScale;
            // mesma velocidade de fechamento, só que com mais caminho no 1º anel
            float duration = BaseTime * Mathf.Pow(SpeedUp, i) * (start - EndScale) / (StartScale - EndScale);
            float t = 0f;
            bool driven = false;

            while (!driven && ok)
            {
                t += Time.deltaTime;
                float scale = Mathf.Lerp(start, EndScale, t / duration);
                closing.localScale = Vector3.one * scale;
                if (t >= duration) { ok = false; break; } // fechou sem clique

                yield return null;
                if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) continue;

                if (Mathf.Abs(scale - 1f) <= Tolerance) driven = true;
                else ok = false; // cedo ou tarde demais
            }
        }

        UnityEngine.Object.Destroy(canvasGo);
        done(ok);
    }
}
