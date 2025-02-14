using UnityEngine;

public class Config
{
    public float Sensitivity;

    public void Save()
    {
        PlayerPrefs.SetString("Config", JsonUtility.ToJson(this));

        PlayerPrefs.Save();
    }

    public static Config Load()
    {
        return JsonUtility.FromJson<Config>(PlayerPrefs.GetString("Config"));
    }
}
