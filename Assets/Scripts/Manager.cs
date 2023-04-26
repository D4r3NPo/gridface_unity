using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using File = System.IO.File;
using Random = UnityEngine.Random;

public class Manager : MonoBehaviour
{
    public static Manager Instance;
    void Awake() => Instance = this;

    [Header("--- Setting ---")]
    public long Step;

    [Header("--- Setup ---")]
    public RawImage RawImage;
    public Slider Timeline;
    public VideoPlayer Player;
    public Text Console;
    public Transform VideoGrid;
    public GameObject VideoButton;
    public GameObject Videos;
    public List<GridButton> GridButtons = new List<GridButton>();
    string RootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "GridFace");
    string videopath => Path.Combine(RootPath, "Video");
    string datapath => Path.Combine(RootPath, "Data");
    public string LoadedVideo = string.Empty;
    void Start()
    {
        if (!Directory.Exists(videopath)) Directory.CreateDirectory(videopath);
        if (!Directory.Exists(datapath)) Directory.CreateDirectory(datapath);
    }

    //:On right shift copy corruent no next frame data 
    IEnumerator Next()
    {
        if(Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift))
        {
            FrameAnalysisData save = Current;
            long startFrame = Player.frame;

            Player.frame = (Player.frame + Step) >= (long)Player.frameCount ? (long)Player.frameCount : (Player.frame + Step);

            while (Player.frame == startFrame) yield return null;

            Current = save;
            UpdateCSV();
            UpdateGridButton();
        }
        else
        {
            long startFrame = Player.frame;

            Player.frame = (Player.frame + Step) >= (long)Player.frameCount ? (long)Player.frameCount : (Player.frame + Step);

            while (Player.frame == startFrame) yield return null;


            Current = Frames.Exists(x => x.Frame == Player.frame) ? Frames.Find(x => x.Frame == Player.frame).ToData() : new FrameAnalysisData();
            UpdateGridButton();
        }
    }
    IEnumerator Previous()
    {
        if (Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift))
        {
            FrameAnalysisData save = Current;
            long startFrame = Player.frame;

            Player.frame = (Player.frame - Step) <= 0 ? 0 : (Player.frame - Step);

            while (Player.frame == startFrame) yield return null;

            Current = save;
            UpdateCSV();
            UpdateGridButton();

        }
        else
        {
            long startFrame = Player.frame;
            Player.frame = (Player.frame - Step) <= 0 ? 0 : (Player.frame - Step);

            while (Player.frame == startFrame) yield return null;

            Current = Frames.Exists(x => x.Frame == Player.frame) ? Frames.Find(x => x.Frame == Player.frame).ToData() : new FrameAnalysisData();
            UpdateGridButton();

        }

    }
    public void SeekFor(float time)
    {
        if(time%Step==0 && IsVideoLoaded)
        {
            StopAllCoroutines();
            StartCoroutine(SeekingFor(time));
        }
    }
    IEnumerator SeekingFor(float time)
    {
        long startFrame = Player.frame;

        Player.frame = (long)time;

        while (Player.frame == startFrame) yield return null;

        Current = Frames.Exists(x => x.Frame == Player.frame) ? Frames.Find(x => x.Frame == Player.frame).ToData() : new FrameAnalysisData();
        UpdateGridButton();
    }

    void UpdateGridButton()
    {
        foreach (var button in GridButtons)
        {
            if (Current.Pouce_L == new Vector2Int((int)button.Row, (int)button.Column)) button.AddFinger(Finger.P_L); else button.RemoveFinger(Finger.P_L);
            if (Current.Index_L == new Vector2Int((int)button.Row, (int)button.Column)) button.AddFinger(Finger.I_L); else button.RemoveFinger(Finger.I_L);
            if (Current.Majeur_L == new Vector2Int((int)button.Row, (int)button.Column)) button.AddFinger(Finger.M_L); else button.RemoveFinger(Finger.M_L);
            if (Current.Annulaire_L == new Vector2Int((int)button.Row, (int)button.Column)) button.AddFinger(Finger.An_L); else button.RemoveFinger(Finger.An_L);
            if (Current.Auriculaire_L == new Vector2Int((int)button.Row, (int)button.Column)) button.AddFinger(Finger.Au_L); else button.RemoveFinger(Finger.Au_L);

            if (Current.Pouce_R == new Vector2Int((int)button.Row, (int)button.Column)) button.AddFinger(Finger.P_R); else button.RemoveFinger(Finger.P_R);
            if (Current.Index_R == new Vector2Int((int)button.Row, (int)button.Column)) button.AddFinger(Finger.I_R); else button.RemoveFinger(Finger.I_R);
            if (Current.Majeur_R == new Vector2Int((int)button.Row, (int)button.Column)) button.AddFinger(Finger.M_R); else button.RemoveFinger(Finger.M_R);
            if (Current.Annulaire_R == new Vector2Int((int)button.Row, (int)button.Column)) button.AddFinger(Finger.An_R); else button.RemoveFinger(Finger.An_R);
            if (Current.Auriculaire_R == new Vector2Int((int)button.Row, (int)button.Column)) button.AddFinger(Finger.Au_R); else button.RemoveFinger(Finger.Au_R);
        }
    }
    public void DisplayVideos()
    {
        if (!Videos.activeSelf)
        {
            Videos.SetActive(true);
            Instantiate(VideoButton.GetComponentInChildren<VideoButton>(), VideoGrid).Name = "CLOSE";
            DirectoryInfo info = new DirectoryInfo(videopath);
            FileInfo[] fileInfo = info.GetFiles("*.mp4");
            foreach (FileInfo file in fileInfo)
            {
                if (file.Name[0] != '.')
                {
                    VideoButton button = Instantiate(VideoButton.GetComponentInChildren<VideoButton>(), VideoGrid);
                    button.Name = file.Name;
                }
            }
        }
    }
    public void LoadVideo(string videoName) => StartCoroutine(LoadingVideo(videoName));
     IEnumerator LoadingVideo(string videoName)
    {
        if (videoName != "CLOSE")
        {
            Player.url = "file://" + videopath + "/" + videoName;
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
        string path = Path.Combine(RootPath, datapath, LoadedVideo.Split('.')[0] + ".csv");

        //Init Frames
        Frames = new List<FrameAnalysis>();

        //Create File if it doesn'n exist
        if (!File.Exists(path)) File.Create(path);
        else
        {
            //Load Frames from file
            string[] frames = File.ReadAllLines(path);
            foreach (string line in frames)
            {
                //Skip Title
                if (line == frames[0]) continue;

                string[] stringValues = line.Split(',');
                int[] values = new int[stringValues.Length];
                for (int i = 0; i < values.Length; i++) values[i] = (stringValues[i] != string.Empty ? int.Parse(stringValues[i]) : -1);

                FrameAnalysis newFrame = new FrameAnalysis();
                newFrame.Frame = values[0];
                //Left
                newFrame.Pouce_L.x = values[1];
                newFrame.Pouce_L.y = values[2];
                newFrame.Index_L.x = values[3];
                newFrame.Index_L.y = values[4];
                newFrame.Majeur_L.x = values[5];
                newFrame.Majeur_L.y = values[6];
                newFrame.Annulaire_L.x = values[7];
                newFrame.Annulaire_L.y = values[8];
                newFrame.Auriculaire_L.x = values[9];
                newFrame.Auriculaire_L.y = values[10];

                //Right
                newFrame.Pouce_R.x = values[11];
                newFrame.Pouce_R.y = values[12];
                newFrame.Index_R.x = values[13];
                newFrame.Index_R.y = values[14];
                newFrame.Majeur_R.x = values[15];
                newFrame.Majeur_R.y = values[16];
                newFrame.Annulaire_R.x = values[17];
                newFrame.Annulaire_R.y = values[18];
                newFrame.Auriculaire_R.x = values[19];
                newFrame.Auriculaire_R.y = values[20];

                Frames.Add(newFrame);
            }
        }

        Current = Frames.Exists(x => x.Frame == 0) ? Frames.Find(x => x.Frame == 0).ToData() : new FrameAnalysisData();
        UpdateGridButton();

    }
    float Zoom { get => RawImage.uvRect.width; set => RawImage.uvRect = new Rect(OffSet.x,OffSet.y,value,value); }
    Vector2 OffSet { get => new Vector2(RawImage.uvRect.x, RawImage.uvRect.y); set => RawImage.uvRect = new Rect(value, RawImage.uvRect.size); }
    void Update()
    {
        if (Player.url != string.Empty)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow)) StartCoroutine(Previous());
            if (Input.GetKeyDown(KeyCode.RightArrow)) StartCoroutine(Next());
            Console.text = "t : " + Player.time + "/" + Player.length.ToString("0.1") + "\n" + "f : " + Player.frame + "/" + Player.frameCount;
        }
        else Console.text = NO_VIDEO;
        if (Input.GetKeyDown(KeyCode.Tab)) { doShow = !doShow; }

        if (Input.GetKeyDown(KeyCode.P)) Zoom -= 0.1f;
        if (Input.GetKeyDown(KeyCode.M)) Zoom += 0.1f;

        if (Input.GetKeyDown(KeyCode.L)) OffSet = new Vector2(OffSet.x + 0.1f, OffSet.y);
        if (Input.GetKeyDown(KeyCode.I)) OffSet = new Vector2(OffSet.x, OffSet.y + 0.1f);
        if (Input.GetKeyDown(KeyCode.J)) OffSet = new Vector2(OffSet.x - 0.1f , OffSet.y);
        if (Input.GetKeyDown(KeyCode.K)) OffSet = new Vector2(OffSet.x, OffSet.y - 0.1f);


    }
    const string NO_VIDEO = "No video loaded";
    GridButton Pouce_L, Index_L, Majeur_L, Annulaire_L, Auriculaire_L, Pouce_R, Index_R, Majeur_R, Annulaire_R, Auriculaire_R;
    bool IsVideoLoaded => LoadedVideo != string.Empty;
    public void OnButton(GridButton button)
    {
        if (!IsVideoLoaded) return;
        // if (Input.GetMouseButtonUp(0))
        // {
        if (Input.GetKey(KeyCode.Space))
        {
            if (Input.GetKey(KeyCode.C))
            {
                Pouce_R?.RemoveFinger(Finger.P_R);

                if (button == Pouce_R)
                {
                    Pouce_R = null;
                    Current.Pouce_R.x = (int)Row.None;
                    Current.Pouce_R.y = (int)Column.None;
                }
                else
                {
                    Pouce_R = button;
                    Pouce_R.AddFinger(Finger.P_R);
                    Current.Pouce_R.x = (int)button.Row;
                    Current.Pouce_R.y = (int)button.Column;
                }
            }
            else if (Input.GetKey(KeyCode.F))
            {
                Index_R?.RemoveFinger(Finger.I_R);

                if (button == Index_R)
                {
                    Index_R = null;
                    Current.Index_R.x = (int)Row.None;
                    Current.Index_R.y = (int)Column.None;
                }
                else
                {
                    Index_R = button;
                    Index_R.AddFinger(Finger.I_R);
                    Current.Index_R.x = (int)button.Row;
                    Current.Index_R.y = (int)button.Column;
                }
            }
            else if (Input.GetKey(KeyCode.E))
            {
                Majeur_R?.RemoveFinger(Finger.M_R);

                if (button == Majeur_R)
                {
                    Majeur_R = null;
                    Current.Majeur_R.x = (int)Row.None;
                    Current.Majeur_R.y = (int)Column.None;
                }
                else
                {
                    Majeur_R = button;
                    Majeur_R.AddFinger(Finger.M_R);
                    Current.Majeur_R.x = (int)button.Row;
                    Current.Majeur_R.y = (int)button.Column;
                }
            }
            else if (Input.GetKey(KeyCode.Z))
            {
                Annulaire_R?.RemoveFinger(Finger.An_R);

                if (button == Annulaire_R)
                {
                    Annulaire_R = null;
                    Current.Annulaire_R.x = (int)Row.None;
                    Current.Annulaire_R.y = (int)Column.None;
                }
                else
                {
                    Annulaire_R = button;
                    Annulaire_R.AddFinger(Finger.An_R);
                    Current.Annulaire_R.x = (int)button.Row;
                    Current.Annulaire_R.y = (int)button.Column;
                }
            }
            else if (Input.GetKey(KeyCode.Q))
            {
                Auriculaire_R?.RemoveFinger(Finger.Au_R);


                if (button == Auriculaire_R)
                {
                    Auriculaire_R = null;
                    Current.Auriculaire_R.x = (int)Row.None;
                    Current.Auriculaire_R.y = (int)Column.None;
                }
                else
                {
                    Auriculaire_R = button;
                    Auriculaire_R.AddFinger(Finger.Au_R);
                    Current.Auriculaire_R.x = (int)button.Row;
                    Current.Auriculaire_R.y = (int)button.Column;
                }

            }
        }
        else
        {

            if (Input.GetKey(KeyCode.C))
            {
                Pouce_L?.RemoveFinger(Finger.P_L);

                if (button == Pouce_L)
                {
                    Pouce_L = null;
                    Current.Pouce_L.x = (int)Row.None;
                    Current.Pouce_L.y = (int)Column.None;
                }
                else
                {
                    Pouce_L = button;
                    Pouce_L.AddFinger(Finger.P_L);
                    Current.Pouce_L.x = (int)button.Row;
                    Current.Pouce_L.y = (int)button.Column;
                }
            }
            else if (Input.GetKey(KeyCode.F))
            {
                Index_L?.RemoveFinger(Finger.I_L);

                if (button == Index_L)
                {
                    Index_L = null;
                    Current.Index_L.x = (int)Row.None;
                    Current.Index_L.y = (int)Column.None;
                }
                else
                {
                    Index_L = button;
                    Index_L.AddFinger(Finger.I_L);
                    Current.Index_L.x = (int)button.Row;
                    Current.Index_L.y = (int)button.Column;
                }
            }
            else if (Input.GetKey(KeyCode.E))
            {
                Majeur_L?.RemoveFinger(Finger.M_L);

                if (button == Majeur_L)
                {
                    Majeur_L = null;
                    Current.Majeur_L.x = (int)Row.None;
                    Current.Majeur_L.y = (int)Column.None;
                }
                else
                {
                    Majeur_L = button;
                    Majeur_L.AddFinger(Finger.M_L);
                    Current.Majeur_L.x = (int)button.Row;
                    Current.Majeur_L.y = (int)button.Column;
                }
            }
            else if (Input.GetKey(KeyCode.Z))
            {
                Annulaire_L?.RemoveFinger(Finger.An_L);
                if (button == Annulaire_L)
                {
                    Annulaire_L = null;
                    Current.Annulaire_L.x = (int)Row.None;
                    Current.Annulaire_L.y = (int)Column.None;
                }
                else
                {
                    Annulaire_L = button;
                    Annulaire_L.AddFinger(Finger.An_L);
                    Current.Annulaire_L.x = (int)button.Row;
                    Current.Annulaire_L.y = (int)button.Column;
                }
            }
            else if (Input.GetKey(KeyCode.Q))
            {
                Auriculaire_L?.RemoveFinger(Finger.Au_L);
                if (button == Auriculaire_L)
                {
                    Auriculaire_L = null;
                    Current.Auriculaire_L.x = (int)Row.None;
                    Current.Auriculaire_L.y = (int)Column.None;
                }
                else
                {
                    Auriculaire_L = button;
                    Auriculaire_L.AddFinger(Finger.Au_L);
                    Current.Auriculaire_L.x = (int)button.Row;
                    Current.Auriculaire_L.y = (int)button.Column;
                }
            }
        }

            UpdateCSV();
       // }
    }
    void UpdateCSV()
    {
        if (Frames.Exists(x => x.Frame == Player.frame)) Frames.Find(x => x.Frame == Player.frame).FromData(Current);
        else Frames.Add(new FrameAnalysis(Player.frame, Current));

        Frames.Sort();
        string[] lines = new string[Frames.Count+1];
        lines[0] = HEADER;
        for (int i = 1; i < Frames.Count+1; i++) lines[i] = Frames[i-1].ToString();
        
        string path = Path.Combine(RootPath, datapath, LoadedVideo.Split('.')[0] + ".csv");
        File.WriteAllLines(path,lines);
    }
    const string HEADER = "Frame,Pouce_L.x ,Pouce_L.y ,Index_L.x ,Index_L.y ,Majeur_L.x ,Majeur_L.y,Annulaire_L.x ,Annulaire_L.y,Auriculaire_L.x,Auriculaire_L.y,Pouce_R.x,Pouce_R.y,Index_R.x,Index_R.y ,Majeur_R.x,Majeur_R.y,Annulaire_R.x,Annulaire_R.y,Auriculaire_R.x,Auriculaire_R.y";



    string myLog = "TAB ->| to mask";
    string filename => LoadedVideo;
    bool doShow = false;
    int kChars = 700;
    void OnEnable() => Application.logMessageReceived += Log;
    void OnDisable() => Application.logMessageReceived -= Log;
    public void Log(string logString, string stackTrace, LogType type)
    {
        // for onscreen...
        myLog = type.ToString()+"|"+ stackTrace + " " + logString + "\n" + myLog;
        if (myLog.Length > kChars) { myLog = myLog.Substring(myLog.Length - kChars); }

        // for the file ...
        string path = Path.Combine(datapath,"log.txt");
        
        try { File.AppendAllText(path, type.ToString() + "|" + stackTrace + " " + logString + "\n"); }
        catch { }
    }

    void OnGUI()
    {
        if (!doShow) return;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
           new Vector3(Screen.width / 1200.0f, Screen.height / 800.0f, 1.0f));
        GUI.TextArea(new Rect(10, 10, 540, 370), myLog);
    }
}

public enum Row { Un, Deux, Trois, Quatre, Cinq, Six, Sept, Huit, Neuf, Dix, Onze, Douze, Treize, Quatoze, Quinze, None = -1 }
public enum Column { A, B, C, D, E, F, G, H, I, J, K, L, M, None= -1 }
public enum Finger { None, P_L, I_L, M_L, An_L, Au_L, P_R, I_R, M_R, An_R, Au_R }
[System.Serializable]
public class FrameAnalysis : IComparable<FrameAnalysis>
{
    public long Frame;
    public Vector2Int Pouce_L, Index_L, Majeur_L, Annulaire_L, Auriculaire_L, Pouce_R, Index_R, Majeur_R, Annulaire_R, Auriculaire_R;
    int IComparable<FrameAnalysis>.CompareTo(FrameAnalysis other) => other == null ? 1 : this.Frame.CompareTo(other.Frame);

    public FrameAnalysis()
    {
        Pouce_L = Index_L = Majeur_L = Annulaire_L = Auriculaire_L = Pouce_R = Index_R = Majeur_R = Annulaire_R = Auriculaire_R = Vector2Int.one * 16;
    }
    public FrameAnalysis(long frame, FrameAnalysisData data)
    {
        Frame = frame;
        Pouce_L = data.Pouce_L;
        Index_L = data.Index_L;
        Majeur_L = data.Majeur_L;
        Annulaire_L = data.Annulaire_L;
        Auriculaire_L = data.Auriculaire_L;
        Pouce_R = data.Pouce_R;
        Index_R = data.Index_R;
        Majeur_R = data.Majeur_R;
        Annulaire_R = data.Annulaire_R;
        Auriculaire_R = data.Auriculaire_R;
    }
    public void FromData(FrameAnalysisData data)
    {
        Pouce_L = data.Pouce_L;
        Index_L = data.Index_L;
        Majeur_L = data.Majeur_L;
        Annulaire_L = data.Annulaire_L;
        Auriculaire_L = data.Auriculaire_L;
        Pouce_R = data.Pouce_R;
        Index_R = data.Index_R;
        Majeur_R = data.Majeur_R;
        Annulaire_R = data.Annulaire_R;
        Auriculaire_R = data.Auriculaire_R;
    }

    public override string ToString() =>
    Frame + "," +
    Pouce_L.x + "," +
    Pouce_L.y + "," +
    Index_L.x + "," +
    Index_L.y + "," +
    Majeur_L.x + "," +
    Majeur_L.y + "," +
    Annulaire_L.x + "," +
    Annulaire_L.y + "," +
    Auriculaire_L.x + "," +
    Auriculaire_L.y + "," +
    Pouce_R.x + "," +
    Pouce_R.y + "," +
    Index_R.x + "," +
    Index_R.y + "," +
    Majeur_R.x + "," +
    Majeur_R.y + "," +
    Annulaire_R.x + "," +
    Annulaire_R.y + "," +
    Auriculaire_R.x + "," +
    Auriculaire_R.y;

    public FrameAnalysisData ToData() => new FrameAnalysisData() { Pouce_L = Pouce_L, Index_L = Index_L, Majeur_L = Majeur_L, Annulaire_L = Annulaire_L, Auriculaire_L = Auriculaire_L, Pouce_R = Pouce_R, Index_R = Index_R, Majeur_R = Majeur_R, Annulaire_R = Annulaire_R, Auriculaire_R = Auriculaire_R };
}
[System.Serializable]
public class FrameAnalysisData
{
    public FrameAnalysisData() => Pouce_L = Index_L = Majeur_L = Annulaire_L = Auriculaire_L = Pouce_R = Index_R = Majeur_R = Annulaire_R = Auriculaire_R = -Vector2Int.one;
    public Vector2Int Pouce_L, Index_L, Majeur_L, Annulaire_L, Auriculaire_L, Pouce_R, Index_R, Majeur_R, Annulaire_R, Auriculaire_R;
    public override string ToString() =>
   Pouce_L.x + "," +
   Pouce_L.y + "," +
   Index_L.x + "," +
   Index_L.y + "," +
   Majeur_L.x + "," +
   Majeur_L.y + "," +
   Annulaire_L.x + "," +
   Annulaire_L.y + "," +
   Auriculaire_L.x + "," +
   Auriculaire_L.y + "," +
   Pouce_R.x + "," +
   Pouce_R.y + "," +
   Index_R.x + "," +
   Index_R.y + "," +
   Majeur_R.x + "," +
   Majeur_R.y + "," +
   Annulaire_R.x + "," +
   Annulaire_R.y + "," +
   Auriculaire_R.x + "," +
   Auriculaire_R.y;
}

/*
 using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using File = System.IO.File;
using Random = UnityEngine.Random;

public class Manager : MonoBehaviour
{
    public static Manager Instance;
    void Awake() => Instance = this;

    [Header("--- Setting ---")]
    public long Step;

    [Header("--- Setup ---")]
    public Slider Timeline;
    public VideoPlayer Player;
    public Text Console;
    public Transform VideoGrid;
    public GameObject VideoButton;
    public GameObject Videos;
    public List<GridButton> GridButtons = new List<GridButton>();
    string RootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "GridFace");
    string videopath => Path.Combine(RootPath, "Video");
    string datapath => Path.Combine(RootPath, "Data");
    public string LoadedVideo = string.Empty;
    void Start()
    {
        if (!Directory.Exists(videopath)) Directory.CreateDirectory(videopath);
        if (!Directory.Exists(datapath)) Directory.CreateDirectory(datapath);
    }

    //:On right shift copy corruent no next frame data 
    IEnumerator Next()
    {
        if(Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift))
        {
            FrameAnalysisData save = Current;
            long startFrame = Player.frame;

            Player.frame = (Player.frame + Step) >= (long)Player.frameCount ? (long)Player.frameCount : (Player.frame + Step);

            while (Player.frame == startFrame) yield return null;

            Current = save;
            UpdateCSV();
            UpdateGridButton();
        }
        else
        {
            long startFrame = Player.frame;

            Player.frame = (Player.frame + Step) >= (long)Player.frameCount ? (long)Player.frameCount : (Player.frame + Step);

            while (Player.frame == startFrame) yield return null;


            Current = Frames.Exists(x => x.Frame == Player.frame) ? Frames.Find(x => x.Frame == Player.frame).ToData() : new FrameAnalysisData();
            UpdateGridButton();
        }
    }
    IEnumerator Previous()
    {
        if (Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift))
        {
            FrameAnalysisData save = Current;
            long startFrame = Player.frame;

            Player.frame = (Player.frame - Step) <= 0 ? 0 : (Player.frame - Step);

            while (Player.frame == startFrame) yield return null;

            Current = save;
            UpdateCSV();
            UpdateGridButton();

        }
        else
        {
            long startFrame = Player.frame;
            Player.frame = (Player.frame - Step) <= 0 ? 0 : (Player.frame - Step);

            while (Player.frame == startFrame) yield return null;

            Current = Frames.Exists(x => x.Frame == Player.frame) ? Frames.Find(x => x.Frame == Player.frame).ToData() : new FrameAnalysisData();
            UpdateGridButton();

        }

    }
    public void SeekFor(float time)
    {
        if(time%Step==0 && IsVideoLoaded)
        {
            StopAllCoroutines();
            StartCoroutine(SeekingFor(time));
        }
    }
    IEnumerator SeekingFor(float time)
    {
        long startFrame = Player.frame;

        Player.frame = (long)time;

        while (Player.frame == startFrame) yield return null;

        Current = Frames.Exists(x => x.Frame == Player.frame) ? Frames.Find(x => x.Frame == Player.frame).ToData() : new FrameAnalysisData();
        UpdateGridButton();
    }

    void UpdateGridButton()
    {
        foreach (var button in GridButtons)
        {
            if (Current.Pouce_L == new Vector2Int((int)button.Row, (int)button.Column)) button.AddFinger(Finger.P_L); else button.RemoveFinger(Finger.P_L);
            if (Current.Index_L == new Vector2Int((int)button.Row, (int)button.Column)) button.AddFinger(Finger.I_L); else button.RemoveFinger(Finger.I_L);
            if (Current.Majeur_L == new Vector2Int((int)button.Row, (int)button.Column)) button.AddFinger(Finger.M_L); else button.RemoveFinger(Finger.M_L);
            if (Current.Annulaire_L == new Vector2Int((int)button.Row, (int)button.Column)) button.AddFinger(Finger.An_L); else button.RemoveFinger(Finger.An_L);
            if (Current.Auriculaire_L == new Vector2Int((int)button.Row, (int)button.Column)) button.AddFinger(Finger.Au_L); else button.RemoveFinger(Finger.Au_L);

            if (Current.Pouce_R == new Vector2Int((int)button.Row, (int)button.Column)) button.AddFinger(Finger.P_R); else button.RemoveFinger(Finger.P_R);
            if (Current.Index_R == new Vector2Int((int)button.Row, (int)button.Column)) button.AddFinger(Finger.I_R); else button.RemoveFinger(Finger.I_R);
            if (Current.Majeur_R == new Vector2Int((int)button.Row, (int)button.Column)) button.AddFinger(Finger.M_R); else button.RemoveFinger(Finger.M_R);
            if (Current.Annulaire_R == new Vector2Int((int)button.Row, (int)button.Column)) button.AddFinger(Finger.An_R); else button.RemoveFinger(Finger.An_R);
            if (Current.Auriculaire_R == new Vector2Int((int)button.Row, (int)button.Column)) button.AddFinger(Finger.Au_R); else button.RemoveFinger(Finger.Au_R);
        }
    }
    public void DisplayVideos()
    {
        if (!Videos.activeSelf)
        {
            Videos.SetActive(true);
            Instantiate(VideoButton.GetComponentInChildren<VideoButton>(), VideoGrid).Name = "CLOSE";
            DirectoryInfo info = new DirectoryInfo(videopath);
            FileInfo[] fileInfo = info.GetFiles("*.mp4");
            foreach (FileInfo file in fileInfo)
            {
                if (file.Name[0] != '.')
                {
                    VideoButton button = Instantiate(VideoButton.GetComponentInChildren<VideoButton>(), VideoGrid);
                    button.Name = file.Name;
                }
            }
        }
    }
    public void LoadVideo(string videoName) => StartCoroutine(LoadingVideo(videoName));
     IEnumerator LoadingVideo(string videoName)
    {
        if (videoName != "CLOSE")
        {
            Player.url = "file://" + videopath + "/" + videoName;
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
        string path = Path.Combine(RootPath, datapath, LoadedVideo.Split('.')[0] + ".csv");

        //Init Frames
        Frames = new List<FrameAnalysis>();

        //Create File if it doesn'n exist
        if (!File.Exists(path)) File.Create(path);
        else
        {
            //Load Frames from file
            string[] frames = File.ReadAllLines(path);
            foreach (string line in frames)
            {
                //Skip Title
                if (line == frames[0]) continue;

                string[] stringValues = line.Split(',');
                int[] values = new int[stringValues.Length];
                for (int i = 0; i < values.Length; i++) values[i] = (stringValues[i] != string.Empty ? int.Parse(stringValues[i]) : -1);

                FrameAnalysis newFrame = new FrameAnalysis();
                newFrame.Frame = values[0];
                //Left
                newFrame.Pouce_L.x = values[1];
                newFrame.Pouce_L.y = values[2];
                newFrame.Index_L.x = values[3];
                newFrame.Index_L.y = values[4];
                newFrame.Majeur_L.x = values[5];
                newFrame.Majeur_L.y = values[6];
                newFrame.Annulaire_L.x = values[7];
                newFrame.Annulaire_L.y = values[8];
                newFrame.Auriculaire_L.x = values[9];
                newFrame.Auriculaire_L.y = values[10];

                //Right
                newFrame.Pouce_R.x = values[11];
                newFrame.Pouce_R.y = values[12];
                newFrame.Index_R.x = values[13];
                newFrame.Index_R.y = values[14];
                newFrame.Majeur_R.x = values[15];
                newFrame.Majeur_R.y = values[16];
                newFrame.Annulaire_R.x = values[17];
                newFrame.Annulaire_R.y = values[18];
                newFrame.Auriculaire_R.x = values[19];
                newFrame.Auriculaire_R.y = values[20];

                Frames.Add(newFrame);
            }
        }

        Current = Frames.Exists(x => x.Frame == 0) ? Frames.Find(x => x.Frame == 0).ToData() : new FrameAnalysisData();
        UpdateGridButton();

    }
    void Update()
    {
        if (Player.url != string.Empty)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow)) StartCoroutine(Previous());
            if (Input.GetKeyDown(KeyCode.RightArrow)) StartCoroutine(Next());
            Console.text = "t : " + Player.time + "/" + Player.length.ToString("0.1") + "\n" + "f : " + Player.frame + "/" + Player.frameCount;
        }
        else Console.text = NO_VIDEO;
        if (Input.GetKeyDown(KeyCode.Tab)) { doShow = !doShow; }
    }
    const string NO_VIDEO = "No video loaded";
    GridButton Pouce_L, Index_L, Majeur_L, Annulaire_L, Auriculaire_L, Pouce_R, Index_R, Majeur_R, Annulaire_R, Auriculaire_R;
    bool IsVideoLoaded => LoadedVideo != string.Empty;
    public void OnButton(GridButton button)
    {
        if (!IsVideoLoaded) return;
       // if (Input.GetMouseButtonUp(0)) {
            if (Input.GetKey(KeyCode.Space))
            {
            if (Input.GetKey(KeyCode.C))
            {
                Pouce_R?.RemoveFinger(Finger.P_R);

                if (button == Pouce_R)
                {
                    Pouce_R = null;
                    Current.Pouce_R.x = (int)Row.None;
                    Current.Pouce_R.y = (int)Column.None;
                }
                else
                {
                    Pouce_R = button;
                    Pouce_R.AddFinger(Finger.P_R);
                    Current.Pouce_R.x = (int)button.Row;
                    Current.Pouce_R.y = (int)button.Column;
                }
            }
            else if (Input.GetKey(KeyCode.F))
            {
                Index_R?.RemoveFinger(Finger.I_R);

                if (button == Index_R)
                {
                    Index_R = null;
                    Current.Index_R.x = (int)Row.None;
                    Current.Index_R.y = (int)Column.None;
                }
                else
                {
                    Index_R = button;
                    Index_R.AddFinger(Finger.I_R);
                    Current.Index_R.x = (int)button.Row;
                    Current.Index_R.y = (int)button.Column;
                }
            }
            else if (Input.GetKey(KeyCode.E))
            {
                Majeur_R?.RemoveFinger(Finger.M_R);
                Current.Majeur_R.x = (int)button.Row;
                Current.Majeur_R.y = (int)button.Column;
                Majeur_R = button;
                Majeur_R.AddFinger(Finger.M_R);

            }
            else if (Input.GetKey(KeyCode.Z))
            {
                Annulaire_R?.RemoveFinger(Finger.An_R);
                Current.Annulaire_R.x = (int)button.Row;
                Current.Annulaire_R.y = (int)button.Column;
                Annulaire_R = button;
                Annulaire_R.AddFinger(Finger.An_R);
            }
            else if (Input.GetKey(KeyCode.Q))
            {
                Auriculaire_R?.RemoveFinger(Finger.Au_R);
                Current.Auriculaire_R.x = (int)button.Row;
                Current.Auriculaire_R.y = (int)button.Column;
                Auriculaire_R = button;
                Auriculaire_R.AddFinger(Finger.Au_R);
            }
            }
            else
            {

                if (Input.GetKey(KeyCode.C))
                {
                    Pouce_L?.RemoveFinger(Finger.P_L);
                    Current.Pouce_L.x = (int)button.Row;
                    Current.Pouce_L.y = (int)button.Column;
                    Pouce_L = button;
                    Pouce_L.AddFinger(Finger.P_L);

                }
                else if (Input.GetKey(KeyCode.F))
                {
                    Index_L?.RemoveFinger(Finger.I_L);
                    Current.Index_L.x = (int)button.Row;
                    Current.Index_L.y = (int)button.Column;
                    Index_L = button;
                    Index_L.AddFinger(Finger.I_L);

                }
                else if (Input.GetKey(KeyCode.E))
                {
                    Majeur_L?.RemoveFinger(Finger.M_L);
                    Current.Majeur_L.x = (int)button.Row;
                    Current.Majeur_L.y = (int)button.Column;
                    Majeur_L = button;
                    Majeur_L.AddFinger(Finger.M_L);

                }
                else if (Input.GetKey(KeyCode.Z))
                {
                    Annulaire_L?.RemoveFinger(Finger.An_L);
                    Current.Annulaire_L.x = (int)button.Row;
                    Current.Annulaire_L.y = (int)button.Column;
                    Annulaire_L = button;
                    Annulaire_L.AddFinger(Finger.An_L);
                }
                else if (Input.GetKey(KeyCode.Q))
                {
                    Auriculaire_L?.RemoveFinger(Finger.Au_L);
                    Current.Auriculaire_L.x = (int)button.Row;
                    Current.Auriculaire_L.y = (int)button.Column;
                    Auriculaire_L = button;
                    Auriculaire_L.AddFinger(Finger.Au_L);
                }
            }

            UpdateCSV();
       // }
    }
    void UpdateCSV()
    {
        if (Frames.Exists(x => x.Frame == Player.frame)) Frames.Find(x => x.Frame == Player.frame).FromData(Current);
        else Frames.Add(new FrameAnalysis(Player.frame, Current));

        Frames.Sort();
        string[] lines = new string[Frames.Count+1];
        lines[0] = HEADER;
        for (int i = 1; i < Frames.Count+1; i++) lines[i] = Frames[i-1].ToString();
        
        string path = Path.Combine(RootPath, datapath, LoadedVideo.Split('.')[0] + ".csv");
        File.WriteAllLines(path,lines);
    }
    const string HEADER = "Frame,Pouce_L.x ,Pouce_L.y ,Index_L.x ,Index_L.y ,Majeur_L.x ,Majeur_L.y,Annulaire_L.x ,Annulaire_L.y,Auriculaire_L.x,Auriculaire_L.y,Pouce_R.x,Pouce_R.y,Index_R.x,Index_R.y ,Majeur_R.x,Majeur_R.y,Annulaire_R.x,Annulaire_R.y,Auriculaire_R.x,Auriculaire_R.y";



    string myLog = "TAB ->| to mask";
    string filename => LoadedVideo;
    bool doShow = false;
    int kChars = 700;
    void OnEnable() => Application.logMessageReceived += Log;
    void OnDisable() => Application.logMessageReceived -= Log;
    public void Log(string logString, string stackTrace, LogType type)
    {
        // for onscreen...
        myLog = type.ToString()+"|"+ stackTrace + " " + logString + "\n" + myLog;
        if (myLog.Length > kChars) { myLog = myLog.Substring(myLog.Length - kChars); }

        // for the file ...
        string path = Path.Combine(datapath,"log.txt");
        
        try { File.AppendAllText(path, type.ToString() + "|" + stackTrace + " " + logString + "\n"); }
        catch { }
    }

    void OnGUI()
    {
        if (!doShow) return;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
           new Vector3(Screen.width / 1200.0f, Screen.height / 800.0f, 1.0f));
        GUI.TextArea(new Rect(10, 10, 540, 370), myLog);
    }
}

public enum Row { Un, Deux, Trois, Quatre, Cinq, Six, Sept, Huit, Neuf, Dix, Onze, Douze, Treize, Quatoze, Quinze, None = -1 }
public enum Column { A, B, C, D, E, F, G, H, I, J, K, L, M, None= -1 }
public enum Finger { None, P_L, I_L, M_L, An_L, Au_L, P_R, I_R, M_R, An_R, Au_R }
[System.Serializable]
public class FrameAnalysis : IComparable<FrameAnalysis>
{
    public long Frame;
    public Vector2Int Pouce_L, Index_L, Majeur_L, Annulaire_L, Auriculaire_L, Pouce_R, Index_R, Majeur_R, Annulaire_R, Auriculaire_R;
    int IComparable<FrameAnalysis>.CompareTo(FrameAnalysis other) => other == null ? 1 : this.Frame.CompareTo(other.Frame);

    public FrameAnalysis()
    {
        Pouce_L = Index_L = Majeur_L = Annulaire_L = Auriculaire_L = Pouce_R = Index_R = Majeur_R = Annulaire_R = Auriculaire_R = Vector2Int.one * 16;
    }
    public FrameAnalysis(long frame, FrameAnalysisData data)
    {
        Frame = frame;
        Pouce_L = data.Pouce_L;
        Index_L = data.Index_L;
        Majeur_L = data.Majeur_L;
        Annulaire_L = data.Annulaire_L;
        Auriculaire_L = data.Auriculaire_L;
        Pouce_R = data.Pouce_R;
        Index_R = data.Index_R;
        Majeur_R = data.Majeur_R;
        Annulaire_R = data.Annulaire_R;
        Auriculaire_R = data.Auriculaire_R;
    }
    public void FromData(FrameAnalysisData data)
    {
        Pouce_L = data.Pouce_L;
        Index_L = data.Index_L;
        Majeur_L = data.Majeur_L;
        Annulaire_L = data.Annulaire_L;
        Auriculaire_L = data.Auriculaire_L;
        Pouce_R = data.Pouce_R;
        Index_R = data.Index_R;
        Majeur_R = data.Majeur_R;
        Annulaire_R = data.Annulaire_R;
        Auriculaire_R = data.Auriculaire_R;
    }

    public override string ToString() =>
    Frame + "," +
    Pouce_L.x + "," +
    Pouce_L.y + "," +
    Index_L.x + "," +
    Index_L.y + "," +
    Majeur_L.x + "," +
    Majeur_L.y + "," +
    Annulaire_L.x + "," +
    Annulaire_L.y + "," +
    Auriculaire_L.x + "," +
    Auriculaire_L.y + "," +
    Pouce_R.x + "," +
    Pouce_R.y + "," +
    Index_R.x + "," +
    Index_R.y + "," +
    Majeur_R.x + "," +
    Majeur_R.y + "," +
    Annulaire_R.x + "," +
    Annulaire_R.y + "," +
    Auriculaire_R.x + "," +
    Auriculaire_R.y;

    public FrameAnalysisData ToData() => new FrameAnalysisData() { Pouce_L = Pouce_L, Index_L = Index_L, Majeur_L = Majeur_L, Annulaire_L = Annulaire_L, Auriculaire_L = Auriculaire_L, Pouce_R = Pouce_R, Index_R = Index_R, Majeur_R = Majeur_R, Annulaire_R = Annulaire_R, Auriculaire_R = Auriculaire_R };
}
[System.Serializable]
public class FrameAnalysisData
{
    public FrameAnalysisData() => Pouce_L = Index_L = Majeur_L = Annulaire_L = Auriculaire_L = Pouce_R = Index_R = Majeur_R = Annulaire_R = Auriculaire_R = -Vector2Int.one;
    public Vector2Int Pouce_L, Index_L, Majeur_L, Annulaire_L, Auriculaire_L, Pouce_R, Index_R, Majeur_R, Annulaire_R, Auriculaire_R;
    public override string ToString() =>
   Pouce_L.x + "," +
   Pouce_L.y + "," +
   Index_L.x + "," +
   Index_L.y + "," +
   Majeur_L.x + "," +
   Majeur_L.y + "," +
   Annulaire_L.x + "," +
   Annulaire_L.y + "," +
   Auriculaire_L.x + "," +
   Auriculaire_L.y + "," +
   Pouce_R.x + "," +
   Pouce_R.y + "," +
   Index_R.x + "," +
   Index_R.y + "," +
   Majeur_R.x + "," +
   Majeur_R.y + "," +
   Annulaire_R.x + "," +
   Annulaire_R.y + "," +
   Auriculaire_R.x + "," +
   Auriculaire_R.y;
}

 */