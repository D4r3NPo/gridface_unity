using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TouchFinger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Finger Finger;
    public GraphicRaycaster Raycaster;
    public EventSystem EventSystem;
    public Image Icon;

    void Awake()
    {
        Icon = GetComponent<Image>();
        Raycaster = GetComponentInParent<GraphicRaycaster>();
        EventSystem = EventSystem.current;
    }

    public void OnPointerDown(PointerEventData eventData) => Icon.color = new Color(Icon.color.r,Icon.color.g,Icon.color.b, 0.5f);

    public void OnPointerUp(PointerEventData eventData)
    {
        Icon.color = new Color(Icon.color.r,Icon.color.g,Icon.color.b, 1f);
        GridButton gridButton = GetGridButton();
        if (gridButton) Manager.Instance.OnTouchButton(this,);
    }

    GridButton GetGridButton()
    {
        PointerEventData pointerEventData = new PointerEventData(EventSystem) { position = Input.mousePosition };
        var results = new List<RaycastResult>();
        Raycaster.Raycast(pointerEventData, results);
        foreach (RaycastResult result in results)
            if (result.gameObject.transform.parent.TryGetComponent(out GridButton gridButton))
                return gridButton;
        return null;
    }
}
