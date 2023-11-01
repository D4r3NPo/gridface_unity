using UnityEngine;
using UnityEngine.EventSystems;

public class GridButton : MonoBehaviour, IDropHandler
{
    [Header("Setting")]
    [SerializeField] Row Row;
    [SerializeField] Column Column;
    public Position Position => new(Column, Row);
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("Drop "+ eventData.pointerDrag);
        if (eventData.pointerDrag.TryGetComponent(out Finger finger)) 
            App.Instance.MoveFingerTo(finger.id, Position);
    }
}