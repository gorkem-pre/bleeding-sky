using UnityEngine;

/// <summary>
/// Oyuncunun mana havuzu. Büyüler mana harcar, mana zamanla dolar.
/// v0.2: Mana bellekte SecureFloat ile gizli (Rapor #02: F-05). Update her karede değeri okuduğu için
/// dondurma ya da doğrudan yazma bir kare içinde tespit edilir.
/// </summary>
public class PlayerMana : MonoBehaviour
{
    [SerializeField] private float maxMana = 100f;
    [SerializeField] private float regenPerSecond = 15f;
    [SerializeField] private float regenDelay = 1f;

    private SecureFloat current;
    private SecureFloat max;

    public float Current { get => current.Value; private set => current.Value = value; }
    public float Max => max.Value;

    /// <summary>Geliştirici menüsü için: açıkken mana hiç tükenmez.</summary>
    public bool Infinite { get; set; }

    public event System.Action<float, float> OnManaChanged; // current, max (UI için)

    private float lastSpendTime = -999f;

    private void Awake()
    {
        max.Value = maxMana;
        Current = maxMana;
    }

    private void OnDestroy()
    {
        current.Release();
        max.Release();
    }

    private void Update()
    {
        float cur = Current, mx = Max;   // her karede bir kez oku (aynı zamanda bütünlük kontrolü)
        if (cur < mx && Time.time >= lastSpendTime + regenDelay)
        {
            Current = Mathf.Min(mx, cur + regenPerSecond * Time.deltaTime);
            OnManaChanged?.Invoke(Current, mx);
        }
    }

    public bool TrySpend(float amount)
    {
        if (Infinite) return true;
        float cur = Current;
        if (cur < amount) return false;

        Current = cur - amount;
        lastSpendTime = Time.time;
        OnManaChanged?.Invoke(Current, Max);
        return true;
    }
}
