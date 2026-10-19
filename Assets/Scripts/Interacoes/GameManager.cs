using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// Fim de fase. Lose(motivo): trava a câmera, fade para preto e fade in do texto branco com o motivo.
public static class GameManager
{
    const float FadeTime = 1.5f;
    const float TextDelay = 1f; // pausa na tela preta antes do texto aparecer

    public static bool Lost { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() => Lost = false; // statics sobrevivem sem domain reload no editor

    public static void Lose(string reason, string hint = null)
    {
        if (Lost) return; // só a primeira derrota conta
        Lost = true;
        Debug.Log("Fase perdida: " + reason);

        var room = Object.FindFirstObjectByType<RoomView>();
        if (room != null) room.Locked = true;

        new GameObject("LoseScreen").AddComponent<LoseScreen>().Show(reason, hint);
    }

    /// Fim de fase com vitoria: mesma tela de fade, so o texto. Marca Lost = true so pra parar o resto do jogo
    /// (inimigos, minigames, camera travada); nao e uma derrota.
    public static void Win(string message)
    {
        if (Lost) return;
        Lost = true;
        Debug.Log("Fase vencida: " + message);

        var room = Object.FindFirstObjectByType<RoomView>();
        if (room != null) room.Locked = true;

        new GameObject("WinScreen").AddComponent<LoseScreen>().Show(message, null);
    }

    class LoseScreen : MonoBehaviour
    {
        public void Show(string reason, string hint) => StartCoroutine(Run(reason, hint));

        IEnumerator Run(string reason, string hint)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            var black = Fullscreen("Black", Color.clear).GetComponent<Image>();
            var text = MakeText("Text", reason, 48, 30f);
            var sub = MakeText("Hint", hint, 28, -40f);

            for (float t = 0f; t < FadeTime; t += Time.unscaledDeltaTime)
            {
                black.color = new Color(0f, 0f, 0f, t / FadeTime);
                yield return null;
            }
            black.color = Color.black;

            yield return new WaitForSecondsRealtime(TextDelay);

            for (float t = 0f; t < FadeTime; t += Time.unscaledDeltaTime)
            {
                var c = new Color(1f, 1f, 1f, t / FadeTime);
                text.color = sub.color = c;
                yield return null;
            }
            text.color = sub.color = Color.white;
        }

        // y = deslocamento vertical (px) do centro da tela
        Text MakeText(string name, string content, int size, float y)
        {
            var t = new GameObject(name, typeof(Text)).GetComponent<Text>();
            t.transform.SetParent(transform, false);
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = content;
            t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(1f, 1f, 1f, 0f);
            var rt = t.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0f, y); rt.offsetMax = new Vector2(0f, y);
            return t;
        }

        GameObject Fullscreen(string name, Color color)
        {
            var go = new GameObject(name, typeof(Image));
            go.transform.SetParent(transform, false);
            go.GetComponent<Image>().color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }
    }
}
