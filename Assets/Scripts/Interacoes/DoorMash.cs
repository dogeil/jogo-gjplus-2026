using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// Minigame de segurar a porta: clicar (mouse esquerdo) repetidamente enche a barra, que esvazia sozinha.
/// Barra cheia = venceu; barra vazia ou tempo esgotado = perdeu. A câmera é travada pelo chamador.
public static class DoorMash
{
    const float BarWidth = 360f;
    const float BarHeight = 36f;
    const float TimerHeight = 8f;
    const float BarOffsetY = 220f; // px acima da porta
    const float StartFill = 0.5f;

    public static IEnumerator Run(Vector3 doorWorld, float timeLimit, float perClick, float drain, Action<bool> done)
    {
        var canvasGo = new GameObject("DoorMash", typeof(Canvas));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        Vector2 center = (Vector2)Camera.main.WorldToScreenPoint(doorWorld) + Vector2.up * BarOffsetY;
        Bar(canvasGo.transform, center, BarWidth + 8f, BarHeight + 8f, new Color(0f, 0f, 0f, 0.7f), 1f);
        var fillBar = Bar(canvasGo.transform, center, BarWidth, BarHeight, Color.white, StartFill);
        var timerBar = Bar(canvasGo.transform, center + Vector2.down * (BarHeight * 0.5f + TimerHeight), BarWidth, TimerHeight, Color.yellow, 1f);

        float fill = StartFill;
        bool ok = false;
        for (float t = 0f; t < timeLimit; t += Time.deltaTime)
        {
            yield return null; // primeiro: o clique que abriu o minigame não conta
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) fill += perClick;
            fill -= drain * Time.deltaTime;

            fillBar.color = Color.Lerp(Color.red, Color.green, fill);
            fillBar.rectTransform.sizeDelta = new Vector2(BarWidth * Mathf.Clamp01(fill), BarHeight);
            timerBar.rectTransform.sizeDelta = new Vector2(BarWidth * (1f - t / timeLimit), TimerHeight);

            if (fill >= 1f) { ok = true; break; }
            if (fill <= 0f) break;
        }

        UnityEngine.Object.Destroy(canvasGo);
        done(ok);
    }

    // Barra ancorada na esquerda (Image sem sprite = cor chapada), centrada em `center` quando cheia.
    static Image Bar(Transform parent, Vector2 center, float width, float height, Color color, float fill)
    {
        var go = new GameObject("Bar", typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.pivot = new Vector2(0f, 0.5f);
        rt.position = center + Vector2.left * (width * 0.5f);
        rt.sizeDelta = new Vector2(width * fill, height);
        return img;
    }
}
