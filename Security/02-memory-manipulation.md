# Bulgu Raporu #02 — Bellek Manipülasyonu

|||
|-|-|
|**Hedef**|Bleeding Sky v0.1 (Windows x64, Mono build)|
|**Bileşenler**|`CoinWallet`, `PlayerMana`, `Health`, `RunRecorder`, `DevMenu`, build içeriği|
|**Tarih**|2026-11-12|
|**Test eden**|Görkem Arıtaşı|
|**Araç**|Cheat Engine 7.x (değer tarama, dondurma, Mono Dissector)|
|**Genel risk**|Yüksek (tek oyunculu oyunda: Orta)|

\---

## Özet

Oyunun bütün kritik değerleri (coin, mana, can) bellekte düz `int` / `float` olarak tutuluyor ve harici bir araçla okunup yazılabiliyor. Mono build, sınıf ve alan adlarını offset'leriyle birlikte açığa çıkardığı için değerleri bulmak tarama bile gerektirmiyor. Bellekte yapılan bir değişiklik, oyunun kendi kayıt mekanizması üzerinden kalıcı kayda geçiyor.

\---

## Bulgular

### F-05 — Değerler bellekte korumasız · **Yüksek**

|Hedef|Tip|Bulma yöntemi|Tarama sayısı|Sonuç|
|-|-|-|-|-|
|Coin (`CoinWallet.RunCoins`)|4 Bytes|Exact Value|3|11 → 500|
|Mana (`PlayerMana.Current`)|Float|Exact Value (regen gecikmesi sırasında)|1|Dondurularak sınırsız mana|
|Can (`Health.Current`)|Float|Exact Value|2|Dondurularak ölümsüzlük|

**Neden:** Değerler şifrelenmemiş, gizlenmemiş, doğrulanmıyor. Ekranda görünen sayıyla bellekteki sayı aynı.
**Etki:** Oyuncu kaynakları sınırsız hâle getirebiliyor, oyunun zorluğu tamamen devre dışı kalıyor.

### F-06 — Mono yapı bilgisi açığa çıkıyor · **Yüksek**

CE → **Mono → Dissect mono** ile `Assembly-CSharp` içindeki bütün sınıflar, alanlar, tipler ve **offset'ler** isimleriyle listelendi:

```
Health:  +0x38 team | +0x3C maxHealth | +0x44 Current | +0x48 IsDead
```

**Find instances of class** ile sahnedeki bütün `Health` nesneleri bulundu; `team == 0` olan oyuncunun nesnesi. Adres = nesne + offset; tarama gerekmiyor.

**Prefab manipülasyonu:** Bulunan nesnelerden biri, sahnede olmayan **iskelet prefab'ı**ydı (`team 1`, `Current 0`, event'ler `null`). Prefab'ın `maxHealth` değeri 75 → 1 yapılınca **sonradan doğan bütün iskeletler** tek vuruşta öldü. Tek bir yazma işlemi bütün düşmanları etkiledi.

**Neden:** Mono backend, IL kodunu ve metadata'yı (isimler, tipler, yapı) build içinde tutuyor.
**Etki:** Tersine mühendislik maliyeti çok düşük. Yeni başlayan biri bile dakikalar içinde stabil hile yapabilir.

### F-07 — Bellek ↔ UI tutarsızlığı (tespit fırsatı) · **Bilgi**

|Değer|CE ile değiştirildi|HUD'un davranışı|
|-|-|-|
|Coin|11 → 500|Bir sonraki coin'e kadar eski değeri gösterdi|
|Mana (100'de donmuş)|Her atışta 90 → 100|**90'da takılı kaldı**|
|Can (100'de donmuş)|Her vuruşta 85 → 100|**85'te takılı kaldı**|

**Neden:** HUD event tabanlı (`OnHealthChanged`, `OnManaChanged`, `OnCoinsChanged`). Harici bir yazma işlemi event tetiklemiyor.
**Önemi:** Bu bir zafiyet değil; **anti-cheat için imza**. "Değer değişti ama onu değiştirecek kod çalışmadı" durumu tespit edilebilir (bkz. Öneri 2).

### F-08 — Bellek manipülasyonu kalıcı kayda sızıyor · **Yüksek**

CE ile `RunCoins` 504'e çıkarıldı. Tur bitince `RunRecorder`:

```csharp
SaveSystem.Data.totalCoins += wallet.RunCoins;  // 504, kontrolsüz
SaveSystem.Save();
```

Sonuç: Kayıt dosyasına **hiç dokunulmadan** toplam coin kalıcı olarak arttı.

**Neden:** Geçici ve güvenilmeyen bir değer (bellek), kontrol edilmeden kalıcı ve güvenilen bir depoya yazılıyor (*trust boundary* ihlali). İki dalgada en fazla yaklaşık 21 coin düşebiliyorken 504 değeri kabul edildi.
**Etki:** Rapor #01'deki dosya imzası (HMAC) bu yolu **kapatmaz**: veri oyunun kendisi tarafından meşru şekilde imzalanıp yazılır.

### F-09 — Editör araçları build'e dahil · **Düşük**

Dissect listesinde `GraveyardBuilder` görüldü (muhtemelen `BoundaryScatter`, `CoverScatter` de var).
**Neden:** Dosyalar `Assets/Scripts/Tools` içinde; `Editor` klasöründe değiller ve `#if UNITY\_EDITOR` ile sarılmamışlar.
**Etki:** Gereksiz saldırı yüzeyi ve geliştirme sürecine dair bilgi ifşası. Ticari oyunlarda unutulan debug kodları, hilelerin sık kullanılan giriş noktalarındandır.

### F-10 — Release build'de launch flag ile açılan gizli dev menü · **Düşük (bilinçli kabul edilen risk)**

`DevMenu` sınıfı release build'e dahil ediliyor. Oyun `--cheats` argümanıyla başlatılırsa (örn. `Hileli Baslat.bat`) ya da exe'nin yanında `cheats.enabled` dosyası varsa F1 menüsü açılıyor: god mode, sınırsız mana, coin ekleme, dalga temizleme, oyun hızı.

|Başlatma|Sonuç|
|-|-|
|`Bleeding Sky.exe` (doğrudan)|Menü yok|
|`Hileli Baslat.bat` (`--cheats`)|F1 menüsü açık|

**Neden:** Menü, test ve gösterim kolaylığı için bilinçli olarak release'de bırakıldı; erişim yalnızca bayrağın adının bilinmemesine dayanıyor (*security through obscurity*).
**Risk:** `--cheats` dizesi derlenmiş DLL'de düz metin olarak duruyor ve Mono build okunabilir olduğu için (F-06) kolayca bulunabilir. Bayrağı bilen herkes menüyü açabilir; sıfırdan hile yazmaya gerek kalmaz.
**Etki değerlendirmesi:**

|Senaryo|Risk|
|-|-|
|Tek oyunculu, offline (v0.1)|Düşük: yalnızca oyuncunun kendi deneyimini etkiler|
|Liderlik tablosu / başarımlar|Orta-Yüksek: hileli skorlar herkese açık listelere girer|
|Çok oyunculu / gerçek para ekonomisi|Kritik|

**Karar:** v0.1 lab sürümü için risk kabul edildi. Herkese açık (Steam) sürümde kabul edilmez.

\---

## Önerilen Çözümler (v0.2)

|#|Çözüm|Kapatır|Not|
|-|-|-|-|
|1|**Değer gizleme (obfuscated types):** `ObscuredInt` / `ObscuredFloat`: değeri XOR anahtarıyla saklayıp sadece okurken çözmek|F-05|Exact Value taramasını kırar; "changed / unchanged" taramasını kırmaz|
|2|**Gölge kopya + tutarlılık kontrolü:** değeri iki yerde (biri gizli) tut; uyuşmazsa hile tespit edilmiş say|F-05, F-07|Dondurma ve doğrudan yazmayı yakalar|
|3|**Değişim hızı doğrulaması:** mana saniyede en fazla 15 artabilir, can sadece `TakeDamage`/`Heal` ile değişebilir|F-07|Dondurmayı yakalar|
|4|**Kayıt öncesi makullük kontrolü:** `RunCoins <= öldürülen düşman × 3` gibi; şüpheliyse kaydetme ve logla|F-08|En ucuz, en etkili çözüm|
|5|**IL2CPP backend** + (isteğe bağlı) obfuscator|F-06|Metadata'yı kaldırır, analizi ciddi şekilde zorlaştırır; imkânsız kılmaz (Il2CppDumper)|
|6|Editör araçlarını `Editor` klasörüne taşımak / `#if UNITY\_EDITOR`|F-09|Basit hijyen|
|7|`DevMenu`'yü yeniden `#if UNITY\_EDITOR \|\| DEBUG` ile sarmak: release'de kod hiç bulunmaz|F-10|Tek satır; doğrulama: release build'de bat ile F1 çalışmamalı, `DevMenu` DLL'de olmamalı|
|8|(Alternatif) Menü kalacaksa kullanımını işaretlemek: `cheatsUsed: true` kaydı, o oturumda rekor/başarım güncellenmez|F-10|Gizlemek yerine sonuçlarını sınırlamak|

> \*\*Sınır:\*\* İstemci, kullanıcının makinesinde çalışır ve kullanıcı belleğin tam sahibidir. Yukarıdaki önlemler hileyi \*\*zahmetli\*\* hâle getirir, \*\*imkânsız\*\* kılmaz. Çok oyunculu bir sürümde otorite sunucuda olmalıdır: istemci sadece girdi göndermeli, sonucu sunucu hesaplamalıdır.

\---

## Öğrenilenler

* **Heap ve ASLR:** Nesneler her çalıştırmada farklı adreslerde oluşur; adres değişir, **yapı** (offset'ler) değişmez.
* **Adres = nesne başlangıcı + offset:** Mono metadata'sı bu haritayı hazır veriyor.
* **Prefab'lar da bellekte birer nesne:** "kalıbı" değiştirmek bütün kopyaları etkiler.
* **Event tabanlı UI**, istemeden bir tespit sinyali üretiyor.
* **Güven sınırları:** Bellekteki her değer güvenilmez kabul edilmeli; kalıcı depoya yazmadan önce doğrulanmalı.
* **Gizlilik güvenlik değildir:** Gizli bir bayrak, istemcideki kod okunabildiği sürece gizli kalmaz. Ya kod hiç bulunmamalı ya da sonuçları sınırlanmalı.
* **Kabul edilen risk de bir karardır:** Her bulgu kapatılmak zorunda değil, ama gerekçesiyle kayda geçirilmeli.

## Ekler

* `BleedingSky.CT`: Cheat Engine tablosu (yalnızca bu proje için)

