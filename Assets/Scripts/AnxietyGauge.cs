using UnityEngine;
using UnityEngine.UI;

/// Medidor de pressao: ansiedade 0-100 gira o ponteiro 0-360 graus (horario, 0 = apontando pra cima)
/// e pinta de vermelho a "fatia de pizza" que o ponteiro ja percorreu. Voltando, a fatia some junto.
public class AnxietyGauge : MonoBehaviour
{
    [SerializeField] RectTransform needle;  // pivot no centro de rotacao (a base do ponteiro)
    [SerializeField] Image fill;            // circulo vermelho ATRAS do ponteiro (qualquer sprite redondo); o script configura o Filled
    [SerializeField] Color fillColor = new Color(0.85f, 0.1f, 0.1f, 0.7f);

    void Awake()
    {
        if (fill == null) return;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Radial360;
        fill.fillOrigin = (int)Image.Origin360.Top;
        fill.fillClockwise = true;
        fill.color = fillColor;
        fill.raycastTarget = false;
    }

    void Update()
    {
        float t = Anxiety.Level / 100f;
        if (needle != null) needle.localRotation = Quaternion.Euler(0f, 0f, -t * 360f);
        if (fill != null) fill.fillAmount = t;
    }
}
