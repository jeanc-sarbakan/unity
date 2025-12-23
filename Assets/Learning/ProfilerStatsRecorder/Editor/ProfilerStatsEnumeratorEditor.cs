#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Unity.Profiling.LowLevel.Unsafe;

// Editor utility to run profiler-stat-enumeration logic for each category from the Unity menu
public static class ProfilerStatsEnumeratorEditor
{
    [MenuItem("Tools/Profiler Stats/List All Categories and Stats")]
    public static void ListAllCategoriesMenu()
    {
        var availableStatHandles = new List<ProfilerRecorderHandle>();
        ProfilerRecorderHandle.GetAvailable(availableStatHandles);
        var categoryStats = new Dictionary<string, List<ProfilerRecorderHandle>>();
        foreach (var h in availableStatHandles)
        {
            var statDesc = ProfilerRecorderHandle.GetDescription(h);
            string cat = statDesc.Category.Name;
            if (!categoryStats.ContainsKey(cat))
                categoryStats[cat] = new List<ProfilerRecorderHandle>();
            categoryStats[cat].Add(h);
        }
        if (categoryStats.Count > 0)
        {
            foreach (var kvp in categoryStats)
            {
                string categoryName = kvp.Key;
                Debug.Log($"<b><color=#FFA500>Category: {categoryName}</color></b>"); // Orange bold
                LogCategoryStats(categoryName);
            }
        }
        else
        {
            Debug.Log("No profiler categories available.");
        }
    }

    private static void LogCategoryStats(string categoryName)
    {
        var availableStatHandles = new List<ProfilerRecorderHandle>();
        ProfilerRecorderHandle.GetAvailable(availableStatHandles);

        var groupedStats = new Dictionary<string, List<string>>();
        var ungroupedStats = new List<string>();
        foreach (var h in availableStatHandles)
        {
            var statDesc = ProfilerRecorderHandle.GetDescription(h);
            if (statDesc.Category.Name == categoryName)
            {
                string statName = statDesc.Name;
                if (statName.Contains("."))
                {
                    string prefix = statName.Substring(0, statName.IndexOf('.'));
                    if (!groupedStats.ContainsKey(prefix))
                        groupedStats[prefix] = new List<string>();
                    groupedStats[prefix].Add($"'{statDesc.Name}' [{statDesc.UnitType}]");
                }
                else
                {
                    ungroupedStats.Add($"'{statDesc.Name}' [{statDesc.UnitType}]");
                }
            }
        }

        bool hasStats = groupedStats.Count > 0 || ungroupedStats.Count > 0;
        if (hasStats)
        {
            foreach (var kvp in groupedStats)
            {
                string statsJoined = string.Join("\n", kvp.Value);
                Debug.Log($"<color=#00FFFF>Group: {kvp.Key}</color>\n{statsJoined}"); // Cyan
            }
            if (ungroupedStats.Count > 0)
            {
                string statsJoined = string.Join("\n", ungroupedStats);
                Debug.Log($"<color=#00FFFF>Group: Ungrouped</color>\n{statsJoined}"); // Cyan
            }
        }
        else
        {
            Debug.Log($"Category: {categoryName} has no available stats.");
        }
    }
}
#endif
