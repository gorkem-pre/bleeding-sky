# Bulgu Raporu #01 — Kayıt Dosyası Manipülasyonu

|||
|-|-|
|**Hedef**|Bleeding Sky v0.1 (Windows, Mono build)|
|**Bileşen**|`SaveSystem.cs` — `save.json`|
|**Tarih**|2026-11-11|
|**Test eden**|Görkem Arıtaşı|
|**Araç**|Not Defteri|
|**Genel risk**|Yüksek (tek oyunculu oyunda: Orta)|

\---

## Özet

Oyun ilerlemesi ve ayarlar düz metin JSON olarak diske yazılıyor. Dosyada şifreleme, bütünlük kontrolü ve değer doğrulaması yok. Herhangi bir kullanıcı metin editörüyle coin, rekor ve ayar değerlerini istediği gibi değiştirebiliyor. Bozuk veri ise uyarı göstermeden bütün ilerlemenin silinmesine yol açıyor.

**Dosya konumu:** `%USERPROFILE%\\\\\\\\AppData\\\\\\\\LocalLow\\\\\\\\<Company>\\\\\\\\Bleeding Sky\\\\\\\\save.json`

```json
{
    "totalCoins": 19,
    "bestWave": 3,
    "mouseSensitivity": 0.119,
    "masterVolume": 0.649,
    "fullscreen": false
}
```

\---

## Bulgular

### F-01 — Bütünlük kontrolü yok (Tamper) · **Yüksek**

|Adım|Sonuç|
|-|-|
|Oyun kapalıyken `totalCoins: 999999`, `bestWave: 100` yazıldı|Menüde **99999 coin / 100. dalga** göründü|

**Neden:** `JsonUtility.FromJson` dosyadaki değeri olduğu gibi kabul ediyor. Dosyanın oyun dışında değiştirilip değiştirilmediğini anlayacak bir imza ya da hash yok.
**Etki:** Ekonomi ve rekorlar tamamen istemci kontrolünde. İleride dükkân, kilit açma ya da liderlik tablosu eklenirse doğrudan istismar edilebilir.

### F-02 — Aralık doğrulaması yok · **Orta**

|Girdi|Sonuç|
|-|-|
|`totalCoins: -5`|Menüde **-5** gösterildi|
|`mouseSensitivity: 50` (normal ≈ 0.1)|Slider görselde sınıra dayandı, oyunda gerçek değer kullanıldı|

**Neden:** Okunan değerler mantıklı bir aralıkta mı diye kontrol edilmiyor.
**Etki:** Negatif bakiye, taşma (overflow) ve oynanamaz ayarlar.
**Not:** Ayarlar ekranındaki slider, oyuncu ona dokunursa değeri kendi aralığına çekiyor. Bu tesadüfi bir doğrulama; güvenlik önlemi olarak sayılamaz.

### F-03 — Bozuk veride sessiz ve tam veri kaybı · **Orta**

|Girdi|Sonuç|
|-|-|
|`totalCoins: "abc"`|Coin **0**, en iyi dalga **0**, bütün ayarlar varsayılana döndü|
|`totalCoins: 9999999999999999999` (int taşması)|Aynı: her şey **0 / varsayılan**|
|Dosya silindi|Her şey varsayılan; dosya ancak bir sonraki kayıtta yeniden oluştu|

**Neden:** `Load()` içindeki `try/catch`, ayrıştırma hatasında **bütün dosyayı** atıp `new SaveData()` döndürüyor. Hata sadece `Player.log`'a uyarı olarak yazılıyor; oyuncuya hiçbir şey gösterilmiyor. Bir sonraki `Save()` çağrısı bozuk dosyanın üstüne yazıyor, yani orijinal veri kalıcı olarak kayboluyor.
**Etki:** Disk hatası ya da yarım yazılmış bir dosya oyuncunun bütün ilerlemesini uyarısız silebilir. Yedek de yok.

### F-04 — Okuma zamanlaması (bilgi) · **Düşük**

Dosya sadece ilk erişimde okunup bellekte tutuluyor. Oyun açıkken yapılan değişiklik görünmüyor ve bir sonraki kayıtta eziliyor. Bu bir zafiyet değil, ama istismarı biraz zorlaştırıyor: saldırganın oyunu kapatması gerekiyor.

\---

## Önerilen Çözümler (v0.2)

|#|Çözüm|Hangi bulguyu kapatır|
|-|-|-|
|1|**HMAC-SHA256 imza:** JSON + gizli anahtar → imza; yüklerken doğrula, uyuşmazsa reddet|F-01|
|2|**Aralık doğrulama (`Validate()`)**: `Clamp`, negatif değerleri reddet, ayarları slider sınırlarına çek|F-02|
|3|**Yedek + atomik yazma:** önce `save.tmp`'ye yaz, sonra yerine taşı; eski dosyayı `save.bak` olarak sakla; bozuksa yedekten dön|F-03|
|4|**Oyuncuya bildirim:** "Kayıt bozuk, yedekten yüklendi" mesajı|F-03|
|5|(İsteğe bağlı) AES ile şifreleme: okunabilirliği kaldırır, tek başına güvenlik sağlamaz|F-01 (kısmen)|

> \\\\\\\*\\\\\\\*Önemli sınır:\\\\\\\*\\\\\\\* İstemcideki her anahtar, yeterli çabayla bulunabilir. Mono build'de `Assembly-CSharp.dll` dnSpy ile okunabildiği için gizli HMAC anahtarı da çıkarılabilir (bkz. Rapor #05). Tek oyunculu oyunda amaç saldırıyı \\\\\\\*\\\\\\\*imkânsız\\\\\\\*\\\\\\\* değil, \\\\\\\*\\\\\\\*zahmetli\\\\\\\*\\\\\\\* kılmaktır. Gerçek koruma (çok oyunculu, ekonomi) için doğrulama sunucu tarafında yapılmalıdır.

\---

## Öğrenilenler

* İstemci tarafındaki veri, kullanıcının kontrolündedir.
* Bir hatayı "sessizce yutmak" çökmeyi engeller, ama veri kaybını ve saldırı girişimlerini gizler.
* Arayüzdeki sınırlar (slider) veri doğrulaması yerine geçmez.

