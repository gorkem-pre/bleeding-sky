using UnityEngine;

public enum Team { Player, Enemy }

/// <summary>
/// Ortak can bileşeni. Oyuncu ve düşmanlar aynı sınıfı kullanır.
/// v0.2: Can ve max can bellekte SecureFloat ile gizli tutulur (Rapor #02: F-05, F-07).
/// maxHealth alanı yalnızca Inspector ayarıdır; oyun sırasında gizli kopya (max) kullanılır.
/// </summary>
public class Health : MonoBehaviour
{
    [SerializeField] private Team team = Team.Enemy;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool destroyOnDeath = true;

    private SecureFloat current;
    private SecureFloat max;

    public float Current { get => current.Value; private set => current.Value = value; }
    public float Max => max.Value;
    public Team Team => team;
    public bool IsDead { get; private set; }

    /// <summary>Prefab/Inspector'daki ham ayar. WaveManager prefab kurcalamasını bununla kontrol eder.</summary>
    public float ConfiguredMax => maxHealth;

    /// <summary>Geliştirici menüsü için: açıkken hiç hasar almaz.</summary>
    public bool Invulnerable { get; set; }

    public event System.Action<float, float> OnHealthChanged; // current, max (UI için)
    public event System.Action<float> OnDamaged;              // öldürmeyen hasar
    public event System.Action<Health> OnDied;

    private void Awake()
    {
        max.Value = maxHealth;
        Current = maxHealth;
    }

    private void OnDestroy()
    {
        current.Release();
        max.Release();
    }

    /// <summary>Kurcalanmış bir değeri doğru değere geri çeker (WaveManager kullanır).</summary>
    public void ResetMax(float value)
    {
        max.Value = value;
        Current = value;
        OnHealthChanged?.Invoke(Current, Max);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;
        if (Invulnerable) return;

        Current = Mathf.Max(0f, Current - amount);
        OnHealthChanged?.Invoke(Current, Max);

        if (Current <= 0f) Die();
        else OnDamaged?.Invoke(amount);
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;

        Current = Mathf.Min(Max, Current + amount);
        OnHealthChanged?.Invoke(Current, Max);
    }

    private void Die()
    {
        IsDead = true;
        OnDied?.Invoke(this);

        if (destroyOnDeath) Destroy(gameObject);
    }
}
