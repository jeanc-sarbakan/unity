using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Profiling;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class StatEntry {
    public string key;
    public double value;
}

[System.Serializable]
public class FrameStats {
    public float time;
    public List<StatEntry> stats;
    public Vector3 cameraPosition;
    public Quaternion cameraRotation;
}

[System.Serializable]
public class FrameStatsCollection {
    public List<FrameStats> frames = new List<FrameStats>();
}

[ExecuteAlways]
public class ProfilerStatsRecorder : MonoBehaviour
{
    [Header("Recording Settings")]
    private int playbackFrame = 0;
    [Range(10f, 120f)]
    [SerializeField] private float fpsWarningThreshold = 30f;
    [Range(8, 128)]
    [SerializeField] private int fontSize = 0;

    // Profiler recorders
    ProfilerRecorder mainThreadTimeRecorder;
    ProfilerRecorder cpuTotalFrameTimeRecorder;
    ProfilerRecorder cpuMainThreadFrameTimeRecorder;
    ProfilerRecorder cpuRenderThreadFrameTimeRecorder;
    ProfilerRecorder gpuFrameTimeRecorder;
    ProfilerRecorder batchesCountRecorder;
    ProfilerRecorder drawCallsCountRecorder;
    ProfilerRecorder setPassCallsCountRecorder;
    ProfilerRecorder trianglesCountRecorder;
    ProfilerRecorder verticesCountRecorder;
    ProfilerRecorder shadowCastersCountRecorder;
    ProfilerRecorder vfxUpdateRecorder;

    private FrameStatsCollection recordedStats = new FrameStatsCollection();
    private bool hasSaved = false;
    private Vector3? originalCameraPosition;
    private Quaternion? originalCameraRotation;
    private FrameStatsCollection loadedStats = new FrameStatsCollection();
    private bool isPlaybackLoaded = false;
    private string statsText;
    private string playbackFilePath = "";

    void OnEnable()
    {
        if (Application.isPlaying)
        {
            mainThreadTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 60);
            cpuTotalFrameTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "CPU Total Frame Time", 60);
            cpuMainThreadFrameTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "CPU Main Thread Frame Time", 60);
            cpuRenderThreadFrameTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "CPU Render Thread Frame Time", 60);
            gpuFrameTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "GPU Frame Time", 60);
            batchesCountRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count", 60);
            drawCallsCountRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 60);
            setPassCallsCountRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count", 60);
            trianglesCountRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count", 60);
            verticesCountRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count", 60);
            shadowCastersCountRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Shadow Casters Count", 60);
            vfxUpdateRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "VFX.Update", 60);
        }
        hasSaved = false;
        recordedStats.frames.Clear();
        isPlaybackLoaded = false;
        if (Camera.main != null)
        {
            originalCameraPosition = Camera.main.transform.position;
            originalCameraRotation = Camera.main.transform.rotation;
        }
    }

    void OnDisable()
    {
        if (Application.isPlaying)
        {
            mainThreadTimeRecorder.Dispose();
            cpuTotalFrameTimeRecorder.Dispose();
            cpuMainThreadFrameTimeRecorder.Dispose();
            cpuRenderThreadFrameTimeRecorder.Dispose();
            gpuFrameTimeRecorder.Dispose();
            batchesCountRecorder.Dispose();
            drawCallsCountRecorder.Dispose();
            setPassCallsCountRecorder.Dispose();
            trianglesCountRecorder.Dispose();
            verticesCountRecorder.Dispose();
            shadowCastersCountRecorder.Dispose();
            vfxUpdateRecorder.Dispose();
        }
        if (Camera.main != null && originalCameraPosition.HasValue && originalCameraRotation.HasValue)
        {
            Camera.main.transform.position = originalCameraPosition.Value;
            Camera.main.transform.rotation = originalCameraRotation.Value;
        }
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            statsText = BuildStatsText(GetAllStatsList());
            // Record stats and camera position
            var frameStats = new FrameStats
            {
                time = Time.time,
                stats = GetAllStatsList(),
                cameraPosition = Camera.main != null ? Camera.main.transform.position : Vector3.zero,
                cameraRotation = Camera.main != null ? Camera.main.transform.rotation : Quaternion.identity
            };
            recordedStats.frames.Add(frameStats);
        }
    }

    void OnDestroy()
    {
        if (Application.isPlaying && recordedStats.frames.Count > 0)
        {
            SaveStatsToFile();
        }
    }

    void OnGUI()
    {
        // Determine which stats to show: live or playback
        string displayText = null;
        if (isPlaybackLoaded && loadedStats != null && loadedStats.frames.Count > 0)
        {
            int frame = Mathf.Clamp(playbackFrame, 0, loadedStats.frames.Count - 1);
            var playback = loadedStats.frames[frame];
            displayText = BuildStatsText(playback.stats);
        }
        else
        {
            displayText = statsText;
        }
        if (!string.IsNullOrEmpty(displayText))
        {
            var style = new GUIStyle(GUI.skin.box)
            {
                fontSize = fontSize > 0 ? fontSize : 14,
                alignment = TextAnchor.UpperLeft,
                richText = true
            };
            GUI.Box(new Rect(10, 10, 400, 400), displayText, style);
        }
    }

    private void ClearPlayback()
    {
        loadedStats = new FrameStatsCollection();
        isPlaybackLoaded = false;
        playbackFilePath = "";
        playbackFrame = 0;
        statsText = "";
    }

    private List<StatEntry> GetAllStatsList()
    {
        var list = new List<StatEntry>
        {
            new StatEntry { key = "Main Thread", value = GetRecorderFrameAverage(mainThreadTimeRecorder) },
            new StatEntry { key = "CPU Total Frame Time", value = GetRecorderFrameAverage(cpuTotalFrameTimeRecorder) },
            new StatEntry { key = "CPU Main Thread Frame Time", value = GetRecorderFrameAverage(cpuMainThreadFrameTimeRecorder) },
            new StatEntry { key = "CPU Render Thread Frame Time", value = GetRecorderFrameAverage(cpuRenderThreadFrameTimeRecorder) },
            new StatEntry { key = "GPU Frame Time", value = GetRecorderFrameAverage(gpuFrameTimeRecorder) },
            new StatEntry { key = "Batches Count", value = GetRecorderFrameAverage(batchesCountRecorder) },
            new StatEntry { key = "Draw Calls Count", value = GetRecorderFrameAverage(drawCallsCountRecorder) },
            new StatEntry { key = "SetPass Calls Count", value = GetRecorderFrameAverage(setPassCallsCountRecorder) },
            new StatEntry { key = "Triangles Count", value = GetRecorderFrameAverage(trianglesCountRecorder) },
            new StatEntry { key = "Vertices Count", value = GetRecorderFrameAverage(verticesCountRecorder) },
            new StatEntry { key = "Shadow Casters Count", value = GetRecorderFrameAverage(shadowCastersCountRecorder) },
            new StatEntry { key = "VFX.Update", value = GetRecorderFrameAverage(vfxUpdateRecorder) }
        };
        return list;
    }

    private string BuildStatsText(List<StatEntry> stats)
    {
        var sb = new StringBuilder();
        sb.AppendLine("--- Profiler Stats ---");
        foreach (var entry in stats)
        {
            if (entry.key.Contains("Frame Time") || entry.key == "Main Thread" || entry.key == "VFX.Update")
            {
                double avgMs = entry.value * 1e-6f;
                double avgFps = avgMs > 0 ? 1000.0 / avgMs : 0.0;
                string msText = avgFps < fpsWarningThreshold ? $"<color=red>{avgMs:F2}</color>" : $"{avgMs:F2}";
                string fpsText = avgFps < fpsWarningThreshold ? $"<color=red>{(int)avgFps}</color>" : $"{(int)avgFps}";
                sb.AppendLine($"{entry.key}: {msText} ms {fpsText} fps");
            }
            else
            {
                sb.AppendLine($"{entry.key}: {entry.value:F0}");
            }
        }
        return sb.ToString();
    }

    static double GetRecorderFrameAverage(ProfilerRecorder recorder)
    {
        var samples = recorder.ToArray();
        if (samples.Length == 0)
            return 0;
        return samples.Average(s => s.Value);
    }

    private void SaveStatsToFile()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string path = Path.Combine(Application.persistentDataPath, sceneName + "_ProfilerStats.json");
        string json = JsonUtility.ToJson(recordedStats);
        File.WriteAllText(path, json);
        Debug.Log($"Saved profiler stats to {path}");
    }

    private void LoadStatsFromFile()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string defaultPath = Path.Combine(Application.persistentDataPath, sceneName + "_ProfilerStats.json");
        string path = !string.IsNullOrEmpty(playbackFilePath) ? playbackFilePath : defaultPath;
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            loadedStats = JsonUtility.FromJson<FrameStatsCollection>(json);
            isPlaybackLoaded = true;
            Debug.Log($"Loaded profiler stats from {path}");
        }
        else
        {
            Debug.LogWarning($"Stats file not found: {path}");
        }
    }
}