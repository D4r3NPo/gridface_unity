using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class GridButton : MonoBehaviour
{
    [Header("Setting")]
    public Row Row;
    public Column Column;
    public Sprite[] FingerIcons;
    //Finger _finger;
    List<Finger> _fingers = new();
    
    public void AddFinger(Finger finger)
    {
        //_finger = finger;
        _fingers.AddIfNo(finger);
        UpdateVisual();
    }
    public void RemoveFinger(Finger finger)
    {
        //_finger = Finger.None;
        _fingers.RemoveIf(finger);
        UpdateVisual();
    }
    void UpdateVisual()
    {
        Text.text = "";
        foreach (Finger finger in _fingers) Text.text += finger + "\n";
        if (_fingers.Count > 0)
        {
            Assignement.enabled = true;
            Assignement.sprite = FingerIcons[(int)_fingers[0]];
        }
        else
        {
            Assignement.enabled = false;
            Assignement.sprite = null;
        }
    }

    [Header("Setup")]
    [SerializeField] Text Text;
    [SerializeField] Image Assignement;

    void OnValidate()
    {
        if (Text) Text.text = $"{Column} / {Row}";
    }
    void Awake()
    {
        name = Text.name = Assignement.name = $"{Row} | {Column}";
        Assignement.enabled = false;
        Text.text = "";
    }

    public void OnButton() => Manager.Instance.OnButton(this);
}