using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class GridButton : MonoBehaviour
{
    [Header("Setting")]
    public Row Row;
    public Column Column;
    public Sprite[] FingerIcons;
    public List<Finger> Fingers = new();
    public void AddFinger(Finger finger)
    {
        Fingers.AddIfNo(finger);
        UpdateVisual();
    }
    public void RemoveFinger(Finger finger)
    {
        Fingers.RemoveIf(finger);
        UpdateVisual();
    }
    void UpdateVisual()
    {
        if (Fingers.Count > 0)
        {
            Assignement.sprite = FingerIcons[(int)Fingers[0]];
            Assignement.enabled = true;
        }
        else Assignement.enabled = false;
        //Text.text = "";
        //foreach (Finger finger in Fingers) Text.text += finger + "\n";
    }

    [Header("Setup")]
    public Text Text;
    public Image Assignement;

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