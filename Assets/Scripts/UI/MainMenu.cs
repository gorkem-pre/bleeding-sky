using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ana menü: Oyna, Ayarlar, Çıkış + toplam coin ve en iyi dalga.
/// MainMenu sahnesindeki Canvas'a eklenir. Butonların OnClick'ine bu metodlar bağlanır.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameScene = "Arena";
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject levelPanel;   // bölüm seçme ekranı
    [SerializeField] private SettingsMenu settings;
    [SerializeField] private TextMeshProUGUI statsText;

    private void Start()
    {
        if (levelPanel != null) levelPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);

        // Oyundan buraya dönüldüyse her şeyi normale al
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        GameSettings.Apply();

        SaveData d = SaveSystem.Data;
        statsText.text = $"Toplam coin: {d.totalCoins}     En iyi dalga: {d.bestWave}";
    }

    /// <summary>Bölüm butonu: oyunu başlatır.</summary>
    public void Play() => SceneManager.LoadScene(gameScene);

    /// <summary>Ana menüdeki "Oyna": bölüm seçme ekranını açar.</summary>
    public void OpenLevelSelect()
    {
        mainPanel.SetActive(false);
        levelPanel.SetActive(true);
    }

    /// <summary>Bölüm seçme ekranındaki "Geri".</summary>
    public void CloseLevelSelect()
    {
        levelPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    public void OpenSettings() => settings.Open(mainPanel);

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // editörde Play'i durdur
#else
        Application.Quit();
#endif
    }
}
