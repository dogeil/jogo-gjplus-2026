using TMPro;
using UnityEngine;

/// Relogio regressivo mm:ss. Chega a 00:00 -> vitoria (fade to black + texto). Para se a fase ja acabou.
public class ClockTimer : MonoBehaviour
{
    [SerializeField] TMP_Text label;          // vazio = pega o do proprio objeto
    [SerializeField] float duration = 120f;   // s
    [SerializeField] string winMessage = "Você sobreviveu o dia!";

    float remaining;
    int shown = -1;

    void Awake()
    {
        if (label == null) label = GetComponent<TMP_Text>();
        remaining = duration;
    }

    void Start()
    {
        if (DayManager.Instance == null) return;
        duration = remaining = DayManager.Instance.Today.duration; // o dia manda na duração
    }

    void Update()
    {
        if (GameManager.Lost) return;
        remaining = Mathf.Max(0f, remaining - Time.deltaTime);

        int s = Mathf.CeilToInt(remaining); // 02:00 dura 1s inteiro, 00:00 so no fim
        if (s != shown)
        {
            shown = s;
            label.text = $"{s / 60:00}:{s % 60:00}";
        }

        if (remaining <= 0f)
        {
            if (DayManager.Instance != null) DayManager.Instance.EndDay();
            else GameManager.Win(winMessage);
        }
    }
}
