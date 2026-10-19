using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// Loop dos dias, num GameObject vazio da cena. Tabela de dias no Inspector.
/// Cada dia: duração, ritmo e quais inimigos entram.
/// Sobreviver ao relógio = próximo dia (recarrega a cena, tudo reseta). Perder = botão pra reiniciar o mesmo dia.
/// Zerar o relógio no último dia = perda forçada (final do jogo).
public class DayManager : MonoBehaviour
{
    [Serializable]
    public class DayConfig
    {
        [Tooltip("Segundos do dia")] public float duration = 120f;
        [Tooltip("Multiplicador de ritmo: intervalos dos inimigos e crescimento do tentáculo dividem por isso")] public float speed = 1f;
        public bool backDoor;
        [Tooltip("Desligado: só o vulto da janela direita")] public bool bothWindows;
        [Tooltip("Parafusos do teto começam a cair")] public bool ceiling;
        public bool tentacles;
    }

    [SerializeField] DayConfig[] days =
    {
        new DayConfig { duration = 120f, speed = 1f },
        new DayConfig { duration = 180f, speed = 1.5f, backDoor = true },
        new DayConfig { duration = 210f, speed = 2f, backDoor = true, bothWindows = true, ceiling = true, tentacles = true },
        new DayConfig { duration = 240f, speed = 2.5f, backDoor = true, bothWindows = true, ceiling = true, tentacles = true },
        new DayConfig { duration = 280f, speed = 5f, backDoor = true, bothWindows = true, ceiling = true, tentacles = true },
    };

    [Tooltip("Multiplica o speed de todos os dias: 0.5 = inimigos com metade do ritmo (mais fácil), 1 = tabela pura")] public float speedScale = 0.5f;
    [SerializeField, Tooltip("Dia em que o jogo começa (só na 1ª vez; depois vale o dia salvo)")] int startDay = 1;
    [SerializeField] string dayWinMessage = "Você sobreviveu ao dia {0}!";
    [SerializeField] string finalMessage = "Eles entraram.";
    [SerializeField] string finalHint = "Fim de jogo";
    [SerializeField] string menuScene = "Menu"; // precisa estar no Build Profiles

    public static DayManager Instance { get; private set; }
    public static int Day { get; private set; }

    public DayConfig Today => days[Mathf.Clamp(Day, 1, days.Length) - 1];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() => Day = 0; // statics sobrevivem sem domain reload no editor

    void Awake()
    {
        Instance = this;
        if (Day < 1) Day = startDay;
    }

    void Start() => StartCoroutine(FadeIn());

    /// Relógio chegou a zero.
    public void EndDay()
    {
        if (Day >= days.Length) GameManager.Lose(finalMessage, finalHint, "Voltar ao menu", ToMenu);
        else GameManager.Win(string.Format(dayWinMessage, Day), () => Reload(Day + 1));
    }

    /// Recarrega a cena no dia dado; zera tudo que é estático.
    public static void Reload(int day)
    {
        Day = day;
        ResetStatics();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void ToMenu()
    {
        Day = 0; // o próximo jogo começa do dia 1
        ResetStatics();
        SceneManager.LoadScene(menuScene);
    }

    static void ResetStatics()
    {
        GameManager.Reset();
        Anxiety.ResetAll();
        InventoryHandler.Reset();
        SecurityManager.Reset();
        Interactable.ResetBusy();
        Time.timeScale = 1f;
    }

    // Tela preta com "Dia N" que clareia
    IEnumerator FadeIn()
    {
        var canvas = new GameObject("DayIntro", typeof(Canvas)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150; // abaixo da tela de fim (200)

        var black = Stretch(new GameObject("Black", typeof(Image)), canvas.transform).GetComponent<Image>();
        black.color = Color.black;
        black.raycastTarget = false;

        var text = Stretch(new GameObject("Day", typeof(Text)), canvas.transform).GetComponent<Text>();
        text.font = UIFont.Get();
        text.text = $"Dia {Day}";
        text.fontSize = 64;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;

        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime) // texto surge
        {
            text.color = new Color(1f, 1f, 1f, t);
            yield return null;
        }
        text.color = Color.white;
        yield return new WaitForSecondsRealtime(1f);

        for (float t = 0f; t < 1.5f; t += Time.unscaledDeltaTime) // clareia
        {
            float a = 1f - t / 1.5f;
            black.color = new Color(0f, 0f, 0f, a);
            text.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }
        Destroy(canvas.gameObject);
    }

    static Transform Stretch(GameObject go, Transform parent)
    {
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return go.transform;
    }
}
