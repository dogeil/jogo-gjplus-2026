using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// Minigame do arame: segurar o botão e girar o mouse em volta do puxador (anel branco), sempre no mesmo sentido.
/// Precisa de várias voltas completas, sem limite de tempo. Ficar fora da faixa do anel com o botão apertado falha,
/// e olhar pro lado (câmera se mexer) cancela como derrota. Soltar o botão só pausa (o progresso fica).
public static class WireTwist
{
    const float RingRadius = 70f;      // px do anel do puxador
    const float BandMin = 0.78f;       // faixa válida: BandMin..BandMax x raio (invisível; fora dela o traço fica vermelho)
    const float BandMax = 1.1f;
    const float Laps = 3f;
    const float Grace = 0.3f;          // s que pode ficar fora da faixa (tudo vermelho) antes de falhar
    const float FailFlash = 0.4f;      // s mostrando vermelho depois de falhar
    const float PointSpacing = 5f;     // px entre pontos da linha
    const float LineWidth = 6f;
    const float ArrowSpeed = 120f;     // graus/s da seta-guia (sentido horário)

    static Sprite arrow;

    public static IEnumerator Run(Vector3 handleWorld, RoomView room, Action<bool> done)
    {
        var canvasGo = new GameObject("WireTwist", typeof(Canvas));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        Vector2 center = Camera.main.WorldToScreenPoint(handleWorld);
        var ring = MakeRing(canvasGo.transform, center, 1f);
        var arrowImg = MakeArrow(canvasGo.transform);
        float arrowAngle = 90f;

        float total = 0f;               // graus acumulados (com sinal)
        float prevAngle = 0f;
        bool engaged = false;           // botão apertado dentro da faixa
        Vector2 lastPoint = center;
        float outsideTime = 0f;
        float progress = 0f;
        bool ok = false;
        bool lookedAway = false;

        while (true)
        {
            yield return null;
            if (room != null && room.IsMoving) { lookedAway = true; break; }
            var mouse = Mouse.current;
            if (mouse == null) break;

            if (arrowImg != null)
            {
                arrowAngle -= ArrowSpeed * Time.deltaTime;
                float rad = arrowAngle * Mathf.Deg2Rad;
                arrowImg.rectTransform.position = center + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * RingRadius;
                arrowImg.rectTransform.rotation = Quaternion.Euler(0f, 0f, arrowAngle - 90f); // aponta na tangente, horário
            }

            if (!mouse.leftButton.isPressed)
            {
                engaged = false;
                ring.color = Color.Lerp(Color.white, Color.green, progress);
                continue;
            }

            Vector2 pos = mouse.position.ReadValue();
            Vector2 off = pos - center;
            float dist = off.magnitude / RingRadius;
            bool inBand = dist >= BandMin && dist <= BandMax;

            // Fora da faixa com o botão apertado: anel e traço ficam vermelhos
            ring.color = inBand ? Color.Lerp(Color.white, Color.green, progress) : Color.red;

            if (!engaged)
            {
                if (!inBand) continue;
                engaged = true;
                if (arrowImg != null) { UnityEngine.Object.Destroy(arrowImg.gameObject); arrowImg = null; } // já entendeu: some a seta
                prevAngle = Mathf.Atan2(off.y, off.x) * Mathf.Rad2Deg;
                lastPoint = pos;
                outsideTime = 0f;
                continue;
            }

            if (inBand) outsideTime = 0f;
            else if ((outsideTime += Time.deltaTime) >= Grace) break; // ficou tempo demais fora: falha

            float angle = Mathf.Atan2(off.y, off.x) * Mathf.Rad2Deg;
            // Conta sempre (com sinal): sair da faixa pra "voltar" o mouse desconta, não dá pra riscar vai-e-volta
            total += Mathf.DeltaAngle(prevAngle, angle);
            progress = Mathf.Clamp01(Mathf.Abs(total) / (360f * Laps));
            prevAngle = angle;

            if (Vector2.Distance(pos, lastPoint) >= PointSpacing)
            {
                Segment(canvasGo.transform, lastPoint, pos, inBand ? Color.gray : Color.red);
                lastPoint = pos;
            }

            if (progress >= 1f) { ok = true; break; }
        }

        if (!ok && !lookedAway) // falhou: congela em vermelho um instante pro jogador ver
        {
            ring.color = Color.red;
            yield return new WaitForSeconds(FailFlash);
        }

        UnityEngine.Object.Destroy(canvasGo);
        done(ok);
    }

    // Trecho da linha: retângulo esticado entre dois pontos (Image sem sprite = cor chapada).
    static void Segment(Transform parent, Vector2 a, Vector2 b, Color color)
    {
        var go = new GameObject("Wire", typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        Vector2 d = b - a;
        rt.sizeDelta = new Vector2(d.magnitude + LineWidth * 0.5f, LineWidth); // sobra pra cobrir a emenda
        rt.position = (a + b) * 0.5f;
        rt.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
    }

    // Triângulo apontando pra +x, gerado em código (como o anel do NailHammer).
    static Sprite Arrow()
    {
        if (arrow != null) return arrow;
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float half = 28f * (60f - x) / 56f; // meia altura: base em x=4, ponta em x=60
                bool inside = x >= 4 && x <= 60 && Mathf.Abs(y - 32f) <= half;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, inside ? 1f : 0f));
            }
        tex.Apply();
        return arrow = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }

    static Image MakeArrow(Transform parent)
    {
        var go = new GameObject("Arrow", typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = Arrow();
        img.color = Color.yellow;
        img.raycastTarget = false;
        img.rectTransform.sizeDelta = new Vector2(36f, 36f);
        return img;
    }

    static Image MakeRing(Transform parent, Vector2 screenPos, float radiusScale)
    {
        var go = new GameObject("Ring", typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = NailHammer.Ring();
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.sizeDelta = Vector2.one * RingRadius * 2f * radiusScale;
        rt.position = screenPos;
        return img;
    }
}
