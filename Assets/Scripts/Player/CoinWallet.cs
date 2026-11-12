using UnityEngine;

/// <summary>
/// Oyuncunun bu turda topladığı coin. Player objesine eklenir.
/// v0.2: RunCoins bellekte SecureInt ile gizli (F-05). Her meşru eklemede OnCoinAdded yayınlanır;
/// RunRecorder bu olaylardan kendi defterini tutar (F-08).
/// </summary>
public class CoinWallet : MonoBehaviour
{
    private SecureInt runCoins;

    public int RunCoins => runCoins.Value;

    public event System.Action<int> OnCoinsChanged; // yeni toplam (HUD için)
    public event System.Action<int> OnCoinAdded;    // eklenen MİKTAR (defter için)

    private void Awake() => runCoins.Value = 0;
    private void OnDestroy() => runCoins.Release();

    public void Add(int amount)
    {
        if (amount <= 0) return;
        runCoins.Value = runCoins.Value + amount;
        OnCoinAdded?.Invoke(amount);
        OnCoinsChanged?.Invoke(RunCoins);
    }
}
