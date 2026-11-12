using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// ESC ile duraklatma: zamanı durdurur (Time.timeScale = 0), menüyü açar.
/// Devam, Ayarlar, Ana menü. Arena sahnesindeki Canvas'a eklenir.
/// Not (cheat lab): Oyunun zamanı Time.timeScale ile yönetiliyor; speedhack bunu hedef alır.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    /// <summary>Diğer script'ler (bakış, büyü) duraklatılmışken çalışmasın diye kontrol eder.</summary>
    public static bool IsPaused { get; private set; }

    [SerializeField] private GameObject pausePanel;
    [SerializeField] private SettingsMenu settings;
    [SerializeField] private Health playerHealth;
    [SerializeField] private RunRecorder recorder;
    [SerializeField] private string menuScene = "MainMenu";

    private void Start()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        // Sahne değişirken statik değer bir sonraki sahneye "yapışık" kalmasın
        IsPaused = false;
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (playerHealth != null && playerHealth.IsDead) return; // ölüm ekranında ESC çalışmasın

        if (!IsPaused) Pause();
        else if (settings != null && settings.IsOpen) settings.Close(); // ayarlardaysa önce ayarları kapat
        else Resume();
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenSettings() => settings.Open(pausePanel);

    public void GoToMainMenu()
    {
        if (recorder != null) recorder.Record(); // yarıda bırakılsa da toplanan coin kaybolmasın
        IsPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuScene);
    }
}
