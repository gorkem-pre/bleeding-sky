using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Bellek koruması (Rapor #02: F-05, F-07). Üç katman:
///
///  1) GİZLEME: Değer bellekte düz tutulmaz; rastgele bir anahtarla XOR'lanır.
///     Her yazmada anahtar yenilenir → CE'de "Exact Value: 100" araması hiçbir şey bulmaz,
///     "changed/unchanged" taraması da her yazmada değişen gürültüyle boğulur.
///
///  2) ÇİFT KODLAMA: Aynı değer iki bağımsız anahtarla iki kez saklanır.
///     Biri dışarıdan değiştirilirse ikisi uyuşmaz → tespit.
///
///  3) KAYIT DEFTERİ: Son meşru değer, ayrı bir statik sözlükte (yine gizlenmiş) tutulur.
///     Struct'ın tamamı CE ile dondurulsa bile, deftere göre "geride kalır" → tespit + onarım.
///
/// Tespit edilince: AntiTamper.Report → CheatState.MarkUsed (tur kalıcı kayda yazılmaz)
/// ve değer defterdeki doğru değere geri döndürülür.
///
/// DÜRÜST SINIR: Bunların hepsi istemcide. Mono metadata yapıyı gösterdiği için (F-06)
/// kararlı bir saldırgan struct'ı, sözlüğü ve anahtarları bulup hepsini tutarlı biçimde
/// değiştirebilir. Amaç imkânsız kılmak değil: tek bir değeri bulup değiştirmeyi
/// yetersiz, birçok yapıyı birlikte anlamayı zorunlu kılmak.
/// </summary>
public static class AntiTamper
{
    private static uint seed = (uint)System.Environment.TickCount | 1u;   // 0 olmasın
    private static readonly int StoreKey = NextKey();
    private static readonly Dictionary<int, int> store = new Dictionary<int, int>();
    private static int nextId;

    /// <summary>Bu oturumda en az bir kurcalama tespit edildi mi?</summary>
    public static bool Detected { get; private set; }

    /// <summary>xorshift32: hızlı, yeterince karışık (kriptografik değil, gerek de yok).</summary>
    public static int NextKey()
    {
        seed ^= seed << 13;
        seed ^= seed >> 17;
        seed ^= seed << 5;
        return (int)seed;
    }

    internal static int NewId() => ++nextId;
    internal static void Store(int id, int bits) => store[id] = bits ^ StoreKey;
    internal static bool Matches(int id, int bits) => store.TryGetValue(id, out int v) && (v ^ StoreKey) == bits;
    internal static int Recall(int id) => store.TryGetValue(id, out int v) ? v ^ StoreKey : 0;
    internal static void Forget(int id) => store.Remove(id);

    internal static void Report()
    {
        if (!Detected)
        {
            Detected = true;
#if UNITY_EDITOR || DEBUG
            Debug.LogWarning("AntiTamper: bellekte değiştirilmiş değer tespit edildi.");
#else
            Debug.LogWarning("Bütünlük kontrolü başarısız.");   // F-11: release'de ayrıntı yok
#endif
        }
        CheatState.MarkUsed();
    }
}

/// <summary>Bellekte gizlenmiş float. Sınıf alanı olarak kullan: private SecureFloat hp;  hp.Value = 100f;</summary>
public struct SecureFloat
{
    private int id, k1, e1, k2, e2;

    [StructLayout(LayoutKind.Explicit)]
    private struct Bits { [FieldOffset(0)] public float f; [FieldOffset(0)] public int i; }

    private static int ToBits(float f) => new Bits { f = f }.i;
    private static float FromBits(int i) => new Bits { i = i }.f;

    public float Value
    {
        get
        {
            if (id == 0) return 0f;   // hiç yazılmamış
            int a = e1 ^ k1, b = e2 ^ k2;
            if (a == b && AntiTamper.Matches(id, a)) return FromBits(a);

            // Tutarsızlık: rapor et, defterdeki son meşru değere onar
            AntiTamper.Report();
            int good = AntiTamper.Recall(id);
            Write(good);
            return FromBits(good);
        }
        set => Write(ToBits(value));
    }

    private void Write(int bits)
    {
        if (id == 0) id = AntiTamper.NewId();
        k1 = AntiTamper.NextKey();
        k2 = AntiTamper.NextKey();
        e1 = bits ^ k1;
        e2 = bits ^ k2;
        AntiTamper.Store(id, bits);
    }

    /// <summary>Sahibi yok edilirken çağır (deftere boşuna kayıt kalmasın).</summary>
    public void Release()
    {
        if (id != 0) AntiTamper.Forget(id);
        id = 0;
    }
}

/// <summary>Bellekte gizlenmiş int. SecureFloat ile aynı mantık.</summary>
public struct SecureInt
{
    private int id, k1, e1, k2, e2;

    public int Value
    {
        get
        {
            if (id == 0) return 0;
            int a = e1 ^ k1, b = e2 ^ k2;
            if (a == b && AntiTamper.Matches(id, a)) return a;

            AntiTamper.Report();
            int good = AntiTamper.Recall(id);
            Write(good);
            return good;
        }
        set => Write(value);
    }

    private void Write(int v)
    {
        if (id == 0) id = AntiTamper.NewId();
        k1 = AntiTamper.NextKey();
        k2 = AntiTamper.NextKey();
        e1 = v ^ k1;
        e2 = v ^ k2;
        AntiTamper.Store(id, v);
    }

    public void Release()
    {
        if (id != 0) AntiTamper.Forget(id);
        id = 0;
    }
}
