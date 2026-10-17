using UnityEngine;
using UnityEngine.EventSystems;

public class Alicate : DraggableObject
{
    public override void OnEndDrag(PointerEventData eventData)
    {
        base.OnEndDrag(eventData);
        Debug.Log(name + " foi solto!");
        // usar ferramenta aqui
    }
}
