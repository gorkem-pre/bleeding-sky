using UnityEngine;

/// <summary>
/// Ses çalar. Kodun her yerinden tek satırla kullanılır:
///   Sfx.Play(Sfx.Lib.coin);                    → 2D (kulakta, mesafeden bağımsız: oyuncunun kendi sesleri, UI)
///   Sfx.PlayAt(Sfx.Lib.enemyHit, position);    → 3D (dünyada bir noktada: uzaklaştıkça kısılır, sağ/sol duyulur)
///
/// Havuz (pool): ilk kullanımda 24 AudioSource'lu gizli bir obje kurar ve sahne değişse de yaşatır.
/// Her ses için yeni obje yaratıp silmek yerine boştaki kaynaklardan birini kullanır.
/// </summary>
public static class Sfx
{
    private const int PoolSize = 24;

    private static SoundLibrary lib;
    private static AudioSource[] pool;
    private static int next;

    /// <summary>Assets/Resources/SoundLibrary.asset. Bulunamazsa boş bir kütüphane döner (oyun çökmez, ses çıkmaz).</summary>
    public static SoundLibrary Lib
    {
        get
        {
            if (lib == null)
            {
                lib = Resources.Load<SoundLibrary>("SoundLibrary");
                if (lib == null)
                {
                    Debug.LogWarning("SoundLibrary bulunamadı. Tools → Arena → Ses Kütüphanesini Kur.");
                    lib = ScriptableObject.CreateInstance<SoundLibrary>();
                }
            }
            return lib;
        }
    }

    /// <summary>2D ses: oyuncunun kendi sesleri ve arayüz.</summary>
    public static void Play(SoundCue cue, float volumeScale = 1f)
    {
        AudioSource src = Setup(cue, volumeScale);
        if (src == null) return;
        src.spatialBlend = 0f;
        src.Play();
    }

    /// <summary>3D ses: dünyada bir noktadan gelir.</summary>
    public static void PlayAt(SoundCue cue, Vector3 position, float volumeScale = 1f)
    {
        AudioSource src = Setup(cue, volumeScale);
        if (src == null) return;
        src.transform.position = position;
        src.spatialBlend = 1f;
        src.Play();
    }

    private static AudioSource Setup(SoundCue cue, float volumeScale)
    {
        if (cue == null || cue.IsEmpty) return null;
        EnsurePool();

        // Sıradaki boş kaynağı bul; hepsi doluysa en eskisini kes
        AudioSource src = pool[next];
        for (int i = 0; i < PoolSize; i++)
        {
            AudioSource candidate = pool[(next + i) % PoolSize];
            if (!candidate.isPlaying) { src = candidate; next = (next + i) % PoolSize; break; }
        }
        next = (next + 1) % PoolSize;

        src.clip = cue.RandomClip();
        src.volume = cue.volume * volumeScale;
        src.pitch = Random.Range(cue.pitch.x, cue.pitch.y);
        return src;
    }

    private static void EnsurePool()
    {
        if (pool != null && pool[0] != null) return;

        var root = new GameObject("[Sfx]");
        Object.DontDestroyOnLoad(root); // menüde basılan butonun sesi sahne değişince kesilmesin
        pool = new AudioSource[PoolSize];
        for (int i = 0; i < PoolSize; i++)
        {
            var child = new GameObject("Source" + i);
            child.transform.SetParent(root.transform);
            var src = child.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.rolloffMode = AudioRolloffMode.Linear; // maxDistance'ta tamamen susar
            src.minDistance = 3f;                      // 3 m'ye kadar tam ses
            src.maxDistance = 45f;                     // 45 m'den sonra duyulmaz
            src.dopplerLevel = 0f;                     // hızlı hareketlerde ses kaymasın
            pool[i] = src;
        }
    }
}
