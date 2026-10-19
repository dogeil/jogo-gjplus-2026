using UnityEngine;
using UnityEngine.UI;

/// Inventario: grid 2 colunas (o ultimo, se sobrar um, centralizado embaixo), cada celula = icone + quantidade.
/// Vai num objeto vazio (RectTransform) filho do Canvas; posicao/ancora/tamanho sao do Rect Transform dele,
/// o grid fica centralizado nele. As celulas sao criadas em Awake. Le InventoryHandler.
[RequireComponent(typeof(RectTransform))]
public class InventoryHUD : MonoBehaviour
{
    [Tooltip("Ordem = enum Recurso: Madeira, Prego, Arame, Parafuso")]
    [SerializeField] Sprite[] icons = new Sprite[4];
    [SerializeField] Vector2 cellSize = new Vector2(150f, 70f);
    [SerializeField] Vector2 spacing = new Vector2(10f, 10f);
    [SerializeField] float iconSize = 56f;
    [SerializeField] int fontSize = 36;

    Text[] counts;
    int[] shown;

    void Awake()
    {
        int n = System.Enum.GetValues(typeof(Recurso)).Length;
        counts = new Text[n];
        shown = new int[n];

        // O Rect Transform deste objeto e seu; o grid fica centralizado nele
        var root = (RectTransform)transform;

        var font = UIFont.Get();
        for (int i = 0; i < n; i++)
        {
            var cell = new GameObject(((Recurso)i).ToString(), typeof(RectTransform)).GetComponent<RectTransform>();
            cell.SetParent(root, false);
            cell.anchorMin = cell.anchorMax = cell.pivot = new Vector2(0.5f, 0.5f);
            cell.sizeDelta = cellSize;
            cell.anchoredPosition = CellPos(i, n);

            var icon = new GameObject("Icon", typeof(Image)).GetComponent<Image>();
            icon.transform.SetParent(cell, false);
            icon.raycastTarget = false; // HUD nao pode bloquear clique nos itens da cena
            icon.sprite = i < icons.Length ? icons[i] : null;
            icon.enabled = icon.sprite != null;
            icon.preserveAspect = true;
            var irt = icon.rectTransform;
            irt.anchorMin = irt.anchorMax = irt.pivot = new Vector2(0f, 0.5f);
            irt.sizeDelta = new Vector2(iconSize, iconSize);
            irt.anchoredPosition = Vector2.zero;

            var t = new GameObject("Count", typeof(Text)).GetComponent<Text>();
            t.transform.SetParent(cell, false);
            t.raycastTarget = false;
            t.font = font;
            t.fontSize = fontSize;
            t.alignment = TextAnchor.MiddleLeft;
            t.color = Color.white;
            var trt = t.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(iconSize + 10f, 0f); trt.offsetMax = Vector2.zero;
            counts[i] = t;
            shown[i] = -1; // forca o primeiro Refresh
        }
    }

    // Centro da celula i (de n) relativo ao centro deste objeto. 2 colunas; se o ultimo ficar sozinho na linha, centraliza.
    Vector2 CellPos(int i, int n)
    {
        int rows = Mathf.CeilToInt(n / 2f);
        bool lonely = i == n - 1 && n % 2 == 1;
        float col = lonely ? 0f : (i % 2 == 0 ? -0.5f : 0.5f); // em passos de (celula + espaco)
        float row = (rows - 1) / 2f - i / 2;
        return new Vector2(col * (cellSize.x + spacing.x), row * (cellSize.y + spacing.y));
    }

    // Selecione o objeto: mostra no Scene view onde ficam as celulas (amarelo) e os icones (ciano).
    // Mexa em cellSize/spacing/iconSize no Inspector e o gizmo acompanha; posicao/tamanho do grupo = Rect Transform.
    void OnDrawGizmosSelected()
    {
        int n = System.Enum.GetValues(typeof(Recurso)).Length;
        Vector3 s = transform.lossyScale;
        for (int i = 0; i < n; i++)
        {
            Vector2 p = CellPos(i, n);
            Vector3 center = transform.TransformPoint(p);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(center, new Vector3(cellSize.x * s.x, cellSize.y * s.y, 0f));

            Vector3 iconCenter = transform.TransformPoint(p + new Vector2(-cellSize.x / 2f + iconSize / 2f, 0f));
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(iconCenter, new Vector3(iconSize * s.x, iconSize * s.y, 0f));
        }
    }

    // Sem evento no InventoryHandler: 5 ints por frame, so mexe no Text se mudou
    void Update()
    {
        for (int i = 0; i < counts.Length; i++)
        {
            int q = InventoryHandler.Quantidade((Recurso)i);
            if (q == shown[i]) continue;
            shown[i] = q;
            counts[i].text = q.ToString();
        }
    }
}
