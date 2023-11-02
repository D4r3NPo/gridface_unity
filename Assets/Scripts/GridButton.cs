using UnityEngine;
using UnityEngine.EventSystems;

public class GridButton : MonoBehaviour, IDropHandler , IPointerClickHandler
{
    [Header("Setting")]
    [SerializeField] Row Row;
    [SerializeField] Column Column;
    public Position Position => new(Column, Row);
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag.TryGetComponent(out Finger finger)) 
            App.Instance.MoveFingerTo(finger.id, Position);
    }

    static Finger.ID GetFinger()
    {
        if (Input.GetKey(KeyCode.C)) return Input.GetKey(KeyCode.Space) ? Finger.ID.P_R : Finger.ID.P_L; 
        if (Input.GetKey(KeyCode.F)) return Input.GetKey(KeyCode.Space) ? Finger.ID.I_R : Finger.ID.I_L; 
        if (Input.GetKey(KeyCode.E)) return Input.GetKey(KeyCode.Space) ? Finger.ID.M_R : Finger.ID.M_L; 
        if (Input.GetKey(KeyCode.W)) return Input.GetKey(KeyCode.Space) ? Finger.ID.An_R : Finger.ID.An_L; 
        if (Input.GetKey(KeyCode.A)) return Input.GetKey(KeyCode.Space) ? Finger.ID.Au_R : Finger.ID.Au_L;
        return Finger.ID.None;
    }
    public void OnPointerClick(PointerEventData eventData) => App.Instance.MoveFingerTo(GetFinger(), Position);
}