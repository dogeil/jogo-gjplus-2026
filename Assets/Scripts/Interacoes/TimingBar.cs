using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// Minigame: marcador vai e volta, clicar com ele na zona verde. N acertos seguidos vencem, qualquer erro falha.
public static class TimingBar
{
    const float BarWidth = 500f, BarHeight = 40f, ZoneWidth = 90f, Speed = 1.2f; // ciclos/s

    public static IEnumerator Run(int hits, Action<bool> done)
    {
        var canvasGo = new GameObject("TimingBar", typeof(Canvas));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        RectTransform Box(string n, Transform parent, Color c, Vector2 size)
        {
            var go = new GameObject(n, typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = c;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            return rt;
        }

        var bar = Box("Bar", canvasGo.transform, Color.black, new Vector2(BarWidth, BarHeight));
        var zone = Box("Zone", bar, Color.green, new Vector2(ZoneWidth, BarHeight));
        var marker = Box("Marker", bar, Color.white, new Vector2(8f, BarHeight + 10f));

        float half = (BarWidth - ZoneWidth) / 2f;
        bool ok = true;
        for (int i = 0; i < hits && ok; i++)
        {
            float zoneX = UnityEngine.Random.Range(-half, half);
            zone.anchoredPosition = new Vector2(zoneX, 0f);
            float t = 0f;
            while (true)
            {
                t += Time.deltaTime * Speed;
                float x = Mathf.PingPong(t, 1f) * BarWidth - BarWidth / 2f;
                marker.anchoredPosition = new Vector2(x, 0f);
                yield return null;
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    ok = Mathf.Abs(x - zoneX) <= ZoneWidth / 2f;
                    break;
                }
            }
        }

        UnityEngine.Object.Destroy(canvasGo);
        done(ok);
    }
}
