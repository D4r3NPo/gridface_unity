using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using Debug = UnityEngine.Debug;
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

    [SerializeField] private Flag m_flag;
    [SerializeField] private InputField m_nameInputField;
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
        _currentFrame.IsFlag = !_currentFrame.IsFlag;
        Clear();
        UpdateFlag();
    }

    public void SetFrameName(string name)
    {
        if (_currentFrame.IsFlag)
        {
            _currentFrame.Name = 
        }
    }

    private void UpdateFlag() => m_flag.Enable = _currentFrame.IsFlag;

    [Header("--- Setting ---")] public long Step;

    private const string NoVideo = "No video loaded";
    private bool IsVideoLoaded => loadedVideo != string.Empty;
    
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

    private readonly string RootPath = Path.Combine(
        Application.platform == RuntimePlatform.IPhonePlayer ?
            Application.persistentDataPath
            : Environment.GetFolderPath(Environment.SpecialFolder.Desktop) ,
        "GridFace");

    private string VideoPath => Path.Combine(RootPath, "Video");
    private string DataPath => Path.Combine(RootPath, "Data");

    #endregion

    private const string Header = "Frame,Pouce_L.x ,Pouce_L.y ,Index_L.x ,Index_L.y ,Majeur_L.x ,Majeur_L.y,Annulaire_L.x ,Annulaire_L.y,Auriculaire_L.x,Auriculaire_L.y,Pouce_R.x,Pouce_R.y,Index_R.x,Index_R.y ,Majeur_R.x,Majeur_R.y,Annulaire_R.x,Annulaire_R.y,Auriculaire_R.x,Auriculaire_R.y";

    private string _myLog = "TAB ->| to mask";
    private bool _doShow;
    private const int KChars = 700;
    public string loadedVideo = string.Empty;
    private FrameAnalysisData _currentFrame = new();
    private readonly SortedDictionary<long, FrameAnalysis> _frames = new();

    private Coroutine m_operation;
    //Dictionary<long,FrameAnalysis> _frames;

    private void Awake() => Instance = this;

    private void Start()
    {
        if (!Directory.Exists(VideoPath)) Directory.CreateDirectory(VideoPath);
        if (!Directory.Exists(DataPath)) Directory.CreateDirectory(DataPath);
    }

    #region Controls

    private IEnumerator SimplePrevious()
    {
        if(m_operation != null) StopCoroutine(m_operation);
        long startFrame = Player.frame;
        Player.frame = Snap((long)Mathf.Clamp(startFrame - Step, 0, Player.frameCount));
        while (Player.frame == startFrame) 
            yield return null;
        _currentFrame = _frames.TryGetValue(Player.frame,out FrameAnalysis value)? value.ToData() : new FrameAnalysisData();
        UpdateGridButton();
    }

    private IEnumerator CopyPrevious()
    {
        if(m_operation != null) StopCoroutine(m_operation);
        FrameAnalysisData save = _currentFrame;
        long startFrame = Player.frame;
        Player.frame = Snap(startFrame - Step <= 0 ? 0 : Player.frame - Step);
        while (Player.frame == startFrame) yield return null;
        _currentFrame = save;
        UpdateCsv();
        UpdateGridButton();
    }

    private IEnumerator SimpleNext()
    {
        if(m_operation != null) StopCoroutine(m_operation);
        long startFrame = Player.frame;
        Player.frame = Snap(startFrame + Step >= (long)Player.frameCount ? (long)Player.frameCount : Player.frame + Step);
        while (Player.frame == startFrame) yield return null;
        _currentFrame = _frames.TryGetValue(Player.frame,out FrameAnalysis value)? value.ToData() : new FrameAnalysisData();
        UpdateGridButton();
    }

    private IEnumerator CopyNext()
    {
        if(m_operation != null) StopCoroutine(m_operation);
        FrameAnalysisData save = _currentFrame;
        long startFrame = Player.frame;
        Player.frame = Snap(startFrame + Step >= (long)Player.frameCount ? (long)Player.frameCount : (Player.frame + Step));
        while (Player.frame == startFrame) yield return null;
        _currentFrame = save;
        UpdateCsv();
        UpdateGridButton();
    }

    // TODO Report to Unity inconsistency in Types
    private long Snap(int value) => SnapToInterval(value, (int)Step);
    private long Snap(long value) => SnapToInterval((int)value, (int)Step);

    private static int SnapToInterval(int value, int interval)
    {
        // Calculate the remainder when dividing the value by the interval
        int remainder = value % interval;

        // If the remainder is less than half of the interval, snap down; otherwise, snap up
        return remainder < interval / 2 ? value - remainder : value + (interval - remainder);
    }

    private IEnumerator SeekingFor(float time)
    {
        if(m_operation != null) StopCoroutine(m_operation);
        long startFrame = Player.frame;
        Player.frame = Snap((long)time);
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
            foreach (FileInfo file in new DirectoryInfo(VideoPath).GetFiles("*.mp4"))
                if (file.Name[0] != '.')
                    Instantiate(VideoButton.GetComponentInChildren<VideoButton>(), VideoGrid).Name = file.Name;
        }
    }

    public void LoadVideo(string videoName) => StartCoroutine(LoadingVideo(videoName));

    private IEnumerator LoadingVideo(string videoName)
    {
        if (videoName != "CLOSE")
        {
            Player.url = "file://" + VideoPath + "/" + videoName;
            loadedVideo = videoName;
            Player.Prepare();
            while (Player.frameCount == 0 || !Player.isPrepared) yield return null;
            Timeline.maxValue = Player.frameCount;
            LoadCsv();
        }

        Videos.SetActive(false);
        VideoGrid.ClearChild();
    }

    private void LoadCsv()
    {
        Debug.Log("Load From CSV");
        
        //Find Curren Load Video Path
        string path = Path.Combine(RootPath, DataPath, loadedVideo.Split('.')[0] + ".csv");

        //Init Frames
        _frames.Clear();

        //Create File if it doesn't exist
        if (!File.Exists(path)) File.Create(path);
        else
        {
            //Load Frames from file
            var loadedFrames = File.ReadAllLines(path);
            for (int f = 1; f < loadedFrames.Length; f++)
            {
                string line = loadedFrames[f];
                
                string[] stringValues = line.Split(',');
                int[] values = new int[stringValues.Length];

                if (stringValues.Length < 1) throw new Exception("Parsing error : invalid csv file");

                bool isFLag = stringValues[1] == "=";
                if (isFLag)
                {
                    values[0] = stringValues[0] != string.Empty && int.TryParse(stringValues[0], out int value)
                        ? value
                        : -1;
                    for (int i = 1; i < values.Length; i++) values[i] = -1;
                }
                else
                {
                    for (int i = 0; i < values.Length; i++)
                        values[i] = stringValues[i] != string.Empty && int.TryParse(stringValues[i], out int value)
                            ? value
                            : -1;
                }

                FrameAnalysis newFrame = new(values[0])
                {
                    Fingers =
                    {
                        [Finger.ID.P_L] = new Vector2Int(values[1], values[2]),
                        [Finger.ID.I_L] = new Vector2Int(values[3], values[4]),
                        [Finger.ID.M_L] = new Vector2Int(values[5], values[6]),
                        [Finger.ID.An_L] = new Vector2Int(values[7], values[8]),
                        [Finger.ID.Au_L] = new Vector2Int(values[9], values[10]),
                        [Finger.ID.P_R] = new Vector2Int(values[11], values[12]),
                        [Finger.ID.I_R] = new Vector2Int(values[13], values[14]),
                        [Finger.ID.M_R] = new Vector2Int(values[15], values[16]),
                        [Finger.ID.An_R] = new Vector2Int(values[17], values[18]),
                        [Finger.ID.Au_R] = new Vector2Int(values[19], values[20])
                    },
                    isFlag = isFLag
                };
                _frames.Add(values[0], newFrame);
            }
        }
        _currentFrame = _frames.TryGetValue(0,out FrameAnalysis frame)? frame.ToData() : new FrameAnalysisData();
        UpdateGridButton();
    }

    private float Zoom
    {
        get => RawImage.uvRect.width;
        set => RawImage.uvRect = new Rect(OffSet.x, OffSet.y, value, value);
    }

    private Vector2 OffSet
    {
        get => new(RawImage.uvRect.x, RawImage.uvRect.y);
        set => RawImage.uvRect = new Rect(value, RawImage.uvRect.size);
    }

    private void Update() => Inputs();

    private void Inputs()
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

    private void UpdateGridButton()
    {
        UpdateFlag();
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

    private void UpdateCsv()
    {
        var frame = Player.frame;
        
        if(_frames.TryGetValue(frame,out var value)) value.FromData(_currentFrame);
        else _frames.Add(Player.frame,new FrameAnalysis(frame, _currentFrame));
        
        Stopwatch stopwatch = new();
        stopwatch.Start();
        
        var lines = new List<string> { Header };
        lines.AddRange(_frames.Values.Select(frameAnalysis => frameAnalysis.ToString()));
        string path = Path.Combine(RootPath, DataPath, loadedVideo.Split('.')[0] + ".csv");
        File.WriteAllLines(path, lines);
        
        stopwatch.Stop();
        Debug.Log($"Update CSV : {stopwatch.ElapsedMilliseconds} ms");
    }


    private void OnEnable() => Application.logMessageReceived += Log;
    private void OnDisable() => Application.logMessageReceived -= Log;

    private void Log(string logString, string stackTrace, LogType type)
    {
        _myLog = $"{type}|{stackTrace} {logString}\n{_myLog}";
        if (_myLog.Length > KChars) _myLog = _myLog.Substring(_myLog.Length - KChars);
        var path = Path.Combine(DataPath, "log.txt");
        try { File.AppendAllText(path, $"{type}|{stackTrace} {logString}\n"); }
        catch { /* ignored*/ }
    }

    private void OnGUI()
    {
        if (!_doShow) return;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1200.0f, Screen.height / 800.0f, 1.0f));
        GUI.TextArea(new Rect(10, 10, 540, 370), _myLog);
    }
}

public class FrameAnalysis : IComparable<FrameAnalysis>
{
    private readonly long _frame;
    public bool isFlag = false;

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
        isFlag = data.IsFlag;
        foreach (var dataFinger in data.FingerPositions) Fingers[dataFinger.Key] = dataFinger.Value;
    }

    public void FromData(FrameAnalysisData data)
    {
        isFlag = data.IsFlag;
        foreach (var dataFinger in data.FingerPositions) Fingers[dataFinger.Key] = dataFinger.Value;
    }

    public override string ToString()
    {
        var line = $"{_frame},";
        foreach (Vector2Int finger in Fingers.Values) line += isFlag ? "=,=," : $"{finger.x},{finger.y},";
        // Remove last coma
        return line.Remove(line.Length - 1);
    }

    public FrameAnalysisData ToData()
    {
        FrameAnalysisData data = new FrameAnalysisData { IsFlag = isFlag };
        foreach (var finger in Fingers) data.FingerPositions[finger.Key] = finger.Value;
        return data;
    }
}

public class FrameAnalysisData
{
    public bool IsFlag = false;
    public string Name = "";

    public readonly Dictionary<Finger.ID, Vector2Int> FingerPositions = new()
    {
        { Finger.ID.P_L, -Vector2Int.one }, { Finger.ID.I_L, -Vector2Int.one }, { Finger.ID.M_L, -Vector2Int.one },
        { Finger.ID.An_L, -Vector2Int.one }, { Finger.ID.Au_L, -Vector2Int.one }, { Finger.ID.P_R, -Vector2Int.one },
        { Finger.ID.I_R, -Vector2Int.one }, { Finger.ID.M_R, -Vector2Int.one }, { Finger.ID.An_R, -Vector2Int.one },
        { Finger.ID.Au_R, -Vector2Int.one }
    };

    public override string ToString()
    {
        string line = "";

        if (IsFlag)
            for (int i = -1; i < 19; i++)
                line += 
                    i == -1 || // Line Start Mark
                    i == 18 || // Line End Mark
                    Name == null || // No name
                    i >= Name.Length ? // Name Index Check
                        "=," 
                        : Name[i];
        else
            foreach (Vector2Int finger in FingerPositions.Values) 
                line += $"{finger.x},{finger.y},";
        
        // Remove last comma
        return line.Remove(line.Length - 1);
    }
}
