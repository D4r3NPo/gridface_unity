using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;
using File = System.IO.File;

public enum Row { Un, Deux, Trois, Quatre, Cinq, Six, Sept, Huit, Neuf, Dix, Onze, Douze, Treize, Quatoze, Quinze, None = -1 }
public enum Column { A, B, C, D, E, F, G, H, I, J, K, L, M, None= -1 }
public enum Finger { None, P_L, I_L, M_L, An_L, Au_L, P_R, I_R, M_R, An_R, Au_R }

public class Manager : MonoBehaviour
{
    public static Manager Instance;

    [Header("--- Setting ---")] public long Step;

    #region Components

    [Header("--- Setup ---")] 
    public GraphicRaycaster Raycaster;
    public EventSystem EventSystem;
    public RawImage RawImage;
    public Slider Timeline;
    public VideoPlayer Player;
    public Text Console;
    public Transform VideoGrid;
    public GameObject VideoButton;
    public GameObject Videos;
    public List<GridButton> GridButtons = new List<GridButton>();

    #endregion

    #region Path

    readonly string _rootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "GridFace");
    string Videopath => Path.Combine(_rootPath, "Video");
    string Datapath => Path.Combine(_rootPath, "Data");

    #endregion
    
    public string LoadedVideo = string.Empty;

    void Awake() => Instance = this;

    void Start()
    {
        if (!Directory.Exists(Videopath)) Directory.CreateDirectory(Videopath);
        if (!Directory.Exists(Datapath)) Directory.CreateDirectory(Datapath);
    }

    #region Controls

    IEnumerator SimplePrevious()
    {
        long startFrame = Player.frame;
        Player.frame = Player.frame - Step <= 0 ? 0 : Player.frame - Step;
        while (Player.frame == startFrame) yield return null;
        Current = Frames.Exists(x => x.Frame == Player.frame)
            ? Frames.Find(x => x.Frame == Player.frame).ToData()
            : new FrameAnalysisData();
        UpdateGridButton();
    }

    IEnumerator CopyPrevious()
    {
        FrameAnalysisData save = Current;
        long startFrame = Player.frame;
        Player.frame = Player.frame - Step <= 0 ? 0 : (Player.frame - Step);
        while (Player.frame == startFrame) yield return null;
        Current = save;
        UpdateCsv();
        UpdateGridButton();
    }

    IEnumerator SimpleNext()
    {
        long startFrame = Player.frame;
        Player.frame = Player.frame + Step >= (long)Player.frameCount ? (long)Player.frameCount : Player.frame + Step;
        while (Player.frame == startFrame) yield return null;
        Current = Frames.Exists(x => x.Frame == Player.frame)
            ? Frames.Find(x => x.Frame == Player.frame).ToData()
            : new FrameAnalysisData();
        UpdateGridButton();
    }

    IEnumerator CopyNext()
    {
        FrameAnalysisData save = Current;
        long startFrame = Player.frame;
        Player.frame = Player.frame + Step >= (long)Player.frameCount ? (long)Player.frameCount : (Player.frame + Step);
        while (Player.frame == startFrame) yield return null;
        Current = save;
        UpdateCsv();
        UpdateGridButton();
    }

    IEnumerator SeekingFor(float time)
    {
        long startFrame = Player.frame;

        Player.frame = (long)time;

        while (Player.frame == startFrame) yield return null;

        Current = Frames.Exists(x => x.Frame == Player.frame)
            ? Frames.Find(x => x.Frame == Player.frame).ToData()
            : new FrameAnalysisData();
        UpdateGridButton();
    }

    public void Next(bool copy) => StartCoroutine(copy ? CopyNext() : SimpleNext());
    public void Previous(bool copy) => StartCoroutine(copy ? CopyPrevious() : SimplePrevious());

    public void SeekFor(float time)
    {
        if (time % Step == 0 && IsVideoLoaded)
        {
            StopAllCoroutines();
            StartCoroutine(SeekingFor(time));
        }
    }


    #endregion

    public void DisplayVideos()
    {
        if (!Videos.activeSelf)
        {
            Videos.SetActive(true);
            Instantiate(VideoButton.GetComponentInChildren<VideoButton>(), VideoGrid).Name = "CLOSE";
            foreach (FileInfo file in new DirectoryInfo(Videopath).GetFiles("*.mp4"))
                if (file.Name[0] != '.')
                    Instantiate(VideoButton.GetComponentInChildren<VideoButton>(), VideoGrid).Name = file.Name;
        }
    }

    public void LoadVideo(string videoName) => StartCoroutine(LoadingVideo(videoName));

    IEnumerator LoadingVideo(string videoName)
    {
        if (videoName != "CLOSE")
        {
            Player.url = "file://" + Videopath + "/" + videoName;
            LoadedVideo = videoName;
            while (Player.frameCount == 0) yield return null;
            Timeline.maxValue = Player.frameCount;
            LoadCSV();
        }

        Videos.SetActive(false);
        VideoGrid.ClearChild();
    }

    public FrameAnalysisData Current = new FrameAnalysisData();
    public List<FrameAnalysis> Frames;

    void LoadCSV()
    {
        //Find Curren Load Video Path
        string path = Path.Combine(_rootPath, Datapath, LoadedVideo.Split('.')[0] + ".csv");

        //Init Frames
        Frames = new List<FrameAnalysis>();

        //Create File if it doesn'n exist
        if (!File.Exists(path)) File.Create(path);
        else
        {
            //Load Frames from file
            var frames = File.ReadAllLines(path);
            foreach (var line in frames)
            {
                //Skip Title
                if (line == frames[0]) continue;

                var stringValues = line.Split(',');
                var values = new int[stringValues.Length];
                for (var i = 0; i < values.Length; i++)
                    values[i] = stringValues[i] != string.Empty ? int.Parse(stringValues[i]) : -1;

                FrameAnalysis newFrame = new FrameAnalysis(values[0])
                {
                    Fingers =
                    {
                        [Finger.P_L] = new Vector2Int(values[1], values[2]),
                        [Finger.I_L] = new Vector2Int(values[3], values[4]),
                        [Finger.M_L] = new Vector2Int(values[5], values[6]),
                        [Finger.An_L] = new Vector2Int(values[7], values[8]),
                        [Finger.Au_L] = new Vector2Int(values[9], values[10]),
                        [Finger.P_R] = new Vector2Int(values[11], values[12]),
                        [Finger.I_R] = new Vector2Int(values[13], values[14]),
                        [Finger.M_R] = new Vector2Int(values[15], values[16]),
                        [Finger.An_R] = new Vector2Int(values[17], values[18]),
                        [Finger.Au_R] = new Vector2Int(values[19], values[20])
                    }
                };

                Frames.Add(newFrame);
            }
        }

        Current = Frames.Exists(x => x.Frame == 0) ? Frames.Find(x => x.Frame == 0).ToData() : new FrameAnalysisData();
        UpdateGridButton();

    }

    float Zoom
    {
        get => RawImage.uvRect.width;
        set => RawImage.uvRect = new Rect(OffSet.x, OffSet.y, value, value);
    }

    Vector2 OffSet
    {
        get => new Vector2(RawImage.uvRect.x, RawImage.uvRect.y);
        set => RawImage.uvRect = new Rect(value, RawImage.uvRect.size);
    }

    void Update()
    {
        if (Player.url != string.Empty)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
                Previous(Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift));
            if (Input.GetKeyDown(KeyCode.RightArrow))
                Next(Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift));
            Console.text = "t : " + Player.time + "/" + Player.length.ToString("0.1") + "\n" + "f : " + Player.frame +
                           "/" + Player.frameCount;
        }
        else Console.text = NoVideo;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            doShow = !doShow;
        }

        if (Input.GetKeyDown(KeyCode.P)) Zoom -= 0.1f;
        if (Input.GetKeyDown(KeyCode.M)) Zoom += 0.1f;

        if (Input.GetKeyDown(KeyCode.L)) OffSet = new Vector2(OffSet.x + 0.1f, OffSet.y);
        if (Input.GetKeyDown(KeyCode.I)) OffSet = new Vector2(OffSet.x, OffSet.y + 0.1f);
        if (Input.GetKeyDown(KeyCode.J)) OffSet = new Vector2(OffSet.x - 0.1f, OffSet.y);
        if (Input.GetKeyDown(KeyCode.K)) OffSet = new Vector2(OffSet.x, OffSet.y - 0.1f);


    }

    const string NoVideo = "No video loaded";

    readonly Dictionary<Finger, GridButton> Fingers = new Dictionary<Finger, GridButton>
    {
        { Finger.Au_L, null }, { Finger.An_L, null }, { Finger.M_L, null }, { Finger.I_L, null }, { Finger.P_L, null },
        { Finger.Au_R, null }, { Finger.An_R, null }, { Finger.M_R, null }, { Finger.I_R, null }, { Finger.P_R, null }
    };

    bool IsVideoLoaded => LoadedVideo != string.Empty;

    void UpdateGridButton()
    {
        foreach (var finger in Current.Fingers)
        foreach (GridButton button in GridButtons)
        {
            if (new Vector2Int((int)button.Row, (int)button.Column) == finger.Value)
            {
                button.AddFinger(finger.Key);
                Fingers[finger.Key]?.RemoveFinger(finger.Key);
                Fingers[finger.Key] = button;
            }
            else button.RemoveFinger(finger.Key);
        }
    }

    public void MoveFingerTo(Finger finger, GridButton gridButton)
    {
        if (!IsVideoLoaded) return;

        Fingers[finger]?.RemoveFinger(finger);

        if (gridButton == Fingers[finger])
        {
            Fingers[finger] = null;
            Current.Fingers[finger] = new Vector2Int((int)Row.None, (int)Column.None);
        }
        else
        {
            Fingers[finger] = gridButton;
            Fingers[finger].AddFinger(finger);
            Current.Fingers[finger] = new Vector2Int((int)gridButton.Row, (int)gridButton.Column);
        }

        UpdateCsv();
    }

    Finger GetFinger() =>
        Input.GetKey(KeyCode.Space)
            ? Input.GetKey(KeyCode.C)
                ? Finger.P_R
                : Input.GetKey(KeyCode.F)
                    ? Finger.I_R
                    : Input.GetKey(KeyCode.E)
                        ? Finger.M_R
                        : Input.GetKey(KeyCode.Z)
                            ? Finger.An_R
                            : Input.GetKey(KeyCode.Q)
                                ? Finger.Au_R
                                : Finger.None
            : Input.GetKey(KeyCode.C)
                ? Finger.P_L
                : Input.GetKey(KeyCode.F)
                    ? Finger.I_L
                    : Input.GetKey(KeyCode.E)
                        ? Finger.M_L
                        : Input.GetKey(KeyCode.Z)
                            ? Finger.An_L
                            : Input.GetKey(KeyCode.Q)
                                ? Finger.Au_L
                                : Finger.None;

    public void OnButton(GridButton button) { MoveFingerTo(GetFinger(), button); }

    void UpdateCsv()
    {
        Debug.Log("Test");
        if (Frames.Exists(x => x.Frame == Player.frame)) Frames.Find(x => x.Frame == Player.frame).FromData(Current);
        else Frames.Add(new FrameAnalysis(Player.frame, Current));

        Frames.Sort();
        string[] lines = new string[Frames.Count + 1];
        lines[0] = HEADER;
        for (int i = 1; i < Frames.Count + 1; i++) lines[i] = Frames[i - 1].ToString();

        string path = Path.Combine(_rootPath, Datapath, LoadedVideo.Split('.')[0] + ".csv");
        File.WriteAllLines(path, lines);
    }

    const string HEADER =
        "Frame,Pouce_L.x ,Pouce_L.y ,Index_L.x ,Index_L.y ,Majeur_L.x ,Majeur_L.y,Annulaire_L.x ,Annulaire_L.y,Auriculaire_L.x,Auriculaire_L.y,Pouce_R.x,Pouce_R.y,Index_R.x,Index_R.y ,Majeur_R.x,Majeur_R.y,Annulaire_R.x,Annulaire_R.y,Auriculaire_R.x,Auriculaire_R.y";

    string myLog = "TAB ->| to mask";
    string filename => LoadedVideo;
    bool doShow = false;
    int kChars = 700;
    void OnEnable() => Application.logMessageReceived += Log;
    void OnDisable() => Application.logMessageReceived -= Log;

    public void Log(string logString, string stackTrace, LogType type)
    {
        // for onscreen...
        myLog = $"{type}|{stackTrace} {logString}\n{myLog}";
        if (myLog.Length > kChars)
        {
            myLog = myLog.Substring(myLog.Length - kChars);
        }

        // for the file ...
        var path = Path.Combine(Datapath, "log.txt");

        try
        {
            File.AppendAllText(path, $"{type}|{stackTrace} {logString}\n");
        }
        catch
        {
        }
    }

    void OnGUI()
    {
        if (!doShow) return;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
            new Vector3(Screen.width / 1200.0f, Screen.height / 800.0f, 1.0f));
        GUI.TextArea(new Rect(10, 10, 540, 370), myLog);
    }
}



[Serializable]
public class FrameAnalysis : IComparable<FrameAnalysis>
{
    public long Frame;

    public readonly Dictionary<Finger, Vector2Int> Fingers = new Dictionary<Finger, Vector2Int>
    {
        { Finger.P_L, Vector2Int.one * 16 }, { Finger.I_L, Vector2Int.one * 16 }, { Finger.M_L, Vector2Int.one * 16 },
        { Finger.An_L, Vector2Int.one * 16 }, { Finger.Au_L, Vector2Int.one * 16 }, { Finger.P_R, Vector2Int.one * 16 },
        { Finger.I_R, Vector2Int.one * 16 }, { Finger.M_R, Vector2Int.one * 16 }, { Finger.An_R, Vector2Int.one * 16 },
        { Finger.Au_R, Vector2Int.one * 16 }
    };

    int IComparable<FrameAnalysis>.CompareTo(FrameAnalysis other) =>
        other == null ? 1 : this.Frame.CompareTo(other.Frame);

    public FrameAnalysis(long frame) => Frame = frame;
    public FrameAnalysis(long frame, FrameAnalysisData data)
    {
        Frame = frame;
        foreach (var dataFinger in data.Fingers) Fingers[dataFinger.Key] = dataFinger.Value;
    }

    public void FromData(FrameAnalysisData data)
    {
        foreach (var dataFinger in data.Fingers) Fingers[dataFinger.Key] = dataFinger.Value;
    }

    public override string ToString()
    {
        var @return = $"{Frame},";
        foreach (Vector2Int finger in Fingers.Values) @return += $"{finger.x},{finger.y},";
        return @return.Remove(@return.Length - 1);
    }

    public FrameAnalysisData ToData()
    {
        var data = new FrameAnalysisData();
        foreach (var finger in Fingers) data.Fingers[finger.Key] = finger.Value;
        return data;
    }
}

[Serializable]
public class FrameAnalysisData
{
    public readonly Dictionary<Finger, Vector2Int> Fingers = new Dictionary<Finger, Vector2Int> { { Finger.P_L,-Vector2Int.one}, { Finger.I_L,-Vector2Int.one}, { Finger.M_L,-Vector2Int.one}, { Finger.An_L,-Vector2Int.one}, { Finger.Au_L,-Vector2Int.one}, { Finger.P_R,-Vector2Int.one}, { Finger.I_R,-Vector2Int.one}, { Finger.M_R,-Vector2Int.one}, { Finger.An_R,-Vector2Int.one}, { Finger.Au_R,-Vector2Int.one} };
    public override string ToString()
    {
        var @return = "";
        foreach (Vector2Int finger in Fingers.Values) @return += $"{finger.x},{finger.y},";
        return @return.Remove(@return.Length - 1);
    }
}
