using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ayarlar paneli: fare hassasiyeti, ses, tam ekran. Ana menüde de duraklatma menüsünde de aynı panel kullanılır.
/// Her değişiklik anında uygulanır ve kayda yazılır.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;           // ayarlar paneli
    [SerializeField] private Slider sensitivitySlider;   // Min 0.02, Max 0.3
    [SerializeField] private Slider volumeSlider;        // Min 0, Max 1
    [SerializeField] private Toggle fullscreenToggle;

    private GameObject returnTo;  // "Geri" basınca açılacak panel (ana menü ya da duraklatma)

    private void Awake()
    {
        sensitivitySlider.onValueChanged.AddListener(v => { SaveSystem.Data.mouseSensitivity = v; Commit(); });
        volumeSlider.onValueChanged.AddListener(v => { SaveSystem.Data.masterVolume = v; Commit(); });
        fullscreenToggle.onValueChanged.AddListener(v => { SaveSystem.Data.fullscreen = v; Commit(); });
        panel.SetActive(false);
    }

    /// <summary>Paneli açar. from = geri dönülecek panel.</summary>
    public void Open(GameObject from)
    {
        returnTo = from;
        if (returnTo != null) returnTo.SetActive(false);

        // Kayıttaki değerleri göster (listener'ları tetiklemeden)
        SaveData d = SaveSystem.Data;
        sensitivitySlider.SetValueWithoutNotify(d.mouseSensitivity);
        volumeSlider.SetValueWithoutNotify(d.masterVolume);
        fullscreenToggle.SetIsOnWithoutNotify(d.fullscreen);

        panel.SetActive(true);
    }

    /// <summary>"Geri" butonu.</summary>
    public void Close()
    {
        panel.SetActive(false);
        if (returnTo != null) returnTo.SetActive(true);
    }

    public bool IsOpen => panel.activeSelf;

    private void Commit()
    {
        GameSettings.Apply();
        SaveSystem.Save();
    }
}
