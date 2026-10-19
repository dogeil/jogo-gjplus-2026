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

        if (remaining <= 0f) GameManager.Win(winMessage);
    }

    // ponytail: debug via OnGUI, remover antes de entregar
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 250, 220, 40));
        if (GUILayout.Button("Tempo = 10s")) remaining = Mathf.Min(remaining, 10f);
        GUILayout.EndArea();
    }
}
