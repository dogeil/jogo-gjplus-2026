using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(RoomView))]
public class EdgeTurn : MonoBehaviour
{
    enum Edge { None, Left, Right, Up, Down }

    [Header("Edge zones")]
    [SerializeField, Range(0.01f, 0.3f)] float sideEdgeSize = 0.08f;     // fração da largura
    [SerializeField, Range(0.01f, 0.3f)] float verticalEdgeSize = 0.08f; // fração da altura
    [SerializeField] float dwellTime = 0.1f;

    [Header("Behaviour")]
    [SerializeField] bool repeatWhileHeld = false; // só afeta esquerda/direita
    [SerializeField] float repeatDelay = 0.45f;
    [SerializeField] bool confineCursor = true;

    RoomView room;
    Edge heldEdge;
    float heldTimer;
    bool armed = true;

    void Awake() => room = GetComponent<RoomView>();

    void OnEnable()  { if (confineCursor) Cursor.lockState = CursorLockMode.Confined; }
    void OnDisable() { if (confineCursor) Cursor.lockState = CursorLockMode.None; }

    void Update()
    {
        Edge edge = GetEdge();

        if (edge != heldEdge)
        {
            heldEdge = edge;
            heldTimer = 0f;
            armed = true;
        }

        if (edge == Edge.None) return;

        heldTimer += Time.deltaTime;
        if (!armed || room.IsTurning || heldTimer < dwellTime) return;

        switch (edge)
        {
            case Edge.Left:  room.TurnLeft();  break;
            case Edge.Right: room.TurnRight(); break;
            case Edge.Up:    room.LookUp();    break;
            case Edge.Down:  room.LookDown();  break;
        }

        bool horizontal = edge == Edge.Left || edge == Edge.Right;
        if (repeatWhileHeld && horizontal)
            heldTimer = dwellTime - repeatDelay;
        else
            armed = false; // precisa sair da borda pra ativar de novo
    }

    Edge GetEdge()
    {
        if (!Application.isFocused) return Edge.None;

        var mouse = Mouse.current;
        if (mouse == null) return Edge.None;

        Vector2 p = mouse.position.ReadValue();
        if (p.x < 0f || p.y < 0f || p.x > Screen.width || p.y > Screen.height) return Edge.None;

        float sx = Screen.width * sideEdgeSize;
        float sy = Screen.height * verticalEdgeSize;

        if (p.x <= sx)                 return Edge.Left;
        if (p.x >= Screen.width - sx)  return Edge.Right;
        if (p.y >= Screen.height - sy) return Edge.Up;    // Y da tela cresce pra cima
        if (p.y <= sy)                 return Edge.Down;
        return Edge.None;
    }
}