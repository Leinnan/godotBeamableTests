using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

namespace UnityEngine;

public class PlayerPrefs
{
    private Dictionary<string, string> Database { get; init; } = new();
    private static PlayerPrefs instance;
    const string prefsDataDat = "user://player_prefs_data.dat";

    private static PlayerPrefs Instance => instance ?? Init();

    private static PlayerPrefs Init()
    {
        if (!FileAccess.FileExists(prefsDataDat))
        {
            instance = new PlayerPrefs();
            return instance;
        }
        try
        {
            using var files = FileAccess.Open(prefsDataDat, FileAccess.ModeFlags.Read);
            string text = files.GetAsText();
            files.Close();
            Dictionary<string, string> parsedData = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);

            instance = new PlayerPrefs { Database = parsedData };
            return instance;
        }
        catch
        {
            Debug.LogError("Error during reading PlayerPrefs");
        }


        return instance;
    }

    public static string GetString(string key, string defaultValue = "") =>
        Instance.Database.TryGetValue(key, out var value) ? value : defaultValue;

    public static int GetInt(string key, int defaultValue = 0) =>
        Instance.Database.TryGetValue(key, out var value) && int.TryParse(value, out int result) ? result : defaultValue;

    public static double GetDouble(string key, double defaultValue = 0.0) =>
        Instance.Database.TryGetValue(key, out var value) && double.TryParse(value, out var result) ? result : defaultValue;

    public static void Save()
    {
        Debug.Log("PlayerPrefs.Save start");
        var content = JsonConvert.SerializeObject(Instance.Database);

        using var files = FileAccess.Open(prefsDataDat, FileAccess.ModeFlags.Write);
        files.StoreString(content);
        Debug.Log("PlayerPrefs.Save end");
    }

    public static void SetString(string key, string value) => Instance.Database[key] = value;

    public static bool HasKey(string name) => Instance.Database.ContainsKey(name);

    public static void DeleteKey(string key)
    {
        Instance.Database.Remove(key);
    }
}