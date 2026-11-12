using UnityEngine;

/// <summary>
/// "Bu turda hile kullanıldı mı?" bayrağı. Her build'de derlenir (RunRecorder buna bakar).
/// Yalnızca DevMenu ayarlar; DevMenu release build'de olmadığı için orada her zaman false kalır.
/// </summary>
public static class CheatState
{
    public static bool Used { get; private set; }

    public static void MarkUsed() => Used = true;

    /// <summary>Yeni tur başlarken çağrılır. Hız 1x değilse tur zaten hileli başlıyor demektir.</summary>
    public static void ResetForNewRun() => Used = !Mathf.Approximately(Time.timeScale, 1f);
}
