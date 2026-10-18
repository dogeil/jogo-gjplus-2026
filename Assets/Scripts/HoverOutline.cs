using UnityEngine;
using UnityEngine.EventSystems;

/// Outline básico no hover: 8 cópias do sprite em branco chapado (shader SolidWhiteSprite), deslocadas, atrás do original.
/// Auto-adicionado por DraggableObject e ClickInteract.
public class HoverOutline : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float width = 0.06f; // unidades de mundo (6 px a PPU 100)

    private static Material _solid;
    private static Material SolidMaterial =>
        _solid != null ? _solid : (_solid = new Material(Resources.Load<Shader>("SolidWhiteSprite")));

    private SpriteRenderer _sr;
    private GameObject _outline;

    private void Awake() => _sr = GetComponentInChildren<SpriteRenderer>();

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_outline != null || _sr == null) return;

        _outline = new GameObject("Outline");
        _outline.transform.SetParent(_sr.transform, false);

        Vector3 s = _sr.transform.lossyScale;
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;
            var go = new GameObject("o" + i);
            go.transform.SetParent(_outline.transform, false);
            go.transform.localPosition = new Vector3(Mathf.Cos(a) * width / s.x, Mathf.Sin(a) * width / s.y, 0f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _sr.sprite;
            sr.flipX = _sr.flipX;
            sr.flipY = _sr.flipY;
            sr.sharedMaterial = SolidMaterial;
            sr.sortingLayerID = _sr.sortingLayerID;
            sr.sortingOrder = _sr.sortingOrder - 1;
        }
    }

    public void OnPointerExit(PointerEventData eventData) => Clear();

    private void OnDisable() => Clear();

    private void Clear()
    {
        if (_outline != null) Destroy(_outline);
        _outline = null;
    }
}
