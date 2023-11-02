using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using File = System.IO.File;

public enum Row { Un, Deux, Trois, Quatre, Cinq, Six, Sept, Huit, Neuf, Dix, Onze, Douze, Treize, Quatoze, Quinze, None = -1 }
public enum Column { A, B, C, D, E, F, G, H, I, J, K, L, M, None= -1 }
public struct Position : IEquatable<Position>
{
    public Row Row;
    public Column Column;

    public static Position None = new(Column.None, Row.None);
    public Position(Column column, Row row)
    {
        Column = column;
        Row = row;
    }

    public static bool operator ==(Position a, Position b) => a.Row == b.Row && a.Column == b.Column;
    public static bool operator !=(Position a, Position b) => !(a == b);
    public bool Equals(Position other) => Row == other.Row && Column == other.Column;
    public override bool Equals(object obj) => obj is Position other && Equals(other);
    public override int GetHashCode() => HashCode.Combine((int)Row, (int)Column);
    public override string ToString() => $"{Row}| {Column}";
}
public class App : MonoBehaviour
{ 
    public static App Instance;
    public event Action<Finger.ID, Position> PositionChanged;

    [SerializeField] Flag m_flag;
    public void Clear()
    {
        foreach (Finger.ID finger in Enum.GetValues(typeof(Finger.ID)))
        {
            if(finger == Finger.ID.None) continue;
            MoveFingerTo(finger,Position.None);
        }
    }

    public void ToggleFlag()
    {
        
    }

    [Header("--- Setting ---")] public long Step;
    
    const string NoVideo = "No video loaded";
    bool IsVideoLoaded => loadedVideo != string.Empty;
    
    #region Components

    [Header("--- Setup ---")] 
    public RawImage RawImage;
    public Slider Timeline;
    public VideoPlayer Player;
    public Text Console;
    public Transform VideoGrid;
    public GameObject VideoButton;
    public GameObject Videos;
    public List<GridButton> GridButtons = new();
    public List<Finger> Fingers = new();

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

                FrameAnalysis newFrame = new(values[0]) { Fingers = { [Finger.ID.P_L] = new Vector2Int(values[1], values[2]), [Finger.ID.I_L] = new Vector2Int(values[3], values[4]), [Finger.ID.M_L] = new Vector2Int(values[5], values[6]), [Finger.ID.An_L] = new Vector2Int(values[7], values[8]), [Finger.ID.Au_L] = new Vector2Int(values[9], values[10]), [Finger.ID.P_R] = new Vector2Int(values[11], values[12]), [Finger.ID.I_R] = new Vector2Int(values[13], values[14]), [Finger.ID.M_R] = new Vector2Int(values[15], values[16]), [Finger.ID.An_R] = new Vector2Int(values[17], values[18]), [Finger.ID.Au_R] = new Vector2Int(values[19], values[20]) } };
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
            Console.text = $"[  T  ] : {Player.time}/{Player.length:0.1}\n" +
                           $"[  % ] : {(float)Player.frame / Player.frameCount * 100.0f:F} %\n" +
                           $"[img] : {Player.frame}/{Player.frameCount}";
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

        if (Input.GetKeyDown(KeyCode.O))
        {
            Zoom = 1f;
            OffSet = Vector2.zero;
        }
    }

    void UpdateGridButton()
    {
        foreach (var fingerData in _currentFrame.FingerPositions)
            PositionChanged?.Invoke(fingerData.Key, new Position((Column)fingerData.Value.y, (Row)fingerData.Value.x));
    }

    public void MoveFingerTo(Finger.ID fingerID, Position position)
    {
        if (!IsVideoLoaded || fingerID == Finger.ID.None) return;

        _currentFrame.FingerPositions[fingerID] = new Vector2Int((int)position.Row, (int)position.Column);
        
        PositionChanged?.Invoke(fingerID,position);
        
        UpdateCsv();
    }
    
    void UpdateCsv()
    {
        Debug.Log("UpdateCSV");
        
        var frame = Player.frame;
        
        if(_frames.TryGetValue(frame,out var value)) value.FromData(_currentFrame);
        else _frames.Add(Player.frame,new FrameAnalysis(frame, _currentFrame));

        var lines = new List<string> { Header };
        foreach (var frameAnalysis in _frames.Values) lines.Add(frameAnalysis.ToString());

        string path = Path.Combine(_rootPath, Datapath, loadedVideo.Split('.')[0] + ".csv");
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
    public bool flagged = false;

    public readonly Dictionary<Finger.ID, Vector2Int> Fingers = new()
    {
        { Finger.ID.P_L, Vector2Int.one * 16 }, { Finger.ID.I_L, Vector2Int.one * 16 }, { Finger.ID.M_L, Vector2Int.one * 16 },
        { Finger.ID.An_L, Vector2Int.one * 16 }, { Finger.ID.Au_L, Vector2Int.one * 16 }, { Finger.ID.P_R, Vector2Int.one * 16 },
        { Finger.ID.I_R, Vector2Int.one * 16 }, { Finger.ID.M_R, Vector2Int.one * 16 }, { Finger.ID.An_R, Vector2Int.one * 16 },
        { Finger.ID.Au_R, Vector2Int.one * 16 }
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
        var line = $"{_frame},";
        foreach (Vector2Int finger in Fingers.Values) line += $"{finger.x},{finger.y},";
        return line.Remove(line.Length - 1);
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
    public readonly Dictionary<Finger.ID, Vector2Int> FingerPositions = new() { { Finger.ID.P_L,-Vector2Int.one}, { Finger.ID.I_L,-Vector2Int.one}, { Finger.ID.M_L,-Vector2Int.one}, { Finger.ID.An_L,-Vector2Int.one}, { Finger.ID.Au_L,-Vector2Int.one}, { Finger.ID.P_R,-Vector2Int.one}, { Finger.ID.I_R,-Vector2Int.one}, { Finger.ID.M_R,-Vector2Int.one}, { Finger.ID.An_R,-Vector2Int.one}, { Finger.ID.Au_R,-Vector2Int.one} };
    public override string ToString()
    {
        var @return = "";
        foreach (Vector2Int finger in FingerPositions.Values) @return += $"{finger.x},{finger.y},";
        return @return.Remove(@return.Length - 1);
    }
}
