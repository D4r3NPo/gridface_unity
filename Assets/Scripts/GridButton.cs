using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class GridButton : MonoBehaviour
{
    [Header("Setting")]
    public Row Row;
    public Column Column;
    public Sprite Pouce_L, Index_L, Majeur_L, Annulaire_L, Auriculaire_L, Pouce_R, Index_R, Majeur_R, Annulaire_R, Auriculaire_R;

    public List<Finger> Fingers = new List<Finger>();
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
        Text.text = "";
        foreach (var item in Fingers)
        {
            Text.text += item.ToString() + "\n";
        }
    }

    [Header("Setup")]
    public Text Text;
    public Image Assignement;

    private void OnValidate()
    {
        if (Text) Text.text = Column.ToString() + "/" + Row.ToString();
    }
    void Awake()
    {
        Assignement.enabled = false;
        Text.text = "";
    }

    public void OnButton() => Manager.Instance.OnButton(this);

}
/* public void SetFinger(Finger finger)
    {
        Assignement.color = finger != Finger.None ? Color.white : new Color(0f, 0f, 0f, 0f);
        Assignement.sprite = finger switch
        {
            Finger.Pouce_L => Pouce_L,
            Finger.Pouce_R => Pouce_R,
            Finger.Index_L => Index_L,
            Finger.Index_R => Index_R,
            Finger.Majeur_L => Majeur_L,
            Finger.Majeur_R => Majeur_R,
            Finger.Annulaire_L => Annulaire_L,
            Finger.Annulaire_R => Annulaire_R,
            Finger.Auriculaire_L => Auriculaire_L,
            Finger.Auriculaire_R => Auriculaire_R,
            Finger.None => null,
            _ => null
        };
    }*/