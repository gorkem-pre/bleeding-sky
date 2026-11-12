using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sahneye göre arka plan müziği / ambiyans çalar. Hiçbir sahneye eklenmez, kendini kurar:
///   [RuntimeInitializeOnLoadMethod] → oyun başlarken Unity bu metodu kendisi çağırır.
/// MainMenu → SoundLibrary.menuMusic,  Arena → SoundLibrary.arenaAmbience.
/// Sahne değişince eski parça söner, yenisi yükselir (crossfade). Pause'da (timeScale = 0) da çalışır.
/// </summary>
public class MusicPlayer : MonoBehaviour
{
    private const float FadeTime = 1.5f;

    private AudioSource a, b;    // iki kaynak: biri sönerken diğeri yükselir
    private AudioSource current;
    private float targetVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        var go = new GameObject("[Music]");
        DontDestroyOnLoad(go);
        go.AddComponent<MusicPlayer>();
    }

    private void Awake()
    {
        a = CreateSource();
        b = CreateSource();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // İlk sahne için sceneLoaded her zaman gelmiyor (özellikle Editor'de Play'e basınca sahne zaten yüklü).
    // O yüzden açılışta aktif sahneye kendimiz bakıyoruz. Olay ayrıca gelirse PlayTrack aynı parçayı tekrar başlatmaz.
    private void Start() => OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

    private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private AudioSource CreateSource()
    {
        var src = gameObject.AddComponent<AudioSource>();
        src.loop = true;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.volume = 0f;
        return src;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SoundLibrary lib = Sfx.Lib;
        switch (scene.name)
        {
            case "MainMenu": PlayTrack(lib.menuMusic, lib.menuMusicVolume); break;
            case "Arena": PlayTrack(lib.arenaAmbience, lib.arenaAmbienceVolume); break;
            default: PlayTrack(null, 0f); break;
        }
    }

    private void PlayTrack(AudioClip clip, float volume)
    {
        // Aynı parça zaten çalıyorsa baştan başlatma (ör. Arena'da "Tekrar Dene")
        if (current != null && current.clip == clip && clip != null) { targetVolume = volume; return; }

        AudioSource next = current == a ? b : a;
        current = next;
        targetVolume = volume;

        if (clip == null) return;
        next.clip = clip;
        next.volume = 0f;
        next.Play();
    }

    private void Update()
    {
        // unscaledDeltaTime: oyun duraklatılınca (timeScale = 0) da sönme/yükselme devam etsin
        float step = Time.unscaledDeltaTime / FadeTime;

        foreach (AudioSource src in new[] { a, b })
        {
            float goal = src == current ? targetVolume : 0f;
            src.volume = Mathf.MoveTowards(src.volume, goal, step);
            if (src != current && src.volume <= 0f && src.isPlaying) src.Stop();
        }
    }
}
