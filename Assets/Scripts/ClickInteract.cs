using Microsoft.VisualBasic;
using UnityEngine;
using UnityEngine.EventSystems; 

[RequireComponent(typeof(Collider2D))]
public class ClickInteract : MonoBehaviour, IPointerDownHandler
{

    public virtual void Interact(GameObject interacted)
    {
        Debug.Log(interacted.name + " foi clicado pelo EventSystem! (mensagem padrão)");
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        Interact(gameObject);
    }
}
