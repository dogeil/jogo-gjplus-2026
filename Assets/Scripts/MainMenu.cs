using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// Menu principal, num objeto qualquer da cena Menu. COMEÇAR carrega o jogo, SAIR fecha.
/// Botoes vazios = acha pelo texto ("COMEÇAR" / "SAIR") entre os Buttons da cena.
public class MainMenu : MonoBehaviour
{
    [SerializeField] Button playButton;
    [SerializeField] Button quitButton;
    [SerializeField] string gameScene = "SampleScene"; // precisa estar no Build Profiles / Scene List

    void Awake()
    {
        Time.timeScale = 1f; // caso volte ao menu de um jogo pausado
        if (playButton == null) playButton = FindByLabel("COMEÇAR");
        if (quitButton == null) quitButton = FindByLabel("SAIR");
        if (playButton != null) playButton.onClick.AddListener(Play);
        if (quitButton != null) quitButton.onClick.AddListener(Quit);
    }

    public void Play() => SceneManager.LoadScene(gameScene);

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    static Button FindByLabel(string label)
    {
        foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var t = b.GetComponentInChildren<TMP_Text>(true);
            if (t != null && t.text.Trim().ToUpperInvariant() == label) return b;
        }
        return null;
    }
}
