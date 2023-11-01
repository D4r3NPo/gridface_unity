using UnityEngine;
using UnityEngine.UI;

public class VideoButton : MonoBehaviour
{
    public Text Text;
    public string Name { get => name; set { name = value; Text.text = value; } }
    public void OnButton() => App.Instance.LoadVideo(Name);
}
