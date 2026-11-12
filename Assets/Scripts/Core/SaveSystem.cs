using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// Kalıcı kayıt. v0.2'de sağlamlaştırıldı. Rapor #01'deki üç bulguyu kapatır:
///   F-01 (bütünlük): HMAC-SHA256 imza. Dosya elle değiştirilirse imza tutmaz, reddedilir.
///   F-02 (aralık):   Validate() bütün değerleri makul sınırlara çeker.
///   F-03 (veri kaybı): Atomik yazma (.tmp → asıl) + yedek (.bak). Bozuk dosyada yedekten kurtarma.
///
/// DÜRÜST SINIR (Rapor #01'de de yazılı): HMAC anahtarı istemcinin içinde.
/// Mono build okunabildiği için (Rapor #02, F-06) yeterince kararlı biri anahtarı çıkarıp
/// geçerli imza üretebilir. Amaç saldırıyı İMKANSIZ değil, ZAHMETLİ kılmak:
///   - Not Defteri ile elle değiştirme → artık çalışmıyor (imza tutmuyor).
///   - Anahtarı çıkarıp yeniden imzalama → mümkün ama ciddi iş ister.
/// Gerçek koruma (çok oyunculu/ekonomi) sunucu tarafında olmalı.
/// </summary>
[System.Serializable]
public class SaveData
{
    public int totalCoins;
    public int bestWave;

    public float mouseSensitivity = 0.1f;
    public float masterVolume = 1f;
    public bool fullscreen = true;

    // Hile işareti: bir oturumda dev menü kullanıldıysa işaretlenir (bkz. RunRecorder).
    public bool cheatsUsed;

    /// <summary>Bütün değerleri makul sınırlara çeker. Sınırlar ayar slider'larıyla aynı.</summary>
    public void Validate()
    {
        totalCoins = Mathf.Clamp(totalCoins, 0, 100_000_000);
        bestWave = Mathf.Clamp(bestWave, 0, 10_000);
        mouseSensitivity = Mathf.Clamp(mouseSensitivity, 0.02f, 0.3f);   // slider: 0.02–0.3
        masterVolume = Mathf.Clamp01(masterVolume);                  // slider: 0–1
    }
}

public static class SaveSystem
{
    // İmza anahtarı. İstemcide olduğu için mutlak koruma değil (yukarıdaki nota bak).
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("BleedingSky-v0.2-7f3a9c21-save-key");

    private static string Dir => Application.persistentDataPath;
    private static string MainPath => Path.Combine(Dir, "save.json");
    private static string BakPath => Path.Combine(Dir, "save.bak");
    private static string TmpPath => Path.Combine(Dir, "save.tmp");

    private static SaveData cached;

    /// <summary>Son yüklemede dosya bozuk/kurcalanmış mıydı? UI "kayıt sıfırlandı" diyebilir.</summary>
    public static bool LastLoadWasTampered { get; private set; }

    public static SaveData Data
    {
        get
        {
            if (cached == null) cached = Load();
            return cached;
        }
    }

    // İmzalı zarf: diskteki dosyanın gerçek biçimi. { "payload": "...", "sig": "..." }
    [System.Serializable]
    private class Envelope { public string payload; public string sig; }

    private static string Sign(string text)
    {
        using var h = new HMACSHA256(Key);
        byte[] mac = h.ComputeHash(Encoding.UTF8.GetBytes(text));
        var sb = new StringBuilder(mac.Length * 2);
        foreach (byte b in mac) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    /// <summary>Zaman-sabit karşılaştırma: imza doğrularken erken çıkış yok (timing attack'a kapalı).</summary>
    private static bool SignatureOk(Envelope env)
    {
        if (env == null || env.payload == null || env.sig == null) return false;
        string expected = Sign(env.payload);
        if (expected.Length != env.sig.Length) return false;
        int diff = 0;
        for (int i = 0; i < expected.Length; i++) diff |= expected[i] ^ env.sig[i];
        return diff == 0;
    }

    /// <summary>Bir dosyayı oku, imzasını doğrula, SaveData'ya çevir. Başarısızsa null.</summary>
    private static SaveData TryReadFile(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            var env = JsonUtility.FromJson<Envelope>(File.ReadAllText(path));
            if (!SignatureOk(env)) return null;              // imza tutmuyor → kurcalanmış

            var data = JsonUtility.FromJson<SaveData>(env.payload);
            if (data == null) return null;
            data.Validate();                                 // F-02
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Kayıt okunamadı ({Path.GetFileName(path)}): {e.Message}");
            return null;
        }
    }

    private static SaveData Load()
    {
        LastLoadWasTampered = false;

        // 1) Asıl dosya
        SaveData data = TryReadFile(MainPath);
        if (data != null) return data;

        // 2) Asıl bozuksa yedek
        data = TryReadFile(BakPath);
        if (data != null)
        {
            Debug.LogWarning("Asıl kayıt bozuk/kurcalanmış; yedekten yüklendi.");
            LastLoadWasTampered = true;
            return data;
        }

        // 3) İkisi de yoksa/bozuksa sıfırdan. Dosya hiç yoktuysa bu normal (ilk açılış).
        if (File.Exists(MainPath) || File.Exists(BakPath))
        {
            Debug.LogWarning("Kayıt kurtarılamadı; varsayılanlarla başlanıyor.");
            LastLoadWasTampered = true;
        }
        return new SaveData();
    }

    public static void Save()
    {
        try
        {
            Data.Validate();                                         // yazmadan önce de sınırla
            string payload = JsonUtility.ToJson(Data);
            var env = new Envelope { payload = payload, sig = Sign(payload) };
            string json = JsonUtility.ToJson(env, true);

            // Atomik yazma: önce .tmp'ye yaz, sonra asıl dosyanın üstüne taşı (F-03).
            // Yazma yarıda kesilse bile asıl dosya bozulmaz.
            File.WriteAllText(TmpPath, json);

            if (File.Exists(MainPath))
            {
                // Eski asıl dosyayı .bak yap, sonra .tmp'yi asıl yap.
                if (File.Exists(BakPath)) File.Delete(BakPath);
                File.Replace(TmpPath, MainPath, BakPath);           // tek işlemde: main→bak, tmp→main
            }
            else
            {
                File.Move(TmpPath, MainPath);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Kayıt yazılamadı: {e.Message}");
        }
    }
}
