using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

[RequireComponent(typeof(Collider2D))]
public class DraggableObject : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private bool _held; // suspenso sem gravidade até o jogador tocar de novo
    private Camera _mainCamera;
    private RoomView _room;
    private Rigidbody2D _rb;
    private float _defaultGravity;
    private Vector3 _dragOffset;
    private Vector2 _lastPointerPos;
    private bool _dragging;

    private void Awake()
    {
        _mainCamera = Camera.main;
        _room = FindFirstObjectByType<RoomView>();
        _rb = GetComponent<Rigidbody2D>();
        if (!TryGetComponent<HoverOutline>(out _)) gameObject.AddComponent<HoverOutline>();
        if (_rb != null) _defaultGravity = _rb.gravityScale;

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Debug.Log("Created a missing EventSystem automatically!");
        }

        if (_mainCamera != null && _mainCamera.GetComponent<Physics2DRaycaster>() == null)
            _mainCamera.gameObject.AddComponent<Physics2DRaycaster>();
    }

    private void Start()
    {
        // Itens colocados no editor ou carregados pelo RoomItems já ficam com a gravidade certa
        UpdateGravity();
    }

    /// Fica sem gravidade (flutuando) até o próximo toque do jogador.
    public void Suspend()
    {
        _held = true;
        UpdateGravity();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_held) return;
        _held = false;
        UpdateGravity();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _held = false;
        _dragging = true;
        _lastPointerPos = eventData.position;

        transform.SetParent(null, true);
        _dragOffset = transform.position - GetMouseWorldPosition(_lastPointerPos);

        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.simulated = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        _lastPointerPos = eventData.position;
    }

    public virtual void OnEndDrag(PointerEventData eventData)
    {
        _dragging = false;
        _lastPointerPos = eventData.position;
        transform.position = GetMouseWorldPosition(_lastPointerPos) + _dragOffset;

        if (_room != null) _room.AttachToWall(transform);

        UpdateGravity();

        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            _rb.simulated = true;
        }

        // Soltou em cima de algo interagível? Tenta usar esta ferramenta nele.
        foreach (var col in Physics2D.OverlapPointAll(transform.position))
            if (col.TryGetComponent(out Interactable it) && it.TryStart(this)) break;
    }

    /// Sem gravidade no chão/teto, gravidade original nas paredes.
    public void UpdateGravity()
    {
        if (_rb == null || _room == null) return;

        bool noGravity = _held || _room.IsFloor(transform.parent);
        _rb.gravityScale = noGravity ? 0f : _defaultGravity;

        if (noGravity)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }
    }

    private void LateUpdate()
    {
        // Jogador olhou pro lado: o martelo suspenso cai
        if (_held && _room != null && _room.IsTurning)
        {
            _held = false;
            UpdateGravity();
        }

        if (!_dragging)
        {
            // Caiu da parede/teto -> aparece em ponto aleatório do chão
            if (_room != null && _room.FellOffSurface(transform))
            {
                _room.DropOnFloor(transform);
                if (_rb != null)
                {
                    _rb.linearVelocity = Vector2.zero;
                    _rb.angularVelocity = 0f;
                }
                UpdateGravity();
            }
            return;
        }

        if (Mouse.current != null)
            _lastPointerPos = Mouse.current.position.ReadValue();

        transform.position = GetMouseWorldPosition(_lastPointerPos) + _dragOffset;
    }

    private Vector3 GetMouseWorldPosition(Vector2 screenPosition)
    {
        Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));
        worldPos.z = transform.position.z;
        return worldPos;
    }
}