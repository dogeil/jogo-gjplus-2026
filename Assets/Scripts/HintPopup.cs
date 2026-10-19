using UnityEngine;
using UnityEngine.UI;

/// Botao de dica: abre/fecha um pop up quase em tela cheia, acima do HUD, e pausa o jogo (timeScale 0).
/// O pop up e criado em runtime num Canvas proprio. Fundo escurecido (blur de verdade pede shader/renderer feature).
public class HintPopup : MonoBehaviour
{
    [SerializeField] Button hintButton;   // precisa ter o componente Button
    [SerializeField, TextArea(4, 12)] string hintText = "Dica aqui.";
    [SerializeField] int fontSize = 40;
    [SerializeField] Color dimColor = new Color(0f, 0f, 0f, 0.7f);
    [SerializeField] Color panelColor = new Color(0.08f, 0.08f, 0.08f, 0.95f);
    [SerializeField, Range(0f, 0.3f)] float screenMargin = 0.06f; // fracao da tela deixada livre em cada lado

    GameObject popup;
    RoomView room;
    bool wasLocked;

    void Awake()
    {
        room = FindFirstObjectByType<RoomView>();
        if (hintButton != null) hintButton.onClick.AddListener(Toggle);
        Build();
    }

    void OnDestroy()
    {
        if (hintButton != null) hintButton.onClick.RemoveListener(Toggle);
        if (popup == null) return;
        if (popup.activeSelf) Time.timeScale = 1f; // saiu da cena com o pop up aberto
        Destroy(popup);
    }

    public void Toggle()
    {
        if (popup.activeSelf) Close();
        else if (!GameManager.Lost) Open();
    }

    void Open()
    {
        popup.SetActive(true);
        Time.timeScale = 0f;
        if (room != null) { wasLocked = room.Locked; room.Locked = true; } // teclas A/D/W/S nao giram durante o pause
    }

    void Close()
    {
        popup.SetActive(false);
        Time.timeScale = 1f;
        if (room != null && !GameManager.Lost) room.Locked = wasLocked;
    }

    void Build()
    {
        popup = new GameObject("HintPopup", typeof(Canvas), typeof(GraphicRaycaster));
        var canvas = popup.GetComponent<Canvas>(); // raiz (sem pai) = Overlay proprio, independente do Canvas do HUD
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150; // acima do HUD, abaixo da tela de fim de fase (200)

        // Fundo escurecido em tela cheia: tambem bloqueia clique no jogo
        var dim = Stretch(new GameObject("Dim", typeof(Image), typeof(Button)).GetComponent<Image>().rectTransform, popup.transform, 0f);
        dim.GetComponent<Image>().color = dimColor;

        var panel = Stretch(new GameObject("Panel", typeof(Image), typeof(Button)).GetComponent<Image>().rectTransform, popup.transform, screenMargin);
        panel.GetComponent<Image>().color = panelColor;

        // Clicar em qualquer lugar do pop up (fundo ou painel) fecha
        foreach (var b in new[] { dim.GetComponent<Button>(), panel.GetComponent<Button>() })
        {
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(Close);
        }

        var text = new GameObject("Text", typeof(Text)).GetComponent<Text>();
        Stretch(text.rectTransform, panel, 0.04f);
        text.raycastTarget = false;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.UpperLeft;
        text.color = Color.white;
        text.text = hintText;

        popup.SetActive(false);
    }

    // Preenche o pai deixando `margin` (fracao) livre em cada lado
    static RectTransform Stretch(RectTransform rt, Transform parent, float margin)
    {
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(margin, margin);
        rt.anchorMax = new Vector2(1f - margin, 1f - margin);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }
}
