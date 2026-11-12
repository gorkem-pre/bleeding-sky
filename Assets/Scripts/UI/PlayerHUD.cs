using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sol alttaki can/mana barları + hasar alınca kenarları kızaran vinyet.
/// Barlar genişlik (anchor) ile dolar → yuvarlak köşeler bozulmaz.
/// "Ghost" bar: hasar alınca beyaz iz kısa bir bekleme sonrası yavaşça kapanır (ne kadar can gittiğini gösterir).
/// HUDBuilder aracı bu bileşeni kurar ve alanlarını bağlar.
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [Header("Kaynaklar")]
    [SerializeField] private Health playerHealth;
    [SerializeField] private PlayerMana playerMana;

    [Header("Can barı")]
    [SerializeField] private RectTransform healthFill;
    [SerializeField] private RectTransform healthGhost;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Mana barı")]
    [SerializeField] private RectTransform manaFill;
    [SerializeField] private TextMeshProUGUI manaText;

    [Header("Ayarlar")]
    [SerializeField] private float barSmooth = 12f;     // barın yeni değere kayma hızı
    [SerializeField] private float ghostDelay = 0.4f;   // beyaz iz ne kadar bekleyip kapanmaya başlasın
    [SerializeField] private float ghostSpeed = 0.6f;   // beyaz iz kapanma hızı (bar/saniye)
    [SerializeField] private float lowHealth = 0.3f;    // bu oranın altında bar yanıp söner

    [Header("Hasar efekti")]
    [SerializeField] private Image damageVignette;
    [SerializeField] private float flashAlpha = 0.7f;
    [SerializeField] private float flashFadeSpeed = 1.5f;

    private float hpTarget = 1f, mpTarget = 1f;  // olması gereken doluluk (0..1)
    private float hpShown = 1f, mpShown = 1f;    // ekranda görünen doluluk
    private float ghost = 1f;
    private float ghostWaitUntil;
    private Color healthBaseColor;

    private void OnEnable()
    {
        playerHealth.OnHealthChanged += HandleHealthChanged;
        playerHealth.OnDamaged += HandleDamaged;
        playerMana.OnManaChanged += HandleManaChanged;
    }

    private void OnDisable()
    {
        playerHealth.OnHealthChanged -= HandleHealthChanged;
        playerHealth.OnDamaged -= HandleDamaged;
        playerMana.OnManaChanged -= HandleManaChanged;
    }

    private void Start()
    {
        healthBaseColor = healthFillImage.color;
        HandleHealthChanged(playerHealth.Current, playerHealth.Max);
        HandleManaChanged(playerMana.Current, playerMana.Max);
        hpShown = ghost = hpTarget;
        mpShown = mpTarget;
        SetVignette(0f);
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // Barlar yumuşakça kayar
        hpShown = Mathf.Lerp(hpShown, hpTarget, barSmooth * dt);
        mpShown = Mathf.Lerp(mpShown, mpTarget, barSmooth * dt);

        // Beyaz iz: beklemeden sonra yavaşça asıl bara yetişir; iyileşmede anında takip eder
        if (Time.time > ghostWaitUntil) ghost = Mathf.MoveTowards(ghost, hpShown, ghostSpeed * dt);
        if (ghost < hpShown) ghost = hpShown;

        SetWidth(healthFill, hpShown);
        SetWidth(healthGhost, ghost);
        SetWidth(manaFill, mpShown);

        // Düşük can: bar beyaza doğru nabız gibi atar
        if (hpTarget <= lowHealth && hpTarget > 0f)
        {
            float pulse = (Mathf.Sin(Time.time * 8f) + 1f) * 0.5f;
            healthFillImage.color = Color.Lerp(healthBaseColor, new Color(1f, 0.6f, 0.55f), pulse * 0.6f);
        }
        else healthFillImage.color = healthBaseColor;

        // Kızarık kenarlar yavaşça söner
        if (damageVignette.color.a > 0f)
            SetVignette(Mathf.MoveTowards(damageVignette.color.a, 0f, flashFadeSpeed * dt));
    }

    private void HandleHealthChanged(float current, float max)
    {
        float t = max > 0f ? current / max : 0f;
        if (t < hpTarget) ghostWaitUntil = Time.time + ghostDelay; // hasar → iz beklesin
        hpTarget = t;
        healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.RoundToInt(max)}";
    }

    private void HandleManaChanged(float current, float max)
    {
        mpTarget = max > 0f ? current / max : 0f;
        manaText.text = $"{Mathf.FloorToInt(current)} / {Mathf.RoundToInt(max)}";
    }

    private void HandleDamaged(float amount)
    {
        SetVignette(flashAlpha);
        Sfx.Play(Sfx.Lib.playerHurt);
    }

    /// <summary>Barı sağ kenarından kısaltır: anchorMax.x = doluluk.</summary>
    private static void SetWidth(RectTransform rt, float t)
    {
        Vector2 a = rt.anchorMax;
        a.x = Mathf.Clamp01(t);
        rt.anchorMax = a;
    }

    private void SetVignette(float alpha)
    {
        Color c = damageVignette.color;
        c.a = alpha;
        damageVignette.color = c;
    }
}
