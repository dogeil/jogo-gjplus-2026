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

    public static void Lose(string reason)
    {
        if (Lost) return; // só a primeira derrota conta
        Lost = true;
        Debug.Log("Fase perdida: " + reason);

        var room = Object.FindFirstObjectByType<RoomView>();
        if (room != null) room.Locked = true;

        new GameObject("LoseScreen").AddComponent<LoseScreen>().Show(reason);
    }

    class LoseScreen : MonoBehaviour
    {
        public void Show(string reason) => StartCoroutine(Run(reason));

        IEnumerator Run(string reason)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            var black = Fullscreen("Black", Color.clear).GetComponent<Image>();
            var text = new GameObject("Text", typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(transform, false);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = reason;
            text.fontSize = 48;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 1f, 1f, 0f);
            var rt = text.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            for (float t = 0f; t < FadeTime; t += Time.unscaledDeltaTime)
            {
                black.color = new Color(0f, 0f, 0f, t / FadeTime);
                yield return null;
            }
            black.color = Color.black;

            yield return new WaitForSecondsRealtime(TextDelay);

            for (float t = 0f; t < FadeTime; t += Time.unscaledDeltaTime)
            {
                text.color = new Color(1f, 1f, 1f, t / FadeTime);
                yield return null;
            }
            text.color = Color.white;
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
