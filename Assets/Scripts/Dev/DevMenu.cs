#if UNITY_EDITOR || DEBUG
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Geliştirici menüsü. YALNIZCA editörde ve Development Build'de derlenir.
/// Release build'de bu dosyanın tamamı derleme dışında kalır: kod DLL'de hiç yok,
/// bat / --cheats ile açılamaz, dnSpy ile de görünmez (Rapor #02, F-10 kapatıldı).
///
/// Her şey oyunun KENDİ C# API'si ile yapılır. Kullanılan her hile CheatState.MarkUsed()
/// ile işaretlenir; RunRecorder o turu kalıcı kayda yazmaz.
/// </summary>
public class DevMenu : MonoBehaviour
{
    private static readonly float[] Speeds = { 0.25f, 0.5f, 1f, 2f, 4f };

    private bool visible;
    private int speedIndex = 2;   // 1x

    private Health playerHealth;
    private PlayerMana playerMana;
    private CoinWallet wallet;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("[DevMenu]");
        DontDestroyOnLoad(go);
        go.AddComponent<DevMenu>();
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.f1Key.wasPressedThisFrame) visible = !visible;
        if (!visible) return;

        EnsureRefs();

        if (kb.f2Key.wasPressedThisFrame && playerHealth != null)
        { playerHealth.Invulnerable = !playerHealth.Invulnerable; CheatState.MarkUsed(); }

        if (kb.f3Key.wasPressedThisFrame && playerMana != null)
        { playerMana.Infinite = !playerMana.Infinite; CheatState.MarkUsed(); }

        if (kb.f4Key.wasPressedThisFrame && wallet != null)
        { wallet.Add(100); CheatState.MarkUsed(); }

        if (kb.f5Key.wasPressedThisFrame && playerHealth != null)
        { playerHealth.Heal(99999f); CheatState.MarkUsed(); }

        if (kb.f6Key.wasPressedThisFrame)
        { KillWave(); CheatState.MarkUsed(); }

        if (kb.f7Key.wasPressedThisFrame)
        {
            speedIndex = (speedIndex + 1) % Speeds.Length;
            if (!PauseMenu.IsPaused) Time.timeScale = Speeds[speedIndex];
            if (speedIndex != 2) CheatState.MarkUsed();
        }
    }

    private void EnsureRefs()
    {
        if (playerHealth != null) return;

        GameObject p = GameObject.FindWithTag("Player");
        if (p == null) return;
        playerHealth = p.GetComponent<Health>();
        playerMana = p.GetComponent<PlayerMana>();
        wallet = p.GetComponent<CoinWallet>();
    }

    private void KillWave()
    {
        foreach (Health h in Object.FindObjectsByType<Health>(FindObjectsSortMode.None))
        {
            if (h.Team == Team.Enemy && !h.IsDead)
                h.TakeDamage(99999f);
        }
    }

    private void OnGUI()
    {
        if (!visible) return;

        var style = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true };

        GUILayout.BeginArea(new Rect(14, 120, 260, 360), GUI.skin.box);
        GUILayout.Label("<b>GELİŞTİRİCİ MENÜSÜ</b>  (F1 kapat)", style);
        GUILayout.Space(6);

        Row(style, "F2  God Mode", playerHealth != null && playerHealth.Invulnerable);
        Row(style, "F3  Sınırsız Mana", playerMana != null && playerMana.Infinite);
        GUILayout.Label("F4  +100 Coin", style);
        GUILayout.Label("F5  Tam Can", style);
        GUILayout.Label("F6  Dalgayı Temizle", style);
        GUILayout.Label($"F7  Hız: {Speeds[speedIndex]}x", style);

        GUILayout.Space(8);
        GUILayout.Label("<i>Yalnızca editör / development build</i>", style);
        GUILayout.EndArea();
    }

    private static void Row(GUIStyle style, string label, bool on)
    {
        GUILayout.Label($"{label}   <b>{(on ? "<color=#7CFF7C>AÇIK</color>" : "<color=#FF7C7C>kapalı</color>")}</b>", style);
    }
}
#endif
