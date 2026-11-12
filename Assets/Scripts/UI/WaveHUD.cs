using TMPro;
using UnityEngine;

/// <summary>
/// Üst ortada "DALGA X" + "Y iskelet kaldı", dalga başlarken ekranın ortasında büyük duyuru.
/// Duyuru büyük başlayıp yerine oturur ("punch"), sonra söner.
/// </summary>
public class WaveHUD : MonoBehaviour
{
    [SerializeField] private WaveManager waves;
    [SerializeField] private TextMeshProUGUI waveText;       // üst orta: DALGA 3
    [SerializeField] private TextMeshProUGUI remainingText;  // altında: 5 iskelet kaldı
    [SerializeField] private TextMeshProUGUI announceText;   // ekranın ortası
    [SerializeField] private float announceDuration = 2.2f;
    [SerializeField] private float fadeTime = 0.6f;
    [SerializeField] private float punchScale = 1.4f;        // duyuru bu boyuttan 1'e küçülür

    private float announceEndTime;
    private bool waveActive;

    private void OnEnable()
    {
        waves.OnWaveStarted += HandleWaveStarted;
        waves.OnWaveCleared += HandleWaveCleared;
        waves.OnAliveCountChanged += HandleAliveChanged;
    }

    private void OnDisable()
    {
        waves.OnWaveStarted -= HandleWaveStarted;
        waves.OnWaveCleared -= HandleWaveCleared;
        waves.OnAliveCountChanged -= HandleAliveChanged;
    }

    private void Start()
    {
        waveText.text = "HAZIRLAN";
        remainingText.text = "İlk dalga geliyor...";
        SetAlpha(announceText, 0f);
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // Duyuru: son fadeTime saniyede söner, ölçeği 1'e oturur
        SetAlpha(announceText, Mathf.Clamp01((announceEndTime - Time.time) / fadeTime));
        announceText.transform.localScale = Vector3.Lerp(announceText.transform.localScale, Vector3.one, 8f * dt);
        remainingText.transform.localScale = Vector3.Lerp(remainingText.transform.localScale, Vector3.one, 10f * dt);
    }

    private void HandleWaveStarted(int wave)
    {
        waveActive = true;
        waveText.text = $"DALGA {wave}";
        Announce($"DALGA {wave}");
        Sfx.Play(Sfx.Lib.waveStart);
        Refresh();
    }

    private void HandleWaveCleared(int wave)
    {
        waveActive = false;
        Announce("DALGA TEMİZLENDİ");
        Sfx.Play(Sfx.Lib.waveCleared);
        remainingText.text = "Sıradaki dalga geliyor...";
    }

    private void HandleAliveChanged(int alive)
    {
        if (!waveActive) return;
        Refresh();
        remainingText.transform.localScale = Vector3.one * 1.25f; // sayı değişince küçük zıplama
    }

    private void Refresh()
    {
        int n = waves.AliveCount;
        remainingText.text = n > 0 ? $"{n} iskelet kaldı" : "Mezarlar uyanıyor...";
    }

    private void Announce(string message)
    {
        announceText.text = message;
        announceEndTime = Time.time + announceDuration;
        announceText.transform.localScale = Vector3.one * punchScale;
    }

    private static void SetAlpha(TextMeshProUGUI t, float a)
    {
        Color c = t.color;
        c.a = a;
        t.color = c;
    }
}
