using UnityEngine;

/// <summary>
/// Tek bir ses "olayı": birden fazla klip (her çalışta rastgele biri seçilir), ses seviyesi ve pitch aralığı.
/// Rastgele klip + hafif pitch farkı → aynı ses arka arkaya çalınca makineli tüfek gibi tekrar etmez.
/// </summary>
[System.Serializable]
public class SoundCue
{
    public AudioClip[] clips;
    [Range(0f, 1f)] public float volume = 1f;
    public Vector2 pitch = new Vector2(0.95f, 1.05f);   // min, max

    public bool IsEmpty => clips == null || clips.Length == 0;

    public AudioClip RandomClip() => clips[Random.Range(0, clips.Length)];
}

/// <summary>
/// Oyundaki bütün seslerin listesi. Assets/Resources/SoundLibrary.asset olarak durur,
/// Sfx sınıfı onu oradan kendisi yükler (sahnelere sürüklemek gerekmez).
/// "Tools → Arena → Ses Kütüphanesini Kur" aracı boş olanları Assets/Audio'dan otomatik doldurur.
/// </summary>
[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Arena/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [Header("Büyü")]
    public SoundCue spellCast = new SoundCue();       // ana ses (fırlatma "fşuu")
    public SoundCue spellCastLayer = new SoundCue();  // üstüne binen ikinci katman (buz şıngırtısı)
    public SoundCue spellImpact = new SoundCue();

    [Header("İskelet")]
    public SoundCue enemyRise = new SoundCue();
    public SoundCue enemySwing = new SoundCue();
    public SoundCue enemyHit = new SoundCue();
    public SoundCue enemyDeath = new SoundCue();

    [Header("Oyuncu")]
    public SoundCue playerHurt = new SoundCue();
    public SoundCue playerDeath = new SoundCue();
    public SoundCue footstep = new SoundCue();

    [Header("Oyun")]
    public SoundCue coin = new SoundCue();
    public SoundCue waveStart = new SoundCue();
    public SoundCue waveCleared = new SoundCue();

    [Header("Arayüz")]
    public SoundCue uiClick = new SoundCue();
    public SoundCue uiHover = new SoundCue();

    [Header("Müzik / ambiyans (sahneye göre otomatik çalar, döngüde)")]
    public AudioClip menuMusic;
    [Range(0f, 1f)] public float menuMusicVolume = 0.5f;
    public AudioClip arenaAmbience;
    [Range(0f, 1f)] public float arenaAmbienceVolume = 0.6f;
}
