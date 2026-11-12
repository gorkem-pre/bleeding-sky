using UnityEngine;

/// <summary>
/// Tur bitince sonucu kalıcı kayda yazar. v0.2:
///   F-08: Kayda CoinWallet.RunCoins DEĞİL, meşru Add() çağrılarından tutulan defter (ledger) yazılır.
///         Bellekte RunCoins'e doğrudan yazmak hiçbir kod çalıştırmadığı için defteri değiştirmez.
///         Cüzdan ≠ defter ise kurcalama tespit edilir. Tavan kontrolü ikinci katman olarak kalır.
///   F-10: Dev menü bu turda kullanıldıysa tur kaydedilmez; bayrak her yeni turda sıfırlanır.
///   F-11: Release build'de log mesajları ayrıntı (sınır değerleri) vermez.
/// </summary>
public class RunRecorder : MonoBehaviour
{
    [SerializeField] private CoinWallet wallet;
    [SerializeField] private Health playerHealth;
    [SerializeField] private WaveManager waves;

    [Header("Makullük (WaveManager ile aynı tut)")]
    [SerializeField] private int firstWaveCount = 3;
    [SerializeField] private int extraPerWave = 2;
    [SerializeField] private int maxCoinPerEnemy = 3;

    private bool recorded;
    private SecureInt ledger;   // bu turda meşru yoldan eklenen coin toplamı (bellekte gizli)

    public int RunCoins => wallet != null ? wallet.RunCoins : 0;
    public int WaveReached => waves != null ? waves.CurrentWave : 0;

    private void Start()
    {
        CheatState.ResetForNewRun();   // önceki turdaki hile, bu turu kirletmesin
    }

    private void OnEnable()
    {
        if (playerHealth != null) playerHealth.OnDied += HandlePlayerDied;
        if (wallet != null) wallet.OnCoinAdded += HandleCoinAdded;
    }

    private void OnDisable()
    {
        if (playerHealth != null) playerHealth.OnDied -= HandlePlayerDied;
        if (wallet != null) wallet.OnCoinAdded -= HandleCoinAdded;
    }

    private void HandleCoinAdded(int amount) => ledger.Value = ledger.Value + amount;
    private void OnDestroy() => ledger.Release();
    private void HandlePlayerDied(Health _) => Record();
    private void OnApplicationQuit() => Record();

    public void Record()
    {
        if (recorded) return;
        recorded = true;

        SaveData data = SaveSystem.Data;

        if (CheatState.Used)
        {
            data.cheatsUsed = true;
            SaveSystem.Save();
            Log("Tur kaydedilmedi (geliştirici menüsü ya da bellek kurcalaması).", "Tur kaydedilmedi.");
            return;
        }

        int gained = ledger.Value;

        // Kurcalama tespiti: cüzdandaki sayı, meşru eklemelerin toplamıyla aynı olmalı
        if (RunCoins != gained)
            Log($"Doğrulama başarısız: cüzdan {RunCoins}, defter {gained}.", "Coin doğrulaması başarısız.");

        // İkinci katman: defter de bir şekilde şişirildiyse fiziksel üst sınır
        int cap = MaxReasonableCoins(WaveReached);
        if (gained > cap)
        {
            Log($"Coin üst sınırı aşıldı: {gained} > {cap} (dalga {WaveReached}).", "Coin doğrulaması başarısız.");
            gained = cap;
        }

        data.totalCoins += gained;
        if (WaveReached > data.bestWave) data.bestWave = WaveReached;
        SaveSystem.Save();
    }

    private int MaxReasonableCoins(int wavesReached)
    {
        int enemies = 0;
        for (int n = 1; n <= wavesReached; n++)
            enemies += firstWaveCount + (n - 1) * extraPerWave;
        return enemies * maxCoinPerEnemy + maxCoinPerEnemy;
    }

    /// <summary>F-11: Editör/dev build'de ayrıntılı mesaj, release'de ayrıntısız.</summary>
    private static void Log(string detailed, string release = null)
    {
#if UNITY_EDITOR || DEBUG
        Debug.LogWarning(detailed);
#else
        Debug.LogWarning(release ?? detailed);
#endif
    }
}
