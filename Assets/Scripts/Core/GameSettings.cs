using UnityEngine;

/// <summary>
/// Kayıttaki ayarları oyuna uygular (ses, tam ekran).
/// Fare hassasiyeti PlayerLook tarafından doğrudan kayıttan okunur.
/// </summary>
public static class GameSettings
{
    public static void Apply()
    {
        SaveData d = SaveSystem.Data;
        AudioListener.volume = Mathf.Clamp01(d.masterVolume);
        Screen.fullScreen = d.fullscreen;
    }
}
