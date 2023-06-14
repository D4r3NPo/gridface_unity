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
    public List<GridButton> GridButtons = new();

    #endregion

    #region Path

    readonly string _rootPath = Path.Combine(
        Application.platform == RuntimePlatform.IPhonePlayer ?
            Application.persistentDataPath
            : Environment.GetFolderPath(Environment.SpecialFolder.Desktop) ,
        "GridFace");

    string Videopath => Path.Combine(_rootPath, "Video");
    string Datapath => Path.Combine(_rootPath, "Data");

    #endregion
    
    const string Header = "Frame,Pouce_L.x ,Pouce_L.y ,Index_L.x ,Index_L.y ,Majeur_L.x ,Majeur_L.y,Annulaire_L.x ,Annulaire_L.y,Auriculaire_L.x,Auriculaire_L.y,Pouce_R.x,Pouce_R.y,Index_R.x,Index_R.y ,Majeur_R.x,Majeur_R.y,Annulaire_R.x,Annulaire_R.y,Auriculaire_R.x,Auriculaire_R.y";
    
    string _myLog = "TAB ->| to mask";
    bool _doShow;
    const int KChars = 700;
    public string loadedVideo = string.Empty;
    FrameAnalysisData _currentFrame = new();
    Dictionary<long,FrameAnalysis> _frames;

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
        Player.frame = startFrame - Step <= 0 ? 0 : Player.frame - Step;
        while (Player.frame == startFrame) yield return null;
        _currentFrame = _frames.TryGetValue(Player.frame,out FrameAnalysis value)? value.ToData() : new FrameAnalysisData();
        UpdateGridButton();
    }

    IEnumerator CopyPrevious()
    {
        FrameAnalysisData save = _currentFrame;
        long startFrame = Player.frame;
        Player.frame = startFrame - Step <= 0 ? 0 : Player.frame - Step;
        while (Player.frame == startFrame) yield return null;
        _currentFrame = save;
        UpdateCsv();
        UpdateGridButton();
    }

    IEnumerator SimpleNext()
    {
        long startFrame = Player.frame;
        Player.frame = startFrame + Step >= (long)Player.frameCount ? (long)Player.frameCount : Player.frame + Step;
        while (Player.frame == startFrame) yield return null;
        _currentFrame = _frames.TryGetValue(Player.frame,out FrameAnalysis value)? value.ToData() : new FrameAnalysisData();
        UpdateGridButton();
    }

    IEnumerator CopyNext()
    {
        FrameAnalysisData save = _currentFrame;
        long startFrame = Player.frame;
        Player.frame = startFrame + Step >= (long)Player.frameCount ? (long)Player.frameCount : (Player.frame + Step);
        while (Player.frame == startFrame) yield return null;
        _currentFrame = save;
        UpdateCsv();
        UpdateGridButton();
    }

    IEnumerator SeekingFor(float time)
    {
        long startFrame = Player.frame;
        Player.frame = (long)time;
        while (Player.frame == startFrame) yield return null;
        _currentFrame = _frames.TryGetValue(Player.frame,out FrameAnalysis value)? value.ToData() : new FrameAnalysisData();
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
            loadedVideo = videoName;
            while (Player.frameCount == 0) yield return null;
            Timeline.maxValue = Player.frameCount;
            LoadCsv();
        }

        Videos.SetActive(false);
        VideoGrid.ClearChild();
    }

    
    

    void LoadCsv()
    {
        Debug.Log("Load From CSV");
        
        //Find Curren Load Video Path
        string path = Path.Combine(_rootPath, Datapath, loadedVideo.Split('.')[0] + ".csv");

        //Init Frames
        _frames = new Dictionary<long, FrameAnalysis>();

        //Create File if it doesn'n exist
        if (!File.Exists(path)) File.Create(path);
        else
        {
            //Load Frames from file
            var loadedFrames = File.ReadAllLines(path);
            foreach (var line in loadedFrames)
            {
                //Skip Title
                if (line == loadedFrames[0]) continue;

                var stringValues = line.Split(',');
                var values = new int[stringValues.Length];
                for (var i = 0; i < values.Length; i++)
                    values[i] = stringValues[i] != string.Empty ? int.Parse(stringValues[i]) : -1;

                FrameAnalysis newFrame = new(values[0]) { Fingers = { [Finger.P_L] = new Vector2Int(values[1], values[2]), [Finger.I_L] = new Vector2Int(values[3], values[4]), [Finger.M_L] = new Vector2Int(values[5], values[6]), [Finger.An_L] = new Vector2Int(values[7], values[8]), [Finger.Au_L] = new Vector2Int(values[9], values[10]), [Finger.P_R] = new Vector2Int(values[11], values[12]), [Finger.I_R] = new Vector2Int(values[13], values[14]), [Finger.M_R] = new Vector2Int(values[15], values[16]), [Finger.An_R] = new Vector2Int(values[17], values[18]), [Finger.Au_R] = new Vector2Int(values[19], values[20]) } };
                _frames.Add(values[0],newFrame);
            }
        }
        _currentFrame = _frames.TryGetValue(0,out FrameAnalysis value)? value.ToData() : new FrameAnalysisData();
        UpdateGridButton();
    }

    float Zoom
    {
        get => RawImage.uvRect.width;
        set => RawImage.uvRect = new Rect(OffSet.x, OffSet.y, value, value);
    }

    Vector2 OffSet
    {
        get => new(RawImage.uvRect.x, RawImage.uvRect.y);
        set => RawImage.uvRect = new Rect(value, RawImage.uvRect.size);
    }

    void Update() => Inputs();
    void Inputs()
    {
        //Controls
        if (Player.url != string.Empty)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow)) Previous(Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift));
            if (Input.GetKeyDown(KeyCode.RightArrow)) Next(Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift));
            Console.text = $"t : {Player.time}/{Player.length:0.1}\nf : {Player.frame}/{Player.frameCount}";
        }
        else Console.text = NoVideo;

        //Toggle Logs
        if (Input.GetKeyDown(KeyCode.Tab)) _doShow = !_doShow;

        //Zoom
        if (Input.GetKeyDown(KeyCode.P)) Zoom -= 0.1f;
        if (Input.GetKeyDown(KeyCode.M)) Zoom += 0.1f;
        //Video View
        if (Input.GetKeyDown(KeyCode.L)) OffSet = new Vector2(OffSet.x + 0.1f, OffSet.y);
        if (Input.GetKeyDown(KeyCode.I)) OffSet = new Vector2(OffSet.x, OffSet.y + 0.1f);
        if (Input.GetKeyDown(KeyCode.J)) OffSet = new Vector2(OffSet.x - 0.1f, OffSet.y);
        if (Input.GetKeyDown(KeyCode.K)) OffSet = new Vector2(OffSet.x, OffSet.y - 0.1f);
    }

    const string NoVideo = "No video loaded";

    readonly Dictionary<Finger, GridButton> _fingerOnButtons = new()
    {
        { Finger.Au_L, null }, { Finger.An_L, null }, { Finger.M_L, null }, { Finger.I_L, null }, { Finger.P_L, null },
        { Finger.Au_R, null }, { Finger.An_R, null }, { Finger.M_R, null }, { Finger.I_R, null }, { Finger.P_R, null }
    };

    bool IsVideoLoaded => loadedVideo != string.Empty;

    void UpdateGridButton()
    {
        foreach (GridButton button in GridButtons)
        foreach (var finger in _currentFrame.FingerPositions)
        {
            if (new Vector2Int((int)button.Row, (int)button.Column) == finger.Value)
            {
                _fingerOnButtons[finger.Key]?.RemoveFinger(finger.Key);
                button.AddFinger(finger.Key);
                _fingerOnButtons[finger.Key] = button;
            }
            else button.RemoveFinger(finger.Key);
        }
    }

    public void MoveFingerTo(Finger finger, GridButton gridButton)
    {
        if (!IsVideoLoaded || finger == Finger.None) return;

        _fingerOnButtons[finger]?.RemoveFinger(finger);

        if (gridButton == _fingerOnButtons[finger])
        {
            _fingerOnButtons[finger] = null;
            _currentFrame.FingerPositions[finger] = new Vector2Int((int)Row.None, (int)Column.None);
        }
        else
        {
            _fingerOnButtons[finger] = gridButton;
            _fingerOnButtons[finger].AddFinger(finger);
            _currentFrame.FingerPositions[finger] = new Vector2Int((int)gridButton.Row, (int)gridButton.Column);
        }

        UpdateCsv();
    }

    static Finger GetFinger() => Input.GetKey(KeyCode.Space) ? Input.GetKey(KeyCode.C) ? Finger.P_R : Input.GetKey(KeyCode.F) ? Finger.I_R : Input.GetKey(KeyCode.E) ? Finger.M_R : Input.GetKey(KeyCode.Z) ? Finger.An_R : Input.GetKey(KeyCode.Q) ? Finger.Au_R : Finger.None : Input.GetKey(KeyCode.C) ? Finger.P_L : Input.GetKey(KeyCode.F) ? Finger.I_L : Input.GetKey(KeyCode.E) ? Finger.M_L : Input.GetKey(KeyCode.Z) ? Finger.An_L : Input.GetKey(KeyCode.Q) ? Finger.Au_L : Finger.None;

    public void OnButton(GridButton button) => MoveFingerTo(GetFinger(), button);

    void UpdateCsv()
    {
        Debug.Log("UpdateCSV");
        
        var frame = Player.frame;
        
        if(_frames.TryGetValue(frame,out FrameAnalysis value)) value.FromData(_currentFrame);
        else _frames.Add(Player.frame,new FrameAnalysis(frame, _currentFrame));

        var lines = new List<string> { Header };
        foreach (FrameAnalysis frameAnalysis in _frames.Values) lines.Add(frameAnalysis.ToString());

        var path = Path.Combine(_rootPath, Datapath, loadedVideo.Split('.')[0] + ".csv");
        File.WriteAllLines(path, lines);
    }

   
    void OnEnable() => Application.logMessageReceived += Log;
    void OnDisable() => Application.logMessageReceived -= Log;
    void Log(string logString, string stackTrace, LogType type)
    {
        _myLog = $"{type}|{stackTrace} {logString}\n{_myLog}";
        if (_myLog.Length > KChars) _myLog = _myLog.Substring(_myLog.Length - KChars);
        var path = Path.Combine(Datapath, "log.txt");
        try { File.AppendAllText(path, $"{type}|{stackTrace} {logString}\n"); }
        catch { /* ignored*/ }
    }

    void OnGUI()
    {
        if (!_doShow) return;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1200.0f, Screen.height / 800.0f, 1.0f));
        GUI.TextArea(new Rect(10, 10, 540, 370), _myLog);
    }
}

public class FrameAnalysis : IComparable<FrameAnalysis>
{
    readonly long _frame;

    public readonly Dictionary<Finger, Vector2Int> Fingers = new Dictionary<Finger, Vector2Int>
    {
        { Finger.P_L, Vector2Int.one * 16 }, { Finger.I_L, Vector2Int.one * 16 }, { Finger.M_L, Vector2Int.one * 16 },
        { Finger.An_L, Vector2Int.one * 16 }, { Finger.Au_L, Vector2Int.one * 16 }, { Finger.P_R, Vector2Int.one * 16 },
        { Finger.I_R, Vector2Int.one * 16 }, { Finger.M_R, Vector2Int.one * 16 }, { Finger.An_R, Vector2Int.one * 16 },
        { Finger.Au_R, Vector2Int.one * 16 }
    };

    int IComparable<FrameAnalysis>.CompareTo(FrameAnalysis other) =>
        other == null ? 1 : _frame.CompareTo(other._frame);

    public FrameAnalysis(long frame) => _frame = frame;
    public FrameAnalysis(long frame, FrameAnalysisData data)
    {
        _frame = frame;
        foreach (var dataFinger in data.FingerPositions) Fingers[dataFinger.Key] = dataFinger.Value;
    }

    public void FromData(FrameAnalysisData data)
    {
        foreach (var dataFinger in data.FingerPositions) Fingers[dataFinger.Key] = dataFinger.Value;
    }

    public override string ToString()
    {
        var @return = $"{_frame},";
        foreach (Vector2Int finger in Fingers.Values) @return += $"{finger.x},{finger.y},";
        return @return.Remove(@return.Length - 1);
    }

    public FrameAnalysisData ToData()
    {
        FrameAnalysisData data = new FrameAnalysisData();
        foreach (var finger in Fingers) data.FingerPositions[finger.Key] = finger.Value;
        return data;
    }
}
public class FrameAnalysisData
{
    public readonly Dictionary<Finger, Vector2Int> FingerPositions = new Dictionary<Finger, Vector2Int> { { Finger.P_L,-Vector2Int.one}, { Finger.I_L,-Vector2Int.one}, { Finger.M_L,-Vector2Int.one}, { Finger.An_L,-Vector2Int.one}, { Finger.Au_L,-Vector2Int.one}, { Finger.P_R,-Vector2Int.one}, { Finger.I_R,-Vector2Int.one}, { Finger.M_R,-Vector2Int.one}, { Finger.An_R,-Vector2Int.one}, { Finger.Au_R,-Vector2Int.one} };
    public override string ToString()
    {
        var @return = "";
        foreach (Vector2Int finger in FingerPositions.Values) @return += $"{finger.x},{finger.y},";
        return @return.Remove(@return.Length - 1);
    }
}
