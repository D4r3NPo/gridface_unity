using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Finger : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerClickHandler
{
    public ID id;
    [SerializeField] CanvasGroup m_canvasGroup;
    Transform m_initialParent;
    float m_lastClickTime;
    const float doubleClickDelay = 0.5f;
    void Awake() => m_initialParent = transform.parent;

    void OnValidate()
    {
        if(id != ID.None) GetComponent<Image>().sprite = id.Icon();
    }
    void OnEnable() => App.Instance.PositionChanged += OnPositionChanged;
    void OnDisable() => App.Instance.PositionChanged -= OnPositionChanged;

    void OnPositionChanged(ID finger, Position position)
    {
        Debug.Log($"Position of {finger} has changed to {position}");
        if (finger == id)
        {
            Debug.Log("I'm concerned");
            bool hasPosition = position == Position.None;
            Debug.Log(hasPosition);
            transform.SetParent(hasPosition
                ? m_initialParent
                : App.Instance.GridButtons.Find(x => x.Position == position).transform,false);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        m_canvasGroup.alpha = 0.5f;
        m_canvasGroup.blocksRaycasts = false;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        m_canvasGroup.alpha = 1f;
        m_canvasGroup.blocksRaycasts = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // TODO Follow mouse positon
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (Time.time - m_lastClickTime < doubleClickDelay) App.Instance.MoveFingerTo(id, Position.None);
        m_lastClickTime = Time.time;
    }
    public enum ID { None, P_L, I_L, M_L, An_L, Au_L, P_R, I_R, M_R, An_R, Au_R }
}

/*GridButton GetGridButton()
   {
       PointerEventData pointerEventData = new(_eventSystem) { position = Input.mousePosition };
       var results = new List<RaycastResult>();
       _raycaster.Raycast(pointerEventData, results);
       foreach (RaycastResult result in results)
           if (result.gameObject.transform.parent.TryGetComponent(out GridButton gridButton))
               return gridButton;
       return null;
   }*/