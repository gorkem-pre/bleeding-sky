# Bleeding Sky — Kendi Oyununa Saldır, Sonra Koru

> Unity 6 ile geliştirdiğim birinci şahıs dalga hayatta kalma oyunu ve onun üzerinde yürüttüğüm uçtan uca bir \*\*oyun güvenliği çalışması\*\*: önce korumasız sürümü kendim test ettim, bulguları raporladım, ardından korumaları yazıp aynı saldırılarla doğruladım.

!\[Unity](https://img.shields.io/badge/Unity-6000.6-black) !\[C#](https://img.shields.io/badge/C%23-.NET%20Standard%202.1-purple) !\[Backend](https://img.shields.io/badge/backend-IL2CPP-blue)

\---

## Oyun

Kızıl ayın altında bir mezarlık. Mezarlardan dalga dalga iskeletler yükseliyor; oyuncu bir büyücü olarak buz büyüleriyle hayatta kalmaya çalışıyor.

* Birinci şahıs hareket, asa ile büyü (mana sistemi)
* NavMesh ile takip eden, mezardan "yükselen" iskeletler; her dalgada artan sayı
* Coin toplama, kalıcı ilerleme (toplam coin, en iyi dalga)
* Ana menü, bölüm seçimi, duraklatma, ayarlar, ölüm ekranı
* Ses sistemi (3D konumlu efektler, müzik/ambiyans geçişleri), özel HUD

\---

## Güvenlik Çalışması

|Aşama|İçerik|
|-|-|
|**v0.1**|Bilinçli olarak korumasız sürüm|
|[Rapor #01](Security/01-save-file-tampering.md)|Kayıt dosyası manipülasyonu: imza yok, aralık kontrolü yok, bozuk veride sessiz veri kaybı|
|[Rapor #02](Security/02-memory-manipulation.md)|Bellek manipülasyonu: korumasız değerler, Mono metadata ifşası, bellek → kalıcı kayıt sızıntısı, build'e sızan araçlar, gizli dev menü|
|**v0.2**|Korumalar|
|[Rapor #03](Security/03-v0.2-verification.md)|Doğrulama: her saldırı aynı adımlarla tekrarlandı; önce/sonra karşılaştırması|

### Önce / Sonra

|Saldırı|v0.1|v0.2|
|-|-|-|
|Kayıt dosyasında coin'i 99999 yapmak|Kabul edildi|İmza tutmadı, yedekten geri yüklendi|
|Bellekte coin'i 500 yapıp turu bitirmek|504 kalıcı kayda geçti|Yalnızca gerçekten toplanan coin kaydedildi (+5)|
|Can değerini bellekte bulup dondurmak|2 taramada bulundu|Değer gizli; dışarıdan yazma tespit edilip düzeltiliyor|
|Otomatik tablo (sınıf/alan adlarıyla)|Bütün değerleri buldu|Kırıldı|
|İskelet prefab'ının canını 1 yapmak|Bütün iskeletler tek vuruşta öldü|Her spawn'da tespit edilip düzeltiliyor|
|Release build'de gizli dev menüyü açmak|Bat ile açılıyordu|Kod release'de hiç yok|

**Bulgu durumu:** 11 bulgudan 9'u kapatıldı, 1'i azaltıldı (F-06: IL2CPP ile denendi; kaynak kod gizlendi, yapı hâlâ görünür). Ayrıntılar Rapor #03'te.

### Uygulanan korumalar

* **İmzalı kayıt:** HMAC-SHA256, zaman-sabit karşılaştırma, atomik yazma + yedek dosya, aralık doğrulama
* **Kaynağı doğrulanan ekonomi:** Kalıcı kayda yalnızca meşru olaylardan tutulan "defter" yazılır; fiziksel üst sınır ikinci katman
* **Bellek koruması:** XOR ile gizlenmiş, çift kodlu değerler + ayrı kayıt defterine karşı doğrulama (`SecureFloat` / `SecureInt`)
* **Build hijyeni:** Dev menü ve editör araçları release build'den derleme düzeyinde çıkarıldı; güvenlik log'ları ayrıntı vermiyor
* **IL2CPP** backend

### Sınırlar (dürüst not)

Bu tek oyunculu bir oyun ve istemci, kullanıcının bilgisayarında çalışıyor. Buradaki önlemler hileyi **imkânsız** değil, **zahmetli ve tespit edilebilir** kılar. Çok oyunculu ya da gerçek ekonomili bir oyunda otorite sunucuda olmalıdır. Kalan riskler Rapor #03'te listelenmiştir.

\---

## Proje Yapısı

```
Assets/Scripts/
  Core/      SaveSystem, SecureTypes, CheatState, RunRecorder, ...
  Combat/    Health
  Player/    hareket, bakış, mana, cüzdan, ölüm
  Spells/    büyü verisi, büyü atma, mermi
  Enemies/   EnemyAI, WaveManager, CoinDrop
  UI/        HUD, menüler, ayarlar
  Audio/     ses kütüphanesi, havuzlu efektler, müzik
  Dev/       geliştirici menüsü (yalnızca editör / development build)
  Editor/    menü/HUD/ses kurulum araçları, build işlemcisi
  Tools/     sahne kurulum araçları (yalnızca editör)
Security/    bulgu ve doğrulama raporları
```

## Çalıştırma

1. Unity **6000.6** ile projeyi aç.
2. Bu repoda Asset Store paketleri **yoktur** (lisans gereği yeniden dağıtılamaz). Kullanılan paketler [CREDITS.md](CREDITS.md)'de listelidir; aynı paketleri Asset Store'dan ekleyerek sahneyi tamamlayabilirsin.
3. `Scenes/MainMenu` → Play.

## Etik

* Bütün testler **yalnızca bu projenin kendi build'leri** üzerinde, kendi bilgisayarımda yapıldı.
* Bu repoda başka oyunlara uygulanabilecek genel amaçlı hile araçları **bulunmaz**. Raporlar yöntemi ve sonuçları belgelemek içindir.

## Krediler

Ses, model ve diğer varlıkların kaynakları için bkz. [CREDITS.md](CREDITS.md).

\---

**Geliştiren:** Görkem Arıtaşı · Unity 6 (URP), C#, TextMeshPro, Input System, AI Navigation

