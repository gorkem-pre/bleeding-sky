using UnityEngine;

/// <summary>
/// Bir büyünün tüm ayarları. Kod değil, "veri dosyası".
/// Project penceresinde: sağ tık → Create → Arena → Spell ile yeni büyü oluşturulur.
/// Ateş topu, buz, şimşek... hepsi aynı kodu kullanır, sadece bu dosyaları farklıdır.
/// </summary>
[CreateAssetMenu(fileName = "NewSpell", menuName = "Arena/Spell")]
public class SpellData : ScriptableObject
{
    public string spellName = "Fireball";
    public Projectile projectilePrefab;

    [Header("Maliyet")]
    public float manaCost = 10f;
    public float cooldown = 0.4f;   // iki atış arası saniye

    [Header("Mermi")]
    public float damage = 25f;
    public float speed = 25f;       // metre/saniye
    public float lifetime = 3f;     // hiçbir şeye çarpmazsa kaç saniye sonra yok olur
}
