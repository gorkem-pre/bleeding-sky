using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Oyuncu ölünce: kontrolleri kapat, ölüm ekranını göster (ulaşılan dalga + kazanılan coin).
/// R: tekrar dene. "Ana menü" butonu: GoToMainMenu.
/// Player objesine eklenir. Kapatılacak script'leri Player ve child'larında kendisi bulur.
/// </summary>
[RequireComponent(typeof(Health))]
public class PlayerDeath : MonoBehaviour
{
    [SerializeField] private GameObject deathPanel;        // "ÖLDÜN" ekranı
    [SerializeField] private TextMeshProUGUI statsText;    // "Dalga 5 · 42 coin"
    [SerializeField] private RunRecorder recorder;
    [SerializeField] private string menuScene = "MainMenu";

    private Health health;
    private bool isDead;

    private void Awake()
    {
        health = GetComponent<Health>();
        health.OnDied += HandleDied;

        if (deathPanel != null) deathPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        health.OnDied -= HandleDied;
    }

    private void HandleDied(Health _)
    {
        isDead = true;
        Sfx.Play(Sfx.Lib.playerDeath);

        // Hareket, bakış, büyü, asa ve kamera sallanması... hepsini durdur
        DisableAll<PlayerMovement>();
        DisableAll<PlayerLook>();
        DisableAll<SpellCaster>();
        DisableAll<HeadBob>();
        DisableAll<WeaponSway>();

        if (statsText != null && recorder != null)
            statsText.text = $"Dalga {recorder.WaveReached}  ·  {recorder.RunCoins} coin";

        if (deathPanel != null) deathPanel.SetActive(true);

        // Fare imlecini serbest bırak (butonlar tıklanabilsin)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>Bu objede ve tüm child'larında T tipindeki script'leri bulup kapatır.</summary>
    private void DisableAll<T>() where T : Behaviour
    {
        foreach (T b in GetComponentsInChildren<T>())
            b.enabled = false;
    }

    private void Update()
    {
        if (!isDead || Keyboard.current == null) return;

        if (Keyboard.current.rKey.wasPressedThisFrame) Retry();
    }

    /// <summary>Aynı sahneyi baştan yükle.</summary>
    public void Retry() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    /// <summary>Ölüm ekranındaki "Ana menü" butonu.</summary>
    public void GoToMainMenu() => SceneManager.LoadScene(menuScene);
}
