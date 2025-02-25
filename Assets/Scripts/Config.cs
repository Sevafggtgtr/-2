using UnityEngine;

public class Config
{
    public float Sensitivity = 1,
                 Volume = 1;

    public void Save()
    {
        PlayerPrefs.SetString("Config", JsonUtility.ToJson(this));

        PlayerPrefs.Save();
    }

    public static Config Load()
    {
        var config = JsonUtility.FromJson<Config>(PlayerPrefs.GetString("Config"));
        if(config == null)
            config = new Config();
        AudioListener.volume = config.Volume;

        return config;
    }
}
