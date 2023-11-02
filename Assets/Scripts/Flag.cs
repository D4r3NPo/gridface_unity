using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Flag : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] Image m_icon;

    public bool Enable
    {
        set
        {
            var color = m_icon.color;
            color.a = value ? 1f : 0.5f;
            m_icon.color = color;
        }
    }

    public void OnPointerClick(PointerEventData eventData) => App.Instance.ToggleFlag();
}
