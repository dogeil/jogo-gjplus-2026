using UnityEngine;
using UnityEngine.UI;

// Fades out the turn-arrow hints after a delay. Put on the Canvas (or any parent of the arrows).
public class ArrowHintFade : MonoBehaviour
{
    [Tooltip("Leave empty to use every Image under this object.")]
    [SerializeField] Graphic[] arrows;
    [SerializeField] float delay = 10f;
    [SerializeField] float fadeTime = 1f;

    float t;

    void Awake()
    {
        if (arrows == null || arrows.Length == 0)
            arrows = GetComponentsInChildren<Image>(true);
    }

    void Update()
    {
        t += Time.unscaledDeltaTime;
        if (t < delay) return;

        float a = 1f - Mathf.Clamp01((t - delay) / fadeTime);
        foreach (var g in arrows)
        {
            var c = g.color;
            c.a = a;
            g.color = c;
        }
        if (a <= 0f)
        {
            foreach (var g in arrows) g.gameObject.SetActive(false);
            enabled = false;
        }
    }
}
