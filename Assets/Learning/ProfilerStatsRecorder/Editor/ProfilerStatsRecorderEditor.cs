using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProfilerStatsRecorder))]
public class ProfilerStatsRecorderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        ProfilerStatsRecorder recorder = (ProfilerStatsRecorder)target;

        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Load", GUILayout.Width(80), GUILayout.Height(28)))
        {
            string path = EditorUtility.OpenFilePanel("Select Profiler Stats File", Application.persistentDataPath, "json");
            if (!string.IsNullOrEmpty(path))
            {
                recorder.GetType().GetMethod("ClearPlayback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(recorder, null);
                recorder.GetType().GetField("playbackFilePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(recorder, path);
                recorder.GetType().GetMethod("LoadStatsFromFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(recorder, null);
            }
        }
        if (GUILayout.Button("Clear", GUILayout.Width(80), GUILayout.Height(28)))
        {
            recorder.GetType().GetMethod("ClearPlayback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(recorder, null);
        }
        GUILayout.EndHorizontal();

        string playbackFilePath = (string)recorder.GetType().GetField("playbackFilePath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(recorder);
        if (!string.IsNullOrEmpty(playbackFilePath))
        {
            GUILayout.Label($"Playback File: {playbackFilePath}");
        }

        bool isPlaybackLoaded = (bool)recorder.GetType().GetField("isPlaybackLoaded", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(recorder);
        var loadedStats = recorder.GetType().GetField("loadedStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(recorder) as FrameStatsCollection;
        int playbackFrame = (int)recorder.GetType().GetField("playbackFrame", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(recorder);
        int fontSize = (int)recorder.GetType().GetField("fontSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(recorder);
        float fpsWarningThreshold = (float)recorder.GetType().GetField("fpsWarningThreshold", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(recorder);

        if (isPlaybackLoaded && loadedStats != null && loadedStats.frames.Count > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label($"Playback Frame: {playbackFrame}/{loadedStats.frames.Count - 1}");
            int newPlaybackFrame = EditorGUILayout.IntSlider(playbackFrame, 0, loadedStats.frames.Count - 1);
            if (newPlaybackFrame != playbackFrame)
            {
                recorder.GetType().GetField("playbackFrame", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(recorder, newPlaybackFrame);
                playbackFrame = newPlaybackFrame;
            }
            var playback = loadedStats.frames[playbackFrame];
            if (Camera.main != null)
            {
                Camera.main.transform.position = playback.cameraPosition;
                Camera.main.transform.rotation = playback.cameraRotation;
            }
            var camPos = playback.cameraPosition;
            GUILayout.Label($"Camera Position: {camPos.x:F2}, {camPos.y:F2}, {camPos.z:F2}");
            // Build and show stats
            var stats = playback.stats;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("--- Profiler Stats ---");
            foreach (var entry in stats)
            {
                if (entry.key.Contains("Frame Time") || entry.key == "Main Thread" || entry.key == "VFX.Update")
                {
                    double avgMs = entry.value * 1e-6f;
                    double avgFps = avgMs > 0 ? 1000.0 / avgMs : 0.0;
                    string msText = avgFps < fpsWarningThreshold ? $"<color=red>{avgMs:F2}</color>" : $"{avgMs:F2}";
                    string fpsText = avgFps < fpsWarningThreshold ? $"<color=red>{(int)avgFps}</color>" : $"{(int)avgFps}";
                    sb.AppendLine($"{entry.key}: {avgMs:F2} ms {((int)avgFps)} fps");
                }
                else
                {
                    sb.AppendLine($"{entry.key}: {entry.value:F0}");
                }
            }
            EditorGUILayout.HelpBox(sb.ToString(), MessageType.None);
        }
    }
}
