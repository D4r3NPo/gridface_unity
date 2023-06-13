using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TouchFinger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,IPointerMoveHandler
{
    [FormerlySerializedAs("Finger")] public Finger finger;
    GraphicRaycaster _raycaster;
    EventSystem _eventSystem;
    RectTransform _rectTransform;
    Image _icon;
    Vector3 _initPosition;
    bool _drag;

    void Awake()
    {
        _icon = GetComponent<Image>();
        _raycaster = GetComponentInParent<GraphicRaycaster>();
        _eventSystem = EventSystem.current;
        //_rectTransform = GetComponent<RectTransform>();
        //_initPosition = _rectTransform.;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        //_drag = true;
        _icon.color = new Color(_icon.color.r, _icon.color.g, _icon.color.b, 0.5f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        //_drag = false;
        //_rectTransform.position = _initPosition;
        _icon.color = new Color(_icon.color.r,_icon.color.g,_icon.color.b, 1f);
        GridButton gridButton = GetGridButton();
        if (gridButton) Manager.Instance.MoveFingerTo(finger,gridButton);
    }

    GridButton GetGridButton()
    {
        PointerEventData pointerEventData = new(_eventSystem) { position = Input.mousePosition };
        var results = new List<RaycastResult>();
        _raycaster.Raycast(pointerEventData, results);
        foreach (RaycastResult result in results)
            if (result.gameObject.transform.parent.TryGetComponent(out GridButton gridButton))
                return gridButton;
        return null;
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        //if(_drag) _rectTransform.position = eventData.position;
    }
}
